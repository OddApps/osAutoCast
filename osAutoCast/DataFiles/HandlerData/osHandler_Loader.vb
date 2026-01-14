Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.LoadTextVisualType
Imports osAutoCast.osLoadingElements
Imports osProgLoad = osAutoCast.osLoadingElements.osLoadingProgressBar

Public Class osHandler_Loader

    Private ReadOnly _triggerLoadTextVis As Func(Of LoadTextVisualType, String, Task)

    Private ReadOnly _fadeTextOut As Func(Of LoadTextVisualType, Task)
    Private ReadOnly _fadeTextIn As Func(Of LoadTextVisualType, Task)

    Private ReadOnly _ValidateLoadText As Func(Of LoadTaskType, Boolean)

    Private _LoaderText As UIElement

    Private _ProgLoadBar As Func(Of osProgLoad)

    Private ReadOnly _setValue As Action(Of Double)
    Private ReadOnly _setLoadText As Action(Of LoadTaskType)

    Private _currentValue As Double

    Private _animationCts As CancellationTokenSource

    Private _renderHandler As EventHandler
    Private _renderStopwatch As Stopwatch
    Private _renderDuration As TimeSpan
    Private _renderFrom As Double
    Private _renderTo As Double
    Private _renderEasing As LoaderEasing
    Private _renderToken As CancellationToken
    Private _renderTcs As TaskCompletionSource(Of Boolean)

    Private Const DriftSpeed As Double = 10.5

    Private ReadOnly _stages As IReadOnlyList(Of osLoader_Stage)

    Private _stageCnt As Integer
    Private _stageLast As Integer

    Private _cts As CancellationTokenSource
    Private _currentStageIndex As Integer = -1

    Private _smoothedValue As Double
    Private Const SmoothingFactor As Double = 0.175

    Private ProgMax As Double
    Private _ProgLoad As osProgLoad

    Private _ProgLoad_Ratio As Double

    Public objLoadAni_TextVis As Storyboard = Nothing

    Private objAni_VisIn As ColorAnimation
    Private objAni_VisOut As ColorAnimation

    Public objTextBrush As SolidColorBrush

    Public ReadOnly Property objVisTxtColor As DependencyProperty
        Get
            Return SolidColorBrush.ColorProperty
        End Get
    End Property

    Public ReadOnly Property objLoadVis_TextFadeOut As ColorAnimation
        Get
            Return objAni_VisOut
        End Get
    End Property

    Public ReadOnly Property objLoadVis_TextFadeIn As ColorAnimation
        Get
            Return objAni_VisIn
        End Get
    End Property

    Private ReadOnly Property ProgressValue As Double
        Get
            Return _ProgLoad_Ratio * _ProgLoad.ActualProgress
        End Get
    End Property

    Public Sub New(objLoadStages As osLoaderStageIdx, pMax As Double, setValueAction As Action(Of Double),
                   objTextVis_Out As Func(Of LoadTextVisualType, Task), objTextVis_In As Func(Of LoadTextVisualType, Task))

        ProgMax = pMax

        _setValue = setValueAction
        _fadeTextOut = objTextVis_Out
        _fadeTextIn = objTextVis_In
        _stages = objLoadStages.LoadStages

        _stageCnt = _stages.Count
        _stageLast = _stages.Count - 1

        _currentValue = 0
        _smoothedValue = 0
        _setValue(_currentValue)
    End Sub

    Public Sub New(objLoadStages As osLoaderStageIdx, objProgBar As osProgLoad, setValueAction As Action(Of Double), objLoaderText As UIElement, funcLoadText As Action(Of LoadTaskType), funcValLoadText As Func(Of LoadTaskType, Boolean),
                   objProg_Load As Func(Of osProgLoad), objTextVis_Out As Func(Of LoadTextVisualType, Task), objTextVis_In As Func(Of LoadTextVisualType, Task))

        _ProgLoadBar = objProg_Load

        Dim objPLB = _ProgLoadBar()
        _ProgLoad = objPLB

        ProgMax = _ProgLoad.Maximum

        _ProgLoad_Ratio = ProgMax / _ProgLoad.ActualWidth

        _setValue = setValueAction

        _LoaderText = objLoaderText
        _setLoadText = funcLoadText

        With New osLoadTextColors(_LoaderText, objTextBrush)
            objAni_VisIn = New ColorAnimation(.txtHidden, .txtShown,
                                    TimeSpan.FromMilliseconds(150), FillBehavior.HoldEnd)
            objAni_VisOut = New ColorAnimation(.txtShown, .txtHidden,
                                    TimeSpan.FromMilliseconds(150), FillBehavior.HoldEnd)
        End With

        _ValidateLoadText = funcValLoadText

        _fadeTextOut = objTextVis_Out
        _fadeTextIn = objTextVis_In
        _stages = objLoadStages.LoadStages

        _stageCnt = _stages.Count
        _stageLast = _stages.Count - 1

        _currentValue = 0
        _smoothedValue = 0
        _setValue(_currentValue)
    End Sub

    Private Sub CalcProgressValue(targetValue As Double)
        _smoothedValue += (targetValue - _smoothedValue) * SmoothingFactor
    End Sub

    Private Sub UpdateSmoothedValue(targetValue As Double, Optional setForce As Boolean = False)
        _currentValue = targetValue

        If targetValue > (ProgMax * 0.985) Then
            _setValue(targetValue)
        Else
            CalcProgressValue(targetValue)
            _setValue(_smoothedValue)
        End If
    End Sub

    Private Shared Function CreateEasing(e As LoaderEasing) As IEasingFunction
        Select Case e
            Case LoaderEasing.EaseIn
                Return New QuadraticEase With {.EasingMode = EasingMode.EaseIn}

            Case LoaderEasing.EaseOut
                Return New QuadraticEase With {.EasingMode = EasingMode.EaseOut}

            Case LoaderEasing.EaseInOut
                Return New ExponentialEase With {.EasingMode = EasingMode.EaseInOut, .Exponent = 2}

            Case LoaderEasing.SmoothStep
                Return New SineEase With {.EasingMode = EasingMode.EaseInOut}

            Case Else
                Return Nothing ' Linear
        End Select
    End Function

    Private Function CalcActualProgress() As Double
        Return _ProgLoad_Ratio * _ProgLoad.ActualProgress
    End Function

    Private Async Function AnimateAsync(fromValue As Double, toValue As Double, duration As TimeSpan,
                                        easing As LoaderEasing, token As CancellationToken) As Task

        CancelCurrentAnimation()

        _animationCts = CancellationTokenSource.CreateLinkedTokenSource(token)
        Dim linkedToken = _animationCts.Token

        Dim completed = New osAutoAnimation()

        Dim anim As New DoubleAnimation With {
            .From = fromValue, .To = toValue,
            .Duration = New Duration(duration),
            .FillBehavior = FillBehavior.HoldEnd,
            .EasingFunction = CreateEasing(easing)
        }

        AddHandler anim.Completed, Sub()
                                       completed.SetAnimation()
                                   End Sub

        Using linkedToken.Register(
            Sub()
                PrepDispatcher().InvokeAsync(
                    Sub()
                        _ProgLoad.BeginAnimation(
                        osLoadingProgressBar.ProgressProperty, Nothing)
                    End Sub, DispatcherPriority.Render)

                completed.SetAnimation()
            End Sub)

            Dim chkFrom = If(toValue >=
                ProgressValue, toValue, ProgressValue)

            Await PrepDispatcher().InvokeAsync(
                Sub()
                    _setValue(chkFrom)
                    _ProgLoad.BeginAnimation(
                    osLoadingProgressBar.ProgressProperty, anim)
                End Sub, DispatcherPriority.Render)

            Await completed.WaitAsync(linkedToken)
        End Using
    End Function

    Private Function DriftTowardAsync(targetValue As Double, token As CancellationToken) As Task
        Dim duration = TimeSpan.FromSeconds(Math.Abs(targetValue - ProgressValue) / DriftSpeed)

        token.Register(Sub() StopRendering())
        Return AnimateAsync(CalcActualProgress(), targetValue,
                             duration, LoaderEasing.Linear, token)
    End Function

    Private Function VerifyLastStage(idxStage As Integer) As Boolean
        Return (idxStage = _stageLast)
    End Function

    Private Function VerifyTextVis(txtMsg As String) As Boolean
        Return Not txtMsg = "skip"
    End Function

    Private Sub FadeTextOut(valTaskType As LoadTaskType, objVisReset As osAutoAnimation)
        PrepDispatcher().InvokeAsync(
                Sub()
                    Dim handler As EventHandler = Nothing

                    handler =
                        Sub(sender, e)
                            RemoveHandler objAni_VisOut.Completed, handler

                            objVisReset.SetAnimation()
                            objTextBrush.BeginAnimation(objVisTxtColor, Nothing)
                        End Sub

                    AddHandler objAni_VisOut.Completed, handler

                    objTextBrush.BeginAnimation(objVisTxtColor, objAni_VisOut)
                End Sub)
    End Sub

    Private Sub FadeTextIn(valTaskType As LoadTaskType, objVisReset As osAutoAnimation)
        PrepDispatcher().InvokeAsync(
                Sub()
                    Dim handler As EventHandler = Nothing

                    handler =
                        Sub(sender, e)
                            RemoveHandler objAni_VisIn.Completed, handler

                            objVisReset.SetAnimation()
                            objTextBrush.BeginAnimation(objVisTxtColor, Nothing)
                        End Sub

                    AddHandler objAni_VisIn.Completed, handler

                    _setLoadText(valTaskType)
                    objTextBrush.BeginAnimation(objVisTxtColor, objAni_VisIn)
                End Sub)
    End Sub

    Public Async Function BeginLoadStage(initLoad As Boolean, Optional stageIndex As Integer = 0) As Task
        If initLoad Then stageIndex = 0

        If stageIndex < 0 OrElse stageIndex >= _stages.Count Then
            Throw New ArgumentOutOfRangeException(NameOf(stageIndex))
        End If

        CancelCurrent()

        _cts = New CancellationTokenSource()
        Dim token = _cts.Token

        _currentStageIndex = stageIndex

        With _stages(stageIndex)
            Dim taskType = .LoadTaskData.LoadType

            Dim gap As Double = CalcActualProgress()
            Dim effectiveEndValue = Math.Max(.LoadStageData.EndValue, gap)

            Dim objTask_Visual As Task = Nothing

            Select Case stageIndex
                Case = 0
                    objTask_Visual = AnimateAsync(gap, effectiveEndValue,
                                                  .LoadStageData.Duration, .LoadStageData.Easing, token)

                    Dim fadeInSignal As New osAutoAnimation()
                    FadeTextIn(taskType, fadeInSignal)
                Case > 0
                    If _ValidateLoadText(taskType) Then
                        Dim fadeOutSignal As New osAutoAnimation()
                        FadeTextOut(taskType, fadeOutSignal)
                        objTask_Visual = AnimateAsync(gap, effectiveEndValue,
                                                  .LoadStageData.Duration, .LoadStageData.Easing, token)

                        Dim fadeInSignal As New osAutoAnimation()
                        FadeTextIn(taskType, fadeInSignal)
                        Await fadeInSignal.WaitAsync(token)
                    Else
                        objTask_Visual = AnimateAsync(gap, effectiveEndValue,
                                                  .LoadStageData.Duration, .LoadStageData.Easing, token)
                    End If
            End Select

            Dim workTask As Task = Nothing
            Dim objTaskReport As New TaskStatusReport

            If .LoadTaskData.LoadTask IsNot Nothing Then
                workTask = Task.Run(
                    Sub()
                        .LoadTaskData.LoadTask.Invoke(objTaskReport)
                    End Sub)

                If VerifyLastStage(stageIndex) Then
                    Await objTask_Visual
                    Await workTask
                Else

                    Dim first = Await Task.WhenAny(objTask_Visual, workTask)

                    If first Is workTask Then
                        Await objTask_Visual
                    End If

                End If
                CancelRenderingOnly()
            End If

            Dim nextIndex = stageIndex + 1

            If nextIndex < _stageCnt AndAlso
                Not token.IsCancellationRequested Then

                Dim nextStartValue = _stages(nextIndex).LoadStageData.StartValue

                If gap < nextStartValue Then
                    Await DriftTowardAsync(nextStartValue, token)
                End If

                Await BeginLoadStage(False, nextIndex)
            End If
        End With
    End Function

    Private Sub CancelRenderingOnly()
        StopRendering()
    End Sub

    Private Sub StopRendering()
        If _renderHandler IsNot Nothing Then
            RemoveHandler CompositionTarget.Rendering, _renderHandler
            _renderHandler = Nothing
        End If

        _renderStopwatch?.Stop()
        _renderStopwatch = Nothing

        _renderTcs?.TrySetResult(True)
    End Sub

    Private Shared Function ApplyEasing(t As Double, easing As LoaderEasing) As Double
        t = Math.Max(0, Math.Min(1, t))

        Select Case easing
            Case LoaderEasing.EaseIn
                Return t * t
            Case LoaderEasing.EaseOut
                Return t * (2 - t)
            Case LoaderEasing.EaseInOut
                If t >= 0.5 Then
                    Return 1.0 - Math.Pow(-2 * t + 2, 3) / 2.0
                Else
                    Return 4 * t * t * t
                End If
            Case LoaderEasing.SmoothStep
                Return t * t * (3 - 2 * t)
            Case Else
                Return t
        End Select
    End Function

    Private Sub CancelCurrent()
        If _cts IsNot Nothing Then
            _cts.Cancel()
            _cts.Dispose()
            _cts = Nothing
        End If
    End Sub

    Private Sub CancelCurrentAnimation()
        If _animationCts IsNot Nothing Then
            _animationCts.Cancel()
            _animationCts.Dispose()
            _animationCts = Nothing
        End If

    End Sub

    Private Shared Function Lerp(a As Double, b As Double, t As Double) As Double
        Return a + (b - a) * Math.Max(0, Math.Min(1, t))
    End Function

End Class


Public Class osHandler_Loader2


    Private ReadOnly _fadeTextOut As Action(Of LoadTextVisualType)
    Private ReadOnly _fadeTextIn As Action(Of LoadTextVisualType)

    Private ReadOnly _ValidateLoadText As Func(Of LoadTaskType, Boolean)

    Private _LoaderText As TextBlock

    Private _ProgLoadBar As Func(Of osProgLoad)

    Private ReadOnly _setValue As Action(Of Double)
    Private ReadOnly _setLoadText As Action(Of LoadTaskType)

    Private _animationCts As CancellationTokenSource

    Private _renderHandler As EventHandler
    Private _renderStopwatch As Stopwatch
    Private _renderDuration As TimeSpan
    Private _renderFrom As Double
    Private _renderTo As Double
    Private _renderEasing As LoaderEasing
    Private _renderToken As CancellationToken
    Private _renderTcs As TaskCompletionSource(Of Boolean)

    Private Const DriftSpeed As Double = 10.5

    Private _stages As IReadOnlyList(Of osLoadStageData2)

    Private _stageCnt As Integer
    Private _stageLast As Integer

    Private _cts As CancellationTokenSource
    Private _currentStageIndex As Integer = -1


    Private ProgMax As Double
    Private _ProgLoad As osProgLoad


    Public objLoadAni_TextVis As Storyboard = Nothing

    Private objAni_VisIn As ColorAnimation
    Private objAni_VisOut As ColorAnimation
    Private objAni_q As ColorAnimation

    Public objTextBrush As SolidColorBrush

    Public ReadOnly Property objVisTxtColor As DependencyProperty
        Get
            Return SolidColorBrush.ColorProperty
        End Get
    End Property

    Public ReadOnly Property objLoadVis_TextFadeOut As ColorAnimation
        Get
            Return objAni_VisOut
        End Get
    End Property

    Public ReadOnly Property objLoadVis_TextFadeIn As ColorAnimation
        Get
            Return objAni_VisIn
        End Get
    End Property


    Public ReadOnly Property objLoadVis_q As ColorAnimation
        Get
            Return objAni_VisIn
        End Get
    End Property

    Private _LoaderProgress As Double

    Private Property LoaderProgress As Double
        Get
            Return _LoaderProgress
        End Get
        Set(value As Double)
            _LoaderProgress = value
            _setValue(value)
        End Set
    End Property
    Private _targetProgress As Double
    Private _visualProgress As Double
    Private _rendering As Boolean

    Private _progressAnim As DoubleAnimation
    Private _animClock As AnimationClock
    Private _isAnimating As Boolean


    Public Sub New(objLoadStages As List(Of osLoadStageData2), objProgBar As osProgLoad, setValueAction As Action(Of Double), objLoaderText As UIElement, funcLoadText As Action(Of LoadTaskType), funcValLoadText As Func(Of LoadTaskType, Boolean),
                   objProg_Load As Func(Of osProgLoad), objTextVis_Out As Action(Of LoadTextVisualType), objTextVis_In As Action(Of LoadTextVisualType))

        _ProgLoadBar = objProg_Load

        Dim objPLB = _ProgLoadBar()
        _ProgLoad = objPLB

        ProgMax = _ProgLoad.Maximum

        _fadeTextOut = objTextVis_Out
        _fadeTextIn = objTextVis_In

        _setValue = setValueAction

        _LoaderText = objLoaderText
        _setLoadText = funcLoadText

        With New osLoadTextColors(_LoaderText, objTextBrush)
            objAni_VisIn = New ColorAnimation(.txtHidden, .txtShown,
                                    TimeSpan.FromMilliseconds(75), FillBehavior.Stop)
            objAni_VisOut = New ColorAnimation(.txtShown, .txtHidden,
                                    TimeSpan.FromMilliseconds(75), FillBehavior.HoldEnd)
        End With

        _ValidateLoadText = funcValLoadText

        _stages = objLoadStages
        '  _stagess = objLoadStages
        '
        _stageCnt = _stages.Count
        _stageLast = _stages.Count - 1

    End Sub

    Private Shared Function CreateEasing(e As LoaderEasing) As IEasingFunction
        Select Case e
            Case LoaderEasing.EaseIn
                Return New QuadraticEase With {.EasingMode = EasingMode.EaseIn}

            Case LoaderEasing.EaseOut
                Return New QuadraticEase With {.EasingMode = EasingMode.EaseOut}

            Case LoaderEasing.EaseInOut
                Return New ExponentialEase With {.EasingMode = EasingMode.EaseInOut, .Exponent = 2}

            Case LoaderEasing.SmoothStep
                Return New SineEase With {.EasingMode = EasingMode.EaseInOut}

            Case Else
                Return Nothing ' Linear
        End Select
    End Function


    Private Function VerifyLastStage(idxStage As Integer) As Boolean
        Return (idxStage = _stageLast)
    End Function

    Private Function VerifyTextVis(txtMsg As String) As Boolean
        Return Not txtMsg = "skip"
    End Function

    Private Sub FadeTextOut(valTaskType As LoadTaskType, objVisReset As osAutoAnimation)
        PrepDispatcher().InvokeAsync(
                Sub()
                    Dim handler As EventHandler = Nothing

                    handler =
                        Sub(sender, e)
                            RemoveHandler objAni_VisOut.Completed, handler

                            objVisReset.SetAnimation()
                            objTextBrush.BeginAnimation(objVisTxtColor, Nothing)
                        End Sub

                    AddHandler objAni_VisOut.Completed, handler

                    objTextBrush.BeginAnimation(objVisTxtColor, objAni_VisOut)
                End Sub)
    End Sub

    Private Sub FadeTextIn(valTaskType As LoadTaskType, objVisReset As osAutoAnimation)
        PrepDispatcher().InvokeAsync(
                Sub()
                    Dim handler As EventHandler = Nothing

                    handler =
                        Sub(sender, e)
                            RemoveHandler objAni_VisIn.Completed, handler

                            objVisReset.SetAnimation()
                            objTextBrush.BeginAnimation(objVisTxtColor, Nothing)
                        End Sub

                    AddHandler objAni_VisIn.Completed, handler

                    _setLoadText(valTaskType)
                    objTextBrush.BeginAnimation(objVisTxtColor, objAni_VisIn)
                End Sub)
    End Sub

    Private Sub FadeTextIn(valTaskType As LoadTaskType)

        Dim handler As EventHandler = Nothing

        handler =
                        Sub(sender, e)
                            RemoveHandler objAni_VisIn.Completed, handler

                            objTextBrush.BeginAnimation(objVisTxtColor, Nothing)
                        End Sub

        AddHandler objAni_VisIn.Completed, handler

        _setLoadText(valTaskType)
        objTextBrush.BeginAnimation(objVisTxtColor, objAni_VisIn)
    End Sub

    Private Sub FadeTextOut(valTaskType As LoadTaskType)

        Dim handler As EventHandler = Nothing

                    handler =
                        Sub(sender, e)
                            RemoveHandler objAni_VisOut.Completed, handler

                            objTextBrush.BeginAnimation(objVisTxtColor, Nothing)
                        End Sub

                    AddHandler objAni_VisOut.Completed, handler

        objTextBrush.BeginAnimation(objVisTxtColor, objAni_VisOut)
    End Sub

    Private Function FadeTextOutAsync(valTaskType As LoadTaskType) As Task
        Dim tcs As New TaskCompletionSource(Of Boolean)

        PrepDispatcher().InvokeAsync(
        Sub()
            Dim handler As EventHandler = Nothing
            handler =
                Sub(sender, e)
                    RemoveHandler objAni_VisOut.Completed, handler
                    objTextBrush.BeginAnimation(objVisTxtColor, Nothing)
                    tcs.TrySetResult(True)
                End Sub

            AddHandler objAni_VisOut.Completed, handler
            objTextBrush.BeginAnimation(objVisTxtColor, objAni_VisOut)
        End Sub, DispatcherPriority.Background)

        Return tcs.Task
    End Function


    Private Function FadeTextInAsync(valTaskType As LoadTaskType) As Task
        Dim tcs As New TaskCompletionSource(Of Boolean)

        PrepDispatcher().InvokeAsync(
        Sub()
            Dim handler As EventHandler = Nothing
            handler =
                Sub(sender, e)
                    RemoveHandler objAni_VisIn.Completed, handler
                    objTextBrush.BeginAnimation(objVisTxtColor, Nothing)
                    tcs.TrySetResult(True)
                End Sub

            AddHandler objAni_VisIn.Completed, handler

            _setLoadText(valTaskType)
            objTextBrush.BeginAnimation(objVisTxtColor, objAni_VisIn)
        End Sub, DispatcherPriority.Render)

        Return tcs.Task
    End Function


    Private Sub CancelRenderingOnly()
        StopRendering()
    End Sub

    Private Shared Function ApplyEasing(t As Double, easing As LoaderEasing) As Double
        t = Math.Max(0, Math.Min(1, t))

        Select Case easing
            Case LoaderEasing.EaseIn
                Return t * t
            Case LoaderEasing.EaseOut
                Return t * (2 - t)
            Case LoaderEasing.EaseInOut
                If t >= 0.5 Then
                    Return 1.0 - Math.Pow(-2 * t + 2, 3) / 2.0
                Else
                    Return 4 * t * t * t
                End If
            Case LoaderEasing.SmoothStep
                Return t * t * (3 - 2 * t)
            Case Else
                Return t
        End Select
    End Function

    Private Sub CancelCurrent()
        If _cts IsNot Nothing Then
            _cts.Cancel()
            _cts.Dispose()
            _cts = Nothing
        End If
    End Sub

    Private Sub CancelCurrentAnimation()
        If _animationCts IsNot Nothing Then
            _animationCts.Cancel()
            _animationCts.Dispose()
            _animationCts = Nothing
        End If

    End Sub

    Private Function CreateProgressReporter() As IProgress(Of Double)
        Return New Progress(Of Double)(
        Sub(value)
            PrepDispatcher().InvokeAsync(
                Sub()
                    AnimateTo(value)
                End Sub,
                DispatcherPriority.Render)
        End Sub)
    End Function

    Private Sub StopRendering()
        If Not _rendering Then Return

        RemoveHandler CompositionTarget.Rendering, _renderHandler
        _renderHandler = Nothing

        _renderStopwatch?.Stop()
        _renderStopwatch = Nothing

        _rendering = False
    End Sub

    Private Sub AnimateTo(value As Double)
        EnsureProgressAnimation()

        value = Math.Max(0, Math.Min(ProgMax, value))

        ' Capture current animated value
        Dim current = _ProgLoad.Progress

        _progressAnim.From = current
        _progressAnim.To = value

        ' Restart the clock WITHOUT replacing composition
        _animClock.Controller.Begin()
    End Sub


    Private Sub EnsureProgressAnimation()
        If _isAnimating Then Return

        _progressAnim = New DoubleAnimation With {
        .Duration = TimeSpan.FromMilliseconds(160),
        .EasingFunction = New QuadraticEase With {.EasingMode = EasingMode.EaseOut},
        .FillBehavior = FillBehavior.HoldEnd
    }

        _animClock = _progressAnim.CreateClock()

        _ProgLoad.ApplyAnimationClock(
        osLoadingProgressBar.ProgressProperty,
        _animClock,
        HandoffBehavior.Compose)

        _isAnimating = True
    End Sub

    Private Function CreateStageProgress(animator As LoaderProgressAnimator, startValue As Double, endValue As Double, token As CancellationToken) As IProgress(Of Double)

        Return New Progress(Of Double)(
            Async Sub(p)

                Dim mapped = startValue + (endValue - startValue) * (p / 100.0)

                Await animator.AnimateToAsync(mapped, TimeSpan.FromMilliseconds(120), New QuadraticEase With {.EasingMode = EasingMode.EaseOut}, token)

            End Sub)
    End Function

    Private Function CreateStageProgress(animator As LoaderProgressAnimator, objLoadStage As osLoadStageData2, token As CancellationToken) As IProgress(Of Double)

        Return New Progress(Of Double)(
             Async Sub(p)
                 With objLoadStage
                     Dim mapped = If(.LastTask, ProgMax, .StartValue + ((.EndValue) - .StartValue) * (p / 100))

                     Await animator.AnimateToAsync(mapped, .Duration, New ExponentialEase With {.EasingMode = EasingMode.EaseOut, .Exponent = 0.75}, token)
                 End With

             End Sub)
    End Function

    Private _lastProgress As Double = 0

    Private Async Function AnimateGapAsync(
    animator As LoaderProgressAnimator,
    fromValue As Double,
    toValue As Double,
    token As CancellationToken) As Task

        If toValue <= fromValue Then Return

        Dim delta = toValue - fromValue

        ' Speed-based duration (prevents slow crawl)
        Dim duration = TimeSpan.FromMilliseconds(
        Math.Max(325, delta * 4))

        Await animator.AnimateToAsync(
        toValue,
        duration,
        Nothing,
        token)
    End Function

    Private Delegate Sub LoadTextFunc_FadeIn(valTaskType As LoadTaskType)
    Private Delegate Sub LoadTextFunc_FadeOut(valTaskType As LoadTaskType)

    Public Async Function ProcessLoadTasks(tokenr As CancellationToken) As Task

        Dim animator As New LoaderProgressAnimator(_ProgLoad)


        For Each objLoadStage In _stages
            CancelCurrent()
            Dim _loaderCts = New CancellationTokenSource()

            With objLoadStage
                Dim aa = FadeTextOutAsync(.LoadType)

                Dim objTask_Load = RunLoadTask(objLoadStage, animator, _loaderCts)
                Dim fadeInSignal As New osAutoAnimation()
                FadeTextIn(.LoadType, fadeInSignal)
                Await fadeInSignal.WaitAsync(_loaderCts.Token)
                Await objTask_Load
            End With
        Next

    End Function

    Public Async Function RunLoadTask(objLoadStageData As osLoadStageData2, objAni As LoaderProgressAnimator, objTokenCT As CancellationTokenSource) As Task

        _cts = objTokenCT
        Dim token = _cts.Token

        token.ThrowIfCancellationRequested()

        Dim taskType = objLoadStageData.LoadType

        Dim progress = CreateStageProgress(
            objAni, objLoadStageData,
            token)


        Dim loadTask = objLoadStageData.LoadTask(progress, token)
        Await loadTask

        Await objAni.AnimateToAsync(
            objLoadStageData.EndValue,
            objLoadStageData.Duration,
            New QuadraticEase With {.EasingMode = EasingMode.EaseOut},
            token)

        _lastProgress = objLoadStageData.EndValue
    End Function

    Public Async Function RunLoaderAsync(tokenr As CancellationToken) As Task

        Dim animator As New LoaderProgressAnimator(_ProgLoad)
        Dim cntStage As Integer = 0

        For Each stage In _stages
            CancelCurrent()

            _cts = New CancellationTokenSource()
            Dim token = _cts.Token

            token.ThrowIfCancellationRequested()

            'Await AnimateGapAsync(
            'animator,
            '_lastProgress,
            'stage.StartValue,
            'token)
            Dim taskType = stage.LoadType
            '  Dim b = FadeTextOutAsync(taskType)
            '    Await FadeTextInAsync(taskType)
            '   Dim fadeOutSignal As New osAutoAnimation()
            '  FadeTextOut(taskType, fadeOutSignal)
            Dim bb = PrepDispatcher().Invoke(Function()
                                                 FadeTextOut(taskType)
                                             End Function, DispatcherPriority.Render)


            'Dim objTask_VisIn = _LoaderText.Dispatcher.BeginInvoke(
            '    DispatcherPriority.Render, New LoadTextFunc_FadeIn(AddressOf FadeTextIn), taskType)
            ' Text transition
            '    Await FadeTextOutAsync(stage.LoadType)
            '   Dim aa = FadeTextInAsync(stage.LoadType)



            ' Progress handler


            Dim kkk = PrepDispatcher().InvokeAsync(Sub()
                                                       FadeTextIn(taskType)
                                                   End Sub, DispatcherPriority.Render)

            Dim progress = CreateStageProgress(
            animator, stage,
            token)
            Await Task.Delay(125)  '      Dim fadeInSignal As New osAutoAnimation()
            Await kkk
            ' Run stage work
            '   Dim aa = FadeTextInAsync(taskType)
            Dim loadTask = stage.LoadTask(progress, token)
            'Await PrepDispatcher().InvokeAsync(Sub()
            '                                       FadeTextIn(taskType)
            '                                   End Sub, DispatcherPriority.Render)
            '     FadeTextIn(taskType, fadeInSignal)
            '     Await fadeInSignal.WaitAsync(token)
            ' Let fade-in complete, but do not block progress
            '  Await fadeInSignal.WaitAsync(token)
            Await loadTask
            'Dim objTask_FadeTextOut = _LoaderText.Dispatcher.BeginInvoke(
            '    DispatcherPriority.Render, Function() FadeTextOutAsync(taskType)) ' Ensure stage end reached

            'Dim objTask_VisOut = _LoaderText.Dispatcher.
            '    BeginInvoke(DispatcherPriority.Render,
            '                New LoadTextFunc_FadeOut(AddressOf FadeTextOut), taskType)

            Await animator.AnimateToAsync(
            stage.EndValue,
            stage.Duration,
            New QuadraticEase,
            token)
            '  Dim bbb = FadeTextOutAsync(taskType)

            _lastProgress = stage.EndValue
            cntStage += 1
        Next
    End Function



    Private _progressState As Double

    Private Sub CommitProgress(value As Double)
        _progressState = value
        _ProgLoad.Progress = value
    End Sub

    Private Async Function AnimateProgress(
        toValue As Double,
        duration As TimeSpan,
        easing As LoaderEasing,
        token As CancellationToken) As Task

        CancelCurrentAnimation()

        Dim fromValue = _progressState
        _progressState = toValue ' 🔑 claim intent immediately

        _animationCts = CancellationTokenSource.CreateLinkedTokenSource(token)
        Dim linkedToken = _animationCts.Token

        Dim tcs As New TaskCompletionSource(Of Boolean)

        Await PrepDispatcher().InvokeAsync(
            Sub()
                Dim anim As New DoubleAnimation With {
                    .From = fromValue,
                    .To = toValue,
                    .Duration = New Duration(duration),
                    .FillBehavior = FillBehavior.HoldEnd,
                    .EasingFunction = CreateEasing(easing)
                }

                AddHandler anim.Completed,
                    Sub()
                        _ProgLoad.Progress = toValue ' 🔑 final snap
                        tcs.TrySetResult(True)
                    End Sub

                _ProgLoad.BeginAnimation(
                    osLoadingProgressBar.ProgressProperty, anim)
            End Sub,
            DispatcherPriority.Render)

        Try
            Using linkedToken.Register(Sub() tcs.TrySetCanceled())
                Await tcs.Task
            End Using
        Catch ex As OperationCanceledException
            ' swallow — animation was cancelled intentionally
        End Try
    End Function


End Class


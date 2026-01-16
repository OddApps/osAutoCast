Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.LoadTextVisualType
Imports osAutoCast.osLoadingElements
Imports osAutoCast.osLoadingObjects
Imports osLoad = osAutoCast.osLoadingObjects
Imports osProgLoad = osAutoCast.osLoadingElements.osLoadingProgressBar

Public Class osHandler_Loader2

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
                Application.Current.Dispatcher.InvokeAsync(
                    Sub()
                        _ProgLoad.BeginAnimation(
                        osLoadingProgressBar.ProgressProperty, Nothing)
                    End Sub, DispatcherPriority.Render)

                completed.SetAnimation()
            End Sub)

            Dim chkFrom = If(toValue >=
                ProgressValue, toValue, ProgressValue)

            Await Application.Current.Dispatcher.InvokeAsync(
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
        Application.Current.Dispatcher.InvokeAsync(
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
        Application.Current.Dispatcher.InvokeAsync(
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


Public Class osHandler_Loader

    Private ReadOnly _fadeTextOut As Func(Of LoadTextVisualType, Task)
    Private ReadOnly _fadeTextIn As Func(Of LoadTextVisualType, Task)

    Private ReadOnly _ValidateLoadText As Func(Of LoadTaskType, Boolean)

    Private _LoaderText As TextBlock
    Private _LoaderTextHost As Grid

    Private _ProgLoadBar As Func(Of osProgLoad)

    Private ReadOnly _setLoadText As Action(Of LoadTaskType)

    Private _animationCts As CancellationTokenSource

    Private ReadOnly _LoadStages As IReadOnlyList(Of LoadStage)

    Private _stageCnt As Integer
    Private _stageLast As Integer

    Private _cts As CancellationTokenSource
    Private _currentStageIndex As Integer = -1

    Private ProgMax As Double
    Private _ProgLoad As osProgLoad

    Private objProgressStage As IProgress(Of Double)
    Private objProgressTimer As DispatcherTimer = Nothing

    Public Sub New(objLoadStages As osLoad, objProgBar As osProgLoad, objProg_Load As Func(Of osProgLoad),
                   objLoaderText As TextBlock, objLoaderTextHost As Grid, funcLoadText As Action(Of LoadTaskType),
                   objTextVis_Out As Func(Of LoadTextVisualType, Task), objTextVis_In As Func(Of LoadTextVisualType, Task))

        _ProgLoadBar = objProg_Load

        Dim objPLB = _ProgLoadBar()
        _ProgLoad = objPLB

        ProgMax = _ProgLoad.Maximum

        _fadeTextOut = objTextVis_Out
        _fadeTextIn = objTextVis_In

        _LoaderText = objLoaderText
        _LoaderTextHost = objLoaderTextHost
        _setLoadText = funcLoadText

        _LoadStages = objLoadStages.LoadStageIdx

        _stageCnt = _LoadStages.Count
        _stageLast = _LoadStages.Count - 1
    End Sub

    Private Sub CancelCurrent()
        If _cts IsNot Nothing Then
            _cts.Cancel()
            _cts.Dispose()
            _cts = Nothing
        End If
    End Sub

    Public Async Function YieldToRender() As Task
        Dim op = Application.Current.Dispatcher.InvokeAsync(
            Sub()
            End Sub, DispatcherPriority.Render)
        Await op.Task
    End Function

    Private Function CreateStageProgress(objAnimator As LoaderProgressAnimator,
                                         objLoadStage As TaskData, token As CancellationToken) As IProgress(Of Double)

        If Not Application.Current.Dispatcher.CheckAccess() Then
            Throw New InvalidOperationException("CreateStageProgress must be created on the UI thread.")
        End If

        Dim lastPercent As Double = Double.NaN
        Dim throttleEpsilon As Double = 0.25

        Return New Progress(Of Double)(
            Sub(percent)
                Try
                    If Not Double.IsNaN(lastPercent) AndAlso
                        Math.Abs(percent - lastPercent) < throttleEpsilon Then
                        Return
                    End If

                    lastPercent = percent

                    Dim mapped As Double = If(objLoadStage.LastTask, ProgMax,
                        objLoadStage.StartValue + ((objLoadStage.EndValue - objLoadStage.StartValue) * (percent / 100.0)))

                    Dim useToken As CancellationToken = If(percent >= 100.0, CancellationToken.None, token)
                    Dim animDuration As TimeSpan = If(percent >= 100.0, TimeSpan.FromMilliseconds(275), objLoadStage.Duration)

                    Dim objAniProgress = objAnimator.AnimateToAsync(mapped, animDuration,
                                                                    New ExponentialEase With {
                                                                        .EasingMode = EasingMode.EaseOut, .Exponent = 0.75
                                                                    }, useToken)

                Catch ex As OperationCanceledException
                Catch ex As Exception : End Try
            End Sub)
    End Function

    Private Sub CalcStageProgress(ByRef valPercent As Double, Optional valStep As Double = 10)
        valPercent = Math.Min(90.0, valPercent + valStep)
    End Sub

    Public Function StartUiDispatcherTimer(animator As LoaderProgressAnimator, objLoadStage As TaskData,
                                           token As CancellationToken, ByRef outTimer As DispatcherTimer) As Progress(Of Double)

        If Not Application.Current.Dispatcher.CheckAccess() Then
            Throw New InvalidOperationException("StartUiDispatcherTimer must be called on the UI thread.")
        End If

        Dim percent As Double = 0.0
        Dim uiProgress As IProgress(Of Double) = CreateStageProgress(animator, objLoadStage, token)

        outTimer = New DispatcherTimer(DispatcherPriority.Background) With {
            .Interval = TimeSpan.FromMilliseconds(85)
        }

        AddHandler outTimer.Tick,
            Sub(sender As Object, e As EventArgs)
                Try
                    If token.IsCancellationRequested OrElse percent >= 90.0 Then
                        CType(sender, DispatcherTimer).Stop()
                        Return
                    End If

                    CalcStageProgress(percent)
                    uiProgress.Report(percent)

                    Application.Current.Dispatcher.InvokeAsync(
                        Sub() HoldVisual(), DispatcherPriority.Render)

                Catch ex As OperationCanceledException
                    CType(sender, DispatcherTimer).Stop()
                Catch ex As Exception
                    CType(sender, DispatcherTimer).Stop()
                End Try
            End Sub

        outTimer.Start()

        Return uiProgress
    End Function

    Public Async Function BeginLoadProcess() As Task
        Dim objLoadProgressVis As New LoaderProgressAnimator(_ProgLoad)
        Dim cntStage As Integer = 0

        For Each LoadStage In _LoadStages
            With LoadStage
                Dim taskType = .TaskType

                If _cts IsNot Nothing Then
                    Await Task.Delay(115)
                    objProgressStage.Report(100)
                    Await Task.Delay(115)

                    objProgressTimer?.Stop()
                    CancelCurrent()
                End If

                _cts = New CancellationTokenSource()
                Dim objLoadStageToken = _cts.Token

                objLoadStageToken.ThrowIfCancellationRequested()

                Await _fadeTextIn(taskType)

                objProgressTimer = Nothing
                objProgressStage = StartUiDispatcherTimer(objLoadProgressVis, .TaskDetails, objLoadStageToken, objProgressTimer)

                Dim objLoadTask = .LoadTask(objLoadStageToken)
                Await objLoadTask

                Dim objTask_ProgressAnimate = objLoadProgressVis.
                    AnimateToAsync(.TaskDetails.EndValue, TimeSpan.FromMilliseconds(300),
                                   New QuadraticEase With {
                                        .EasingMode = EasingMode.EaseOut
                                   }, CancellationToken.None)
            End With

        Next
    End Function

End Class


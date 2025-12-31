Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.LoadTextVisualType

Public Class osHandler_Loader

    Private ReadOnly _triggerLoadTextVis As Func(Of LoadTextVisualType, String, Task)

    Private ReadOnly _fadeTextOut As Func(Of LoadTextVisualType, Task)
    Private ReadOnly _fadeTextIn As Func(Of LoadTextVisualType, Task)

    Private ReadOnly _setValue As Action(Of Double)
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
    Private Const SmoothingFactor As Double = 0.275

    Private ProgMax As Double

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

    Private Sub UpdateSmoothedValue(targetValue As Double, Optional setForce As Boolean = False)
        _currentValue = targetValue

        If targetValue > (ProgMax * 0.975) Then
            _setValue(targetValue)
        Else
            _smoothedValue += (targetValue - _smoothedValue) * SmoothingFactor
            _setValue(_smoothedValue)
        End If
    End Sub

    Private Function AnimateAsync(fromValue As Double, toValue As Double, duration As TimeSpan,
                                  easing As LoaderEasing, token As CancellationToken) As Task
        _renderFrom = fromValue
        _renderTo = toValue
        _renderDuration = duration
        _renderEasing = easing
        _renderToken = token

        _renderStopwatch = Stopwatch.StartNew()
        _renderTcs = New TaskCompletionSource(Of Boolean)

        _renderHandler =
            Sub(sender As Object, e As EventArgs)
                PrepDispatcher().Invoke(
                    Sub()
                        OnRenderFrame()
                    End Sub, DispatcherPriority.Render)
            End Sub

        AddHandler CompositionTarget.Rendering, _renderHandler

        token.Register(Sub() StopRendering())

        Return _renderTcs.Task
    End Function

    Private Sub OnRenderFrame()
        If _renderToken.IsCancellationRequested Then
            StopRendering()
            Return
        End If

        Dim elapsed = _renderStopwatch.Elapsed
        Dim rawT = elapsed.TotalMilliseconds / _renderDuration.TotalMilliseconds

        If rawT >= 1.0 Then
            UpdateSmoothedValue(_renderTo, True)
            StopRendering()
            Return
        End If

        Dim easedT = ApplyEasing(rawT, _renderEasing)
        Dim targetValue = Lerp(_renderFrom, _renderTo, easedT)

        UpdateSmoothedValue(targetValue)
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

    Private Function DriftTowardAsync(targetValue As Double, token As CancellationToken) As Task
        _renderFrom = _currentValue
        _renderTo = targetValue
        _renderDuration = TimeSpan.FromSeconds(Math.Abs(targetValue - _currentValue) / DriftSpeed)

        _renderEasing = LoaderEasing.Linear
        _renderToken = token

        _renderStopwatch = Stopwatch.StartNew()
        _renderTcs = New TaskCompletionSource(Of Boolean)

        _renderHandler =
            Sub(sender As Object, e As EventArgs)
                PrepDispatcher().Invoke(
                Sub()
                    OnRenderFrame()
                End Sub, DispatcherPriority.Render)
            End Sub

        AddHandler CompositionTarget.Rendering, _renderHandler
        token.Register(Sub() StopRendering())

        Return _renderTcs.Task
    End Function

    Private Function VerifyLastStage(idxStage As Integer) As Boolean
        Return (idxStage = _stageLast)
    End Function

    Private Function VerifyTextVis(txtMsg As String) As Boolean
        Return Not txtMsg = "skip"
    End Function

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
            Dim objTask_VisOut = _fadeTextOut(.LoadTaskData.LoadType)

            Dim effectiveStartValue = _currentValue
            Dim effectiveEndValue = Math.Max(.LoadStageData.EndValue, _currentValue)

            Dim objTask_Visual = AnimateAsync(effectiveStartValue, effectiveEndValue,
                                              .LoadStageData.Duration, .LoadStageData.Easing, token)

            Await _fadeTextIn(.LoadTaskData.LoadType)

            Dim objTaskReport As New TaskStatusReport
            Dim workTask As Task = Nothing

            If .LoadTaskData.LoadTask IsNot Nothing Then
                workTask = .LoadTaskData.
                    LoadTask.Invoke(objTaskReport)
            End If

            If workTask IsNot Nothing Then
                If VerifyLastStage(stageIndex) Then
                    Await objTask_Visual
                    Await workTask
                Else
                    Await Task.WhenAny(objTask_Visual, workTask)

                    If objTaskReport.IsCompleted Then
                        Await objTask_Visual
                    End If

                    CancelRenderingOnly()
                End If
            Else
                Await objTask_Visual
            End If

            Dim nextIndex = stageIndex + 1
            If nextIndex < _stageCnt AndAlso Not token.IsCancellationRequested Then
                Dim nextStartValue = _stages(nextIndex).LoadStageData.StartValue

                If _currentValue < nextStartValue Then
                    Await DriftTowardAsync(nextStartValue, token)
                End If

                Await BeginLoadStage(False, nextIndex)
            End If
        End With
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

    Private Shared Function Lerp(a As Double, b As Double, t As Double) As Double
        Return a + (b - a) * Math.Max(0, Math.Min(1, t))
    End Function

End Class
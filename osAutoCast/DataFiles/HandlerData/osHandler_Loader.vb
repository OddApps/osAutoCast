

Public Class osHandler_Loader

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

    Private Const DriftSpeed As Double = 5.0

    Public Sub New(setValueAction As Action(Of Double), initialValue As Double)
        _setValue = setValueAction
        _currentValue = initialValue
        _smoothedValue = initialValue

        _setValue(_currentValue)
    End Sub

    Private ReadOnly _stages As IReadOnlyList(Of osLoader_Stage)

    Private _cts As CancellationTokenSource
    Private _currentStageIndex As Integer = -1

    Private _smoothedValue As Double
    Private Const SmoothingFactor As Double = 0.35

    Public Sub New(setValueAction As Action(Of Double),
                   stages As IReadOnlyList(Of osLoader_Stage), initialValue As Double)

        _setValue = setValueAction
        _stages = stages
        _currentValue = initialValue
        _smoothedValue = initialValue

        _setValue(_currentValue)
    End Sub

    Private Sub UpdateSmoothedValue(targetValue As Double)
        _currentValue = targetValue
        _smoothedValue += (targetValue - _smoothedValue) * SmoothingFactor
        _setValue(_smoothedValue)
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
                OnRenderFrame()
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
            UpdateSmoothedValue(_renderTo)
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
                OnRenderFrame()
            End Sub

        AddHandler CompositionTarget.Rendering, _renderHandler
        token.Register(Sub() StopRendering())

        Return _renderTcs.Task
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
        Dim stage = _stages(stageIndex)

        Dim effectiveStartValue = _currentValue
        Dim effectiveEndValue = Math.Max(stage.EndValue, _currentValue)

        Dim animationTask = AnimateAsync(fromValue:=effectiveStartValue, toValue:=effectiveEndValue,
                                         duration:=stage.Duration, easing:=stage.Easing, token:=token)

        Dim workTask As Task = Nothing

        If stage.LoadTask2 IsNot Nothing Then
            Dim done As New TaskStatusReport()

            workTask = stage.LoadTask2(done)
        End If

        Dim isLastStage As Boolean = (stageIndex = _stages.Count - 1)

        If workTask IsNot Nothing Then
            If isLastStage Then
                Await animationTask
                Await workTask
            Else
                Await Task.WhenAny(animationTask, workTask)
                CancelRenderingOnly()
            End If
        Else
            Await animationTask
        End If

        Dim nextIndex = stageIndex + 1

        If nextIndex < _stages.Count AndAlso Not token.IsCancellationRequested Then
            Dim nextStartValue = _stages(nextIndex).StartValue

            If _currentValue < nextStartValue Then
                Await DriftTowardAsync(nextStartValue, token)
            End If

            Await BeginLoadStage(False, nextIndex)
        End If
    End Function

    Public Async Function StartLoadStage(initLoad As Boolean, Optional stageIndex As Integer = 0) As Task
        If initLoad Then stageIndex = 0

        If stageIndex < 0 OrElse stageIndex >= _stages.Count Then
            Throw New ArgumentOutOfRangeException(NameOf(stageIndex))
        End If

        CancelCurrent()

        _cts = New CancellationTokenSource()
        Dim token = _cts.Token

        _currentStageIndex = stageIndex
        Dim stage = _stages(stageIndex)

        Dim effectiveStartValue = _currentValue
        Dim effectiveEndValue = Math.Max(stage.EndValue, _currentValue)

        Dim animationTask = AnimateAsync(fromValue:=effectiveStartValue, toValue:=effectiveEndValue,
                                         duration:=stage.Duration, easing:=stage.Easing, token:=token)

        Dim workTask As Task = Nothing
        If stage.LoadTask IsNot Nothing Then
            workTask = stage.LoadTask.Invoke()
        End If

        Dim isLastStage As Boolean = (stageIndex = _stages.Count - 1)

        If workTask IsNot Nothing Then
            If isLastStage Then
                Await animationTask
                Await workTask
            Else
                Await Task.WhenAny(animationTask, workTask)
                CancelRenderingOnly()
            End If
        Else
            Await animationTask
        End If

        Dim nextIndex = stageIndex + 1

        If nextIndex < _stages.Count AndAlso Not token.IsCancellationRequested Then
            Dim nextStartValue = _stages(nextIndex).StartValue

            If _currentValue < nextStartValue Then
                Await DriftTowardAsync(nextStartValue, token)
            End If

            Await StartLoadStage(False, nextIndex)
        End If
    End Function

    Private Sub CancelRenderingOnly()
        StopRendering()
    End Sub

    Private Shared Function ApplyEasing(
        t As Double,
        easing As LoaderEasing) As Double

        t = Math.Max(0, Math.Min(1, t))

        Select Case easing
            Case LoaderEasing.EaseIn
                Return t * t

            Case LoaderEasing.EaseOut
                Return t * (2 - t)

            Case LoaderEasing.EaseInOut
                If t < 0.5 Then
                    Return 2 * t * t
                Else
                    Return 1 - Math.Pow(-2 * t + 2, 2) / 2
                End If

            Case LoaderEasing.SmoothStep
                Return t * t * (3 - 2 * t)

            Case Else ' Linear
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


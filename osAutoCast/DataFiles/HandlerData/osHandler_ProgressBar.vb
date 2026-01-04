Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.LoadTextVisualType
Imports osColors = System.Windows.Media

Public Class osHandler_ProgressBar

    Private ReadOnly _triggerLoadTextVis As Func(Of LoadTextVisualType, String, Task)

    Public ReadOnly _UpdateProgress As Action(Of Double)
    Public ReadOnly _DisplayTextFunc As Action(Of String)
    Public ReadOnly _UpdateColorFunc As Action(Of osColors.Color)

    Private _ProgressDuration As Double

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

    Public Sub New()
    End Sub

    Public Sub New(ProgDuration As Integer, pMax As Double, func_SetProgress As Action(Of Double),
                   func_UpdateColor As Action(Of osColors.Color), func_DispText As Action(Of String))

        ProgMax = pMax
        _ProgressDuration = ProgDuration

        _UpdateProgress = func_SetProgress
        _UpdateColorFunc = func_UpdateColor
        _DisplayTextFunc = func_DispText

        _currentValue = 0
        _smoothedValue = 0

        _UpdateProgress(_currentValue)
    End Sub

    Private Sub CalcProgressValue(targetValue As Double)
        _smoothedValue += (targetValue - _smoothedValue) * SmoothingFactor
    End Sub

    Private Sub UpdateSmoothedValue(targetValue As Double, Optional setForce As Boolean = False)
        _currentValue = targetValue

        If targetValue > (ProgMax * 0.985) Then
            _UpdateProgress(targetValue)
        Else
            CalcProgressValue(targetValue)
            _UpdateProgress(_smoothedValue)
        End If
    End Sub

    Private Function AnimateAsync(token As CancellationToken) As Task
        _renderFrom = 0
        _renderTo = ProgMax
        _renderDuration = TimeSpan.FromMilliseconds(_ProgressDuration)
        _renderEasing = LoaderEasing.Linear  ' Use linear easing
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

    Public Async Function InitiateProgress() As Task
        _cts = New CancellationTokenSource()
        Dim token = _cts.Token

        Await AnimateAsync(token)
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

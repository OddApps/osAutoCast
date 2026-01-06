Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.LoadTextVisualType
Imports osColors = System.Windows.Media
Imports osAutoCast.DataTypeLib.ProgressMode
Imports osAutoCast.DataTypeLib.ProgResult
Imports System.Windows.Media.Animation
Imports osAutoCast.osControls

Public Class osHandler_ProgressBar

    Public ReadOnly _UpdateProgress As Action(Of Double)
    Public ReadOnly _DisplayTextFunc As Action(Of String)
    Public ReadOnly _UpdateColorFunc As Action(Of osColors.Color)

    Private _ProgressDuration As Double

    Private _currentValue As Double

    Public objProgResult As ProgResult

    Private _renderCompleted As Boolean
    Private _renderHandler As EventHandler
    Private _renderStopwatch As Stopwatch
    Private _renderDuration As TimeSpan
    Private _renderFrom As Double
    Private _renderTo As Double
    Private _renderEasing As LoaderEasing
    Private _renderToken As CancellationToken
    Private _renderTcs As TaskCompletionSource(Of Boolean)

    Private _winAP As progUI_AutoPass

    Private _smoothedValue As Double
    Private Const SmoothingFactor As Double = 0.175

    Private ProgDisplayMax As Double
    Private ProgMax As Double

    Public Sub New()
    End Sub

    Public Sub New(isAutoPass As Boolean, ProgDuration As Integer, pMax As Double, func_SetProgress As Action(Of Double),
                   func_UpdateColor As Action(Of osColors.Color), func_DispText As Action(Of String))

        ProgMax = pMax
        ProgDisplayMax = If(isAutoPass, 0, ProgMax)

        _ProgressDuration = ProgDuration
        _renderDuration = TimeSpan.FromMilliseconds(_ProgressDuration)

        _UpdateProgress = func_SetProgress
        _UpdateColorFunc = func_UpdateColor
        _DisplayTextFunc = func_DispText

        _currentValue = 0
        _smoothedValue = 0

        _UpdateProgress(_currentValue)
    End Sub

    Public Sub New(objWinAP As progUI_AutoPass, isAutoPass As Boolean, ProgDuration As Integer, pMax As Double, func_SetProgress As Action(Of Double),
                   func_UpdateColor As Action(Of osColors.Color), func_DispText As Action(Of String))

        _winAP = objWinAP

        ProgMax = pMax
        ProgDisplayMax = If(isAutoPass, 0, ProgMax)

        _ProgressDuration = ProgDuration
        _renderDuration = TimeSpan.FromMilliseconds(_ProgressDuration)

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

        '   If targetValue > (ProgMax * 0.985) Then
        _UpdateProgress(targetValue)
        '   Else
        '    CalcProgressValue(targetValue)
        '   _UpdateProgress(_smoothedValue)

        '    End If
    End Sub

    Private Sub StopProgressStoryboard()
        If _progressStoryboard Is Nothing Then Return

        _progressStoryboard.Stop()
        _progressStoryboard = Nothing
    End Sub

    Private Sub OnProgressCompleted(sender As Object, e As EventArgs)
        RemoveHandler _progressStoryboard.Completed, AddressOf OnProgressCompleted
        _progressStoryboard = Nothing

        _UpdateProgress(ProgMax) ' snap final value
        HandleProgressResult(True)
    End Sub


    Private _progressStoryboard As Storyboard

    'Public Function StartProgressStoryboard(token As CancellationToken) As Task(Of Boolean)
    '    StopProgressStoryboard()

    '    _renderFrom = 0
    '    _renderTo = ProgMax
    '    '    _renderDuration = TimeSpan.FromMilliseconds(_ProgressDuration)
    '    _renderEasing = LoaderEasing.EaseInOut  ' Use linear easing
    '    _renderToken = token

    '    _renderCompleted = False

    '    Dim apProgRender As New DoubleAnimation() With {
    '        .From = 0, .To = _renderTo, .FillBehavior = FillBehavior.Stop,
    '        .Duration = _renderDuration,
    '        .EasingFunction = New EaseInOutExpoEase
    '    }

    '    Storyboard.SetTarget(apProgRender, _winAP.objProgBar)
    '    Storyboard.SetTargetProperty(apProgRender, New PropertyPath(osProgressBar.ProgressProperty))

    '    _progressStoryboard = New Storyboard()
    '    _progressStoryboard.Children.Add(apProgRender)

    '    _renderTcs = New TaskCompletionSource(Of Boolean)

    '    token.Register(Sub()
    '                       FinalizeProgress(True, False)
    '                   End Sub)

    '    AddHandler _progressStoryboard.Completed, Sub()
    '                                                  FinalizeProgress(True, True)
    '                                              End Sub

    '    _progressStoryboard.Begin()

    '    Return _renderTcs.Task
    'End Function

    Public Function StartProgressStoryboard(token As CancellationToken) As Task(Of Boolean)
        _renderToken = token

        token.Register(
            Sub()
                FinalizeProgress(True, False)
            End Sub)

        _renderTcs = New TaskCompletionSource(Of Boolean)

        _progressStoryboard.Begin()

        Return _renderTcs.Task
    End Function

    Public Sub PrepProgVis()
        StopProgressStoryboard()

        _renderFrom = 0
        _renderTo = ProgMax
        '    _renderDuration = TimeSpan.FromMilliseconds(_ProgressDuration)
        _renderEasing = LoaderEasing.EaseInOut  ' Use linear easing


        _renderCompleted = False

        Dim apProgRender As New DoubleAnimation() With {
            .From = 0, .To = _renderTo, .FillBehavior = FillBehavior.Stop,
            .Duration = _renderDuration,
            .EasingFunction = New EaseInOutExpoEase
        }

        Storyboard.SetTarget(apProgRender, _winAP.objProgBar)
        Storyboard.SetTargetProperty(apProgRender, New PropertyPath(osProgressBar.ProgressProperty))

        _progressStoryboard = New Storyboard()
        _progressStoryboard.Children.Add(apProgRender)

        AddHandler _progressStoryboard.Completed, Sub()
                                                      FinalizeProgress(True, True)
                                                  End Sub
    End Sub

    Public Async Function StartProgress(isAni As Boolean, Optional objAbortToken As CancellationToken = Nothing) As Task(Of ProgResult)
        objProgResult = Nothing

        Dim objProgressResult = Await StartProgressStoryboard(objAbortToken)
        Return HandleProgressResult(objProgressResult)
    End Function

    Public Async Function StartProgress(Optional objAbortToken As CancellationToken = Nothing) As Task(Of ProgResult)
        objProgResult = Nothing

        Dim objProgressResult = Await RenderProgress(objAbortToken)
        Return HandleProgressResult(objProgressResult)
    End Function

    Private Function HandleProgressResult(isProgComplete As Boolean) As ProgResult
        Return If(isProgComplete, ProgResult.Completed,
            ProgResult.Cancelled)
    End Function

    Private Function RenderProgress(token As CancellationToken) As Task(Of Boolean)
        _renderFrom = 0
        _renderTo = ProgMax
        '    _renderDuration = TimeSpan.FromMilliseconds(_ProgressDuration)
        _renderEasing = LoaderEasing.EaseInOut  ' Use linear easing
        _renderToken = token

        _renderCompleted = False
        _renderStopwatch = Stopwatch.StartNew()
        _renderTcs = New TaskCompletionSource(Of Boolean)

        _renderHandler =
            Sub(sender As Object, e As EventArgs)
                OnRenderFrame()
            End Sub

        AddHandler CompositionTarget.Rendering, _renderHandler

        token.Register(Sub()
                           FinalizeProgress(False)
                       End Sub)

        Return _renderTcs.Task
    End Function

    Private Sub OnRenderFrame()
        If _renderToken.IsCancellationRequested Then
            FinalizeProgress(False)
            Return
        End If

        Dim elapsed = _renderStopwatch.Elapsed
        Dim rawT = elapsed.TotalMilliseconds / _renderDuration.TotalMilliseconds
        rawT = Math.Min(1.0, rawT)

        If rawT >= 1.0 Then
            UpdateSmoothedValue(_renderTo, True)
            FinalizeProgress(True)

            Return
        End If

        Dim easedT = ApplyEasing(rawT, _renderEasing)
        Dim targetValue = Lerp(_renderFrom, _renderTo, easedT)

        UpdateSmoothedValue(targetValue)
    End Sub

    Public Sub SetMaxFill()
        _UpdateProgress(ProgDisplayMax)
    End Sub

    Private Sub FinalizeProgress(isAni As Boolean, isProgComplete As Boolean)
        _renderCompleted = True

        StopProgressStoryboard()
        'RemoveHandler _progressStoryboard.Completed

        SetMaxFill()
        _renderTcs.TrySetResult(isProgComplete)
    End Sub

    Private Sub FinalizeProgress(isProgComplete As Boolean)
        If _renderCompleted Then Return
        _renderCompleted = True

        RemoveHandler CompositionTarget.Rendering, _renderHandler
        _renderStopwatch.Stop()

        SetMaxFill()
        _renderTcs.TrySetResult(isProgComplete)

        'If _renderHandler IsNot Nothing Then
        '    RemoveHandler CompositionTarget.Rendering, _renderHandler
        '    _renderHandler = Nothing
        'End If

        '_renderStopwatch?.Stop()
        '_renderStopwatch = Nothing


        '_renderTcs?.TrySetResult(isProgComplete)
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

    Private Shared Function Lerp(a As Double, b As Double, t As Double) As Double
        Return a + (b - a) * Math.Max(0, Math.Min(1, t))
    End Function

End Class

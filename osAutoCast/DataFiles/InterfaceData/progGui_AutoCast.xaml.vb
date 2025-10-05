Imports System.ComponentModel
Imports System.Windows.Media
Imports System.Threading

Public Class progGui_AutoCast

    Private pHeight As Integer
    Private pWidth As Integer

    Private chkAutoCastResult As TaskCompletionSource(Of Boolean)
    Private chkAutoCastComplete As Boolean

    Private acProgressHandler As EventHandler = Nothing

    Private acTimer As Stopwatch

    '   Private valFuse As Integer = GetFuse()
    ' Private invFuse As Double = 1.0 / valFuse

    Private Async Function AutoCast_Prep() As Task
        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.Reset)
        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.DispMsg, "Release Shift")

        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AC_Start)

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.Reset)

        Await Task.Delay(500)

        InitiateAutoCast(chkAutoCastResult)
    End Function

    Private Sub InitiateAutoCast(ByRef objChkResult As TaskCompletionSource(Of Boolean))
        If objChkResult IsNot Nothing Then objChkResult = Nothing
        If acTimer IsNot Nothing Then acTimer = Nothing

        objChkResult = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                    RunContinuationsAsynchronously)
        ' acTimer = Stopwatch.StartNew
    End Sub
    ' Class-level field so we can remove the handler reliably
    ' Private acProgressHandler As EventHandler

    'Public Async Function LaunchAutoCast(isNew As Boolean) As Task(Of ProgResult)

    '    Await AutoCast_Prep()

    '    Dim tcs As New TaskCompletionSource(Of Boolean)(TaskCreationOptions.RunContinuationsAsynchronously)
    '    Dim durationMs As Double = osFuncLib_Progress.ProgDuration
    '    Dim started As Boolean = False
    '    Dim startTime As TimeSpan = TimeSpan.Zero
    '    Dim lastEdge As Integer = -1

    '    ' 1) Subscribe once; compute progress from the rendering clock
    '    acProgressHandler = Sub(sender As Object, e As EventArgs)
    '                            Dim re = TryCast(e, System.Windows.Media.RenderingEventArgs)
    '                            If re Is Nothing Then Exit Sub

    '                            If Not started Then
    '                                startTime = re.RenderingTime
    '                                started = True
    '                            End If

    '                            Dim elapsedMs As Double = (re.RenderingTime - startTime).TotalMilliseconds
    '                            If elapsedMs < 0 Then elapsedMs = 0

    '                            Dim f As Double = Math.Min(1.0, elapsedMs / durationMs)

    '                            ' Throttle: only update if the filled pixel width changes
    '                            Dim edge As Integer = CInt(Math.Round(Me.OddProgBar1.ActualWidth * f))
    '                            If edge <> lastEdge Then
    '                                lastEdge = edge
    '                                ' Directly set ProgressFraction; your control will ease & clamp
    '                                Me.OddProgBar1.ProgressFraction = f
    '                            End If

    '                            If f >= 1.0 Then
    '                                RemoveHandler CompositionTarget.Rendering, acProgressHandler
    '                                tcs.TrySetResult(True) ' completed successfully
    '                            End If
    '                        End Sub

    '    AddHandler CompositionTarget.Rendering, acProgressHandler

    '    ' 2) Register cancellation ONCE; no per-frame exceptions
    '    Using ctr = CoreDataLib.objCancelState.Register(
    '    Sub()
    '        RemoveHandler CompositionTarget.Rendering, acProgressHandler
    '        tcs.TrySetResult(False) ' canceled
    '    End Sub)

    '        ' 3) Await finish/cancel, then finalize
    '        Dim ok As Boolean = Await tcs.Task
    '        TerminateAutoCast(ok)

    '        Dim acResult = Await chkAutoCastResult.Task
    '        Return AutoCast_HandleResult(acResult)
    '    End Using
    'End Function

    Public Async Function LaunchAutoCast(isNew As Boolean) As Task(Of ProgResult)

        Await AutoCast_Prep()

        Dim osProcessProg As New System.Windows.Media.Animation.DoubleAnimation() With {
            .From = 0.0, .To = 1.0,
            .Duration = TimeSpan.FromMilliseconds(osFuncLib_Progress.ProgDuration),
            .FillBehavior = Animation.FillBehavior.Stop
        }

        AddHandler osProcessProg.Completed, Sub()
                                                TerminateAutoCast(True)
                                            End Sub

        ' Start animation (linear). Your ProgressFraction setter applies EaseInOutExpo.
        Me.OddProgBar1.BeginAnimation(OddLib_ProgressBar.ProgressValueProperty, osProcessProg)

        Using reg As CancellationTokenRegistration = CoreDataLib.
            objCancelState.Register(
                Sub()
                    ' Stop animation and finish as canceled
                    Me.OddProgBar1.BeginAnimation(OddLib_ProgressBar.ProgressValueProperty, Nothing)
                    TerminateAutoCast(False)
                End Sub)

            Dim acResult = Await chkAutoCastResult.Task
            Return AutoCast_HandleResult(acResult)
        End Using

    End Function


    Public Async Function LaunchAutoCast() As Task(Of ProgResult)

        Await AutoCast_Prep()

        acProgressHandler = Sub(sender As Object, e As EventArgs)
                                Try
                                    CoreDataLib.objCancelState.ThrowIfCancellationRequested()

                                    ' Dim progRatio As Double = CalcProgress(acTimer.ElapsedMilliseconds, invFuse)

                                    'OddProgBar1.ProgressFraction = CalcProgress(acTimer.ElapsedMilliseconds, invFuse)
                                    Me.OddProgBar1.UpdateProgress(acTimer.ElapsedMilliseconds)

                                    If acTimer.ElapsedMilliseconds >= osFuncLib_Progress.ProgDuration Then
                                        TerminateAutoCast(True)
                                    End If

                                Catch ex As OperationCanceledException
                                    TerminateAutoCast(False)
                                End Try
                            End Sub

        AddHandler CompositionTarget.Rendering, acProgressHandler

        Dim acResult = Await chkAutoCastResult.Task
        Return AutoCast_HandleResult(acResult)
    End Function

    Private Sub TerminateAutoCast(acComplete As Boolean)
        '  RemoveHandler CompositionTarget.Rendering, acProgressHandler

        ' acTimer.Stop()
        chkAutoCastComplete = acComplete
        chkAutoCastResult.TrySetResult(acComplete)
    End Sub

    Private Function CalcProgress(msDuration As Long, valStep As Double) As Double
        Return CDbl(Math.Min(1.0, msDuration * valStep))
    End Function

    Private Function CalcProgress(acTmr As Stopwatch, valFuse As Integer) As Double
        Return Math.Min(1.0, acTmr.Elapsed.TotalMilliseconds / valFuse)
    End Function

    Private Function AutoCast_HandleResult(acComplete As Boolean) As ProgResult

        Dim retProgResult As ProgResult = Nothing

        If acComplete Then
            If osFuncLib_Progress.GetProgState() = ProgStatus.Running Then osFuncLib_Progress.SetProgStatus(ProgAction.Complete,
                                                                      TriggerType.AutoCast)
            SetProgResult(acComplete, retProgResult)
        Else
            osFuncLib_Progress.SetProgStatus(ProgAction.Abort, TriggerType.AutoCast)
            SetProgResult(acComplete, retProgResult)
        End If

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.MaxFill)

        Return retProgResult

    End Function

    Public Sub BeginPrep() Handles Me.Loaded
        Me.OddProgBar1.Background = New SolidColorBrush(System.Windows.Media.Color.FromRgb(57, 57, 57))

        ' Me.OddProgBar1.BorderThickness = 2
        ' Me.OddProgBar1.BorderBrush = New SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0))
    End Sub

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub

    Private Sub progGui_AutoCast_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        e.Cancel = True
        Me.Hide()
    End Sub

End Class

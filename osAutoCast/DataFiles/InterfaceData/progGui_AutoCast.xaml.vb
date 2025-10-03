Imports System.ComponentModel
Imports System.Windows.Media

Public Class progGui_AutoCast

    Private pHeight As Integer
    Private pWidth As Integer

    Private chkAutoCastResult As TaskCompletionSource(Of Boolean)
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
        acTimer = Stopwatch.StartNew
    End Sub

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
        RemoveHandler CompositionTarget.Rendering, acProgressHandler

        acTimer.Stop()
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

        Me.OddProgBar1.BorderThickness = 2
        Me.OddProgBar1.BorderBrush = New SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0))
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

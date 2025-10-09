Imports System.ComponentModel
Imports System.Windows.Media
Imports System.Threading

Public Class progGui_AutoCast

    Private pHeight As Integer
    Private pWidth As Integer

    Private chkAutoCastResult As TaskCompletionSource(Of Boolean)
    Private retProgResult As ProgResult = Nothing

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

        objChkResult = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                    RunContinuationsAsynchronously)
    End Sub

    Public Async Function LaunchAutoCast() As Task(Of ProgResult)

        Await AutoCast_Prep()

        Dim acProgRender As New Animation.DoubleAnimation() With {
            .From = 0.0, .To = 1.0,
            .Duration = TimeSpan.FromMilliseconds(osFuncLib_Progress.ProgDuration),
            .FillBehavior = Animation.FillBehavior.Stop
        }

        AddHandler acProgRender.Completed, Sub() TerminateAutoCast(True)

        Me.OddProgBar1.BeginAnimation(OddLib_ProgressBar.ProgressValueProperty, acProgRender)

        Using CancelStateReg As CancellationTokenRegistration = CoreDataLib.
            objCancelState.Register(
                Sub()
                    Me.OddProgBar1.BeginAnimation(OddLib_ProgressBar.ProgressValueProperty, Nothing)
                    TerminateAutoCast(False)
                End Sub)

            Dim acResult = Await chkAutoCastResult.Task
            Return AutoCast_HandleResult(acResult)
        End Using

    End Function

    Private Sub TerminateAutoCast(acComplete As Boolean)
        chkAutoCastResult.TrySetResult(acComplete)
    End Sub

    Private Function AutoCast_HandleResult(acComplete As Boolean) As ProgResult
        If acComplete Then
            If osFuncLib_Progress.IsProgRunning() Then osFuncLib_Progress.SetProgStatus(ProgAction.Complete,
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

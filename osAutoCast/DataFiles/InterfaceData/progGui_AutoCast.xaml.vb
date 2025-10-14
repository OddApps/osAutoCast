Imports System.ComponentModel
Imports System.Windows.Media
Imports System.Threading

Public Class progGui_AutoCast

    Private pHeight As Integer
    Private pWidth As Integer

    Private chkAutoCastResult As TaskCompletionSource(Of Boolean)
    Private retProgResult As ProgResult = Nothing

    Private Async Function AutoCast_Prep() As Task

        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AC_Start)

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.ClrMsg)
        osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoCast, ProgAction.Activate)

        'Dim tier As Integer = (RenderCapability.Tier >> 16)
        'If tier < 2 Then
        '    ' Tier 0/1: be conservative
        '    minDt = TimeSpan.FromMilliseconds(33)  ' 30 FPS
        '    OddProgBar1.MinDelta = 0.005           ' ~0.5% pixel threshold
        'End If


        Await Task.Delay(375)

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
            .FillBehavior = Animation.FillBehavior.HoldEnd,
            .EasingFunction = New EaseInOutExpoEase
        }

        Animation.Timeline.SetDesiredFrameRate(acProgRender, 45)

        AddHandler acProgRender.Completed, Sub()
                                               TerminateAutoCast(True)
                                           End Sub

        OddProgBar1.BeginAnimation(OddLib_ProgressBar.ProgressValueProperty, acProgRender)

        Using CancelStateReg As CancellationTokenRegistration = CoreDataLib.
            objCancelState.Register(
                Sub()
                    OddProgBar1.BeginAnimation(OddLib_ProgressBar.ProgressValueProperty, Nothing)
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
            osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoCast,
                                                    ProgAction.Complete)
            SetProgResult(acComplete, retProgResult)
        Else
            osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoCast, ProgAction.Abort)
            SetProgResult(acComplete, retProgResult)
        End If

        Return retProgResult
    End Function

    Public Sub BeginPrep()
        ' OddProgBar1.Background = New SolidColorBrush(System.Windows.Media.Color.FromRgb(57, 57, 57))
        OddProgBar1.IsAutoPass = False
    End Sub

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub

    Private Sub progGui_AutoCast_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        'e.Cancel = True
        'Hide()
    End Sub

End Class

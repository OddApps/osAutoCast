Imports System.ComponentModel
Imports System.Windows.Media
Imports System.Threading

Public Class progGui_AutoPass

    Private chkAutoPassResult As TaskCompletionSource(Of Boolean)
    Private apProgressHandler As EventHandler = Nothing

    ' Private valSafetyTimer As Integer ' = GetSafetyTimer()
    ' Private invST As Double '= 1.0 / valSafetyTimer

    Private pHeight As Integer
    Private pWidth As Integer

    Private Async Function AutoPass_Prep() As Task

        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AP_Start)
        Await Task.Delay(100)

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "Release Shift or Press C To Cancel")
        InitiateAutoPass(chkAutoPassResult)
    End Function

    Private Sub InitiateAutoPass(ByRef objChkResult As TaskCompletionSource(Of Boolean))
        If objChkResult IsNot Nothing Then objChkResult = Nothing

        objChkResult = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                    RunContinuationsAsynchronously)
    End Sub

    Public Async Function LaunchAutoPass() As Task(Of ProgResult)

        Await AutoPass_Prep()

        Dim apProgRender As New Animation.DoubleAnimation() With {
            .From = 1.0, .To = 0.0,
            .Duration = TimeSpan.FromMilliseconds(osFuncLib_Progress.ProgDuration),
            .FillBehavior = Animation.FillBehavior.Stop,
            .EasingFunction = New EaseInOutExpoEase
        }

        Animation.Timeline.SetDesiredFrameRate(apProgRender, 45)

        AddHandler apProgRender.Completed, Sub()
                                               TerminateAutoPass(True)
                                           End Sub

        Me.OddProgBar_AP.BeginAnimation(OddLib_ProgressBar.ProgressValueProperty, apProgRender)

        Using CancelStateReg As CancellationTokenRegistration = CoreDataLib.objCancelState.Register(
            Sub()
                Me.OddProgBar_AP.BeginAnimation(OddLib_ProgressBar.ProgressValueProperty, Nothing)
                TerminateAutoPass(False)
            End Sub)

            Dim apResult = Await chkAutoPassResult.Task
            Return AutoPass_HandleResult(apResult)
        End Using
    End Function


    Private Function AutoPass_HandleResult(apComplete As Boolean) As ProgResult

        Dim retProgResult As ProgResult = Nothing

        If apComplete Then
            osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoPass,
                                                ProgAction.Complete)

            SetProgResult(apComplete, retProgResult)
        Else
            osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoPass, ProgAction.Abort)
            SetProgResult(apComplete, retProgResult)
        End If

        ' CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.MaxFill)
        ' CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "Release Shift To AutoPass | Press C To Cancel")

        Return retProgResult

    End Function

    Public Sub BeginPrep()
        With CoreDataLib.FetchProgSizeReport(TriggerType.AutoPass)
            pHeight = .Item("pH")
            pWidth = .Item("pW")
        End With

        Me.OddProgBar_AP.ProgressFlow = ProgFlow.Descending
        Me.OddProgBar_AP.IsAutoPass = True
    End Sub

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub

    Private Function CalcProgress(msDuration As Long, valStep As Double) As Double
        Return CDbl(Math.Min(1.0, msDuration * valStep))
    End Function

    Private Sub TerminateAutoPass(apComplete As Boolean)
        chkAutoPassResult.TrySetResult(apComplete)
    End Sub

    Private Sub progGui_AutoPass_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        'e.Cancel = True
        'Me.Hide()
    End Sub

End Class

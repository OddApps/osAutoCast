Imports System.ComponentModel
Imports System.Windows.Media

Public Class progGui_AutoPass

    Private chkAutoPassResult As TaskCompletionSource(Of Boolean)
    Private apProgressHandler As EventHandler = Nothing

    Private apTimer As Stopwatch

    ' Private valSafetyTimer As Integer ' = GetSafetyTimer()
    ' Private invST As Double '= 1.0 / valSafetyTimer

    Private pHeight As Integer
    Private pWidth As Integer

    Private Async Function AutoPass_Prep() As Task
        CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.Reset)
        CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "Release Mouse To Begin")

        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AP_Start)

        ' CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.Reset)

        '  valSafetyTimer = GetSafetyTimer()
        '  invST = 1.0 / valSafetyTimer

        Await Task.Delay(100)

        InitiateAutoPass(chkAutoPassResult)
    End Function

    Private Sub InitiateAutoPass(ByRef objChkResult As TaskCompletionSource(Of Boolean))
        If objChkResult IsNot Nothing Then objChkResult = Nothing
        If apTimer IsNot Nothing Then apTimer = Nothing

        objChkResult = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                    RunContinuationsAsynchronously)
        apTimer = Stopwatch.StartNew
    End Sub

    Public Async Function LaunchAutoPass() As Task(Of ProgResult)

        Await AutoPass_Prep()

        apProgressHandler = Sub(sender As Object, e As EventArgs)
                                Try
                                    CoreDataLib.objCancelState.ThrowIfCancellationRequested()

                                    Me.OddProgBar_AP.ProgressFraction = CalcProgress(apTimer.ElapsedMilliseconds, osFuncLib_Progress.ProgInv)
                                    ' Me.OddProgBar_AP.UpdateProgress(apTimer.ElapsedMilliseconds)

                                    If apTimer.ElapsedMilliseconds >= osFuncLib_Progress.ProgDuration Then
                                        TerminateAutoPass(True)
                                    End If

                                Catch ex As OperationCanceledException
                                    TerminateAutoPass(False)
                                End Try
                            End Sub

        AddHandler CompositionTarget.Rendering, apProgressHandler

        Dim acResult = Await chkAutoPassResult.Task
        Return AutoPass_HandleResult(acResult)
    End Function

    Private Function AutoPass_HandleResult(apComplete As Boolean) As ProgResult

        Dim retProgResult As ProgResult = Nothing

        If apComplete Then
            If osFuncLib_Progress.GetProgState() = ProgStatus.StartAP Then osFuncLib_Progress.SetProgStatus(ProgAction.Complete,
                                                                      TriggerType.AutoPass)
            SetProgResult(apComplete, retProgResult)
        Else
            osFuncLib_Progress.SetProgStatus(ProgAction.Abort, TriggerType.AutoPass)
            SetProgResult(apComplete, retProgResult)
        End If

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.MaxFill)

        Return retProgResult

    End Function

    Public Sub BeginPrep() Handles Me.Loaded
        With CoreDataLib.FetchProgSizeReport(TriggerType.AutoPass)
            pHeight = .Item("pH")
            pWidth = .Item("pW")
        End With

        Me.OddProgBar_AP.ProgressFlow = ProgFlow.Descending
    End Sub

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub

    Private Function CalcProgress(msDuration As Long, valStep As Double) As Double
        Return CDbl(Math.Min(1.0, msDuration * valStep))
    End Function

    Private Sub TerminateAutoPass(apComplete As Boolean)
        RemoveHandler CompositionTarget.Rendering, apProgressHandler

        apTimer.Stop()
        chkAutoPassResult.TrySetResult(apComplete)
    End Sub

    Private Sub progGui_AutoPass_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        e.Cancel = True
        Me.Hide()
    End Sub

End Class

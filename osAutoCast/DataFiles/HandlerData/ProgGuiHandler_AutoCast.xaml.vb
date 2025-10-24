Imports System.ComponentModel
Imports System.Windows.Media
Imports System.Threading
Imports osAutoCast.osFuncLib_Progress
Imports osAutoCast.CoreDataLib
Imports osDraw = System.Drawing
Imports osForms = System.Windows.Forms

Public Class ProgGuiHandler_AutoCast


    Private pHeight As Integer
    Private pWidth As Integer

    Private chkAutoCastResult As TaskCompletionSource(Of Boolean)
    Private retProgResult As ProgResult = Nothing

    Private AutoCastComplete As Boolean

    Public acProgressGui As ProgBarGui_AutoCast

    Private acProgLoc As osDraw.Point

    Private Async Function AutoCast_Prep() As Task
        Await Task.Delay(100)
        acProgressGui.DisplayMsg("Release Shift", TriggerType.AutoCast)

        Await InputMonSvc.AnticipateInput(InputAction.AC_Start)

        ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.ClrMsg)
        UpdateProgStatus(TriggerAction.AutoCast, ProgAction.Activate)

        Await Task.Delay(375)
    End Function

    Public Sub InitiateAutoCast()
        With acProgressGui
            SetProgressEvents()

            GetPosGui(acProgLoc)
            Dim locProg = SetPosData(acProgLoc)

            ' .BeginPrep()

            .Show()
            .Left = locProg.X
            .Top = locProg.Y
            '  .Invalidate()
            .DrawBG()
            ' .DisplayMsg("Release Shift", TriggerType.AutoCast)
            '  .SetProgress01(0)
            '.Left = locProg.X
            '.Top = locProg.Y
        End With
    End Sub

    Public Async Function LaunchAutoCast() As Task(Of ProgResult)
        Await AutoCast_Prep()

        Dim acProgTask = acProgressGui.
            BeginProgress(ProgTimeSpan, objCancelState, AddressOf EaseInOutCirc)
        Await acProgTask

        Return AutoCast_HandleResult(AutoCastComplete)
    End Function

    Private Sub SetAutoCastResult(acComplete As Boolean)
        AutoCastComplete = acComplete
    End Sub

    Private Sub SetProgressEvents()
        Dim evProgComplete As EventHandler = Sub() SetAutoCastResult(True)
        Dim evProgFail As EventHandler = Sub() SetAutoCastResult(False)

        With acProgressGui
            RemoveHandler .ProgressSuccess, evProgComplete
            RemoveHandler .ProgressFail, evProgFail

            AddHandler .ProgressSuccess, evProgComplete
            AddHandler .ProgressFail, evProgFail
        End With
    End Sub

    Private Function AutoCast_HandleResult(acComplete As Boolean) As ProgResult
        If acComplete Then
            UpdateProgStatus(TriggerAction.AutoCast,
                             ProgAction.Complete, True)
            SetProgResult(acComplete, retProgResult)
        Else
            UpdateProgStatus(TriggerAction.AutoCast,
                             ProgAction.Abort)
            SetProgResult(acComplete, retProgResult)
        End If

        Return retProgResult
    End Function

    Public Sub BeginPrep()
        If acProgressGui IsNot Nothing Then
            acProgressGui.Dispose()
        End If

        acProgressGui = CreateProgressGui()
    End Sub

    Private Function CreateProgressGui() As ProgBarGui_AutoCast
        Dim progSizeReport = GetProgSizeReport(TriggerType.AutoCast)

        Return New ProgBarGui_AutoCast With {
            .FormBorderStyle = osForms.FormBorderStyle.None,
            .ProgressHeight = progSizeReport.pHeight,
            .ProgressWidth = progSizeReport.pWidth,
            .BackColor = osDraw.Color.FromArgb(57, 57, 57)
        }
    End Function

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub

End Class

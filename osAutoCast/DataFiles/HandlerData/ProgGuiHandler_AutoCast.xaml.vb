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

    Private retProgResult As ProgResult = Nothing

    Private AutoCastComplete As Boolean

    Public acProgressGui As ProgBarGui_AutoCast = Nothing

    Private acProgLoc As osDraw.Point

    Private Async Function AutoCast_Prep() As Task
        Await Task.Delay(5)
        acProgressGui.DisplayMsg("Release Shift", TriggerType.AutoCast)

        Await InputMonSvc.AnticipateInput(InputAction.AC_Start)

        ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.ClrMsg)

        Await Task.Delay(375)
    End Function

    Public Sub InitiateAutoCast()
        With acProgressGui
            SetProgressEvents()

            GetPosGui(acProgLoc)
            Dim locProg = SetPosData(acProgLoc)

            .Left = locProg.X
            .Top = locProg.Y
            .Show()
            .DrawBG()
        End With
    End Sub

    Private Sub SetProgLocation(acComplete As Boolean)
        AutoCastComplete = acComplete
    End Sub

    Public Async Function LaunchAutoCast() As Task(Of ProgResult)
        Await AutoCast_Prep()

        Try
            Dim acProgTask = acProgressGui.BeginProgress(ProgTimeSpan, objCancelState, AddressOf EaseCustom)
            Await acProgTask
            Return AutoCast_HandleResult(AutoCastComplete)
        Finally
            UnsetProgressEvents()             ' <— important
        End Try
    End Function

    Private Sub SetAutoCastResult(acComplete As Boolean)
        AutoCastComplete = acComplete
    End Sub
    Private _evProgComplete As EventHandler
    Private _evProgFail As EventHandler

    Private Sub SetProgressEvents()
        ' If already wired for this acProgressGui, bail
        If _evProgComplete IsNot Nothing Then Exit Sub

        _evProgComplete = Sub() SetAutoCastResult(True)
        _evProgFail = Sub() SetAutoCastResult(False)

        AddHandler acProgressGui.ProgressSuccess, _evProgComplete
        AddHandler acProgressGui.ProgressFail, _evProgFail
    End Sub

    ' Call once when you’re done (end of LaunchAutoCast / right before disposing GUI):
    Private Sub UnsetProgressEvents()
        If _evProgComplete IsNot Nothing Then
            RemoveHandler acProgressGui.ProgressSuccess, _evProgComplete
            _evProgComplete = Nothing
        End If
        If _evProgFail IsNot Nothing Then
            RemoveHandler acProgressGui.ProgressFail, _evProgFail
            _evProgFail = Nothing
        End If
    End Sub
    'Private Sub SetProgressEvents()
    '    Dim evProgComplete As EventHandler = Sub() SetAutoCastResult(True)
    '    Dim evProgFail As EventHandler = Sub() SetAutoCastResult(False)

    '    With acProgressGui
    '        RemoveHandler .ProgressSuccess, evProgComplete
    '        RemoveHandler .ProgressFail, evProgFail

    '        AddHandler .ProgressSuccess, evProgComplete
    '        AddHandler .ProgressFail, evProgFail
    '    End With
    'End Sub

    Private Function AutoCast_HandleResult(acComplete As Boolean) As ProgResult
        If acComplete Then
            UpdateProgStatus(TriggerAction.AutoCast,
                             ProgAction.Complete, True)
            SetProgResult(acComplete, retProgResult)
        Else
            UpdateProgStatus(TriggerAction.AutoCast,
                             ProgAction.Abort, True)
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
        With GetProgSizeReport(TriggerType.AutoCast)
            Return New ProgBarGui_AutoCast(.pWidth, .pHeight)
        End With
    End Function

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub


End Class

Imports System.ComponentModel
Imports System.Windows.Media
Imports System.Threading
Imports osAutoCast.osFuncLib_Progress
Imports osAutoCast.CoreDataLib

Public Class progGui_AutoCast

    Private pHeight As Integer
    Private pWidth As Integer

    Private chkAutoCastResult As TaskCompletionSource(Of Boolean)
    Private retProgResult As ProgResult = Nothing

    Private AutoCastComplete As Boolean

    Private Async Function AutoCast_Prep() As Task

        Await InputMonSvc.AnticipateInput(InputAction.AC_Start)

        ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.ClrMsg)
        UpdateProgStatus(TriggerAction.AutoCast, ProgAction.Activate)

        InitiateAutoCast()
        Await Task.Delay(375)
    End Function

    Private Sub InitiateAutoCast()
        Dim evProgComplete As EventHandler = Sub() SetAutoCastResult(True)
        Dim evProgFail As EventHandler = Sub() SetAutoCastResult(False)

        With OddProgBar1
            RemoveHandler .ProgressComplete, evProgComplete
            RemoveHandler .ProgressFailed, evProgFail

            AddHandler .ProgressComplete, evProgComplete
            AddHandler .ProgressFailed, evProgFail
        End With
    End Sub

    Public Async Function LaunchAutoCast() As Task(Of ProgResult)
        Await AutoCast_Prep()

        Dim acProgTask = OddProgBar1.
            InitiateProgress(ProgTimeSpan, objCancelState, AddressOf EaseInOutCirc)

        Await acProgTask

        Return AutoCast_HandleResult(AutoCastComplete)
    End Function

    Private Sub SetAutoCastResult(acComplete As Boolean)
        'Debug.WriteLine(OddProgBar1.ProgressChunk)
        AutoCastComplete = acComplete
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
        ' OddProgBar1.Background = New SolidColorBrush(System.Windows.Media.Color.FromRgb(57, 57, 57))
        'OddProgBar1.IsAutoPass = False
    End Sub

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub

End Class

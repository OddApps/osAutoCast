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
        With OddProgBar1
            AddHandler .ProgressComplete, Sub() SetAutoCastResult(True)
            AddHandler .ProgressFailed, Sub() SetAutoCastResult(False)
        End With
    End Sub

    Public Async Function LaunchAutoCast() As Task(Of ProgResult)

        Await AutoCast_Prep()

        AddHandler OddProgBar1.ProgressComplete, Sub()
                                                     SetAutoCastResult(True)
                                                 End Sub

        AddHandler OddProgBar1.ProgressFailed, Sub()
                                                   SetAutoCastResult(False)
                                               End Sub

        Dim acStartTask = OddProgBar1.BeginProgress(ProgTimeSpan, objCancelState, AddressOf EaseInOutExpo)

        Await acStartTask

        Return AutoCast_HandleResult(AutoCastComplete)
    End Function

    Private Sub SetAutoCastResult(acComplete As Boolean)
        AutoCastComplete = acComplete
    End Sub

    Private Function AutoCast_HandleResult(acComplete As Boolean) As ProgResult
        If acComplete Then
            UpdateProgStatus(TriggerAction.AutoCast,
                             ProgAction.Complete)
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

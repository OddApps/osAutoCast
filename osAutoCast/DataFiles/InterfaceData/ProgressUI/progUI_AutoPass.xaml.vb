Imports osAutoCast.DataTypeLib.ProgressMode
Imports osAutoCast.DataTypeLib.TriggerAction
Imports osPrefData = osAutoCast.osPrefLib.osPreferenceLib
Imports osColors = System.Windows.Media
Imports osAutoCast.osControls

Public Class progUI_AutoPass

    Private chkAutoPassResult As TaskCompletionSource(Of Boolean)
    Private apProgressHandler As EventHandler = Nothing

    Private Async Function AutoPass_Prep() As Task

        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AP_Start)
        Await Task.Delay(100)

        osHandler_UI.osGui_AutoPass2.apHandler._DisplayTextFunc("Release Shift or Press C To Cancel")
        chkAutoPassResult.ResetAndInitTask()
    End Function

    Private Sub progUI_AutoPass_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.DataContext = osPrefData.Data
    End Sub

    Public Sub PrepAutoPass()
        apHandler = New osHandler_ProgressBar(GetSafetyTimer(), objProgBar.Maximum,
                                              AddressOf SetProgress, AddressOf SetColor, AddressOf SetDisplayText)
    End Sub

    Private Sub SetProgress(pVal As Double)
        objProgBar.Progress = pVal
    End Sub

    Private Sub SetColor(pColor As osColors.Color)
        objProgBar.FillColor = New SolidColorBrush(pColor)
    End Sub

    Private Sub SetDisplayText(pText As String)
        objProgBar.DisplayText = pText
    End Sub

    Private Function GetSafetyTimer() As Integer
        Return osPrefLib.osPreferenceLib.Data.AutoPass_SafetyTimer
    End Function

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
            osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass,
                                                ProgAction.Complete)

            SetProgResult(apComplete, retProgResult)
        Else
            osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, ProgAction.Abort)
            SetProgResult(apComplete, retProgResult)
        End If

        Return retProgResult

    End Function

    Public Sub BeginPrep()
        With CoreDataLib.FetchProgSizeReport(TriggerType.AutoPass)
            'pHeight = .Item("pH")
            'pWidth = .Item("pW")
        End With

        '   Me.OddProgBar_AP.ProgressFlow = ProgFlow.Descending
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

End Class

Partial Public Class progUI_AutoPass

    Public Property apHandler As osHandler_ProgressBar = Nothing

    Public ReadOnly Property objProgBar As osProgressBar
        Get
            Return Me.OddProgBar_AP
        End Get
    End Property

    Public Sub New()
        InitializeComponent()
    End Sub

End Class
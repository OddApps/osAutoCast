Imports osAutoCast.DataTypeLib.ProgressMode
Imports osAutoCast.DataTypeLib.TriggerAction
Imports osPrefData = osAutoCast.osPrefLib.osPreferenceLib
Imports osColors = System.Windows.Media
Imports osAutoCast.osControls
Imports osAutoCast.osFuncLib_Progress
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Windows.Interop

Public Class progUI_AutoPass

    Private Async Function AutoPass_Prep() As Task
        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AP_Start)
        Await Task.Delay(100)

        Me.SetDisplayText("Release Shift or Press C To Cancel")
    End Function

    Public Sub BeginPrep()
        Me.OddProgBar_AP.IsAutoPass = True
    End Sub

    Public Sub PrepAutoPass()
        With Me
            .DataContext = osPrefData.Data

            .objHandlerAP = New osHandler_ProgressBar(Me, True, GetSafetyTimer(), objProgBar.Maximum,
                                              AddressOf SetProgress, AddressOf SetColor, AddressOf SetDisplayText)

            .Show()
            .Hide()

            .Opacity = 1

            .objHandlerAP.PrepProgVis()
        End With
    End Sub

    Public Async Function LaunchAutoPass() As Task(Of ProgResult)
        Await AutoPass_Prep()

        Dim apResult = Await Me.objHandlerAP.StartProgress(True, CoreDataLib.objCancelState)
        Return AutoPass_HandleResult(apResult)
    End Function

    Private Function AutoPass_HandleResult(apResult As ProgResult) As ProgResult
        UpdateProgStatus(TriggerAutoPass, If(apResult = ProgResult.Completed,
                         ProgAction.Complete, ProgAction.Abort))

        Return apResult
    End Function

End Class

Partial Public Class progUI_AutoPass

    Public Property objHandlerAP As osHandler_ProgressBar = Nothing

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

    Public ReadOnly Property objProgBar As osProgressBar
        Get
            Return Me.OddProgBar_AP
        End Get
    End Property

    Public Sub New()
        InitializeComponent()
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

    Public Sub PresentAutoPassUI()
        '      Me.Show()

        osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, ProgAction.Activate)
        apHandler._DisplayTextFunc("Release Mouse To Begin")

        Dim objHwnd = New WindowInteropHelper(Me).Handle
        SetWindowPos(objHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
    End Sub

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        CoreDataLib.SetWinOpts(CoreDataLib.GetWinHwnd(Me))
    End Sub

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer,
                                         Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

End Class
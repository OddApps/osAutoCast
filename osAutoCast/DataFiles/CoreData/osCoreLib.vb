Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Reactive.Linq
Imports System.Reactive.Subjects
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Threading


Public NotInheritable Class osFuncLib_InputScan

    <DllImport("user32.dll")>
    Private Shared Function GetCursorPos(ByRef lpPoint As Point) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Integer) As Short
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetKeyState(nVirtKey As Integer) As Short
    End Function

    <DllImport("user32.dll")>
    Public Shared Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As UInteger, vk As UInteger) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Shared Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
    End Function

    Public Shared curMonitorStatus As MonitorStatus

    Public Shared isActionComplete As Boolean

    Public Sub New()
    End Sub

    Public Function GetMonitorState() As MonitorStatus
        Return curMonitorStatus
    End Function

    Public Shared Sub SetMonitorState(setMonStatus As MonitorStatus)
        curMonitorStatus = setMonStatus
    End Sub

    Public Shared Sub ActivateMonitor()
        SetMonitorState(MonitorStatus.Watching)
        osFuncLib_InputScan.isActionComplete = False
    End Sub

    Public Shared Function isMonitorInStartup() As Boolean
        Return curMonitorStatus = MonitorStatus.Starting
    End Function

    Public Shared Function isMonitorActive() As Boolean
        Return curMonitorStatus = MonitorStatus.Watching
    End Function

    Public Shared Function isActionTriggered() As Boolean
        Return curMonitorStatus = MonitorStatus.InCmd
    End Function

    Public Shared Function FindProgPosition(ByRef pt As Point) As Boolean
        Return GetCursorPos(pt)
    End Function

End Class

Public NotInheritable Class osFuncLib_Progress

    Public Shared progValue As Double = 0.0F

    Public Shared progDispMsg As String = "noStatus"
    Public Shared progShowMsg As Boolean = False

    Public Shared progContObj As Control

    Public Shared progSteps As Integer = 200

    Public Shared ProgDuration As Integer
    Public Shared ProgInv As Double

    Public Shared progBlock_W As Single
    Public Shared progBlock_H As Integer = 25

    Public Shared progCurStatus As ProgStatus

    Public Shared progColor As Color
    Public Shared progColor_AutoCast As Windows.Media.Color

    Public Shared progColorData As Windows.Media.Color

    Public Shared ProgBrush_BG As SolidBrush
    Public Shared ProgBrush_Active As SolidBrush
    Public Shared ProgBrush_Border As Pen

    Public Shared pBrush_BG As New SolidColorBrush(Media.Color.FromRgb(57, 57, 57))

    Public Shared pBrush_Active As SolidColorBrush
    Public Shared pBrush_Border As New Media.Pen(Media.Brushes.Black, 2)

    Public Shared ProgContainer As Rectangle
    Public Shared ProgContainerBorder As Rectangle

    Public Shared progFont_AC As New Font("Segoe UI", 9, FontStyle.Bold)
    Public Shared progFont_AP As New Font("Segoe UI", 10, FontStyle.Bold)

    Public Shared objAutoPassProg As SmoothProgressBarr = Nothing

    Private Shared ProgStatusColors As New Dictionary(Of ProgStatus, Color) From {
        {ProgStatus.Idle, Color.White},
        {ProgStatus.Running, Color.FromArgb(82, 96, 117)},
        {ProgStatus.Success, Color.ForestGreen},
        {ProgStatus.Fail, Color.Maroon},
        {ProgStatus.StartAP, Color.Maroon}
    }

    Private Shared ReadOnly ProgStatusColors_AutoCast As New Dictionary(Of ProgStatus, System.Windows.Media.Color) From {
        {ProgStatus.Idle, System.Windows.Media.Color.FromRgb(57, 57, 57)},
        {ProgStatus.Running, System.Windows.Media.Color.FromRgb(82, 96, 117)},
        {ProgStatus.Success, System.Windows.Media.Color.FromRgb(34, 139, 34)},
        {ProgStatus.Fail, System.Windows.Media.Color.FromRgb(97, 20, 20)},
        {ProgStatus.StartAP, System.Windows.Media.Color.FromRgb(97, 20, 20)}
    }

    Private Shared ReadOnly ProgStatusColorsIndex As New Dictionary(Of ProgStatus, System.Windows.Media.Color) From {
        {ProgStatus.Idle, System.Windows.Media.Color.FromRgb(57, 57, 57)},
        {ProgStatus.Running, System.Windows.Media.Color.FromRgb(82, 96, 117)},
        {ProgStatus.Success, System.Windows.Media.Color.FromRgb(34, 139, 34)},
        {ProgStatus.Fail, System.Windows.Media.Color.FromRgb(97, 20, 20)},
        {ProgStatus.StartAP, System.Windows.Media.Color.FromRgb(97, 20, 20)}
    }

    Public Shared Sub SetProgContainer(pType As TriggerType)
        ProgContainer = New Rectangle(0, 0, CoreDataLib.GetProgSize(pType), CoreDataLib.GetProgSize(pType, True))
        ProgContainerBorder = New Rectangle(0, 0, CoreDataLib.GetProgSize(pType) - 1, CoreDataLib.GetProgSize(pType, True) - 1)
    End Sub

    Public Shared Sub SetProgState(newStatus As ProgStatus)
        progCurStatus = newStatus
        pBrush_BG = New SolidColorBrush()
    End Sub

    Public Shared Sub SetProgState(optStatus As String)
        Select Case optStatus.ToLower()
            Case "i"
                progCurStatus = ProgStatus.Idle
            Case "r"
                progCurStatus = ProgStatus.Running
            Case "s"
                progCurStatus = ProgStatus.Success
            Case "f"
                progCurStatus = ProgStatus.Fail
            Case "sap"
                progCurStatus = ProgStatus.StartAP
        End Select
    End Sub

    Public Shared Function GetProgState() As ProgStatus
        Return progCurStatus
    End Function

    Public Shared Function IsProgRunning() As Boolean
        Return osFuncLib_Progress.GetProgState() = ProgStatus.Running
    End Function

    Public Shared Function ShowProgFull() As Boolean
        Return progCurStatus = ProgStatus.Success OrElse progCurStatus = ProgStatus.Fail
    End Function

    Public Shared Function IsProgSuccess() As ProgStatus
        Return progCurStatus = ProgStatus.Success
    End Function

    Public Shared Sub SetProgStatus(setAction As ProgAction, pType As TriggerType, Optional progGUI As Form = Nothing)
        If pType = TriggerType.AutoCast Then
            Select Case setAction
                Case ProgAction.Abort
                    SetProgState("f")
                    progValue = 1.0
                Case ProgAction.Activate
                    progValue = 0.0
                    SetProgState("r")
                Case ProgAction.Complete
                    SetProgState("s")
                    progValue = 1.0
                Case ProgAction.Reset
                    SetProgState("i")
                    progValue = 0.0
            End Select
            SetProgColor(pType)
        Else
            Select Case setAction
                Case ProgAction.Abort
                    SetProgState("f")
                    progValue = 1.0
                Case ProgAction.Activate
                    SetProgState("sap")
                    progValue = 1.0
                Case ProgAction.Complete
                    SetProgState("s")
                    progValue = 1.0
                Case ProgAction.Reset
                    SetProgState("i")
                    progValue = 0.0
            End Select
            SetProgColor(pType)
        End If
    End Sub

    Public Shared Sub DisplayProgText(progTxt As String, Optional guiUpdate As Boolean = False, Optional guiForm As Form = Nothing, Optional guiProg As Control = Nothing)
        If progTxt = "" Then
            progShowMsg = False
            progDispMsg = ""
        Else
            progShowMsg = True
            progDispMsg = progTxt
            If guiUpdate Then
                guiForm?.Invalidate()
                guiProg?.Invalidate()
            End If
        End If
    End Sub

    Public Shared Sub DisplayProgText(progTxt As String, showProgMsg As Boolean)
        If progTxt = "" Then
            progShowMsg = False
            progDispMsg = ""
        Else
            progShowMsg = True
            progDispMsg = progTxt
        End If
    End Sub

    Public Shared Function CalcTargetTime(sTime As Long, valDuration As Integer, repCnt As Integer, repRate As Double) As Long
        Return sTime + CLng(Math.Round(valDuration * repCnt * repRate))
    End Function

    Public Shared Sub ApplyActiveColor(optColor As System.Windows.Media.Color)
        pBrush_Active = New SolidColorBrush(optColor)
    End Sub

    Private Shared Sub SetProgColor(pType As TriggerType)
        Select Case pType
            Case TriggerType.AutoCast
                osHandler_GUI.osGui_AutoCast.
                    Dispatcher.Invoke(
                    Sub()
                        progColorData = FetchProgColor(GetProgState(), True)
                        ApplyActiveColor(progColorData)
                        osHandler_GUI.osGui_AutoCast.OddProgBar1.SetProgColor(progColorData)
                    End Sub)
            Case TriggerType.AutoPass
                osHandler_GUI.osGui_AutoPass.
                    Dispatcher.Invoke(
                    Sub()
                        progColorData = FetchProgColor(GetProgState(), True)
                        ApplyActiveColor(progColorData)
                        osHandler_GUI.osGui_AutoPass.OddProgBar_AP.SetProgColor(progColorData)
                    End Sub)
        End Select
    End Sub

    Public Shared Sub ConstructProgContainer(progG As Graphics)
        progG.SmoothingMode = SmoothingMode.AntiAlias
        progG.Clear(Color.FromArgb(22, 22, 22))
        progG.FillRectangle(ProgBrush_BG, ProgContainer)
    End Sub

    Public Shared Sub ConstructProgBorder(progG As Graphics, Optional noFill As Boolean = False)
        progG.SmoothingMode = SmoothingMode.AntiAlias
        If Not noFill Then progG.Clear(Color.FromArgb(22, 22, 22))
        progG.DrawRectangle(ProgBrush_Border, ProgContainerBorder)
    End Sub

    Public Shared Sub ConstructProgFull(progG As Graphics, Optional noFill As Boolean = False)
        progG.SmoothingMode = SmoothingMode.AntiAlias
        If Not noFill Then progG.Clear(Color.FromArgb(22, 22, 22))
        progG.DrawRectangle(ProgBrush_Border, ProgContainerBorder)
    End Sub

    Public Shared Sub InitProgColors()
        ProgBrush_BG = New SolidBrush(Color.FromArgb(40, 40, 40))
        ProgBrush_Active = New SolidBrush(Color.DeepSkyBlue)
        ProgBrush_Border = New Pen(Color.Black, 2.0F)
    End Sub

    Public Shared Function FetchProgColor(pStatus As ProgStatus) As Color
        Return ProgStatusColors(pStatus)
    End Function

    Public Shared Function FetchProgColor(pStatus As ProgStatus, idxColors As Boolean) As System.Windows.Media.Color
        Return ProgStatusColorsIndex(pStatus)
    End Function

    Public Shared Function CalcPosData(ptPos As Point) As Point
        Return New Point(ptPos.X - CInt(Math.Round(CoreDataLib.GetProgSize(TriggerType.AutoCast) / 2.0)),
                         ptPos.Y - CoreDataLib.GetProgSize(TriggerType.AutoCast, True) - 22)
    End Function

    Public Shared Function CalcProgSize() As System.Drawing.Size
        Return New System.Drawing.Size(CoreDataLib.GetProgSize(TriggerType.AutoCast), CoreDataLib.GetProgSize(TriggerType.AutoCast, True))
    End Function

    Public Shared Function CalcProgSize(pType As TriggerType) As System.Drawing.Size
        Return New System.Drawing.Size(CoreDataLib.GetProgSize(pType), CoreDataLib.GetProgSize(pType, True))
    End Function

    Public Shared Sub SetProgBlockData(trigType As TriggerType)
        ProgDuration = If(trigType = TriggerType.AutoCast, CoreDataLib.GetFuse(), CoreDataLib.GetSafetyTimer())
        ProgInv = 1.0 / ProgDuration
    End Sub

    Public Shared Sub DisplayProgress(guiAutoCast As Form, ptPos As Point)
        guiAutoCast.Location = CalcPosData(ptPos)
        guiAutoCast.Show()
        guiAutoCast.Size = CalcProgSize()
    End Sub

    Public Shared Sub DisplayProgress(guiAutoPass As Form, isAutoPass As Boolean)
        guiAutoPass.Show()
        guiAutoPass.Invalidate()
    End Sub



    ' -- Easing functions --

    Public Shared Function EaseInOutExpo(x As Double) As Double
        If x = 0.0 Then Return 0.0
        If x = 1.0 Then Return 1.0
        Return If(x < 0.5, Math.Pow(2, 20 * x - 10) / 2, (2 - Math.Pow(2, -20 * x + 10)) / 2)
    End Function

    Public Shared Function EaseInOutCustom(x As Double) As Double
        Dim num As Double
        If x < 0.4 Then
            Dim t = x / 0.4
            num = 0.32 * t * t
        ElseIf x >= 0.8 Then
            num = 0.88 + (1.0 - Math.Pow(1.0 - (x - 0.8) / 0.2, 2.0)) * 0.12
        Else
            num = 0.32 + (x - 0.4) / 0.4 * 0.56
        End If
        Return num
    End Function

    Public Shared Function EaseInOutSine(x As Double) As Double
        Return -(Math.Cos(Math.PI * x) - 1.0) / 2.0
    End Function

    Public Shared Function EaseInOutCube(x As Double) As Double
        Return If(x >= 0.5, 1.0 - Math.Pow(-2 * x + 2, 3) / 2.0, 4 * x * x * x)
    End Function

    Public Shared Function EaseOutCubic(t As Double) As Double
        Return 1.0 - Math.Pow(1.0 - t, 3)
    End Function

End Class

Public NotInheritable Class osFuncLib_AutoCast

    Private Shared ptPos As Point
    Private Shared objFuncLib_InputScan As New osFuncLib_InputScan()

    <DllImport("user32.dll", EntryPoint:="mouse_event", SetLastError:=True)>
    Private Shared Sub InvokeMouse(dwFlags As UInteger, dx As UInteger, dy As UInteger, cButtons As UInteger, dwExtraInfo As IntPtr)
    End Sub

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function EnableWindow(hWnd As IntPtr, bEnable As Boolean) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function GetModuleHandle(lpModuleName As String) As IntPtr
    End Function

    Public Shared Sub ToggleInputBlock(doBlock As Boolean)
        EnableWindow(DetectGameUI.FetchHwndMTGA(), Not doBlock)
    End Sub

    Public Shared Async Sub InvokeAutoCast()
        If CoreDataLib.isRTC() Then
            Await Task.Delay(75)
            Await osHandler_Input.SuppressInput()
            ExecClicker(True)
            HoldInputs(True)
        Else
            LiberateLeftClick()
            Await osHandler_Input.SuppressInput()
            ExecClicker(True)
            HoldInputs(True)
        End If
    End Sub

    Public Shared Async Sub EngageAutoCast()
        If CoreDataLib.isRTC() Then
            Await Task.Delay(75)
            ExecClicker(True)
            HoldInputs(True)
        Else
            LiberateLeftClick()
            Await Task.Delay(75)
            ExecClicker(True)
            HoldInputs(True)
        End If
    End Sub

    'Public Shared Async Function ExecuteAutoCast() As Task
    '    Dim acResult As ProgResult = ProgResult.Completed

    '    Await TaskExtensions.Unwrap(osHandler_GUI.osGui_AutoCast.Dispatcher.InvokeAsync(Of Task)(Async Function()
    '                                                                                                 If Not osFuncLib_InputScan.FindProgPosition(ptPos) Then
    '                                                                                                     Return
    '                                                                                                 End If
    '                                                                                                 osFuncLib_Progress.SetProgContainer(TriggerType.AutoCast)
    '                                                                                                 osHandler_GUI.DisplayGUI(TriggerType.AutoCast, ptPos)
    '                                                                                                 osFuncLib_Progress.SetProgBlockData(TriggerType.AutoCast)

    '                                                                                                 ' Closure variable (compiler-generated in C#)
    '                                                                                                 Dim closure90 As Object = Nothing
    '                                                                                                 closure90 = Await osHandler_GUI.osGui_AutoCast.LaunchAutoCast()
    '                                                                                             End Function).Task)

    '    Await ProcessResult(acResult)
    '    osFuncLib_InputScan.isActionComplete = True
    'End Function

    Public Shared Async Function ExecuteAutoCast() As Task
        Dim acResult As ProgResult = ProgResult.Completed

        Dim retProgResult As ProgResult = Nothing

        Dim isTask_AutoCast = osHandler_GUI.osGui_AutoCast.
            Dispatcher.InvokeAsync(Async Function()
                                       If osFuncLib_InputScan.FindProgPosition(ptPos) Then
                                           osFuncLib_Progress.SetProgContainer(TriggerType.AutoCast)

                                           osHandler_GUI.DisplayGUI(TriggerType.AutoCast, ptPos)
                                           osFuncLib_Progress.SetProgBlockData(TriggerType.AutoCast)

                                           retProgResult = Await osHandler_GUI.osGui_AutoCast.LaunchAutoCast(True)
                                       End If
                                   End Function)

        Await isTask_AutoCast.Task.Unwrap()

        Await ProcessResult(retProgResult)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetCursorPos(ByRef lpPoint As Point) As Boolean
    End Function
    Private Shared Function FindProgPos(ByRef pt As Point) As Boolean
        Return GetCursorPos(pt)
    End Function

    Private Shared Async Function ProcessResult(acResult As ProgResult) As Task
        Try
            Select Case acResult
                Case ProgResult.Completed
                    If CoreDataLib.isRTC() Then
                        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.DispMsg, "Release To Cast")
                        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AC_RTC)
                    End If

                    Await osHandler_GUI.osGui_AutoCast.Dispatcher.InvokeAsync(
                        Async Function()
                            Await Task.Delay(150)

                            CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.DispMsg, "Casting")
                        End Function, DispatcherPriority.ApplicationIdle)
                    EngageAutoCast()

                Case ProgResult.Cancelled
                    Await osHandler_GUI.osGui_AutoCast.Dispatcher.InvokeAsync(
                        Async Function()
                            Await Task.Delay(150)
                            CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast,
                                                             ProgEvent.DispMsg, "Cancelled")
                        End Function, DispatcherPriority.ApplicationIdle)
            End Select

            Await FinalizeAutoCast()
        Catch ex As Exception

        End Try
    End Function

    Private Shared Async Function FinalizeAutoCast() As Task
        Await Task.Delay(750)
        Dim dispatcher As Dispatcher = osHandler_GUI.osGui_AutoCast.Dispatcher
        Dim callback As Action = Sub() osHandler_GUI.osGui_AutoCast.Close()
        dispatcher.Invoke(callback)
    End Function

    Private Shared Async Sub HoldInputs(doAsync As Boolean)
        Await Task.Delay(1250)
        Await osHandler_Input.RestoreInput()
    End Sub

    Private Shared Async Sub ExecClicker(Optional doDbl As Boolean = False)
        PerformLeftClk()
        If doDbl Then
            Await Task.Delay(105)
            PerformLeftClk()
        End If
    End Sub

    Private Shared Sub PerformLeftClk()
        InvokeMouse(2UI, 0UI, 0UI, 0UI, IntPtr.Zero)
        InvokeMouse(4UI, 0UI, 0UI, 0UI, IntPtr.Zero)
    End Sub

    Private Shared Sub LiberateLeftClick()
        InvokeMouse(4UI, 0UI, 0UI, 0UI, IntPtr.Zero)
    End Sub

End Class

Public NotInheritable Class osFuncLib_ShowOpts

    Private Shared chkCloseSettings As TaskCompletionSource(Of Boolean)

    Public Shared Async Function ExecuteDispOpts() As Task
        CoreDataLib.PrepTrigger(TriggerType.ShowPrefs)

        osHandler_GUI.osGui_InputMonitor2.Dispatcher.Invoke(Sub()
                                                                With osHandler_GUI.osGui_Prefs
                                                                    .Show()
                                                                    .Focus()
                                                                    osPrefs_PrepHandlers()
                                                                End With
                                                            End Sub)

        Await chkCloseSettings.Task

        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Shared Sub osPrefs_PrepHandlers()
        chkCloseSettings = New TaskCompletionSource(Of Boolean)()
        Dim osGuiPrefs As osPrefs = osHandler_GUI.osGui_Prefs

        RemoveHandler osGuiPrefs.VisibleChanged, Nothing
        AddHandler osGuiPrefs.VisibleChanged, Sub(sender As Object, e As EventArgs)
                                                  If osGuiPrefs.Visible Then
                                                      Return
                                                  End If
                                                  chkCloseSettings.TrySetResult(True)
                                              End Sub
    End Sub

    Private Shared Sub ResetStatus()
        If osFuncLib_InputScan.isActionComplete Then
            osFuncLib_InputScan.isActionComplete = False
        End If
        osFuncLib_InputScan.SetMonitorState(MonitorStatus.Watching)
    End Sub

End Class

Public NotInheritable Class osFuncLib_AutoPass

    Public Shared Async Sub InvokeAutoPass()
        CoreDataLib.SetGameFocus()
        Await Task.Delay(100)
        osHandler_Input.InjectAutoPassInputs()
    End Sub

    Public Shared Async Function ExecuteAutoPass() As Task
        Dim acResult As ProgResult = ProgResult.Completed

        ' Display GUI and set progress
        Await osHandler_GUI.osGui_AutoPass.Dispatcher.InvokeAsync(Async Function()
                                                                      osHandler_GUI.DisplayGUI(TriggerType.AutoPass)
                                                                      osFuncLib_Progress.SetProgBlockData(TriggerType.AutoPass)
                                                                      Await Task.Delay(50)
                                                                      Await osHandler_GUI.osGui_AutoPass.LaunchAutoPass()
                                                                  End Function)

        Await ProcessResult(acResult)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Shared Async Function AnticipateLaunchAP() As Task(Of Boolean)
        Dim flag As Boolean = Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AP_Exec)
        If Not flag Then
            osFuncLib_Progress.SetProgStatus(ProgAction.Abort, TriggerType.AutoPass)
            CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
        End If
        Return flag
    End Function

    Private Shared Async Function ProcessResult(acResult As ProgResult) As Task
        Try
            Select Case acResult
                Case ProgResult.Completed
                    CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "Release Shift To AutoPass | Press C To Cancel")
                    If Await AnticipateLaunchAP() Then
                        CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPassing")
                        InvokeAutoPass()
                    Else
                        osFuncLib_Progress.SetProgStatus(ProgAction.Abort, TriggerType.AutoPass)
                        CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
                    End If

                Case ProgResult.Cancelled
                    osFuncLib_Progress.SetProgStatus(ProgAction.Abort, TriggerType.AutoPass)
                    CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
            End Select

            Await FinalizeAutoPass()
        Catch ex As Exception
        End Try
    End Function

    Private Shared Async Function FinalizeAutoPass() As Task
        Await Task.Delay(750)
        Await osHandler_GUI.osGui_AutoPass.Dispatcher.InvokeAsync(Async Function()
                                                                      CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.Reset)
                                                                      Await Task.Delay(100)
                                                                      osHandler_GUI.osGui_AutoPass.Close()
                                                                  End Function)
    End Function

End Class

Module osFuncLib_TrayMenu

    Private Property osIsEnabled As Boolean

    Private osTrayIcon As New NotifyIcon

    Private objInputMon As osInMon

    Public Function GetEnabledStatus() As Boolean
        Return osIsEnabled
        '   osTrayIcon.
    End Function

    Public Function IsDisabled() As Boolean
        Return osIsEnabled = False
    End Function

    Private Function ConfirmStatusChange(newStatus As Boolean) As UpdateStatus
        If newStatus = False Then
            Select Case MsgBox("Disable OddScript?", vbYesNo,
                                   "Confirm...")
                Case vbYes
                    osFuncLib_InputScan.SetMonitorState(MonitorStatus.Paused)
                    Return UpdateStatus.ToDisabled
                Case Else
                    Return UpdateStatus.CancelUpdate
            End Select
        Else
            osFuncLib_InputScan.SetMonitorState(MonitorStatus.Starting)
            objInputMon.RestartMonitor()

            Return UpdateStatus.ToEnabled
        End If
    End Function

    Private Sub SetNewStatus(setStatus As Boolean)
        osIsEnabled = setStatus
    End Sub

    Private Sub UpdateTrayIcon(chkStatus As Boolean)
        osTrayIcon.Icon = If(chkStatus, My.Resources.osIcon,
            My.Resources.osIcon_Disabled)
    End Sub

    Private Sub PrepTrayMenu(objOsMenu As ContextMenuStrip)
        With osTrayIcon
            .Icon = My.Resources.osIcon
            .Text = "OddMTGA | Enabled"
            .Visible = True

            .ContextMenuStrip = objOsMenu
        End With
    End Sub

    Private Function GenMenuItem(txtMenu As String) As ToolStripMenuItem
        Return New ToolStripMenuItem(txtMenu)
    End Function

    Public Function CreateTrayMenu() As ContextMenuStrip
        Dim osMenuComponents As New System.ComponentModel.Container()

        Dim osMenuObj As New ContextMenuStrip(osMenuComponents)

        Dim osMenu_EnDis As New ToolStripMenuItem("Enabled") With {.Checked = True, .CheckState = CheckState.Checked, .Name = "osMenuEnDis"}
        Dim ToolStripSeparator2 As New ToolStripSeparator()
        Dim osMenu_GameOpts = GenMenuItem("MTG Menu")
        Dim osGameMenu_Play = GenMenuItem("Play Game")
        Dim osGameMenu_Leave = GenMenuItem("Leave Game")
        Dim ToolStripSeparator1 As New ToolStripSeparator()
        Dim osMenu_Opts = GenMenuItem("Preferences")
        Dim osMenuExit = GenMenuItem("Exit")

        osMenu_GameOpts.DropDownItems.AddRange(New ToolStripItem() {osGameMenu_Play, osGameMenu_Leave})
        osMenuObj.Items.AddRange(New ToolStripItem() {osMenu_EnDis, ToolStripSeparator2, osMenu_GameOpts, ToolStripSeparator1, osMenu_Opts, osMenuExit})

        osMenuObj.Font = New Font("Trebuchet MS", 11.25F, FontStyle.Regular)
        osMenuObj.RenderMode = ToolStripRenderMode.Professional
        osMenuObj.ShowCheckMargin = True

        AddHandler osMenu_Opts.Click, Async Sub()
                                          osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
                                          Await osFuncLib_ShowOpts.ExecuteDispOpts()
                                      End Sub

        AddHandler osMenuExit.Click, Sub()
                                         Dim chkConfirmExit = MsgBox("Are you sure you want to exit OddScript?",
                                                                     vbYesNo + vbQuestion, "Confirm Close")

                                         If chkConfirmExit = vbNo Then Exit Sub

                                         osStopApp()
                                     End Sub

        Return osMenuObj
    End Function

    Private Sub osStopApp()
        osTrayIcon.Visible = False
        End
    End Sub

    Public Sub osMenu_Init(objInMon As osInMon)

        CoreDataLib.osTrayMenu = CreateTrayMenu()
        PrepTrayMenu(CoreDataLib.osTrayMenu)

        osIsEnabled = True

        objInputMon = objInMon

        With New osFuncData(AddressOf GetEnabledStatus, AddressOf ConfirmStatusChange,
                            AddressOf SetNewStatus, AddressOf UpdateTrayIcon)

            osMenuFuncBinder.BindChecked(CoreDataLib.osTrayMenu.Items.Item("osMenuEnDis"),
                                         .osFunc_GetStatus, .osFunc_ApplyStatus,
                                         .osFunc_ConfirmStatus, .osFunc_UpdateIcon)
        End With

    End Sub

End Module

Module osFuncLib_Timer

    Private Const INFINITE As UInteger = &HFFFFFFFFUI

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function CreateWaitableTimer(lpTimerAttributes As IntPtr, bManualReset As Boolean, lpTimerName As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function SetWaitableTimer(hTimer As IntPtr, ByRef lpDueTime As Long, lPeriod As Integer,
    pfnCompletionRoutine As IntPtr, lpArgToCompletionRoutine As IntPtr, fResume As Boolean) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function WaitForSingleObject(hHandle As IntPtr, dwMilliseconds As UInteger) As UInteger
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function CancelWaitableTimer(hTimer As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function CloseHandle(hObject As IntPtr) As Boolean
    End Function

    Public Async Function ProgressDelay(delayInMilliseconds As Double) As Task
        Await Task.Run(Sub()
                           ProgWait(delayInMilliseconds)
                       End Sub)
    End Function

    Public Sub ProgWait(delayInMilliseconds As Double)
        Dim hTimer As IntPtr = CreateWaitableTimer(IntPtr.Zero, True, Nothing)

        If hTimer = IntPtr.Zero Then
            Throw New Exception("Failed to create timer.")
        End If

        Dim dueTime As Long = -CLng(delayInMilliseconds * 10000)

        If Not SetWaitableTimer(hTimer, dueTime, 0, IntPtr.Zero, IntPtr.Zero, False) Then
            Throw New Exception("Failed to set timer.")
        End If

        WaitForSingleObject(hTimer, INFINITE)

        CancelWaitableTimer(hTimer)
        CloseHandle(hTimer)
    End Sub

End Module

Module osFuncLib_UI

    Public Sub SetRoundedCorners(panel As Panel, radius As Integer)
        Dim path As New GraphicsPath()

        With path
            .StartFigure()
            .AddArc(New Rectangle(0, 0, radius, radius), 180, 90)
            .AddArc(New Rectangle(panel.Width - radius, 0, radius, radius), 270, 90)
            .AddArc(New Rectangle(panel.Width - radius, panel.Height - radius, radius, radius), 0, 90)
            .AddArc(New Rectangle(0, panel.Height - radius, radius, radius), 90, 90)
            .CloseFigure()
        End With

        panel.Region = New Region(path)
    End Sub

    Private Function GetRoundedRectangle(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()

        With path
            .StartFigure()
            .AddArc(rect.X, rect.Y, radius, radius, 180, 90)
            .AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90)
            .AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90)
            .AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90)
            .CloseFigure()
        End With

        Return path
    End Function

    Public Sub ApplySmoothRoundedPanel(ctrlPanel As Panel)
        Dim borderPanelPath As GraphicsPath = GetRoundedRectangle(ctrlPanel.ClientRectangle, 22)
        ctrlPanel.Region = New Region(borderPanelPath)

        AddHandler ctrlPanel.Paint, Sub(sender, e)
                                        Using BorderPanelStylus As New Pen(Color.Black, 4)
                                            BorderPanelStylus.Alignment = Drawing2D.PenAlignment.Outset
                                            e.Graphics.SmoothingMode = SmoothingMode.HighQuality
                                            e.Graphics.DrawPath(BorderPanelStylus, borderPanelPath)
                                        End Using
                                    End Sub
        ctrlPanel.Invalidate()
    End Sub

End Module

Public Class osMenuFuncBinder

    Public Enum UpdateStatus
        ToEnabled
        ToDisabled
        CancelUpdate
    End Enum

    Public Shared Sub BindChecked(objMenuItem As ToolStripMenuItem,
                                  DoFunc_FetchStatus As Func(Of Boolean),
                                  DoFunc_ApplyStatus As Action(Of Boolean),
                                  DoFunc_ConfirmStatus As Func(Of Boolean, osMenuFuncBinder.UpdateStatus),
                                  DoFunc_UpdateIcon As Action(Of Boolean))
        With objMenuItem
            .Checked = DoFunc_FetchStatus()

            AddHandler .Click, Sub()
                                   Dim chkCurStatus = .Checked
                                   Dim chkNewStatus = Not chkCurStatus

                                   If DoFunc_ConfirmStatus(chkNewStatus) =
                                   UpdateStatus.CancelUpdate Then Return

                                   .Checked = chkNewStatus
                                   DoFunc_UpdateIcon(chkNewStatus)
                                   DoFunc_ApplyStatus(chkNewStatus)
                               End Sub
        End With
    End Sub

End Class

Module ControlExtensions

    <Extension()>
    Public Function InvokeAsync(ctrl As Control, action As Action) As Task
        Dim tcs As New TaskCompletionSource(Of Object)()

        If ctrl.InvokeRequired Then
            ctrl.BeginInvoke(
                Sub()
                    Try
                        action()
                        tcs.SetResult(Nothing)
                    Catch ex As Exception
                        tcs.SetException(ex)
                    End Try
                End Sub)
        Else
            Try
                action()
                tcs.SetResult(Nothing)
            Catch ex As Exception
                tcs.SetException(ex)
            End Try
        End If

        Return tcs.Task
    End Function

    <Extension()>
    Public Function FirstOrDefault(Of TSource)(source As IEnumerable(Of TSource), predicate As Func(Of TSource, Boolean),
                                               defaultValue As TSource) As TSource
        For Each item In source
            If predicate(item) Then Return item
        Next

        Return defaultValue
    End Function

End Module

Public NotInheritable Class TextBlockExtensions
    Private Sub New()
    End Sub

    ' Public attached property: CharacterSpacing (in device-independent pixels)
    Public Shared ReadOnly CharacterSpacingProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "CharacterSpacing",
                GetType(Double),
                GetType(TextBlockExtensions),
                New PropertyMetadata(0.0, AddressOf OnCharacterSpacingChanged))

    Public Shared Sub SetCharacterSpacing(obj As DependencyObject, value As Double)
        obj.SetValue(CharacterSpacingProperty, value)
    End Sub

    Public Shared Function GetCharacterSpacing(obj As DependencyObject) As Double
        Return CDbl(obj.GetValue(CharacterSpacingProperty))
    End Function

    ' Internal flag so we only hook once
    Private Shared ReadOnly IsHookedProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "IsHooked",
                GetType(Boolean),
                GetType(TextBlockExtensions),
                New PropertyMetadata(False))

    Private Shared Sub SetIsHooked(obj As DependencyObject, value As Boolean)
        obj.SetValue(IsHookedProperty, value)
    End Sub

    Private Shared Function GetIsHooked(obj As DependencyObject) As Boolean
        Return CBool(obj.GetValue(IsHookedProperty))
    End Function

    ' Shared descriptor for TextBlock.Text changes
    Private Shared ReadOnly TextDescriptor As System.ComponentModel.DependencyPropertyDescriptor =
            System.ComponentModel.DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, GetType(TextBlock))

    Private Shared Sub OnCharacterSpacingChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim tb = TryCast(d, TextBlock)
        If tb Is Nothing Then Return

        Dim spacing = CDbl(e.NewValue)

        If spacing <> 0 Then
            Hook(tb)
            UpdateEffects(tb, spacing)
        Else
            ' spacing == 0 → remove effects and unhook
            tb.TextEffects = Nothing
            Unhook(tb)
        End If
    End Sub

    Private Shared Sub Hook(tb As TextBlock)
        If GetIsHooked(tb) Then Return
        SetIsHooked(tb, True)

        ' Apply once when loaded (covers initial layout)
        AddHandler tb.Loaded, AddressOf Tb_Loaded

        ' Re-apply whenever Text changes
        If TextDescriptor IsNot Nothing Then
            TextDescriptor.AddValueChanged(tb, AddressOf Tb_TextChanged)
        End If
    End Sub

    Private Shared Sub Unhook(tb As TextBlock)
        If Not GetIsHooked(tb) Then Return
        SetIsHooked(tb, False)

        RemoveHandler tb.Loaded, AddressOf Tb_Loaded

        If TextDescriptor IsNot Nothing Then
            TextDescriptor.RemoveValueChanged(tb, AddressOf Tb_TextChanged)
        End If
    End Sub

    Private Shared Sub Tb_Loaded(sender As Object, e As RoutedEventArgs)
        Dim tb = DirectCast(sender, TextBlock)
        UpdateEffects(tb, GetCharacterSpacing(tb))
    End Sub

    Private Shared Sub Tb_TextChanged(sender As Object, e As EventArgs)
        Dim tb = DirectCast(sender, TextBlock)
        UpdateEffects(tb, GetCharacterSpacing(tb))
    End Sub

    Private Shared Sub UpdateEffects(tb As TextBlock, spacing As Double)
        If tb Is Nothing Then Return

        ' Clear previous effects (if any)
        tb.TextEffects = Nothing

        If spacing = 0 Then Return

        Dim text = If(tb.Text, String.Empty)
        If text.Length = 0 Then Return

        ' Build a TextEffect per character that shifts it by i * spacing
        Dim effects As New TextEffectCollection()

        ' NOTE: This is simple indexing and does not special-case surrogate pairs.
        ' For typical UI strings it’s fine; expand as needed for advanced scenarios.
        For i As Integer = 0 To text.Length - 1
            Dim te As New TextEffect() With {
                    .PositionStart = i,
                    .PositionCount = 1,
                    .Transform = New TranslateTransform(i * spacing, 0)
                }
            effects.Add(te)
        Next

        tb.TextEffects = effects
    End Sub
End Class
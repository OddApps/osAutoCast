Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Reactive.Linq
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Threading
Imports System.Text
Imports System.Globalization
Imports System.Windows.Markup
Imports System.Xml
Imports osAutoCast.DataTypeLib.TriggerType
Imports osAutoCast.DataTypeLib.TriggerAction
Imports osAutoCast.DataTypeLib.ProgMode
Imports osAutoCast.DataTypeLib.ProgAction
Imports osAutoCast.DataTypeLib.ProgStatus
Imports osAutoCast.DataTypeLib.ProgEvent
Imports osAutoCast.DataTypeLib.ProgEaseVals
Imports osAutoCast.CoreDataLib
Imports osForms = System.Windows.Forms
Imports osInput = System.Windows.Input
Imports osBinder = System.Windows.Data
Imports osColors = System.Windows.Media
Imports osThreads = System.Threading
Imports osControls = System.Windows.Controls

#Disable Warning IDE0060 ' Remove unused parameter
#Disable Warning IDE1006 ' Remove unused parameter
Public NotInheritable Class osFuncLib_InputScan

    Public Shared curMonitorStatus As MonitorStatus
    Public Shared isActionComplete As Boolean

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

    Public Shared Sub FindPosGui(ByRef pt As Point)
        GetCursorPos(pt)
    End Sub

End Class

Public Module osFuncLib_Pos

    <DllImport("user32.dll")>
    Private Function GetCursorPos(ByRef lpPoint As Point) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Function GetAsyncKeyState(vKey As Integer) As Short
    End Function

    <DllImport("user32.dll")>
    Private Function GetKeyState(nVirtKey As Integer) As Short
    End Function

    <DllImport("user32.dll")>
    Public Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As UInteger, vk As UInteger) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
    End Function

    Public curMonitorStatus As MonitorStatus

    Public isActionComplete As Boolean

    Public Function GetMonitorState() As MonitorStatus
        Return curMonitorStatus
    End Function

    Public Sub SetMonitorState(setMonStatus As MonitorStatus)
        curMonitorStatus = setMonStatus
    End Sub

    Public Sub ActivateMonitor()
        SetMonitorState(MonitorStatus.Watching)
        osFuncLib_InputScan.isActionComplete = False
    End Sub

    Public Function isMonitorInStartup() As Boolean
        Return curMonitorStatus = MonitorStatus.Starting
    End Function

    Public Function isMonitorActive() As Boolean
        Return curMonitorStatus = MonitorStatus.Watching
    End Function

    Public Function isActionTriggered() As Boolean
        Return curMonitorStatus = MonitorStatus.InCmd
    End Function

    Public Function FindProgPosition(ByRef pt As Point) As Boolean
        Return GetCursorPos(pt)
    End Function

    Public Sub GetPosGui(ByRef pt As Point)
        GetCursorPos(pt)
    End Sub

    Public Function SetPosData(ptPos As Point) As Point
        Return New Point(ptPos.X - CInt(Math.Round(GetProgSize(TriggerType.AutoCast) / 2.0)),
                         ptPos.Y - GetProgSize(TriggerType.AutoCast, True) - 22)
    End Function

End Module

Public NotInheritable Class osFuncLib_Progress

    Public Shared progValue As Double = 0.0F

    Public Shared progDispMsg As String = "noStatus"
    Public Shared progShowMsg As Boolean = False

    Public Shared progContObj As Control

    Public Shared progSteps As Integer = 200

    Public Shared ProgTimeSpan As TimeSpan
    Public Shared ProgDuration As Integer
    Public Shared ProgInv As Double

    Public Shared progBlock_W As Single
    Public Shared progBlock_H As Integer = 25

    Public Shared progCurStatus As ProgStatus

    Public Shared progColor As Color
    Public Shared progColor_AutoCast As osColors.Color

    Public Shared progColorData As osColors.Color

    Public Shared ProgBrush_BG As SolidBrush
    Public Shared ProgBrush_Active As SolidBrush
    Public Shared ProgBrush_Border As Pen

    Public Shared pBrush_BG As New SolidColorBrush(osColors.Color.FromRgb(57, 57, 57))

    Public Shared pBrush_Active As SolidColorBrush
    Public Shared pBrush_Border As New osColors.Pen(osColors.Brushes.Black, 2)

    Public Shared ProgContainer As Rectangle
    Public Shared ProgContainerBorder As Rectangle

    Public Shared progFont_AC As New Font("Segoe UI", 9, FontStyle.Bold)
    Public Shared progFont_AP As New Font("Segoe UI", 10, FontStyle.Bold)

    Public Shared objAutoPassProg As SmoothProgressBarr = Nothing

    Public Shared ReadOnly ProgBG As New SolidColorBrush(osColors.Color.FromRgb(57, 57, 57))

    Private Shared ProgStatusColors As New Dictionary(Of ProgStatus, Color) From {
        {Idle, Color.White},
        {Running, Color.FromArgb(82, 96, 117)},
        {Success, Color.ForestGreen},
        {Fail, Color.Maroon},
        {StartAP, Color.Maroon}
    }

    Private Shared ReadOnly ProgColorIdx_AutoCast As New Dictionary(Of ProgStatus, osColors.Color) From {
        {Idle, osColors.Color.FromRgb(57, 57, 57)},
        {Running, osColors.Color.FromRgb(82, 96, 117)},
        {Success, osColors.Color.FromRgb(34, 139, 34)},
        {Fail, osColors.Color.FromRgb(97, 20, 20)}
    }

    Private Shared ReadOnly ProgColorIdx_AutoPass As New Dictionary(Of ProgStatus, osColors.Color) From {
        {Idle, osColors.Color.FromRgb(57, 57, 57)},
        {Running, osColors.Color.FromRgb(82, 96, 117)},
        {Success, osColors.Color.FromRgb(34, 139, 34)},
        {Fail, osColors.Color.FromRgb(97, 20, 20)},
        {StartAP, osColors.Color.FromRgb(82, 96, 117)}
    }

    Public Shared Sub SetProgContainer(pType As TriggerType)
        ProgContainer = New Rectangle(0, 0, GetProgSize(pType), GetProgSize(pType, True))
        ProgContainerBorder = New Rectangle(0, 0, GetProgSize(pType) - 1, GetProgSize(pType, True) - 1)
    End Sub

    Public Shared Sub SetProgState(newStatus As ProgStatus)
        progCurStatus = newStatus
        pBrush_BG = New SolidColorBrush()
    End Sub

    Public Shared Function ApplyProgState(setAction As ProgAction, Optional isAutoPass As Boolean = False) As ProgStatus
        Select Case setAction
            Case ResetProgress
                progCurStatus = Idle
            Case Activate
                progCurStatus = If(isAutoPass, StartAP,
                    Running)
            Case Complete
                progCurStatus = Success
            Case Abort
                progCurStatus = Fail
        End Select

        Return progCurStatus
    End Function

    Public Shared Function GetProgState() As ProgStatus
        Return progCurStatus
    End Function

    Public Shared Function IsProgRunning() As Boolean
        Return osFuncLib_Progress.GetProgState() = Running
    End Function

    Public Shared Function ShowProgFull() As Boolean
        Return progCurStatus = Success OrElse progCurStatus = Fail
    End Function

    Public Shared Function IsProgSuccess() As ProgStatus
        Return progCurStatus = Success
    End Function

    Public Shared Sub UpdateProgStatus(pType As TriggerAction, pAction As ProgAction)
        Dim isValAP = If(pType = TriggerAction.AutoPass, True, False)
        Dim getProgStatus = ApplyProgState(pAction, isValAP)

        ApplyProgColor(pType, getProgStatus)
    End Sub

    Private Shared Sub ApplyProgColor(pType As TriggerType, pStatus As ProgStatus)
        progColorData = FetchProgColor(pStatus, pType)

        Select Case pType
            Case TriggerType.AutoCast
                osHandler_GUI.osGui_AutoCast.Dispatcher.
                    Invoke(Sub()
                               osHandler_GUI.osGui_AutoCast.OddProgBar1.SetProgColor(progColorData)
                           End Sub)
            Case TriggerType.AutoPass
                osHandler_GUI.osGui_AutoPass.Dispatcher.
                    Invoke(Sub()
                               If pStatus = StartAP Then
                                   osHandler_GUI.osGui_AutoPass.OddProgBar_AP.SetProgress(1)
                               End If

                               osHandler_GUI.osGui_AutoPass.OddProgBar_AP.SetProgColor(progColorData)
                           End Sub)
        End Select
    End Sub

    Public Shared Function CalcTargetTime(sTime As Long, valDuration As Integer, repCnt As Integer, repRate As Double) As Long
        Return sTime + CLng(Math.Round(valDuration * repCnt * repRate))
    End Function

    Public Shared Function FetchProgColor(pStatus As ProgStatus) As Color
        Return ProgStatusColors(pStatus)
    End Function

    Public Shared Function FetchProgColor(pStatus As ProgStatus, pType As TriggerType) As osColors.Color
        Return If(pType = TriggerType.AutoCast, ProgColorIdx_AutoCast(pStatus), ProgColorIdx_AutoPass(pStatus))
    End Function

    Public Shared Function CalcPosData(ptPos As Point) As Point
        Return New Point(ptPos.X - CInt(Math.Round(GetProgSize(TriggerType.AutoCast) / 2.0)),
                         ptPos.Y - GetProgSize(TriggerType.AutoCast, True) - 22)
    End Function

    Public Shared Function CalcProgSize() As System.Drawing.Size
        Return New System.Drawing.Size(GetProgSize(TriggerType.AutoCast), GetProgSize(TriggerType.AutoCast, True))
    End Function

    Public Shared Function CalcProgSize(pType As TriggerType) As System.Drawing.Size
        Return New System.Drawing.Size(GetProgSize(pType), GetProgSize(pType, True))
    End Function

    Public Shared Sub SetProgBlockData(trigType As TriggerType)
        ProgDuration = If(trigType = TriggerType.AutoCast, GetFuse(), GetSafetyTimer())
        ProgTimeSpan = TimeSpan.FromMilliseconds(ProgDuration)
        ProgInv = 1.0 / ProgDuration
    End Sub

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
        If isRTC() Then
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
        If isRTC() Then
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

    Public Shared Async Function ExecuteAutoCast() As Task
        Dim isTask_AutoCast = PrepDispatcher().InvokeAsync(
            Async Function()
                GetPosGui(ptPos)

                osHandler_GUI.DisplayGUI(TriggerType.AutoCast, ptPos)
                osFuncLib_Progress.SetProgBlockData(TriggerType.AutoCast)
                'osHandler_GUI.DisplayGUI(TriggerType.AutoCast, ptPos)
                ' osFuncLib_Progress.SetProgBlockData(TriggerType.AutoCast)

                Dim retAC = Await osHandler_GUI.osGui_AutoCast.LaunchAutoCast()
                Return retAC
            End Function)

        Dim retProgResult = Await isTask_AutoCast.Task.Unwrap()

        Await ProcessResult(retProgResult)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Shared Async Function ProcessResult(acResult As ProgResult) As Task

        Dim procTask As DispatcherOperation(Of Task) = Nothing

        Select Case acResult
            Case ProgResult.Completed
                procTask = PrepDispatcher().InvokeAsync(
                    Async Function()
                        If isRTC() Then
                            ProcessProgressEvent(ProgMode.AutoCast, DispMsg, "Release To Cast")
                            Await InputMonSvc.AnticipateInput(InputAction.AC_RTC)

                            Await Task.Delay(100)
                        End If

                        ProcessProgressEvent(ProgMode.AutoCast, DispMsg, "Casting")

                        EngageAutoCast()
                    End Function)
            Case ProgResult.Cancelled
                procTask = PrepDispatcher().InvokeAsync(
                    Async Function()
                        ProcessProgressEvent(ProgMode.AutoCast, MaxFill)
                        ProcessProgressEvent(ProgMode.AutoCast, DispMsg, "Cancelled")

                        Await Task.Delay(10)
                    End Function)
        End Select

        Await procTask.Task.Unwrap()

        Await FinalizeAutoCast()
    End Function

    Private Shared Async Function FinalizeAutoCast() As Task
        Await Task.Delay(750)
        osHandler_GUI.osGui_AutoCast.Dispatcher.
            Invoke(Sub()
                       osHandler_GUI.ResetUI(TriggerAction.AutoCast, True)
                   End Sub)
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
        PrepUtilityTrigger(TriggerType.ShowPrefs)

        Application.Current.Dispatcher.Invoke(Sub()
                                                  With osHandler_GUI.osGui_Prefs
                                                      .Show()
                                                      .Focus()
                                                  End With
                                              End Sub)

        Await AnticipateExit(osHandler_GUI.osGui_Prefs)

        osHandler_GUI.ResetOptsUI()
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

        AddHandler osGuiPrefs.FormClosing, Sub(sender As Object, e As EventArgs)
                                               If osGuiPrefs.Visible Then
                                                   Return
                                               End If
                                               chkCloseSettings.TrySetResult(True)
                                           End Sub
    End Sub

    Private Shared Function AnticipateExit(guiPrefs As Form) As Task
        Dim tcs = New TaskCompletionSource(Of Object)(TaskCreationOptions.RunContinuationsAsynchronously)
        AddHandler guiPrefs.FormClosed, Sub(sender, e)
                                            tcs.TrySetResult(Nothing)
                                        End Sub
        Return tcs.Task
    End Function

End Class

Public NotInheritable Class osFuncLib_AutoPass

    Public Shared Async Sub InvokeAutoPass()
        SetGameFocus()
        Await Task.Delay(100)
        osHandler_Input.InjectAutoPassInputs()
    End Sub

    Public Shared Async Function ExecuteAutoPass() As Task
        Dim isTask_AutoPass = osHandler_GUI.osGui_AutoPass.
            Dispatcher.InvokeAsync(Async Function()
                                       osHandler_GUI.DisplayGUI(TriggerType.AutoPass)
                                       osFuncLib_Progress.SetProgBlockData(TriggerType.AutoPass)
                                       Await Task.Delay(50)

                                       Dim retAP = Await osHandler_GUI.osGui_AutoPass.LaunchAutoPass()
                                       Return retAP
                                   End Function)

        Dim retProgResult = Await isTask_AutoPass.Task.Unwrap()

        Await ProcessResult(retProgResult)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Shared Async Function AnticipateLaunchAP() As Task(Of Boolean)
        Dim chkExecAP = Await InputMonSvc.AnticipateInput(InputAction.AP_Exec)
        Return chkExecAP
    End Function

    Private Shared Async Function ProcessResult(acResult As ProgResult) As Task
        Try
            Select Case acResult
                Case ProgResult.Completed
                    ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "Release Shift To AutoPass | Press C To Cancel")
                    ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.MaxFill)

                    Dim chkLaunchAP = Await AnticipateLaunchAP()

                    If chkLaunchAP Then
                        ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPassing")
                        InvokeAutoPass()
                    Else
                        osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoPass, Abort)

                        ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.MaxFill)
                        ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
                    End If

                Case ProgResult.Cancelled
                    osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoPass, Abort)

                    ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.MaxFill)
                    ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
            End Select

            Await FinalizeAutoPass()
        Catch ex As Exception
        End Try
    End Function

    Private Shared Async Function FinalizeAutoPass() As Task
        Await Task.Delay(750)
        osHandler_GUI.osGui_AutoPass.Dispatcher.Invoke(Sub()
                                                           osHandler_GUI.ResetUI(TriggerAction.AutoPass, True)
                                                       End Sub)
    End Function

End Class

Public Class MenuHostWindow
    Inherits Window

    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80

    Private Const WM_MOUSEACTIVATE As Integer = &H21
    Private Const MA_NOACTIVATE As Integer = 3

    <DllImport("user32.dll", EntryPoint:="GetWindowLongW", SetLastError:=True)>
    Private Shared Function GetWindowLong32(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="SetWindowLongW", SetLastError:=True)>
    Private Shared Function SetWindowLong32(hWnd As IntPtr, nIndex As Integer, dwNewLong As Integer) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="GetWindowLongPtrW", SetLastError:=True)>
    Private Shared Function GetWindowLongPtr64(hWnd As IntPtr, nIndex As Integer) As IntPtr
    End Function

    <DllImport("user32.dll", EntryPoint:="SetWindowLongPtrW", SetLastError:=True)>
    Private Shared Function SetWindowLongPtr64(hWnd As IntPtr, nIndex As Integer, dwNewLong As IntPtr) As IntPtr
    End Function

    Private Shared Function GetWindowLongPtr(hWnd As IntPtr, nIndex As Integer) As IntPtr
        If IntPtr.Size = 8 Then
            Return GetWindowLongPtr64(hWnd, nIndex)
        Else
            Return New IntPtr(GetWindowLong32(hWnd, nIndex))
        End If
    End Function

    Private Shared Function SetWindowLongPtr(hWnd As IntPtr, nIndex As Integer, dwNewLong As IntPtr) As IntPtr
        If IntPtr.Size = 8 Then
            Return SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
        Else
            Return New IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()))
        End If
    End Function

    Public Sub New()
        WindowStyle = WindowStyle.None
        AllowsTransparency = True
        ShowInTaskbar = False
        Opacity = 0.0
        Width = 1
        Height = 1
        Topmost = True
        ShowActivated = False
    End Sub

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)

        If System.ComponentModel.DesignerProperties.GetIsInDesignMode(Me) Then Return

        Dim objHwnd = New Interop.WindowInteropHelper(Me).Handle

        Dim ex = GetWindowLongPtr(objHwnd, GWL_EXSTYLE)
        Dim newEx As Integer = ex.ToInt32() Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
        SetWindowLongPtr(objHwnd, GWL_EXSTYLE, New IntPtr(newEx))

        Dim objHwndSrc = Interop.HwndSource.FromHwnd(objHwnd)
        If objHwndSrc IsNot Nothing Then
            objHwndSrc.AddHook(New Interop.HwndSourceHook(AddressOf WndProcHook))
        End If
    End Sub

    Private Function WndProcHook(hwnd As IntPtr, msg As Integer, wParam As IntPtr,
                                 lParam As IntPtr, ByRef handled As Boolean) As IntPtr
        If msg = WM_MOUSEACTIVATE Then
            handled = True
            Return New IntPtr(MA_NOACTIVATE)
        End If
        Return IntPtr.Zero
    End Function

End Class

Public Class MenuOverlayWindow
    Inherits Window

    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80

    <DllImport("user32.dll", EntryPoint:="GetWindowLongPtrW", SetLastError:=True)>
    Private Shared Function GetWindowLongPtr(hWnd As IntPtr, nIndex As Integer) As IntPtr
    End Function
    <DllImport("user32.dll", EntryPoint:="SetWindowLongPtrW", SetLastError:=True)>
    Private Shared Function SetWindowLongPtr(hWnd As IntPtr, nIndex As Integer, dwNewLong As IntPtr) As IntPtr
    End Function

    Public Sub New()
        WindowStyle = WindowStyle.None
        AllowsTransparency = True
        Background = Windows.Media.Brushes.Black       ' Transparent but still hit-testable
        ShowInTaskbar = False
        Topmost = True
        ShowActivated = False                  ' don't activate
        Opacity = 0.01                           ' fully transparent but clickable
    End Sub

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        Dim hwnd = New Interop.WindowInteropHelper(Me).Handle
        Dim ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64()
        ex = ex Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, New IntPtr(ex))
    End Sub
End Class

Module osFuncLib_TrayMenu

    Private _inputSub As InputManager

    Private Property osIsEnabled As Boolean

    Private objInputMon As osInMon

    Private chkMenuOpen As TaskCompletionSource(Of Boolean)

    Private MenuCloseClkMon As ObserveMenuCloseClick

    Private objMenuHost As MenuHostWindow

    Private osMenuObj As osControls.ContextMenu

    Private osMenuOverlay As MenuOverlayWindow

    Private osMenu_EnDis As osControls.MenuItem
    Private osMenu_GameOpts As osControls.MenuItem
    Private osGameMenu_Play As osControls.MenuItem
    Private osGameMenu_Leave As osControls.MenuItem
    Private osMenu_Opts As osControls.MenuItem
    Private osMenuExit As osControls.MenuItem

    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000

    <DllImport("user32.dll", SetLastError:=True)>
    Private Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As Integer) As Integer
    End Function

    Public Async Function DisplayMenuPopup() As Task
        PrepUtilityTrigger(TriggerType.ShowMenu)

        Dim objGetMenu = osPopupMenu

        Await Application.Current.Dispatcher.InvokeAsync(
            Sub()
                osMenuOverlay = New MenuOverlayWindow()

                With osMenuOverlay
                    Dim vr = SystemInformation.VirtualScreen
                    .Left = vr.Left
                    .Top = vr.Top
                    .Width = vr.Width
                    .Height = vr.Height

                    AddHandler .MouseDown, Sub() ClosePopupMenu()
                    AddHandler .MouseWheel, Sub() ClosePopupMenu()

                    .Show()
                End With

                GenMenuHost()

                With objGetMenu
                    .PlacementTarget = objMenuHost
                    .Placement = osControls.Primitives.PlacementMode.MousePoint
                    .StaysOpen = False
                    .IsOpen = True
                End With

                SetNoActivateStyleForContextMenu(osMenuObj)

            End Sub)

        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Sub ClosePopupMenu()

        osPopupMenu.IsOpen = False

        If osMenuOverlay IsNot Nothing Then
            osMenuOverlay.Close()
            osMenuOverlay = Nothing
        End If

        Dim doGameFocus = SetGameFocus()
    End Sub

    Private Sub CloseMenuHost()
        Try
            objMenuHost.Close()
        Catch ex As Exception

        End Try

        If objMenuHost IsNot Nothing Then
            objMenuHost = Nothing
        End If
    End Sub

    Private Sub GenMenuHost()
        If objMenuHost IsNot Nothing Then
            objMenuHost = Nothing
        End If

        objMenuHost = New MenuHostWindow()

        With objMenuHost
            Dim p = Control.MousePosition
            .Left = p.X
            .Top = p.Y

            .Show()
        End With
    End Sub

    Public Function GetEnabledStatus() As Boolean
        Return osIsEnabled
        '   osTrayIcon.
    End Function

    Public Function IsDisabled() As Boolean
        Return osIsEnabled = False
    End Function

    Private Function ConfirmStatusChange(newStatus As Boolean) As UpdateStatus
        If newStatus = False Then
            Select Case MsgBox("Disable osAutoCast?", vbYesNo,
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

    Public Sub VerifyStatusChange(setStatus As Boolean)
        Dim result = ConfirmStatusChange(setStatus)
        If result = UpdateStatus.CancelUpdate Then Return

        SetNewStatus(setStatus)
    End Sub

    Private Sub SetNewStatus(setStatus As Boolean)
        osIsEnabled = setStatus
    End Sub

    Private Sub UpdateTrayIcon(chkStatus As Boolean)
        If chkStatus Then
            osTrayIcon.Icon = My.Resources.osIcon
            osTrayIcon.Text = "osAutoCast | Enabled"
        Else
            osTrayIcon.Icon = My.Resources.osIcon_Disabled
            osTrayIcon.Text = "osAutoCast | Disabled"
        End If
    End Sub

    Private Sub UpdateTrayText(isEnabled As Boolean)
        osTrayIcon.Text = If(isEnabled, "osAutoCast | Enabled", "osAutoCast | Disabled")
    End Sub

    Public Sub UpdateTray(isEnabled As Boolean)
        UpdateTrayIcon(isEnabled)
        UpdateTrayText(isEnabled)
    End Sub

    Private Sub PrepTrayMenu(objOsMenu As osControls.ContextMenu)
        osTrayIcon = New NotifyIcon With {
            .Icon = My.Resources.osIcon,
            .Text = "osAutoCast | Enabled",
            .Visible = True
        }

        AddHandler osTrayIcon.MouseUp,
            Async Sub(sender As Object, e As MouseEventArgs)
                If e.Button = osForms.MouseButtons.Right Then
                    Await DisplayMenuPopup()
                End If
            End Sub
    End Sub

    Private Function CreateMenuItem(menuHeader As String) As osControls.MenuItem
        Return New osControls.MenuItem With {.Header = menuHeader}
    End Function

    Private Sub CreateMenuItem(menuHeader As String, ByRef objMenu As osControls.MenuItem, Optional isEnable As Boolean = False)
        If isEnable Then
            objMenu = New osControls.MenuItem With {
                .Header = "Enabled",
                .IsCheckable = True,
                .IsChecked = True,
                .Name = "osMenuEnDis"
            }
        Else
            objMenu = New osControls.MenuItem With {
                .Header = menuHeader
            }
        End If
    End Sub

    Private Sub EstablishPopupMenu(ByRef objPopupMenu As osControls.ContextMenu)
        objPopupMenu = New osControls.ContextMenu With {
            .FontFamily = New osColors.FontFamily("Trebuchet MS"),
            .FontSize = 14
        }
    End Sub

    Private Sub PopulatePopupMenu()
        osMenu_GameOpts.Items.Add(osGameMenu_Play)
        osMenu_GameOpts.Items.Add(osGameMenu_Leave)

        With osMenuObj
            .Items.Add(osMenu_EnDis)
            .Items.Add(New Separator())
            .Items.Add(osMenu_GameOpts)
            .Items.Add(New Separator())
            .Items.Add(osMenu_Opts)
            .Items.Add(osMenuExit)
        End With
    End Sub

    Private Sub SetMenuHandlers()

        AddHandler osMenu_Opts.Click,
            Async Sub()
                osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
                Await osFuncLib_ShowOpts.ExecuteDispOpts()

                ClosePopupMenu()
            End Sub

        AddHandler osMenuExit.Click,
            Sub()
                Dim chkConfirmExit = GetResponse(PromptType.CloseApp)
                If chkConfirmExit = DialogResult.No Then Exit Sub

                ClosePopupMenu()
                osStopApp()
            End Sub

        AddHandler osGameMenu_Play.Click,
            Sub()
                Dim procStart_MTGA As New ProcessStartInfo With {
                    .FileName = dirMtgaExe,
                    .WorkingDirectory = dirMtga,
                    .WindowStyle = ProcessWindowStyle.Maximized
                }

                Process.Start(procStart_MTGA)
                ClosePopupMenu()
            End Sub

        AddHandler osGameMenu_Leave.Click,
            Sub()
                Dim chkConfirmCloseGame = GetResponse(PromptType.GameMenu_Leave)

                If chkConfirmCloseGame = DialogResult.Yes Then
                    Dim cmdCloseMTGA = CmdRunner.RunCmd("taskkill", "/f /im MTGA.exe")
                End If

                ClosePopupMenu()
            End Sub

        AddHandler osMenuObj.Closed, Sub()
                                         CloseMenuHost()
                                     End Sub

    End Sub

    Private Sub PopulateMenu_Popup(ByRef objSetMenu As osControls.ContextMenu)
        EstablishPopupMenu(osMenuObj)

        CreateMenuItem("Enabled", osMenu_EnDis, True)
        CreateMenuItem("MTG Menu", osMenu_GameOpts)
        CreateMenuItem("Play Game", osGameMenu_Play)
        CreateMenuItem("Leave Game", osGameMenu_Leave)
        CreateMenuItem("Preferences", osMenu_Opts)
        CreateMenuItem("Exit", osMenuExit)

        PopulatePopupMenu()

        SetMenuHandlers()

        objSetMenu = osMenuObj
    End Sub

    Private Sub osStopApp()
        osTrayIcon.Visible = False
        End
    End Sub

    Public Sub osMenu_Init(objInMon As osInMon)

        PopulateMenu_Popup(osPopupMenu)
        PrepTrayMenu(osPopupMenu)

        osIsEnabled = True
        objInputMon = objInMon

        With New osMenuFuncData(AddressOf GetEnabledStatus, AddressOf VerifyStatusChange)
            osMenuFuncBinder.BindChecked_Popup(osPopupMenu.Items.Item(0),
                                         .osMenuFunc_GetStatus, .osMenuFunc_ApplyStatus)
        End With

    End Sub

    Private Sub SetNoActivateStyleForContextMenu(cm As osControls.ContextMenu)
        Dim src = TryCast(PresentationSource.FromVisual(cm), Interop.HwndSource)
        If src Is Nothing Then Return
        Dim h = src.Handle
        Dim ex = GetWindowLong(h, GWL_EXSTYLE)
        SetWindowLong(h, GWL_EXSTYLE, ex Or WS_EX_NOACTIVATE)
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

    Public Function PrepDispatcher(Optional IsAutoPass As Boolean = False) As Dispatcher
        Return If(IsAutoPass, osHandler_GUI.osGui_AutoPass.Dispatcher,
            osHandler_GUI.osGui_AutoCast.Dispatcher)
    End Function

    Public Function GetResponse(pType As PromptType) As DialogResult
        With New PromptData(pType)
            Dim chkPromptResponse = ResponseBox.DisplayPopup(.Msg, .Title, .MsgType)
            Return chkPromptResponse
        End With
    End Function

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

    Public Function EaseInOutExpo(pDuration As Double) As Double
        If pDuration = 0.0 Then Return 0.0
        If pDuration = 1.0 Then Return 1.0
        Return If(pDuration < 0.5, Math.Pow(2, 20 * pDuration - 10) / 2,
            (2 - Math.Pow(2, -20 * pDuration + 10)) / 2)
    End Function

    Public Function EaseProgress(pDuration As Double) As Double
        If pDuration <= 0 Then Return 0
        If pDuration >= 1 Then Return 1

        Dim valDuration As Double = pDuration

        For i As Integer = 0 To 4
            Dim x As Double = CalcEase(valDuration, 0.0, eVal_x1, eVal_x2, 1.0)
            Dim dx As Double = CalcEaseSupport(valDuration, 0.0, eVal_x1, eVal_x2, 1.0)

            If dx = 0 Then Exit For

            valDuration -= (x - pDuration) / dx
            valDuration = Math.Max(0, Math.Min(1, valDuration))
        Next

        Return CalcEase(valDuration, 0.0, eVal_y1, eVal_y2, 1.0)
    End Function

    Public Function EaseInOutCirc(x As Double) As Double
        If x < 0.5 Then
            Return (1.0 - Math.Sqrt(1.0 - Math.Pow(2.0 * x, 2))) / 2.0
        Else
            Return (Math.Sqrt(1.0 - Math.Pow(-2.0 * x + 2.0, 2)) + 1.0) / 2.0
        End If
    End Function

    Public Function EaseCustom(t As Double) As Double
        ' 1) Clamp input
        t = Math.Max(0.0, Math.Min(1.0, t))

        ' 2) First 10% linear
        Const threshold As Double = 0.15
        If t < threshold Then
            Return t
        End If

        ' 3) Map the rest [0.1…1] → [0…1]
        Dim u As Double = (t - threshold) / (1.0 - threshold)

        ' 4) Sine ease-in/out: slow start & slow end
        '    y_sine = -(cos(π·u) - 1) / 2
        Dim ySine As Double = -(Math.Cos(Math.PI * u) - 1) / 2

        ' 5) Scale back into [0.1…1]
        Return threshold + ySine * (1.0 - threshold)
    End Function

    Public Function EaseLinearThenExpoIn(t As Double,
                                     Optional threshold As Double = 0.15,
                                     Optional k As Double = 5.7) As Double

        ' Clamp input
        t = Math.Max(0.0, Math.Min(1.0, t))

        ' 1) First segment: pure linear [0 … threshold]
        If t < threshold Then
            Return t
        End If

        ' 2) Remap t from [threshold…1] → u ∈ [0…1]
        Dim u As Double = (t - threshold) / (1.0 - threshold)

        ' 3) Exponential ease-in: y ∈ [0…1]
        Dim yExp As Double
        If u <= 0.0 Then
            yExp = 0.0
        ElseIf u >= 1.0 Then
            yExp = 1.0
        Else
            ' Classic ease-in expo: starts very slowly, then accelerates
            yExp = Math.Pow(2, k * (u - 1.0))
        End If

        ' 4) Scale back into [threshold…1]
        Return threshold + yExp * (1.0 - threshold)
    End Function

    Private Function ConvDur(pDuration As Double, cntEval As Integer) As Double
        Return (1 - pDuration) * cntEval
    End Function

    Private Function CalcEase(t As Double, p0 As Double, p1 As Double, p2 As Double, p3 As Double) As Double
        Dim mt As Double = 1 - t
        Return mt * mt * mt * p0 +
               3 * mt * mt * t * p1 +
               3 * mt * t * t * p2 +
               t * t * t * p3
    End Function

    Private Function CalcEaseSupport(t As Double, p0 As Double, p1 As Double, p2 As Double, p3 As Double) As Double
        Dim mt As Double = 1 - t
        Return 3 * mt * mt * (p1 - p0) +
               6 * mt * t * (p2 - p1) +
               3 * t * t * (p3 - p2)
    End Function


End Module

'Public Class BoolToEnabledTextConverter
'    Implements IValueConverter

'    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
'        Implements IValueConverter.Convert
'        Dim b = False
'        If value IsNot Nothing Then b = System.Convert.ToBoolean(value)
'        Return If(b, "Enabled", "Disabled")
'    End Function

'    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
'        Implements IValueConverter.ConvertBack
'        ' Not used
'        Throw New NotSupportedException()
'    End Function
'End Class

Public Class isEnabledConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return If(System.Convert.ToBoolean(value), "Enabled", "Disabled")
    End Function
    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

Public Class MenuFuncAdapter
    Implements System.ComponentModel.INotifyPropertyChanged

    Private ReadOnly _get As Func(Of Boolean)
    Private ReadOnly _set As Action(Of Boolean)

    Public Sub New(getter As Func(Of Boolean), setter As Action(Of Boolean))
        _get = getter
        _set = setter
    End Sub

    Public Property Value As Boolean
        Get
            Return _get()
        End Get
        Set(v As Boolean)
            _set?.Invoke(v)
            RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(NameOf(Value)))
        End Set
    End Property

    ' Call this if the underlying state changes elsewhere and you want the UI to refresh
    Public Sub Refresh()
        RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(NameOf(Value)))
    End Sub

    Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler _
        Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
End Class

Public Class TrayIconBridge
    Inherits DependencyObject
    Public Property NotifyIcon As NotifyIcon

    Public Shared ReadOnly IsEnabledProperty As DependencyProperty =
        DependencyProperty.Register(
            NameOf(IsEnabled),
            GetType(Boolean),
            GetType(TrayIconBridge),
            New PropertyMetadata(True, AddressOf OnIsEnabledChanged))

    Public Property IsEnabled As Boolean
        Get
            Return CBool(GetValue(IsEnabledProperty))
        End Get
        Set(value As Boolean)
            SetValue(IsEnabledProperty, value)
        End Set
    End Property

    Private Shared Sub OnIsEnabledChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim br = DirectCast(d, TrayIconBridge)
        If br.NotifyIcon Is Nothing Then Exit Sub
        Dim onOff = CBool(e.NewValue)
        br.NotifyIcon.Icon = If(onOff, My.Resources.osIcon, My.Resources.osIcon_Disabled)
        br.NotifyIcon.Text = If(onOff, "osAutoCast (Enabled)", "osAutoCast (Disabled)")
        br.NotifyIcon.Visible = True
    End Sub
End Class

Public Class osMenuFuncBinder

    Public Enum UpdateStatus
        ToEnabled
        ToDisabled
        CancelUpdate
    End Enum

    Private Enum MenuBinderType
        isChk
        isMenu
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

    Private Shared Sub ApplyMenuBinding(MenuItemObj As DependencyObject, MenuItemBinder As osBinder.Binding, BinderType As MenuBinderType)
        Select Case BinderType
            Case MenuBinderType.isChk
                BindingOperations.SetBinding(MenuItemObj, osControls.MenuItem.IsCheckedProperty, MenuItemBinder)
            Case MenuBinderType.isMenu
                BindingOperations.SetBinding(MenuItemObj, HeaderedItemsControl.HeaderProperty, MenuItemBinder)
        End Select
    End Sub

    Private Shared Function GenMenuBinding(MenuBindSrc As Object, BinderType As MenuBinderType) As osBinder.Binding
        Select Case BinderType
            Case MenuBinderType.isChk
                Return New osBinder.Binding("Value") With {
                    .Mode = BindingMode.TwoWay,
                    .UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    .Source = MenuBindSrc
                }
            Case MenuBinderType.isMenu
                Dim osConvEnabled = New isEnabledConverter()

                Return New osBinder.Binding("Value") With {
                    .Mode = BindingMode.OneWay,
                    .Source = MenuBindSrc,
                    .Converter = osConvEnabled
                }
            Case Else
                Return Nothing
        End Select
    End Function

    Private Shared Function GenTrayBinding(TrayBindSrc As Object) As osBinder.Binding
        Return New osBinder.Binding("Value") With {
            .Mode = BindingMode.OneWay,
            .Source = TrayBindSrc
        }
    End Function

    Private Shared Sub ApplyTrayBinding(TrayMenuObj As DependencyObject, TrayMenuBinder As osBinder.Binding)
        BindingOperations.SetBinding(TrayMenuObj, TrayIconBridge.IsEnabledProperty, TrayMenuBinder)
    End Sub

    Public Shared Sub BindChecked_Popup(objMenuItem As osControls.MenuItem,
                                        DoFunc_FetchStatus As Func(Of Boolean),
                                        DoFunc_ConfirmStatus As Action(Of Boolean))

        Dim osMenuAdapter = New MenuFuncAdapter(DoFunc_FetchStatus, DoFunc_ConfirmStatus)

        Dim osBinder_ChkEnabled = GenMenuBinding(osMenuAdapter, MenuBinderType.isChk)
        ApplyMenuBinding(objMenuItem, osBinder_ChkEnabled, MenuBinderType.isChk)

        Dim osBinder_MenuText = GenMenuBinding(osMenuAdapter, MenuBinderType.isMenu)
        ApplyMenuBinding(objMenuItem, osBinder_MenuText, MenuBinderType.isMenu)

        AddHandler osMenuAdapter.PropertyChanged, Sub(sender, e)
                                                      If e.PropertyName = NameOf(osMenuAdapter.Value) Then
                                                          Dim isEnabled As Boolean = osMenuAdapter.Value
                                                          UpdateTray(isEnabled)
                                                      End If
                                                  End Sub


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

    <Runtime.CompilerServices.Extension>
    Public Function FreezeReturn(Of T As Freezable)(item As T) As T
        If item.CanFreeze Then item.Freeze()
        Return item
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

Public Module CmdRunner

    Public Function RunCmd(cmd As String,
                           Optional arguments As String = "",
                           Optional timeoutMs As Integer = 30000,
                           Optional workingDir As String = Nothing,
                           Optional forceUtf8 As Boolean = True) _
                           As (ExitCode As Integer, StdOut As String, StdErr As String)

        Dim outSb As New StringBuilder()
        Dim errSb As New StringBuilder()

        ' Build the full command line that cmd.exe will execute
        ' Example final: /c chcp 65001 & ipconfig /all
        Dim inner As New StringBuilder()
        inner.Append("/c ")
        If forceUtf8 Then inner.Append("chcp 65001 >nul & ") ' make stdout UTF-8 to avoid mojibake
        inner.Append(cmd)
        If Not String.IsNullOrWhiteSpace(arguments) Then
            inner.Append(" "c).Append(arguments)
        End If

        Dim psi As New ProcessStartInfo() With {
            .FileName = "cmd.exe",
            .Arguments = inner.ToString(),
            .UseShellExecute = False,            ' must be False to redirect
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .CreateNoWindow = True,
            .StandardOutputEncoding = Encoding.UTF8,
            .StandardErrorEncoding = Encoding.UTF8
        }
        If Not String.IsNullOrWhiteSpace(workingDir) Then psi.WorkingDirectory = workingDir

        Dim p As New Process()
        p.StartInfo = psi

        ' Async read to avoid deadlocks
        AddHandler p.OutputDataReceived, Sub(sender, e)
                                             If e.Data IsNot Nothing Then outSb.AppendLine(e.Data)
                                         End Sub
        AddHandler p.ErrorDataReceived, Sub(sender, e)
                                            If e.Data IsNot Nothing Then errSb.AppendLine(e.Data)
                                        End Sub

        p.Start()
        p.BeginOutputReadLine()
        p.BeginErrorReadLine()

        Dim exited As Boolean = p.WaitForExit(timeoutMs)
        If Not exited Then
            Try : p.Kill() : Catch : End Try
            errSb.AppendLine($"Timed out after {timeoutMs} ms")
        Else
            ' Ensure async handlers flush
            p.WaitForExit()
        End If

        Dim code As Integer = If(exited, p.ExitCode, -1)
        Return (code, outSb.ToString().TrimEnd(), errSb.ToString().TrimEnd())
    End Function
End Module



Module CloneHelpers

    Public Function CloneElement(Of T As FrameworkElement)(guiXaml As T) As T
        Dim xamlProGui As String = XamlWriter.Save(guiXaml)

        Using objReader As New System.IO.StringReader(xamlProGui),
            objXamlReader As XmlReader = XmlReader.Create(objReader)
            Return CType(XamlReader.Load(objXamlReader), T)
        End Using

    End Function

End Module

#Enable Warning IDE0060 ' Remove unused parame
#Enable Warning IDE1006 ' Remove unused parameterter
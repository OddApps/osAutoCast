Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Reactive.Linq
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.CoreDataLib
Imports osAutoCast.DataTypeLib.AnimationType
Imports osAutoCast.DataTypeLib.AnimationVisual
Imports osAutoCast.DataTypeLib.OverlayVisualType
Imports osAutoCast.DataTypeLib.PopupCloseAction
Imports osAutoCast.DataTypeLib.ProgAction
Imports osAutoCast.DataTypeLib.ProgEvent
Imports osAutoCast.DataTypeLib.ProgressMode
Imports osAutoCast.DataTypeLib.ProgStatus
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.DataTypeLib.TriggerAction
Imports osAutoCast.DataTypeLib.UpdateStatusAction
Imports osBinder = System.Windows.Data
Imports osBrushColor = System.Windows.Media.Brushes
Imports osColors = System.Windows.Media
Imports osWinControls = System.Windows.Controls
Imports osHorz = System.Windows.HorizontalAlignment
Imports osPoint = System.Windows.Point
Imports osSize = System.Windows.Size
Imports osProgColor = SharpDX.Mathematics.Interop.RawColor4
Imports osRect = SharpDX.Mathematics.Interop
Imports osSweep = System.Windows.Media.SweepDirection
Imports osUtilities = SharpDX.Utilities
Imports osVert = System.Windows.VerticalAlignment
Imports pxShader_Pixel = SharpDX.Direct3D11.PixelShader
Imports pxShader_Vertex = SharpDX.Direct3D11.VertexShader

#Disable Warning IDE0060 ' Remove unused parameter
#Disable Warning IDE1006 ' Remove unused parameter
#Disable Warning BC42353
#Disable Warning BC42107
#Disable Warning BC42104

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
    Public Shared ProgWidthInv As Single

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

    Public Shared ReadOnly ProgBG As New SolidColorBrush(osColors.Color.FromRgb(57, 57, 57))

    Public Shared TextColorARGB As osRect.RawColor4 = New osRect.RawColor4(1.0F, 1.0F, 1.0F, 1.0F)

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

    Public Shared Sub UpdateProgStatus(pType As TriggerAction, pAction As ProgAction, Optional pUpdate As Boolean = False)
        Dim isValAP = If(pType = TriggerAutoPass, True, False)
        Dim getProgStatus = ApplyProgState(pAction, isValAP)

        ApplyProgColor(pType, getProgStatus, pUpdate)
    End Sub

    Private Shared Sub ApplyProgColor(pType As TriggerType, pStatus As ProgStatus, Optional pUpdate As Boolean = False)
        progColorData = FetchProgColor(pStatus, pType)

        Select Case pType
            Case TriggerType.AutoCast
                Application.Current.
                    Dispatcher.Invoke(
                        Sub()
                            osHandler_UI.osGui_AutoCastProgress.SetProgColor(progColorData, pUpdate)
                        End Sub)
            Case TriggerType.AutoPass
                osHandler_UI.osGui_AutoPass2.
                    Dispatcher.Invoke(
                    Sub()
                        If pStatus = StartAP Then
                            osHandler_UI.osGui_AutoPass2.apHandler._UpdateProgress(0)
                        End If

                        osHandler_UI.osGui_AutoPass2.apHandler._UpdateColorFunc(progColorData)
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

    Public Shared Function RetProgColor(pStatus As ProgStatus, pType As TriggerType) As osProgColor
        With FetchProgColor(pStatus, pType)
            Return New osProgColor(CalcRGB(.R),
                                   CalcRGB(.G),
                                   CalcRGB(.B),
                                   1.0F)
        End With
    End Function

    Private Shared Function CalcRGB(cVal As Byte) As Single
        Return cVal / 255.0F
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
        ProgWidthInv = 1.0 / CalcProgSize(DataTypeLib.TriggerType.AutoCast).Width
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

                osFuncLib_Progress.SetProgBlockData(TriggerType.AutoCast)
                osHandler_UI.DisplayGUI(TriggerType.AutoCast, ptPos)

                Dim retAC = Await osHandler_UI.osGui_AutoCastProgress.LaunchAutoCast()
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
                            ProcessProgressEvent(ProgMode_AutoCast, ShowFullMsg, "Release To Cast")
                            Await InputMonSvc.AnticipateInput(InputAction.AC_RTC)

                            Await Task.Delay(100)
                        End If

                        ProcessProgressEvent(ProgMode_AutoCast, ShowFullMsg, "Casting")

                        EngageAutoCast()
                    End Function)
            Case ProgResult.Cancelled
                procTask = PrepDispatcher().InvokeAsync(
                    Async Function()
                        ProcessProgressEvent(ProgMode_AutoCast, ShowFullMsg, "Cancelled")

                        Await Task.Delay(10)
                    End Function)
        End Select

        Await procTask.Task.Unwrap()

        Await FinalizeAutoCast()
    End Function

    Private Shared Async Function FinalizeAutoCast() As Task
        Await Task.Delay(750)
        PrepDispatcher().
            Invoke(Sub()
                       osHandler_UI.ResetUI(TriggerAutoCast)
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

        chkCloseSettings.ResetAndInitTask()

        Await osHandler_UI.ShowPrefsUI(chkCloseSettings, True)
        Await AnticipateExit()

        Await osHandler_UI.ResetOptsUI(True)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    Public Shared Async Function ExecuteDispOpts(fromTray As Boolean) As Task
        PrepUtilityTrigger(TriggerType.ShowPrefs)

        chkCloseSettings.ResetAndInitTask()

        Await osHandler_UI.ShowPrefsUI(chkCloseSettings, True)
        'Await osHandler_UI.ShowPrefsUI(chkCloseSettings)
        Await AnticipateExit()

        Await osHandler_UI.ResetOptsUI(True)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Shared Function AnticipateExit() As Task
        Return chkCloseSettings.Task
    End Function

End Class

Public NotInheritable Class osFuncLib_AutoPass

    Public Shared Async Sub InvokeAutoPass()
        SetGameFocus()
        Await Task.Delay(100)
        osHandler_Input.InjectAutoPassInputs()
    End Sub

    Public Shared Async Function ExecuteAutoPass() As Task
        Dim isTask_AutoPass = osHandler_UI.osGui_AutoPass.
            Dispatcher.InvokeAsync(Async Function()
                                       osHandler_UI.DisplayGUI(TriggerType.AutoPass)
                                       osFuncLib_Progress.SetProgBlockData(TriggerType.AutoPass)
                                       Await Task.Delay(50)

                                       Dim retAP = Await osHandler_UI.osGui_AutoPass.LaunchAutoPass()
                                       Return retAP
                                   End Function)

        Dim retProgResult = Await isTask_AutoPass.Task.Unwrap()

        Await ProcessResult(retProgResult)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    Public Shared Async Function ExecuteAutoPass(isNew As Boolean) As Task
        Dim isTask_AutoPass = osHandler_UI.
            osGui_AutoPass2.Dispatcher.InvokeAsync(
                Async Function()

                    osHandler_UI.DisplayGUI(TriggerType.AutoPass)
                    osFuncLib_Progress.SetProgBlockData(TriggerType.AutoPass)
                    Await Task.Delay(50)

                    Dim retAP = Await osHandler_UI.osGui_AutoPass2.LaunchAutoPass()
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
                    ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.DispMsg, "Release Shift To AutoPass | Press C To Cancel")
                    ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.MaxFill)

                    Dim chkLaunchAP = Await AnticipateLaunchAP()

                    If chkLaunchAP Then
                        ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.DispMsg, "AutoPassing")
                        InvokeAutoPass()
                    Else
                        osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, Abort)

                        ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.MaxFill)
                        ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
                    End If

                Case ProgResult.Cancelled
                    osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, Abort)

                    ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.MaxFill)
                    ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
            End Select

            Await FinalizeAutoPass()
        Catch ex As Exception
        End Try
    End Function

    Private Shared Async Function FinalizeAutoPass() As Task
        Await Task.Delay(750)
        osHandler_UI.osGui_AutoPass.Dispatcher.
            Invoke(Sub()
                       osHandler_UI.ResetUI(TriggerAutoPass, True)
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

Public NotInheritable Class MenuOverlayWindow
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

    Private objTask_Open As TaskCompletionSource(Of Boolean)
    Private objTask_Close As TaskCompletionSource(Of Boolean)

    Private objAnimation_Open As Storyboard = Nothing
    Private objAnimation_Close As Storyboard = Nothing

    Private OpenCompleteEvent As EventHandler = AddressOf OpenComplete

    Private isFromTray As Boolean

    Private OverlayOpacity_Tray As Double = 0.01
    Private OverlayOpacity_Popup As Double = 0.0

    Private VisualDataLocation As String = "/DataFiles/VisualData/StyleLib/StyleResources/StyleConfigs/osUI_StyleVisuals.xaml"
    Private VisualDataURI As System.Uri = New System.Uri(VisualDataLocation, System.UriKind.Relative)

    Private objScreenData As Rectangle = SystemInformation.VirtualScreen

    Private idxOverlayVisuals As New Dictionary(Of OverlayVisualType, String) From {
        {OverlayVisual_Open, "OverlayVisual_Open"},
        {OverlayVisual_Close, "OverlayVisual_Close"},
        {OverlayVisual_CloseByBtn, "OverlayVisual_CloseByBtn"},
        {OverlayVisual_CloseByCmd, "OverlayVisual_CloseByCmd"}
    }

    Public hasClosedClicked As Boolean = False

    Public Sub New(Optional isTray As Boolean = False)
        With Me
            .isFromTray = isTray

            .Title = ""
            .WindowStyle = WindowStyle.None
            .AllowsTransparency = True
            .ShowInTaskbar = False
            .ShowActivated = False
            .Topmost = True
            .Focusable = False
        End With

        SetBG()
    End Sub

    Private Sub SetVisualMode(objAniType As AnimationType)
        Dim setBitMapMode As BitmapScalingMode
        Dim setCacheMode As CacheMode

        Select Case objAniType
            Case aniOpen
                setBitMapMode = BitmapScalingMode.HighQuality
                setCacheMode = Nothing
            Case aniClose
                setBitMapMode = BitmapScalingMode.LowQuality
                setCacheMode = New BitmapCache()
        End Select

        Me.CacheMode = setCacheMode
        RenderOptions.SetBitmapScalingMode(Me, setBitMapMode)
    End Sub

    Public Sub ApplyTrayConfig()
        With Me
            .isFromTray = True
            .Style = Nothing

            If .Resources.MergedDictionaries.Count > 0 Then
                .Resources.
                    MergedDictionaries.RemoveAt(0)
            End If

            .Background = osBrushColor.Black
            .Opacity = OverlayOpacity_Tray
            .Topmost = False
        End With
    End Sub

    Private Sub SetBG()
        With Me
            If .isFromTray Then
                .Background = osBrushColor.Black
                .Opacity = OverlayOpacity_Tray
            Else
                SetOverlayVisuals()

                .Background = osBrushColor.Black
                .Opacity = OverlayOpacity_Popup
            End If
        End With
    End Sub

    Public Sub InitPopupMenuOverlay()
        With Me
            .ShowInTaskbar = False
            .ShowActivated = False
            .Focusable = False

            .Topmost = True

            .HorizontalAlignment = osHorz.Left
            .VerticalAlignment = osVert.Top
            .Width = objScreenData.Width
            .Height = objScreenData.Height

            .Show()
        End With
    End Sub

    Public Async Function InitOverlayOpen(objVisType As OverlayVisualType) As Task
        BeginOpenTask(objTask_Open)
        InitTransitionVisuals(aniOpen, objVisType)

        Await objTask_Open.Task
    End Function

    Public Sub PrepTrayMenuOverlay()

        Me.ShowInTaskbar = False
        Me.ShowActivated = False
        Me.Focusable = False

        Me.Topmost = True
    End Sub

    Public Sub PrepPopupMenuOverlay()
        With SystemInformation.VirtualScreen
            Me.Left = .Left
            Me.Top = .Top
            Me.Width = .Width
            Me.Height = .Height
        End With
    End Sub

    Private Sub SetOverlayVisuals()
        Dim objOverlayVis As New ResourceDictionary() With {
            .Source = VisualDataURI
        }

        With Me
            .Resources.
                MergedDictionaries.Add(objOverlayVis)

            .Style = CType(
                objOverlayVis("OverlayVisuals"), Style)
        End With

    End Sub

    Private Sub OpenComplete()
        Try
            RemoveHandler objAnimation_Open.Completed,
                OpenCompleteEvent
        Catch : End Try

        OverlayOpenComplete(objTask_Open)
        objAnimation_Open = Nothing

        SetVisualMode(aniOpen)
    End Sub

    Private Sub BeginClosingTask(ByRef objCloseResult As TaskCompletionSource(Of Boolean))
        objCloseResult.ResetAndInitTask()
    End Sub

    Private Sub BeginOpenTask(ByRef objOpenResult As TaskCompletionSource(Of Boolean))
        objOpenResult.ResetAndInitTask()
    End Sub

    Private Sub OverlayOpenComplete(ByRef objTask As TaskCompletionSource(Of Boolean))
        objTask.TrySetResult(True)
    End Sub

    Private Sub OverlayCloseComplete(ByRef objTask As TaskCompletionSource(Of Boolean))
        objTask.TrySetResult(True)
    End Sub

    Private Sub PrepTransitionVisuals(objAniType As AnimationType, objVisType As OverlayVisualType)
        Select Case objAniType
            Case aniOpen
                objAnimation_Open = EstablishVisual(Me, objVisType)

                AddHandler objAnimation_Open.Completed,
                    OpenCompleteEvent
            Case aniClose
                objAnimation_Close = EstablishVisual(Me, objVisType)

                AddHandler objAnimation_Close.Completed,
                    Sub()
                        OverlayCloseComplete(objTask_Close)
                    End Sub
        End Select
    End Sub

    Private Function VisRes_Select(objContainer As FrameworkElement) As Style
        Return objContainer.Style
    End Function

    Private Function VisRes_Set(objVisResource As Style, objVisType As PopupVisualType) As Storyboard
        Return TryCast(objVisResource.
            Resources(GetVisualKey(objVisType)), Storyboard)
    End Function

    Private Function EstablishVisual(objContainer As FrameworkElement, objVisType As OverlayVisualType) As Storyboard
        Dim sbMain = VisRes_Set(VisRes_Select(objContainer), objVisType)

        Dim objOverlayVis As Storyboard = sbMain.Clone()

        Storyboard.SetTarget(objOverlayVis, objContainer)
        Storyboard.SetDesiredFrameRate(objOverlayVis, 40)

        Return objOverlayVis
    End Function

    Private Function GetVisualKey(objVisType As OverlayVisualType) As String
        Return idxOverlayVisuals.First(Function(visKey)
                                           Return visKey.Key = objVisType
                                       End Function).Value
    End Function

    Private Function ConfigVisual(visType As AnimationVisual) As DependencyProperty
        Select Case visType
            Case isOpacity
                Return StackPanel.OpacityProperty
        End Select
    End Function

    Public Sub InitTransitionVisuals(objAniType As AnimationType, objVisType As OverlayVisualType)
        PrepTransitionVisuals(objAniType, objVisType)

        Select Case objAniType
            Case aniOpen
                TriggerVisuals(Me, objAnimation_Open)
            Case aniClose
                SetVisualMode(aniClose)
                TriggerVisuals(Me, objAnimation_Close)
        End Select
    End Sub

    Public Async Function InitOverlayClose(objVisType As OverlayVisualType) As Task
        BeginClosingTask(objTask_Close)
        InitTransitionVisuals(aniClose, objVisType)

        Await objTask_Close.Task
    End Function

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)

        Dim hwnd = New Interop.WindowInteropHelper(Me).Handle
        Dim ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64()

        ex = ex Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, New IntPtr(ex))
    End Sub

    Private Async Sub TriggerVisuals(objOverlayWindow As FrameworkElement, objVisualData As Storyboard)
        Await objOverlayWindow.
            Dispatcher.BeginInvoke(
            Sub()
                Try
                    objVisualData.Begin(objOverlayWindow)
                Catch ex As Exception
                    Debug.WriteLine("Failed to begin storyboard: " & ex.Message)
                End Try
            End Sub, DispatcherPriority.Render)
    End Sub

End Class

Public NotInheritable Class osFuncLib_PopupMenu

    ' Private Shared objGui_Popup As osPopupMenu_GUI = Nothing

    Private Shared objPopupTaskMonitor As Task

    Private Shared objPopupTaskPending As TaskCompletionSource(Of Boolean)
    Private Shared objPopupTaskRunning As Boolean

    Public Shared Event EvCloseByClick(sender As Object, e As EventArgs)
    Private Shared Event EvCloseByCmd(sender As Object, e As EventArgs)

    Private Shared ReadOnly Property objGui_Popup As osPopupMenu_GUI
        Get
            Return osHandler_UI.osPopupMenuN
        End Get
    End Property

    Public Shared Async Function ShowPopupMenu() As Task
        InitCloseMonitor(objPopupTaskPending)

        Await osHandler_UI.GeneratePopupMenu()
        Await osHandler_UI.PresentPopupMenu2()

        Dim objPopupResult = Await PopupCloseDetect(objPopupTaskMonitor,
                                                     objPopupTaskPending)

        FinalizePopupMenu(objPopupResult,
                          objPopupTaskMonitor,
                          objPopupTaskPending)
    End Function

    Private Shared Sub ProcessCloseEvent(objPopRes As Boolean)
        If objPopRes Then
            InvokeCloseByCmd()
        End If
    End Sub

    Private Shared Sub FinalizePopupMenu(objPopRes As Boolean, ByRef objMonTask As Task,
                                         ByRef objTaskS As TaskCompletionSource(Of Boolean))
        ProcessCloseEvent(objPopRes)

        RemoveCloseEvents()
        StopCloseDetect(objMonTask, objTaskS)
    End Sub

    Private Shared Async Sub InvokeCloseByCmd()
        Await osHandler_UI.ClosePopupMenu(ClosePopup_ByCmd)
    End Sub

    Private Shared Sub SetMonitorResult(setRes As Boolean, ByRef objTaskS As TaskCompletionSource(Of Boolean))
        objTaskS.TrySetResult(setRes)
    End Sub

    Private Shared Sub PopupCloseEvent_Cmd(sender As Object, e As EventArgs)
        objPopupTaskRunning = False
        SetMonitorResult(True, objPopupTaskPending)
    End Sub

    Private Shared Sub PopupCloseEvent_Click(sender As Object, e As EventArgs)
        objPopupTaskRunning = False
        SetMonitorResult(False, objPopupTaskPending)
    End Sub

    Private Shared Sub SetCloseEvents()
        AddHandler EvCloseByCmd, AddressOf PopupCloseEvent_Cmd

        PrepDispatcher().InvokeAsync(
            Sub()
                AddHandler objGui_Popup.EvCloseByClick,
                AddressOf PopupCloseEvent_Click
            End Sub)
    End Sub

    Private Shared Sub RemoveCloseEvents()
        RemoveHandler EvCloseByCmd, AddressOf PopupCloseEvent_Cmd

        PrepDispatcher().InvokeAsync(
            Sub()
                RemoveHandler objGui_Popup.EvCloseByClick,
                AddressOf PopupCloseEvent_Click
            End Sub)
    End Sub

    Private Shared Function PopupCloseDetect(ByRef objMonTask As Task,
                                              ByRef objTaskS As TaskCompletionSource(Of Boolean)) As Task(Of Boolean)
        InitPopupCmdMon(objMonTask, objTaskS)
        SetCloseEvents()

        Return objTaskS.Task
    End Function

    Private Shared Sub InitCloseMonitor(ByRef objTaskS As TaskCompletionSource(Of Boolean))
        objTaskS = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                        RunContinuationsAsynchronously)
        objPopupTaskRunning = True
    End Sub

    Private Shared Sub InitPopupCmdMon(ByRef objMonTask As Task, objTaskS As TaskCompletionSource(Of Boolean))
        objMonTask = Task.Run(Sub()
                                  MonitorPopupCmd(objTaskS)
                              End Sub)
    End Sub

    Private Shared Sub StopCloseDetect(ByRef objMonTask As Task, ByRef objTaskS As TaskCompletionSource(Of Boolean))
        objMonTask.Wait()
        objMonTask = Nothing

        objTaskS = Nothing
    End Sub

    Private Shared Async Sub MonitorPopupCmd(objTaskS As TaskCompletionSource(Of Boolean))
        While objPopupTaskRunning
            If InputMonSvc.DetectTrigger(DetectOpts.MonitorPopup) Then
                RaiseEvent EvCloseByCmd(objGui_Popup, EventArgs.Empty)
                Exit While
            End If
            Await Task.Delay(5)
        End While
    End Sub

End Class

Public Module osFuncLib_TrayMenu

    Public Property isAppLoaded As Boolean = False

    Public Sub DisplayTrayMenu()
        With osHandler_UI.osTrayMenuN
            .DisplayTrayMenu()
            .Activate()
        End With

        PrepDispatcher().BeginInvoke(
            DispatcherPriority.Background,
                Sub()
                    PrepUtilityTrigger(TriggerType.ShowTrayMenu)
                End Sub)
    End Sub

    Private Sub UpdateTrayIcon(chkStatus As Boolean)
        osTrayIcon.Icon = If(chkStatus, My.Resources.osIcon,
            My.Resources.osIcon_Disabled)
    End Sub

    Private Sub UpdateTrayText(isEnabled As Boolean)
        osTrayIcon.Text = If(isEnabled, "osAutoCast | Enabled", "osAutoCast | Disabled")
    End Sub

    Public Sub UpdateTray(isEnabled As Boolean, Optional isFromTray As Boolean = False)
        UpdateTrayIcon(isEnabled)
        UpdateTrayText(isEnabled)
    End Sub

    Public Sub PrepTrayMenu()
        osTrayIcon = New NotifyIcon With {
            .Icon = My.Resources.osIcon,
            .Text = "osAutoCast | Enabled",
            .Visible = True
        }

        AddHandler osTrayIcon.Click,
             Sub(sender As Object, e As EventArgs)
                 If Not isAppLoaded Then Exit Sub
                 DisplayTrayMenu()
             End Sub
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

    Private Const progEaseThreshold As Double = 0.32

    Private dirSweep As osSweep = osSweep.Clockwise

    Public Function PrepDispatcher(Optional IsAutoPass As Boolean = False) As Dispatcher
        Return If(IsAutoPass, osHandler_UI.osGui_AutoPass2.Dispatcher,
            Application.Current.Dispatcher)
    End Function

    Public Function GetResponse(pType As PromptType) As PromptResponse
        With New PromptData(pType)
            Dim chkPromptResponse = ResponseBox.DisplayPopup(.Msg, .Title, .MsgType)
            Return chkPromptResponse
        End With
    End Function

    Public Function GetResponse(pType As PromptType, respTranslate As Boolean) As Boolean
        With New PromptData(pType)
            Dim chkPromptResponse = ResponseBox.DisplayPopup(.Msg, .Title, .MsgType)
            Return TranslateResponse(chkPromptResponse)
        End With
    End Function

    Public Function TranslateResponse(pResp As PromptResponse) As Boolean
        Return If(pResp = isYes,
            True, False)
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

    Private Function GetBrdr() As SolidColorBrush
        Return New SolidColorBrush(GetBrdrColor()).FreezeReturn()
    End Function

    Private Function GetBrdrColor() As osColors.Color
        Return osColors.Color.FromArgb(CalcRGB(255), CalcRGB(48), CalcRGB(0), CalcRGB(0))
    End Function

    Private Function CalcRGB(cVal As Byte) As Byte
        Return CByte(cVal)
    End Function

    Private Function SetPoint(pX As Double, pY As Double) As osPoint
        Return New osPoint(pX, pY)
    End Function

    Private Function SetSize(sW As Double, sH As Double) As osSize
        Return New osSize(sW, sH)
    End Function

    Private Function SnapToPixel(pt As osPoint, dpi As DpiScale) As osPoint
        Dim dx = Math.Round(pt.X * dpi.DpiScaleX)
        Dim dy = Math.Round(pt.Y * dpi.DpiScaleY)

        Return New osPoint(dx / dpi.DpiScaleX, dy / dpi.DpiScaleY)
    End Function

    Private Function DefineBorderOutline(elem As Visual, objBorderOutline As Rect, radius As Double) As Geometry
        Dim dpiRes = VisualTreeHelper.GetDpi(elem)

        Dim objRadius = Math.Max(0, Math.Min(radius, Math.Min(objBorderOutline.Width / 2.0, objBorderOutline.Height / 2.0)))
        Dim objOutlineGeometry As New StreamGeometry()

        Using ctx As StreamGeometryContext = objOutlineGeometry.Open()
            With objBorderOutline
                Dim p0 = SnapToPixel(SetPoint(.X + objRadius, .Y), dpiRes)
                Dim p1 = SnapToPixel(SetPoint(.Right - objRadius, .Y), dpiRes)
                Dim p2 = SnapToPixel(SetPoint(.Right, .Y + objRadius), dpiRes)
                Dim p3 = SnapToPixel(SetPoint(.Right, .Bottom - objRadius), dpiRes)
                Dim p4 = SnapToPixel(SetPoint(.Right - objRadius, .Bottom), dpiRes)
                Dim p5 = SnapToPixel(SetPoint(.X + objRadius, .Bottom), dpiRes)
                Dim p6 = SnapToPixel(SetPoint(.X, .Bottom - objRadius), dpiRes)
                Dim p7 = SnapToPixel(SetPoint(.X, .Y + objRadius), dpiRes)

                With ctx
                    .BeginFigure(p0, True, True)

                    .LineTo(p1, True, False)
                    .ArcTo(p2, SetSize(objRadius, objRadius), 0, False,
                          SweepDirection.Clockwise, True, False)

                    .LineTo(p3, True, False)
                    .ArcTo(p4, SetSize(objRadius, objRadius), 0, False,
                          SweepDirection.Clockwise, True, False)

                    .LineTo(p5, True, False)
                    .ArcTo(p6, SetSize(objRadius, objRadius), 0, False,
                          SweepDirection.Clockwise, True, False)

                    .LineTo(p7, True, False)
                    .ArcTo(p0, SetSize(objRadius, objRadius), 0, False,
                          SweepDirection.Clockwise, True, False)
                End With
            End With
        End Using

        objOutlineGeometry.Freeze()

        Return objOutlineGeometry
    End Function

    Public Sub EstablishOutline(objOutline As FrameworkElement, radius As Double, Optional clipOffset As Double = 2.5)
        If objOutline Is Nothing Then Return

        objOutline.SetValue(UIElement.ClipToBoundsProperty, True)

        Dim Func_SetOutline As Action =
            Sub()
                Dim outlineW = objOutline.ActualWidth - clipOffset
                Dim outlineH = objOutline.ActualHeight - clipOffset

                If outlineW <= 0 OrElse
                    outlineH <= 0 Then Return

                Dim outlineRect = New Rect(1.5, 1.5, outlineW, outlineH)
                Dim outlineGeo = DefineBorderOutline(objOutline, outlineRect, radius)

                objOutline.Clip = outlineGeo
            End Sub

        AddHandler objOutline.SizeChanged,
            Sub(s, e)
                Func_SetOutline()
            End Sub

        If Not objOutline.IsLoaded Then
            AddHandler objOutline.Loaded,
                Sub(s, e)
                    Func_SetOutline()
                End Sub
        Else
            Func_SetOutline()
        End If
    End Sub

    Public Function EaseProgress2(progVal As Double) As Single
        Dim pVal = Math.Max(0.0, Math.Min(1.0, progVal))

        If pVal < progEaseThreshold Then
            Return pVal
        End If

        Dim pThreshold As Double = (pVal - progEaseThreshold) / (1.0 - progEaseThreshold)
        Dim a = 1.0 - Math.Pow(1.0 - pThreshold, 3)

        Return progEaseThreshold + a * (1.0 - progEaseThreshold)
    End Function

    Public Function EaseProgress(pVal As Double) As Single
        Dim progVal = Math.Max(0.0, Math.Min(1.0, pVal))

        If progEaseThreshold <= 0.0 Then
            Return 1.0 - Math.Pow(1.0 - progVal, 4.0)
        End If
        If progEaseThreshold >= 1.0 Then
            Return progVal
        End If

        If progVal <= progEaseThreshold Then
            Return progVal
        Else
            Dim pThreshold = (progVal - progEaseThreshold) / (1.0 - progEaseThreshold)
            Dim pEased = 1.0 - Math.Pow(1.0 - pThreshold, 4.0)
            Return progEaseThreshold + (1.0 - progEaseThreshold) * pEased
        End If
    End Function

    Public Function CalcEase(eVal As Double) As Double
        Dim pThreshold As Double = (eVal - progEaseThreshold) / (1.0 - progEaseThreshold)
        Return 1.0 - Math.Pow(1.0 - pThreshold, 3)
    End Function

    Public Function EaseInOutExpo(pDuration As Double) As Double
        If pDuration = 0.0 Then Return 0.0
        If pDuration = 1.0 Then Return 1.0
        Return If(pDuration < 0.5, Math.Pow(2, 20 * pDuration - 10) / 2,
            (2 - Math.Pow(2, -20 * pDuration + 10)) / 2)
    End Function

    Public Function DetermineMouseClick(MouseArgs As MouseButtonEventArgs) As Boolean
        Return If(MouseArgs.ClickCount > 0,
            True, False)
    End Function

End Module



Public Class osPrefExpandEase
    Inherits EasingFunctionBase

    Public Property X1 As Double = 0.64
    Public Property Y1 As Double = 0.37
    Public Property X2 As Double = 0.34
    Public Property Y2 As Double = 1.44

    Protected Overrides Function CreateInstanceCore() As Freezable
        Return New osPrefExpandEase With {
                .X1 = X1,
                .Y1 = Y1,
                .X2 = X2,
                .Y2 = Y2
            }
    End Function

    Protected Overrides Function EaseInCore(normalizedTime As Double) As Double
        ' If both control x's are the trivial curve, return linear
        If X1 = X2 AndAlso Y1 = Y2 AndAlso X1 = 0 AndAlso Y1 = 0 Then
            Return normalizedTime
        End If

        ' Precompute polynomial coefficients for x and y
        Dim cx As Double = 3.0 * X1
        Dim bx As Double = 3.0 * (X2 - X1) - cx
        Dim ax As Double = 1.0 - cx - bx

        Dim cy As Double = 3.0 * Y1
        Dim by As Double = 3.0 * (Y2 - Y1) - cy
        Dim ay As Double = 1.0 - cy - by

        ' Solve for parameter t such that x(t) == normalizedTime
        Dim t As Double = SolveForT(normalizedTime, ax, bx, cx)

        ' Evaluate y(t)
        Dim result As Double = ((ay * t + by) * t + cy) * t
        Return result
    End Function

    ' Evaluate x(t) polynomial
    Private Function SampleCurveX(t As Double, ax As Double, bx As Double, cx As Double) As Double
        Return ((ax * t + bx) * t + cx) * t
    End Function

    ' Evaluate derivative dx/dt
    Private Function SampleCurveDerivativeX(t As Double, ax As Double, bx As Double, cx As Double) As Double
        Return (3.0 * ax * t * t) + (2.0 * bx * t) + cx
    End Function

    ' Solve x(t) = x for t using Newton-Raphson with binary fallback
    Private Function SolveForT(x As Double, ax As Double, bx As Double, cx As Double) As Double
        Dim t As Double = x ' good initial guess
        Const NEWTON_ITERATIONS As Integer = 8
        Const EPS As Double = 0.0000001

        For i As Integer = 0 To NEWTON_ITERATIONS - 1
            Dim xAtT As Double = SampleCurveX(t, ax, bx, cx) - x
            Dim dx As Double = SampleCurveDerivativeX(t, ax, bx, cx)
            If Math.Abs(dx) < 0.000001 Then Exit For
            Dim tNext As Double = t - xAtT / dx
            If Double.IsNaN(tNext) OrElse tNext < 0 OrElse tNext > 1 Then Exit For
            t = tNext
        Next

        ' If Newton didn't converge, use binary search
        Dim lo As Double = 0.0
        Dim hi As Double = 1.0
        t = x
        For i As Integer = 0 To 30
            Dim xAtT As Double = SampleCurveX(t, ax, bx, cx)
            If Math.Abs(xAtT - x) < EPS Then Exit For
            If x > xAtT Then
                lo = t
                t = (t + hi) / 2.0
            Else
                hi = t
                t = (t + lo) / 2.0
            End If
        Next

        Return Math.Min(1.0, Math.Max(0.0, t))
    End Function
End Class

Public Class isEnabledConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return If(CBool(value), "Enabled", "Disabled")
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function

End Class

Public Class VisQualityConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert

        If value Is Nothing Then Return Nothing

        Select Case CInt(value)
            Case 0 : Return "Performance"
            Case 1 : Return "Quality"
            Case Else : Return "Unknown"
        End Select
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack

        If value Is Nothing Then Return 0

        Select Case value.ToString()
            Case 0 : Return "Performance"
            Case 1 : Return "Quality"
            Case Else : Return 0
        End Select
    End Function

End Class

Public Class MenuFuncAdapter
    Implements INotifyPropertyChanged

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
            RaiseEvent PropertyChanged(Me,
                                       New PropertyChangedEventArgs(NameOf(Value)))
        End Set
    End Property

    Public Sub Refresh()
        RaiseEvent PropertyChanged(Me,
                                   New PropertyChangedEventArgs(NameOf(Value)))
    End Sub

    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged
End Class

Public Class TrayIconBridge
    Inherits DependencyObject

    Public Property NotifyIcon As NotifyIcon

    Public Shared ReadOnly IsEnabledProperty As DependencyProperty =
        DependencyProperty.Register(NameOf(IsEnabled), GetType(Boolean), GetType(TrayIconBridge),
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
        Dim objTrayIcon = DirectCast(d, TrayIconBridge)

        With objTrayIcon
            If .NotifyIcon Is Nothing Then Exit Sub

            Dim onOff = CBool(e.NewValue)
            .NotifyIcon.Icon = If(onOff, My.Resources.osIcon, My.Resources.osIcon_Disabled)
            .NotifyIcon.Text = If(onOff, "osAutoCast | Enabled", "osAutoCast | Disabled")
            .NotifyIcon.Visible = True
        End With

    End Sub
End Class

Public Class osEnabledStatusConfig
    Implements INotifyPropertyChanged

    Private Shared _instance As osEnabledStatusConfig
    Public Shared ReadOnly Property Instance As osEnabledStatusConfig
        Get
            If _instance Is Nothing Then
                _instance = New osEnabledStatusConfig()
            End If
            Return _instance
        End Get
    End Property

    Private Shared _EnabledStatus As Boolean = True
    Public Property osEnabledStatus As Boolean
        Get
            Return _EnabledStatus
        End Get
        Set(newStatus As Boolean)
            PromptResponseState.EnterPromptResponse()

            Try
                Dim TriggerStatusChange As Boolean = True

                Select Case ValidateStatus(newStatus)
                    Case SetStatus_Enabled
                        ApplyStatusUpdate(SetStatus_Enabled)
                    Case SetStatus_Disabled
                        If ConfirmStatusUpdate() = UpdateStatus.ToDisabled Then
                            ApplyStatusUpdate(SetStatus_Disabled)
                        Else
                            TriggerStatusChange = False : End If
                    Case RetainStatus
                        TriggerStatusChange = False
                End Select

                If TriggerStatusChange Then
                    RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(
                                           NameOf(osEnabledStatus))) : End If
            Finally
                PromptResponseState.ExitPromptResponse()
            End Try
        End Set
    End Property

    Private Function ValidateStatus(chkNewStatus As Boolean) As UpdateStatusAction
        If _EnabledStatus <> chkNewStatus Then
            Return If(chkNewStatus, SetStatus_Enabled,
                SetStatus_Disabled)
        Else
            Return RetainStatus
        End If
    End Function

    Private Sub ApplyStatusUpdate(newStatus As UpdateStatusAction)
        Select Case newStatus
            Case SetStatus_Enabled
                osFuncLib_InputScan.SetMonitorState(MonitorStatus.Starting)
                Application.RestartMonitor()
            Case SetStatus_Disabled
                osFuncLib_InputScan.SetMonitorState(MonitorStatus.Paused)
        End Select

        SetStatusAction(TranslateStatus(newStatus))
    End Sub

    Private Function TranslateStatus(valStatus As UpdateStatusAction) As Boolean
        Return If(valStatus = SetStatus_Enabled,
            True, False)
    End Function

    Private Function ConfirmStatusUpdate() As UpdateStatus
        Select Case GetResponse(PromptType.DisableService)
            Case isYes
                Return UpdateStatus.ToDisabled
            Case Else
                Return UpdateStatus.CancelUpdate
        End Select
    End Function

    Private Sub SetStatusAction(setStatus As Boolean)
        _EnabledStatus = setStatus
        UpdateTray(setStatus)
    End Sub

    Public Function osEnabledStatus_Fetch() As Boolean
        Return _EnabledStatus
    End Function

    Public Function osChkEnabledStatus_IsDisabled() As Boolean
        Return _EnabledStatus = False
    End Function

    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged
End Class

Public NotInheritable Class osMenuFuncBinder
    Implements INotifyPropertyChanged

    Public Enum UpdateStatus
        ToEnabled
        ToDisabled
        CancelUpdate
    End Enum

    Private Enum MenuBinderType
        isChk
        isMenu
    End Enum

    Private Shared Sub ApplyMenuBinding(MenuItemObj As DependencyObject,
                                        MenuItemBinder As osBinder.Binding,
                                        BinderType As MenuBinderType)
        Select Case BinderType
            Case MenuBinderType.isChk
                BindingOperations.
                    SetBinding(MenuItemObj,
                               osWinControls.MenuItem.IsCheckedProperty,
                               MenuItemBinder)
            Case MenuBinderType.isMenu
                BindingOperations.
                    SetBinding(MenuItemObj,
                               HeaderedItemsControl.HeaderProperty,
                               MenuItemBinder)
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

    Public Shared Async Function BindChecked_Popup(objMenuItem As osWinControls.MenuItem,
                                        DoFunc_FetchStatus As Func(Of Boolean),
                                        DoFunc_ConfirmStatus As Action(Of Boolean)) As Task
        Await Task.Run(
            Async Function()
                Await PrepDispatcher().InvokeAsync(
                    Sub()
                        Dim osMenuAdapter = New MenuFuncAdapter(DoFunc_FetchStatus, DoFunc_ConfirmStatus)

                        Dim osBinder_ChkEnabled = GenMenuBinding(osMenuAdapter, MenuBinderType.isChk)
                        ApplyMenuBinding(objMenuItem, osBinder_ChkEnabled, MenuBinderType.isChk)

                        Dim osBinder_MenuText = GenMenuBinding(osMenuAdapter, MenuBinderType.isMenu)
                        ApplyMenuBinding(objMenuItem, osBinder_MenuText, MenuBinderType.isMenu)

                        AddHandler osMenuAdapter.PropertyChanged,
                        Sub(sender, e)
                            If e.PropertyName = NameOf(osMenuAdapter.Value) Then
                                Dim isEnabled As Boolean = osMenuAdapter.Value

                                UpdateTray(isEnabled)
                            End If
                        End Sub
                    End Sub)
            End Function)
    End Function

    Public Event Binding_NotifyPropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub OnPropertyChanged(<CallerMemberName> Optional name As String = Nothing)
        RaiseEvent Binding_NotifyPropertyChanged(Me, New PropertyChangedEventArgs(name))
    End Sub

End Class

Public Module ControlExtensions

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

    <Runtime.CompilerServices.Extension>
    Public Function GetWidth(ByVal rectf As osRect.RawRectangleF) As Single
        Return rectf.Right - rectf.Left
    End Function

    <Runtime.CompilerServices.Extension>
    Public Sub SafeDispose(Of T As {Class, IDisposable})(ByRef obj As T)
        If obj IsNot Nothing Then
            Try
                osUtilities.Dispose(obj)
            Finally
                obj = Nothing
            End Try
        End If
    End Sub

    <Runtime.CompilerServices.Extension>
    Public Sub DisposeMonitor(Of T As {Class, IDisposable})(ByRef obj As T)
        If obj IsNot Nothing Then
            Try
                obj.Dispose()
            Finally
                obj = Nothing
            End Try
        End If
    End Sub

    <Runtime.CompilerServices.Extension>
    Public Sub ResetTask(ByRef objTask As TaskCompletionSource(Of Boolean))
        If objTask IsNot Nothing Then
            objTask = Nothing : End If
    End Sub

    <Runtime.CompilerServices.Extension>
    Public Sub ResetAndInitTask(ByRef objTask As TaskCompletionSource(Of Boolean))
        PrepTask(objTask)

        objTask = New TaskCompletionSource(Of Boolean)(
            TaskCreationOptions.RunContinuationsAsynchronously)
    End Sub

    Private Sub PrepTask(ByRef objTask As TaskCompletionSource(Of Boolean))
        If objTask IsNot Nothing Then
            objTask = Nothing : End If
    End Sub

    <Runtime.CompilerServices.Extension>
    Public Function PrefVal(ByRef objPref As Object) As String
        Return Convert.ToString(objPref)
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function ToShaderPx(objPref As Object(), index As Integer) As pxShader_Pixel
        Dim objPxS = CType(objPref(index), pxShader_Pixel)
        Return objPxS
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function ToShaderVer(objPref As Object(), index As Integer) As pxShader_Vertex
        Dim objPxE = CType(objPref(index), pxShader_Vertex)
        Return objPxE
    End Function

End Module

Public NotInheritable Class TextBlockExtensions

    Private Sub New()
    End Sub

    Public Shared ReadOnly CharacterSpacingProperty As DependencyProperty = DependencyProperty.
        RegisterAttached("CharacterSpacing", GetType(Double), GetType(TextBlockExtensions),
                         New PropertyMetadata(0.0, AddressOf OnCharacterSpacingChanged))

    Public Shared Sub SetCharacterSpacing(obj As DependencyObject, value As Double)
        obj.SetValue(CharacterSpacingProperty, value)
    End Sub

    Public Shared Function GetCharacterSpacing(obj As DependencyObject) As Double
        Return CDbl(obj.GetValue(CharacterSpacingProperty))
    End Function

    Private Shared ReadOnly IsHookedProperty As DependencyProperty = DependencyProperty.
        RegisterAttached("IsHooked", GetType(Boolean),
                         GetType(TextBlockExtensions), New PropertyMetadata(False))

    Private Shared Sub SetIsHooked(obj As DependencyObject, value As Boolean)
        obj.SetValue(IsHookedProperty, value)
    End Sub

    Private Shared Function GetIsHooked(obj As DependencyObject) As Boolean
        Return CBool(obj.GetValue(IsHookedProperty))
    End Function

    Private Shared ReadOnly TextDescriptor As DependencyPropertyDescriptor = DependencyPropertyDescriptor.
        FromProperty(TextBlock.TextProperty, GetType(TextBlock))

    Private Shared Sub OnCharacterSpacingChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim tb = TryCast(d, TextBlock)
        If tb Is Nothing Then Return

        Dim spacing = CDbl(e.NewValue)

        If spacing <> 0 Then
            Hook(tb)
            UpdateEffects(tb, spacing)
        Else
            tb.TextEffects = Nothing
            Unhook(tb)
        End If
    End Sub

    Private Shared Sub Hook(tb As TextBlock)
        If GetIsHooked(tb) Then Return
        SetIsHooked(tb, True)

        AddHandler tb.Loaded, AddressOf Tb_Loaded

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

        tb.TextEffects = Nothing

        If spacing = 0 Then Return

        Dim text = If(tb.Text, String.Empty)
        If text.Length = 0 Then Return

        Dim effects As New TextEffectCollection()

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

Public Module osRunCmd

    Public cmd_KillGame() As String = "taskkill,/f /im MTGA.exe".Split(",")
    Public cmd_StartGame() As String = "taskkill,/f /im MTGA.exe".Split(",")

    Public Sub RunCmd(cmd As String, Optional arguments As String = "",
                           Optional timeoutMs As Integer = 30000, Optional workingDir As String = Nothing,
                           Optional forceUtf8 As Boolean = True)

        Dim inner As New StringBuilder()

        inner.Append("/c ")

        If forceUtf8 Then
            inner.Append("chcp 65001 >nul & ")
        End If

        inner.Append(cmd)

        If Not String.IsNullOrWhiteSpace(arguments) Then
            inner.Append(" "c).Append(arguments)
        End If

        Dim objStartProcess As New ProcessStartInfo() With {
            .FileName = "cmd.exe",
            .Arguments = inner.ToString(),
            .UseShellExecute = False,
            .CreateNoWindow = True
        }

        If Not String.IsNullOrWhiteSpace(workingDir) Then
            objStartProcess.WorkingDirectory = workingDir
        End If

        With New Process()
            .StartInfo = objStartProcess
            .Start()

            If Not .WaitForExit(timeoutMs) Then
                Try
                    .Kill()
                Catch : End Try
            Else
                .WaitForExit()
            End If
        End With

    End Sub

End Module

#Enable Warning BC42353
#Enable Warning IDE0060 ' Remove unused parame
#Enable Warning IDE1006 ' Remove unused parameterter
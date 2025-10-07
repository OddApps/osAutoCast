Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows

Public Class osHandler_Input

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowsHookEx(idHook As Integer, lpfn As LowLevelMouseProc, hMod As IntPtr, dwThreadId As UInteger) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function UnhookWindowsHookEx(hhk As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function CallNextHookEx(hhk As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function GetModuleHandle(lpModuleName As String) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function PeekMessage(ByRef lpMsg As NativeMessage, hWnd As IntPtr, wMsgFilterMin As UInteger,
                                        wMsgFilterMax As UInteger, wRemoveMsg As UInteger) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function TranslateMessage(ByRef lpMsg As NativeMessage) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function DispatchMessage(ByRef lpMsg As NativeMessage) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SendInput(nInputs As UInteger, ByRef pInputs As INPUT, cbSize As Integer) As UInteger
    End Function

    <DllImport("user32.dll")>
    Public Shared Function MapVirtualKey(code As UInteger, mapType As UInteger) As UInteger
    End Function

    <DllImport("user32.dll")>
    Public Shared Function GetMessageExtraInfo() As IntPtr
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Private Structure MSLLHOOKSTRUCT
        Public pt As Point
        Public mouseData As UInteger
        Public flags As UInteger
        Public time As UInteger
        Public dwExtraInfo As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Structure INPUT
        Public type As UInteger
        Public U As InputUnion
    End Structure

    <StructLayout(LayoutKind.Explicit)>
    Structure InputUnion
        <FieldOffset(0)> Public ki As KEYBDINPUT
        <FieldOffset(0)> Public mi As MOUSEINPUT
        <FieldOffset(0)> Public hi As HARDWAREINPUT
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Structure KEYBDINPUT
        Public wVk As UShort
        Public wScan As UShort
        Public dwFlags As UInteger
        Public time As UInteger
        Public dwExtraInfo As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Structure MOUSEINPUT
        Public dx As Integer
        Public dy As Integer
        Public mouseData As Integer
        Public dwFlags As UInteger
        Public time As UInteger
        Public dwExtraInfo As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Structure HARDWAREINPUT
        Public uMsg As UInteger
        Public wParamL As UShort
        Public wParamH As UShort
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure NativeMessage
        Public hWnd As IntPtr
        Public message As UInteger
        Public wParam As IntPtr
        Public lParam As IntPtr
        Public time As UInteger
        Public pt As Point
    End Structure

    Private Delegate Function LowLevelMouseProc(nCode As Integer, wParam As IntPtr,
                                                lParam As IntPtr) As IntPtr

    Private Const HC_ACTION As Integer = 0
    Private Const LLMHF_INJECTED As Integer = &H1

    Private Const WH_MOUSE_LL As Integer = 14

    Private Const WM_LBUTTONDOWN As Integer = &H201
    Private Const WM_LBUTTONUP As Integer = &H202

    Private Const VK_SHIFT As UShort = &H10
    Private Const VK_RSHIFT As UShort = &HA1
    Private Const VK_RETURN As UShort = &HD

    Private Const SC_SHIFT As UShort = &H2A
    Private Const SC_ENTER As UShort = &H1C

    Private Const INPUT_KEYBOARD As Integer = 1
    Private Const KEYEVENTF_KEYUP As UInteger = &H2
    Private Const KEYEVENTF_SCANCODE As UInteger = &H8

    Private Shared hookHandle As IntPtr = IntPtr.Zero

    Private Shared hookProc_PreventClick As LowLevelMouseProc = AddressOf InvokeInputHook
    Private Shared hookProc_CloseMenu As LowLevelMouseProc = AddressOf InvokeInputHook

    Private Shared messagePumpTask As Task
    Private Shared cancelSource As CancellationTokenSource

    Public Shared Async Function SuppressInput() As Task
        If hookHandle <> IntPtr.Zero Then Exit Function

        cancelSource = New CancellationTokenSource()

        hookHandle = SetWindowsHookEx(WH_MOUSE_LL, hookProc_PreventClick, GetModuleHandle(Nothing), 0)

        messagePumpTask = Task.Run(Sub() MessagePump(cancelSource.Token))
        Await Task.CompletedTask

        Await Task.Delay(75)
    End Function

    Public Shared Async Function RestoreInput() As Task
        If hookHandle = IntPtr.Zero Then Exit Function

        cancelSource.Cancel()

        UnhookWindowsHookEx(hookHandle)
        hookHandle = IntPtr.Zero

        Try
            Await messagePumpTask
        Catch ex As TaskCanceledException
        End Try

        Await Task.CompletedTask
    End Function

    Private Shared Function InvokeInputHook(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        If ConfirmAction(nCode) Then
            If VerifyInjection(ConstructHook(lParam)) Then
                If DetectClickType(wParam) Then
                    Return CType(1, IntPtr)
                End If
            End If
        End If

        Return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam)
    End Function

    Private Shared Function ConfirmAction(nCode As Integer) As Boolean
        Return nCode = HC_ACTION
    End Function

    Private Shared Function ConstructHook(chkParam As IntPtr) As MSLLHOOKSTRUCT
        Return Marshal.PtrToStructure(Of MSLLHOOKSTRUCT)(chkParam)
    End Function

    Private Shared Function VerifyInjection(objInputHook As MSLLHOOKSTRUCT) As Boolean
        Return Not (objInputHook.flags And LLMHF_INJECTED) = LLMHF_INJECTED
    End Function

    Private Shared Function DetectClickType(chkParam As IntPtr) As Boolean
        Return chkParam.ToInt32() = WM_LBUTTONDOWN OrElse
            chkParam.ToInt32() = WM_LBUTTONUP
    End Function

    Private Shared Sub MessagePump(token As CancellationToken)
        Dim msg As NativeMessage

        While Not token.IsCancellationRequested
            If PeekMessage(msg, IntPtr.Zero, 0, 0, 1) Then
                TranslateMessage(msg)
                DispatchMessage(msg)
            End If

            Thread.Sleep(1)
        End While
    End Sub

    Public Shared Sub InjectInput(iType As InjectType)
        With New InjectInputData(iType)
            Dim objInputData As New INPUT()

            objInputData.type = INPUT_KEYBOARD
            objInputData.U.ki.wVk = .KeyType
            objInputData.U.ki.wScan = 0
            objInputData.U.ki.dwFlags = .InjectData
            objInputData.U.ki.time = 0
            objInputData.U.ki.dwExtraInfo = GetMessageExtraInfo()

            SendInput(1, objInputData, Marshal.SizeOf(GetType(INPUT)))
        End With
    End Sub

    Public Shared Async Sub InjectAutoPassInputs()
        Await InjectShiftKey()

        Await InjectReturnKey()

        Await InjectShiftKey(True)
    End Sub

    Private Shared Async Function InjectShiftKey(Optional kUp As Boolean = False) As Task
        If kUp Then
            osHandler_Input.InjectInput(InjectType.Shift_U)
        Else
            osHandler_Input.InjectInput(InjectType.Shift_D)
        End If

        Await Task.Delay(50)
    End Function

    Private Shared Async Function InjectReturnKey() As Task
        osHandler_Input.InjectInput(InjectType.Enter_D)
        Await Task.Delay(50)

        osHandler_Input.InjectInput(InjectType.Enter_U)
        Await Task.Delay(50)
    End Function

End Class

Public Class ObserveMenuCloseClick
    Implements Forms.IMessageFilter

    Private ReadOnly _menu As Forms.ContextMenuStrip

    ' Mouse down + non-client mouse down messages
    Private Const WM_LBUTTONDOWN As Integer = &H201
    Private Const WM_RBUTTONDOWN As Integer = &H204
    Private Const WM_MBUTTONDOWN As Integer = &H207
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const WM_NCRBUTTONDOWN As Integer = &HA4
    Private Const WM_NCMBUTTONDOWN As Integer = &HA7

    Public Sub New(menu As Forms.ContextMenuStrip)
        _menu = menu
    End Sub

    Public Function PreFilterMessage(ByRef m As Forms.Message) As Boolean Implements Forms.IMessageFilter.PreFilterMessage
        If _menu Is Nothing OrElse Not _menu.Visible Then
            Return False
        End If

        Select Case m.Msg
            Case WM_LBUTTONDOWN, WM_RBUTTONDOWN, WM_MBUTTONDOWN,
                 WM_NCLBUTTONDOWN, WM_NCRBUTTONDOWN, WM_NCMBUTTONDOWN

                Dim pos As System.Drawing.Point = Forms.Control.MousePosition ' screen coords
                ' If click is outside the menu bounds, close it
                If Not _menu.Bounds.Contains(pos) Then
                    _menu.Close(Forms.ToolStripDropDownCloseReason.AppClicked)
                    ' Return False so the click continues to its target
                    Return False
                End If
        End Select

        Return False
    End Function
End Class
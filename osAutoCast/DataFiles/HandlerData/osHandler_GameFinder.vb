Imports System.Runtime.InteropServices
Imports System.Text

Public Class DetectGameUI

    <DllImport("user32.dll")>
    Private Shared Function EnumWindows(ByVal lpEnumFunc As EnumWindowsProc,
                                        ByVal lParam As IntPtr) As Boolean
    End Function

    Private Delegate Function EnumWindowsProc(ByVal hWnd As IntPtr,
                                              ByVal lParam As IntPtr) As Boolean

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function GetWindowText(hWnd As IntPtr, lpString As StringBuilder, nMaxCount As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function IsWindowVisible(hWnd As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef lpdwProcessId As UInteger) As UInteger
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetForegroundWindow(hWnd As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function OpenProcess(dwDesiredAccess As Integer,
                                        bInheritHandle As Boolean,
                                        dwProcessId As UInteger) As IntPtr
    End Function

    <DllImport("psapi.dll", SetLastError:=True)>
    Private Shared Function GetModuleBaseName(hProcess As IntPtr, hModule As IntPtr,
                                              lpBaseName As StringBuilder, nSize As Integer) As Integer
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function CloseHandle(hObject As IntPtr) As Boolean
    End Function

    Private Const PROCESS_QUERY_INFORMATION As Integer = &H400
    Private Const PROCESS_VM_READ As Integer = &H10

    Public Shared Function FocusMTGA(Optional setFocus As Boolean = False) As Boolean
        Dim mtgaHwnd As IntPtr = IntPtr.Zero

        EnumWindows(Function(hWnd, lParam)
                        If Not IsWindowVisible(hWnd) Then Return True

                        Dim pid As UInteger
                        GetWindowThreadProcessId(hWnd, pid)

                        Dim hProcess As IntPtr = OpenProcess(PROCESS_QUERY_INFORMATION Or PROCESS_VM_READ,
                                                             False, pid)
                        If hProcess = IntPtr.Zero Then Return True

                        Dim exeMTGA As New StringBuilder(256)

                        GetModuleBaseName(hProcess, IntPtr.Zero, exeMTGA, exeMTGA.Capacity)
                        CloseHandle(hProcess)

                        If exeMTGA.ToString().ToLower() = "mtga.exe" Then
                            mtgaHwnd = hWnd
                            Return False ' stop searching
                        End If

                        Return True
                    End Function, IntPtr.Zero)

        If mtgaHwnd <> IntPtr.Zero Then
            If setFocus Then SetForegroundWindow(mtgaHwnd)
            Return GetForegroundWindow() = mtgaHwnd
        End If

        Return False
    End Function

    Public Shared Function FetchHwndMTGA() As IntPtr
        Dim mtgaHwnd As IntPtr = IntPtr.Zero

        EnumWindows(Function(hWnd, lParam)
                        If Not IsWindowVisible(hWnd) Then Return True

                        Dim pid As UInteger
                        GetWindowThreadProcessId(hWnd, pid)

                        Dim hProcess As IntPtr = OpenProcess(PROCESS_QUERY_INFORMATION Or PROCESS_VM_READ,
                                                             False, pid)
                        If hProcess = IntPtr.Zero Then Return True

                        Dim exeMTGA As New StringBuilder(256)

                        GetModuleBaseName(hProcess, IntPtr.Zero, exeMTGA, exeMTGA.Capacity)
                        CloseHandle(hProcess)

                        If exeMTGA.ToString().ToLower() = "mtga.exe" Then
                            mtgaHwnd = hWnd
                            Return False ' stop searching
                        End If

                        Return True
                    End Function, IntPtr.Zero)

        Return mtgaHwnd
    End Function

End Class

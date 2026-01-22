Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports osRunForm = System.Windows.Forms.Application

Public NotInheritable Class osHandler_AutoCast

    Private Const WM_QUIT As Integer = &H12

    Private Shared _osGui_AutoCastProgress As AutoCastGui = Nothing
    Private Shared _autoCastProgress As ProgBarGui_AutoCast = Nothing

    Private Shared _autoCastCts As CancellationTokenSource = Nothing

    Private Shared _autoCastThread As Thread = Nothing
    Private Shared _autoCastThreadId As Integer = 0

    Private Shared ReadOnly _startupLock As New Object()
    Private Shared ReadOnly _startupEvent As New ManualResetEvent(False)

    Public Shared Sub InitializeAutoCastUI()
        SyncLock _startupLock

            If _autoCastThread IsNot Nothing AndAlso _autoCastThread.IsAlive Then
                Return
            End If

            _autoCastCts = New CancellationTokenSource()
            _startupEvent.Reset()

            _autoCastThread = New Thread(
                Sub()
                    _autoCastThreadId = GetCurrentThreadId()

                    Dim objContextAC As New ApplicationContext()

                    With CoreDataLib.FetchProgSizeReport(TriggerType.AutoCast, True)
                        Dim objWin_AC As New ProgBarGui_AutoCast(.Item("pW"), .Item("pH"),
                                            osFuncLib_Progress.ProgTimeSpan_AC, AddressOf EaseProgress)

                        '     _autoCastProgress = objWin_AC

                        _osGui_AutoCastProgress = New AutoCastGui()
                        ProgBarGui_AutoCast.Instance = objWin_AC

                        AddHandler objWin_AC.FormClosed,
                            Sub()
                                Try
                                    objContextAC.ExitThread()
                                Catch : End Try
                            End Sub

                        _startupEvent.Set()
                        osRunForm.Run(objContextAC)

                        '    _autoCastProgress = Nothing
                        ProgBarGui_AutoCast.Instance = Nothing
                        _osGui_AutoCastProgress = Nothing
                        _autoCastThreadId = 0
                    End With
                End Sub)

            _autoCastThread.SetApartmentState(ApartmentState.STA)
            _autoCastThread.IsBackground = True
            _autoCastThread.Start()
        End SyncLock
    End Sub

    Public Shared Function StopAutoCastGui(Optional timeoutMs As Integer = 5000) As Boolean
        SyncLock _startupLock
            If _autoCastThread Is Nothing Then
                Return True
            End If

            Try
                Dim objAcInstance = ProgBarGui_AutoCast.Instance

                If objAcInstance IsNot Nothing AndAlso Not objAcInstance.IsDisposed Then
                    Try
                        objAcInstance.BeginInvoke(
                            New Action(Sub()
                                           If Not objAcInstance.IsDisposed Then
                                               objAcInstance.Close()
                                           End If
                                       End Sub))
                    Catch : End Try
                Else
                    If _autoCastThreadId <> 0 Then
                        Try
                            PostThreadMessage(_autoCastThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero)
                        Catch : End Try
                    End If : End If
            Catch : End Try

            If _autoCastThread IsNot Nothing Then
                _autoCastThread.Join(timeoutMs)
            End If

            Try
                _autoCastCts?.Cancel()
                _autoCastCts?.Dispose()
            Catch : End Try

            '      _autoCastProgress = Nothing
            _autoCastCts = Nothing
            _autoCastThread = Nothing
            _osGui_AutoCastProgress = Nothing
            ProgBarGui_AutoCast.Instance = Nothing
            _startupEvent.Reset()
            _autoCastThreadId = 0

            Return True
        End SyncLock
    End Function

    Private Shared Sub SetCloseEvent(acContext As ApplicationContext)
        AddHandler _autoCastProgress.FormClosed,
            Sub()
                Try
                    acContext.ExitThread()
                Catch : End Try
            End Sub
    End Sub

    Private Shared Sub ResetAutoCast(Optional doAll As Boolean = False)
        If doAll Then
            _autoCastProgress = Nothing
            _autoCastCts = Nothing
            _autoCastThread = Nothing
            _osGui_AutoCastProgress = Nothing
            ProgBarGui_AutoCast.Instance = Nothing
            _startupEvent.Reset()
            _autoCastThreadId = 0
        Else
            _autoCastProgress = Nothing
            ProgBarGui_AutoCast.Instance = Nothing
            _osGui_AutoCastProgress = Nothing
            _autoCastThreadId = 0
        End If
    End Sub

    Public Shared ReadOnly Property AutoCast_UI As AutoCastGui
        Get
            Return _osGui_AutoCastProgress
        End Get
    End Property

    <DllImport("kernel32.dll")>
    Private Shared Function GetCurrentThreadId() As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function PostThreadMessage(dwThreadId As Integer, Msg As Integer, wParam As IntPtr, lParam As IntPtr) As Boolean
    End Function

End Class
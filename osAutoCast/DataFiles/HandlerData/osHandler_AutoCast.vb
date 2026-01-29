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

    Private Shared _autoCastTcs As TaskCompletionSource(Of Boolean) = Nothing

    Public Shared Function StartAutoCastAsync() As Task(Of Boolean)
        ' Pre-fetch UI size on caller thread (avoid doing this while holding the lock / on the STA thread).
        Dim progSize = CoreDataLib.FetchProgSizeReport(TriggerType.AutoCast, True)
        Dim pW As Integer = CInt(progSize.Item("pW"))
        Dim pH As Integer = CInt(progSize.Item("pH"))

        SyncLock _startupLock
            ' If already running, return the same Task so callers can await it
            If _autoCastThread IsNot Nothing AndAlso _autoCastThread.IsAlive Then
                If _autoCastTcs IsNot Nothing Then
                    Return _autoCastTcs.Task
                Else
                    Return Task.FromResult(False)
                End If
            End If

            ' Dispose old CTS safely
            Try
                _autoCastCts?.Dispose()
            Catch : End Try

            _autoCastCts = New CancellationTokenSource()
            _autoCastTcs = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.RunContinuationsAsynchronously)

            ' Create and start STA thread, pass the pre-fetched sizes to the thread proc
            _autoCastThread = New Thread(Sub() AutoCastThreadProc(pW, pH, _autoCastTcs)) With {
            .IsBackground = True
        }
            _autoCastThread.SetApartmentState(ApartmentState.STA)
            _autoCastThread.Start()

            Return _autoCastTcs.Task
        End SyncLock
    End Function

    ' The STA thread body now accepts the pre-fetched pW/pH
    Private Shared Sub AutoCastThreadProc(pW As Integer, pH As Integer, tcs As TaskCompletionSource(Of Boolean))
        Dim localCts As CancellationTokenSource = Nothing
        Dim objWin_AC As ProgBarGui_AutoCast = Nothing
        Dim objContextAC As ApplicationContext = Nothing

        Try
            _autoCastThreadId = GetCurrentThreadId()
            localCts = _autoCastCts ' local reference

            ' Application context & UI creation
            objContextAC = New ApplicationContext()

            objWin_AC = New ProgBarGui_AutoCast(pW, pH, osFuncLib_Progress.ProgTimeSpan_AC, AddressOf EaseProgress)

            _osGui_AutoCastProgress = New AutoCastGui()
            ProgBarGui_AutoCast.Instance = objWin_AC

            ' Use named handler if you need to remove it later; inline is OK if you never remove.
            AddHandler objWin_AC.FormClosed, Sub()
                                                 Try
                                                     objContextAC.ExitThread()
                                                 Catch : End Try
                                             End Sub

            ' Signal success to awaiters: UI is created and message loop will start.
            If tcs IsNot Nothing Then
                tcs.TrySetResult(True)
            End If

            ' Run the message loop (blocks until ExitThread/Application.Exit)
            osRunForm.Run(objContextAC)

        Catch ex As Exception
            ' Make the exception visible to awaiting callers
            If tcs IsNot Nothing Then
                tcs.TrySetException(ex)
            End If
        Finally
            ' Clean-up UI references and thread state
            Try
                If objWin_AC IsNot Nothing Then
                    Try
                        If Not objWin_AC.IsDisposed Then
                            objWin_AC.Close()
                            objWin_AC.Dispose()
                        End If
                    Catch : End Try
                End If
            Catch : End Try

            ProgBarGui_AutoCast.Instance = Nothing
            _osGui_AutoCastProgress = Nothing

            _autoCastThreadId = 0

            SyncLock _startupLock
                _autoCastThread = Nothing
            End SyncLock

            ' If the TCS hasn't been completed (e.g., thread ended before UI created), ensure it completes.
            If tcs IsNot Nothing AndAlso Not tcs.Task.IsCompleted Then
                tcs.TrySetResult(False)
            End If
        End Try
    End Sub

    ' Call this to start the background UI/thread
    Public Shared Sub StartAutoCast()
        SyncLock _startupLock
            If _autoCastThread IsNot Nothing AndAlso _autoCastThread.IsAlive Then
                Return
            End If

            ' Dispose previous CTS if any
            Try
                _autoCastCts?.Dispose()
            Catch : End Try

            _autoCastCts = New CancellationTokenSource()
            _startupEvent.Reset()

            ' Create the thread but don't run heavy work inside the lock.
            _autoCastThread = New Thread(AddressOf AutoCastThreadProc1) With {
            .IsBackground = True
        }
            _autoCastThread.SetApartmentState(ApartmentState.STA)
            _autoCastThread.Start()
        End SyncLock
    End Sub

    ' The actual thread body is a named method (no outer captures)
    Private Shared Sub AutoCastThreadProc1()
        ' Remember thread id for outside usage (GetCurrentThreadId is assumed defined elsewhere)
        Try
            _autoCastThreadId = GetCurrentThreadId()

            Dim cts = _autoCastCts ' local reference (may be Nothing if disposed concurrently)
            Dim token As CancellationToken = If(cts?.Token, CancellationToken.None)

            ' Fetch sizing and other pre-work on this thread (or do it before starting thread if preferred)
            Dim progSize = CoreDataLib.FetchProgSizeReport(TriggerType.AutoCast, True)
            Dim pW = progSize.Item("pW")
            Dim pH = progSize.Item("pH")

            Dim objContextAC As New ApplicationContext()

            Dim objWin_AC As ProgBarGui_AutoCast = Nothing

            Try
                objWin_AC = New ProgBarGui_AutoCast(pW, pH, osFuncLib_Progress.ProgTimeSpan_AC, AddressOf EaseProgress)

                _osGui_AutoCastProgress = New AutoCastGui()
                ProgBarGui_AutoCast.Instance = objWin_AC

                AddHandler objWin_AC.FormClosed, Sub()
                                                     Try
                                                         objContextAC.ExitThread()
                                                     Catch ex As Exception
                                                         ' optional: log ex
                                                     End Try
                                                 End Sub

                ' signal caller that the UI has been created and started (if somebody is waiting)
                _startupEvent.Set()

                ' Run the WinForms message loop on this STA thread
                osRunForm.Run(objContextAC)

            Catch ex As Exception
                ' TODO: log the exception so crashes are visible
                ' Example: Logger.LogError(ex, "AutoCast thread failed")
            Finally
                ' Clean up UI references and free resources
                Try
                    If objWin_AC IsNot Nothing Then
                        RemoveHandler objWin_AC.FormClosed, Nothing ' no-op removal pattern; if you used a named handler remove it explicitly
                        If Not objWin_AC.IsDisposed Then
                            Try
                                objWin_AC.Close()
                                objWin_AC.Dispose()
                            Catch : End Try
                        End If
                    End If
                Catch : End Try

                ProgBarGui_AutoCast.Instance = Nothing
                _osGui_AutoCastProgress = Nothing

                ' Reset thread id and clear thread reference atomically
                _autoCastThreadId = 0
                SyncLock _startupLock
                    _autoCastThread = Nothing
                End SyncLock

                ' Dispose CTS here if it is not used elsewhere
                Try
                    If cts IsNot Nothing Then
                        cts.Dispose()
                    End If
                Catch : End Try
            End Try
        Finally
            ' ensure startupEvent is set in case of unhandled early exit so waiters don't block forever
            Try
                _startupEvent.Set()
            Catch : End Try
        End Try
    End Sub

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

                        ProgBarGui_AutoCast.Instance = Nothing
                        _osGui_AutoCastProgress = Nothing
                        _autoCastThreadId = 0
                    End With
                End Sub)

            With _autoCastThread
                .SetApartmentState(ApartmentState.STA)
                .IsBackground = True
                .Start()
            End With

        End SyncLock
    End Sub

    Public Shared Function StopAutoCastGui(Optional timeoutMs As Integer = 5000) As Boolean
        SyncLock _startupLock
            If _autoCastThread Is Nothing Then
                Return True
            End If

            Try
                Dim objAcInstance = ProgBarGui_AutoCast.Instance

                If ValidateInstance(objAcInstance) Then
                    Try
                        objAcInstance.BeginInvoke(New Action(
                                Sub()
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

            _autoCastCts = Nothing
            _autoCastThread = Nothing
            _osGui_AutoCastProgress = Nothing
            ProgBarGui_AutoCast.Instance = Nothing
            _startupEvent.Reset()
            _autoCastThreadId = 0

            Return True
        End SyncLock
    End Function

    Private Shared Function ValidateInstance(objInstance As ProgBarGui_AutoCast) As Boolean
        Return objInstance IsNot Nothing AndAlso Not objInstance.IsDisposed
    End Function

    Public Shared ReadOnly Property AutoCast_UI As AutoCastGui
        Get
            Return _osGui_AutoCastProgress
        End Get
    End Property

    <DllImport("kernel32.dll")>
    Private Shared Function GetCurrentThreadId() As Integer : End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function PostThreadMessage(dwThreadId As Integer, Msg As Integer,
                                              wParam As IntPtr, lParam As IntPtr) As Boolean : End Function

End Class
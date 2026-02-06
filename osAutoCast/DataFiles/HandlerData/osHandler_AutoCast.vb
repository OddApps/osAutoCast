Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports osRunForm = System.Windows.Forms.Application
Imports osAutoCast.osFuncLib_Progress
Imports osAutoCast.CoreDataLib
Imports System.Threading.Interlocked

Public NotInheritable Class osHandler_AutoCast

    Private Shared _osGui_AutoCastProgress As AutoCastGui = Nothing
    Private Shared _autoCastProgress As ProgBarGui_AutoCast = Nothing

    Private Shared _autoCastCts As CancellationTokenSource = Nothing
    Private Shared _autoCastTcs As TaskCompletionSource(Of Boolean) = Nothing

    Private Shared _autoCastTask As Task = Nothing
    Private Shared _uiContext As WindowsFormsSynchronizationContext = Nothing

    Public Shared ReadOnly Property AutoCast_UI As AutoCastGui
        Get
            Return _osGui_AutoCastProgress
        End Get
    End Property

    Public Shared Async Function StartAutoCastAsync() As Task(Of Boolean)
        Dim chkExisting = _autoCastTcs

        If chkExisting IsNot Nothing Then
            Return Await chkExisting.Task.ConfigureAwait(False)
        End If

        Dim objTask_ProgSize = Task.Run(
            Function()
                Return FetchProgSizeReport(TriggerType.AutoCast, True)
            End Function)

        InitAutoCastTask(_autoCastCts, _autoCastTcs)

        Dim acProgSize = Await objTask_ProgSize

        Dim pW = acProgSize("pW")
        Dim pH = acProgSize("pH")

        _autoCastTask = Task.Run(
            Sub()
                RunAutoCastUiLoop(pW, pH, _autoCastCts, _autoCastTcs)
            End Sub)

        Return Await _autoCastTcs.Task.ConfigureAwait(False)
    End Function

    Private Shared Sub RunAutoCastUiLoop(pW As Integer, pH As Integer, cts As CancellationTokenSource, tcs As TaskCompletionSource(Of Boolean))
        Dim syncContext = New WindowsFormsSynchronizationContext()

        SynchronizationContext.SetSynchronizationContext(syncContext)
        _uiContext = syncContext

        Dim context As New ApplicationContext()
        Dim win As ProgBarGui_AutoCast = Nothing

        Try
            win = New ProgBarGui_AutoCast(pW, pH,
                                          ProgTimeSpan_AC, AddressOf EaseProgress)

            _osGui_AutoCastProgress = New AutoCastGui()
            ProgBarGui_AutoCast.Instance = win

            AddHandler win.FormClosed,
                Sub()
                    Try
                        context.ExitThread()
                    Catch : End Try
                End Sub

            ' Signal: UI is alive
            tcs.TrySetResult(True)

            ' Message loop
            osRunForm.Run(context)

        Catch ex As Exception
            tcs.TrySetException(ex)

        Finally
            Try
                If win IsNot Nothing AndAlso Not win.IsDisposed Then
                    win.Dispose()
                End If
            Catch
            End Try

            ProgBarGui_AutoCast.Instance = Nothing
            _osGui_AutoCastProgress = Nothing

            _uiContext = Nothing
            _autoCastTask = Nothing
            _autoCastTcs = Nothing

            If Not tcs.Task.IsCompleted Then
                tcs.TrySetResult(False)
            End If
        End Try
    End Sub

    Public Shared Async Function StopAutoCastAsync() As Task(Of Boolean)

        Dim task = _autoCastTask
        If task Is Nothing Then Return True

        Dim ctx = _uiContext
        Dim cts = _autoCastCts

        ' Request cancellation
        Try
            cts?.Cancel()
        Catch
        End Try

        ' Ask UI to close on its own context
        If ctx IsNot Nothing Then
            ctx.Post(
            Sub()
                Try
                    Dim win = ProgBarGui_AutoCast.Instance
                    If win IsNot Nothing AndAlso Not win.IsDisposed Then
                        win.Close()
                    End If
                Catch
                End Try
            End Sub,
            Nothing)
        End If

        ' Await task completion (no blocking, no Join)
        Await task.ConfigureAwait(False)

        Try
            cts?.Dispose()
        Catch
        End Try

        Return True
    End Function

    Private Shared Sub InitAutoCastTask(ByRef objAbortToken As CancellationTokenSource, ByRef objTaskSource As TaskCompletionSource(Of Boolean))
        objAbortToken = New CancellationTokenSource()
        objTaskSource = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.RunContinuationsAsynchronously)
    End Sub

    Private Shared Function ValidateInstance(objInstance As ProgBarGui_AutoCast) As Boolean
        Return objInstance IsNot Nothing AndAlso Not objInstance.IsDisposed
    End Function

    Private Shared Function ValidateThread(objThread As Thread) As Boolean
        Return objThread IsNot Nothing AndAlso objThread.IsAlive
    End Function

    Private Shared Function ValidateThread(objThread As Thread, chkDisposed As Boolean) As Boolean
        Return objThread Is Nothing OrElse (Not objThread.IsAlive)
    End Function

    Private Const WM_QUIT As Integer = &H12

    <DllImport("kernel32.dll")>
    Private Shared Function GetCurrentThreadId() As Integer : End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function PostThreadMessage(dwThreadId As Integer, Msg As Integer,
                                              wParam As IntPtr, lParam As IntPtr) As Boolean : End Function

End Class
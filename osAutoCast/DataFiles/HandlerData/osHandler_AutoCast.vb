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
                Return GetSizeReport(TriggerType.AutoCast)
            End Function)

        InitAutoCastTask(_autoCastCts, _autoCastTcs)

        Dim acProgSize = Await objTask_ProgSize

        _autoCastTask = Task.Run(
            Sub()
                RunAutoCastUiLoop(acProgSize.pWidth, acProgSize.pHeight, acProgSize.pBorder,
                                  _autoCastCts, _autoCastTcs)
            End Sub)

        Return Await _autoCastTcs.
            Task.ConfigureAwait(False)
    End Function

    Private Shared Sub RunAutoCastUiLoop(pW As Integer, pH As Integer, pB As Integer, cts As CancellationTokenSource, tcs As TaskCompletionSource(Of Boolean))
        Dim syncContext = New WindowsFormsSynchronizationContext()
        SynchronizationContext.SetSynchronizationContext(syncContext)

        _uiContext = syncContext

        Dim objUiContext As New ApplicationContext()
        Dim objAcInstance As ProgBarGui_AutoCast = Nothing

        Try
            objAcInstance = New ProgBarGui_AutoCast(pW, pH, pB,
                                          ProgTimeSpan_AC, AddressOf EaseProgress)

            _osGui_AutoCastProgress = New AutoCastGui()
            ProgBarGui_AutoCast.Instance = objAcInstance

            AddHandler objAcInstance.FormClosed,
                Sub()
                    Try
                        objUiContext.ExitThread()
                    Catch : End Try
                End Sub

            tcs.TrySetResult(True)

            osRunForm.Run(objUiContext)
        Catch ex As Exception
            tcs.TrySetException(ex)
        Finally
            Try
                If objAcInstance IsNot Nothing AndAlso Not objAcInstance.IsDisposed Then
                    objAcInstance.Dispose()
                End If
            Catch : End Try

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
        Dim objAutoCastTask = _autoCastTask
        If objAutoCastTask Is Nothing Then Return True

        Dim ctx = _uiContext
        Dim cts = _autoCastCts

        Try
            cts?.Cancel()
        Catch
        End Try

        If ctx IsNot Nothing Then
            ctx.Post(
                Sub()
                    Try
                        Dim objAcInstance = ProgBarGui_AutoCast.Instance

                        If objAcInstance IsNot Nothing AndAlso Not objAcInstance.IsDisposed Then
                            objAcInstance.Close()
                        End If
                    Catch : End Try
                End Sub, Nothing)
        End If

        Await objAutoCastTask.ConfigureAwait(False)

        Try
            cts?.Dispose()
        Catch : End Try

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
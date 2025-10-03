Imports System.Reactive.Linq
Imports System.Reactive.Subjects
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Forms
Imports System.Windows.Threading

Public Class InputMonitorService
    Implements IDisposable

    <DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Integer) As Short
    End Function

    Private Const VK_LBUTTON As Integer = &H1
    Private Const VK_RBUTTON As Integer = &H2

    Private Const VK_SHIFT As Integer = Keys.ShiftKey
    Private Const VK_ALT As Integer = &H12
    Private Const VK_O As Integer = Keys.O
    Private Const VK_C As Integer = Keys.C

    Private Shared ReadOnly InputActionIdx As New Dictionary(Of InputAction, DetectOpts) From {
        {InputAction.AC_Start, DetectOpts.MonitorShift},
        {InputAction.AC_RTC, DetectOpts.MonitorMouse},
        {InputAction.AP_Start, DetectOpts.MonitorMouseR},
        {InputAction.AP_Exec, DetectOpts.MonitorShift}
    }

    Private InputMonitorAbortSrc As CancellationTokenSource
    Private Shared InputMon_Support As IDisposable
    Private Shared InputMon_Observer As IDisposable
    Private Shared TriggerCmd As New Subject(Of DataTypeLib.TriggerAction)
    Private Shared _MonitorState As DataTypeLib.MonitorStatus

    Private Shared ReadOnly TriggerBindings As (TriggerCondition As Func(Of Boolean), TriggerHandler As DataTypeLib.TriggerAction)() = {
            (Function() CmdBind_AutoCast(), DataTypeLib.TriggerAction.AutoCast),
            (Function() CmdBind_ShowOpts(), DataTypeLib.TriggerAction.ShowOpts),
            (Function() CmdBind_AutoPass(), DataTypeLib.TriggerAction.AutoPass)
        }

    Public Shared Property InputTriggerActions As IObservable(Of DataTypeLib.TriggerAction)
        Get
            Return TriggerCmd
        End Get
        Set(value As IObservable(Of DataTypeLib.TriggerAction))
            TriggerCmd = DirectCast(value, Subject(Of DataTypeLib.TriggerAction))
        End Set
    End Property

    Public Shared Property MonitorState As DataTypeLib.MonitorStatus
        Get
            Return _MonitorState
        End Get
        Set(value As DataTypeLib.MonitorStatus)
            _MonitorState = value
        End Set
    End Property

    Private Shared Sub SetMonitorState(setMonStatus As MonitorStatus)
        MonitorState = setMonStatus
    End Sub

    Public Sub SelectState(setMonStatus As MonitorStatus)
        SetMonitorState(setMonStatus)
    End Sub

    Private Shared Function GetMonitorState() As MonitorStatus
        Return MonitorState
    End Function

    Public Function GetState() As MonitorStatus
        Return GetMonitorState()
    End Function

    Public Sub New()
        SetMonitorState(MonitorStatus.Starting)
    End Sub

    Public Sub New(Optional AutoLaunchMonitor As Boolean = False)
        SetMonitorState(MonitorStatus.Starting)

        If AutoLaunchMonitor Then
            LaunchTriggerMonitor()
        End If
    End Sub


    Private Shared Function CmdBind_AutoCast() As Boolean
        Return InputMon_ShiftDown() AndAlso InputMon_MouseDown()
    End Function

    Private Shared Function CmdBind_AutoPass() As Boolean
        Return InputMon_ShiftDown() AndAlso InputMon_MouseDown(True)
    End Function

    Private Shared Function CmdBind_ShowOpts() As Boolean
        Return InputMon_ShiftDown() AndAlso InputMon_AltDown() AndAlso InputMon_ODown()
    End Function

    Private Shared Function InputMon_ShiftDown() As Boolean
        Return (GetAsyncKeyState(VK_SHIFT) And &H8000) <> 0
    End Function

    Private Shared Function InputMon_MouseDown() As Boolean
        Return (GetAsyncKeyState(VK_LBUTTON) And &H8000) <> 0
    End Function

    Private Shared Function InputMon_MouseDown(isClkR As Boolean) As Boolean
        Return (GetAsyncKeyState(VK_RBUTTON) And &H8000) <> 0
    End Function

    Private Shared Function InputMon_AltDown() As Boolean
        Return (GetAsyncKeyState(VK_ALT) And &H8000) <> 0
    End Function

    Private Shared Function InputMon_ODown() As Boolean
        Return (GetAsyncKeyState(VK_O) And &H8000) <> 0
    End Function

    Private Shared Function InputMon_CDown() As Boolean
        Return (GetAsyncKeyState(VK_C) And &H8000) <> 0
    End Function

    Public Shared Function isAutoPassCancelled() As Boolean
        Return InputMon_CDown()
    End Function

    Public Async Function AnticipateInput(inputType As TriggerType, Optional initAction As Boolean = False) As Task(Of Boolean)
        Return Await InputDetection(inputType, initAction)
    End Function

    Public Async Function AnticipateInput(inAction As InputAction) As Task(Of Boolean)
        Return Await HoldForAction(inAction)
    End Function

    Private Shared Function SelAction(initAction As Boolean) As DetectOpts
        Return If(initAction, DetectOpts.MonitorShift,
            DetectOpts.MonitorMouse)
    End Function

    Private Shared Function SelAction(inAction As InputAction) As DetectOpts
        Return InputActionIdx(inAction)
    End Function

    Private Shared Async Function HoldForAction(inAction As DataTypeLib.InputAction) As Task(Of Boolean)
        While CoreDataLib.InputMonSvc.DetectTrigger(SelAction(inAction))
            If inAction = DataTypeLib.InputAction.AP_Exec AndAlso isAutoPassCancelled() Then
                Return False
            End If
            Await Task.Delay(10)
        End While
        Return True
    End Function

    Private Shared Async Function InputDetection(inAction As InputAction) As Task(Of Boolean)
        'Dim chkInput As Boolean

        'Await HoldForAction(inAction)

        'Select Case inputType
        '    Case TriggerType.AutoCast

        '        While InputMonSvc.DetectTrigger(SelAction(initAction))
        '            Await Task.Delay(10)
        '        End While
        '    Case TriggerType.AutoPass
        '        If initAction Then
        '            While InputMonSvc.DetectTrigger(DetectOpts.MonitorMouseR)
        '                Await Task.Delay(10)
        '            End While

        '            chkInput = True
        '        Else

        '            While InputMonSvc.DetectTrigger(DetectOpts.MonitorShift)
        '                If isAutoPassCancelled() Then
        '                    chkInput = False
        '                End If
        '                Await Task.Delay(10)
        '            End While

        '            chkInput = True
        '        End If
        'End Select

        'Return chkInput
    End Function

    Private Shared Async Function InputDetection(inputType As DataTypeLib.TriggerType, Optional initAction As Boolean = False) As Task(Of Boolean)
        Dim chkInput As Boolean
        Select Case inputType
            Case DataTypeLib.TriggerType.AutoCast
                While CoreDataLib.InputMonSvc.DetectTrigger(SelAction(initAction))
                    Await Task.Delay(10)
                End While
                chkInput = True
            Case DataTypeLib.TriggerType.AutoPass
                If initAction Then
                    While CoreDataLib.InputMonSvc.DetectTrigger(DataTypeLib.DetectOpts.MonitorMouseR)
                        Await Task.Delay(10)
                    End While
                    chkInput = True
                Else
                    While CoreDataLib.InputMonSvc.DetectTrigger(DataTypeLib.DetectOpts.MonitorShift)
                        If isAutoPassCancelled() Then chkInput = False
                        Await Task.Delay(10)
                    End While
                    chkInput = True
                End If
        End Select
        Return chkInput
    End Function

    Private Shared Sub ActivateTriggerMonitor()
        InputMon_Observer = Observable.Interval(TimeSpan.FromMilliseconds(100)).
            Select(Function(chkDuration) EvalInputActionInternal()).
            Where(Function(getTrigger) getTrigger <> TriggerAction.None).
            Subscribe(Sub(taskTrigger) TriggerCmd.OnNext(taskTrigger))
    End Sub



    Private Shared Function EvalInputActionInternal() As TriggerAction
        Return TriggerBindings.
            FirstOrDefault(Function(evalTrigger)
                               Return evalTrigger.TriggerCondition()
                           End Function, (Nothing, TriggerAction.None)).TriggerHandler
    End Function

    Public Sub LaunchTriggerMonitor()
        StartTriggerMonitor()
    End Sub

    Private Shared Sub EstablishTriggerMonitor(ByRef objMonitor As IDisposable)
        Dim disposable = InputTriggerActions.Subscribe(
                Async Sub(objInputAction)
                    If objInputAction <> DataTypeLib.TriggerAction.None Then
                        SuspendMonitoring()
                        Try
                            Await CoreDataLib.ExecuteTrigger(objInputAction)
                        Finally
                            StartTriggerMonitor()
                        End Try
                    End If
                End Sub)
        objMonitor = disposable
    End Sub

    Private Shared Sub StartTriggerMonitor()
        ActivateTriggerMonitor()

        SetMonitorState(MonitorStatus.Watching)
        EstablishTriggerMonitor(InputMon_Support)
    End Sub

    Public Function DetectTrigger(Optional DetectMode As DetectOpts = DetectOpts.MonitorAll) As Boolean
        Return InputTriggerDetected(DetectMode)
    End Function

    Public Shared Function InputTriggerDetected(Optional DetectMode As DataTypeLib.DetectOpts = DataTypeLib.DetectOpts.MonitorAll) As Boolean
        Select Case DetectMode
            Case DataTypeLib.DetectOpts.MonitorMouse
                Return InputMon_MouseDown()
            Case DataTypeLib.DetectOpts.MonitorMouseR
                Return InputMon_MouseDown(True)
            Case DataTypeLib.DetectOpts.MonitorShift
                Return InputMon_ShiftDown()
            Case DataTypeLib.DetectOpts.MonitorAll
                Return CmdBind_AutoCast()
            Case Else
                Return InputMon_ShiftDown()
        End Select
    End Function

    Public Shared Function InputTriggerDetected3(Optional DetectMode As DetectOpts = DetectOpts.MonitorAll) As Boolean

        Return CmdBind_AutoCast() OrElse CmdBind_ShowOpts()

    End Function

    Public Shared Sub SuspendMonitoring()
        If InputMon_Support IsNot Nothing Then
            InputMon_Support.Dispose()
            InputMon_Support = Nothing
        End If

        If InputMon_Observer IsNot Nothing Then
            InputMon_Observer.Dispose()
            InputMon_Observer = Nothing
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        SuspendMonitoring()
    End Sub

End Class
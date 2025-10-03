Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Windows.Threading

Public NotInheritable Class CoreDataLib

    Private Sub New()
    End Sub

    Private Shared isDebug As Boolean = False

    Public Shared osTrayIcon As Forms.NotifyIcon
    Public Shared osTrayMenu As Forms.ContextMenuStrip

    Public Shared osAppDataDir As String = Environment.GetFolderPath(CType(26, Environment.SpecialFolder))
    Public Shared osRootDir As String = AppDomain.CurrentDomain.BaseDirectory
    Public Shared osPrefDir As String = Path.Combine(osAppDataDir, "osData", "Data")
    Public Shared osPrefFile As String = Path.Combine(osPrefDir, "prefSettings.osps")
    Public Shared osPrefIndex As PrefRecordIndex
    Public Shared osPrefStoreData As osPrefStore
    Public Shared isDispPref As Boolean = False

    Public Const mEvent_Down As Integer = 2
    Public Const mEvent_Up As Integer = 4

    Public Shared chkActionAbort As CancellationTokenSource
    Public Shared objCancelState As CancellationToken
    Public Shared InputMonSvc As InputMonitorService = Nothing

    Public Shared TriggerHandlers As (DataTypeLib.TriggerAction, Func(Of Task))() = {
            (DataTypeLib.TriggerAction.AutoCast, Function() osFuncLib_AutoCast.ExecuteAutoCast()),
            (DataTypeLib.TriggerAction.AutoPass, Function() osFuncLib_AutoPass.ExecuteAutoPass()),
            (DataTypeLib.TriggerAction.ShowOpts, Function() osFuncLib_ShowOpts.ExecuteDispOpts())
        }

    Private Shared ib As Integer = 0

    Public Shared Function GetFuse() As Integer
        Return osPrefStoreData.AutoCast_Fuse
    End Function

    Public Shared Function isRTC() As Boolean
        Return osPrefStoreData.AutoCast_RTC
    End Function

    Public Shared Function GetProgSize(isProgType As DataTypeLib.TriggerType, Optional getH As Boolean = False) As Integer
        Select Case isProgType
            Case DataTypeLib.TriggerType.AutoCast
                Return If(getH, osPrefStoreData.MainOpts_acProgH, osPrefStoreData.MainOpts_acProgW)
            Case DataTypeLib.TriggerType.AutoPass
                Return If(getH, osPrefStoreData.MainOpts_apProgH, osPrefStoreData.MainOpts_apProgW)
            Case Else
                Return 0
        End Select
    End Function

    Public Shared Function FetchProgSizeReport(isProgType As DataTypeLib.TriggerType) As Dictionary(Of String, Integer)
        Select Case isProgType
            Case DataTypeLib.TriggerType.AutoCast
                Return New Dictionary(Of String, Integer) From {
                        {"pH", osPrefStoreData.MainOpts_acProgH},
                        {"pW", osPrefStoreData.MainOpts_acProgW}
                    }
            Case DataTypeLib.TriggerType.AutoPass
                Return New Dictionary(Of String, Integer) From {
                        {"pH", osPrefStoreData.MainOpts_apProgH},
                        {"pW", osPrefStoreData.MainOpts_apProgW}
                    }
            Case Else
                Return Nothing
        End Select
    End Function

    Public Shared Function GetSafetyTimer() As Integer
        Return osPrefStoreData.AutoPass_SafetyTimer
    End Function

    Public Shared Function ChkExecPermission() As Boolean
        If Not IsDebugBuild() Then
            Return DetectGameUI.FocusMTGA()
        Else
            Return Not DetectGameUI.FocusMTGA()
        End If
    End Function

    Public Shared Function ChkExecPermission(tType As DataTypeLib.TriggerAction) As Boolean
        If Not IsDebugBuild() Then
            Return DetectGameUI.FocusMTGA()
        Else
            Return Not DetectGameUI.FocusMTGA()
        End If
    End Function

    Public Shared Function IsDebugBuild() As Boolean
        Return True
        'Dim customAttribute As DebuggableAttribute =
        '        CType(Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), GetType(DebuggableAttribute)), DebuggableAttribute)
        'Return customAttribute IsNot Nothing AndAlso customAttribute.IsJITTrackingEnabled
    End Function

    Public Shared Function IsDebugBuild(isOld As Boolean) As Boolean
        Dim attr = CType(Attribute.GetCustomAttribute(
        Assembly.GetExecutingAssembly(),
        GetType(DebuggableAttribute)), DebuggableAttribute)

        Return attr IsNot Nothing AndAlso attr.IsJITTrackingEnabled
    End Function

    Public Shared Function SetGameFocus() As Boolean
        Return DetectGameUI.FocusMTGA(True)
    End Function

    Public Shared Async Function ExecuteTriggerCmd(tType As DataTypeLib.TriggerType) As Task
        If osFuncLib_InputScan.isActionTriggered() Then Return
        PrepTrigger(tType)
        Select Case tType
            Case DataTypeLib.TriggerType.AutoCast
                Await osFuncLib_AutoCast.ExecuteAutoCast()
            Case DataTypeLib.TriggerType.AutoPass
                Await osFuncLib_AutoPass.ExecuteAutoPass()
            Case DataTypeLib.TriggerType.ShowPrefs
                Await osFuncLib_ShowOpts.ExecuteDispOpts()
        End Select
        ResolveAction()
    End Function

    Private Shared Sub ResolveAction()
        If Not osFuncLib_InputScan.isActionComplete Then Return
        osFuncLib_InputScan.isActionComplete = False
    End Sub

    Private Shared Sub ResetStatus()
        InputMonSvc.SelectState(DataTypeLib.MonitorStatus.Watching)
    End Sub

    Public Shared Sub PrepTrigger(pType As DataTypeLib.TriggerType)
        osFuncLib_InputScan.SetMonitorState(DataTypeLib.MonitorStatus.InCmd)
        If pType = DataTypeLib.TriggerType.ShowPrefs Then Return
        osFuncLib_Progress.SetProgStatus(DataTypeLib.ProgAction.Activate, pType)
        StartCancelWatcher(pType)
    End Sub

    Public Shared Async Function ExecuteTrigger(tType As DataTypeLib.TriggerAction) As Task
        If ValidateTrigger(CType(tType, DataTypeLib.TriggerType)) Then
            InputMonSvc.SelectState(DataTypeLib.MonitorStatus.InCmd)
            Dim func = TriggerHandlers.FirstOrDefault(Function(th) th.Item1 = tType).Item2
            If func IsNot Nothing Then
                Await func()
            End If
        End If
        ResolveAction()
    End Function

    Public Shared Function ValidateTrigger(pType As DataTypeLib.TriggerType) As Boolean
        If pType = DataTypeLib.TriggerType.ShowPrefs Then
            Return True
        ElseIf ChkExecPermission() Then
            osFuncLib_Progress.SetProgStatus(DataTypeLib.ProgAction.Activate, pType)
            StartCancelWatcher(pType)
            Return True
        Else
            Return False
        End If
    End Function

    Private Shared Function StartCancelMonitor(cts As CancellationTokenSource,
                                                  Optional chkType As DataTypeLib.TriggerType = DataTypeLib.TriggerType.AutoCast) As Task
        Return Task.Run(Sub() MonitorForCancel(cts, chkType))
    End Function

    Private Shared Sub StartCancelWatcher(Optional chkType As DataTypeLib.TriggerType = DataTypeLib.TriggerType.AutoCast)
        SetCT()
        StartCancelMonitor(chkActionAbort, chkType)
    End Sub

    Private Shared Async Sub MonitorForCancel(cts As CancellationTokenSource,
                                                 Optional pType As DataTypeLib.TriggerType = DataTypeLib.TriggerType.AutoCast)
        While Not cts.Token.IsCancellationRequested
            If pType = DataTypeLib.TriggerType.AutoCast Then
                If Not InputMonSvc.DetectTrigger(DataTypeLib.DetectOpts.MonitorMouse) Then
                    cts.Cancel()
                    Exit While
                End If
            ElseIf pType = DataTypeLib.TriggerType.AutoPass AndAlso
                       Not InputMonSvc.DetectTrigger(DataTypeLib.DetectOpts.MonitorShift) Then
                cts.Cancel()
                Exit While
            End If
            Await Task.Delay(5)
        End While
    End Sub

    Private Shared Sub SetCT()
        chkActionAbort = New CancellationTokenSource()
        objCancelState = chkActionAbort.Token
    End Sub

    Public Shared Sub ProcessProgressEvent2(pMode As DataTypeLib.ProgMode,
                                               pEvent As DataTypeLib.ProgEvent,
                                               ParamArray pEventData() As Object)

        Dim pEventElement As OddLib_ProgressBar = Nothing
        Dim strEventData As String = ""

        Try
            strEventData = pEventData(0).ToString()
        Catch ex As Exception

        End Try

        Dim progressEvent As ProgressEvent = PrepareProgEvent(pEventElement)
        progressEvent.evDispatch.Invoke(Sub()
                                            progressEvent.evAction(GenerateProgEventData(pEvent,
                                                                                            DataTypeLib.TriggerType.AutoCast,
                                                                                            strEventData))
                                        End Sub)
    End Sub

    Public Shared Sub ProcessProgressEvent(pMode As ProgMode, pEvent As ProgEvent, ParamArray pEventData() As Object)
        Dim objProgEventType As TriggerType = Nothing

        Dim osProgElement As OddLib_ProgressBar =
            Function() As OddLib_ProgressBar
                Select Case pMode
                    Case ProgMode.AutoCast
                        objProgEventType = TriggerType.AutoCast
                        Return osHandler_GUI.osGui_AutoCast.OddProgBar1
                    Case ProgMode.AutoPass
                        objProgEventType = TriggerType.AutoPass
                        Return osHandler_GUI.osGui_AutoPass.OddProgBar_AP
                    Case Else
                        Return Nothing
                End Select
            End Function.Invoke()

        Dim strEventData As String = ""

        Try
            strEventData = pEventData(0).ToString()
        Catch ex As Exception

        End Try

        With PrepareProgEvent(osProgElement)
            .evDispatch.Invoke(Sub()
                                   .evAction(GenerateProgEventData(pEvent, objProgEventType,
                                                                   strEventData))
                               End Sub)
        End With
    End Sub

    Private Shared Function GenerateProgEventData(pEvent As DataTypeLib.ProgEvent,
                                                      pType As DataTypeLib.TriggerType,
                                                      Optional pDispText As String = "") As ProgressEventData
        Return New ProgressEventData(pEvent, pType, pDispText)
    End Function

    Private Shared Function PrepareProgEvent(pEventElement As OddLib_ProgressBar) As ProgressEvent
        Return New ProgressEvent(pEventElement)
    End Function

End Class

'Module CoreDataLib

'    Private isDebug As Boolean = False
'    ' Private isDebug As Boolean = True

'    Public osTrayIcon As Forms.NotifyIcon
'    Public osTrayMenu As Forms.ContextMenuStrip

'    Public osAppDataDir As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

'    Public osRootDir As String = AppDomain.CurrentDomain.BaseDirectory
'    Public osPrefDir As String = Path.Combine(osAppDataDir, "osData", "Data")
'    Public osPrefFile As String = Path.Combine(osPrefDir, "prefSettings.osps")

'    Public osPrefIndex As PrefRecordIndex
'    Public osPrefStoreData As osPrefStore

'    Public isDispPref As Boolean = False

'    Public Const mEvent_Down As Integer = &H2
'    Public Const mEvent_Up As Integer = &H4

'    Public chkActionAbort As CancellationTokenSource
'    Public objCancelState As CancellationToken

'    Public InputMonSvc As InputMonitorService = Nothing

'    Private TriggerHandlers As (HandleAction As TriggerAction,
'        HandleEvent As Func(Of Task))() = {
'            (TriggerAction.AutoCast, Function() ExecuteAutoCast()),
'            (TriggerAction.AutoPass, Function() ExecuteAutoPass()),
'            (TriggerAction.ShowOpts, Function() ExecuteDispOpts())
'        }

'    Public Function GetFuse() As Integer
'        Return osPrefStoreData.AutoCast_Fuse
'    End Function

'    Public Function isRTC() As Boolean
'        Return osPrefStoreData.AutoCast_RTC
'    End Function

'    Public Function GetProgSize(isProgType As TriggerType, Optional getH As Boolean = False) As Integer
'        Select Case isProgType
'            Case TriggerType.AutoCast
'                Return If(getH, osPrefStoreData.MainOpts_acProgH,
'                    osPrefStoreData.MainOpts_acProgW)
'            Case TriggerType.AutoPass
'                Return If(getH, osPrefStoreData.MainOpts_apProgH,
'                    osPrefStoreData.MainOpts_apProgW)
'            Case Else
'                Return Nothing
'        End Select
'    End Function

'    Public Function FetchProgSizeReport(isProgType As TriggerType) As Dictionary(Of String, Integer)
'        Select Case isProgType
'            Case TriggerType.AutoCast
'                Return New Dictionary(Of String, Integer) From {
'                    {"pH", osPrefStoreData.MainOpts_acProgH},
'                    {"pW", osPrefStoreData.MainOpts_acProgW}
'                }
'            Case TriggerType.AutoPass
'                Return New Dictionary(Of String, Integer) From {
'                    {"pH", osPrefStoreData.MainOpts_apProgH},
'                    {"pW", osPrefStoreData.MainOpts_apProgW}
'                }
'            Case Else
'                Return Nothing
'        End Select
'    End Function

'    Public Function GetSafetyTimer() As Integer
'        Return osPrefStoreData.AutoPass_SafetyTimer
'    End Function

'    Public Function ChkExecPermission() As Boolean
'        Return If(IsDebugBuild(), Not DetectGameUI.FocusMTGA,
'            DetectGameUI.FocusMTGA)
'    End Function

'    Public Function ChkExecPermission(tType As TriggerAction) As Boolean

'        Return If(IsDebugBuild(), Not DetectGameUI.FocusMTGA,
'            DetectGameUI.FocusMTGA)
'    End Function

'    Public Function IsDebugBuild() As Boolean
'        Dim attr = CType(Attribute.GetCustomAttribute(
'        Assembly.GetExecutingAssembly(),
'        GetType(DebuggableAttribute)), DebuggableAttribute)

'        Return attr IsNot Nothing AndAlso attr.IsJITTrackingEnabled
'    End Function

'    Public Function SetGameFocus() As Boolean
'        Return DetectGameUI.FocusMTGA(True)
'    End Function
'    Dim ib As Integer = 0

'    'Public Async Function ExecuteTriggerCmd(tType As TriggerType) As Task
'    '    If osFuncLib_InputScan.isActionTriggered() Then Exit Function

'    '    If ValidateTrigger(tType) Then

'    '        Select Case tType
'    '            Case TriggerAction.AutoCast
'    '                Await ExecuteAutoCast()
'    '            Case TriggerAction.AutoPass
'    '                Await ExecuteAutoPass()
'    '            Case TriggerAction.ShowOpts
'    '                Await ExecuteDispOpts()
'    '        End Select

'    '        ResolveAction()
'    '    End If
'    'End Function

'    Public Async Function ExecuteTriggerCmd(tType As TriggerType) As Task
'        If osFuncLib_InputScan.isActionTriggered() Then Exit Function

'        PrepTrigger(tType)

'        Select Case tType
'            Case TriggerAction.AutoCast
'                Await ExecuteAutoCast()
'            Case TriggerAction.AutoPass
'                Await ExecuteAutoPass()
'            Case TriggerAction.ShowOpts
'                Await ExecuteDispOpts()
'        End Select

'        ResolveAction()
'    End Function

'    Private Sub ResolveAction()
'        If osFuncLib_InputScan.isActionComplete Then osFuncLib_InputScan.isActionComplete = False

'        '  ResetStatus()
'    End Sub

'    Private Sub ResetStatus()
'        '  SetProgStatus(ProgAction.Reset, TriggerType.AutoCast)
'        'osFuncLib_InputScan.SetMonitorState(MonitorStatus.Watching)
'        InputMonSvc.SelectState(MonitorStatus.Watching)
'        '  osInputMonitor.InputMonitor_Start()
'    End Sub

'    Public Sub PrepTrigger(pType As TriggerType)
'        osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)

'        If Not pType = TriggerType.ShowPrefs Then
'            SetProgStatus(ProgAction.Activate, pType)
'            StartCancelWatcher(pType)
'        End If
'    End Sub

'    Private Function GetTriggerHandler(tType As TriggerAction) As (HandleAction As TriggerAction, HandleEvent As Func(Of Task))
'        Return TriggerHandlers.FirstOrDefault(Function(chkTriggerHandle)
'                                                  Return chkTriggerHandle.HandleAction = tType
'                                              End Function)
'    End Function

'    Public Async Function ExecuteTrigger(tType As TriggerAction) As Task
'        If ValidateTrigger(tType) Then
'            InputMonSvc.SelectState(MonitorStatus.InCmd)

'            ' Dim objTrigger = GetTriggerHandler(tType)

'            'Dim objHandlerEvent As Func(Of Task) = If(objTrigger.HandleEvent, Nothing)

'            Dim objHandlerEvent = TriggerHandlers.
'                 FirstOrDefault(Function(TriggerHandle) TriggerHandle.HandleAction = tType,
'                                (TriggerAction.None, CType(Nothing, Func(Of Task)))).HandleEvent

'            If objHandlerEvent IsNot Nothing Then
'                    Await objHandlerEvent()
'                End If
'            End If

'            ResolveAction()
'    End Function

'    Public Function ValidateTrigger(pType As TriggerType) As Boolean
'        If pType = TriggerType.ShowPrefs Then Return True

'        If ChkExecPermission() Then
'            SetProgStatus(ProgAction.Activate, pType)
'            StartCancelWatcher(pType)

'            Return True
'        Else
'            Return False
'        End If
'    End Function

'    Private Function StartCancelMonitor(cts As CancellationTokenSource, Optional chkType As TriggerType = TriggerType.AutoCast) As Task
'        Return Task.Run(Sub() MonitorForCancel(cts, chkType))
'    End Function

'    Private Sub StartCancelWatcher(Optional chkType As TriggerType = TriggerType.AutoCast)
'        SetCT()

'        Dim cancelMonitorTask As Task = StartCancelMonitor(chkActionAbort, chkType)
'    End Sub

'    Private Async Sub MonitorForCancel(cts As CancellationTokenSource, Optional pType As TriggerType = TriggerType.AutoCast)
'        Do While Not cts.Token.IsCancellationRequested
'            If pType = TriggerType.AutoCast Then
'                If Not InputMonSvc.DetectTrigger(DetectOpts.MonitorMouse) Then
'                    cts.Cancel()
'                    Exit Do
'                End If
'            ElseIf pType = TriggerType.AutoPass Then
'                If Not InputMonSvc.DetectTrigger(DetectOpts.MonitorShift) Then
'                    cts.Cancel()
'                    Exit Do
'                End If
'            End If
'            'If osFuncLib_InputScan.isActionComplete Then
'            '    cts.Cancel()
'            '    Exit Do
'            'End If
'            Await Task.Delay(5)
'        Loop
'    End Sub

'    Private Sub SetCT()
'        chkActionAbort = New CancellationTokenSource()
'        objCancelState = chkActionAbort.Token
'    End Sub

'    Public Sub ProcessProgressEvent(pMode As ProgMode, pEvent As ProgEvent, ParamArray pEventData() As Object)
'        Dim objProgEventType As TriggerType = Nothing

'        Dim osProgElement As OddLib_ProgressBar =
'            Function() As OddLib_ProgressBar
'                Select Case pMode
'                    Case ProgMode.AutoCast
'                        objProgEventType = TriggerType.AutoCast
'                        Return osGui_AutoCast.OddProgBar1
'                    Case ProgMode.AutoPass
'                        objProgEventType = TriggerType.AutoPass
'                        Return osGui_AutoPass.OddProgBar_AP
'                    Case Else
'                        Return Nothing
'                End Select
'            End Function.Invoke()

'        Dim strEventData As String = ""

'        Try
'            strEventData = pEventData(0).ToString()
'        Catch ex As Exception

'        End Try

'        With PrepareProgEvent(osProgElement)
'            .evDispatch.Invoke(Sub()
'                                   .evAction(GenerateProgEventData(pEvent, objProgEventType,
'                                                                   strEventData))
'                               End Sub)
'        End With
'    End Sub

'    Private Function GenerateProgEventData(pEvent As ProgEvent, pType As TriggerType, Optional pDispText As String = "") As ProgressEventData
'        Return New ProgressEventData(pEvent, pType, pDispText)
'    End Function

'    Private Function PrepareProgEvent(pEventElement As OddLib_ProgressBar) As ProgressEvent
'        Return New ProgressEvent(pEventElement)
'    End Function


'End Module

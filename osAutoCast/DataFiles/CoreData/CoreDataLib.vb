Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Windows.Threading

Public NotInheritable Class CoreDataLib

#Disable Warning IDE0060 ' Remove unused parameter
    Private Sub New()
    End Sub

    Private Shared ReadOnly isDebug As Boolean = False

    Public Shared osTrayIcon As Forms.NotifyIcon
    Public Shared osPopupMenu As System.Windows.Controls.ContextMenu

    Public Shared dirProgFiles As String = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
    Public Shared dirMtga As String = Path.Combine(dirProgFiles, "Wizards of the Coast",
                                                   "MTGA", "MTGALauncher")
    Public Shared dirMtgaExe As String = Path.Combine(dirMtga, "MTGALauncher.exe")

    Public Shared osAppDataDir As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

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

    Private Shared ReadOnly TriggerHandlers As (HandleAction As TriggerAction, HandleEvent As Func(Of Task))() = {
        (TriggerAction.AutoCast, Function() osFuncLib_AutoCast.ExecuteAutoCast()),
        (TriggerAction.AutoPass, Function() osFuncLib_AutoPass.ExecuteAutoPass()),
        (TriggerAction.ShowOpts, Function() osFuncLib_ShowOpts.ExecuteDispOpts()),
        (TriggerAction.ShowMenu, Function() osFuncLib_TrayMenu.DisplayMenuPopup())
    }

    Public Shared Function GetFuse() As Integer
        Return osPrefStoreData.AutoCast_Fuse
    End Function

    Public Shared Function isRTC() As Boolean
        Return osPrefStoreData.AutoCast_RTC
    End Function

    Public Shared Function GetProgSize(isProgType As TriggerType, Optional getH As Boolean = False) As Integer
        Select Case isProgType
            Case TriggerType.AutoCast
                Return If(getH, osPrefStoreData.MainOpts_acProgH, osPrefStoreData.MainOpts_acProgW)
            Case TriggerType.AutoPass
                Return If(getH, osPrefStoreData.MainOpts_apProgH, osPrefStoreData.MainOpts_apProgW)
            Case Else
                Return 0
        End Select
    End Function

    Public Shared Function FetchProgSizeReport(isProgType As TriggerType) As Dictionary(Of String, Integer)
        Select Case isProgType
            Case TriggerType.AutoCast
                Return New Dictionary(Of String, Integer) From {
                        {"pH", osPrefStoreData.MainOpts_acProgH},
                        {"pW", osPrefStoreData.MainOpts_acProgW}
                    }
            Case TriggerType.AutoPass
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

    Public Shared Function VerifyRunStatus() As Boolean
        If IsDisabled() Then
            Return False
        Else
            Return ChkExecPermission()
        End If
    End Function


    Public Shared Function ChkExecPermission(tType As TriggerAction) As Boolean

        If Not IsDebugBuild() Then
            Return DetectGameUI.FocusMTGA()
        Else
            Return Not DetectGameUI.FocusMTGA()
        End If
    End Function

    Public Shared Function IsDebugBuild() As Boolean
        Dim customAttribute As DebuggableAttribute =
                CType(Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), GetType(DebuggableAttribute)), DebuggableAttribute)
        Return customAttribute IsNot Nothing AndAlso customAttribute.IsJITTrackingEnabled
    End Function

    Public Shared Function IsDebugBuild(isDebug As Boolean) As Boolean
        Return True
    End Function

    Public Shared Function SetGameFocus() As Boolean
        Return DetectGameUI.FocusMTGA(True)
    End Function

    Private Shared Sub ResolveAction()
        If Not osFuncLib_InputScan.isActionComplete Then Return
        osFuncLib_InputScan.isActionComplete = False
    End Sub

    Private Shared Sub ResetStatus()
        InputMonSvc.SelectState(MonitorStatus.Watching)
    End Sub

    Public Shared Sub PrepUtilityTrigger(pType As TriggerType)
        osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
        If isUtilityTrigger(pType) Then Return
    End Sub

    Private Shared Function isUtilityTrigger(pType As TriggerAction) As Boolean
        Return pType = TriggerAction.ShowMenu OrElse pType = TriggerAction.ShowOpts
    End Function

    Public Shared Async Function ExecuteTrigger(tType As TriggerAction) As Task
        If ValidateTrigger(tType) Then
            InputMonSvc.SelectState(MonitorStatus.InCmd)

            Dim objHandlerEvent = TriggerHandlers.
                 FirstOrDefault(Function(TriggerHandle) TriggerHandle.HandleAction = tType,
                                (TriggerAction.None, CType(Nothing, Func(Of Task)))).HandleEvent

            If objHandlerEvent IsNot Nothing Then
                Await objHandlerEvent()
            End If
        End If

        ResolveAction()
    End Function

    Public Shared Function ValidateTrigger(pType As TriggerAction) As Boolean
        If isUtilityTrigger(pType) Then Return True

        If VerifyRunStatus() Then
            osFuncLib_Progress.UpdateProgStatus(pType, ProgAction.Activate)
            StartCancelWatcher(pType)

            Return True
        Else
            Return False
        End If
    End Function

    Private Shared Function StartCancelMonitor(cts As CancellationTokenSource,
                                                  Optional chkType As TriggerType = TriggerType.AutoCast) As Task
        Return Task.Run(Sub() MonitorForCancel(cts, chkType))
    End Function

    Private Shared Sub StartCancelWatcher(Optional chkType As TriggerType = TriggerType.AutoCast)
        SetCT()
        StartCancelMonitor(chkActionAbort, chkType)
    End Sub

    Private Shared Async Sub MonitorForCancel(cts As CancellationTokenSource,
                                                 Optional pType As TriggerType = TriggerType.AutoCast)
        While Not cts.Token.IsCancellationRequested
            If pType = TriggerType.AutoCast Then
                If Not InputMonSvc.DetectTrigger(DetectOpts.MonitorMouse) Then
                    Await PrepDispatcher().InvokeAsync(Sub()
                                                           cts.Cancel()
                                                       End Sub)
                    Exit While
                End If
            ElseIf pType = TriggerType.AutoPass AndAlso
                       Not InputMonSvc.DetectTrigger(DetectOpts.MonitorShift) Then
                Await PrepDispatcher(True).InvokeAsync(Sub()
                                                           cts.Cancel()
                                                       End Sub)
                Exit While
            End If
            Await Task.Delay(5)
        End While
    End Sub

    Private Shared Sub SetCT()
        chkActionAbort = New CancellationTokenSource()
        objCancelState = chkActionAbort.Token
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
            .evDispatch.Invoke(
                Sub()
                    .evAction(GenerateProgEventData(pEvent, objProgEventType,
                                                    strEventData))
                End Sub)
        End With
    End Sub

    Private Shared Function GenerateProgEventData(pEvent As ProgEvent,
                                                      pType As TriggerType,
                                                      Optional pDispText As String = "") As ProgressEventData
        Return New ProgressEventData(pEvent, pType, pDispText)
    End Function

    Private Shared Function PrepareProgEvent(pEventElement As OddLib_ProgressBar) As ProgressEvent
        Return New ProgressEvent(pEventElement)
    End Function
#Enable Warning IDE0060 ' Remove unused parameter
End Class
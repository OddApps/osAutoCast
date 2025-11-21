Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Windows.Threading
Imports osAutoCast.osShaderDataLib
Imports SharpDX.Direct3D11
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports System.Resources
Imports System.Globalization
Imports osResDict = System.Collections.DictionaryEntry

Public NotInheritable Class CoreDataLib

#Disable Warning IDE0060 ' Remove unused parameter
    Private Sub New()
    End Sub

    Private Shared ReadOnly isDebug As Boolean = False

    Public Shared osTrayIcon As Forms.NotifyIcon
    Public Shared osTrayPopupMenu As System.Windows.Controls.ContextMenu

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

    Public Shared Property osEnabledStatus As Boolean = True
    Public Shared osEnStatus_Popup As Boolean = False

    Public Const mEvent_Down As Integer = 2
    Public Const mEvent_Up As Integer = 4

    Public Shared chkActionAbort As CancellationTokenSource
    Public Shared objCancelState As CancellationToken

    Private Shared objCancelTask As Task

    Public Shared InputMonSvc As InputMonitorService = Nothing

    Private Shared ReadOnly TriggerHandlers As (HandleAction As TriggerAction, HandleEvent As Func(Of Task))() = {
        (TriggerAction.AutoCast, Function() osFuncLib_AutoCast.ExecuteAutoCast()),
        (TriggerAction.AutoPass, Function() osFuncLib_AutoPass.ExecuteAutoPass()),
        (TriggerAction.ShowOpts, Function() osFuncLib_ShowOpts.ExecuteDispOpts()),
        (TriggerAction.ShowMenu, Function() osFuncLib_PopupMenu.ShowPopupMenu())
    }

    Public Shared Function osStatus_Fetch() As Boolean
        Return osEnabledStatus
    End Function

    Public Shared Function osStatus_IsDisabled() As Boolean
        Return osEnabledStatus = False
    End Function

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

    Public Shared Function GetProgSizeReport(isProgType As TriggerType) As ProgSizeReport
        Select Case isProgType
            Case TriggerType.AutoCast
                Return New ProgSizeReport(osPrefStoreData.MainOpts_acProgW,
                                          osPrefStoreData.MainOpts_acProgH)
            Case TriggerType.AutoPass
                Return New ProgSizeReport(osPrefStoreData.MainOpts_apProgW,
                                          osPrefStoreData.MainOpts_apProgH)
            Case Else
                Return Nothing
        End Select
    End Function

    Private Shared Function GetResourceList() As List(Of String)
        Return PopulateResources().Select(
            Function(objRes) objRes).ToList()
    End Function

    Private Shared Function PopulateResources() As String()
        Return Assembly.GetExecutingAssembly().
            GetManifestResourceNames()
    End Function

    Private Shared Function GetShaderType(objShaderRes As String) As osShaderType
        Return If(objShaderRes.Contains("Effect"),
            osShaderType.ShaderEffect, osShaderType.ShaderObject)
    End Function

    Private Shared Function ValidateShader(objShaderRes As String) As Boolean
        Return If(objShaderRes.Contains("osShader"), True, False)
    End Function

    Private Shared Function GenerateShaderList() As List(Of osShaderDetails)
        Return GetResourceList().Where(
            Function(valRes) ValidateShader(valRes)).
            Select(Function(objRes)
                       Return New osShaderDetails(objRes, GetShaderType(objRes))
                   End Function).ToList()
    End Function

    Public Shared Sub ComposeShaderIdx()
        ShaderIdxData = GenerateShaderList()
    End Sub

    Public Shared Function GetShaderDevice() As osProgDevice
        Return osHandler_Graphics.pDevice
    End Function

    Public Shared Function GetSafetyTimer() As Integer
        Return osPrefStoreData.AutoPass_SafetyTimer
    End Function

    Public Shared Function GetVisualQuality() As ProgVisOpts
        Dim objVQ = osPrefStoreData.GenOpts_VisualQuality
        Return If(objVQ = 0, ProgVisOpts.Performance, ProgVisOpts.Quality)
    End Function

    Public Shared Function ChkExecPermission() As Boolean
        If Not IsDebugBuild() Then
            Return DetectGameUI.FocusMTGA()
        Else
            Return Not DetectGameUI.FocusMTGA()
        End If
    End Function

    Public Shared Function VerifyRunStatus() As Boolean
        If osStatus_IsDisabled() Then
            Return False
        Else
            Return ChkExecPermission()
        End If
    End Function

    Public Shared Function IsDebugBuild() As Boolean
        Dim customAttribute As DebuggableAttribute =
                CType(Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), GetType(DebuggableAttribute)), DebuggableAttribute)
        Return customAttribute IsNot Nothing AndAlso customAttribute.IsJITTrackingEnabled
    End Function

    Public Shared Function SetGameFocus() As Boolean
        Return DetectGameUI.FocusMTGA(True)
    End Function

    Public Shared Function IsGameRunning() As Boolean
        Return Process.GetProcessesByName("MTGA").Length > 0
    End Function

    Private Shared Sub ResolveAction()
        objCancelTask = Nothing
        If Not osFuncLib_InputScan.isActionComplete Then Return
        osFuncLib_InputScan.isActionComplete = False
    End Sub

    Public Shared Sub PrepUtilityTrigger(pType As TriggerType)
        osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
        If isUtilityTrigger(pType) Then Return
    End Sub

    Private Shared Function isUtilityTrigger(pType As TriggerAction) As Boolean
        Return pType = TriggerAction.ShowMenu OrElse pType = TriggerAction.ShowOpts
    End Function

    Public Shared Async Function ExecuteTrigger(tType As TriggerAction) As Task
        Dim objHandlerEvent As Func(Of Task) = Nothing

        Dim objTriggerVal = ValidateTrigger(tType)

        If ProcessTrigger(objTriggerVal) Then
            InputMonSvc.SelectState(MonitorStatus.InCmd)

            objHandlerEvent = TriggerHandlers.
                 FirstOrDefault(Function(TriggerHandle) TriggerHandle.HandleAction = tType,
                                (TriggerAction.None, CType(Nothing, Func(Of Task)))).HandleEvent
        Else
            ResolveAction()
            Exit Function
        End If

        If objHandlerEvent IsNot Nothing Then
            If objTriggerVal = TriggerValidation.ValidTrigger Then
                osFuncLib_Progress.SetProgBlockData(TriggerType.AutoCast)
                Await osHandler_UI.LaunchGui(tType)
                osFuncLib_Progress.UpdateProgStatus(tType, ProgAction.Activate)
            End If

            Await objHandlerEvent()
        End If

        ResolveAction()
    End Function

    Private Shared Function ProcessTrigger(valType As TriggerValidation) As Boolean
        Return Not valType = TriggerValidation.InvalidTrigger
    End Function

    Public Shared Function ValidateTrigger(pType As TriggerAction) As TriggerValidation
        If isUtilityTrigger(pType) Then Return TriggerValidation.ValidUtility

        If VerifyRunStatus() Then

            InitAbortMonitor(pType)

            Return TriggerValidation.ValidTrigger
        Else
            Return TriggerValidation.InvalidTrigger
        End If
    End Function

    Private Shared Function StartCancelMonitor(cts As CancellationTokenSource,
                                                  Optional chkType As TriggerType = TriggerType.AutoCast) As Task
        Return Task.Run(Sub()
                            MonitorForCancel(cts, chkType)
                        End Sub)
    End Function

    Private Shared Sub InitAbortMonitor(Optional chkType As TriggerType = TriggerType.AutoCast)
        PrepAbortMonitor()
        CreateAbortMonitor(chkActionAbort, chkType)
    End Sub

    Private Shared Sub PrepAbortMonitor()
        If chkActionAbort IsNot Nothing Then
            chkActionAbort.Dispose()
            chkActionAbort = Nothing
        End If

        objCancelState = Nothing
    End Sub

    Private Shared Async Sub MonitorForCancel(cts As CancellationTokenSource,
                                                 Optional pType As TriggerType = TriggerType.AutoCast)
        While Not cts.Token.IsCancellationRequested
            If pType = TriggerType.AutoCast Then
                If Not InputMonSvc.DetectTrigger(DetectOpts.MonitorMouse) Then
                    cts.Cancel()
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

    Private Shared Sub CreateAbortMonitor(ByRef cts As CancellationTokenSource,
                                                  Optional chkType As TriggerType = TriggerType.AutoCast)
        cts = New CancellationTokenSource()
        objCancelState = cts.Token

        objCancelTask = StartCancelMonitor(cts, chkType)
    End Sub

    Private Shared Sub ResetCancelWatch()
        chkActionAbort = New CancellationTokenSource()
        objCancelState = chkActionAbort.Token
    End Sub

    Public Shared Sub ProcessProgressEvent(pMode As ProgMode, pEvent As ProgEvent, ParamArray pEventData() As Object)
        Dim strEventData As String = ""

        Dim objProgEventType = If(pMode = ProgMode.AutoCast,
            TriggerType.AutoCast, TriggerType.AutoPass)

        If pMode = ProgMode.AutoCast Then
            If pEventData.Length > 0 Then
                strEventData = pEventData(0).ToString()
            End If

            With PrepareProgEvent(osHandler_UI.osGui_AutoCastProgress)
                .evDispatch.Invoke(
                    Sub()
                        .evAction(GenerateProgEventData(pEvent, objProgEventType,
                                                        strEventData))
                    End Sub)
            End With
        Else
            Dim osProgElement = osHandler_UI.osGui_AutoPass.OddProgBar_AP

            If pEventData.Length > 0 Then
                strEventData = pEventData(0).ToString()
            End If

            With PrepareProgEvent(osProgElement)
                .evDispatch.Invoke(
                    Sub()
                        .evAction(GenerateProgEventData(pEvent, objProgEventType,
                                                        strEventData))
                    End Sub)
            End With
        End If
    End Sub

    Private Shared Function GenerateProgEventData(pEvent As ProgEvent,
                                                      pType As TriggerType,
                                                      Optional pDispText As String = "") As ProgressEventData
        Return New ProgressEventData(pEvent, pType, pDispText)
    End Function

    Private Shared Function PrepareProgEvent(pEventElement As OddLib_ProgressBar) As ProgressEvent
        Return New ProgressEvent(pEventElement)
    End Function

    Private Shared Function PrepareProgEvent(pEventElement As ProgBarGui_AutoCast) As ProgressEvent
        Return New ProgressEvent(pEventElement)
    End Function

#Enable Warning IDE0060 ' Remove unused parameter
End Class
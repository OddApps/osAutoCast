Imports System.IO
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Runtime.InteropServices
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.osEnabledStatusConfig
Imports osAutoCast.DataTypeLib.DetectOpts
Imports osAutoCast.DataTypeLib.TriggerType
Imports osAutoCast.DataTypeLib.ProgressMode
Imports osAutoCast.DataTypeLib.TriggerAction
Imports osAutoCast.DataTypeLib.TriggerValidation
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports osRegEx = System.Text.RegularExpressions.Regex
Imports osStatus = osAutoCast.osEnabledStatusConfig
Imports osAutoCast.osControls
Imports osPrefData = osAutoCast.osPrefLib.osPreferenceLib
Imports osAutoCast.osFuncLib_AutoCast
Imports osAutoCast.osFuncLib_AutoPass
Imports osAutoCast.osFuncLib_ShowOpts
Imports osAutoCast.osFuncLib_PopupMenu

#Disable Warning IDE0060 ' Remove unused parameter
#Disable Warning BC42353

Public NotInheritable Class CoreDataLib

    Private Sub New()
    End Sub

    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000

    Private Const MA_NOACTIVATE As Integer = 3

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As Integer) As Integer
    End Function

    Private Shared ReadOnly isDebug As Boolean = False
    Public Shared ReadOnly UtilizeTestShader As Boolean = True

    Public Shared osTrayIcon As Forms.NotifyIcon

    Public Shared osTrayMenuVisualsApplied As Boolean = False

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

    Public Shared Property osEnabledStatus2 As Boolean = True
    Public Shared osEnStatus_Popup As Boolean = False

    Public Const mEvent_Down As Integer = 2
    Public Const mEvent_Up As Integer = 4

    Public Shared chkActionAbort As CancellationTokenSource
    Public Shared objCancelState As CancellationToken

    Private Shared objCancelTask As Task

    Private Shared ReadOnly ShaderTypeIdx As New Dictionary(Of String, osShaderType) From {
        {"TextGlow", sTypeText_G},
        {"TextStroke", sTypeText_S},
        {"ProgPixel", sTypePixel},
        {"ProgVertex", sTypeVertex}
    }

    Public Shared InputMonSvc As InputMonitorService = Nothing

    Private Shared ReadOnly TriggerHandlers As (HandleAction As TriggerAction, HandleEvent As Func(Of Task))() = {
        (TriggerAutoCast, Function() ExecuteAutoCast()),
        (TriggerAutoPass, Function() ExecuteAutoPass()),
        (TriggerShowOpts, Function() ExecuteDispOpts()),
        (TriggerShowMenu, Function() ShowPopupMenu())
    }

    Public Shared Function osStatus_Fetch() As Boolean
        Return osStatus.Instance.osEnabledStatus_Fetch()
    End Function

    Public Shared Function osStatus_IsDisabled() As Boolean
        Return osStatus.Instance.osEnabledStatus = False
    End Function

    Public Shared Function GetFuse() As Integer
        Return osPrefData.Data.AutoCast_Fuse
    End Function

    Public Shared Function isRTC() As Boolean
        Return osPrefData.Data.AutoCast_RTC
    End Function

    Public Shared Function GetWinHwnd(objWin As Window) As IntPtr
        Return New Interop.WindowInteropHelper(objWin).Handle
    End Function

    Public Shared Function GetWinOpts(hwndWin As IntPtr) As IntPtr
        Return (GetWindowLong(hwndWin, GWL_EXSTYLE) Or WS_EX_NOACTIVATE)
    End Function

    Public Shared Sub SetWinOpts(hwndWin As IntPtr)
        SetWindowLong(hwndWin, GWL_EXSTYLE,
                      GetWinOpts(hwndWin))
    End Sub

    Public Shared Function GetProgSize(isProgType As TriggerType, Optional getH As Boolean = False) As Integer
        Select Case isProgType
            Case AutoCast
                Return If(getH, osPrefData.Data.MainOpts_acProgH, osPrefData.Data.MainOpts_acProgW)
            Case AutoPass
                Return If(getH, osPrefData.Data.MainOpts_apProgH, osPrefData.Data.MainOpts_apProgW)
        End Select
    End Function

    Public Shared Function FetchProgSizeReport(isProgType As TriggerType, Optional chkVisQ As Boolean = False) As Dictionary(Of String, Integer)
        Select Case isProgType
            Case AutoCast
                osFuncLib_Progress.SetProgBlockData(TriggerType.AutoCast)

                With osPrefData.Data
                    Dim progW = .MainOpts_acProgW
                    Dim progH = .MainOpts_acProgH

                    If CoreDataLib.VerifyVisQualityPref() Then
                        progW += 4 : progH += 4
                    End If

                    Return New Dictionary(Of String, Integer) From {
                        {"pH", progH}, {"pW", progW}
                    }
                End With
            Case AutoPass
                Return New Dictionary(Of String, Integer) From {
                        {"pH", osPrefData.Data.MainOpts_apProgH},
                        {"pW", osPrefData.Data.MainOpts_apProgW}
                    }
        End Select
    End Function

    Public Shared Function GetProgSizeReport(isProgType As TriggerType, chkVisQ As Boolean) As Dictionary(Of String, Integer)
        Select Case isProgType
            Case AutoCast
                osFuncLib_Progress.SetProgBlockData(TriggerType.AutoCast)

                With osPrefData.Data
                    Dim progW = .MainOpts_acProgW
                    Dim progH = .MainOpts_acProgH

                    If CoreDataLib.VerifyVisQualityPref() Then
                        progW += 4 : progH += 4
                    End If

                    Return New Dictionary(Of String, Integer) From {
                        {"pH", progH}, {"pW", progW}
                    }
                End With
            Case AutoPass
                Return New Dictionary(Of String, Integer) From {
                        {"pH", osPrefData.Data.MainOpts_apProgH},
                        {"pW", osPrefData.Data.MainOpts_apProgW}
                    }
        End Select
    End Function

    Public Shared Sub TerminateTrayMenu()
        Try
            osHandler_UI.osTrayMenu.Close()
        Catch : End Try
    End Sub

    Private Shared Function GetShaderType(objShaderRes As String) As osShaderType
        Return ShaderTypeIdx(DetermineShaderType(objShaderRes))
    End Function

    Private Shared Function DetermineShaderType(objShaderName As String) As String
        Return osRegEx.Match(objShaderName, "_(.*?)\.ps",
                             RegexOptions.IgnoreCase).Groups(1).Value
    End Function
    Private Shared Function GenerateShaderList() As List(Of osShaderDetails)
        Return osShaderNameList.Select(
            Function(shaderRes)
                Return CreateShaderRecord(shaderRes)
            End Function).ToList()
    End Function

    Public Shared Async Function GenerateShaderList(isNew As Boolean) As Task
        Dim list As New List(Of osShaderDetails)

        For Each shaderRes In osShaderNameList
            Dim objShaderRec = CreateShaderRecord(shaderRes)
            Await AddShaderToIdx(objShaderRec).ConfigureAwait(False)
            list.Add(objShaderRec)
        Next

        ShaderDetailsIdx = list
    End Function

    Private Shared Function CreateShaderRecord(objShaderRes As String) As osShaderDetails
        Return New osShaderDetails(objShaderRes, GetShaderType(objShaderRes))
    End Function

    Public Shared Async Function ComposeShaderIdx() As Task
        Await GenerateShaderList(True)
    End Function

    Public Shared Function GetShaderDevice() As osProgDevice
        Return osHandler_Graphics.pDevice
    End Function

    Public Shared Function GetSafetyTimer() As Integer
        Return osPrefData.Data.AutoPass_SafetyTimer
    End Function

    Public Shared Function GetVisualQuality() As ProgVisOpts
        Dim objVQ = osPrefData.Data.GenOpts_VisualQuality
        Return If(objVQ = 0, ProgVisOpts.Performance, ProgVisOpts.Quality)
    End Function

    Public Shared Function VerifyVisQualityPref() As Boolean
        Dim objVQ = osPrefData.Data.GenOpts_VisualQuality
        Return If(objVQ = 0, False, True)
    End Function

    Public Shared Function ChkExecPermission() As Boolean
        If Not IsDebugBuild() Then
            Return DetectGameUI.FocusMTGA()
        Else
            Return Not DetectGameUI.FocusMTGA()
        End If
    End Function

    Public Shared Function VerifyRunStatus() As Boolean
        If osStatus.Instance.osChkEnabledStatus_IsDisabled() Then
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
        If Not osFuncLib_InputScan.isActionComplete Then Return
        osFuncLib_InputScan.isActionComplete = True
    End Sub

    Public Shared Sub PrepUtilityTrigger(pType As TriggerType)
        osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
        If isUtilityTrigger(pType) Then Return
    End Sub

    Private Shared Function isUtilityTrigger(pType As TriggerAction) As Boolean
        Return ValidateUtilityTrigger(pType)
    End Function

    Private Shared Function ValidateUtilityTrigger(pType As TriggerAction) As Boolean
        Select Case pType
            Case TriggerShowMenu
                Return True
            Case TriggerShowTrayMenu
                Return True
            Case TriggerShowOpts
                Return True
            Case Else
                Return False
        End Select
    End Function

    Public Shared Async Function ExecuteTrigger(tType As TriggerAction) As Task
        Dim objTriggerActionData As New TriggerActionData(tType)

        If objTriggerActionData.isValidTrigger Then
            Await objTriggerActionData.
                TriggerFunc().Invoke()
        End If

        ResolveAction()
    End Function

    Public Shared Function ValidateTrigger(pType As TriggerAction) As TriggerValidation

        If isUtilityTrigger(pType) Then Return ValidUtility

        If VerifyRunStatus() Then : InitAbortMonitor(pType)
            Return ValidTrigger
        Else
            Return InvalidTrigger
        End If
    End Function

    Private Shared Function StartCancelMonitor(cts As CancellationTokenSource,
                                                  Optional chkType As TriggerType = AutoCast) As Task
        Return MonitorForCancel(cts, chkType)
    End Function

    Private Shared Sub InitAbortMonitor(Optional chkType As TriggerType = AutoCast)
        PrepAbortMonitor()
        CreateAbortMonitor(chkActionAbort, chkType)
    End Sub

    Private Shared Sub PrepAbortMonitor()
        If chkActionAbort IsNot Nothing Then
            chkActionAbort.Cancel()


            objCancelTask = Nothing

            chkActionAbort.Dispose()
            chkActionAbort = Nothing
        End If

        objCancelState = Nothing
    End Sub

    Private Shared Async Function MonitorForCancel(cts As CancellationTokenSource,
                                                 Optional pType As TriggerType = AutoCast) As Task
        Dim token = cts.Token

        Try
            While Not token.IsCancellationRequested
                Select Case pType
                    Case AutoCast
                        If Not InputMonSvc.DetectTrigger(MonitorMouse) Then
                            cts.Cancel()
                            Exit While
                        End If
                    Case AutoPass
                        If InputMonSvc.DetectTrigger(MonitorAutoPassAbort) Then
                            Await PrepDispatcher(True).InvokeAsync(Sub()
                                                                       cts.Cancel()
                                                                   End Sub)
                            Exit While
                        End If
                End Select

                Await Task.Delay(5, cts.Token)
            End While
        Catch ex As OperationCanceledException : End Try
    End Function

    Private Shared Sub CreateAbortMonitor(ByRef cts As CancellationTokenSource,
                                                  Optional chkType As TriggerType = AutoCast)
        cts = New CancellationTokenSource()
        objCancelState = cts.Token

        objCancelTask = StartCancelMonitor(cts, chkType)
    End Sub

    Private Shared Sub ResetCancelWatch()
        chkActionAbort = New CancellationTokenSource()
        objCancelState = chkActionAbort.Token
    End Sub

    Public Shared Sub ProcessProgressEvent(pMode As ProgressMode, pEvent As ProgEvent, ParamArray pEventData() As Object)
        Dim strEventData As String = ""

        Dim objProgEventType = If(pMode =
            ProgMode_AutoCast, AutoCast, AutoPass)

        If pMode = ProgMode_AutoCast Then
            If pEventData.Length > 0 Then
                strEventData = pEventData(0).ToString()
            End If

            ui_AutoCast.InvokeAsync(
                Sub(gui)
                    With PrepareProgEvent(ProgBarGui_AutoCast.Instance)
                        .evDispatch.Invoke(
                            Sub()
                                .evAction(GenerateProgEventData(
                                          pEvent, objProgEventType, strEventData))
                            End Sub)
                    End With
                End Sub)
        Else
            Dim osProgElement = ui_AutoPass.OddProgBar_AP

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

    Private Shared Function GenerateProgEventData(pEvent As ProgEvent, pType As TriggerType,
                                                  Optional pDispText As String = "") As ProgressEventData
        Return New ProgressEventData(pEvent, pType, pDispText)
    End Function

    Private Shared Function PrepareProgEvent(pEventElement As osProgressBar) As ProgressEvent
        Return New ProgressEvent(pEventElement)
    End Function

    Private Shared Function PrepareProgEvent(pEventElement As ProgBarGui_AutoCast) As ProgressEvent
        Return New ProgressEvent(pEventElement)
    End Function

End Class
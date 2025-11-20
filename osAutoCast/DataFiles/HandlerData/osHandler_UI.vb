Imports System.Windows.Forms
Imports System.Windows.Threading
Imports System.Threading
Imports osDraw = System.Drawing
Imports System.Runtime.InteropServices
Imports System.Diagnostics
Imports osTrash = System.Runtime.GCSettings
Imports osTrashCompact = System.Runtime.GCLargeObjectHeapCompactionMode

Public NotInheritable Class osHandler_UI

    Public Shared pmFunc_TerminatePopupMenu As MouseButtonEventHandler = AddressOf TerminatePopupMenu

    ' Public Shared Property osGui_InputMonitor As Form
    ' Public Shared Property osGui_InputMonitor2 As Window

    Private Shared _osPrefs As Lazy(Of osPrefs)
    Public Shared ReadOnly Property osGui_Prefs As osPrefs
        Get
            Return _osPrefs.Value
        End Get
    End Property

    Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
        Get
            Return _osPopupMenu.Value
        End Get
    End Property
    Private Shared _osPopupMenu As New Lazy(Of osPopupMenu_GUI)(
        GeneratePopupMenuGUI(), LazyThreadSafetyMode.ExecutionAndPublication)

    Private Shared _osPopupMenuOverlay As Lazy(Of MenuOverlayWindow)
    Public Shared ReadOnly Property osPopupMenuOverlay As MenuOverlayWindow
        Get
            Return _osPopupMenuOverlay.Value
        End Get
    End Property

    Private Shared _autoPass As Lazy(Of progGui_AutoPass)
    Public Shared ReadOnly Property osGui_AutoPass As progGui_AutoPass
        Get
            Return _autoPass.Value
        End Get
    End Property

    Private Shared _autoCastProgress As ProgBarGui_AutoCast
    Public Shared ReadOnly Property osGui_AutoCastProgress As ProgBarGui_AutoCast
        Get
            Return _autoCastProgress
        End Get
    End Property

    Private Shared Sub GenerateGUI(objGenGui As TriggerAction)
        Select Case objGenGui
            Case TriggerAction.AutoPass
                PrepAutoPass()
            Case TriggerAction.AutoCast
                PrepAutoCast()
            Case TriggerAction.ShowMenu
                PrepUI_PopupMenuOverlay()
                PrepUI_PopupMenu()
            Case TriggerAction.ShowMenuOverlay
                PrepUI_PopupMenuOverlay(True)
        End Select
    End Sub

    Public Shared Sub PrepDispatch(sender As Object, e As EventArgs)
        Dim objWin_PopupMenu = TryCast(sender, osPopupMenu_GUI)
        Dim objWin_PopupMenuOverlay = _osPopupMenuOverlay.Value

        DisposeUI_PopupMenu.Invoke(objWin_PopupMenu)
        DisposeUI_PopupMenuOverlay.Invoke(objWin_PopupMenuOverlay)

        ClearHandlers(objWin_PopupMenu, objWin_PopupMenuOverlay)

        _osPopupMenu = Nothing
        _osPopupMenuOverlay = Nothing

        ResetUI(TriggerAction.ShowMenu)

        InitResourceAlloc()
    End Sub

    Public Shared Sub DispatchOverlay()
        Dim objWin_PopupMenuOverlay = _osPopupMenuOverlay.Value

        DisposeUI_TrayOverlay.Invoke(objWin_PopupMenuOverlay)
        _osPopupMenuOverlay = Nothing

        InitResourceAlloc()
    End Sub

    Private Shared Sub ClearHandlers(objPopupMenu As osPopupMenu_GUI, objPopupMenuOverlayWindow As MenuOverlayWindow)
        RemoveHandler objPopupMenu.Closed, AddressOf PrepDispatch
        RemoveHandler objPopupMenuOverlayWindow.MouseDown, pmFunc_TerminatePopupMenu
    End Sub

    Private Shared Sub LoadOptsUI()
        _osPrefs = New Lazy(Of osPrefs)(
            Function() New osPrefs(), LazyThreadSafetyMode.ExecutionAndPublication)

        Dim handle As IntPtr = osGui_Prefs.Handle
    End Sub

    Private Shared Sub LoadPrefData()
        LoadOptsUI()
        osGui_Prefs.osPrefsPrep()
    End Sub

    Public Shared Sub PrepAndLoadUI()
        LoadPrefData()
        PrepUI_PopupMenu()

        osHandler_Graphics.EnsureCreated()

        CoreDataLib.ComposeShaderIdx()
        LoadAllShaders()
    End Sub

    Private Shared Sub LoadAllShaders()
        ShaderIdxData.ForEach(
            Sub(objShader) osHandler_Shader.
                AddShaderToIdx(osHandler_Graphics.pDevice, objShader))
    End Sub

    Public Shared Async Function LaunchGui(progGui As TriggerAction) As Task
        Select Case progGui
            Case TriggerAction.AutoCast
                GenerateGUI(progGui)
            Case TriggerAction.AutoPass
                Dim guiTask = Application.Current.Dispatcher.InvokeAsync(
                    Sub()
                        GenerateGUI(progGui)
                        Dim guiReset = _autoPass.Value

                        guiReset.BeginPrep()
                    End Sub)
                Await guiTask
            Case TriggerAction.ShowMenu
                GenerateGUI(progGui)
            Case TriggerAction.ShowMenuOverlay
                GenerateGUI(progGui)
        End Select
    End Function

    Public Shared Sub LaunchOverlayGui()
        GenerateGUI(TriggerAction.ShowMenuOverlay)
    End Sub

    Public Shared Sub DisplayGUI(guiType As TriggerType, Optional ptPosData As osDraw.Point = Nothing)
        Select Case guiType
            Case TriggerType.AutoCast
                Application.Current.Dispatcher.Invoke(
                    Sub()
                        osGui_AutoCastProgress.InitiateAutoCast()
                    End Sub)
            Case TriggerType.AutoPass
                osGui_AutoPass.Dispatcher.Invoke(
                    Sub()
                        osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoPass, ProgAction.Activate)
                        CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "Release Mouse To Begin")

                        osGui_AutoPass.Show()
                    End Sub)
            Case TriggerType.ShowMenu
                ShowPopupUI()
            Case TriggerType.ShowMenuOverlay
                ShowPopupUI(True)
        End Select
    End Sub

    Private Shared Sub ShowPopupUI(Optional isFromTray As Boolean = False)
        Application.Current.Dispatcher.Invoke(
            Sub()
                Dim objWin_PopupMenuOverlay = _osPopupMenuOverlay.Value

                If isFromTray Then
                    DisplayUI_TrayOverlay.Invoke(objWin_PopupMenuOverlay)
                Else
                    DisplayUI_PopupMenuOverlay.Invoke(objWin_PopupMenuOverlay)

                    Dim objWin_PopupMenu = _osPopupMenu.Value
                    DisplayUI_PopupMenu.Invoke(objWin_PopupMenu, objWin_PopupMenuOverlay)
                End If
            End Sub)
    End Sub

    Public Shared Sub ResetPopupMenu()
        TerminatePopupMenuByCmd()
    End Sub

    Private Shared Async Sub TerminatePopupMenuByCmd()
        Dim objWin_PopupMenu = _osPopupMenu.Value
        Dim objWin_PopupMenuOverlay = _osPopupMenuOverlay.Value

        Dim objTerminateTask = objWin_PopupMenu.Dispatcher.
            InvokeAsync(Async Function()

                            objWin_PopupMenu.Topmost = True

                            Await objWin_PopupMenu.InitPopupClose(True)
                            Await objWin_PopupMenuOverlay.InitOverlayClose()

                            DispatchUI(TriggerAction.ShowMenu)
                        End Function)

        Await objTerminateTask.Task.Unwrap()

        Dim doGameFocus = CoreDataLib.SetGameFocus()
    End Sub

    Private Shared Async Sub TerminatePopupMenu()
        Dim objWin_PopupMenu = _osPopupMenu.Value
        Dim objWin_PopupMenuOverlay = _osPopupMenuOverlay.Value

        Dim objTerminateTask = objWin_PopupMenu.Dispatcher.
            InvokeAsync(Async Function()

                            objWin_PopupMenu.Topmost = True

                            Await objWin_PopupMenu.InitPopupClose()
                            Await objWin_PopupMenuOverlay.InitOverlayClose()

                            DispatchUI(TriggerAction.ShowMenu)
                        End Function)

        Await objTerminateTask.Task.Unwrap()

        Dim doGameFocus = CoreDataLib.SetGameFocus()
    End Sub

    Public Shared Sub ResetOptsUI()
        _osPrefs = New Lazy(Of osPrefs)(
            Function() New osPrefs(), LazyThreadSafetyMode.ExecutionAndPublication)

        Dim handle As IntPtr = osGui_Prefs.Handle

        osGui_Prefs.osPrefsPrep()
    End Sub

    Private Shared Sub PrepAutoCast()
        With CoreDataLib.GetProgSizeReport(TriggerType.AutoCast)
            _autoCastProgress = New ProgBarGui_AutoCast(.pWidth, .pHeight,
                                                        osFuncLib_Progress.ProgTimeSpan, AddressOf EaseProgress)
        End With

        Dim guiLoad = _autoCastProgress.Handle
        guiLoad = Nothing
    End Sub

    Private Shared Sub PrepAutoPass()
        _autoPass = New Lazy(Of progGui_AutoPass)(
                   Function()
                       Return Application.Current.Dispatcher.
                       Invoke(Function()
                                  Return New progGui_AutoPass()
                              End Function)
                   End Function, LazyThreadSafetyMode.ExecutionAndPublication)
    End Sub

    Private Shared Sub PrepUI_PopupMenuOverlay(Optional isFromTray As Boolean = False)
        If _osPopupMenuOverlay Is Nothing Then
            _osPopupMenuOverlay = New Lazy(Of MenuOverlayWindow)(
                GeneratePopupMenuOverlayGUI(isFromTray), LazyThreadSafetyMode.ExecutionAndPublication)
        End If

        osPopupMenuOverlay.PrepPopupMenuOverlay()
    End Sub

    Private Shared Sub PrepUI_PopupMenu()
        If _osPopupMenu Is Nothing Then
            PrepDispatcher().Invoke(Sub()
                                        _osPopupMenu = New Lazy(Of osPopupMenu_GUI)(
                                        GeneratePopupMenuGUI(), LazyThreadSafetyMode.ExecutionAndPublication)
                                    End Sub)
        End If
    End Sub

    Public Shared Function FetchPopupMenuOverlay() As MenuOverlayWindow
        Return osPopupMenuOverlay
    End Function

    Public Shared Function FetchPopupMenu() As osPopupMenu_GUI
        Return osPopupMenu
    End Function

    Public Shared Sub DispatchUI()
        Dim objWin_PopupMenu = _osPopupMenu.Value
        objWin_PopupMenu.Close()
    End Sub

    Public Shared Sub DispatchUI(guiType As TriggerAction, Optional isOverlay As Boolean = False)
        Select Case guiType
            Case TriggerAction.ShowMenu
                If isOverlay Then

                Else
                    Dim objWin_PopupMenu = _osPopupMenu.Value
                    objWin_PopupMenu.Close()
                End If
        End Select
    End Sub

    Public Shared Sub ResetUI(guiType As TriggerAction, Optional forceCreateNew As Boolean = False)
        Dim guiReset As Object

        Select Case guiType
            Case TriggerAction.AutoCast
                Application.Current.Dispatcher.
                Invoke(Sub()
                           osGui_AutoCastProgress.Close()
                           osGui_AutoCastProgress.Dispose()
                       End Sub)

                _autoCastProgress = Nothing
            Case TriggerAction.AutoPass
                guiReset = _autoPass.Value

                Using objPrepData As New GUI_PrepData(guiType, guiReset)
                    If objPrepData.guiIsLoaded Then
                        If objPrepData.guiDispatch.CheckAccess() Then
                            objPrepData.guiAction.Invoke(guiReset)
                        Else
                            objPrepData.guiDispatch.Invoke(objPrepData.guiAction,
                                                            DispatcherPriority.Normal, guiReset)
                        End If
                    End If
                End Using

                If forceCreateNew Then
                    GenerateGUI(guiType)
                    guiReset.BeginPrep()
                End If
            Case TriggerAction.ShowMenu
                PrepUI_PopupMenu()
        End Select

        guiReset = Nothing

        InitResourceAlloc()

        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
    End Sub

    Private Shared Sub ImplementHandler(objWinPopupMenu_GUI As osPopupMenu_GUI)
        AddHandler objWinPopupMenu_GUI.Closed, AddressOf PrepDispatch
    End Sub

    Public Shared Sub RecaptureResources()
        InitResourceAlloc()

        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
    End Sub

    Private Shared Sub InitResourceAlloc()
        osTrash.LargeObjectHeapCompactionMode = osTrashCompact.CompactOnce

        GC.Collect()
        GC.WaitForPendingFinalizers()

        AllocResources()
    End Sub

    <DllImport("psapi.dll", EntryPoint:="EmptyWorkingSet")>
    Private Shared Function ProcessAlloc(hProcess As IntPtr) As Boolean
    End Function

    Private Shared Sub AllocResources()
        Try
            ProcessAlloc(Process.GetCurrentProcess().Handle)
        Catch

        End Try
    End Sub

End Class

Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.OverlayVisualType
Imports osAutoCast.DataTypeLib.PopupVisualType
Imports osAutoCast.DataTypeLib.PopupCloseAction
Imports osAutoCast.DataTypeLib.ProgressMode
Imports osAutoCast.DataTypeLib.TriggerAction
Imports osDraw = System.Drawing
Imports osTrash = System.Runtime.GCSettings
Imports osTrashCompact = System.Runtime.GCLargeObjectHeapCompactionMode
Imports osTimeSeek = System.Windows.Media.Animation.TimeSeekOrigin
Imports osThreadMode = System.Threading.LazyThreadSafetyMode
Imports System.ComponentModel

Public NotInheritable Class osHandler_UI

    Public Shared pmFunc_TerminatePopupMenu As MouseButtonEventHandler = AddressOf TerminatePopupMenu

    Private Shared _osPrefs As Lazy(Of osPrefs)
    Public Shared ReadOnly Property osGui_Prefs As osPrefs
        Get
            Return _osPrefs.Value
        End Get
    End Property

    Private Shared _osPopupMenu As Lazy(Of osPopupMenu_GUI)
    Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
        Get
            Return _osPopupMenu.Value
        End Get
    End Property

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
            Case TriggerAutoPass
                PrepAutoPass()
            Case TriggerAutoCast
                PrepAutoCast()
            Case TriggerShowMenu
                PrepUI_PopupMenuOverlay()
                PrepUI_PopupMenu()
            Case TriggerShowTrayMenu
                PrepUI_PopupMenuOverlay(True)
        End Select
    End Sub

    Public Shared Sub PrepDispatch(sender As Object, e As EventArgs)
        Dim osPopupMenu = TryCast(sender, osPopupMenu_GUI)
        Dim osPopupMenuOverlay = _osPopupMenuOverlay.Value

        DisposeUI_PopupMenu.Invoke(osPopupMenu)
        DisposeUI_PopupMenuOverlay.Invoke(osPopupMenuOverlay)

        ClearHandlers(osPopupMenu, osPopupMenuOverlay)

        _osPopupMenu = Nothing
        _osPopupMenuOverlay = Nothing

        ResetUI(TriggerShowMenu)

        InitResourceAlloc()
    End Sub

    Public Shared Sub DispatchOverlay()
        Task.Run(Sub()
                     DismissPopupMenuOverlay()
                 End Sub)

        InitResourceAlloc()
    End Sub

    Private Shared Sub ClearHandlers(objPopupMenu As osPopupMenu_GUI, objPopupMenuOverlayWindow As MenuOverlayWindow)
        RemoveHandler objPopupMenu.Closed, AddressOf PrepDispatch
        RemoveHandler objPopupMenuOverlayWindow.MouseDown, pmFunc_TerminatePopupMenu
        RemoveHandler objPopupMenuOverlayWindow.MouseUp, pmFunc_TerminatePopupMenu
    End Sub

    Private Shared Sub LoadOptsUI()
        _osPrefs = New Lazy(Of osPrefs)(
            Function() New osPrefs(),
            osThreadMode.ExecutionAndPublication)

        Dim handle As IntPtr = osGui_Prefs.Handle
    End Sub

    Private Shared Sub LoadPrefData()
        LoadOptsUI()
        osGui_Prefs.osPrefsPrep()
    End Sub

    Public Shared Async Function PrepAndLoadUI() As Task

        Await Task.Run(
            Async Function()
                Dim aa = Task.Run(
                    Sub()
                        PrepDispatcher().
                    Invoke(Sub()
                               LoadPrefData()
                           End Sub)
                    End Sub)

                Dim ab = Task.Run(
                    Sub()
                        PrepDispatcher().
                    Invoke(Sub()
                               GeneratePopupMenu(True)
                           End Sub)
                    End Sub)

                Await Task.Run(
                    Async Function()
                        Await osHandler_Graphics.EnsureCreated()
                    End Function)

                Await Task.Run(
                    Sub()
                        CoreDataLib.ComposeShaderIdx()
                        LoadAllShaders()
                    End Sub)
            End Function)

    End Function

    Private Shared Sub LoadAllShaders()
        Parallel.ForEach(ShaderIdxData,
                         Async Sub(objShader)
                             Await AppendShader(objShader)
                         End Sub)
    End Sub

    Private Shared Async Function AppendShader(objShaderDetails As osShaderDetails) As Task
        Await PrepDispatcher.InvokeAsync(
            Sub()
                AddShaderToIdx(objShaderDetails)
            End Sub)
    End Function

    Public Shared Async Function LaunchGui(progGui As TriggerAction) As Task
        Select Case progGui
            Case TriggerAutoCast
                GenerateGUI(progGui)
            Case TriggerAutoPass
                Dim guiTask = PrepDispatcher().InvokeAsync(
                    Sub()
                        GenerateGUI(progGui)
                        Dim guiReset = _autoPass.Value

                        guiReset.BeginPrep()
                    End Sub)
                Await guiTask
            Case TriggerShowMenu
                Await GeneratePopupMenu()
            Case TriggerShowTrayMenu
                Await GenerateTrayOverlay()
        End Select
    End Function

    Public Shared Sub DisplayGUI(guiType As TriggerType, Optional ptPosData As osDraw.Point = Nothing)
        Select Case guiType
            Case TriggerType.AutoCast
                PrepDispatcher().Invoke(
                    Sub()
                        osGui_AutoCastProgress.InitiateAutoCast()
                    End Sub)
            Case TriggerType.AutoPass
                osGui_AutoPass.Dispatcher.Invoke(
                    Sub()
                        osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, ProgAction.Activate)
                        CoreDataLib.ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.DispMsg, "Release Mouse To Begin")

                        osGui_AutoPass.Show()
                    End Sub)
            Case TriggerType.ShowMenu
                ShowPopupUI()
            Case TriggerType.ShowTrayMenu
                ShowTrayUI()
        End Select
    End Sub

    Private Shared Sub ShowPopupUI(Optional isFromTray As Boolean = False)
        PrepDispatcher().Invoke(
            Sub()
                Dim osPopupMenuOverlay = _osPopupMenuOverlay.Value

                If isFromTray Then
                    DisplayUI_TrayOverlay.Invoke(osPopupMenuOverlay)
                Else
                    DisplayUI_PopupMenuOverlay.Invoke(osPopupMenuOverlay)

                    Dim osPopupMenu = _osPopupMenu.Value
                    DisplayUI_PopupMenu.Invoke(osPopupMenu, osPopupMenuOverlay)
                End If
            End Sub)
    End Sub

    Private Shared Sub ShowTrayUI()
        PrepDispatcher().Invoke(
            Sub()
                DisplayUI_TrayOverlay.Invoke(osPopupMenuOverlay)
            End Sub)
    End Sub

    Public Shared Async Function PresentPopupMenu() As Task
        Dim objTask_ShowPopupMenu = PrepDispatcher().InvokeAsync(
            Async Function()
                With osPopupMenuOverlay

                    AddHandler .MouseUp, osHandler_UI.pmFunc_TerminatePopupMenu

                    .InitPopupMenuOverlay()
                    Await .InitOverlayOpen(OverlayVisual_Open)

                End With

                With osPopupMenu
                    .Owner = osPopupMenuOverlay
                    .Owner.ShowInTaskbar = False

                    .ShowInTaskbar = False
                    .Topmost = True
                    .ShowActivated = False

                    .Show()
                    Await .TriggerPopupMenu()

                End With
            End Function, DispatcherPriority.Render)

        Await objTask_ShowPopupMenu.Task.Unwrap()
    End Function

    Public Shared Async Function ClosePopupMenu(popupCloseAction As PopupCloseAction) As Task
        ClearHandlers(osPopupMenu, osPopupMenuOverlay)
        Await TerminatePopupMenu(popupCloseAction)
    End Function

    Private Shared Async Sub TerminatePopupMenu(sender As Object, e As MouseButtonEventArgs)
        If DetermineMouseClick(e) Then
            ClearHandlers(osPopupMenu, osPopupMenuOverlay)

            Await TerminatePopupMenu(ClosePopup_Default)
        End If
    End Sub

    Private Shared Async Sub OverlayCloseByClk(sender As Object, e As MouseButtonEventArgs)
        If DetermineMouseClick(e) Then
            osHandler_UI.pmFunc_TerminatePopupMenu(sender, e)
        End If
    End Sub

    Private Shared Async Function TerminatePopupMenu(popupCloseAction As PopupCloseAction) As Task
        Dim objTerminateTask As DispatcherOperation(Of Task) = Nothing

        Select Case popupCloseAction
            Case ClosePopup_Default
                objTerminateTask = PrepDispatcher().
                    InvokeAsync(Async Function()
                                    LiftPopupMenu()

                                    Await osPopupMenu.InitPopupClose(PopupVisual_Close)
                                    Await osPopupMenuOverlay.InitOverlayClose(OverlayVisual_Close)
                                End Function, DispatcherPriority.Render)
            Case ClosePopup_ByBtn
                objTerminateTask = PrepDispatcher().
                    InvokeAsync(Async Function()
                                    LiftPopupMenu(True)

                                    Await osPopupMenu.InitPopupClose(PopupVisual_CloseByBtn)
                                    Await osPopupMenuOverlay.InitOverlayClose(OverlayVisual_CloseByBtn)
                                End Function, DispatcherPriority.Render)
            Case ClosePopup_ByCmd
                objTerminateTask = PrepDispatcher().
                    InvokeAsync(Async Function()
                                    LiftPopupMenu()

                                    Await osPopupMenu.InitPopupClose(PopupVisual_CloseByCmd)
                                    Await osPopupMenuOverlay.InitOverlayClose(OverlayVisual_CloseByCmd)
                                End Function, DispatcherPriority.Render)
        End Select

        Await objTerminateTask.Task.Unwrap()
        Await DisposePopupMenu()
    End Function

    Private Shared Sub LiftPopupMenu(Optional KillOwner As Boolean = False)
        osPopupMenu.Topmost = True

        If KillOwner Then
            osPopupMenu.Owner = Nothing : End If
    End Sub

    Private Shared Async Function DisposePopupMenu() As Task
        ClearHandlers(osPopupMenu,
                      osPopupMenuOverlay)

        Await Task.WhenAll(DismissPopupMenu(),
                           DismissPopupMenuOverlay())

        Await GeneratePopupMenu()
        InitResourceAlloc()
    End Function

    Private Shared Function DismissPopupMenu() As Task
        Return PrepDispatcher().BeginInvoke(
            Sub()
                With osPopupMenu
                    If .IsLoaded Then
                        .IsHitTestVisible = False
                        .Opacity = 0
                        .DataContext = Nothing

                        .Close()
                    End If
                End With

                _osPopupMenu = Nothing
            End Sub).Task
    End Function

    Private Shared Function DismissPopupMenuOverlay() As Task
        Return PrepDispatcher().BeginInvoke(
            Sub()
                Try
                    With osPopupMenuOverlay
                        If .IsLoaded Then
                            .IsHitTestVisible = False
                            .Opacity = 0
                            .DataContext = Nothing

                            .Close()
                        End If
                    End With

                    _osPopupMenuOverlay = Nothing
                Catch ex As Exception : End Try
            End Sub).Task
    End Function

    Private Shared Sub GeneratePopupMenu(isNEw As Boolean)
        ' Both CreatePopupMenu and CreatePopupMenuOverlay return Tasks that schedule UI work.
        Parallel.ForEach(
            New List(Of Task) From {
            {CreatePopupMenuOverlay()}, {CreatePopupMenu()}},
                Async Sub(objTask)
                    Await objTask
                End Sub)
    End Sub

    Private Shared Async Function GeneratePopupMenu() As Task
        ' Both CreatePopupMenu and CreatePopupMenuOverlay return Tasks that schedule UI work.
        Await Task.WhenAll(CreatePopupMenuOverlay(),
                       CreatePopupMenu())
    End Function

    Private Shared Function CreatePopupMenu() As Task
        ' Schedule only the UI action on the dispatcher and await that operation's Task.
        ' Use Normal priority unless you need a different one.
        Return PrepDispatcher().InvokeAsync(Sub()
                                                PrepUI_PopupMenu()
                                            End Sub, DispatcherPriority.Normal).Task
    End Function

    Private Shared Function CreatePopupMenuOverlay() As Task
        Return PrepDispatcher().InvokeAsync(Sub()
                                                PrepUI_PopupMenuOverlay()
                                            End Sub, DispatcherPriority.Normal).Task
    End Function

    'Private Shared Async Function GeneratePopupMenu() As Task
    '    Await Task.WhenAll(CreatePopupMenuOverlay(),
    '                       CreatePopupMenu())
    'End Function

    'Private Shared Function CreatePopupMenu() As Task
    '    Return PrepDispatcher().BeginInvoke(Sub()
    '                                            PrepUI_PopupMenu()
    '                                        End Sub).Task
    'End Function

    'Private Shared Function CreatePopupMenuOverlay() As Task
    '    Return PrepDispatcher().BeginInvoke(
    '        Sub()
    '            PrepUI_PopupMenuOverlay(True)
    '        End Sub).Task
    'End Function

    Private Shared Async Function GeneratePopupOverlay() As Task
        Await Task.Run(Sub() CreatePopupMenuOverlay())
    End Function

    Private Shared Async Function GenerateTrayOverlay() As Task
        Await Task.Run(Sub() CreateTrayOverlay())
    End Function

    Private Shared Function CreateTrayOverlay() As Task
        Return PrepDispatcher().BeginInvoke(
            Sub()
                PrepUI_PopupMenuOverlay(True)
            End Sub).Task
    End Function

    Public Shared Sub ResetOptsUI()
        _osPrefs = New Lazy(Of osPrefs)(
            Function() New osPrefs(),
            osThreadMode.ExecutionAndPublication)

        Dim handle As IntPtr = osGui_Prefs.Handle

        osGui_Prefs.osPrefsPrep()
    End Sub

    Private Shared Sub PrepAutoCast()
        With CoreDataLib.GetProgSizeReport(TriggerType.AutoCast)
            _autoCastProgress = New ProgBarGui_AutoCast(.pWidth, .pHeight,
                                                        osFuncLib_Progress.ProgTimeSpan,
                                                        AddressOf EaseProgress)
        End With

        Dim guiLoad = _autoCastProgress.Handle
        guiLoad = Nothing
    End Sub

    Private Shared Sub PrepAutoPass()
        _autoPass = New Lazy(Of progGui_AutoPass)(
                   Function()
                       Return PrepDispatcher().Invoke(
                       Function() New progGui_AutoPass())
                   End Function,
                   osThreadMode.ExecutionAndPublication)
    End Sub

    Private Shared Sub PrepUI_PopupMenuOverlay(Optional isFromTray As Boolean = False)
        If _osPopupMenuOverlay Is Nothing Then
            _osPopupMenuOverlay = New Lazy(Of MenuOverlayWindow)(
                GeneratePopupMenuOverlayGUI(isFromTray),
                osThreadMode.ExecutionAndPublication)
        Else
            If isFromTray Then
                osPopupMenuOverlay.ApplyTrayConfig()
            End If
        End If

        osPopupMenuOverlay.PrepPopupMenuOverlay()
    End Sub

    Private Shared Sub PrepUI_PopupMenu()
        If _osPopupMenu Is Nothing Then
            _osPopupMenu = New Lazy(Of osPopupMenu_GUI)(
                GeneratePopupMenuGUI(),
                osThreadMode.ExecutionAndPublication)
        End If
    End Sub

    Public Shared Function isOverlayActive() As Boolean
        Return If(osPopupMenuOverlay IsNot Nothing, True, False)
    End Function

    Public Shared Function FetchPopupMenuOverlay() As MenuOverlayWindow
        Return osPopupMenuOverlay
    End Function

    Public Shared Function FetchPopupMenu() As osPopupMenu_GUI
        Return osPopupMenu
    End Function

    Public Shared Sub ResetUI(guiType As TriggerAction, Optional forceCreateNew As Boolean = False)
        Dim guiReset As Object

        Select Case guiType
            Case TriggerAutoCast
                PrepDispatcher().
                Invoke(Sub()
                           osGui_AutoCastProgress.Close()
                           osGui_AutoCastProgress.Dispose()
                       End Sub)

                _autoCastProgress = Nothing
            Case TriggerAutoPass
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
            Case TriggerShowMenu
                PrepUI_PopupMenuOverlay()
                PrepUI_PopupMenu()
            Case TriggerShowTrayMenu
                PrepUI_PopupMenuOverlay()
        End Select

        guiReset = Nothing

        RecaptureResources()
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

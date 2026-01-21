Imports System.ComponentModel
Imports System.Data
Imports System.Runtime.InteropServices
Imports System.Windows.Threading
Imports osAutoCast.osFuncLib_Progress
Imports osAutoCast.DataTypeLib.OverlayVisualType
Imports osAutoCast.DataTypeLib.PopupCloseAction
Imports osAutoCast.DataTypeLib.PopupVisualType
Imports osAutoCast.DataTypeLib.ProgressMode
Imports osAutoCast.DataTypeLib.TriggerAction
Imports osDraw = System.Drawing
Imports osThreadMode = System.Threading.LazyThreadSafetyMode
Imports osTimeSeek = System.Windows.Media.Animation.TimeSeekOrigin
Imports osTrash = System.Runtime.GCSettings
Imports osTrashCompact = System.Runtime.GCLargeObjectHeapCompactionMode
Imports osAutoCast.osHandler_AutoCast

#Disable Warning BC42104

Public NotInheritable Class osHandler_UI

    Public Shared pmFunc_TerminatePopupMenu As MouseButtonEventHandler =
        AddressOf TerminatePopupMenu

    Private Shared _osPrefs As Lazy(Of osPrefs)
    Public Shared ReadOnly Property osGui_Prefs As osPrefs
        Get
            Return _osPrefs.Value
        End Get
    End Property

    Public Shared _osPrefsWindow As Lazy(Of osPrefs_GUI)
    Public Shared ReadOnly Property osPrefsWindow As osPrefs_GUI
        Get
            Return _osPrefsWindow.Value
        End Get
    End Property

    Public Shared _autoPass As Lazy(Of progGui_AutoPass)
    Public Shared ReadOnly Property osGui_AutoPass As progGui_AutoPass
        Get
            Return _autoPass.Value
        End Get
    End Property

    '   Private Shared _autoPass2 As progUI_AutoPass
    Public Shared _autoPass2 As Lazy(Of progUI_AutoPass)
    Public Shared ReadOnly Property osGui_AutoPass2 As progUI_AutoPass
        Get
            Return _autoPass2.Value
        End Get
    End Property

    Public Shared ReadOnly Property osGui_AutoCastProgress As AutoCastGui
        Get
            Return AutoCast_UI
        End Get
    End Property

    Public Shared _acProgress As Lazy(Of ProgBarGui_AutoCast)
    Public Shared ReadOnly Property acProgress As ProgBarGui_AutoCast
        Get
            Return _acProgress.Value
        End Get
    End Property

    Public Shared _osTrayMenu As Lazy(Of osTrayMenu_GUI)
    Public Shared ReadOnly Property osTrayMenu As osTrayMenu_GUI
        Get
            Return _osTrayMenu.Value
        End Get
    End Property

    Public Shared _osPopupMenu As Lazy(Of osPopupMenu_GUI)
    Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
        Get
            Return _osPopupMenu.Value
        End Get
    End Property

    Public Shared _osPopupMenuOverlay As Lazy(Of osPopupMenuOverlay_GUI)
    Public Shared ReadOnly Property osPopupMenuOverlay As osPopupMenuOverlay_GUI
        Get
            Return _osPopupMenuOverlay.Value
        End Get
    End Property

    Public Shared Async Function RestoreUI_PopupMenu() As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osPopupMenuOverlay = PrepUI_PopupMenuOverlay()
                _osPopupMenu = PrepUI_PopupMenu()

                osPopupMenuOverlay.PrepTrayMenuOverlay()
            End Sub, DispatcherPriority.Background).Task
    End Function

    Public Shared Async Function RestoreUI_TrayMenu() As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osTrayMenu = PrepUI_TrayMenu()
                osTrayMenu.PrepTrayMenuInit()
            End Sub, DispatcherPriority.Background)
        'Await Task.Run(
        '    Sub()
        '        PrepDispatcher().Invoke(
        '            Sub()
        '                _osTrayMenu = PrepUI_TrayMenu()
        '                osTrayMenu.PrepTrayMenuInit()
        '            End Sub, DispatcherPriority.Background)
        '    End Sub)
    End Function

    Public Shared Async Function RestoreUI_Prefs() As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osPrefsWindow = PrepUI_Opts()
            End Sub, DispatcherPriority.Background)

        AuthorizeInputMonitor()
    End Function

    Public Shared Function PrepUI_PopupMenuOverlay(Optional isFromTray As Boolean = False) As Lazy(Of osPopupMenuOverlay_GUI)
        Return New Lazy(Of osPopupMenuOverlay_GUI)(
            Function()
                Dim objPopupMenuOverlay As New osPopupMenuOverlay_GUI
                Return objPopupMenuOverlay
            End Function,
        LazyThreadSafetyMode.None)
    End Function

    Public Shared Function PrepUI_TrayMenu() As Lazy(Of osTrayMenu_GUI)
        Return New Lazy(Of osTrayMenu_GUI)(
            Function()
                Dim objTrayMenu As New osTrayMenu_GUI
                '  objTrayMenu.PrepTrayMenuInit()
                '    objTrayMenu.PrewarmAllVisuals()

                Return objTrayMenu
            End Function, LazyThreadSafetyMode.None)
    End Function

    Public Shared Function PrepUI_PopupMenu() As Lazy(Of osPopupMenu_GUI)
        Return New Lazy(Of osPopupMenu_GUI)(
            Function()
                Dim win As New osPopupMenu_GUI
                '     AddHandler win.Closed, AddressOf osHandler_UI.PrepDispatch
                win.PrepPopupMenu()
                '      win.PrewarmAllVisuals()
                Return win
            End Function, LazyThreadSafetyMode.None)
    End Function

    Public Shared Function PrepUI_Opts() As Lazy(Of osPrefs_GUI)
        Return New Lazy(Of osPrefs_GUI)(
            Function()
                Dim objPrefWin As New osPrefs_GUI()
                Return objPrefWin
            End Function, LazyThreadSafetyMode.None)
    End Function

    Public Shared Function PrepUI_AutoPass() As Lazy(Of progUI_AutoPass)
        Return New Lazy(Of progUI_AutoPass)(
            Function()
                Dim objPrefWin As New progUI_AutoPass()
                '  objPrefWin.PrepAutoPass()
                Return objPrefWin
            End Function, LazyThreadSafetyMode.None)
    End Function

    Public Shared Async Function ComposeTrayMenu() As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osTrayMenu = PrepUI_TrayMenu()

                Dim guiReset = _osTrayMenu.Value
                guiReset.PrepTrayMenuInit()
            End Sub, DispatcherPriority.Background)
    End Function

    Public Shared Async Function ComposePopupMenu() As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osPopupMenu = PrepUI_PopupMenu()

                Dim guiReset = _osPopupMenu.Value
                guiReset.WarmupPopupMenu()
            End Sub, DispatcherPriority.Background)
    End Function

    Public Shared Async Function ComposePopupMenuOverlay() As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osPopupMenuOverlay = PrepUI_PopupMenuOverlay()
                Dim guiReset = _osPopupMenuOverlay.Value
            End Sub,
        DispatcherPriority.Background)
    End Function

    Public Shared Async Function GenerateUI_TrayMenu() As Task
        Await RestoreUI_TrayMenu()
    End Function

    Public Shared Function GenerateUI_PopupMenu() As Task
        Return ComposePopupMenu()
    End Function

    Public Shared Function GenerateUI_PopupMenuOverlay() As Task
        Return ComposePopupMenuOverlay()
    End Function

    Public Shared Sub PrepDispatch_Popup()
        Dim objOsPopupMenu = _osPopupMenu.Value

        ClearHandlers(objOsPopupMenu)

        DisposeUI_PopupMenu.Invoke(objOsPopupMenu)

        _osPopupMenu = Nothing

        Dim objResetPopupMenu = GenerateUI_PopupMenu()

        Dim objTask_VisAdapter = osVisQualityAdapter.InitAdapter(
            VisTypeAdapter.VisAdapter_PopupMenu, objOsPopupMenu.objAnimation_Open, True, True, objOsPopupMenu.objContainer)
    End Sub

    Public Shared Sub PrepDispatch(sender As Object, e As EventArgs)
        Dim objOsPopupMenu = TryCast(sender, osPopupMenu_GUI)
        Dim objOsPopupMenuOverlay = osPopupMenuOverlay

        ClearHandlers(objOsPopupMenu, objOsPopupMenuOverlay)

        DisposeUI_PopupMenu.Invoke(objOsPopupMenu)
        DisposeUI_PopupMenuOverlay.Invoke(objOsPopupMenuOverlay)

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

    Private Shared Sub ClearHandlers(objPopupMenu As osPopupMenu_GUI)
        RemoveHandler objPopupMenu.Closed, AddressOf PrepDispatch
    End Sub

    Private Shared Sub ClearHandlers(objPopupMenu As osPopupMenu_GUI, objPopupMenuOverlayWindow As osPopupMenuOverlay_GUI)
        RemoveHandler objPopupMenu.Closed, AddressOf PrepDispatch
        RemoveHandler objPopupMenuOverlayWindow.MouseDown, pmFunc_TerminatePopupMenu
        RemoveHandler objPopupMenuOverlayWindow.MouseUp, pmFunc_TerminatePopupMenu
    End Sub

    Public Shared Function CreateOptsUI() As Task
        _osPrefsWindow = New Lazy(Of osPrefs_GUI)(
                Function()
                    Return PrepDispatcher().
                        Invoke(Function()
                                   Dim objPrefWin As New osPrefs_GUI()
                                   objPrefWin.PrepPrefVis()

                                   Return objPrefWin
                               End Function, DispatcherPriority.Background)
                End Function, LazyThreadSafetyMode.ExecutionAndPublication)
    End Function

    'Public Shared Function CreateUI_AutoPass() As Task(Of Lazy(Of progUI_AutoPass))
    '    Return Task.Run(
    '        Sub()
    '            _autoPass2 = New Lazy(Of progUI_AutoPass)(
    '            Function()
    '                Return PrepDispatcher().
    '                    Invoke(Function()
    '                               Dim objPrefWin As New progUI_AutoPass()
    '                               objPrefWin.PrepAutoPass()

    '                               Return objPrefWin
    '                           End Function, DispatcherPriority.Background)
    '            End Function, LazyThreadSafetyMode.ExecutionAndPublication)
    '            PrepDispatcher().
    '                    Invoke(Sub()
    '                               Dim guiReset = _autoPass2.Value
    '                               guiReset.BeginPrep()
    '                           End Sub, DispatcherPriority.Background)
    '        End Sub)
    'End Function

    Public Shared Function CreateUI_AutoPass() As Lazy(Of progUI_AutoPass)
        Return New Lazy(Of progUI_AutoPass)(
            Function()
                Return PrepDispatcher().Invoke(
                    Function()
                        Dim objPrefWin As New progUI_AutoPass()
                        objPrefWin.PrepAutoPass()

                        Return objPrefWin
                    End Function)
            End Function, LazyThreadSafetyMode.ExecutionAndPublication)
    End Function

    'Public Shared Function CreateUI_AutoCast() As Task(Of ProgBarGui_AutoCast)
    '    Return Task.Run(
    '        Function()
    '            Return PrepDispatcher().Invoke(
    '                Function()
    '                    With CoreDataLib.GetProgSizeReport(TriggerType.AutoCast)
    '                        SetProgBlockData(TriggerType.AutoCast)

    '                        Dim objWin_AC As New ProgBarGui_AutoCast(.pWidth, .pHeight,
    '                                                                 ProgTimeSpan_AC, AddressOf EaseProgress)
    '                        Dim guiLoad = objWin_AC.Handle
    '                        guiLoad = Nothing

    '                        Return objWin_AC
    '                    End With

    '                End Function, DispatcherPriority.Background)
    '        End Function)
    'End Function

    Public Shared Function ComposeAP() As Task
        Return PrepDispatcher().Invoke(
            Function()
                _autoPass2 = CreateUI_AutoPass()
                Dim guiReset = _autoPass2.Value

                guiReset.BeginPrep()
            End Function, DispatcherPriority.Background)
    End Function

    Public Shared Function GenerateUI_AutoPass() As Task
        Return Task.Run(
             Sub()
                 Dim aa = ComposeAP()
             End Sub)
    End Function

    Public Shared Async Function LaunchGui_AP() As Task
        Await PrepDispatcher(True).InvokeAsync(
            Sub()
                osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, ProgAction.Activate)

                apHandler._DisplayTextFunc("Release Mouse To Begin")
                osGui_AutoPass2.Show()
            End Sub, DispatcherPriority.Background).Task
    End Function

    Public Shared Function DisplayGUI(isAsync As Boolean, guiType As TriggerType, Optional ptPosData As osDraw.Point = Nothing) As Task
        Select Case guiType
            Case TriggerType.AutoCast
                Return Task.Run(
                    Sub()
                        osFuncLib_Progress.UpdateProgStatus(TriggerAutoCast, ProgAction.Activate)
                        osGui_AutoCastProgress.InvokeAsync(Sub(gui)
                                                               gui.InitiateAutoCast()
                                                           End Sub)

                        ' osGui_AutoCastProgress.InitiateAutoCast()
                    End Sub)', DispatcherPriority.Background).Task
            Case TriggerType.AutoPass
                PrepDispatcher(True).Invoke(
                    Sub()
                        osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, ProgAction.Activate)

                        apHandler._DisplayTextFunc("Release Mouse To Begin")
                        osGui_AutoPass2.Show()
                    End Sub)
            Case TriggerType.ShowMenu
            Case TriggerType.ShowTrayMenu
        End Select
    End Function

    Public Shared Sub SetVisualMode(visObj As FrameworkElement)
        RenderOptions.ProcessRenderMode = Interop.RenderMode.Default

        visObj.CacheMode = New BitmapCache()

        RenderOptions.SetBitmapScalingMode(visObj, BitmapScalingMode.LowQuality)
        RenderOptions.SetEdgeMode(visObj, EdgeMode.Aliased)
    End Sub

    Public Shared Function PresentPopupMenu() As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                With osPopupMenu
                    .ConfigureVisual()

                    .Owner = osPopupMenuOverlay
                    .Owner.ShowInTaskbar = False

                    .ShowInTaskbar = False
                    .Topmost = True
                    .ShowActivated = False

                    SetVisualMode(.objContainer)
                    .Show()

                    .TriggerPopupMenu(True)
                End With
            End Sub, DispatcherPriority.Render).Task
    End Function

    Public Shared Sub PresentPopupMenu(isN As Boolean)
        With osPopupMenu
            .ConfigureVisual()

            .Owner = osPopupMenuOverlay
            .Owner.ShowInTaskbar = False

            .ShowInTaskbar = False
            .Topmost = True
            .ShowActivated = False

            .TriggerPopupMenu(True)
        End With
    End Sub

    Public Shared Sub PresentPopupMenuOverlay(isN As Boolean, objOverlayDisplay As TaskCompletionSource(Of Boolean))
        With osPopupMenuOverlay
            AddHandler .MouseUp, osHandler_UI.pmFunc_TerminatePopupMenu

            .InitPopupMenuOverlay(objOverlayDisplay)
            '     Return .InitOverlayOpen(OverlayVisual_Open)
        End With
    End Sub

    Public Shared Sub WarmupPopupMenu()
        Dim a = osPopupMenu.WarmupPopupMenu()
    End Sub

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

    Private Shared Function CloseUI_PopupMenu(objVisType As PopupVisualType, Optional doOwnerKill As Boolean = False) As Task
        Return PrepDispatcher().Invoke(
            Function()
                LiftPopupMenu(doOwnerKill)
                Return osPopupMenu.InitPopupClose(objVisType)
            End Function, DispatcherPriority.Render)
    End Function

    Private Shared Function CloseUI_PopupMenu(isN As Boolean, objVisType As PopupVisualType, Optional doOwnerKill As Boolean = False) As Task
        LiftPopupMenu(doOwnerKill)
        Return osPopupMenu.InitPopupClose(objVisType)
    End Function

    Private Shared Async Function TerminatePopupMenu(popupCloseAction As PopupCloseAction) As Task
        Dim objTask_Terminate As Task = Nothing

        Select Case popupCloseAction
            Case ClosePopup_Default
                Await PrepDispatcher().Invoke(
                    Function()
                        Return CloseUI_PopupMenu(True, PopupVisual_Close)
                    End Function, DispatcherPriority.Render)

                PrepDispatcher().Invoke(
                    Sub()
                        osPopupMenuOverlay.InitOverlayClose(OverlayVisual_Close, True)
                    End Sub, DispatcherPriority.Render)
            Case ClosePopup_ByBtn
                Await PrepDispatcher().Invoke(
                    Function()
                        Return CloseUI_PopupMenu(True, PopupVisual_CloseByBtn, True)
                    End Function, DispatcherPriority.Render)

                PrepDispatcher().Invoke(
                    Sub()
                        osPopupMenuOverlay.InitOverlayClose(OverlayVisual_CloseByBtn, True)
                    End Sub, DispatcherPriority.Render)
            Case ClosePopup_ByCmd
                Await PrepDispatcher().Invoke(
                    Function()
                        Return CloseUI_PopupMenu(True, PopupVisual_CloseByCmd)
                    End Function, DispatcherPriority.Render)

                PrepDispatcher().Invoke(
                    Sub()
                        osPopupMenuOverlay.InitOverlayClose(OverlayVisual_CloseByCmd, True)
                    End Sub, DispatcherPriority.Render)
        End Select

        '   Await objTask_Terminate
        '   Await DisposePopupMenu()
    End Function

    'Private Shared Async Function TerminatePopupMenu(popupCloseAction As PopupCloseAction) As Task
    '    Dim objTask_Terminate As Task = Nothing

    '    Select Case popupCloseAction
    '        Case ClosePopup_Default
    '            Await PrepDispatcher().Invoke(
    '                Function()
    '                    Return CloseUI_PopupMenu(True, PopupVisual_Close)
    '                End Function, DispatcherPriority.Render)

    '            Await PrepDispatcher().Invoke(
    '                Function()
    '                    Return CloseUI_PopupMenuOverlay(True, OverlayVisual_Close)
    '                End Function, DispatcherPriority.Render)
    '        Case ClosePopup_ByBtn
    '            Await PrepDispatcher().Invoke(
    '                Function()
    '                    Return CloseUI_PopupMenu(True, PopupVisual_CloseByBtn, True)
    '                End Function, DispatcherPriority.Render)

    '            Await PrepDispatcher().Invoke(
    '                Function()
    '                    Return CloseUI_PopupMenuOverlay(True, OverlayVisual_CloseByBtn)
    '                End Function, DispatcherPriority.Render)
    '        Case ClosePopup_ByCmd
    '            Await PrepDispatcher().Invoke(
    '                Function()
    '                    Return CloseUI_PopupMenu(True, PopupVisual_CloseByCmd)
    '                End Function, DispatcherPriority.Render)

    '            Await PrepDispatcher().Invoke(
    '                Function()
    '                    Return CloseUI_PopupMenuOverlay(True, OverlayVisual_CloseByCmd)
    '                End Function, DispatcherPriority.Render)
    '    End Select

    '    '   Await objTask_Terminate
    '    Await DisposePopupMenu()
    'End Function

    'Private Shared Async Function TerminatePopupMenu(popupCloseAction As PopupCloseAction) As Task
    '    Dim objTask_Terminate As Task = Nothing

    '    Select Case popupCloseAction
    '        Case ClosePopup_Default
    '            objTask_Terminate = Task.Run(
    '               Async Function()
    '                   Await CloseUI_PopupMenu(PopupVisual_Close)
    '                   Await CloseUI_PopupMenuOverlay(OverlayVisual_Close)
    '               End Function)
    '        Case ClosePopup_ByBtn
    '            objTask_Terminate = Task.Run(
    '                Async Function()
    '                    Await CloseUI_PopupMenu(PopupVisual_CloseByBtn, True)
    '                    Await CloseUI_PopupMenuOverlay(OverlayVisual_CloseByBtn)
    '                End Function)
    '        Case ClosePopup_ByCmd
    '            objTask_Terminate = Task.Run(
    '                Async Function()
    '                    Await CloseUI_PopupMenu(PopupVisual_CloseByCmd)
    '                    Await CloseUI_PopupMenuOverlay(OverlayVisual_CloseByCmd)
    '                End Function)
    '    End Select

    '    Await objTask_Terminate
    '    Await DisposePopupMenu()
    'End Function

    Private Shared Sub LiftPopupMenu(Optional KillOwner As Boolean = False)
        osPopupMenu.Topmost = True

        If KillOwner Then
            osPopupMenu.Owner = Nothing : End If
    End Sub

    Public Shared Async Sub CloseAndRestorePopupMenu()
        ClearHandlers(osPopupMenu, osPopupMenuOverlay)

        Await Task.WhenAll(DismissPopupMenu(),
                           DismissPopupMenuOverlay())

        Await RestoreUI_PopupMenu()

        InitResourceAlloc()
        AuthorizeInputMonitor()
    End Sub

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

    Public Shared Sub TerminateTrayMenu()
        Try
            osTrayMenu.Close()
        Catch : End Try

        _osTrayMenu = Nothing

        Dim objTask_GenTrayMenu = GenerateUI_TrayMenu()
    End Sub

    Public Shared Function GeneratePopupMenu() As Task
        Return Task.WhenAll(GenerateUI_PopupMenuOverlay(), GenerateUI_PopupMenu())
    End Function

    Public Shared Sub PrepDispatch_Prefs(sender As Object, e As EventArgs)
        Dim objOsTrayMenu_GUI = TryCast(sender, osPrefs_GUI)

        DisposeUI_Prefs.Invoke(objOsTrayMenu_GUI)
        objOsTrayMenu_GUI = Nothing

        InitResourceAlloc()
    End Sub

    Public Shared Async Function ResetOptsUI() As Task
        Await RestoreUI_Prefs()
    End Function

    Private Shared Sub PrepAutoPass()
        _autoPass = New Lazy(Of progGui_AutoPass)(
            Function()
                Return PrepDispatcher().Invoke(
                    Function() New progGui_AutoPass())
            End Function, osThreadMode.ExecutionAndPublication)
    End Sub

    Public Shared Sub ShowPrefsUI(objAwaitClose As TaskCompletionSource(Of Boolean), isNew As Boolean)
        osPrefsWindow.osPrefsIU_Present()
        osPrefsWindow.DisplayPrefsUI(objAwaitClose)
    End Sub

    Public Shared Function isOverlayActive() As Boolean
        Return If(osPopupMenuOverlay IsNot Nothing, True, False)
    End Function

    Public Shared Function FetchPopupMenu() As osPopupMenu_GUI
        Return osPopupMenu
    End Function

    Private Shared Function DismissAutoPassUI() As Task
        PrepDispatcher().Invoke(
            Sub()
                With osGui_AutoPass2
                    If .IsLoaded Then
                        .IsHitTestVisible = False
                        .Opacity = 0
                        .DataContext = Nothing

                        .Close()
                    End If
                End With

                _autoPass2 = Nothing
            End Sub)
    End Function

    Public Shared Function CloseAndResetAutoCast(isTask As Boolean) As Task
        osHandler_AutoCast.StopAutoCastGui()
        osHandler_AutoCast.InitializeAutoCastUI()

        Return Task.CompletedTask
    End Function

    Public Shared Sub CloseAndResetAutoCast()
        osHandler_AutoCast.StopAutoCastGui()
        osHandler_AutoCast.InitializeAutoCastUI()
    End Sub

    Public Shared Sub ResetUI(guiType As TriggerAction, Optional forceCreateNew As Boolean = False)
        Dim guiReset As Object

        Select Case guiType
            Case TriggerAutoCast
                Dim objTask_ResetAutoCast = CloseAndResetAutoCast(True)
            Case TriggerAutoPass
                DismissAutoPassUI()
                Dim objTask_ComposeAutoPass = ComposeAP()
            Case TriggerShowOpts

            Case TriggerShowMenu
                Dim objTask_GenPopupMenu = RestoreUI_PopupMenu()
            Case TriggerShowTrayMenu
                '      PrepUI_PopupMenuOverlay()
        End Select

        guiReset = Nothing

        RecaptureResources()
        AuthorizeInputMonitor()
    End Sub

    Public Shared Sub RecaptureResources()
        InitResourceAlloc()

        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
    End Sub

    Public Shared Sub InitResourceAlloc()
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
        Catch : End Try
    End Sub

End Class

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
Imports System.Windows.Media.Animation

#Disable Warning BC42104

Public NotInheritable Class osHandler_UI

    Public Shared pmFunc_TerminatePopupMenu As MouseButtonEventHandler =
        AddressOf TerminatePopupMenu

    Public Shared _osPrefsWindow As osPrefs_GUI
    Public Shared ReadOnly Property osPrefsWindow As osPrefs_GUI
        Get
            Return _osPrefsWindow
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

    Public Shared _osTrayMenu As osTrayMenu_GUI
    Public Shared ReadOnly Property osTrayMenu As osTrayMenu_GUI
        Get
            Return _osTrayMenu
        End Get
    End Property

    'Public Shared _osPopupMenu As Lazy(Of osPopupMenu_GUI)
    'Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
    '    Get
    '        Return _osPopupMenu.Value
    '    End Get
    'End Property

    Public Shared _osPopupMenu As osPopupMenu_GUI
    Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
        Get
            Return _osPopupMenu
        End Get
    End Property

    Public Shared _osPopupMenuOverlay As osPopupMenuOverlay_GUI
    Public Shared ReadOnly Property osPopupMenuOverlay As osPopupMenuOverlay_GUI
        Get
            Return _osPopupMenuOverlay
        End Get
    End Property

    Public Shared Async Function RestoreUI_PopupMenu() As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osPopupMenuOverlay = New osPopupMenuOverlay_GUI
                _osPopupMenu = New osPopupMenu_GUI
            End Sub, DispatcherPriority.Background)

        Await PrepDispatcher().InvokeAsync(
            Async Function()
                Await osPopupMenu.InitPopupMenuVis()
                Await osPopupMenuOverlay.PrepPopupMenuOverlay()
            End Function, DispatcherPriority.Background).Task.Unwrap()
    End Function

    Public Shared Async Function RestoreUI_TrayMenu() As Task
        Await PrepDispatcher().InvokeAsync(
            Function()
                _osTrayMenu = New osTrayMenu_GUI()
                Return osTrayMenu.PrepTrayMenuInit()
            End Function, DispatcherPriority.Background)
    End Function

    Public Shared Async Function RestoreUI_Prefs() As Task
        Await PrepDispatcher().InvokeAsync(
          Function()
              _osPrefsWindow = PrepUI_Opts()
              Return osPrefsWindow.osPrefs_InitUi()
          End Function, DispatcherPriority.Background)

        AuthorizeInputMonitor()
    End Function

    Public Shared Function PrepUI_PopupMenuOverlay(Optional isFromTray As Boolean = False) As osPopupMenuOverlay_GUI
        Return New osPopupMenuOverlay_GUI
    End Function

    Public Shared Function PrepUI_TrayMenu() As osTrayMenu_GUI
        Return New osTrayMenu_GUI()
    End Function

    Public Shared Function PrepUI_PopupMenu() As osPopupMenu_GUI
        Return New osPopupMenu_GUI()
    End Function

    Public Shared Function PrepUI_Opts() As osPrefs_GUI
        Return PrepDispatcher().Invoke(
           Function()
               Return New osPrefs_GUI
           End Function, DispatcherPriority.Render)

    End Function

    Public Shared Function PrepUI_AutoPass() As Lazy(Of progUI_AutoPass)
        Return New Lazy(Of progUI_AutoPass)(
            Function()
                Dim objPrefWin As New progUI_AutoPass()
                Return objPrefWin
            End Function, LazyThreadSafetyMode.None)
    End Function

    Public Shared Async Function GenerateUI_TrayMenu() As Task
        Await RestoreUI_TrayMenu()
    End Function

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

    Public Shared Function ComposeAP() As DispatcherOperation
        Return PrepDispatcher().InvokeAsync(
            Sub()
                osHandler_UI._autoPass2 = osHandler_UI.PrepUI_AutoPass()
                osHandler_UI.osGui_AutoPass2.PrepAutoPass()
            End Sub, DispatcherPriority.Background)
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
            .Owner = osPopupMenuOverlay
            .Owner.ShowInTaskbar = False

            .ShowInTaskbar = False
            .Topmost = True
            .ShowActivated = False

            .TriggerPopupMenu(True)
        End With
    End Sub

    Public Shared Sub PresentPopupMenuOverlay(isN As Boolean)
        With osPopupMenuOverlay
            AddHandler .MouseUp, osHandler_UI.pmFunc_TerminatePopupMenu
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

    Private Shared Sub CloseUI_PopupMenu(isN As Boolean, objVisType As PopupVisualType, Optional doOwnerKill As Boolean = False)
        LiftPopupMenu(doOwnerKill)
        osPopupMenu.InitPopupClose(objVisType)
    End Sub

    Private Shared Async Function TerminatePopupMenu(popupCloseAction As PopupCloseAction) As Task
        Dim objTask_Terminate As Task = Nothing

        Select Case popupCloseAction
            Case ClosePopup_Default
                Dim visFadeAwayOverlay = Await PrepDispatcher().InvokeAsync(
                    Async Function()
                        CloseUI_PopupMenu(True, PopupVisual_Close)

                        ' Dim objTask_ClosePopupMenu = osPopupMenu.TriggerVisuals_Close()
                        Await osPopupMenu.TriggerVisuals_Close()
                        Return osPopupMenu.objTask_Closing.Task
                    End Function, DispatcherPriority.Render).Task

                Await visFadeAwayOverlay.Unwrap()

                Await PrepDispatcher().InvokeAsync(
                    Function()
                        osPopupMenuOverlay.InitOverlayClose(OverlayVisual_Close, True)
                        Return osPopupMenuOverlay.TriggerVisuals_Close()
                    End Function, DispatcherPriority.Render)
            Case ClosePopup_ByBtn
                Dim visFadeAwayOverlay = Await PrepDispatcher().InvokeAsync(
                    Async Function()
                        CloseUI_PopupMenu(True, PopupVisual_CloseByBtn, True)

                        Await osPopupMenu.TriggerVisuals_Close()
                        Return osPopupMenu.objTask_Closing.Task
                    End Function, DispatcherPriority.Render)

                Await visFadeAwayOverlay.Unwrap()

                Await PrepDispatcher().InvokeAsync(
                    Function()
                        osPopupMenuOverlay.InitOverlayClose(OverlayVisual_CloseByBtn, True)
                        Return osPopupMenuOverlay.TriggerVisuals_Close()
                    End Function, DispatcherPriority.Render)
            Case ClosePopup_ByCmd
                Dim visFadeAwayOverlay = Await PrepDispatcher().InvokeAsync(
                    Async Function()
                        CloseUI_PopupMenu(True, PopupVisual_CloseByCmd)

                        Await osPopupMenu.TriggerVisuals_Close()
                        Return osPopupMenu.objTask_Closing.Task
                    End Function, DispatcherPriority.Render)

                Await visFadeAwayOverlay.Unwrap()

                Await PrepDispatcher().InvokeAsync(
                    Function()
                        osPopupMenuOverlay.InitOverlayClose(OverlayVisual_CloseByCmd, True)
                        Return osPopupMenuOverlay.TriggerVisuals_Close()
                    End Function, DispatcherPriority.Render)
        End Select
    End Function

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

    Public Shared Async Function TerminateTrayMenu() As Task
        Try
            osTrayMenu.Close()
        Catch : End Try

        _osTrayMenu = Nothing

        Await RestoreUI_TrayMenu()
    End Function

    Public Shared Sub PrepDispatch_Prefs(sender As Object, e As EventArgs)
        Dim objOsTrayMenu_GUI = TryCast(sender, osPrefs_GUI)

        DisposeUI_Prefs.Invoke(objOsTrayMenu_GUI)
        objOsTrayMenu_GUI = Nothing

        InitResourceAlloc()
    End Sub

    Public Shared Async Function ResetOptsUI() As Task
        If _osPrefsWindow IsNot Nothing Then
            _osPrefsWindow = Nothing
        End If

        Await PrepDispatcher().InvokeAsync(
           Function()
               _osPrefsWindow = New osPrefs_GUI
               Return osPrefsWindow.osPrefs_InitUi()
           End Function, DispatcherPriority.Render)

        AuthorizeInputMonitor()
    End Function

    Private Shared Sub PrepAutoPass()
        _autoPass = New Lazy(Of progGui_AutoPass)(
            Function()
                Return PrepDispatcher().Invoke(
                    Function() New progGui_AutoPass())
            End Function, osThreadMode.ExecutionAndPublication)
    End Sub

    Public Shared Function isOverlayActive() As Boolean
        Return If(osPopupMenuOverlay IsNot Nothing, True, False)
    End Function

    Public Shared Function FetchPopupMenu() As osPopupMenu_GUI
        Return osPopupMenu
    End Function

    Private Shared Function DismissAutoPassUI() As Task
        Return PrepDispatcher().InvokeAsync(
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
            End Sub).Task
    End Function

    Public Shared Async Function CloseAndResetAutoCast() As Task
        osHandler_AutoCast.StopAutoCastGui()
        Await osHandler_AutoCast.StartAutoCastAsync()
    End Function

    Public Shared Async Function ResetUI(guiType As TriggerAction) As Task
        Select Case guiType
            Case TriggerAutoCast
                Await CloseAndResetAutoCast()
            Case TriggerAutoPass
                Await DismissAutoPassUI()
                Await ComposeAP()
            Case TriggerShowOpts

            Case TriggerShowMenu
                Await RestoreUI_PopupMenu()
        End Select

        RecaptureResources()
        AuthorizeInputMonitor()
    End Function

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

    Private Shared Sub AllocResources()
        Try
            ProcessAlloc(Process.GetCurrentProcess().Handle)
        Catch : End Try
    End Sub

    <DllImport("psapi.dll", EntryPoint:="EmptyWorkingSet")>
    Private Shared Function ProcessAlloc(hProcess As IntPtr) As Boolean : End Function

End Class
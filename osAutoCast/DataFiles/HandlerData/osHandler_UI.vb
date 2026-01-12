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

    Public Shared _autoCastProgress As ProgBarGui_AutoCast
    Public Shared ReadOnly Property osGui_AutoCastProgress As ProgBarGui_AutoCast
        Get
            Return _autoCastProgress
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

    Public Shared _osPopupMenuOverlay As Lazy(Of MenuOverlayWindow)
    Public Shared ReadOnly Property osPopupMenuOverlay As MenuOverlayWindow
        Get
            Return _osPopupMenuOverlay.Value
        End Get
    End Property

    Public Shared Async Function GenerateAllUI() As Task

        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osPopupMenuOverlay = PrepUI_PopupMenuOverlay()
                osPopupMenuOverlay.SetBG()
            End Sub, DispatcherPriority.Background)

        Await DispatcherHelpers.YieldToRenderAsync()

        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osPopupMenu = PrepUI_PopupMenu()
                osPopupMenu.WarmupPopupMenu()
            End Sub, DispatcherPriority.Background)

        Await DispatcherHelpers.YieldToRenderAsync()

        Await PrepDispatcher().InvokeAsync(
            Sub()
                _osTrayMenu = PrepUI_TrayMenu2()
                osTrayMenu.PrepTrayMenuInit()
            End Sub, DispatcherPriority.Background)

    End Function

    'Public Shared Function PrepUI_PopupMenuOverlay(Optional isFromTray As Boolean = False) As Lazy(Of MenuOverlayWindow)
    '    Return New Lazy(Of MenuOverlayWindow)(
    '            Function()
    '                Return PrepDispatcher().Invoke(
    '                    Function()
    '                        Dim objPopupMenuOverlay As New MenuOverlayWindow()
    '                        objPopupMenuOverlay.PrepPopupMenuOverlay()

    '                        If isFromTray Then objPopupMenuOverlay.ApplyTrayConfig()

    '                        Return objPopupMenuOverlay
    '                    End Function, DispatcherPriority.Background)
    '            End Function, LazyThreadSafetyMode.ExecutionAndPublication)
    'End Function

    Public Shared Async Function PrepUI_TrayMenu() As Task
        _osTrayMenu = Await Task.Run(
            Function() New Lazy(Of osTrayMenu_GUI)(
                Function()
                    Return PrepDispatcher().
                        Invoke(Function()
                                   Dim objTrayMenu As New osTrayMenu_GUI()
                                   objTrayMenu.PrepTrayMenuInit()

                                   Return objTrayMenu
                               End Function, DispatcherPriority.Background)
                End Function, LazyThreadSafetyMode.ExecutionAndPublication))
    End Function

    Public Shared Async Function GenerateAllUIAsync() As Task
        Await PrepDispatcher().InvokeAsync(
        Sub()
            ' Overlay
            _osPopupMenuOverlay = PrepUI_PopupMenuOverlay()
            _osPopupMenuOverlay.Value.SetBG()

            ' Popup menu
            _osPopupMenu = PrepUI_PopupMenu()
            _osPopupMenu.Value.WarmupPopupMenu()

            ' Tray menu
            _osTrayMenu = PrepUI_TrayMenu2()
            _osTrayMenu.Value.PrepTrayMenuInit()
        End Sub,
        DispatcherPriority.Background)
    End Function

    Public Shared Function PrepUI_PopupMenuOverlay(
    Optional isFromTray As Boolean = False) As Lazy(Of MenuOverlayWindow)

        Return New Lazy(Of MenuOverlayWindow)(
        Function()
            ' Must already be on UI thread
            Dim win As New MenuOverlayWindow()
            win.PrepPopupMenuOverlay()
            If isFromTray Then win.ApplyTrayConfig()
            Return win
        End Function,
        LazyThreadSafetyMode.None)
    End Function

    Public Shared Function PrepUI_TrayMenu2() As Lazy(Of osTrayMenu_GUI)
        Return New Lazy(Of osTrayMenu_GUI)(
            Function()
                Dim objTrayMenu As New osTrayMenu_GUI()
                objTrayMenu.InitTrayMenuVis()

                Return objTrayMenu
            End Function, LazyThreadSafetyMode.None)
    End Function

    Public Shared Function PrepUI_PopupMenu() As Lazy(Of osPopupMenu_GUI)
        Return New Lazy(Of osPopupMenu_GUI)(
            Function()
                Dim win As New osPopupMenu_GUI
                AddHandler win.Closed, AddressOf osHandler_UI.PrepDispatch
                win.PrepPopupMenu()

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
                Return objPrefWin
            End Function, LazyThreadSafetyMode.None)
    End Function

    Public Shared Function PrepUI_AutoCast() As Lazy(Of ProgBarGui_AutoCast)
        Return New Lazy(Of ProgBarGui_AutoCast)(
            Function()
                With CoreDataLib.GetProgSizeReport(TriggerType.AutoCast)
                    SetProgBlockData(TriggerType.AutoCast)

                    Dim objWin_AC As New ProgBarGui_AutoCast(.pWidth, .pHeight,
                                                                     ProgTimeSpan_AC, AddressOf EaseProgress)
                    Dim guiLoad = objWin_AC.Handle
                    guiLoad = Nothing

                    Return objWin_AC
                End With
            End Function, LazyThreadSafetyMode.None)
    End Function

    Public Shared Async Function ComposeTrayMenu() As Task
        Await PrepDispatcher().InvokeAsync(
        Sub()
            _osTrayMenu = PrepUI_TrayMenu2()

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
            guiReset.SetBG()
        End Sub,
        DispatcherPriority.Background)
    End Function

    Public Shared Function GenerateUI_TrayMenu() As Task
        Return ComposeTrayMenu()
    End Function

    Public Shared Function GenerateUI_PopupMenu() As Task
        Return ComposePopupMenu()
    End Function

    Public Shared Function GenerateUI_PopupMenuOverlay() As Task
        Return ComposePopupMenuOverlay()
    End Function

    'Public Shared Function GenerateUI_PopupMenuOverlay() As Task
    '    Return Task.Run(
    '         Sub()
    '             Dim aa = ComposePopupMenuOverlay()
    '         End Sub)
    'End Function

    Private Shared Sub GenerateGUI(objGenGui As TriggerAction)
        Select Case objGenGui
            Case TriggerAutoPass
                PrepAutoPass()
            Case TriggerAutoCast
                PrepAutoCast()
            Case TriggerShowMenu
              '  PrepUI_PopupMenuOverlay()
            Case TriggerShowTrayMenu
                '   PrepUI_PopupMenuOverlay(True)
        End Select
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

    Private Shared Sub ClearHandlers(objPopupMenu As osPopupMenu_GUI, objPopupMenuOverlayWindow As MenuOverlayWindow)
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

    Private Shared Sub PrepAutoCast()
        With CoreDataLib.GetProgSizeReport(TriggerType.AutoCast)
            _autoCastProgress = New ProgBarGui_AutoCast(.pWidth, .pHeight,
                                        osFuncLib_Progress.ProgTimeSpan_AC,
                                        AddressOf EaseProgress)
        End With

        Dim guiLoad = _autoCastProgress.Handle
        guiLoad = Nothing
    End Sub

    Public Shared Function CreateUI_AutoCast() As ProgBarGui_AutoCast
        Return PrepDispatcher().Invoke(
                    Function()
                        With CoreDataLib.GetProgSizeReport(TriggerType.AutoCast)
                            SetProgBlockData(TriggerType.AutoCast)

                            Dim objWin_AC As New ProgBarGui_AutoCast(.pWidth, .pHeight,
                                                                     ProgTimeSpan_AC, AddressOf EaseProgress)
                            Dim guiLoad = objWin_AC.Handle
                            guiLoad = Nothing

                            Return objWin_AC
                        End With

                    End Function, DispatcherPriority.Background)
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

    Public Shared Function ComposeAC() As Task
        Return PrepDispatcher().Invoke(
            Function()
                _autoCastProgress = CreateUI_AutoCast()
            End Function, DispatcherPriority.Background)
    End Function

    Public Shared Function ComposeAP() As Task
        Return PrepDispatcher().Invoke(
            Function()
                _autoPass2 = CreateUI_AutoPass()
                Dim guiReset = _autoPass2.Value

                guiReset.BeginPrep()
            End Function, DispatcherPriority.Background)
    End Function

    Public Shared Function GenerateUI_AutoCast() As Task
        Return Task.Run(
             Sub()
                 Dim aa = ComposeAC()
             End Sub)
    End Function

    Public Shared Function GenerateUI_AutoPass() As Task
        Return Task.Run(
             Sub()
                 Dim aa = ComposeAP()
             End Sub)
    End Function

    Public Shared Async Function LoadGUI() As Task
        Await Task.WhenAll(GenerateUI_AutoCast(), GenerateUI_AutoPass())
    End Function

    Public Shared Async Function LoadMenuGUI() As Task
        Await Task.WhenAll(GenerateUI_PopupMenuOverlay(), GenerateUI_PopupMenu())
    End Function

    Public Shared Function LaunchGui(progGui As TriggerAction) As Task
        Select Case progGui
            Case TriggerAutoCast
                'If _autoCastProgress Is Nothing Then
                '    _autoCastProgress = Await CreateUI_AutoCast()
                'End If
            Case TriggerAutoPass
                'If _autoPass2 Is Nothing Then
                '    _autoPass2 = CreateUI_AutoPass()
                'End If
            Case TriggerShowMenu
             '   Await GeneratePopupMenu()
            Case TriggerShowTrayMenu
                '  Await GenerateTrayOverlay()
        End Select

        osFuncLib_Progress.UpdateProgStatus(progGui, ProgAction.Activate)
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
                Return PrepDispatcher().InvokeAsync(
                    Sub()
                        osFuncLib_Progress.UpdateProgStatus(TriggerAutoCast, ProgAction.Activate)
                        osGui_AutoCastProgress.InitiateAutoCast()
                    End Sub, DispatcherPriority.Background).Task
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

    Public Shared Function PresentPopupMenuOverlay() As Task
        Return Task.Run(
             Function()
                 Return PrepDispatcher().Invoke(
                    Function()
                        With osPopupMenuOverlay
                            AddHandler .MouseUp, osHandler_UI.pmFunc_TerminatePopupMenu

                            .InitPopupMenuOverlay()
                            Return .InitOverlayOpen(OverlayVisual_Open)
                        End With
                    End Function)
             End Function)
    End Function

    Public Shared Function PresentPopupMenu() As Task
        Return Task.Run(
             Function()
                 Return PrepDispatcher().Invoke(
                    Function()
                        With osPopupMenu
                            .Owner = osPopupMenuOverlay
                            .Owner.ShowInTaskbar = False

                            .ShowInTaskbar = False
                            .Topmost = True
                            .ShowActivated = False

                            .Show()
                            Return .TriggerPopupMenu()
                        End With
                    End Function)
             End Function)
    End Function

    Public Shared Async Function DisplayPopupMenu() As Task
        Await PresentPopupMenuOverlay()
        Await PresentPopupMenu()
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

    Private Shared Function CloseUI_PopupMenu(objVisType As PopupVisualType, Optional doOwnerKill As Boolean = False) As Task
        Return PrepDispatcher().Invoke(
            Function()
                LiftPopupMenu(doOwnerKill)
                Return osPopupMenu.InitPopupClose(objVisType)
            End Function, DispatcherPriority.Render)
    End Function

    Private Shared Function CloseUI_PopupMenuOverlay(objVisType As OverlayVisualType) As Task
        Return PrepDispatcher().Invoke(
            Function()
                Return osPopupMenuOverlay.InitOverlayClose(objVisType)
            End Function, DispatcherPriority.Render)
    End Function

    Private Shared Async Function TerminatePopupMenu(popupCloseAction As PopupCloseAction) As Task
        Dim objTask_Terminate As Task = Nothing

        Select Case popupCloseAction
            Case ClosePopup_Default
                objTask_Terminate = Task.Run(
                   Async Function()
                       Await CloseUI_PopupMenu(PopupVisual_Close)
                       Await CloseUI_PopupMenuOverlay(OverlayVisual_Close)
                   End Function)
            Case ClosePopup_ByBtn
                objTask_Terminate = Task.Run(
                    Async Function()
                        Await CloseUI_PopupMenu(PopupVisual_CloseByBtn, True)
                        Await CloseUI_PopupMenuOverlay(OverlayVisual_CloseByBtn)
                    End Function)
            Case ClosePopup_ByCmd
                objTask_Terminate = Task.Run(
                    Async Function()
                        Await CloseUI_PopupMenu(PopupVisual_CloseByCmd)
                        Await CloseUI_PopupMenuOverlay(OverlayVisual_CloseByCmd)
                    End Function)
        End Select

        Await objTask_Terminate
        Await DisposePopupMenu()
    End Function

    Private Shared Sub LiftPopupMenu(Optional KillOwner As Boolean = False)
        osPopupMenu.Topmost = True

        If KillOwner Then
            osPopupMenu.Owner = Nothing : End If
    End Sub

    Private Shared Async Function DisposePopupMenu() As Task
        ClearHandlers(osPopupMenu, osPopupMenuOverlay)

        Await Task.WhenAll(DismissPopupMenu(),
                           DismissPopupMenuOverlay())

        Await GeneratePopupMenu()
        InitResourceAlloc()

        AuthorizeInputMonitor()
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

    Public Shared Function ResetOptsUI(isAsync As Boolean) As Task
        Return Task.Run(Sub()
                            Dim aa = CreateOptsUI()
                        End Sub)
    End Function

    Private Shared Sub PrepAutoPass()
        _autoPass = New Lazy(Of progGui_AutoPass)(
            Function()
                Return PrepDispatcher().Invoke(
                    Function() New progGui_AutoPass())
            End Function, osThreadMode.ExecutionAndPublication)
    End Sub

    Public Shared Async Function ShowPrefsUI(objAwaitClose As TaskCompletionSource(Of Boolean), isNew As Boolean) As Task
        Dim _objAwaitClose = objAwaitClose

        Await PrepDispatcher().InvokeAsync(
            Sub()
                AddHandler osPrefsWindow.Closed,
                            Sub(sender, e)
                                DisposeUI_Prefs.Invoke(osPrefsWindow)
                                _osPrefsWindow = Nothing

                                _objAwaitClose.TrySetResult(True)
                                InitResourceAlloc()
                            End Sub

                osPrefsWindow.DisplayPrefsUI()
            End Sub, DispatcherPriority.Background)
    End Function

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

    Public Shared Sub ResetUI(guiType As TriggerAction, Optional forceCreateNew As Boolean = False)
        Dim guiReset As Object

        Select Case guiType
            Case TriggerAutoCast
                PrepDispatcher().Invoke(
                    Sub()
                        osGui_AutoCastProgress.Close()
                        osGui_AutoCastProgress.Dispose()
                    End Sub)
                Dim objTask_ComposeAutoCast = ComposeAC()
            Case TriggerAutoPass
                DismissAutoPassUI()
                Dim objTask_ComposeAutoPass = ComposeAP()
            Case TriggerShowOpts

            Case TriggerShowMenu
                Dim objTask_GenPopupMenu = GeneratePopupMenu()
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
        Catch : End Try
    End Sub

End Class

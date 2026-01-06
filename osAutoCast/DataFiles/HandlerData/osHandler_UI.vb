Imports System.Runtime.InteropServices
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

    Public Shared pmFunc_TerminatePopupMenu As MouseButtonEventHandler =
        AddressOf TerminatePopupMenu

    Private Shared _osPrefs As Lazy(Of osPrefs)
    Public Shared ReadOnly Property osGui_Prefs As osPrefs
        Get
            Return _osPrefs.Value
        End Get
    End Property

    Private Shared _osPrefsWindow As Lazy(Of osPrefs_GUI)
    Public Shared ReadOnly Property osPrefsWindow As osPrefs_GUI
        Get
            Return _osPrefsWindow.Value
        End Get
    End Property

    Private Shared _autoPass As Lazy(Of progGui_AutoPass)
    Public Shared ReadOnly Property osGui_AutoPass As progGui_AutoPass
        Get
            Return _autoPass.Value
        End Get
    End Property

    '   Private Shared _autoPass2 As progUI_AutoPass
    Private Shared _autoPass2 As Lazy(Of progUI_AutoPass)
    Public Shared ReadOnly Property osGui_AutoPass2 As progUI_AutoPass
        Get
            Return _autoPass2.Value
        End Get
    End Property

    Private Shared _autoCastProgress As ProgBarGui_AutoCast
    Public Shared ReadOnly Property osGui_AutoCastProgress As ProgBarGui_AutoCast
        Get
            Return _autoCastProgress
        End Get
    End Property

    Private Shared _osTrayMenu As Lazy(Of osTrayMenu_GUI)
    Public Shared ReadOnly Property osTrayMenu As osTrayMenu_GUI
        Get
            Return _osTrayMenu.Value
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

    Public Shared Async Function PrepUI_PopupMenuOverlay(Optional isFromTray As Boolean = False) As Task
        _osPopupMenuOverlay = Await Task.Run(
            Function() New Lazy(Of MenuOverlayWindow)(
                Function()
                    Return PrepDispatcher().
                        Invoke(Function()
                                   Dim objPopupMenuOverlay As New MenuOverlayWindow()
                                   objPopupMenuOverlay.PrepPopupMenuOverlay()

                                   If isFromTray Then objPopupMenuOverlay.ApplyTrayConfig()

                                   Return objPopupMenuOverlay
                               End Function, DispatcherPriority.Background)
                End Function, LazyThreadSafetyMode.ExecutionAndPublication))
    End Function

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

    Public Shared Async Function PrepUI_PopupMenu() As Task
        _osPopupMenu = Await Task.Run(
            Function() New Lazy(Of osPopupMenu_GUI)(
                Function()
                    Return PrepDispatcher().
                        Invoke(Function()
                                   Dim objPopupMenu = New osPopupMenu_GUI()
                                   AddHandler objPopupMenu.Closed, AddressOf osHandler_UI.PrepDispatch

                                   Return objPopupMenu
                               End Function, DispatcherPriority.Background)
                End Function, LazyThreadSafetyMode.ExecutionAndPublication))
    End Function

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

        DisposeUI_PopupMenu.Invoke(objOsPopupMenu)
        DisposeUI_PopupMenuOverlay.Invoke(objOsPopupMenuOverlay)

        ClearHandlers(objOsPopupMenu, objOsPopupMenuOverlay)

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

    Public Shared Async Function CreateOptsUI() As Task
        _osPrefsWindow = Await Task.Run(
            Function() New Lazy(Of osPrefs_GUI)(
                Function()
                    Return PrepDispatcher().
                        Invoke(Function()
                                   Dim objPrefWin As New osPrefs_GUI()
                                   objPrefWin.PrepPrefVis()

                                   Return objPrefWin
                               End Function, DispatcherPriority.Background)
                End Function, LazyThreadSafetyMode.ExecutionAndPublication))
    End Function

    Public Shared Async Function CreateUI_AutoPass() As Task
        _autoPass2 = Await Task.Run(
            Function() New Lazy(Of progUI_AutoPass)(
                Function()
                    Return PrepDispatcher().
                        Invoke(Function()
                                   Dim objPrefWin As New progUI_AutoPass()
                                   objPrefWin.PrepAutoPass()

                                   Return objPrefWin
                               End Function, DispatcherPriority.Background)
                End Function, LazyThreadSafetyMode.ExecutionAndPublication))
    End Function

    Public Shared Async Function LaunchGui(progGui As TriggerAction) As Task
        Select Case progGui
            Case TriggerAutoCast
                GenerateGUI(progGui)
            Case TriggerAutoPass
                Await CreateUI_AutoPass()
            Case TriggerShowMenu
                Await GeneratePopupMenu()
            Case TriggerShowTrayMenu
                '  Await GenerateTrayOverlay()
        End Select
    End Function

    Public Shared Async Function LaunchGui_AP() As Task
        Await PrepDispatcher(True).InvokeAsync(
            Sub()
                osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, ProgAction.Activate)

                apHandler._DisplayTextFunc("Release Mouse To Begin")
                osGui_AutoPass2.Show()
            End Sub, DispatcherPriority.Background).Task
    End Function

    Public Shared Sub DisplayGUI(guiType As TriggerType, Optional ptPosData As osDraw.Point = Nothing)
        Select Case guiType
            Case TriggerType.AutoCast
                PrepDispatcher().Invoke(
                    Sub()
                        osGui_AutoCastProgress.InitiateAutoCast()

                    End Sub)
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
    End Sub

    Public Shared Async Function PresentPopupMenu() As Task
        Await PrepDispatcher().InvokeAsync(
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
            End Function)
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

    Private Shared Async Function TerminatePopupMenu(popupCloseAction As PopupCloseAction) As Task
        Dim objTerminateTask As DispatcherOperation(Of Task) = Nothing

        Select Case popupCloseAction
            Case ClosePopup_Default
                objTerminateTask = PrepDispatcher().InvokeAsync(
                    Async Function()
                        LiftPopupMenu()

                        Await osPopupMenu.InitPopupClose(PopupVisual_Close)
                        Await osPopupMenuOverlay.InitOverlayClose(OverlayVisual_Close)
                    End Function, DispatcherPriority.Render)
            Case ClosePopup_ByBtn
                objTerminateTask = PrepDispatcher().InvokeAsync(
                    Async Function()
                        LiftPopupMenu(True)

                        Await osPopupMenu.InitPopupClose(PopupVisual_CloseByBtn)
                        Await osPopupMenuOverlay.InitOverlayClose(OverlayVisual_CloseByBtn)
                    End Function, DispatcherPriority.Render)
            Case ClosePopup_ByCmd
                objTerminateTask = PrepDispatcher().InvokeAsync(
                    Async Function()
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

    Public Shared Sub TerminateTrayMenu()
        Try
            osTrayMenu.Close()
        Catch : End Try

        _osTrayMenu = Nothing

        Dim objTask_GenTrayMenu = PrepUI_TrayMenu()
    End Sub

    Public Shared Async Function GeneratePopupMenu() As Task
        Await Task.WhenAll(PrepUI_PopupMenu(),
                           PrepUI_PopupMenuOverlay())
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

                _autoCastProgress = Nothing
            Case TriggerAutoPass
                DismissAutoPassUI()

                If forceCreateNew Then
                    Dim objTask_LaunchAP = CreateUI_AutoPass()
                End If
            Case TriggerShowOpts

            Case TriggerShowMenu
            '    PrepUI_PopupMenuOverlay()
            Case TriggerShowTrayMenu
                '      PrepUI_PopupMenuOverlay()
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
        Catch : End Try
    End Sub

End Class

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

    'Private Shared _osTrayMenu As Lazy(Of osTrayMenu_GUI)
    'Public Shared ReadOnly Property osTrayMenu As osTrayMenu_GUI
    '    Get
    '        Try
    '            Return _osTrayMenu.Value
    '        Catch ex As Exception
    '            Return Nothing
    '        End Try
    '    End Get
    'End Property

    'Private Shared _osPrefsWindow As osPrefs_GUI
    'Public Shared ReadOnly Property osPrefsWindow As osPrefs_GUI
    '    Get
    '        Try
    '            Return _osPrefsWindow
    '        Catch ex As Exception
    '            Return Nothing
    '        End Try
    '    End Get
    'End Property

    Private Shared _osPrefsWindow As Lazy(Of osPrefs_GUI)
    Public Shared ReadOnly Property osPrefsWindow As osPrefs_GUI
        Get
            Return _osPrefsWindow.Value
        End Get
    End Property

    Public Shared _osTrayMenu As osTrayMenu_GUI
    Public Shared ReadOnly Property osTrayMenu As osTrayMenu_GUI
        Get
            Try
                Return _osTrayMenu
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property

    Public Shared _osPopupMenu As osPopupMenu_GUI
    Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
        Get
            Try
                Return _osPopupMenu
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property

    Public Shared _osPopupMenuOverlay As MenuOverlayWindow
    Public Shared ReadOnly Property osPopupMenuOverlay As MenuOverlayWindow
        Get
            Try
                Return _osPopupMenuOverlay
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property

    Private Shared ReadOnly _overlayGate As New SemaphoreSlim(1, 1)
    Private Shared ReadOnly _popupGate As New SemaphoreSlim(1, 1)
    Private Shared ReadOnly _trayGate As New SemaphoreSlim(1, 1)
    Private Shared ReadOnly _prefsGate As New SemaphoreSlim(1, 1)

    'Private Shared _osTrayMenuN As Lazy(Of osTrayMenu_GUI)
    'Public Shared ReadOnly Property osTrayMenuN As osTrayMenu_GUI
    '    Get
    '        Return _osTrayMenuN.Value
    '    End Get
    'End Property

    'Private Shared _osPopupMenuN As Lazy(Of osPopupMenu_GUI)
    'Public Shared ReadOnly Property osPopupMenuN As osPopupMenu_GUI
    '    Get
    '        Return _osPopupMenuN.Value
    '    End Get
    'End Property

    'Private Shared _osPopupMenuOverlayN As Lazy(Of MenuOverlayWindow)
    'Public Shared ReadOnly Property osPopupMenuOverlayN As MenuOverlayWindow
    '    Get
    '        Return _osPopupMenuOverlayN.Value
    '    End Get
    'End Property

    'Private Shared _osPopupMenu As Lazy(Of osPopupMenu_GUI)
    'Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
    '    Get
    '        Return _osPopupMenu.Value
    '    End Get
    'End Property

    'Private Shared _osPopupMenuOverlay As Lazy(Of MenuOverlayWindow)
    'Public Shared ReadOnly Property osPopupMenuOverlay As MenuOverlayWindow
    '    Get
    '        Return _osPopupMenuOverlay.Value
    '    End Get
    'End Property

    Private Shared _autoPass As Lazy(Of progGui_AutoPass)
    Public Shared ReadOnly Property osGui_AutoPass As progGui_AutoPass
        Get
            Return _autoPass.Value
        End Get
    End Property

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

    Private Shared _osTrayMenuN As Lazy(Of osTrayMenu_GUI)
    Public Shared ReadOnly Property osTrayMenuN As osTrayMenu_GUI
        Get
            Return _osTrayMenuN.Value
        End Get
    End Property

    Private Shared _osPopupMenuN As Lazy(Of osPopupMenu_GUI)
    Public Shared ReadOnly Property osPopupMenuN As osPopupMenu_GUI
        Get
            Return _osPopupMenuN.Value
        End Get
    End Property

    Private Shared _osPopupMenuOverlayN As Lazy(Of MenuOverlayWindow)
    Public Shared ReadOnly Property osPopupMenuOverlayN As MenuOverlayWindow
        Get
            Return _osPopupMenuOverlayN.Value
        End Get
    End Property

    Private Shared Sub PrepUI_PopupMenuOverlayN(Optional isFromTray As Boolean = False)
        If _osPopupMenuOverlayN Is Nothing Then
            _osPopupMenuOverlayN = New Lazy(Of MenuOverlayWindow)(
                GeneratePopupMenuOverlayGUI(isFromTray),
                osThreadMode.ExecutionAndPublication)
        Else
            If isFromTray Then
                osPopupMenuOverlayN.ApplyTrayConfig()
            End If
        End If

        osPopupMenuOverlayN.PrepPopupMenuOverlay()
    End Sub

    Public Shared Async Function PrepUI_PopupMenuOverlayN2(Optional isFromTray As Boolean = False) As Task
        _osPopupMenuOverlayN = Await Task.Run(
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

    Private Shared Sub PrepUI_TrayMenuN()
        If _osTrayMenuN Is Nothing Then
            _osTrayMenuN = New Lazy(Of osTrayMenu_GUI)(
            GenerateTrayMenuGUI(),
            osThreadMode.ExecutionAndPublication)
        End If
    End Sub

    Public Shared Async Function PrepUI_TrayMenuN2() As Task
        _osTrayMenuN = Await Task.Run(
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

    Private Shared Sub PrepUI_PopupMenuN()
        If _osPopupMenuN Is Nothing Then
            _osPopupMenuN = New Lazy(Of osPopupMenu_GUI)(
            GeneratePopupMenuGUI(),
            osThreadMode.ExecutionAndPublication)
        End If
    End Sub

    Public Shared Async Function PrepUI_PopupMenuN2() As Task
        _osPopupMenuN = Await Task.Run(
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
                PrepUI_PopupMenuOverlay()
                PrepUI_PopupMenu()
            Case TriggerShowTrayMenu
                PrepUI_PopupMenuOverlay(True)
        End Select
    End Sub

    Public Shared Sub PrepDispatch(sender As Object, e As EventArgs)
        Dim objOsPopupMenu = TryCast(sender, osPopupMenu_GUI)
        Dim osPopupMenuOverlay = osPopupMenuOverlayN

        DisposeUI_PopupMenu.Invoke(objOsPopupMenu)
        DisposeUI_PopupMenuOverlay.Invoke(osPopupMenuOverlay)

        ClearHandlers(objOsPopupMenu, osPopupMenuOverlay)

        _osPopupMenuN = Nothing
        _osPopupMenuOverlayN = Nothing

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
                osGui_AutoPass2.Dispatcher.Invoke(
                    Sub()
                        osFuncLib_Progress.UpdateProgStatus(TriggerAutoPass, ProgAction.Activate)
                        '   CoreDataLib.ProcessProgressEvent(ProgMode_AutoPass, ProgEvent.DispMsg, "Release Mouse To Begin")

                        osHandler_UI.osGui_AutoPass2.apHandler._DisplayTextFunc("Release Mouse To Begin")
                        osGui_AutoPass2.Show()
                    End Sub)
            Case TriggerType.ShowMenu
            Case TriggerType.ShowTrayMenu
                ShowTrayUI()
        End Select
    End Sub

    Private Shared Sub ShowTrayUI()
        PrepDispatcher().Invoke(
            Sub()
                DisplayUI_TrayOverlay.Invoke(osPopupMenuOverlay)
            End Sub)
    End Sub

    'Public Shared Async Function ShowPrefsUI1(objAwaitClose As TaskCompletionSource(Of Boolean)) As Task
    '    Dim _objAwaitClose = objAwaitClose

    '    Await PrepDispatcher().InvokeAsync(
    '        Async Function()
    '            AddHandler osPrefsWindow.Closed,
    '                Sub(sender, e)
    '                    DisposeUI_Prefs.Invoke(osPrefsWindow)
    '                    _osPrefsWindow = Nothing

    '                    _objAwaitClose.TrySetResult(True)
    '                    InitResourceAlloc()
    '                End Sub

    '            Await osPrefsWindow.ShowPrefsUICore()
    '        End Function)
    'End Function

    Public Shared Async Function PresentPopupMenu2() As Task
        Await PrepDispatcher().InvokeAsync(
            Async Function()
                With osPopupMenuOverlayN
                    AddHandler .MouseUp, osHandler_UI.pmFunc_TerminatePopupMenu

                    .InitPopupMenuOverlay()
                    Await .InitOverlayOpen(OverlayVisual_Open)
                End With

                With osPopupMenuN
                    .Owner = osPopupMenuOverlayN
                    .Owner.ShowInTaskbar = False

                    .ShowInTaskbar = False
                    .Topmost = True
                    .ShowActivated = False

                    .Show()
                    Await .TriggerPopupMenu()
                End With
            End Function)
    End Function

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
        ClearHandlers(osPopupMenuN, osPopupMenuOverlayN)
        Await TerminatePopupMenu(popupCloseAction)
    End Function

    Private Shared Async Sub TerminatePopupMenu(sender As Object, e As MouseButtonEventArgs)
        If DetermineMouseClick(e) Then
            ClearHandlers(osPopupMenuN, osPopupMenuOverlayN)

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

                        Await osPopupMenuN.InitPopupClose(PopupVisual_Close)
                        Await osPopupMenuOverlayN.InitOverlayClose(OverlayVisual_Close)
                    End Function, DispatcherPriority.Render)
            Case ClosePopup_ByBtn
                objTerminateTask = PrepDispatcher().InvokeAsync(
                    Async Function()
                        LiftPopupMenu(True)

                        Await osPopupMenuN.InitPopupClose(PopupVisual_CloseByBtn)
                        Await osPopupMenuOverlayN.InitOverlayClose(OverlayVisual_CloseByBtn)
                    End Function, DispatcherPriority.Render)
            Case ClosePopup_ByCmd
                objTerminateTask = PrepDispatcher().InvokeAsync(
                    Async Function()
                        LiftPopupMenu()

                        Await osPopupMenuN.InitPopupClose(PopupVisual_CloseByCmd)
                        Await osPopupMenuOverlayN.InitOverlayClose(OverlayVisual_CloseByCmd)
                    End Function, DispatcherPriority.Render)
        End Select

        Await objTerminateTask.Task.Unwrap()
        Await DisposePopupMenu()
    End Function

    Private Shared Sub LiftPopupMenu(Optional KillOwner As Boolean = False)
        osPopupMenuN.Topmost = True

        If KillOwner Then
            osPopupMenuN.Owner = Nothing : End If
    End Sub

    Private Shared Async Function DisposePopupMenu() As Task
        ClearHandlers(osPopupMenuN,
                      osPopupMenuOverlayN)

        Await Task.WhenAll(DismissPopupMenu(),
                           DismissPopupMenuOverlay())

        Await GeneratePopupMenu()
        InitResourceAlloc()
    End Function

    Private Shared Function DismissPopupMenu() As Task
        Return PrepDispatcher().BeginInvoke(
            Sub()
                With osPopupMenuN
                    If .IsLoaded Then
                        .IsHitTestVisible = False
                        .Opacity = 0
                        .DataContext = Nothing

                        .Close()
                    End If
                End With

                _osPopupMenuN = Nothing
            End Sub).Task
    End Function

    Private Shared Function DismissPopupMenuOverlay() As Task
        Return PrepDispatcher().BeginInvoke(
            Sub()
                Try
                    With osPopupMenuOverlayN
                        If .IsLoaded Then
                            .IsHitTestVisible = False
                            .Opacity = 0
                            .DataContext = Nothing

                            .Close()
                        End If
                    End With

                    _osPopupMenuOverlayN = Nothing
                Catch ex As Exception : End Try
            End Sub).Task
    End Function

    Private Shared Function CreateTrayMenuAsync(Optional isReload As Boolean = False) As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                PrepUI_TrayMenu(isReload)
                osHandler_UI.osTrayMenu.PrepTrayMenuInit()
            End Sub, DispatcherPriority.Normal).Task
    End Function

    Public Shared Async Function PrepTrayMenuDisp(Optional isReload As Boolean = False, Optional objTaskStatus As TaskStatusReport = Nothing) As Task
        Await CreateTrayMenuAsync(isReload)
        objTaskStatus.SetTaskComplete()
    End Function


    Private Shared Function CreateTrayMenu(Optional isReload As Boolean = False) As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                PrepUI_TrayMenu(isReload)
            End Sub, DispatcherPriority.Normal).Task
    End Function

    Private Shared Async Function GenerateTrayMenu(Optional isReload As Boolean = False) As Task
        Await Task.Run(Sub() CreateTrayMenu(isReload))
    End Function

    Public Shared Sub TerminateTrayMenu()
        Try
            osTrayMenuN.Close()
        Catch : End Try

        _osTrayMenuN = Nothing

        Dim objTask_GenTrayMenu = PrepUI_TrayMenuN2()
    End Sub

    Public Shared Async Function GeneratePopupMenu() As Task
        Await Task.WhenAll(PrepUI_PopupMenuN2(),
                           PrepUI_PopupMenuOverlayN2())
    End Function

    Public Shared Async Function CreatePopupMenuUI(objTaskStatus As TaskStatusReport) As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                PrepUI_PopupMenuOverlay()
                PrepUI_PopupMenu()
                PrepUI_TrayMenu()
                objTaskStatus.SetTaskComplete()
            End Sub, DispatcherPriority.Normal).Task
    End Function

    Public Shared Function CreatePopupMenu() As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                PrepUI_PopupMenu()
            End Sub, DispatcherPriority.Normal).Task
    End Function

    Public Shared Function CreatePopupMenuOverlay() As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                PrepUI_PopupMenuOverlay()
            End Sub, DispatcherPriority.Render).Task
    End Function

    Public Shared Function GeneratePopupOverlay() As Task
        Return Task.Run(Sub() CreatePopupMenuOverlay())
    End Function

    Public Shared Function GenerateTrayOverlay() As Task
        Return Task.Run(Sub() CreateTrayOverlay())
    End Function

    Private Shared Function CreateTrayOverlay() As Task
        Return PrepDispatcher().BeginInvoke(
            Sub()
                PrepUI_PopupMenuOverlay(True)
            End Sub).Task
    End Function

    Private Shared Sub PrepUI_PopupMenuOverlay(Optional isFromTray As Boolean = False)
        If _osPopupMenuOverlay Is Nothing Then
            _osPopupMenuOverlay = New MenuOverlayWindow(isFromTray)
        Else
            If isFromTray Then
                osPopupMenuOverlay.ApplyTrayConfig()
            End If
        End If

        osPopupMenuOverlay.PrepPopupMenuOverlay()
    End Sub

    Private Shared Sub PrepUI_TrayMenu(Optional isReload As Boolean = False)
        If _osTrayMenu Is Nothing Then
            _osTrayMenu = New osTrayMenu_GUI

            If isReload Then
                osTrayMenu.PrepTrayMenuInit()
            End If
        End If

    End Sub

    Private Shared Sub PrepUI_PopupMenu()
        If _osPopupMenu Is Nothing Then
            _osPopupMenu = New osPopupMenu_GUI
        End If
    End Sub

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

    Public Shared Async Function EnsurePopupMenuOverlayAsync(
    Optional isFromTray As Boolean = False
) As Task(Of MenuOverlayWindow)

        If _osPopupMenuOverlay IsNot Nothing Then
            If isFromTray Then _osPopupMenuOverlay.ApplyTrayConfig()
            Return _osPopupMenuOverlay
        End If

        '   Await _overlayGate.WaitAsync()
        Try
            If _osPopupMenuOverlay Is Nothing Then
                _osPopupMenuOverlay =
                Await PrepDispatcher().InvokeAsync(
                    Function()
                        Return New MenuOverlayWindow(isFromTray)
                    End Function,
                    DispatcherPriority.Render)

                _osPopupMenuOverlay.PrepPopupMenuOverlay()
            ElseIf isFromTray Then
                _osPopupMenuOverlay.ApplyTrayConfig()
            End If

            Return _osPopupMenuOverlay
        Finally
            '   _overlayGate.Release()
        End Try
    End Function

    Public Shared Async Function EnsurePopupMenuAsync() As Task(Of osPopupMenu_GUI)
        If _osPopupMenu IsNot Nothing Then Return _osPopupMenu

        '      Await _popupGate.WaitAsync()
        Try
            If _osPopupMenu Is Nothing Then
                _osPopupMenu =
                Await PrepDispatcher().InvokeAsync(
                    Function()
                        Dim win = New osPopupMenu_GUI()
                        AddHandler win.Closed, AddressOf osHandler_UI.PrepDispatch
                        Return win
                    End Function,
                    DispatcherPriority.Render)
            End If

            Return _osPopupMenu
        Finally
            '           _popupGate.Release()
        End Try
    End Function

    Public Shared Async Function EnsureTrayMenuAsync() As Task(Of osTrayMenu_GUI)
        If _osTrayMenu IsNot Nothing Then Return _osTrayMenu

        '   Await _trayGate.WaitAsync()
        Try
            If _osTrayMenu Is Nothing Then
                _osTrayMenu =
                Await PrepDispatcher().InvokeAsync(
                    Function()
                        Return New osTrayMenu_GUI()
                    End Function,
                    DispatcherPriority.Render)

                _osTrayMenu.PrepTrayMenuInit()
            End If

            Return _osTrayMenu
        Finally
            '         _trayGate.Release()
        End Try
    End Function

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

    'Public Shared Async Function ShowPrefsUI(objAwaitClose As TaskCompletionSource(Of Boolean)) As Task
    '    Dim _objAwaitClose = objAwaitClose

    '    Await PrepDispatcher().InvokeAsync(
    '        Async Function()
    '            AddHandler osPrefsWindow.Closed,
    '                Sub(sender, e)
    '                    DisposeUI_Prefs.Invoke(osPrefsWindow)
    '                    _osPrefsWindow = Nothing

    '                    _objAwaitClose.TrySetResult(True)
    '                    InitResourceAlloc()
    '                End Sub

    '            Await osPrefsWindow.ShowPrefsUICore()
    '        End Function)
    'End Function

    Public Shared Function isOverlayActive() As Boolean
        Return If(osPopupMenuOverlay IsNot Nothing, True, False)
    End Function

    Public Shared Function FetchPopupMenuOverlay() As MenuOverlayWindow
        Return osPopupMenuOverlay
    End Function

    Public Shared Function FetchPopupMenu() As osPopupMenu_GUI
        Return osPopupMenuN
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
                guiReset = _autoPass.Value

                Using objPrepData As New GUI_PrepData(guiType, guiReset)
                    If objPrepData.guiIsLoaded Then
                        If objPrepData.guiDispatch.CheckAccess() Then
                            objPrepData.guiAction.Invoke(guiReset)
                        Else
                            objPrepData.guiDispatch.
                                Invoke(objPrepData.guiAction,
                                       DispatcherPriority.Normal, guiReset)
                        End If
                    End If
                End Using

                If forceCreateNew Then
                    GenerateGUI(guiType)
                    guiReset.BeginPrep()
                End If
            Case TriggerShowOpts

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
        Catch : End Try
    End Sub

End Class

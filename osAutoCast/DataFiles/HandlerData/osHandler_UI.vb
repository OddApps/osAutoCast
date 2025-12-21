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

    Private Shared objTask_PreloadShaders As New List(Of Task)
    Private Shared objTask_LoadShaders As New List(Of Task)

    Public Shared objBuildShader As Task(
        Of Dictionary(Of String, Byte())) = Nothing

    Private Shared _osPrefs As Lazy(Of osPrefs)
    Public Shared ReadOnly Property osGui_Prefs As osPrefs
        Get
            Return _osPrefs.Value
        End Get
    End Property



    Private Shared _osTrayMenu As Lazy(Of osTrayMenu_GUI)
    Public Shared ReadOnly Property osTrayMenu As osTrayMenu_GUI
        Get
            Try
                Return _osTrayMenu.Value
            Catch ex As Exception
                Return Nothing
            End Try
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

    Public Shared Function CreateOptsUI() As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                _osPrefs = New Lazy(Of osPrefs)(
                    Function()
                        Return New osPrefs()
                    End Function, osThreadMode.ExecutionAndPublication)
            End Sub).Task
    End Function

    Public Shared Async Function LoadOptsUI(objTask_BindPrefLst As Task(Of List(Of BindingDef))) As Task
        Await osGui_Prefs.osPrefsPrepAsync(objTask_BindPrefLst)
    End Function

    Public Shared Async Function LoadOptsUI() As Task
        Await CreateOptsUI()

        Dim objPrefLst = osGui_Prefs.
            BuildPrefBindingsAsync()

        Await osGui_Prefs.osPrefsPrepAsync(objPrefLst)
    End Function

    Private Shared Async Function PrepShaderData() As Task
        Dim objTask_InitGraphics = osHandler_Graphics.EnsureCreated()
        Dim objTask_ComposeShaders = CoreDataLib.ComposeShaderIdx()

        Await Task.WhenAll(objTask_InitGraphics,
                           objTask_ComposeShaders)
    End Function

    'Public Shared Async Function PrepAndLoadUI(objTaskStatus As TaskStatusReport) As Task
    '    Await Task.Run(Async Function()
    '                       Try
    '                           objBuildShader = BuildShaderCatalog()

    '                           Await CreateOptsUI()

    '                           Dim objPrefLst = osGui_Prefs.BuildPrefBindingsAsync()
    '                           Dim objTask_PrepGraphics = PrepShaderData()

    '                           Dim objTask_LoadPrefs = LoadOptsUI(objPrefLst)


    '                           Await Task.WhenAll(CreatePopupMenuOverlay(), CreatePopupMenu(),
    '                       LoadAllShaders(objTask_PrepGraphics, objBuildShader), objTask_LoadPrefs)
    '                       Finally
    '                           objTaskStatus.SetTaskComplete()
    '                       End Try
    '                   End Function)
    'End Function

    'Public Shared Async Function PrepAndLoadUI(objTaskStatus As TaskStatusReport) As Task
    '    Try
    '        objBuildShader = BuildShaderCatalog()

    '        Dim objTask_LoadPrefs = LoadOptsUI()
    '        Dim objTask_PrepGraphics = PrepShaderData()

    '        Await Task.WhenAll(CreatePopupMenuOverlay(), CreatePopupMenu(),
    '                       LoadAllShaders(objTask_PrepGraphics, objBuildShader), objTask_LoadPrefs)
    '    Finally
    '        objTaskStatus.SetTaskComplete()
    '    End Try
    'End Function

    Public Shared Async Function PrepAndLoadUI(objTaskStatus As TaskStatusReport) As Task
        Await Task.Run(Async Function()
                           Try
                               objBuildShader = BuildShaderCatalog()

                               Await CreateOptsUI()

                               Dim objPrefLst = osGui_Prefs.BuildPrefBindingsAsync()
                               Dim objTask_PrepGraphics = PrepShaderData()

                               Dim objTask_LoadPrefs = LoadOptsUI(objPrefLst)


                               Await Task.WhenAll(CreatePopupMenuOverlay(), CreatePopupMenu(),
                           LoadAllShaders(objTask_PrepGraphics, objBuildShader), objTask_LoadPrefs)
                           Finally
                               objTaskStatus.SetTaskComplete()
                           End Try
                       End Function)
    End Function



    Private Shared Async Function LoadAllShaders(objTaskPrepGraphics As Task, objShaderTask As Task(Of Dictionary(Of String, Byte()))) As Task(Of Task)
        Await objTaskPrepGraphics

        Dim objLoadedShaders = Await objShaderTask
        Await PreloadShaderCatalog(objLoadedShaders)

        Return Task.Run(
            Function()
                For Each objShader In ShaderDetailsIdx
                    objTask_LoadShaders.Add(AddShaderToIdx(objShader))
                Next

                Return Task.WhenAll(objTask_LoadShaders)
            End Function)
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

    Public Shared Async Function PrepTrayMenuDisp() As Task
        Await GenerateTrayMenu()
    End Function

    Private Shared Function CreateTrayMenuAsync(Optional isReload As Boolean = False) As Task
        Return PrepDispatcher().InvokeAsync(
        Sub()
            PrepUI_TrayMenu(isReload)
        End Sub,
        DispatcherPriority.Normal
    ).Task
    End Function

    Public Shared Async Function PrepTrayMenuDispr(Optional isReload As Boolean = False) As Task
        Await CreateTrayMenuAsync(isReload)
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
            osTrayMenu.Close()
        Catch : End Try

        _osTrayMenu = Nothing

        Dim objTask_GenTrayMenu = GenerateTrayMenu(True)
    End Sub

    Private Shared Async Function GeneratePopupMenu() As Task
        Await Task.WhenAll(CreatePopupMenuOverlay(),
                       CreatePopupMenu())
    End Function



    Private Shared Function CreatePopupMenu() As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                PrepUI_PopupMenu()
            End Sub, DispatcherPriority.Normal).Task
    End Function

    Private Shared Function CreatePopupMenuOverlay() As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                PrepUI_PopupMenuOverlay()
            End Sub, DispatcherPriority.Normal).Task
    End Function

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

    Public Shared Async Function ResetOptsUI(isAsync As Boolean) As Task
        _osPrefs = New Lazy(Of osPrefs)(
            Function() New osPrefs(), osThreadMode.ExecutionAndPublication)

        Dim objPrefLst = osGui_Prefs.BuildPrefBindingsAsync()
        Await osGui_Prefs.osPrefsPrepAsync(objPrefLst)
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

    Private Shared Sub PrepUI_TrayMenu(Optional isReload As Boolean = False)
        If _osTrayMenu Is Nothing Then
            _osTrayMenu = New Lazy(Of osTrayMenu_GUI)(
                GenerateTrayMenuGUI(), osThreadMode.ExecutionAndPublication)

            If isReload Then
                osTrayMenu.PrepTrayMenuInit()
            End If
        End If

    End Sub

    Private Shared Sub PrepUI_PopupMenu()
        If _osPopupMenu Is Nothing Then
            _osPopupMenu = New Lazy(Of osPopupMenu_GUI)(
                GeneratePopupMenuGUI(), osThreadMode.ExecutionAndPublication)
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

Imports System.Windows.Forms
Imports System.Windows.Threading
Imports System.Threading
Imports osDraw = System.Drawing

Public NotInheritable Class osHandler_UI
    Public Shared Property osGui_InputMonitor As Form
    Public Shared Property osGui_InputMonitor2 As Window

    Private Shared _osPrefs As New Lazy(Of osPrefs)(
    Function() New osPrefs(), LazyThreadSafetyMode.ExecutionAndPublication)
    Public Shared ReadOnly Property osGui_Prefs As osPrefs
        Get
            Return _osPrefs.Value
        End Get
    End Property

    'Private Shared _osPopupMenu As Lazy(Of osPopupMenu_GUI)
    'Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
    '    Get
    '        Return _osPopupMenu.Value
    '    End Get
    'End Property

    Private Shared _osPopupMenu As New Lazy(Of osPopupMenu_GUI)(
                    Function()
                        Return Application.Current.Dispatcher.
                        Invoke(Function()
                                   Return New osPopupMenu_GUI()
                               End Function)
                    End Function, LazyThreadSafetyMode.ExecutionAndPublication)
    Public Shared ReadOnly Property osPopupMenu As osPopupMenu_GUI
        Get
            Return _osPopupMenu.Value
        End Get
    End Property

    'Private Shared _osPopupMenuOverlay As New Lazy(Of MenuOverlayWindow)(
    'Function() New MenuOverlayWindow(), LazyThreadSafetyMode.ExecutionAndPublication)
    'Public Shared ReadOnly Property osPopupMenuOverlay As MenuOverlayWindow
    '    Get
    '        Return _osPopupMenuOverlay.Value
    '    End Get
    'End Property

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

    Private Shared pmFunc_TerminatePopupMenu As MouseButtonEventHandler = AddressOf TerminatePopupMenu

    Private Shared Sub GenerateGUI(objGenGui As TriggerAction)
        Select Case objGenGui
            Case TriggerAction.AutoPass
                _autoPass = New Lazy(Of progGui_AutoPass)(
                    Function()
                        Return Application.Current.Dispatcher.
                        Invoke(Function()
                                   Return New progGui_AutoPass()
                               End Function)
                    End Function, LazyThreadSafetyMode.ExecutionAndPublication)
            Case TriggerAction.AutoCast
                With CoreDataLib.GetProgSizeReport(TriggerType.AutoCast)
                    _autoCastProgress = New ProgBarGui_AutoCast(.pWidth, .pHeight,
                                                                osFuncLib_Progress.ProgTimeSpan, AddressOf EaseProgress)
                End With

                Dim guiLoad = _autoCastProgress.Handle
            Case TriggerAction.ShowMenu
                If _osPopupMenu Is Nothing Then
                    _osPopupMenu = New Lazy(Of osPopupMenu_GUI)(
                    Function()
                        Return Application.Current.Dispatcher.
                        Invoke(Function()
                                   Return New osPopupMenu_GUI()
                               End Function)
                    End Function, LazyThreadSafetyMode.ExecutionAndPublication)
                End If

                'osPopupMenuOverlay.PrepPopupMenuOverlay()

                '_osPopupMenu = New Lazy(Of osPopupMenu_GUI)(
                '    Function()
                '        Return Application.Current.Dispatcher.
                '        Invoke(Function()
                '                   Return New osPopupMenu_GUI()
                '               End Function)
                '    End Function, LazyThreadSafetyMode.ExecutionAndPublication)
        End Select
    End Sub

    Public Shared Sub PreloadForms(guiInputMon As Window)
        Dim handle As IntPtr = osGui_Prefs.Handle

        osGui_Prefs.osPrefsPrep()

        Dim objOsInputMon As New osInputMonitor
        Dim tmpHandle = objOsInputMon.Handle

        osGui_InputMonitor = objOsInputMon
        osGui_InputMonitor2 = guiInputMon
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
        End Select
    End Function

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
                'Application.Current.Dispatcher.Invoke(
                '    Sub()
                '        With osPopupMenuOverlay
                '            AddHandler .MouseDown, pmFunc_TerminatePopupMenu

                '            .InitPopupMenuOverlay()
                '            .Show()
                '        End With
                '    End Sub)

                Application.Current.Dispatcher.Invoke(
                    Sub()
                        Dim w = _osPopupMenu.Value

                        w.Owner = Application.Current.MainWindow
                        w.InitPopupMenu()
                        w.ShowInTaskbar = False
                        ' w.Topmost = True
                        w.ShowActivated = False

                        w.WindowStartupLocation = WindowStartupLocation.Manual
                        w.Left = 0
                        w.Top = 0
                        w.Width = 100
                        w.Height = 100
                        'w.Width = SystemParameters.PrimaryScreenWidth
                        'w.Height = SystemParameters.PrimaryScreenHeight

                        RemoveHandler w.Closed, AddressOf Popup_Closed
                        AddHandler w.Closed, AddressOf Popup_Closed

                        w.Show()
                    End Sub)
        End Select
    End Sub

    Private Shared Sub ClosePopupMenu()
        Application.Current.Dispatcher.Invoke(
        Sub()
            If _osPopupMenu IsNot Nothing AndAlso _osPopupMenu.IsValueCreated Then
                Dim w = _osPopupMenu.Value
                Try
                    RemoveHandler w.Closed, AddressOf Popup_Closed
                    w.Close()                      ' <-- this actually tears it down
                Catch
                    ' ignore if already closed
                End Try
            End If
            _osPopupMenu = Nothing                ' release our strong reference
        End Sub)
    End Sub

    Private Shared Sub Popup_Closed(sender As Object, e As EventArgs)
        Dim w = TryCast(sender, Window)
        If w IsNot Nothing Then RemoveHandler w.Closed, AddressOf Popup_Closed
        _osPopupMenu = Nothing
    End Sub

    Private Shared Sub TerminatePopupMenu()
        ' RemoveHandler osPopupMenuOverlay.MouseDown, pmFunc_TerminatePopupMenu
        ClosePopupMenu()
        ' osHandler_UI.ResetUI(TriggerAction.ShowMenu)
        Dim doGameFocus = CoreDataLib.SetGameFocus()
    End Sub

    Public Shared Sub ResetOptsUI()
        _osPrefs = New Lazy(Of osPrefs)(
            Function() New osPrefs(), LazyThreadSafetyMode.ExecutionAndPublication)

        Dim handle As IntPtr = osGui_Prefs.Handle

        osGui_Prefs.osPrefsPrep()
    End Sub

    Private Shared Sub ResetPopupMenuOverlay()
        _osPopupMenuOverlay = New Lazy(Of MenuOverlayWindow)(
            Function()
                Return Application.Current.Dispatcher.
                Invoke(Function()
                           Return New MenuOverlayWindow()
                       End Function)
            End Function, LazyThreadSafetyMode.ExecutionAndPublication)
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
                ' guiReset = _osPopupMenu.Value

                ClosePopupMenu()
                'Using objPrepData As New GUI_PrepData(guiType, guiReset)
                '    If objPrepData.guiIsLoaded Then
                '        If objPrepData.guiDispatch.CheckAccess() Then
                '            objPrepData.guiAction.Invoke(guiReset)
                '        Else
                '            objPrepData.guiDispatch.Invoke(objPrepData.guiAction,
                '                                            DispatcherPriority.Normal, guiReset)
                '        End If
                '    End If
                'End Using

                'guiReset = _osPopupMenuOverlay.Value

                'Using objPrepData As New GUI_PrepData(guiType, guiReset, True)
                '    If objPrepData.guiIsLoaded Then
                '        If objPrepData.guiDispatch.CheckAccess() Then
                '            objPrepData.guiAction.Invoke(guiReset)
                '        Else
                '            objPrepData.guiDispatch.Invoke(objPrepData.guiAction,
                '                                            DispatcherPriority.Normal, guiReset)
                '        End If
                '    End If
                'End Using

                ' _osPopupMenu = Nothing
                ' _osPopupMenuOverlay = Nothing

                'ResetPopupMenuOverlay()
        End Select

        guiReset = Nothing

        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
    End Sub
End Class

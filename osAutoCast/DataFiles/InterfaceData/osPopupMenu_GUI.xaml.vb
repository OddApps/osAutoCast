Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports osAutoCast.GameMenuOpts
Imports osAutoCast.CoreDataLib
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports osAutoCast.DataTypeLib.PromptResponse

Public Class osPopupMenu_GUI

    Private _PopupMenuData As PopupMenuDataModel

    Private Sub pmCmd_ShowGameMenu(sender As Object, e As RoutedEventArgs) Handles btnShowGameMenu.Checked
        ShowGameMenuItem()
    End Sub

    Private Sub ShowGameMenuItem()
        _PopupMenuData.DisplayGameMenuItem = If(CoreDataLib.IsGameRunning(),
            GameMenuItem.ShowClose, GameMenuItem.ShowStart)
    End Sub

    Private Async Sub pmCmd_ShowOpts(sender As Object, e As RoutedEventArgs) Handles pmBtn_ShowOptions.Click
        ExitPopupMenu()

        osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
        Await osFuncLib_ShowOpts.ExecuteDispOpts()

        Dim doGameFocus = SetGameFocus()
    End Sub

    Private Sub osStopApp()
        ExitPopupMenu()

        osTrayIcon.Visible = False
        End
    End Sub

    Private Sub pmCmd_StartGame(sender As Object, e As RoutedEventArgs) Handles pmBtn_StartGame.Click
        Process.Start(New ProcessStartInfo With {
                          .FileName = dirMtgaExe,
                          .WorkingDirectory = dirMtga,
                          .WindowStyle = ProcessWindowStyle.Maximized
                      })

        ExitPopupMenu()
    End Sub

    Private Sub pmCmd_CloseGame(sender As Object, e As RoutedEventArgs) Handles pmBtn_CloseGame.Click
        ExitPopupMenu()

        Dim chkConfirmCloseGame = GetResponse(PromptType.GameMenu_Leave)

        If chkConfirmCloseGame = isYes Then
            Dim cmdCloseMTGA = CmdRunner.RunCmd("taskkill", "/f /im MTGA.exe")
        End If

        Me.Close()
    End Sub

    Private Sub pmCmd_Exit(sender As Object, e As RoutedEventArgs) Handles pmBtn_Exit.Click
        ExitPopupMenu()

        Dim chkConfirmExit = GetResponse(PromptType.CloseApp)
        If chkConfirmExit = isNo Then Exit Sub

        osStopApp()
    End Sub

    Public Sub InitPopupMenu()
        _PopupMenuData = New PopupMenuDataModel
        DataContext = _PopupMenuData

        AddHandler RootOverlay.MouseDown, AddressOf OnOverlayClick
        AddHandler CenterCard.MouseDown, Sub(s, e) e.Handled = True

        ShowGameMenuItem()
    End Sub

    Private Sub ExitPopupMenu()
        'Application.Current.Dispatcher.Invoke(
        '    Sub()
        osHandler_UI.ResetUI(TriggerAction.ShowMenu)
        '    End Sub)
    End Sub

    Private Sub OnOverlayClick(sender As Object, e As MouseButtonEventArgs)
        Close()
        ExitPopupMenu()
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        MyBase.OnClosed(e)

        CommandBindings.Clear()
        InputBindings.Clear()

        ' 3) Clear visual-tree bindings (you already clear Me; also clear children)
        DetachAllBindings(Me)

        ' 4) Break DC & resources
        DataContext = Nothing
        _PopupMenuData.Dispose()
        _PopupMenuData = Nothing
        BindingOperations.ClearAllBindings(Me)
        Resources.MergedDictionaries.Clear()
        Resources.Clear()
        Style = Nothing

        ' 5) Hint the GC (optional)
        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
    End Sub

    Private Sub DetachAllBindings(root As DependencyObject)
        If root Is Nothing Then Return
        BindingOperations.ClearAllBindings(root)
        Dim count = VisualTreeHelper.GetChildrenCount(root)
        For i = 0 To count - 1
            Dim child = VisualTreeHelper.GetChild(root, i)
            DetachAllBindings(child)
        Next
    End Sub

End Class
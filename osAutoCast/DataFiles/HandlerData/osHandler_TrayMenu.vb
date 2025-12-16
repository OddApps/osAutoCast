Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Data
Imports System.Windows.Media
Imports osControls = System.Windows.Controls

Public Module osHandler_TrayMenu

    Public Function BuildTrayContextMenu(ownerWindow As Window) As ContextMenu
        If ownerWindow Is Nothing Then Throw New ArgumentNullException(NameOf(ownerWindow))

        Dim tryGetStyle As Func(Of String, Style) = Function(key)
                                                        If Application.Current IsNot Nothing AndAlso Application.Current.Resources.Contains(key) Then
                                                            Return TryCast(Application.Current.Resources(key), Style)
                                                        End If
                                                        Return Nothing
                                                    End Function

        Dim traymenuMainContainer As New osControls.Border With {
            .Name = "traymenuMainContainer",
            .CacheMode = New BitmapCache(),
            .Padding = New Thickness(0)
        }

        RenderOptions.SetBitmapScalingMode(traymenuMainContainer, BitmapScalingMode.HighQuality)

        Dim borderStyle = tryGetStyle("BorderStyle1")
        If borderStyle IsNot Nothing Then traymenuMainContainer.Style = borderStyle

        Dim traymenuContentContainer As New Grid With {
            .Name = "traymenuContentContainer"
        }
        Dim gridStyle = tryGetStyle("GridStyle1")
        If gridStyle IsNot Nothing Then traymenuContentContainer.Style = gridStyle

        traymenuContentContainer.ColumnDefinitions.Add(New ColumnDefinition())

        traymenuContentContainer.RowDefinitions.Add(New RowDefinition() With {.Height = GridLength.Auto})
        traymenuContentContainer.RowDefinitions.Add(New RowDefinition() With {.Height = GridLength.Auto})
        traymenuContentContainer.RowDefinitions.Add(New RowDefinition() With {.Height = GridLength.Auto})
        traymenuContentContainer.RowDefinitions.Add(New RowDefinition() With {.Height = GridLength.Auto})

        Dim pmChk_IsEnabled As New ToggleButton With {
            .Name = "pmChk_IsEnabled",
            .Focusable = False
        }
        Grid.SetRow(pmChk_IsEnabled, 0)
        Grid.SetColumn(pmChk_IsEnabled, 0)

        Dim pmBtnEnabledStatusStyle = tryGetStyle("pmBtnEnabledStatusStyle")
        If pmBtnEnabledStatusStyle IsNot Nothing Then pmChk_IsEnabled.Style = pmBtnEnabledStatusStyle

        Dim bindingEnabled As New Binding("IsAppEnabled") With {
            .Source = ownerWindow,
            .Mode = BindingMode.TwoWay
        }
        pmChk_IsEnabled.SetBinding(ToggleButton.IsCheckedProperty, bindingEnabled)

        Dim stackPanel As New StackPanel With {
            .Name = "stackPanel"
        }
        Grid.SetRow(stackPanel, 1)
        Grid.SetColumn(stackPanel, 0)

        stackPanel.DataContext = ownerWindow

        Dim btnShowGameMenu As New ToggleButton With {
            .Name = "btnShowGameMenu",
            .Content = "Game Menu",
            .IsChecked = False
        }
        Dim toggleStyle1 = tryGetStyle("ToggleButtonStyle1")
        If toggleStyle1 IsNot Nothing Then btnShowGameMenu.Style = toggleStyle1

        Dim GameMenuPanel As New Border With {
            .Name = "GameMenuPanel"
        }
        Dim hideShowGameMenuStyle = tryGetStyle("HideShowGameMenu")
        If hideShowGameMenuStyle IsNot Nothing Then GameMenuPanel.Style = hideShowGameMenuStyle

        Dim innerStack As New StackPanel()

        Dim pmBtn_StartGame As New Button With {
            .Name = "pmBtn_StartGame",
            .Content = "Start MTGA"
        }
        Dim startStyle = tryGetStyle("GameMenu_ShowStart")
        If startStyle IsNot Nothing Then pmBtn_StartGame.Style = startStyle

        Dim pmBtn_CloseGame As New Button With {
            .Name = "pmBtn_CloseGame",
            .Content = "Close MTGA"
        }
        Dim closeStyle = tryGetStyle("GameMenu_ShowClose")
        If closeStyle IsNot Nothing Then pmBtn_CloseGame.Style = closeStyle

        innerStack.Children.Add(pmBtn_StartGame)
        innerStack.Children.Add(pmBtn_CloseGame)
        GameMenuPanel.Child = innerStack

        stackPanel.Children.Add(btnShowGameMenu)
        stackPanel.Children.Add(GameMenuPanel)

        AddHandler btnShowGameMenu.Checked, Sub(s, e) GameMenuPanel.Visibility = Visibility.Visible
        AddHandler btnShowGameMenu.Unchecked, Sub(s, e) GameMenuPanel.Visibility = Visibility.Collapsed

        GameMenuPanel.Visibility = Visibility.Collapsed

        Dim pmBtn_ShowOptions As New Button With {
            .Name = "pmBtn_ShowOptions",
            .Content = "Options",
            .Focusable = False
        }
        Grid.SetRow(pmBtn_ShowOptions, 2)
        Grid.SetColumn(pmBtn_ShowOptions, 0)
        Dim btnStyle1 = tryGetStyle("ButtonStyle1")
        If btnStyle1 IsNot Nothing Then pmBtn_ShowOptions.Style = btnStyle1

        Dim pmBtn_Exit As New Button With {
            .Name = "pmBtn_Exit",
            .Content = "Exit",
            .Focusable = False
        }
        Grid.SetRow(pmBtn_Exit, 3)
        Grid.SetColumn(pmBtn_Exit, 0)
        Dim pmBtnBrdrStyle = tryGetStyle("pmBtnBrdrStyle")
        If pmBtnBrdrStyle IsNot Nothing Then pmBtn_Exit.Style = pmBtnBrdrStyle

        traymenuContentContainer.Children.Add(pmChk_IsEnabled)
        traymenuContentContainer.Children.Add(stackPanel)
        traymenuContentContainer.Children.Add(pmBtn_ShowOptions)
        traymenuContentContainer.Children.Add(pmBtn_Exit)

        traymenuMainContainer.Child = traymenuContentContainer

        Dim containerMenuItem As New MenuItem With {
            .StaysOpenOnClick = True,
            .Focusable = False
        }

        containerMenuItem.Header = traymenuMainContainer

        Dim ctx As New ContextMenu()
        ctx.Items.Add(containerMenuItem)

        AddHandler pmBtn_StartGame.Click, Sub(s, e)

                                              MessageBox.Show("Start MTGA clicked")
                                          End Sub

        AddHandler pmBtn_CloseGame.Click, Sub(s, e)

                                              MessageBox.Show("Close MTGA clicked")
                                          End Sub

        AddHandler pmBtn_ShowOptions.Click, Sub(s, e)

                                                MessageBox.Show("Options clicked")
                                            End Sub

        AddHandler pmBtn_Exit.Click, Sub(s, e)
                                         Application.Current.Shutdown()
                                     End Sub

        AddHandler ctx.PreviewMouseDown, Sub(s, e)

                                             e.Handled = False
                                         End Sub

        Return ctx
    End Function

End Module

Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports osAutoCast.GameMenuOpts
Imports osAutoCast.CoreDataLib
Imports System.Runtime.InteropServices
Imports System.Windows.Interop

Public Class osPopupMenu_GUI

    Private Sub pmCmd_ShowGameMenu(sender As Object, e As RoutedEventArgs) Handles btnShowGameMenu.Checked
        ApplyMenuState()
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
        ExitPopupMenu()

        Process.Start(New ProcessStartInfo With {
                          .FileName = dirMtgaExe,
                          .WorkingDirectory = dirMtga,
                          .WindowStyle = ProcessWindowStyle.Maximized
                      })
    End Sub

    Private Sub pmCmd_CloseGame(sender As Object, e As RoutedEventArgs) Handles pmBtn_CloseGame.Click
        ExitPopupMenu()

        Dim chkConfirmCloseGame = GetResponse(PromptType.GameMenu_Leave)

        If chkConfirmCloseGame = System.Windows.Forms.DialogResult.Yes Then
            Dim cmdCloseMTGA = CmdRunner.RunCmd("taskkill", "/f /im MTGA.exe")
        End If

        Me.Close()
    End Sub

    Private Sub pmCmd_Exit(sender As Object, e As RoutedEventArgs) Handles pmBtn_Exit.Click
        ExitPopupMenu()

        Dim chkConfirmExit = GetResponse(PromptType.CloseApp)
        If chkConfirmExit = System.Windows.Forms.DialogResult.No Then Exit Sub

        osStopApp()
    End Sub

End Class

Partial Public Class osPopupMenu_GUI
    Implements INotifyPropertyChanged

    Private _dispGameMenuItem As GameMenuItem
    Public Property DisplayGameMenuItem As GameMenuItem
        Get
            Return _dispGameMenuItem
        End Get
        Set(value As GameMenuItem)
            If _dispGameMenuItem <> value Then
                _dispGameMenuItem = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Private _isAppEnabled As Boolean
    Public Property IsAppEnabled As Boolean
        Get
            Return osIsEnabled
        End Get
        Set(value As Boolean)
            If osIsEnabled <> value Then
                Dim result = ConfirmStatusChange(value)

                If result = UpdateStatus.CancelUpdate Then
                    ExitPopupMenu()
                    Exit Property
                Else
                    SetNewStatus(value)

                    OnPropertyChanged()
                    ExitPopupMenu()
                End If
            End If
        End Set
    End Property

    Private Function ConfirmStatusChange(newStatus As Boolean) As UpdateStatus
        If newStatus = False Then
            Dim chkConfirmDisable = GetResponse(PromptType.DisableService)

            Select Case chkConfirmDisable
                Case System.Windows.Forms.DialogResult.Yes
                    osFuncLib_InputScan.SetMonitorState(MonitorStatus.Paused)
                    Return UpdateStatus.ToDisabled
                Case Else
                    Return UpdateStatus.CancelUpdate
            End Select
        Else
            osFuncLib_InputScan.SetMonitorState(MonitorStatus.Starting)
            objInputMon.RestartMonitor()

            Return UpdateStatus.ToEnabled
        End If
    End Function

    Public Sub VerifyStatusChange(setStatus As Boolean)
        Dim result = ConfirmStatusChange(setStatus)
        If result = UpdateStatus.CancelUpdate Then Return

        SetNewStatus(setStatus)
    End Sub

    Private Sub SetNewStatus(setStatus As Boolean)
        osIsEnabled = setStatus
        _isAppEnabled = setStatus

        UpdateTray(setStatus)
    End Sub

    Public Sub New()
        InitializeComponent()
        InitPopupMenu()
    End Sub

    Private Sub InitPopupMenu()
        DataContext = Me
        _isAppEnabled = True

        ApplyMenuState()
    End Sub

    Private Sub ExitPopupMenu()
        Application.Current.Dispatcher.Invoke(
            Sub()
                osHandler_UI.ResetUI(TriggerAction.ShowMenu)
            End Sub)
    End Sub

    Private Sub ApplyMenuState()
        Dim chkGameState = CoreDataLib.IsGameRunning()
        DisplayGameMenuItem = If(chkGameState, GameMenuItem.ShowClose, GameMenuItem.ShowStart)
    End Sub

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub OnPropertyChanged(<CallerMemberName> Optional name As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
    End Sub
End Class
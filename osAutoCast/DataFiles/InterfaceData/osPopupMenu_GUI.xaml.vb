Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports osAutoCast.GameMenuOpts
Imports osAutoCast.CoreDataLib
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports osAutoCast.DataTypeLib.PromptResponse
Imports System.Windows.Media.Animation
Imports System.Threading

Public Class osPopupMenu_GUI

    Private _hasAnimated As Boolean = False

    Private objTask_Closing As TaskCompletionSource(Of Boolean)

    Private objAnimation_Open As osPopupAnimation = Nothing
    Private objAnimation_Close As osPopupAnimation = Nothing

    Private OpenCompleteEvent As EventHandler = AddressOf OpenComplete

    Public Async Function InitPopupClose() As Task
        BeginClosingTask(objTask_Closing)

        objAnimation_Close = New osPopupAnimation(AnimationType.aniClose)

        AddHandler objAnimation_Close.aniY.Completed,
            Sub()
                PopupCloseComplete(objTask_Closing)
            End Sub

        Me.popScale.BeginAnimation(ScaleTransform.ScaleXProperty, objAnimation_Close.aniX)
        Me.popScale.BeginAnimation(ScaleTransform.ScaleYProperty, objAnimation_Close.aniY)

        Me.popupMainContainer.BeginAnimation(Border.OpacityProperty, objAnimation_Close.aniFade)

        Dim resPopupClose = Await objTask_Closing.Task

    End Function

    Public Sub InitPopupOpen()
        If Not _hasAnimated Then
            _hasAnimated = True
            ActivateWindowDisplay()
        End If
    End Sub

    Private Sub SetAniDuration(ByRef objDur As Duration, valDur As TimeSpan)
        objDur = New Duration(valDur)
    End Sub

    Private Sub BeginClosingTask(ByRef objCloseResult As TaskCompletionSource(Of Boolean))
        If objCloseResult IsNot Nothing Then objCloseResult = Nothing

        objCloseResult = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                    RunContinuationsAsynchronously)
    End Sub

    Private Sub PopupCloseComplete(ByRef objTask As TaskCompletionSource(Of Boolean))
        objTask.TrySetResult(True)

        objAnimation_Close.DisposeAni()
        objAnimation_Close = Nothing

        Me.Owner = Nothing
    End Sub

    Private Sub ActivateWindowDisplay()

        objAnimation_Open = New osPopupAnimation(AnimationType.aniOpen)

        AddHandler objAnimation_Open.aniY.Completed, OpenCompleteEvent

        Me.popScale.BeginAnimation(ScaleTransform.ScaleXProperty, objAnimation_Open.aniX)
        Me.popScale.BeginAnimation(ScaleTransform.ScaleYProperty, objAnimation_Open.aniY)

        Me.popupMainContainer.BeginAnimation(Border.OpacityProperty, objAnimation_Open.aniFade)
    End Sub

    Private Sub OpenComplete()
        Try
            RemoveHandler objAnimation_Open.aniY.Completed, OpenCompleteEvent
        Catch : End Try

        objAnimation_Open = Nothing
    End Sub

    Private Sub pmCmd_ShowGameMenu(sender As Object, e As RoutedEventArgs) Handles btnShowGameMenu.Checked
        ShowGameMenuItem()
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
                          .FileName = dirMtgaExe, .WorkingDirectory = dirMtga,
                          .WindowStyle = ProcessWindowStyle.Maximized
                      })

        ExitPopupMenu()
    End Sub

    Private Sub pmCmd_CloseGame(sender As Object, e As RoutedEventArgs) Handles pmBtn_CloseGame.Click
        Dim chkConfirmCloseGame = GetResponse(PromptType.GameMenu_Leave)

        If chkConfirmCloseGame = isYes Then
            Dim cmdCloseMTGA = CmdRunner.RunCmd("taskkill", "/f /im MTGA.exe")
            ExitPopupMenu()
        End If
    End Sub

    Private Sub pmCmd_Exit(sender As Object, e As RoutedEventArgs) Handles pmBtn_Exit.Click
        Dim chkConfirmExit = GetResponse(PromptType.CloseApp)
        If chkConfirmExit = isNo Then Exit Sub

        osStopApp()
    End Sub

    Public Sub InitPopupMenu()
        ShowGameMenuItem()
    End Sub

    Private Sub ExitPopupMenu()
        osHandler_UI.DispatchUI()
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        MyBase.OnClosed(e)

        ClearResources(Me)

        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
    End Sub

    Private Sub ClearResources(root As DependencyObject)
        For Each sb In Me.Resources.Values.OfType(Of Animation.Storyboard)()
            sb.Remove(Me)
        Next

        Me.CommandBindings.Clear()
        Me.InputBindings.Clear()

        If root Is Nothing Then Return

        BindingOperations.ClearAllBindings(root)

        For i = 0 To VisualTreeHelper.GetChildrenCount(root) - 1
            ClearResources(VisualTreeHelper.GetChild(root, i))
        Next

        Me.Resources.MergedDictionaries.Clear()
        Me.Resources.Clear()

        Me.Style = Nothing

        Me.DataContext = Nothing
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

    Public _isAppEnabled As Boolean
    Public Property IsAppEnabled As Boolean
        Get
            Return osStatus_Fetch()
        End Get
        Set(value As Boolean)
            If osIsEnabled <> value Then
                Dim result = ConfirmStatusChange(value)

                If result <> UpdateStatus.CancelUpdate Then
                    SetNewStatus(value)
                    OnPropertyChanged()
                End If
            End If
        End Set
    End Property

    Private Function ConfirmStatusChange(newStatus As Boolean) As UpdateStatus
        If newStatus = False Then
            Dim chkConfirmDisable = GetResponse(PromptType.DisableService)

            Select Case chkConfirmDisable
                Case isYes
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

    Private Sub SetNewStatus(setStatus As Boolean)
        osIsEnabled = setStatus
        osEnabledStatus = setStatus
        _isAppEnabled = setStatus

        UpdateTray(setStatus)
    End Sub

    Public Sub New()
        InitializeComponent()
        ShowGameMenuItem()
    End Sub

    Private Sub ShowGameMenuItem()
        DisplayGameMenuItem = If(CoreDataLib.IsGameRunning(),
            GameMenuItem.ShowClose, GameMenuItem.ShowStart)
    End Sub

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub OnPropertyChanged(<CallerMemberName> Optional name As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
    End Sub

End Class
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.osStyle
Imports osAutoCast.CoreDataLib
Imports osAutoCast.GameMenuOpts
Imports osAutoCast.DataTypeLib.VisualAction
Imports osAutoCast.DataTypeLib.UpdateStatus
Imports osAutoCast.DataTypeLib.AnimationType
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.DataTypeLib.PopupVisualType
Imports osAutoCast.DataTypeLib.PopupCloseAction
Imports osAutoCast.DataTypeLib.GameMenuState
Imports osAutoCast.DataTypeLib.TrayMenuState
Imports osToggle = System.Windows.Controls.Primitives.ToggleButton
Imports osVisibility = System.Windows.Visibility
Imports osKeyTime = System.Windows.Media.Animation.KeyTime
Imports osForms = System.Windows.Forms
Imports osCursor = System.Drawing.Point
Imports System.Windows.Interop
Imports osAutoCast.osVisualAdapter
Imports osAutoCast.osControls

#Disable Warning BC42353
#Disable Warning BC42024
#Disable Warning BC42104

Public Class osTrayMenu_GUI

    Public Async Function InitTrayMenuClose(isAsync As Boolean, Optional setTaskRun As Boolean = False) As Task
        Await SetCloseVisualData_WithTask(
            GetVisualKey(TrayMenu_Close),
                Sub()
                    objCloseMonitor.TrySetResult(True)
                End Sub,
                Async Function()
                    If setTaskRun Then
                        TrayMenuCloseComplete()
                    End If

                    Await osHandler_UI.TerminateTrayMenu()
                End Function)
    End Function

    Private Function EstablishVisConfig() As VisAdapterConfig
        Return VisAdapterConfig.EnableAll
    End Function

    Private Sub PrepTrayMenuDisplay()
        ShowGameMenuItem()

        With Me
            .Width = wTrayMenu
            .Height = hTrayMenu

            .Show()
            .Hide()
        End With
    End Sub

    Private Sub InitTrayMenuDisplay()
        Me.Activate()
    End Sub

    Public Async Function PrepTrayMenuInit() As Task
        Await InitializeVisAdapter(GetVisualKey(TrayMenu_Open), Me, EstablishVisConfig(),
                                   Sub() PrepTrayMenuDisplay(), Sub() InitTrayMenuDisplay(),
                                   Function() SetVisSideboard(GetVisualKey(GameMenu_Open, True)),
                                   True, TrayMenuContainer, TrayMenuContent, TrayGameMenuContainer)
    End Function

    Public Sub SetCloseMonitor(objAwaitClose As TaskCompletionSource(Of Boolean))
        objCloseMonitor = objAwaitClose
    End Sub

    Public Sub TrayMenuInit()
        CalcTrayPos()

        With Me
            PresentTrayMenu()

            .Left = .TrayMenuPos_X
            .Top = .TrayMenuPos_Y

            .Topmost = True
        End With

        allowTrayClose = True
    End Sub

End Class

Partial Public Class osTrayMenu_GUI
    Implements INotifyPropertyChanged

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

    Private idxTrayMenuVisuals As New Dictionary(Of TrayMenuState, String) From {
        {TrayMenuState.TrayMenu_Open, "TrayMenuVis_Display"},
        {TrayMenuState.TrayMenu_Close, "TrayMenuVis_Close"}
    }

    Private idxTrayGameMenuVisuals As New Dictionary(Of GameMenuState, String) From {
        {GameMenuState.GameMenu_Open, "TrayGameMenuVis_Expand"},
        {GameMenuState.GameMenu_Close, "TrayGameMenuVis_Collapse"}
    }

    Private isGameMenuStateResolved As Boolean = True

    Private objTask_Closing As TaskCompletionSource(Of Boolean) = Nothing
    Private objCloseMonitor As TaskCompletionSource(Of Boolean)

    Private hTrayMenu As Double = 220
    Private wTrayMenu As Double = 196

    Private objCurPos As osCursor

    Public allowTrayClose As Boolean

    Private SeperatorVisDuration As TimeSpan = TimeSpan.FromMilliseconds(750)

#Region "Properties"

    Private Property TrayMenuPos_X As Double
    Private Property TrayMenuPos_Y As Double

    Private _dispGameTrayMenuItem As GameMenuItem
    Public Property DisplayGameTrayMenuItem As GameMenuItem
        Get
            Return _dispGameTrayMenuItem
        End Get
        Set(value As GameMenuItem)
            If _dispGameTrayMenuItem <> value Then
                _dispGameTrayMenuItem = value
                OnDisplayGameTrayMenuChanged()
            End If
        End Set
    End Property

    Public ReadOnly Property TrayMenuContainer As osBorder
        Get
            Return Me.TrayMainContainer
        End Get
    End Property

    Public ReadOnly Property TrayMenuContent As Grid
        Get
            Return Me.TrayContentContainer
        End Get
    End Property

    Public ReadOnly Property TrayGameMenuContainer As Border
        Get
            Return Me.TrayMenuGamePanel
        End Get
    End Property

    Private _IsTrayMenuOpen As Boolean
    Public Property VerifyTrayMenuOpen As Boolean
        Get
            Return _IsTrayMenuOpen
        End Get
        Set(value As Boolean)
            If _IsTrayMenuOpen <> value Then
                _IsTrayMenuOpen = value
            End If
        End Set
    End Property

#End Region

#Region "Visual Data"

    Private Function SetVisDuration() As Duration
        Return New Duration(TimeSpan.FromMilliseconds(650))
    End Function

    Private Function GetVisualKey(objVisType As TrayMenuState) As String
        Return idxTrayMenuVisuals.First(
            Function(visKey)
                Return visKey.Key = objVisType
            End Function).Value
    End Function

    Private Function GetVisualKey(objVisType As GameMenuState, isGameMenu As Boolean) As String
        Return idxTrayGameMenuVisuals.First(
            Function(visKey)
                Return visKey.Key = objVisType
            End Function).Value
    End Function

#End Region

#Region "Tray Menu Helpers"

    Private Sub HoldTask() : End Sub

    Private Sub BufferTrayMenu()
        Me.Show()
        Me.Hide()
    End Sub

    Private Sub SetTrayMenuActive()
        Me.Activate()
        Me.Focus()
    End Sub

    Private Sub CalcTrayPos()
        objCurPos = osForms.Cursor.Position

        With objCurPos
            Me.TrayMenuPos_X = .X - wTrayMenu
            Me.TrayMenuPos_Y = .Y - hTrayMenu
        End With
    End Sub

#End Region

    Private Async Function RunTrayMenuCloseTask(objRunTask As Func(Of Task)) As Task
        objTask_Closing.ResetAndInitTask()

        Await TriggerTrayMenuDispose(True)

        Await objTask_Closing.Task
        Await Task.Delay(150)

        Await objRunTask()
    End Function

    Private Async Function RunTrayMenuCloseTask(objRunTask As Action) As Task
        objTask_Closing.ResetAndInitTask()

        Await TriggerTrayMenuDispose(True)

        Await objTask_Closing.Task
        Await Task.Delay(150)

        Await Task.Run(objRunTask)
    End Function

    Private Async Function TriggerTrayMenuDispose(Optional setTaskRun As Boolean = False) As Task
        PromptResponseState.PreventSecondaryClose()

        allowTrayClose = False
        Await InitTrayMenuClose(True, setTaskRun)
    End Function

    Private Async Function TriggerShowOpts() As Task
        osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
        Await osFuncLib_ShowOpts.ExecuteDispOpts()
    End Function

    Private Sub ExecTrayMenuCloseTrigger()
        With New Window With {
            .WindowStyle = WindowStyle.None,
            .Width = 0, .Height = 0,
            .ShowInTaskbar = False, .Topmost = True
        }

            .Show()
            .Activate()
            .Close()
        End With
    End Sub

    Private Function ValidateTrayMenuClose() As Boolean
        Return PromptResponseState.isPromptResponseOpen
    End Function

    Private Function DetermineToggleState(objSender As Object) As GameMenuState
        Dim objToggle = TryCast(objSender, osToggle)

        Return If(objToggle.IsChecked,
            GameMenu_Open, GameMenu_Close)
    End Function

    Private Sub ShowGameMenuItem()
        DisplayGameTrayMenuItem = If(CoreDataLib.IsGameRunning(),
            GameMenuItem.ShowClose, GameMenuItem.ShowStart)
    End Sub

    Private Sub TrayMenuCloseComplete()
        objTask_Closing.TrySetResult(True)
    End Sub

    Private Function IsTrayMenuOpen(objMenuState As TrayMenuState) As Boolean
        Return objMenuState = TrayMenu_Open
    End Function

    Private Function GetGameMenuState(chkMenuState As GameMenuState) As Boolean
        isGameMenuStateResolved = False
        Return chkMenuState = GameMenu_Open
    End Function

    Private Sub PresentTrayMenu()
        Me.Show()

        SetWindowPos(VisHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
    End Sub

#Region "Tray Menu Event Handlers"

    Public Sub New(Optional objLoadTask As TaskCompletionSource(Of Boolean) = Nothing)
        InitializeComponent()

        If objLoadTask IsNot Nothing Then
            objLoadTask.TrySetResult(True)
        End If
    End Sub

    Protected Overrides Async Sub OnDeactivated(e As EventArgs)
        MyBase.OnDeactivated(e)

        If Not isAppLoaded Then Exit Sub

        If allowTrayClose Then
            If ValidateTrayMenuClose() Then
                Return
            Else
                Await TriggerTrayMenuDispose()
            End If
        End If
    End Sub

    Private Sub TrayMenuClosed(sender As Object, e As EventArgs) Handles Me.Closed
        Try
            PromptResponseState.ExitPromptResponse()
        Catch ex As Exception : End Try
    End Sub

#End Region

#Region "Button Event Handlers"

    Private Sub TriggerStartGame()
        Dim procGameInfo = New ProcessStartInfo With {
            .FileName = dirMtgaExe, .WorkingDirectory = dirMtga,
            .WindowStyle = ProcessWindowStyle.Maximized
        }

        Process.Start(procGameInfo)
    End Sub

    Private Async Sub TrayMenuBtn_StartGame_Click(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_StartGame.Click
        '  PromptResponseState.EnterPromptResponse()
        Await RunTrayMenuCloseTask(AddressOf TriggerStartGame)
        'Await RunTrayMenuCloseTask(
        '    Sub()
        '        Process.Start(New ProcessStartInfo With {
        '                      .FileName = dirMtgaExe, .WorkingDirectory = dirMtga,
        '                      .WindowStyle = ProcessWindowStyle.Maximized
        '                  })
        '    End Sub)
    End Sub

    Private Async Sub TrayMenuBtn_CloseGame_Click(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_CloseGame.Click
        PromptResponseState.EnterPromptResponse()

        Dim chkCloseGameTrigger = GetResponse(PromptType.GameMenu_Leave, True)

        If chkCloseGameTrigger Then
            Await RunTrayMenuCloseTask(
                Sub()
                    With cmd_KillGame
                        osRunCmd.RunCmd(.First(), .Last())
                    End With
                End Sub)
        Else
            PromptResponseState.ExitPromptResponse()
            SetTrayMenuActive()
        End If
    End Sub

    Private Async Sub TrayMenuBtn_RestartGame_Click(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_RestartGame.Click
        PromptResponseState.EnterPromptResponse()

        Dim chkRestartGameTrigger = GetResponse(PromptType.GameMenu_Restart, True)

        If chkRestartGameTrigger Then
            Await RunTrayMenuCloseTask(
                Async Function()
                    With cmd_KillGame
                        osRunCmd.RunCmd(.First(), .Last())
                    End With

                    While IsGameRunning()
                        Await Task.Delay(500)
                        If Not IsGameRunning() Then Exit While
                    End While

                    Process.Start(New ProcessStartInfo With {
                              .FileName = dirMtgaExe, .WorkingDirectory = dirMtga,
                              .WindowStyle = ProcessWindowStyle.Maximized
                          })

                End Function)
        Else
            PromptResponseState.ExitPromptResponse()
            SetTrayMenuActive()
        End If
    End Sub

    Private Async Sub TrayMenuBtn_Exit_Click(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_Exit.Click
        PromptResponseState.EnterPromptResponse()

        Dim chkExitTrigger = GetResponse(PromptType.CloseApp, True)

        If chkExitTrigger Then
            Await RunTrayMenuCloseTask(
                Sub()
                    osTrayIcon.Visible = False
                    End
                End Sub)
        Else
            PromptResponseState.ExitPromptResponse()
            SetTrayMenuActive()
        End If
    End Sub

    Private Async Sub pmCmd_ShowOpts(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_ShowOptions.Click
        Await RunTrayMenuCloseTask(AddressOf TriggerShowOpts)
    End Sub

    Private Async Sub DetermineTrayGameMenu(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_ToggleGameMenu.Click
        Dim chkToggleState = DetermineToggleState(sender)

        If isGameMenuStateResolved Then
            Dim objGameMenuState = GetGameMenuState(chkToggleState)
            Dim visTask_Seperator = InitializeSeperatorVisuals(Not objGameMenuState)

            If objGameMenuState Then
                Await ValidateDispatch(
                    Function() TriggerVisuals_Sideboard(
                        GetVisualKey(GameMenu_Close, True),
                            Sub()
                                isGameMenuStateResolved = True
                            End Sub, True), True, Function() Task.WhenAll(visTask_Seperator))
            Else
                Await ValidateDispatch(
                    Function() TriggerVisuals_Sideboard(
                        GetVisualKey(GameMenu_Open, True),
                            Sub()
                                isGameMenuStateResolved = True
                            End Sub, True), True, Function() Task.WhenAll(visTask_Seperator))
            End If
        End If
    End Sub

    Private Function InitializeSeperatorVisuals(Optional isCollapse As Boolean = False) As List(Of Task)
        Dim objSepColorData = PrepSeperatorColorData()
        Dim objSeperatorColor = GenerateSeperatorColor(isCollapse)

        Return SeperatorArray().Select(
            Function(objSeperator)
                Dim objSepColorVisual = ComposeSeperatorVisual(objSeperatorColor)
                Dim objSepColor = objSepColorData(objSeperator)

                Dim evtSepVisComplete As EventHandler =
                   Sub()
                       RemoveHandler objSepColorVisual.Completed, evtSepVisComplete

                       objSepColor.Color = GenerateSeperatorColor(isCollapse)
                       objSepColor.BeginAnimation(GradientStop.ColorProperty, Nothing)
                   End Sub

                AddHandler objSepColorVisual.Completed, evtSepVisComplete

                Dim visSeperatorTask =
                   Async Function() As Task
                       Await PrepDispatcher().InvokeAsync(
                           Sub()
                               objSepColor.BeginAnimation(
                                   GradientStop.ColorProperty, objSepColorVisual)
                           End Sub, DispatcherPriority.Render)
                   End Function

                Return Task.Run(visSeperatorTask)
            End Function).ToList()
    End Function

    Private Function SeperatorArray() As IEnumerable(Of Integer)
        Return Enumerable.Range(1, 2)
    End Function

    Private Function GenerateSeperatorColor(Optional isCollapse As Boolean = False) As Color
        Return CType(ColorConverter.ConvertFromString(
            If(isCollapse, "#A1060606", "#F1AAAAAA")), Color)
    End Function

    Private Function FetchContainerSeperator() As Rectangle
        Return TrayMenuContent.Children.OfType(Of Rectangle)().
            Where(Function(objSep) Equals(objSep.Tag, "Content_Separator")).
            OrderBy(Function(objSep) Grid.GetRow(objSep))(1)
    End Function

    Private Function PrepSeperatorColorData() As GradientStopCollection
        Dim objSeperator = FetchContainerSeperator()

        Dim objSeperatorBrush = TryCast(
            objSeperator.Fill, LinearGradientBrush)

        objSeperatorBrush = objSeperatorBrush.CloneCurrentValue()
        objSeperator.Fill = objSeperatorBrush

        Return objSeperatorBrush.GradientStops
    End Function

    Private Function ComposeSeperatorVisual(visSepColor As Color) As ColorAnimation
        Return New ColorAnimation(visSepColor, SeperatorVisDuration) With {
                .FillBehavior = FillBehavior.HoldEnd,
                .EasingFunction = New ExponentialEase With {
                    .EasingMode = EasingMode.EaseIn,
                    .Exponent = 1.25
                }
            }
    End Function

#End Region

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer,
                                        Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

    Public Event DisplayGameTrayMenuChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub OnDisplayGameTrayMenuChanged(<CallerMemberName> Optional name As String = Nothing)
        RaiseEvent DisplayGameTrayMenuChanged(Me, New PropertyChangedEventArgs(name))
    End Sub

End Class
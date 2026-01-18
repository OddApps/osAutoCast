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
Imports osAutoCast.DataTypeLib.GameMenuVisuals
Imports osAutoCast.DataTypeLib.TrayMenuState
Imports osToggle = System.Windows.Controls.Primitives.ToggleButton
Imports osVisibility = System.Windows.Visibility
Imports osKeyTime = System.Windows.Media.Animation.KeyTime
Imports osForms = System.Windows.Forms
Imports osCursor = System.Drawing.Point
Imports System.Windows.Interop

#Disable Warning BC42353
#Disable Warning BC42024
#Disable Warning BC42104

Public Class osTrayMenu_GUI

    Private Async Function TriggerShowOpts() As Task
        osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
        Await osFuncLib_ShowOpts.ExecuteDispOpts()
    End Function

    Private Sub SetTrayMenuEvent(objMenuState As TrayMenuState, Optional setTaskRun As Boolean = False)
        If IsTrayMenuOpen(objMenuState) Then
            RemoveHandler visTrayMenu_Open.Completed,
                                                evtDisplayTrayMenu
            evtDisplayTrayMenu =
                Sub()
                    RemoveHandler visTrayMenu_Open.Completed,
                                                evtDisplayTrayMenu

                    visTrayMenu_Close = EstablishVisual(TrayMenu_Close)
                    '   SetVisualMode(TrayMenu_Open)
                End Sub

            AddHandler visTrayMenu_Open.Completed,
                                    evtDisplayTrayMenu
        Else
            AddHandler visTrayMenu_Close.Completed,
                 Sub()
                     If setTaskRun Then
                         TrayMenuCloseComplete()
                     End If

                     osHandler_UI.TerminateTrayMenu()
                 End Sub
        End If
    End Sub

    Public Sub InitTrayMenuClose(Optional setTaskRun As Boolean = False)
        SetTrayMenuEvent(TrayMenu_Close, setTaskRun)
        '    SetVisualMode(VisRenderMode.VisMode_LowQuality)
        Dim aa = osVisQualityAdapter.EstablishVisDataSettings(VisTypeAdapter.VisAdapter_TrayMenu, VisRenderMode.VisMode_LowQuality, True)
        visTrayMenu_Close.Begin(TrayMenuOutline, True)
        'PrepDispatcher().Invoke(
        '    Sub()
        '        visTrayMenu_Close.Begin()
        '    End Sub, DispatcherPriority.Render)
    End Sub

    Public Sub DisplayTrayMenu()
        'If PrepDispatcher().CheckAccess() Then
        '    ShowTrayMenuCore()
        ''Else
        'PrepDispatcher().Invoke(
        '        AddressOf TriggerVisuals,
        '        DispatcherPriority.Render)
        '   End If
        'EstablishVisual(TrayMenu_Open, visTrayMenu_Open)
        'InitTrayMenuVis()

        'With Me
        '    .Width = wTrayMenu
        '    .Height = hTrayMenu

        '    BufferTrayMenu()
        'End With

        'Dim a = osVisQualityAdapter.InitAdapter(VisTypeAdapter.VisAdapter_TrayMenu, visTrayMenu_Open,
        '                                         True, True, TrayMenuOutline, TrayMainContainer)
        TriggerVisuals()
    End Sub

    Public Sub InitTrayMenuVis()
        RemoveHandler visTrayMenu_Open.Completed,
                                                evtDisplayTrayMenu
        evtDisplayTrayMenu =
                Sub()
                    RemoveHandler visTrayMenu_Open.Completed,
                                                evtDisplayTrayMenu

                    visTrayMenu_Close = EstablishVisual(TrayMenu_Close)
                    '  SetVisualMode(VisRenderMode.VisMode_HighQuality)
                End Sub

        AddHandler visTrayMenu_Open.Completed,
                                    evtDisplayTrayMenu
    End Sub

    Public Sub PrepTrayMenuInit()
        Dispatcher.BeginInvoke(
    Sub()
        visTrayMenu_Open = EstablishVisual(TrayMenu_Open)
        InitTrayMenuVis()

        Dim aa = osVisQualityAdapter.InitAdapter(VisTypeAdapter.VisAdapter_TrayMenu, visTrayMenu_Open,
                                                  True, True, TrayMenuOutline, TrayMainContainer)
        With Me
            .Width = wTrayMenu
            .Height = hTrayMenu

            BufferTrayMenu()
        End With
    End Sub,
    DispatcherPriority.Background)

    End Sub

    Public Async Function PrepTrayMenuInit(isN As Boolean) As Task
        Await PrepDispatcher().InvokeAsync(
                    Sub()
                        visTrayMenu_Open = EstablishVisual(TrayMenu_Open)
                        InitTrayMenuVis()

                        With Me
                            .Width = wTrayMenu
                            .Height = hTrayMenu

                            '  BufferTrayMenu()
                        End With

                        Dim a = osVisQualityAdapter.InitAdapter(VisTypeAdapter.VisAdapter_TrayMenu, visTrayMenu_Open,
                                                 True, True, TrayMenuOutline, TrayMainContainer)
                    End Sub, DispatcherPriority.Background)

    End Function

    Private Sub TriggerVisuals()
        'Await Dispatcher.CurrentDispatcher.BeginInvoke(
        '    Sub()
        Try
            CalcTrayPos()

            With Me
                PresentTrayMenu()

                .Left = .TrayMenuPos_X
                .Top = .TrayMenuPos_Y

                .Topmost = True
            End With

            visTrayMenu_Open.Begin(TrayMenuOutline, True)

        Catch ex As Exception : End Try
        'End Sub, DispatcherPriority.Render)
    End Sub

    Private Sub ShowTrayMenuCore()
        CalcTrayPos()

        With Me
            PresentTrayMenu()

            .Left = .TrayMenuPos_X
            .Top = .TrayMenuPos_Y

            .Topmost = True
        End With


        visTrayMenu_Open.Begin(TrayMenuOutline)
    End Sub

    Private Sub osTrayMenu_GUI_ContentRendered(sender As Object, e As EventArgs) Handles Me.Loaded
        'With Me
        '    .Width = wTrayMenu
        '    .Height = hTrayMenu

        '    BufferTrayMenu()
        'End With
        'visTrayMenu_Open = EstablishVisual(TrayMenu_Open)
        'InitTrayMenuVis()


        'Dim a = osVisQualityAdapter.InitAdapter(VisTypeAdapter.VisAdapter_TrayMenu, visTrayMenu_Open,
        '                                         True, True, TrayMenuOutline, TrayMainContainer)
    End Sub
End Class

Partial Public Class osTrayMenu_GUI
    Implements INotifyPropertyChanged

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

    Private idxTrayMenuVis As New List(Of GameMenuVisuals) From {
        {GameMenuVis_Height}, {GameMenuVis_Opacity},
        {GameMenuVis_Visible}, {GameMenuVis_Position}
    }

    Private idxTrayMenuVisuals As New Dictionary(Of TrayMenuState, String) From {
        {TrayMenuState.TrayMenu_Open, "TrayMenuVis_Display"},
        {TrayMenuState.TrayMenu_Close, "TrayMenuVis_Close"}
    }

    Private visGameMenu_Open As Storyboard = Nothing
    Private visGameMenu_Close As Storyboard = Nothing

    Private visTrayMenu_Open As Storyboard = Nothing
    Private visTrayMenu_Close As Storyboard = Nothing

    Private evtDispTrayMenuTask As Action = AddressOf ShowTrayMenuCore

    Private evtCloseTrayGameMenu As EventHandler
    Private evtDisplayTrayMenu As EventHandler

    Private objTask_Closing As TaskCompletionSource(Of Boolean) = Nothing

    Private hTrayMenu As Double = 152
    Private hTrayMenu_GameMenu As Double = 184

    Private wTrayMenu As Double = 196

    Private objCurPos As osCursor

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
                OnPropertyChanged()
            End If
        End Set
    End Property

    Private ReadOnly Property TrayMenuRes As Style
        Get
            Return Me.Style
        End Get
    End Property

#End Region

#Region "Visual Data"

    Private Sub SetVisualMode(objVMode As VisRenderMode)
        With New osVisRenderMode(objVMode)
            TrayMenuOutline.CacheMode = .visCache
            TrayMainContainer.CacheMode = .visCache

            RenderOptions.SetBitmapScalingMode(TrayMenuOutline, .visBitMap)
            RenderOptions.SetBitmapScalingMode(TrayMainContainer, .visBitMap)
            RenderOptions.SetEdgeMode(TrayMenuOutline, .visEdges)
            RenderOptions.SetEdgeMode(TrayMainContainer, .visEdges)
        End With
    End Sub

    Private Sub SetVisualMode(objMenuState As TrayMenuState)
        Dim setBitMapMode As BitmapScalingMode
        Dim setCacheMode As CacheMode

        RenderOptions.ProcessRenderMode = Interop.RenderMode.Default

        Select Case objMenuState
            Case TrayMenu_Open
                setBitMapMode = BitmapScalingMode.HighQuality
                setCacheMode = Nothing
            Case TrayMenu_Close
                setBitMapMode = BitmapScalingMode.LowQuality
                setCacheMode = New BitmapCache()
        End Select

        TrayMenuOutline.CacheMode = setCacheMode
        RenderOptions.SetBitmapScalingMode(TrayMenuOutline, setBitMapMode)
    End Sub

    Private Sub ResetVisuals(chkMenuState As GameMenuState)
        If GetGameMenuState(chkMenuState) Then
            If visGameMenu_Open IsNot Nothing Then
                visGameMenu_Open.Children.Clear()
                visGameMenu_Open = Nothing
            End If

            visGameMenu_Open = New Storyboard()
        Else
            If visGameMenu_Close IsNot Nothing Then
                visGameMenu_Close.Children.Clear()
                visGameMenu_Close = Nothing
            End If

            visGameMenu_Close = New Storyboard()
        End If
    End Sub

    Private Function SetVisDuration() As Duration
        Return New Duration(TimeSpan.FromMilliseconds(650))
    End Function

    Private Function SetVisDuration(chkMenuState As GameMenuState, isVis As Boolean) As osKeyTime
        Dim valDur = If(GetGameMenuState(chkMenuState), 0, 649)
        Return osKeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(valDur))
    End Function

    Private Function GetVisValues(visMenuType As GameMenuVisuals, chkMenuState As GameMenuState) As GameMenuVisData
        Dim visValStart As Double = Nothing
        Dim visValEnd As Double = Nothing
        Dim visValVisibility As osVisibility = Nothing

        Dim valState = GetGameMenuState(chkMenuState)

        Select Case visMenuType
            Case GameMenuVis_Height
                visValStart = If(valState, 0, 64)
                visValEnd = If(valState, 64, 0)
            Case GameMenuVis_Opacity
                visValStart = If(valState, 0, 1)
                visValEnd = If(valState, 1, 0)
            Case GameMenuVis_Visible
                visValVisibility = If(valState,
                    osVisibility.Visible, osVisibility.Collapsed)
            Case GameMenuVis_Position
                visValStart = Me.Top
                visValEnd = If(valState,
                    Me.Top - 64, Me.Top + 64)
        End Select

        Return New GameMenuVisData(visValStart, visValEnd, visValVisibility)
    End Function

    Private Sub EstablishGameMenuVisual(visMenuType As GameMenuVisuals, chkMenuState As GameMenuState)
        Dim objVisualData = GetVisValues(visMenuType, chkMenuState)
        Dim objMenuVis As Object

        Dim _objVis = If(chkMenuState = GameMenu_Open,
            visGameMenu_Open, visGameMenu_Close)

        If visMenuType <> GameMenuVis_Visible Then
            objMenuVis = TryCast(objMenuVis, DoubleAnimation)

            objMenuVis = New DoubleAnimation() With {
                    .From = objVisualData.visStart, .To = objVisualData.visEnd,
                    .Duration = SetVisDuration(),
                    .EasingFunction = New QuadraticEase With {
                        .EasingMode = EasingMode.EaseInOut
                    }
                }
        Else
            objMenuVis = TryCast(objMenuVis, ObjectAnimationUsingKeyFrames)
            objMenuVis = New ObjectAnimationUsingKeyFrames()

            objMenuVis.KeyFrames.Add(
                New DiscreteObjectKeyFrame() With {
                    .KeyTime = SetVisDuration(chkMenuState, True),
                    .Value = objVisualData.visVisibility
                })
        End If

        Select Case visMenuType
            Case GameMenuVis_Height
                Storyboard.SetTarget(objMenuVis, TrayMenuGamePanel)
                Storyboard.SetTargetProperty(objMenuVis, SetVisAttr(visMenuType))
            Case GameMenuVis_Opacity
                Storyboard.SetTarget(objMenuVis, TrayMenuGamePanel)
                Storyboard.SetTargetProperty(objMenuVis, SetVisAttr(visMenuType))
            Case GameMenuVis_Position
                Storyboard.SetTarget(objMenuVis, Me)
                Storyboard.SetTargetProperty(objMenuVis, SetVisAttr(visMenuType))
            Case GameMenuVis_Visible
                Storyboard.SetTarget(objMenuVis, TrayMenuGamePanel)
                Storyboard.SetTargetProperty(objMenuVis, SetVisAttr(visMenuType))
        End Select

        _objVis.Children.Add(objMenuVis)
    End Sub

    Private Function SetVisAttr(visMenuType As GameMenuVisuals) As PropertyPath
        Select Case visMenuType
            Case GameMenuVis_Height
                Return New PropertyPath("Height")
            Case GameMenuVis_Opacity
                Return New PropertyPath("Opacity")
            Case GameMenuVis_Visible
                Return New PropertyPath("Visibility")
            Case GameMenuVis_Position
                Return New PropertyPath(Window.TopProperty)
        End Select
    End Function

    Private Sub SetGameMenuVisuals(setMenuState As GameMenuState)
        ResetVisuals(setMenuState)

        For Each visType In idxTrayMenuVis
            EstablishGameMenuVisual(visType, setMenuState)
        Next
    End Sub

    Private Function LoadVis_Select() As Style
        Return TrayMenuRes
    End Function

    Private Function LoadVis_Set(objVisType As TrayMenuState) As Storyboard
        Return TryCast(Me.Resources(GetVisualKey(objVisType)), Storyboard)
    End Function

    Private Function LoadVis_Set(objVisResource As Style, objVisType As TrayMenuState) As Storyboard
        Return TryCast(Me.Resources(GetVisualKey(objVisType)), Storyboard)
    End Function

    Private Function EstablishVisual(objVisType As TrayMenuState) As Storyboard
        Return LoadVis_Set(TrayMenuRes, objVisType)
    End Function

    Private Sub EstablishVisual(objVisType As TrayMenuState, ByRef objVisData As Storyboard)
        Dim objTrayMenuVis = LoadVis_Set(objVisType).Clone()


    End Sub

    Private Function GetVisualKey(objVisType As TrayMenuState) As String
        Return idxTrayMenuVisuals.First(
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

        TriggerTrayMenuDispose(True)

        Await objTask_Closing.Task
        Await Task.Delay(150)

        Await objRunTask()
    End Function

    Private Async Function RunTrayMenuCloseTask(objRunTask As Action) As Task
        objTask_Closing.ResetAndInitTask()

        TriggerTrayMenuDispose(True)

        Await objTask_Closing.Task
        Await Task.Delay(150)

        Await Task.Run(objRunTask)
    End Function

    Private Sub TriggerTrayMenuDispose(Optional setTaskRun As Boolean = False)
        PromptResponseState.PreventSecondaryClose()

        InitTrayMenuClose(setTaskRun)
    End Sub

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

    Private Sub ApplyTrayMenuSize(Optional isTrayOpen As Boolean = True)
        With Me
            .Width = wTrayMenu : .Height = If(isTrayOpen,
                hTrayMenu_GameMenu, hTrayMenu)
        End With
    End Sub

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
        Return chkMenuState = GameMenu_Open
    End Function

    Private Sub PresentTrayMenu()
        Me.Show()

        Dim objHwnd = New WindowInteropHelper(Me).Handle

        SetWindowPos(objHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
    End Sub

#Region "Tray Menu Event Handlers"

    Public Sub New(Optional objLoadTask As TaskCompletionSource(Of Boolean) = Nothing)
        InitializeComponent()
        ShowGameMenuItem()

        If objLoadTask IsNot Nothing Then
            objLoadTask.TrySetResult(True)
        End If
    End Sub

    Private Sub ComposeOutline(sender As Object, e As RoutedEventArgs) Handles TrayMenuOutline.Loaded
        EstablishOutline(TrayMenuOutline, 6)
    End Sub

    Protected Overrides Sub OnDeactivated(e As EventArgs)
        MyBase.OnDeactivated(e)

        If Not isAppLoaded Then Exit Sub

        If visTrayMenu_Close Is Nothing Then
            visTrayMenu_Close = EstablishVisual(TrayMenu_Close)
            osVisQualityAdapter.UpdateVisData(VisTypeAdapter.VisAdapter_TrayMenu, visTrayMenu_Close)

        End If

        If ValidateTrayMenuClose() Then
            Return
        Else
            TriggerTrayMenuDispose()
        End If
    End Sub

    Private Sub TrayMenuClosed(sender As Object, e As EventArgs) Handles Me.Closed
        Try
            PromptResponseState.ExitPromptResponse()
        Catch ex As Exception : End Try
    End Sub

#End Region

#Region "Button Event Handlers"

    Private Async Sub TrayMenuBtn_StartGame_Click(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_StartGame.Click
        PromptResponseState.EnterPromptResponse()

        Await RunTrayMenuCloseTask(
            Sub()
                Process.Start(New ProcessStartInfo With {
                              .FileName = dirMtgaExe, .WorkingDirectory = dirMtga,
                              .WindowStyle = ProcessWindowStyle.Maximized
                          })
            End Sub)
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

    Private Async Sub PrepTrayGameMenu(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_ToggleGameMenu.Checked
        Await Task.Run(
            Sub()
                ShowGameMenuItem()
            End Sub)
    End Sub

    Private Async Sub pmCmd_ShowOpts(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_ShowOptions.Click
        Await RunTrayMenuCloseTask(AddressOf TriggerShowOpts)
    End Sub

    Private Async Sub DetermineTrayGameMenu(sender As Object, e As RoutedEventArgs) Handles TrayMenuBtn_ToggleGameMenu.Click
        Dim chkToggleState = DetermineToggleState(sender)
        SetGameMenuVisuals(chkToggleState)

        If GetGameMenuState(chkToggleState) Then
            Storyboard.SetTarget(visGameMenu_Open,
                                 TrayMenuGamePanel)
            ApplyTrayMenuSize(True)

            TrayMenuGamePanel.UpdateLayout()
            Await Dispatcher.InvokeAsync(
                    Sub() HoldTask(),
                    DispatcherPriority.Render)

            visGameMenu_Open.Begin()
        Else
            Storyboard.SetTarget(visGameMenu_Close,
                                 TrayMenuGamePanel)

            evtCloseTrayGameMenu =
                Sub()
                    RemoveHandler visGameMenu_Close.Completed,
                                                evtCloseTrayGameMenu
                    ApplyTrayMenuSize()
                End Sub

            AddHandler visGameMenu_Close.Completed,
                                    evtCloseTrayGameMenu

            Await Dispatcher.InvokeAsync(Sub() HoldTask(),
                                         DispatcherPriority.Render)

            TrayMenuGamePanel.UpdateLayout()
            visGameMenu_Close.Begin()
        End If
    End Sub

#End Region

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer,
                                        Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub OnPropertyChanged(<CallerMemberName> Optional name As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
    End Sub

End Class
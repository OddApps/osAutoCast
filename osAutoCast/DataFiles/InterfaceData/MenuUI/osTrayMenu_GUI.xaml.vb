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

#Disable Warning BC42353
#Disable Warning BC42024
#Disable Warning BC42104

Public Class osTrayMenu_GUI

    Private Sub ResetVisuals(chkMenuState As GameMenuState)
        If GetMenuState(chkMenuState) Then
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
        Dim valDur = If(GetMenuState(chkMenuState), 0, 649)
        Return osKeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(valDur))
    End Function

    Private Function GetMenuState(chkMenuState As GameMenuState) As Boolean
        Return chkMenuState = GameMenu_Open
    End Function

    Private Function GetVisValues(visMenuType As GameMenuVisuals, chkMenuState As GameMenuState) As GameMenuVisData
        Dim visValStart As Double = Nothing
        Dim visValEnd As Double = Nothing
        Dim visValVisibility As osVisibility = Nothing

        Dim valState = GetMenuState(chkMenuState)

        Select Case visMenuType
            Case GameMenuVis_Height
                visValStart = If(valState, 0, 30)
                visValEnd = If(valState, 30, 0)
            Case GameMenuVis_Opacity
                visValStart = If(valState, 0, 1)
                visValEnd = If(valState, 1, 0)
            Case GameMenuVis_Visible
                visValVisibility = If(valState,
                    osVisibility.Visible, osVisibility.Collapsed)
            Case GameMenuVis_Position
                visValStart = Me.Top
                visValEnd = If(valState,
                    Me.Top - 30, Me.Top + 30)
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

    Private Async Function TriggerTrayMenuClose() As Task
        Await Task.Run(
            Async Function()
                Await PrepDispatcher().InvokeAsync(
                    Function()
                        With New Window With {
                            .WindowStyle = WindowStyle.None,
                            .Width = 0, .Height = 0,
                            .ShowInTaskbar = False,
                            .Topmost = True
                        }
                            .Show()
                            .Activate()
                            .Close()
                        End With
                    End Function, DispatcherPriority.Normal)
            End Function)
    End Function

    Private Sub SetGameMenuVisuals(setMenuState As GameMenuState)
        ResetVisuals(setMenuState)

        For Each visType In idxTrayMenuVis
            EstablishGameMenuVisual(visType, setMenuState)
        Next
    End Sub

    Private Async Sub pmCmd_ShowOpts(sender As Object, e As RoutedEventArgs) Handles pmBtn_ShowOptions.Click
        Dim objTask_TriggerClose = TriggerTrayMenuClose()

        Dim objTask_ShowPrefs = PrepDispatcher().InvokeAsync(
            Async Function()
                osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
                Await osFuncLib_ShowOpts.ExecuteDispOpts()
            End Function)

        Await objTask_ShowPrefs.Task.Unwrap()
    End Sub

    Private Async Sub DetermineTrayGameMenu(sender As Object, e As RoutedEventArgs) Handles btnShowGameTrayMenu.Click
        Await Task.Run(
            Async Function()
                Await PrepDispatcher().InvokeAsync(
                    Function()
                        Dim chkToggleState = DetermineToggleState(sender)
                        SetGameMenuVisuals(chkToggleState)

                        If GetMenuState(chkToggleState) Then
                            Storyboard.SetTarget(visGameMenu_Open, TrayMenuGamePanel)

                            With Me
                                .Width = wTrayMenu
                                .Height = hTrayMenu_GameMenu
                            End With

                            visGameMenu_Open.Begin()
                        Else
                            evtCloseTrayGameMenu = Nothing
                            evtCloseTrayGameMenu =
                                Sub()
                                    RemoveHandler visGameMenu_Close.Completed,
                                            evtCloseTrayGameMenu

                                    Me.Width = wTrayMenu
                                    Me.Height = hTrayMenu
                                End Sub

                            AddHandler visGameMenu_Close.Completed,
                                evtCloseTrayGameMenu

                            Storyboard.SetTarget(visGameMenu_Close, TrayMenuGamePanel)
                            visGameMenu_Close.Begin()
                        End If
                    End Function, DispatcherPriority.Normal)
            End Function)
    End Sub

    Private Sub SetTrayMenuEvent(objMenuState As TrayMenuState,
                                 Optional ByRef objEvtTask As TaskCompletionSource(Of Boolean) = Nothing)
        Select Case objMenuState
            Case TrayMenu_Open
                evtDisplayTrayMenu = Nothing
                evtDisplayTrayMenu =
                    Sub()
                        RemoveHandler visTrayMenu_Open.Completed, evtDisplayTrayMenu
                        TrayMenuOutline.CacheMode = Nothing
                    End Sub

                AddHandler visTrayMenu_Open.Completed, evtDisplayTrayMenu
            Case TrayMenu_Close
                AddHandler visTrayMenu_Close.Completed,
                    Sub()
                        CoreDataLib.TerminateTrayMenu()
                    End Sub
        End Select
    End Sub

    Public Async Function InitTrayMenuClose() As Task
        Await PrepDispatcher().InvokeAsync(
            Function()
                visTrayMenu_Close = EstablishVisual(TrayMenu_Close)
                SetTrayMenuEvent(TrayMenu_Close, objTask_Closing)

                TrayMenuOutline.CacheMode = New BitmapCache()

                visTrayMenu_Close.Begin(Me)
            End Function)
    End Function

    Public Async Function DisplayTrayMenu() As Task
        If PrepDispatcher().CheckAccess() Then
            ShowTrayMenuCore()
        Else
            Await PrepDispatcher().InvokeAsync(
                evtDispTrayMenuTask, DispatcherPriority.Render)
        End If
    End Function

    Private Sub ShowTrayMenuCore()
        CalcTrayPos()

        With Me
            .Topmost = True

            .Left = .TrayMenuPos_X : .Top = .TrayMenuPos_Y
            .Width = wTrayMenu : .Height = hTrayMenu

            .Show()
        End With

        visTrayMenu_Open = EstablishVisual(TrayMenu_Open)
        SetTrayMenuEvent(TrayMenu_Open)

        visTrayMenu_Open.Begin(Me)
    End Sub

End Class

Partial Public Class osTrayMenu_GUI
    Implements INotifyPropertyChanged

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

    Private objTask_Open As TaskCompletionSource(Of Boolean)
    Private objTask_Closing As TaskCompletionSource(Of Boolean)

    Private evtDispTrayMenuTask As Action = AddressOf ShowTrayMenuCore

    Private evtCloseTrayGameMenu As EventHandler

    Private evtDisplayTrayMenu As EventHandler
    Private evtCloseTrayMenu As EventHandler

    Private hTrayMenu As Double = 141
    Private hTrayMenu_GameMenu As Double = 171

    Private wTrayMenu As Double = 196

    Private objCurPos As osCursor

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

    Public Sub New()
        InitializeComponent()
        ShowGameMenuItem()
    End Sub

    Private Sub CalcTrayPos()
        objCurPos = osForms.Cursor.Position

        With objCurPos
            Me.TrayMenuPos_X = .X - wTrayMenu
            Me.TrayMenuPos_Y = .Y - hTrayMenu
        End With
    End Sub

    Private Function LoadVis_Select() As Style
        Return TrayMenuRes
    End Function

    Private Function LoadVis_Set(objVisResource As Style, objVisType As TrayMenuState) As Storyboard
        Return TryCast(objVisResource.
            Resources(GetVisualKey(objVisType)), Storyboard)
    End Function

    Private Function EstablishVisual(objVisType As TrayMenuState) As Storyboard
        Dim objLoadVis = LoadVis_Set(TrayMenuRes, objVisType)
        Return objLoadVis.Clone()
    End Function

    Private Function GetVisualKey(objVisType As TrayMenuState) As String
        Return idxTrayMenuVisuals.First(
            Function(visKey)
                Return visKey.Key = objVisType
            End Function).Value
    End Function

    Protected Overrides Async Sub OnDeactivated(e As EventArgs)
        MyBase.OnDeactivated(e)

        If ValidateTrayMenuClose() Then
            Return : End If

        Await Task.Run(
            Async Function()
                Await Task.Delay(150)
                Await InitTrayMenuClose()
            End Function)
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

    Private Async Sub PrepTrayGameMenu(sender As Object, e As RoutedEventArgs) Handles btnShowGameTrayMenu.Checked
        Await Task.Run(
            Sub()
                ShowGameMenuItem()
            End Sub)
    End Sub

    Private Sub ComposeOutline(sender As Object, e As RoutedEventArgs) Handles TrayMenuOutline.Loaded
        EstablishOutline(TrayMenuOutline, 6)
    End Sub

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub OnPropertyChanged(<CallerMemberName> Optional name As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
    End Sub

End Class


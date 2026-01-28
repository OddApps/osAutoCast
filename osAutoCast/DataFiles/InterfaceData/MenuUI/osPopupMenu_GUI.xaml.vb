Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.CoreDataLib
Imports osAutoCast.DataTypeLib.AnimationType
Imports osAutoCast.DataTypeLib.PopupVisualType
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.DataTypeLib.VisualAction
Imports osAutoCast.DataTypeLib.PopupCloseAction
Imports osAutoCast.DataTypeLib.VisTypeAdapter
Imports osAutoCast.GameMenuOpts
Imports osAutoCast.osStyle
Imports osAutoCast.osControls
Imports osAutoCast.osVisualAdapter

#Disable Warning BC42353
#Disable Warning BC42104

Public Class osPopupMenu_GUI

    Private _hasAnimated As Boolean = False

    Public Event EvCloseByClick(sender As Object, e As EventArgs)

    Public objTask_Closing As TaskCompletionSource(Of Boolean)
    Private objTask_Open As TaskCompletionSource(Of Boolean)

    Public objAnimation_Open As New Storyboard
    Private objAnimation_Close As New Storyboard

    Private idxPopupVisuals As New Dictionary(Of PopupVisualType, String) From {
        {PopupVisual_Open, "PopupVisual_Open"},
        {PopupVisual_Close, "PopupVisual_Close"},
        {PopupVisual_CloseByBtn, "PopupVisual_CloseByBtn"},
        {PopupVisual_CloseByCmd, "PopupVisual_CloseByCmd"}
    }

    Private OpenCompleteEvent As EventHandler = AddressOf PopupOpenComplete
    Private CloseCompleteEvent As EventHandler = AddressOf PopupCloseComplete

    Private Property isVisualConfigured As Boolean

    Public ReadOnly Property objContainer As Border
        Get
            Return Me.popupMainContainer
        End Get
    End Property

    Private Sub ResetVisualConfig()
        isVisualConfigured = False
    End Sub

    Private Sub BeginOpenTask(ByRef objOpenResult As TaskCompletionSource(Of Boolean))
        objOpenResult.ResetAndInitTask()
    End Sub

    Public Sub InitPopupClose(objVisType As PopupVisualType)
        DetectCloseMethod(objVisType)
        BeginClosingTask(objTask_Closing)

        EstablishVisual(objVisType)
        ProcessVisualEvents(aniClose, VisualStarted)
    End Sub

    Public Async Function InterceptPopupMenuClose() As Task
        Await objTask_Closing.Task
    End Function

    'Public Overrides Async Function TriggerVisuals_Close() As Task
    '    Await MyBase.TriggerVisuals_Close()
    '    Await objTask_Closing.Task
    'End Function

    Private Function GetVisualKey(objVisType As PopupVisualType) As String
        Return idxPopupVisuals.
            First(Function(visKey)
                      Return visKey.Key = objVisType
                  End Function).Value
    End Function

    Private Sub ProcessVisualEvents(objAniType As AnimationType, objVisAction As VisualAction)
        Select Case objAniType
            Case aniOpen
                Select Case objVisAction
                    Case VisualStarted
                        AddHandler VisDataObject.Completed,
                            OpenCompleteEvent
                    Case VisualComplete
                        RemoveHandler VisDataObject.Completed, OpenCompleteEvent
                End Select
            Case aniClose
                Select Case objVisAction
                    Case VisualStarted
                        AddHandler VisDataObject.Completed,
                            CloseCompleteEvent
                    Case VisualComplete
                        RemoveHandler VisDataObject.Completed,
                            CloseCompleteEvent
                End Select
        End Select
    End Sub

    Private Function ConvVisual(objVis As Object) As Storyboard
        Return TryCast(objVis, Storyboard)
    End Function

    Private Function ConvVisualData(objVisData As Object) As Style
        Return CType(objVisData, Style)
    End Function

    Private Function GetVisualData() As ResourceDictionary
        With objContainer.Style
            Return .Resources.MergedDictionaries.First()
        End With
    End Function

    Private Function SelectVisual() As Style
        Return ConvVisualData(GetVisualData()("PopupVisuals"))
    End Function

    Private Async Sub TriggerVisuals(objVisualData As Storyboard)
        Await objContainer.Dispatcher.BeginInvoke(
            Sub()
                Try

                    objVisualData.Begin(objContainer)
                Catch ex As Exception : End Try
            End Sub, DispatcherPriority.Render)
    End Sub

    Private Async Sub TriggerVisuals(objVisualData As Storyboard, isNew As Boolean)
        Await objContainer.Dispatcher.BeginInvoke(
            Sub()
                Try : objVisualData.Begin(objContainer,
                                          isControllable:=True)
                Catch ex As Exception : End Try
            End Sub, DispatcherPriority.Render)
    End Sub

    Private _popupWarmupDone As Boolean = False

    Public Function WarmupPopupMenu() As Task
        Return Task.Run(Sub()
                            PrepDispatcher().Invoke(
                                Sub()
                                    ConfigureVisual()
                                End Sub)
                        End Sub)
    End Function

    Private Function GetVisual(objVisType As PopupVisualType, isNew As Boolean) As Storyboard
        Return ConvVisual(Me.Resources(GetVisualKey(objVisType)))
    End Function

    Private Function GetVisual(objVisType As PopupVisualType) As Storyboard
        With ConvVisual(SelectVisual().
                Resources(GetVisualKey(objVisType)))
            Return .Clone()
        End With
    End Function

    Private Function LoadVis_Set(objVisType As PopupVisualType) As Storyboard
        Return ConvVisual(SelectVisual().
                Resources(GetVisualKey(objVisType)))
    End Function

    Private Function GetVisual2(objVisType As PopupVisualType) As Storyboard
        Return _visualCache(objVisType)
    End Function

    Private Sub SetVisualMode(objAniType As AnimationType)
        Dim setBitMapMode As BitmapScalingMode
        Dim setCacheMode As CacheMode

        Select Case objAniType
            Case aniOpen
                setBitMapMode = BitmapScalingMode.HighQuality
                setCacheMode = Nothing

                RenderOptions.SetEdgeMode(objContainer, EdgeMode.Unspecified)
            Case aniClose
                setBitMapMode = BitmapScalingMode.LowQuality
                setCacheMode = New BitmapCache()

                RenderOptions.SetEdgeMode(objContainer, EdgeMode.Aliased)
        End Select

        objContainer.CacheMode = setCacheMode
        RenderOptions.SetBitmapScalingMode(objContainer, setBitMapMode)
    End Sub

    Private _visualCache As New Dictionary(Of PopupVisualType, Storyboard)()
    Private _visDataIdx As New Dictionary(Of PopupVisualType, Storyboard)()

    Public Sub EstablishVisual(objVisType As PopupVisualType)
        Me.VisDataObject = GetVisual(objVisType, True)
    End Sub

    Public Sub EstablishVisual(objVisType As PopupVisualType, ByRef objSetVisual As Storyboard)
        Dim objPopupVis As Storyboard = GetVisual(objVisType, True)
        objSetVisual = objPopupVis
    End Sub

    Private Sub SetOpenEvents()
        OpenCompleteEvent =
            Sub()
                RemoveHandler VisDataObject.Completed, OpenCompleteEvent
                VisDataObject.Stop()
            End Sub

        AddHandler VisDataObject.Completed, OpenCompleteEvent
    End Sub

    Public Sub ConfigureVisual()
        '    EstablishVisual(PopupVisual_Open)
        'SetOpenEvents()
        '   PrepTransitionVisuals(aniOpen, PopupVisual_Open, VisualStarted)
        '     VisDataObject = objAnimation_Open
    End Sub

    Private Sub InitTransitionVisuals(objAniType As AnimationType, objVisType As PopupVisualType, objVisAction As VisualAction)
        Select Case objAniType
            Case aniOpen
                PrepTransitionVisuals(aniOpen, PopupVisual_Open, VisualStarted)
                TriggerVisuals(objAnimation_Open, True)
            Case aniClose
                PrepTransitionVisuals(objAniType, objVisType, objVisAction)

                ' Dim objTask_VisAdapter = objVisAdapt.ApplyVisuals(True)
                objAnimation_Close.Begin(objContainer, True)
        End Select
    End Sub

    Private Sub PrepTransitionVisuals(objAniType As AnimationType, objVisType As PopupVisualType, objVisAction As VisualAction)
        EstablishVisual(objAniType)
        ProcessVisualEvents(objAniType, objVisAction)
    End Sub

    Public Sub DetectCloseMethod(closeType As PopupVisualType)
        If Not closeType = PopupVisual_CloseByCmd Then
            RaiseEvent EvCloseByClick(Me, EventArgs.Empty)
        End If
    End Sub

    Public Async Function TriggerPopupMenu() As Task
        If Not _hasAnimated Then
            _hasAnimated = True

            BeginOpenTask(objTask_Open)

            '  InitTransitionVisuals(aniOpen, PopupVisual_Open, VisualStarted)

            objAnimation_Open.Begin(Me)

            Await objTask_Open.Task
            SetVisualMode(aniOpen)
        End If
    End Function

    Public Sub TriggerPopupMenu(isN As Boolean)
        If Not _hasAnimated Then
            _hasAnimated = True
        End If
    End Sub

    Private Sub SetAniDuration(ByRef objDur As Duration, valDur As TimeSpan)
        objDur = New Duration(valDur)
    End Sub

    Private Sub BeginClosingTask(ByRef objCloseResult As TaskCompletionSource(Of Boolean))
        objCloseResult.ResetAndInitTask()
    End Sub

    Private Sub PopupCloseComplete()
        objTask_Closing.TrySetResult(True)

        ProcessVisualEvents(aniClose, VisualComplete)

        VisDataObject.Stop()
        Me.Owner = Nothing
    End Sub

    Private Sub PopupOpenComplete(sender As Object, e As EventArgs)
        '  objTask_Open.TrySetResult(True)
        Try
            objAnimation_Open = Nothing
        Catch ex As Exception

        End Try
    End Sub

    Private Sub pmCmd_ShowGameMenu(sender As Object, e As RoutedEventArgs) Handles btnShowGameMenu.Checked
        ShowGameMenuItem()
    End Sub

    Private Async Sub pmCmd_ShowOpts(sender As Object, e As RoutedEventArgs) Handles pmBtn_ShowOptions.Click
        Await ExitPopupMenu(ClosePopup_ByBtn,
                            Async Function()
                                osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
                                Await osFuncLib_ShowOpts.ExecuteDispOpts()
                            End Function)
    End Sub

    Private Async Sub osStopApp()
        Await ExitPopupMenu(ClosePopup_ByBtn,
            Sub()
                osTrayIcon.Visible = False
                End
            End Sub)
    End Sub

    Private Async Sub pmCmd_StartGame(sender As Object, e As RoutedEventArgs) Handles pmBtn_StartGame.Click
        Await ExitPopupMenu(ClosePopup_ByBtn,
                            Sub()
                                Process.Start(New ProcessStartInfo With {
                                  .FileName = dirMtgaExe, .WorkingDirectory = dirMtga,
                                  .WindowStyle = ProcessWindowStyle.Maximized
                              })
                            End Sub)
    End Sub

    Private Async Sub pmCmd_CloseGame(sender As Object, e As RoutedEventArgs) Handles pmBtn_CloseGame.Click
        Dim chkConfirmCloseGame = GetResponse(PromptType.GameMenu_Leave)

        If chkConfirmCloseGame = isYes Then
            Await ExitPopupMenu(ClosePopup_ByBtn,
                                Sub()
                                    With cmd_KillGame
                                        osRunCmd.RunCmd(.First(),
                                        .Last())
                                    End With
                                End Sub)
        End If
    End Sub

    Private Async Sub pmCmd_RestartGame(sender As Object, e As RoutedEventArgs) Handles pmBtn_RestartGame.Click
        Dim chkRestartGameTrigger = GetResponse(PromptType.GameMenu_Restart)

        If chkRestartGameTrigger = isYes Then
            Await ExitPopupMenu(ClosePopup_ByBtn,
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
        End If
    End Sub

    Private Sub pmCmd_Exit(sender As Object, e As RoutedEventArgs) Handles pmBtn_Exit.Click
        If GetResponse(PromptType.CloseApp) = isNo Then Exit Sub

        osStopApp()
    End Sub

    Public objVisAdapt As VisQualityAdapter

    Private Function EstablishVisConfig() As VisAdapterConfig
        Return VisAdapterConfig.EnableAll
    End Function

    Private Sub BufferPopupMenu()
        Me.Show()
        Me.Hide()
    End Sub

    Public Sub PrepPopupMenu()
        EstablishVisual(PopupVisual_Open)
        SetOpenEvents()

        InitializeVisAdapter(Me, EstablishVisConfig(), objContainer)
    End Sub

    Private Async Function ExitPopupMenu(popupCloseAction As PopupCloseAction, objMenuCmd As Action) As Task
        Await osHandler_UI.ClosePopupMenu(popupCloseAction)
        Await Task.Delay(200)

        PrepDispatcher().
            Invoke(Sub()
                       objMenuCmd()
                   End Sub)
    End Function

    Private Async Function ExitPopupMenu(popupCloseAction As PopupCloseAction, objMenuCmd As Func(Of Task)) As Task
        Await osHandler_UI.ClosePopupMenu(popupCloseAction)
        Await Task.Delay(200)

        Dim objTask_MenuCmd = PrepDispatcher().
            InvokeAsync(Function()
                            Return objMenuCmd()
                        End Function)

        Await objTask_MenuCmd.Task.Unwrap()
    End Function

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

        With Me
            .CommandBindings.Clear()
            .InputBindings.Clear()

            If root Is Nothing Then Return

            BindingOperations.ClearAllBindings(root)

            For i = 0 To VisualTreeHelper.GetChildrenCount(root) - 1
                ClearResources(VisualTreeHelper.GetChild(root, i))
            Next

            .Resources.MergedDictionaries.Clear()
            .Resources.Clear()

            .Style = Nothing

            .DataContext = Nothing
        End With
    End Sub

    Private Sub osPopupMenu_GUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ShowGameMenuItem()
    End Sub

End Class

Partial Public Class osPopupMenu_GUI
    Implements INotifyPropertyChanged

    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000

    Private Const MA_NOACTIVATE As Integer = 3

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As Integer) As Integer
    End Function

    Public ReadOnly Property popupBoundWidth As Double
        Get
            With Forms.SystemInformation.VirtualScreen
                Return .Width
            End With
        End Get
    End Property

    Public ReadOnly Property popupBoundHeight As Double
        Get
            With Forms.SystemInformation.VirtualScreen
                Return .Height
            End With
        End Get
    End Property

    Private _dispGameMenuItem As GameMenuItem
    Public Property DisplayGameMenuItem As GameMenuItem
        Get
            Return _dispGameMenuItem
        End Get
        Set(value As GameMenuItem)
            If _dispGameMenuItem <> value Then
                _dispGameMenuItem = value
                OnDisplayGameMenuChanged()
            End If
        End Set
    End Property

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub ShowGameMenuItem()
        DisplayGameMenuItem = If(CoreDataLib.IsGameRunning(),
            GameMenuItem.ShowClose, GameMenuItem.ShowStart)
    End Sub

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        SetWinOpts(GetWinHwnd(Me))
    End Sub

    Public Event DisplayGameMenuChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub OnDisplayGameMenuChanged(<CallerMemberName> Optional name As String = Nothing)
        RaiseEvent DisplayGameMenuChanged(Me, New PropertyChangedEventArgs(name))
    End Sub

End Class
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports osAutoCast.GameMenuOpts
Imports osAutoCast.CoreDataLib
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports osAutoCast.DataTypeLib.AnimationType
Imports osAutoCast.DataTypeLib.AnimationVisual
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.DataTypeLib.PopupVisualType
Imports osAutoCast.DataTypeLib.VisualAction
Imports System.Windows.Media.Animation
Imports System.Threading
Imports osAutoCast.osStyle
Imports System.Windows.Threading

#Disable Warning BC42353

Public Class osPopupMenu_GUI

    Private _hasAnimated As Boolean = False

    Public Event EvCloseByClick(sender As Object, e As EventArgs)

    Private objTask_Closing As TaskCompletionSource(Of Boolean)

    Private objAnimation_Open As Storyboard = Nothing
    Private objAnimation_Close As Storyboard = Nothing

    Private sb2 As Storyboard = Nothing

    Private idxPopupVisuals As New Dictionary(Of PopupVisualType, String) From {
        {PopupVisual_Open, "PopupVisual_Open"},
        {PopupVisual_Close, "PopupVisual_Close"},
        {PopupVisual_CloseQuick, "PopupVisual_QuickClose"},
        {PopupVisual_CloseByCmd, "PopupVisual_Close"}
    }

    Private OpenCompleteEvent As EventHandler = AddressOf PopupOpenComplete
    Private CloseCompleteEvent As EventHandler = AddressOf PopupCloseComplete

    Private Property isVisualConfigured As Boolean

    Private ReadOnly Property objContainer As Border
        Get
            Return Me.popupMainContainer
        End Get
    End Property

    Private Sub ResetVisualConfig()
        isVisualConfigured = False
    End Sub

    Public Async Function InitPopupClose(objVisType As PopupVisualType, Optional closeFromCmd As Boolean = False,
                                         Optional isQuickClose As Boolean = False) As Task

        DetectCloseMethod(objVisType)
        BeginClosingTask(objTask_Closing)

        InitTransitionVisuals(aniClose, objVisType, VisualStarted)

        Await objTask_Closing.Task

    End Function

    Private Function GetVisualKey(objVisType As PopupVisualType) As String
        Return idxPopupVisuals.
            First(Function(visKey)
                      Return visKey.Key = objVisType
                  End Function).Value
    End Function

    Private Sub PrepTransitionVisuals(objAniType As AnimationType, objVisType As PopupVisualType, objVisAction As VisualAction)
        Select Case objAniType
            Case aniOpen
                EstablishVisual(objContainer, objVisType, objAnimation_Open)
            Case aniClose
                EstablishVisual(objContainer, objVisType, objAnimation_Close)
        End Select

        ProcessVisualEvents(objAniType, objVisAction)
    End Sub

    Private Sub ProcessVisualEvents(objAniType As AnimationType, objVisAction As VisualAction)
        Select Case objAniType
            Case aniOpen
                Select Case objVisAction
                    Case VisualStarted
                        AddHandler objAnimation_Open.Completed,
                            OpenCompleteEvent
                    Case VisualComplete
                        RemoveHandler objAnimation_Open.Completed,
                            OpenCompleteEvent
                End Select
            Case aniClose
                Select Case objVisAction
                    Case VisualStarted
                        AddHandler objAnimation_Close.Completed,
                            CloseCompleteEvent
                    Case VisualComplete
                        RemoveHandler objAnimation_Close.Completed,
                            CloseCompleteEvent
                End Select
        End Select
    End Sub

    Private Function SelectVisual(objContainer As FrameworkElement) As Style
        With objContainer.Style
            Dim objContainer_Visual = .Resources.MergedDictionaries.First()
            Return CType(objContainer_Visual("PopupVisuals"), Style)
        End With
    End Function

    Private Async Sub TriggerVisuals(objPopupWindow As FrameworkElement, objVisualData As Storyboard)
        Await objPopupWindow.Dispatcher.BeginInvoke(
            Sub()
                Try
                    objVisualData.Begin(objPopupWindow)
                Catch ex As Exception : End Try
            End Sub, DispatcherPriority.Render)
    End Sub

    Private Function isOpenScaleVisual(element As FrameworkElement, ByRef objScaleObj As Double) As Boolean
        If element.Name = "visPopupScale" Then
            Dim openScaleObj As Double = CDbl(element.TryFindResource("PopupAni_OpenScale"))

            If openScaleObj = Nothing Then
                objScaleObj = openScaleObj
                Return True
            Else : Return False : End If
        Else : Return False : End If
    End Function

    Private Function GetVisual(objVisType As PopupVisualType) As Storyboard
        With TryCast(SelectVisual(objContainer).
                Resources(GetVisualKey(objVisType)), Storyboard)
            Return .Clone()
        End With
    End Function

    Private Sub EstablishVisual(element As FrameworkElement, objVisType As PopupVisualType, ByRef objSetVisual As Storyboard)
        Dim openScaleObj As Double

        If isOpenScaleVisual(element, openScaleObj) Then
            osPopupScale.SetPopupScale(element, 0.01)
        Else : osPopupScale.SetPopupScale(element, CDbl(openScaleObj))
        End If

        Dim objPopupVis As Storyboard = GetVisual(objVisType)

        For Each objAnimation As Timeline In objPopupVis.Children
            Timeline.SetDesiredFrameRate(objAnimation, 30)
            Storyboard.SetTarget(objAnimation, objContainer)
            '  ProcessVisual(objAnimation)
        Next

        objSetVisual = objPopupVis
    End Sub

    Private Sub ProcessVisual(tl As Timeline)
        If tl Is Nothing Then Return

        Dim dpPath As PropertyPath = Storyboard.GetTargetProperty(tl)
        If dpPath IsNot Nothing Then
            Dim pathStr As String = If(dpPath.Path, String.Empty)

            If pathStr.Contains("PopupScale") Then
                Dim objAnimationData = TryCast(tl, DoubleAnimation)
                If objAnimationData IsNot Nothing Then
                    objAnimationData.From = Nothing
                Else
                    Dim dakf = TryCast(tl, DoubleAnimationUsingKeyFrames)
                    If dakf IsNot Nothing AndAlso dakf.KeyFrames.Count > 0 Then
                        Dim k0 = dakf.KeyFrames(0)
                        If k0.KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero) Then
                            dakf.KeyFrames.RemoveAt(0)
                        End If
                    End If
                End If
            End If
        End If

        'Dim container = TryCast(tl, ParallelTimeline)
        'If container IsNot Nothing Then
        '    For Each child As Timeline In container.Children
        '        Storyboard.SetTarget(child, Storyboard.GetTarget(tl))
        '        ProcessVisual(child)
        '    Next
        'End If

        Dim sbAsTl = TryCast(tl, Storyboard)
        If sbAsTl IsNot Nothing Then
            For Each child As Timeline In sbAsTl.Children
                Storyboard.SetTarget(child, Storyboard.GetTarget(tl))
                ProcessVisual(child)
            Next
        End If
    End Sub

    Private Sub ConfigureVisual()
        PrepTransitionVisuals(aniOpen, PopupVisualType.PopupVisual_Open, VisualStarted)
    End Sub

    Private Sub InitTransitionVisuals(objAniType As AnimationType, objVisType As PopupVisualType, objVisAction As VisualAction)
        Select Case objAniType
            Case aniOpen : TriggerVisuals(objContainer, objAnimation_Open)
            Case aniClose
                PrepTransitionVisuals(objAniType, objVisType, objVisAction)
                TriggerVisuals(objContainer, objAnimation_Close)
        End Select
    End Sub

    Public Sub DetectCloseMethod(closeType As PopupVisualType)
        If Not closeType = PopupVisual_CloseByCmd Then
            RaiseEvent EvCloseByClick(Me, EventArgs.Empty)
        End If
    End Sub

    Public Sub InitPopupOpen()
        If Not _hasAnimated Then
            _hasAnimated = True
            InitTransitionVisuals(aniOpen, PopupVisual_Open, VisualStarted)
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

        ProcessVisualEvents(AnimationType.aniClose, VisualComplete)

        objAnimation_Close = Nothing
        Me.Owner = Nothing
    End Sub

    Private Sub PopupOpenComplete()
        ProcessVisualEvents(AnimationType.aniOpen, VisualComplete)

        objAnimation_Open = Nothing
    End Sub

    Private Sub pmCmd_ShowGameMenu(sender As Object, e As RoutedEventArgs) Handles btnShowGameMenu.Checked
        ShowGameMenuItem()
    End Sub

    Private Async Sub pmCmd_ShowOpts(sender As Object, e As RoutedEventArgs) Handles pmBtn_ShowOptions.Click
        Await ExitPopupMenu(Async Function()
                                osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
                                Await osFuncLib_ShowOpts.ExecuteDispOpts()
                            End Function)
    End Sub

    Private Async Sub osStopApp()
        Await ExitPopupMenu(
            Sub()
                osTrayIcon.Visible = False
                End
            End Sub)
    End Sub

    Private Async Sub pmCmd_StartGame(sender As Object, e As RoutedEventArgs) Handles pmBtn_StartGame.Click
        Await ExitPopupMenu(
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
            Await ExitPopupMenu(
                Sub()
                    Dim cmdCloseMTGA = CmdRunner.RunCmd("taskkill", "/f /im MTGA.exe")
                End Sub)
        End If
    End Sub

    Private Sub pmCmd_Exit(sender As Object, e As RoutedEventArgs) Handles pmBtn_Exit.Click
        Dim chkConfirmExit = GetResponse(PromptType.CloseApp)
        If chkConfirmExit = isNo Then Exit Sub

        osStopApp()
    End Sub

    Public Sub InitPopupMenu()
        ConfigureVisual()
        ShowGameMenuItem()
    End Sub

    Private Async Function ExitPopupMenu() As Task
        Await osHandler_UI.ResetPopupMenu(True)
        Await Task.Delay(200)
    End Function

    Private Async Function ExitPopupMenu(objMenuCmd As Action) As Task
        Await osHandler_UI.ResetPopupMenu(True)
        Await Task.Delay(200)

        PrepDispatcher().
            Invoke(Sub()
                       objMenuCmd()
                   End Sub)
    End Function

    Private Async Function ExitPopupMenu(objMenuCmd As Func(Of Task)) As Task
        Await osHandler_UI.ResetPopupMenu(PopupVisual_CloseQuick)
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
            End Select
        Else
            osFuncLib_InputScan.SetMonitorState(MonitorStatus.Starting)
            Application.RestartMonitor()

            Return UpdateStatus.ToEnabled
        End If
    End Function

    Private Sub SetNewStatus(setStatus As Boolean)
        osIsEnabled = setStatus
        osEnabledStatus = setStatus
        _isAppEnabled = setStatus


        UpdateTray(setStatus, True)
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
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

    Public Event EvCloseByClick(sender As Object, e As EventArgs)

    Private objTask_Closing As TaskCompletionSource(Of Boolean)

    Private objAnimation_Open As osPopupAnimation = Nothing
    Private objAnimation_Close As osPopupAnimation = Nothing

    Private OpenCompleteEvent As EventHandler = AddressOf OpenComplete

    Public Async Function InitPopupClose(Optional closeFromCmd As Boolean = False,
                                         Optional isQuickClose As Boolean = False) As Task

        DetectCloseMethod(closeFromCmd)
        BeginClosingTask(objTask_Closing)

        objAnimation_Close = New osPopupAnimation(AnimationType.aniClose,
                                                  AnimationObject.aniPopup, isQuickClose)

        AddHandler objAnimation_Close.aniY.Completed,
            Sub()
                PopupCloseComplete(objTask_Closing)
            End Sub

        If isQuickClose Then
            Me.winPopupMenu.MaxHeight = Double.PositiveInfinity
            Me.winPopupMenu.MaxWidth = Double.PositiveInfinity

            Me.popupContentContainer.MaxHeight = Double.PositiveInfinity
            Me.popupContentContainer.MaxWidth = Double.PositiveInfinity

            Me.popupMainContainer.MaxHeight = Double.PositiveInfinity
            Me.popupMainContainer.MaxWidth = Double.PositiveInfinity

        End If

        Me.popScale.BeginAnimation(ScaleTransform.ScaleXProperty, objAnimation_Close.aniX)
        Me.popScale.BeginAnimation(ScaleTransform.ScaleYProperty, objAnimation_Close.aniY)

        Me.popupMainContainer.BeginAnimation(Border.OpacityProperty, objAnimation_Close.aniFade)

        Dim resPopupClose = Await objTask_Closing.Task

    End Function

    Private Sub OptimizeAndSetMaxSize(root As DependencyObject)
        If root Is Nothing Then Return

        ' Collapse the container so layout is not performed on each change
        Dim wasCollapsed As Boolean = False
        Dim feRoot = TryCast(root, FrameworkElement)
        If feRoot IsNot Nothing Then
            If feRoot.Visibility <> Visibility.Collapsed Then
                feRoot.Visibility = Visibility.Collapsed
            Else
                wasCollapsed = True
            End If
        End If

        ' Iterative visual-only traversal (stack) — no LogicalTreeHelper calls
        Dim stack As New Stack(Of DependencyObject)()
        Dim seen As New HashSet(Of DependencyObject)()
        stack.Push(root)
        seen.Add(root)

        While stack.Count > 0
            Dim current = stack.Pop()

            Dim fe = TryCast(current, FrameworkElement)
            If fe IsNot Nothing Then

                ' Only change if different to avoid unnecessary layout invalidations
                If fe.MaxWidth <> Double.PositiveInfinity Then fe.MaxWidth = Double.PositiveInfinity
                If fe.MaxHeight <> Double.PositiveInfinity Then fe.MaxHeight = Double.PositiveInfinity

                ' Use Auto sizing only if currently not Auto - avoids resetting same values
                If Not Double.IsNaN(fe.Width) Then fe.Width = Double.NaN
                If Not Double.IsNaN(fe.Height) Then fe.Height = Double.NaN
            End If

            Dim childCount As Integer = 0
            Try
                childCount = VisualTreeHelper.GetChildrenCount(current)
            Catch ex As Exception
                childCount = 0
            End Try

            For i As Integer = 0 To childCount - 1
                Dim child = VisualTreeHelper.GetChild(current, i)
                If child IsNot Nothing AndAlso Not seen.Contains(child) Then
                    seen.Add(child)
                    stack.Push(child)
                End If
            Next
        End While

        ' Restore visibility (unless it was already collapsed)
        If feRoot IsNot Nothing AndAlso Not wasCollapsed Then
            feRoot.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub SetAllControlsMaxSize(root As DependencyObject)
        If root Is Nothing Then Return

        Dim q As New Queue(Of DependencyObject)()
        q.Enqueue(root)

        While q.Count > 0
            Dim current = q.Dequeue()

            ' If it's a FrameworkElement, set sizing properties
            Dim fe = TryCast(current, FrameworkElement)
            If fe IsNot Nothing Then
                ' Don't change transforms or RenderTransforms; only layout properties
                fe.MaxWidth = Double.PositiveInfinity
                fe.MaxHeight = Double.PositiveInfinity

                ' Let layout size automatically unless a fixed size is desired:
                fe.Width = Double.NaN
                fe.Height = Double.NaN
            End If

            ' Also consider FrameworkContentElement if needed (rare)
            ' Many content elements don't expose Width/Height so we skip setting them.

            ' Enqueue visual children (use VisualTreeHelper where available)
            Try
                Dim visualChildCount = VisualTreeHelper.GetChildrenCount(current)
                For i As Integer = 0 To visualChildCount - 1
                    Dim child = VisualTreeHelper.GetChild(current, i)
                    If child IsNot Nothing Then q.Enqueue(child)
                Next
            Catch ex As Exception
                ' Some nodes may throw for GetChildrenCount; ignore and continue
            End Try

            ' Enqueue logical children (to catch content presenters / items etc.)
            Try
                For Each obj In LogicalTreeHelper.GetChildren(current)
                    Dim dep = TryCast(obj, DependencyObject)
                    If dep IsNot Nothing Then q.Enqueue(dep)
                Next
            Catch ex As Exception
                ' ignore
            End Try
        End While
    End Sub

    Public Sub DetectCloseMethod(Optional isCmd As Boolean = False)
        If Not isCmd Then
            RaiseEvent EvCloseByClick(Me, EventArgs.Empty)
        End If
    End Sub

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

        objAnimation_Open = New osPopupAnimation(AnimationType.aniOpen, AnimationObject.aniPopup)

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
        Await osHandler_UI.ResetPopupMenu(True)
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
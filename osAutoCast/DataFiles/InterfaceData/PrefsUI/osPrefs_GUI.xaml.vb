Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.PrefSaveState
Imports osAutoCast.DataTypeLib.PrefUI_State
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.osControls
Imports osAutoCast.osVisualAdapter
Imports osKeyTime = System.Windows.Media.Animation.KeyTime
Imports osPrefData = osAutoCast.osPrefLib.osPreferenceLib

#Disable Warning BC42353
#Disable Warning BC42104
#Disable Warning BC42024

Public Class osPrefs_GUI

    Private Function GetVisualState(objVisType As PrefUI_State) As String
        Return idxOsPrefVisuals.First(
            Function(visKey)
                Return visKey.Key = objVisType
            End Function).Value
    End Function

    Private Sub InitVisual(objVisType As PrefUI_State)
        Select Case objVisType
            Case PrefUI_Open
                SetOpenEvents()
                BufferPrefWin()
            Case PrefUI_Close
                Dim visDataN = GetVisualState(PrefUI_Close)
                SetCloseVisualData(visDataN)
        End Select
    End Sub

    Private Sub BufferPrefWin()
        Me.Show()
        Me.Hide()
    End Sub

    Private Function EstablishVisConfig() As VisAdapterConfig
        Return VisAdapterConfig.EnableAll
        'Dim a = VisAdapterConfig.EnableAll
        'a = a And Not VisAdapterConfig.ModifyLayout
        'Return a
    End Function

    Public Async Function osPrefs_InitUi() As Task
        Await InitializeVisAdapter(GetVisualState(PrefUI_Open), Me, EstablishVisConfig(),
                                   Sub() BufferPrefWin(), Sub()
                                                              SetVisualMode(PrefUI_Open)
                                                              ActivatePrefTracker()
                                                          End Sub, SetVisTargets())
    End Function

    Private Function SetVisTargets() As UIElement()
        Return {Me, prefContainer, osTitleCover, osContentContainer}
    End Function

    Public Sub SetCloseMonitor(objAwaitClose As TaskCompletionSource(Of Boolean))
        objCloseMonitor = objAwaitClose
    End Sub

    Private Sub SetOpenEvents()
        evtComplete_Open =
            Sub()
                RemoveHandler VisDataObject.Completed, evtComplete_Open

                SetVisualMode(PrefUI_Open)
                ActivatePrefTracker()
            End Sub

        AddHandler VisDataObject.Completed, evtComplete_Open
    End Sub

    Private Sub SetVisualMode(valPrefState As PrefUI_State)
        Select Case valPrefState
            Case PrefUI_Open
                osTitleCover.Visibility = Visibility.Collapsed
            Case PrefUI_Close
                osTitleCover.Visibility = Visibility.Visible
        End Select
    End Sub

    Private Sub SetCloseEvents()
        RemoveHandler VisDataObject.Completed,
                                            evtComplete_Open
        InitVisual(PrefUI_Close)

        evtComplete_Close =
            Sub()
                RemoveHandler VisDataObject.Completed,
                                                        evtComplete_Close
                Me.Close()
            End Sub

        AddHandler VisDataObject.Completed,
                                            evtComplete_Close

        SetVisualMode(PrefUI_Close)
    End Sub

    Private Async Function SetCloseEvents(isN As Boolean) As Task
        Await SetCloseVisualData_WithTask(
            GetVisualState(PrefUI_Close), Sub()
                                              Me.Close()
                                          End Sub)

        SetVisualMode(PrefUI_Close)
    End Function

    Public Sub osPrefsIU_Present()
        Me.Show()

        Dim objHwnd = New WindowInteropHelper(Me).Handle
        SetWindowPos(objHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)

        Me.Topmost = True
    End Sub

    Private Async Sub osPrefsBtnClk_SavePrefs(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Save.Click
        If objOsPrefTracker.prefsChanged Then
            Dim chkDoSave = GetResponse(PromptType.Prefs_Save)

            If chkDoSave = isYes Then
                Await TriggerPrefSave()
            End If
        End If
    End Sub

    Private Function GenerateLoadSpinOverlay() As Border
        If objLoadSpinContainer IsNot Nothing Then Return objLoadSpinContainer

        Dim objLoadSpinner As New osControls.osLoadSpinner With {
            .SpinnerSize = 50, .StrokeThickness = 16, .IsActive = False,
            .SpinnerBrush = New SolidColorBrush(Color.FromRgb(&H5F, &H12, &H12))
        }

        Dim objLoadText As New TextBlock With {
            .Text = "Please Wait", .FontSize = 16,
            .Foreground = Brushes.White,
            .Margin = New Thickness(0, 2, 0, 0),
            .HorizontalAlignment = HorizontalAlignment.Center
        }

        Dim objLoadSpinContent As New StackPanel With {
            .Orientation = Orientation.Vertical,
            .VerticalAlignment = VerticalAlignment.Center,
            .Margin = New Thickness(8, 4, 8, 2)
        }

        objLoadSpinContent.Children.Add(objLoadSpinner)
        objLoadSpinContent.Children.Add(objLoadText)

        objLoadSpinContainer = New Border With {
            .Background = New SolidColorBrush(Color.FromArgb(&HB4, &H22, &H22, &H22)),
            .Child = objLoadSpinContent, .Opacity = 0, .Visibility = Visibility.Collapsed,
            .HorizontalAlignment = HorizontalAlignment.Center, .VerticalAlignment = VerticalAlignment.Center,
            .Margin = New Thickness(0, 0, 0, 20), .Padding = New Thickness(4, 8, 4, 4),
            .CornerRadius = New CornerRadius(8), .IsHitTestVisible = True
        }

        Return objLoadSpinContainer
    End Function

    Public Async Function ComposeLoadOverlay() As Task
        Await Dispatcher.InvokeAsync(AddressOf ShowOverlay_UI)
        Await Dispatcher.Yield(DispatcherPriority.Render)
    End Function

    Private Sub ShowOverlay_UI()
        SetVisualMode(PrefUI_Close)

        SyncLock objLoadSpinLock

            osSpinLoadContainer = New Grid With {
                .Name = "osSpinLoadContainer",
                .Visibility = Visibility.Visible,
                .Opacity = 0,
                .IsHitTestVisible = True,
                .Background = New SolidColorBrush(Color.FromArgb(&HC4, 0, 0, 0)),
                .HorizontalAlignment = HorizontalAlignment.Stretch,
                .VerticalAlignment = VerticalAlignment.Stretch
            }

            Grid.SetRow(osSpinLoadContainer, 0)
            Grid.SetRowSpan(osSpinLoadContainer, 4)

            Panel.SetZIndex(osSpinLoadContainer, 999)
            osContentContainer.Children.Add(osSpinLoadContainer)

            Dim objLoadOverlay = GenerateLoadSpinOverlay()
            osSpinLoadContainer.Children.Add(objLoadOverlay)

            objLoadOverlay.Visibility = Visibility.Visible

            Dim objLoadSpinner = DirectCast(DirectCast(objLoadOverlay.Child, StackPanel).
                Children(0), osControls.osLoadSpinner)

            objLoadSpinner.IsActive = True

            osSpinLoadContainer.BeginAnimation(
                Grid.OpacityProperty, New DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(115)) With
                    {.EasingFunction = New QuadraticEase()
                })

            objLoadOverlay.BeginAnimation(
                Border.OpacityProperty, New DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(115)) With
                    {.EasingFunction = New QuadraticEase()
                })

            Dispatcher.BeginInvoke(Sub() YieldVisuals(),
                                   DispatcherPriority.Render, Nothing)
        End SyncLock
    End Sub

    Private Sub YieldVisuals() : End Sub

    Public Async Function HideOverlay() As Task
        Await Dispatcher.InvokeAsync(AddressOf HideOverlay_UI)
        Await Dispatcher.Yield(DispatcherPriority.Render)
    End Function

    Private Sub HideOverlay_UI()
        SyncLock objLoadSpinLock
            If objLoadSpinContainer Is Nothing Then Return

            Dim objLoadSpinner = DirectCast(DirectCast(objLoadSpinContainer.Child, StackPanel).Children(0),
                                 osControls.osLoadSpinner)
            objLoadSpinner.IsActive = False

            Dim visFadeOut As New DoubleAnimation(0, TimeSpan.FromMilliseconds(150))

            AddHandler visFadeOut.Completed,
                Sub()
                    objLoadSpinContainer.Visibility = Visibility.Collapsed
                    osSpinLoadContainer.Children.Clear()
                    osContentContainer.Children.Remove(osSpinLoadContainer)

                    SetVisualMode(PrefUI_Open)
                End Sub

            osSpinLoadContainer.BeginAnimation(Grid.OpacityProperty, visFadeOut)
        End SyncLock
    End Sub

    Public Async Function DisplayLoadTask(taskReload As Func(Of Task)) As Task
        Await ComposeLoadOverlay()

        Await Task.Delay(75)
        Await taskReload()

        Await HideOverlay()
    End Function

    Private Async Function TriggerPrefSave(Optional closeOnSave As Boolean = False) As Task
        Await DisplayLoadTask(AddressOf ReloadPrefs)

        If closeOnSave Then
            Await osPrefs_InitClose()
        End If
    End Function

    Private Async Function ReloadPrefs() As Task
        Await osPrefDataIdx.SavePrefsFileAsync()

        Await Task.Run(
            Async Function()
                Await osHandler_AutoCast.StopAutoCastAsync()

                Await Task.Delay(2750)
                Await osHandler_AutoCast.StartAutoCastAsync()
                Await Task.Delay(1500)
                '    Await osHandler_UI.CloseAndResetAutoCast()
            End Function)

        SetSaveState(PrefSaveState.Prefs_Saved)

        ActivatePrefTracker(True)
        RefreshVisQuality()
    End Function

    Private Sub RefreshVisQuality()
        _VisQualitySetting = "n/a"
        OnPropertyChanged(NameOf(VisQualitySetting))
    End Sub

    Private Async Function osPrefs_InitClose() As Task
        Await SetCloseEvents(True)
        objCloseMonitor.TrySetResult(True)
    End Function

    Private Async Sub osPrefsBtnClk_Close(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Close.Click
        Select Case GetPrefSaveState()
            Case Prefs_NoChanges
                Await osPrefs_InitClose()
            Case Prefs_Saved
                Await osPrefs_InitClose()
            Case Prefs_NotSaved
                Select Case GetResponse(PromptType.Prefs_Close)
                    Case isYes
                        Await TriggerPrefSave(True)
                    Case isNo
                        RevertPrefSettings()
                    Case isCancel
                        Exit Sub
                End Select
        End Select
    End Sub

    Private Sub osPreferenceLib_PropertyChanged(sender As Object, e As PropertyChangedEventArgs)
        If prefsReverted Then
            SetSaveState(PrefSaveState.Prefs_NoChanges)
            prefsReverted = False
        Else
            SetSaveState(PrefSaveState.Prefs_NotSaved)
        End If
    End Sub

    Private Sub SetSaveState(valSaveState As PrefSaveState)
        Select Case valSaveState
            Case PrefSaveState.Prefs_NotSaved
                isSaved = False
                IsSaveEnabled = True
            Case PrefSaveState.Prefs_Saved
                isSaved = True
                IsSaveEnabled = False
            Case PrefSaveState.Prefs_NoChanges
                isSaved = True
                IsSaveEnabled = False
        End Select
    End Sub

End Class

Partial Public Class osPrefs_GUI
    Implements INotifyPropertyChanged

    Private objCloseMonitor As TaskCompletionSource(Of Boolean)

    Private evtComplete_Open As EventHandler
    Private evtComplete_Close As EventHandler

    Private isSaved As Boolean = False
    Private prefsReverted As Boolean = False

    Private objExpandH As Double

    Private objOsPrefTracker As osPrefLib.osPrefMonitor(Of osPrefData)

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

    Private objLoadSpinContainer As Border
    Private osSpinLoadContainer As Grid

    Private ReadOnly objLoadSpinLock As New Object()

    Private idxOsPrefVisuals As New Dictionary(Of PrefUI_State, String) From {
        {PrefUI_Open, "osPrefsVis_Disp"},
        {PrefUI_Close, "osPrefsVis_Close"}
    }

    Private _VisQualitySetting As String = "n/a"
    Public ReadOnly Property VisQualitySetting As String
        Get
            If _VisQualitySetting = "n/a" Then
                _VisQualitySetting = osPrefData.Data.GenOpts_VisualQuality
            End If

            If isSaved Then
                _VisQualitySetting = osPrefData.Data.GenOpts_VisualQuality
            End If

            Return _VisQualitySetting
        End Get
    End Property

    Private _saveEnabled As Boolean = False
    Public Property IsSaveEnabled As Boolean
        Get
            Return _saveEnabled
        End Get
        Set(ByVal canSave As Boolean)
            _saveEnabled = canSave
            OnPropertyChanged(NameOf(IsSaveEnabled))
        End Set
    End Property

    Public ReadOnly Property prefContainer As osBorder
        Get
            Return Me.osPrefsMainContainer
        End Get
    End Property

    Public ReadOnly Property osContentContainer As Grid
        Get
            Return Me.BottomContainer
        End Get
    End Property

    Public ReadOnly Property osTitleCover As Border
        Get
            Return Me.osPrefsTitlePanel
        End Get
    End Property

    Public ReadOnly Property osScaleRender As ScaleTransform
        Get
            Return Me.osPrefsOutlineRender
        End Get
    End Property

    Private ReadOnly Property osPrefDataIdx As osPrefData.osPrefIndex
        Get
            Return osPrefData.Data.objOsPrefIdx
        End Get
    End Property

    Private ReadOnly Property osVisDataArray As IEnumerable(Of DoubleAnimationUsingKeyFrames)
        Get
            Return VisDataObject.Children.OfType(Of DoubleAnimationUsingKeyFrames)
        End Get
    End Property

    Private Function CreatePrefMonitor() As osPrefLib.osPrefMonitor(Of osPrefData)
        Return New osPrefLib.osPrefMonitor(Of osPrefData)
    End Function

    Private Sub InitializePrefMonitor()
        objOsPrefTracker.Attach(DirectCast(Me.DataContext, osPrefData))
    End Sub

    Public Sub ActivatePrefTracker(Optional doReset As Boolean = False)
        If doReset Then
            objOsPrefTracker.Detach()
            objOsPrefTracker = Nothing
        End If

        objOsPrefTracker = CreatePrefMonitor()
        InitializePrefMonitor()
    End Sub

    Public Sub RevertPrefSettings()
        prefsReverted = True
        objOsPrefTracker.Revert()
    End Sub

    Private Function isMouseDown(e As MouseButtonEventArgs) As Boolean
        Return e.LeftButton = MouseButtonState.Pressed
    End Function

    Private Function HasPrefChanges() As Boolean
        Return objOsPrefTracker.prefsChanged
    End Function

    Private Function HasUnsavedChanges() As Boolean
        Return HasPrefChanges() And Not isSaved
    End Function

    Private Function HasNoChanges() As Boolean
        Return Not HasPrefChanges() AndAlso Not isSaved
    End Function

    Private Function GetPrefSaveState() As PrefSaveState
        If HasUnsavedChanges() Then
            Return Prefs_NotSaved : End If

        Select Case True
            Case HasNoChanges() : Return Prefs_NoChanges
            Case isSaved : Return Prefs_Saved
        End Select
    End Function

    Private Sub DragMovePrefWin(sender As Object, e As MouseButtonEventArgs) Handles osPrefs_TitleBar.MouseLeftButtonDown
        If isMouseDown(e) Then
            DragMove()
        End If
    End Sub

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        CoreDataLib.SetWinOpts(CoreDataLib.GetWinHwnd(Me))
    End Sub

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer,
                                         Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub OnPropertyChanged(Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

End Class
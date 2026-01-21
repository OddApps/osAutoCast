Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.PrefUI_State
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.DataTypeLib.PrefSaveState
Imports osAutoCast.DataTypeLib.VisRenderMode
Imports osAutoCast.osStyle
Imports osAutoCast.osControls
Imports osPrefData = osAutoCast.osPrefLib.osPreferenceLib
Imports osKeyTime = System.Windows.Media.Animation.KeyTime

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

    Private Function AllocVis(visObject As Object) As Storyboard
        Return TryCast(visObject, Storyboard)
    End Function

    Private Function FetchPrefVis(objVisResource As Style, objVisType As PrefUI_State) As Storyboard
        Return AllocVis(Me.Resources(GetVisualState(objVisType)))
    End Function

    Private Function SetVisual(objVisType As PrefUI_State) As Storyboard
        Return If(objVisType = PrefUI_Open,
            visPrefUI_Open, visPrefUI_Close)
    End Function

    Private Sub EstablishVisual(objVisType As PrefUI_State, ByRef objVis As Storyboard)
        Dim objLoadVis = FetchPrefVis(osPrefRes, objVisType)
        objVis = objLoadVis
    End Sub

    Private Sub InitVisual(objVisType As PrefUI_State)
        Select Case objVisType
            Case PrefUI_Open
                EstablishVisual(PrefUI_Open, visPrefUI_Open)

                ApplyExpanderSize()
                SetOpenEvents()
                BufferPrefWin()
            Case PrefUI_Close
                EstablishVisual(PrefUI_Close, visPrefUI_Close)
                ApplyCloserSize()
        End Select
    End Sub

    Private Sub BufferPrefWin()
        Me.Show()
        Me.Hide()
    End Sub

    Private Function CreatePrefMonitor() As osPrefLib.osPrefMonitor(Of osPrefData)
        Return New osPrefLib.osPrefMonitor(Of osPrefData)
    End Function

    Private Sub InitializePrefMonitor()
        objOsPrefTracker.Attach(DirectCast(Me.DataContext, osPrefData))
    End Sub

    Public Sub ActivatePrefTracker()
        objOsPrefTracker = CreatePrefMonitor()
        InitializePrefMonitor()
        'objOsPrefTracker = New osPrefTracker(Of osPrefData)(osPrefData.Data)
        'Await objOsPrefTracker.PreservePrefs(True)
    End Sub

    Public Sub PrepPrefVis()
        InitVisual(PrefUI_Open)
    End Sub

    Public Sub DisplayPrefsUI(objAwaitClose As TaskCompletionSource(Of Boolean))
        objCloseMonitor = objAwaitClose
        visPrefUI_Open.Begin(prefContainer, True)
    End Sub

    Private Function FetchExpandVisual() As DoubleAnimationUsingKeyFrames
        Return CType(visPrefUI_Open.Children.First(
            Function(objVis)
                Return objVis.Name = "osPrefExpandVis"
            End Function), DoubleAnimationUsingKeyFrames)
    End Function

    Private Function FetchCloseVisual() As DoubleAnimationUsingKeyFrames
        Return CType(visPrefUI_Close.Children.First(
            Function(objVis)
                Return objVis.Name = "osPrefExpandVisC"
            End Function), DoubleAnimationUsingKeyFrames)
    End Function

    Private Function SetVisDuration(valDur As Double) As osKeyTime
        Return osKeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(valDur))
    End Function

    Private objExpandH As Double

    Private Sub ApplyExpanderSize()
        Dim objExpandVis = FetchExpandVisual()

        With osContentContainer
            .Height = Double.NaN
            .Measure(New Size(.ActualWidth, Double.PositiveInfinity))

            objExpandH = .DesiredSize.Height

            objExpandVis.KeyFrames.Add(
                New EasingDoubleKeyFrame() With {
                    .KeyTime = SetVisDuration(1600),
                    .Value = objExpandH,
                    .EasingFunction = New osPrefExpandEase() With {
                        .EasingMode = EasingMode.EaseIn
                    }})

            .Height = 0
        End With
    End Sub

    Private Sub ApplyCloserSize()
        Dim objCloseVis = FetchCloseVisual()

        objCloseVis.KeyFrames.Insert(0,
               New EasingDoubleKeyFrame() With {
                   .KeyTime = SetVisDuration(100),
                   .Value = objExpandH,
                   .EasingFunction = New QuadraticEase() With {.EasingMode = EasingMode.EaseInOut}
               })
    End Sub

    Public Sub ShowPrefsUICore()
        With Me
            osPrefsIU_Present()
            .Topmost = True
        End With

        visPrefUI_Open.Begin()
    End Sub

    Private Sub SetOpenEvents()
        evtComplete_Open =
            Sub()
                RemoveHandler visPrefUI_Open.Completed, evtComplete_Open

                visPrefUI_Open.Stop()
                visPrefUI_Open = Nothing

                SetVisualMode(PrefUI_Open)
            End Sub

        AddHandler visPrefUI_Open.Completed, evtComplete_Open
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
        InitVisual(PrefUI_Close)

        evtComplete_Close =
            Sub()
                RemoveHandler visPrefUI_Close.Completed,
                                                        evtComplete_Close
                Me.Close()
            End Sub

        osVisQualityAdapter.UpdateVisData(VisTypeAdapter.
                                          VisAdapter_Opts, visPrefUI_Close)

        AddHandler visPrefUI_Close.Completed,
                                            evtComplete_Close

        SetVisualMode(PrefUI_Close)
    End Sub

    Public Sub osPrefsIU_Present()
        Me.Show()

        Dim objHwnd = New WindowInteropHelper(Me).Handle

        SetWindowPos(objHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)

        Me.Topmost = True
    End Sub

    Private Sub osPrefsBtnClk_SavePrefs(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Save.Click
        If objOsPrefTracker.prefsChanged Then
            Dim chkDoSave = GetResponse(PromptType.Prefs_Save)

            If chkDoSave = isYes Then
                TriggerPrefSave()
            End If
        End If
    End Sub

    Private Sub DragMovePrefWin(sender As Object, e As MouseButtonEventArgs) Handles osPrefs_TitleBar.MouseLeftButtonDown
        If isMouseDown(e) Then
            DragMove()
        End If
    End Sub

    Private Sub TriggerPrefSave()
        isSaved = True
        osPrefDataIdx.SavePrefsFile()

        Dim objTask_ResetAutoCast = osHandler_UI.CloseAndResetAutoCast(True)
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

    Private Sub osPrefs_InitClose()
        SetCloseEvents()
        Dispatcher.CurrentDispatcher.
            BeginInvoke(DispatcherPriority.Render,
                        Sub()
                            visPrefUI_Close.Begin(Me, True)
                        End Sub)
    End Sub

    Private Sub osPrefs_InitClose(isN As Boolean)
        SetCloseEvents()
        objCloseMonitor.TrySetResult(True)
    End Sub

    Public Async Function osPrefs_InitCloseVis() As Task
        Await osVisQualityAdapter.EstablishVisDataSettings(VisTypeAdapter.VisAdapter_Opts, VisRenderMode.VisMode_LowQuality, True)
        visPrefUI_Close.Begin(prefContainer, True)
    End Function

    Private Sub osPrefsBtnClk_Close(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Close.Click
        Select Case GetPrefSaveState()
            Case Prefs_NoChanges
                osPrefs_InitClose(True)
            Case Prefs_Saved
                osPrefs_InitClose(True)
            Case Prefs_NotSaved
                Select Case GetResponse(PromptType.Prefs_Close)
                    Case isYes
                        TriggerPrefSave()
                        osPrefs_InitClose(True)
                    Case isNo Or isCancel
                        Exit Sub
                End Select
        End Select
    End Sub

End Class

Partial Public Class osPrefs_GUI

    Private objCloseMonitor As TaskCompletionSource(Of Boolean)

    Private evtComplete_Open As EventHandler
    Private evtComplete_Close As EventHandler

    Private isSaved As Boolean = False

    'Private objOsPrefTracker As osPrefTracker(Of osPrefData)
    'Private objOsPrefTracker As osPrefTracker(Of osPrefData)

    Private objOsPrefTracker As osPrefLib.osPrefMonitor(Of osPrefData)

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

    Public visPrefUI_Open As New Storyboard
    Public visPrefUI_Close As Storyboard = Nothing

    Private idxOsPrefVisuals As New Dictionary(Of PrefUI_State, String) From {
        {PrefUI_Open, "osPrefsVis_Disp"},
        {PrefUI_Close, "osPrefsVis_Close"}
    }

    Private ReadOnly Property osPrefRes As Style
        Get
            Return Me.Style
        End Get
    End Property

    Public ReadOnly Property prefContainer As osBorder
        Get
            Return Me.osPrefsMainContainer
        End Get
    End Property

    Public ReadOnly Property osContentContainer As StackPanel
        Get
            Return Me.BottomContainer
        End Get
    End Property

    Public ReadOnly Property osTitleCover As Border
        Get
            Return Me.osPrefsTitlePanel
        End Get
    End Property

    Private ReadOnly Property osPrefDataIdx As osPrefData.osPrefIndex
        Get
            Return osPrefData.Data.objOsPrefIdx
        End Get
    End Property

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub osPrefs_GUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ActivatePrefTracker()
    End Sub

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        CoreDataLib.SetWinOpts(CoreDataLib.GetWinHwnd(Me))
    End Sub

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer,
                                         Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

End Class


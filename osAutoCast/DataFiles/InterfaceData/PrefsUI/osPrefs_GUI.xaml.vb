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

    Private Function AllocVis(visObject As Object) As Storyboard
        Return TryCast(visObject, Storyboard)
    End Function

    Private Function FetchPrefVis(objVisResource As Style, objVisType As PrefUI_State) As Storyboard
        Return AllocVis(Me.Resources(GetVisualState(objVisType)))
    End Function

    Private Function SetVisual(objVisType As PrefUI_State) As Storyboard
        Return If(objVisType = PrefUI_Open,
            VisDataObject, visPrefUI_Close)
    End Function

    Private Sub EstablishVisual(objVisType As PrefUI_State, ByRef objVis As Storyboard)
        Dim objLoadVis = FetchPrefVis(osPrefRes, objVisType)
        objVis = objLoadVis
    End Sub

    Private Sub InitVisual(objVisType As PrefUI_State)
        Select Case objVisType
            Case PrefUI_Open
                EstablishVisual(PrefUI_Open, visPrefUI_Open)
                Me.VisDataObject = visPrefUI_Open

                '       Dim objConfigSettings = EstablishVisConfig()

                '   Dim aa = objVisAdapt.InitVisualAdapter()

                SetOpenEvents()
                BufferPrefWin()

            Case PrefUI_Close
                EstablishVisual(PrefUI_Close, visPrefUI_Close)
                Me.VisDataObject = visPrefUI_Close

                ApplyCloserSize()
        End Select
    End Sub

    Private Sub BufferPrefWin()
        Me.Show()
        Me.Hide()

    End Sub

    Private Function EstablishVisConfig() As VisAdapterConfig
        Return VisAdapterConfig.ObjectReset Or VisAdapterConfig.UpdateAsync_OnDispose Or VisAdapterConfig.UpdateAsync_OnReset
    End Function

    Public Sub PrepPrefVis()
        InitVisual(PrefUI_Open)
        ' Me.VisDataObject = visPrefUI_Open
        Dim objConfigSettings = EstablishVisConfig()
        Me.VisAdapter = New VisQualityAdapter(Me, objConfigSettings,
                                                   prefContainer, osTitleCover, osContentContainer)
        ApplyExpanderSize()
        '     objVisAdapt = New VisQualityAdapter(Me, EstablishVisConfig(), prefContainer, osTitleCover, osContentContainer)
    End Sub

    Public Overrides Property VisDataObject As Storyboard
        Get
            Return MyBase.VisDataObject
        End Get
        Set(value As Storyboard)
            MyBase.VisDataObject = value
        End Set
    End Property

    Public Sub DisplayPrefsUI(objAwaitClose As TaskCompletionSource(Of Boolean))
        objCloseMonitor = objAwaitClose
        Dim ba = TriggerVisuals_Open(False, True)
    End Sub

    Public Overrides Function TriggerVisuals_Open(Optional doAsync As Boolean = True, Optional objAwaitClose As TaskCompletionSource(Of Boolean) = Nothing) As Task
        objCloseMonitor = objAwaitClose
        Dim aaa = MyBase.TriggerVisuals_Open(doAsync)
    End Function

    Private Function TriggerDisplayPrefsUI() As Task
        VisDataObject.Begin(prefContainer, True)
    End Function

    Private Function FetchExpandVisual() As DoubleAnimationUsingKeyFrames
        Return CType(osVisDataArray_Open().First(
            Function(objVis)
                With VerifyVisData(objVis)
                    Return .VisTarget = "BottomContainer" AndAlso
                        .VisProperty = "Height"
                End With
            End Function), DoubleAnimationUsingKeyFrames)
    End Function

    Private Function FetchCloseVisual() As DoubleAnimationUsingKeyFrames
        Return CType(osVisDataArray_Close().First(
            Function(objVis)
                With VerifyVisData(objVis)
                    Return .VisTarget = "BottomContainer" AndAlso
                        .VisProperty = "Height"
                End With
            End Function), DoubleAnimationUsingKeyFrames)
    End Function

    Private Function VerifyVisData(objVisData As DoubleAnimationUsingKeyFrames) As VisDataDetails
        Return New VisDataDetails(objVisData)
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

        VisDataObject.Begin()
    End Sub

    Private Sub SetOpenEvents()
        evtComplete_Open =
            Sub()
                RemoveHandler VisDataObject.Completed, evtComplete_Open

                VisDataObject.Stop()
                '  visPrefUI_Open = Nothing

                SetVisualMode(PrefUI_Open)
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
        InitVisual(PrefUI_Close)

        evtComplete_Close =
            Sub()
                RemoveHandler VisDataObject.Completed,
                                                        evtComplete_Close
                Me.Close()
            End Sub

        'osVisQualityAdapter.UpdateVisData(VisTypeAdapter.
        '                                  VisAdapter_Opts, visPrefUI_Close)

        AddHandler VisDataObject.Completed,
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

    'Private Sub osPrefs_InitClose()
    '    SetCloseEvents()
    '    Dispatcher.CurrentDispatcher.
    '        BeginInvoke(DispatcherPriority.Render,
    '                    Sub()
    '                        visPrefUI_Close.Begin(Me, True)
    '                    End Sub)
    'End Sub

    'Private Function osPrefs_InitClose() As Task
    '    SetCloseEvents()
    '    objCloseMonitor.TrySetResult(True)
    'End Function

    Private Function osPrefs_InitClose() As Task
        '   Dim ba = TriggerVisuals_Close(False, True)
        SetCloseEvents()
        objCloseMonitor.TrySetResult(True)
    End Function

    Private Sub osPrefs_InitClose2()
        SetCloseEvents()
        objCloseMonitor.TrySetResult(True)
    End Sub

    Public Function osPrefs_InitCloseVis() As Task
        '   Await objVisAdapt.ApplyVisuals(True)
        '    visPrefUI_Close.Begin(prefContainer, True)
        '  Dim ba = TriggerVisuals_Close(False, True)
        '  Return Task.CompletedTask
    End Function

    Public Overrides Async Function TriggerVisuals_Close(Optional doAsync As Boolean = True) As Task
        Await MyBase.TriggerVisuals_Close(doAsync)
    End Function

    Private Sub osPrefsBtnClk_Close(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Close.Click
        Select Case GetPrefSaveState()
            Case Prefs_NoChanges
                osPrefs_InitClose2()'  osPrefs_InitClose()
            Case Prefs_Saved
                osPrefs_InitClose2()' osPrefs_InitClose()
            Case Prefs_NotSaved
                Select Case GetResponse(PromptType.Prefs_Close)
                    Case isYes
                        TriggerPrefSave()
                        osPrefs_InitClose2()'  osPrefs_InitClose()
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

    Private objOsPrefTracker As osPrefLib.osPrefMonitor(Of osPrefData)

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

    Public visPrefUI_Open As New Storyboard
    Public visPrefUI_Close As New Storyboard

    Private idxOsPrefVisuals As New Dictionary(Of PrefUI_State, String) From {
        {PrefUI_Open, "osPrefsVis_Disp"},
        {PrefUI_Close, "osPrefsVis_Close"}
    }

    Private VisQualityValidated As Boolean

    Public objVisAdapt As VisQualityAdapter

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

    Private ReadOnly Property osVisDataArray_Open As IEnumerable(Of DoubleAnimationUsingKeyFrames)
        Get
            Return VisDataObject.Children.OfType(Of DoubleAnimationUsingKeyFrames)
            'Return visPrefUI_Open.Children.OfType(Of DoubleAnimationUsingKeyFrames)
        End Get
    End Property

    Private ReadOnly Property osVisDataArray_Close As IEnumerable(Of DoubleAnimationUsingKeyFrames)
        Get
            Return VisDataObject.Children.OfType(Of DoubleAnimationUsingKeyFrames)
        End Get
    End Property

    'Public Overrides Property VisDataObject As Storyboard
    '    Get
    '        Return MyBase.VisDataObject
    '    End Get
    '    Set(value As Storyboard)
    '        MyBase.VisDataObject = value
    '    End Set
    'End Property

    Private Function CreatePrefMonitor() As osPrefLib.osPrefMonitor(Of osPrefData)
        Return New osPrefLib.osPrefMonitor(Of osPrefData)
    End Function

    Private Sub InitializePrefMonitor()
        objOsPrefTracker.Attach(DirectCast(Me.DataContext, osPrefData))
    End Sub

    Public Sub ActivatePrefTracker()
        objOsPrefTracker = CreatePrefMonitor()
        InitializePrefMonitor()
    End Sub

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


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

    Private Function AllocVis(visObject As Object) As Storyboard
        Return TryCast(visObject, Storyboard)
    End Function

    Public Function FetchPrefVis(objVisType As PrefUI_State) As Storyboard
        Return AllocVis(Me.Resources(GetVisualState(objVisType)))
    End Function

    Private Sub EstablishVisual(objVisType As PrefUI_State)
        Me.VisDataObject = FetchPrefVis(objVisType)
    End Sub

    Private Sub InitVisual(objVisType As PrefUI_State)
        Select Case objVisType
            Case PrefUI_Open
                '    EstablishVisual(PrefUI_Open)

                SetOpenEvents()
                BufferPrefWin()
            Case PrefUI_Close
                EstablishVisual(PrefUI_Close)
        End Select
    End Sub

    Private Sub BufferPrefWin()
        Me.Show()
        Me.Hide()
    End Sub

    Private Function EstablishVisConfig() As VisAdapterConfig
        Return VisAdapterConfig.EnableAll
    End Function

    Public Sub PrepPrefVis2()
        InitVisual(PrefUI_Open)
        ApplyExpanderSize()
    End Sub

    Public Async Function PrepPrefVis() As Task
        Dim visDataN = GetVisualState(PrefUI_Open)

        Await InitializeVisAdapter(visDataN, Me, EstablishVisConfig(), AddressOf PrepPrefVis2,
                                    prefContainer, osTitleCover, osContentContainer)
    End Function

    Public Sub SetCloseMonitor(objAwaitClose As TaskCompletionSource(Of Boolean))
        objCloseMonitor = objAwaitClose
    End Sub

    Private Function FetchExpandVisual() As DoubleKeyFrameCollection
        Return CType(osVisDataArray().First(
            Function(objVis)
                With VerifyVisData(objVis)
                    Return .VisTarget = "BottomContainer" AndAlso
                        .VisProperty = "Height"
                End With
            End Function).KeyFrames, DoubleKeyFrameCollection)
    End Function

    Private Function FetchCloseVisual() As DoubleKeyFrameCollection
        Return CType(osVisDataArray().First(
            Function(objVis)
                With VerifyVisData(objVis)
                    Return .VisTarget = "BottomContainer" AndAlso
                        .VisProperty = "Height"
                End With
            End Function).KeyFrames, DoubleKeyFrameCollection)
    End Function

    Private Function VerifyVisData(objVisData As DoubleAnimationUsingKeyFrames) As VisDataDetails
        Return New VisDataDetails(objVisData)
    End Function

    Private Function SetVisDuration(valDur As Double) As osKeyTime
        Return osKeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(valDur))
    End Function

    Private Sub ApplyExpanderSize()
        With FetchExpandVisual()
            .Add(ComposeVisObjectKeyFrame())
        End With
    End Sub

    Private Sub ApplyCloserSize(objCloseVisData As Storyboard)
        With FetchCloseVisual()
            .Insert(0, ComposeVisObjectKeyFrame(False))
        End With
    End Sub

    Private Function ComposeVisObjectKeyFrame(Optional isOpenVis As Boolean = True) As EasingDoubleKeyFrame
        If isOpenVis Then
            With osContentContainer
                .Height = Double.NaN
                .Measure(New Size(.ActualWidth, Double.PositiveInfinity))

                objExpandH = .DesiredSize.Height
                .Height = 0

                Return New EasingDoubleKeyFrame() With {
                    .KeyTime = SetVisDuration(1600),
                    .Value = objExpandH,
                    .EasingFunction = New osPrefExpandEase() With {
                        .EasingMode = EasingMode.EaseIn
                    }}
            End With
        Else
            Return New EasingDoubleKeyFrame() With {
                    .KeyTime = SetVisDuration(100),
                    .Value = objExpandH,
                    .EasingFunction = New QuadraticEase() With {
                        .EasingMode = EasingMode.EaseIn
                    }}
        End If
    End Function

    Private Sub SetOpenEvents()
        evtComplete_Open =
            Sub()
                RemoveHandler VisDataObject.Completed, evtComplete_Open
                '  VisDataObject.Stop()

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

    Private Sub TriggerPrefSave()
        isSaved = True
        osPrefDataIdx.SavePrefsFile()

        Dim objTask_ResetAutoCast = osHandler_UI.CloseAndResetAutoCast()
        _VisQualitySetting = "n/a"
        OnPropertyChanged(NameOf(VisQualitySetting))
    End Sub

    Private Sub osPrefs_InitClose()
        SetCloseEvents()
        objCloseMonitor.TrySetResult(True)
    End Sub

    Private Sub osPrefsBtnClk_Close(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Close.Click
        Select Case GetPrefSaveState()
            Case Prefs_NoChanges
                osPrefs_InitClose()
            Case Prefs_Saved
                osPrefs_InitClose()
            Case Prefs_NotSaved
                Select Case GetResponse(PromptType.Prefs_Close)
                    Case isYes
                        TriggerPrefSave()
                        osPrefs_InitClose()
                    Case isNo Or isCancel
                        Exit Sub
                End Select
        End Select
    End Sub

End Class

Partial Public Class osPrefs_GUI
    Implements INotifyPropertyChanged


    Private objCloseMonitor As TaskCompletionSource(Of Boolean)

    Private evtComplete_Open As EventHandler
    Private evtComplete_Close As EventHandler

    Private isSaved As Boolean = False

    Private objExpandH As Double

    Private objOsPrefTracker As osPrefLib.osPrefMonitor(Of osPrefData)

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

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

    Public Sub ActivatePrefTracker()
        objOsPrefTracker = CreatePrefMonitor()
        InitializePrefMonitor()
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
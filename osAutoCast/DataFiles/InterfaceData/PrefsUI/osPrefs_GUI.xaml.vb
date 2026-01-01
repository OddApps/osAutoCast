Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.PrefUI_State
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.DataTypeLib.PrefSaveState
Imports osAutoCast.osStyle
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

    Private Function FetchPrefVis(objVisResource As Style, objVisType As PrefUI_State) As Storyboard
        Return TryCast(objVisResource.
            Resources(GetVisualState(objVisType)), Storyboard)
    End Function

    Private Function EstablishVisual(objVisType As PrefUI_State) As Storyboard
        Dim objLoadVis = FetchPrefVis(osPrefRes, objVisType)
        Return objLoadVis.Clone()
    End Function

    Private Sub BufferPrefWin()
        Me.Show()
        Me.Hide()
    End Sub

    Public Sub PrepPrefVis()
        visPrefUI_Open = EstablishVisual(PrefUI_Open)
        visPrefUI_Close = EstablishVisual(PrefUI_Close)

        objOsPrefTracker = New osPrefTracker(Of osPrefData)(osPrefData.Data)
        BufferPrefWin()

        Dim objExpandVis = FetchExpandVisual()

        osContentContainer.Height = Double.NaN
        osContentContainer.Measure(New Size(osContentContainer.ActualWidth, Double.PositiveInfinity))

        objExpandVis.To = osContentContainer.DesiredSize.Height
        osContentContainer.Height = 0
    End Sub

    Public Sub DisplayPrefsUI()
        If PrepDispatcher().CheckAccess() Then
            ShowPrefsUICore()
        Else
            PrepDispatcher().Invoke(
                AddressOf ShowPrefsUICore,
                DispatcherPriority.Background)
        End If
    End Sub

    Private Function FetchExpandVisual() As DoubleAnimation
        Return CType(visPrefUI_Open.Children.First(
            Function(objVis)
                Return objVis.Name = "osPrefExpandVis"
            End Function), DoubleAnimation)
    End Function

    Public Sub ShowPrefsUICore()
        With Me
            osPrefsIU_Present()
            .Topmost = True
        End With

        AddHandler visPrefUI_Open.Completed, Sub()
                                                 osTitleCover.Visibility = Visibility.Collapsed
                                                 SetVisualMode(PrefUI_Open)
                                             End Sub

        visPrefUI_Open.Begin(prefContainer)
    End Sub

    Private Sub SetCloseEvents()
        evtCloseCompleteEvent =
            Sub()
                RemoveHandler visPrefUI_Close.Completed,
                                                        evtCloseCompleteEvent
                Me.Close()
            End Sub

        AddHandler visPrefUI_Close.Completed,
                                            evtCloseCompleteEvent

        SetVisualMode(PrefUI_Close)
        osTitleCover.Visibility = Visibility.Visible
    End Sub

    Private Sub SetVisualMode(objAniType As PrefUI_State)
        Dim setBitMapMode As BitmapScalingMode
        Dim setCacheMode As CacheMode

        Select Case objAniType
            Case PrefUI_Open
                setBitMapMode = BitmapScalingMode.HighQuality
                setCacheMode = Nothing
            Case PrefUI_Close
                setBitMapMode = BitmapScalingMode.LowQuality
                setCacheMode = New BitmapCache()
        End Select

        prefContainer.CacheMode = setCacheMode
        RenderOptions.SetBitmapScalingMode(prefContainer, setBitMapMode)

        osContentContainer.CacheMode = setCacheMode
        RenderOptions.SetBitmapScalingMode(osContentContainer, setBitMapMode)
    End Sub

    Public Sub osPrefsIU_Present()
        Me.Show()

        Dim objHwnd = New WindowInteropHelper(Me).Handle

        SetWindowPos(objHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
    End Sub

    Private Sub osPrefs_GUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.DataContext = osPrefData.Data
    End Sub

    Private Sub osPrefsBtnClk_SavePrefs(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Save.Click
        If objOsPrefTracker.HasChanges Then
            Dim chkDoSave = GetResponse(PromptType.Prefs_Save)

            If chkDoSave = isYes Then
                osPrefDataIdx.SavePrefsFile()
                objOsPrefTracker.HasChanges()

                isSaved = True
            Else
                objOsPrefTracker.Revert()
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
    End Sub

    Private Function isMouseDown(e As MouseButtonEventArgs) As Boolean
        Return e.LeftButton = MouseButtonState.Pressed
    End Function

    Private Function HasPrefChanges() As Boolean
        Return objOsPrefTracker.HasChanges
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
        visPrefUI_Close.Begin(Me, True)
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
                    Case isNo
                        objOsPrefTracker.Revert()
                    Case isCancel
                        Exit Sub
                End Select
        End Select
    End Sub

End Class

Partial Public Class osPrefs_GUI

    Private evtCloseCompleteEvent As EventHandler

    Private isSaved As Boolean = False
    Private objOsPrefTracker As osPrefTracker(Of osPrefData)

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

    Private visPrefUI_Open As Storyboard = Nothing
    Private visPrefUI_Close As Storyboard = Nothing

    Private idxOsPrefVisuals As New Dictionary(Of PrefUI_State, String) From {
        {PrefUI_Open, "osPrefsVis_Disp"},
        {PrefUI_Close, "osPrefsVis_Close"}
    }

    Private ReadOnly Property osPrefRes As Style
        Get
            Return Me.Style
        End Get
    End Property

    Private ReadOnly Property prefContainer As osBorder
        Get
            Return Me.osPrefsMainContainer
        End Get
    End Property

    Private ReadOnly Property osContentContainer As StackPanel
        Get
            Return Me.BottomContainer
        End Get
    End Property

    Private ReadOnly Property osTitleCover As Border
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

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        CoreDataLib.SetWinOpts(CoreDataLib.GetWinHwnd(Me))
    End Sub

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer,
                                         Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

End Class


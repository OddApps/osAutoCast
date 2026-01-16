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

    'Private Function EstablishVisual(objVisType As PrefUI_State) As Storyboard
    '    Dim objLoadVis = FetchPrefVis(osPrefRes, objVisType)
    '    Return objLoadVis.Clone()
    'End Function

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
        End Select
    End Sub

    Private Sub BufferPrefWin()
        Me.Show()
        Me.Hide()
    End Sub

    Public Sub ActivatePrefTracker()
        objOsPrefTracker = New osPrefTracker(Of osPrefData)(osPrefData.Data)
    End Sub

    Public Sub PrepPrefVis()
        InitVisual(PrefUI_Open)
        '  ActivatePrefTracker()
    End Sub

    Public Sub DisplayPrefsUI()
        Dim a = PrepDispatcher().BeginInvoke(DispatcherPriority.Render,
            Sub() ShowPrefsUICore())
    End Sub

    Public Function DisplayPrefsUI(isN As Boolean) As Task
        Return PrepDispatcher().BeginInvoke(DispatcherPriority.Render,
            Sub() ShowPrefsUICore()).Task
    End Function

    Private Function FetchExpandVisual() As DoubleAnimation
        Return CType(visPrefUI_Open.Children.First(
            Function(objVis)
                Return objVis.Name = "osPrefExpandVis"
            End Function), DoubleAnimation)
    End Function

    Private Sub ApplyExpanderSize()
        Dim objExpandVis = FetchExpandVisual()

        With osContentContainer
            .Height = Double.NaN
            .Measure(New Size(.ActualWidth, Double.PositiveInfinity))

            objExpandVis.To = .DesiredSize.Height
            .Height = 0
        End With
    End Sub

    Public Sub ShowPrefsUICore()
        With Me
            osPrefsIU_Present()
            .Topmost = True
        End With

        visPrefUI_Open.Begin(prefContainer)
    End Sub

    Private Sub SetOpenEvents()
        evtComplete_Open =
                    Sub()
                        RemoveHandler visPrefUI_Open.Completed,
                                                                evtComplete_Open


                        SetVisualMode(PrefUI_Open)

                        visPrefUI_Open.Stop()
                        visPrefUI_Open = Nothing
                    End Sub

        AddHandler visPrefUI_Open.Completed,
                                            evtComplete_Open
    End Sub

    Private Sub SetCloseEvents()
        InitVisual(PrefUI_Close)

        evtComplete_Close =
            Sub()
                RemoveHandler visPrefUI_Close.Completed,
                                                        evtComplete_Close
                Me.Close()
            End Sub

        AddHandler visPrefUI_Close.Completed,
                                            evtComplete_Close

        SetVisualMode(PrefUI_Close)
    End Sub

    Private Sub SetVisualQuality(objVMode As VisRenderMode)
        With New osVisRenderMode(objVMode)
            prefContainer.CacheMode = .visCache
            osContentContainer.CacheMode = .visCache

            RenderOptions.SetBitmapScalingMode(osContentContainer, .visBitMap)
            RenderOptions.SetBitmapScalingMode(prefContainer, .visBitMap)
        End With
    End Sub

    Private Sub SetVisualMode(valPrefState As PrefUI_State)
        Select Case valPrefState
            Case PrefUI_Open
                osTitleCover.Visibility = Visibility.Collapsed
                RenderOptions.SetEdgeMode(prefContainer, EdgeMode.Unspecified)

                SetVisualQuality(VisMode_Open)
            Case PrefUI_Close
                osTitleCover.Visibility = Visibility.Visible
                RenderOptions.SetEdgeMode(prefContainer, EdgeMode.Aliased)

                SetVisualQuality(VisMode_Close)
        End Select
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

    Private evtComplete_Open As EventHandler
    Private evtComplete_Close As EventHandler

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


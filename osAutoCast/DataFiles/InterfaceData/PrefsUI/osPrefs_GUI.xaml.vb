Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.PrefUI_State
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.osStyle
Imports osPrefData = osAutoCast.osPrefLib.osPreferenceLib

#Disable Warning BC42353
#Disable Warning BC42104

Public Class osPrefs_GUI

    Private visPrefUI_Open As Storyboard = Nothing
    Private visPrefUI_Close As Storyboard = Nothing

    Private idxOsPrefVisuals As New Dictionary(Of PrefUI_State, String) From {
        {PrefUI_Open, "osPrefsVis_Disp"},
        {PrefUI_Close, "osPrefsVis_Close"}
    }

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

    Public Sub PrepPrefVis()
        objOsPrefTracker = New osPrefTracker(Of osPrefData)(osPrefData.Data)

        Me.Show()
        Me.Hide()

        visPrefUI_Open = EstablishVisual(PrefUI_Open)
        visPrefUI_Close = EstablishVisual(PrefUI_Close)
    End Sub

    Public Async Function PrepPrefVis(isAsync As Boolean) As Task
        Await PrepDispatcher().InvokeAsync(
                            Sub()
                                objOsPrefTracker = New osPrefTracker(Of osPrefData)(osPrefData.Data)

                                visPrefUI_Open = EstablishVisual(PrefUI_Open)
                                visPrefUI_Close = EstablishVisual(PrefUI_Close)
                            End Sub, DispatcherPriority.Background)
    End Function

    Public Sub DisplayPrefsUI()
        If osHandler_UI.osPrefsWindow.Dispatcher.CheckAccess() Then
            Dim a = ShowPrefsUICore()
        Else
            osHandler_UI.osPrefsWindow.Dispatcher.Invoke(
                AddressOf ShowPrefsUICore,
                DispatcherPriority.Render)
        End If
    End Sub

    Private Function FetchExpandVisual() As DoubleAnimation
        Return CType(visPrefUI_Open.Children.First(
            Function(objVis)
                Return objVis.Name = "osPrefExpandVis"
            End Function), DoubleAnimation)
    End Function

    Public Async Function ShowPrefsUICore() As Task
        Await PrepDispatcher().BeginInvoke(
            Sub()
                With Me
                    osPrefsIU_Present()
                    .Topmost = True
                End With

                Dim objExpandVis = FetchExpandVisual()

                osContentContainer.Height = Double.NaN
                osContentContainer.Measure(New Size(osContentContainer.ActualWidth, Double.PositiveInfinity))

                objExpandVis.To = osContentContainer.DesiredSize.Height
                osContentContainer.Height = 0


                AddHandler visPrefUI_Open.Completed, Sub()
                                                         osTitleCover.Visibility = Visibility.Collapsed
                                                         SetVisualMode(PrefUI_Open)
                                                     End Sub

                visPrefUI_Open.Begin(Me)
            End Sub, DispatcherPriority.Render)
    End Function

    Private Sub HoldTask() : End Sub

    Public Sub ClosePrefsUI()
        If osHandler_UI.osPrefsWindow.Dispatcher.CheckAccess() Then
            ClosePrefsUICore()
        Else
            osHandler_UI.osPrefsWindow.Dispatcher.Invoke(
                AddressOf ClosePrefsUICore,
                DispatcherPriority.Background)
        End If
    End Sub

    Private evtCloseCompleteEvent As EventHandler

    Private Sub ClosePrefsUICore()
        evtCloseCompleteEvent = Sub()
                                    RemoveHandler visPrefUI_Close.Completed,
                                                                            evtCloseCompleteEvent
                                    Me.Close()
                                End Sub

        AddHandler visPrefUI_Close.Completed,
                                            evtCloseCompleteEvent

        SetVisualMode(PrefUI_Close)
        osTitleCover.Visibility = Visibility.Visible

        visPrefUI_Close.Begin(Me, True)
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

    Private Sub ClosePrefs(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If objOsPrefTracker.HasChanges Then
            If Not isSaved Then
                Select Case GetResponse(PromptType.Prefs_Close)
                    Case isYes
                        osPrefData.Data.objOsPrefIdx.SavePrefsFile()
                    Case isNo
                        objOsPrefTracker.Revert()
                    Case isCancel
                        e.Cancel = True
                End Select
            End If
        End If
    End Sub

    Private Sub SavePrefsOnClk(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Save.Click
        If objOsPrefTracker.HasChanges Then
            Dim chkDoSave = GetResponse(PromptType.Prefs_Save)

            If chkDoSave = isYes Then
                osPrefData.Data.objOsPrefIdx.SavePrefsFile()
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

    Private Function isMouseDown(e As MouseButtonEventArgs) As Boolean
        Return e.LeftButton = MouseButtonState.Pressed
    End Function

    Private Sub osPrefsBtn_Close_Click(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Close.Click
        ClosePrefsUI()
    End Sub

End Class

Partial Public Class osPrefs_GUI

    Private isSaved As Boolean = False
    Private objOsPrefTracker As osPrefTracker(Of osPrefData)

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

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

    Private ReadOnly Property osTitleCover As StackPanel
        Get
            Return Me.osPrefsTitlePanel
        End Get
    End Property

    Public Sub New()
        InitializeComponent()

        PrepPrefVis()
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


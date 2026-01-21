Imports System.Windows.Forms
Imports System.Drawing
Imports osHorz = System.Windows.HorizontalAlignment
Imports osVert = System.Windows.VerticalAlignment
Imports osAutoCast.DataTypeLib.OverlayVisualType
Imports osAutoCast.DataTypeLib.AnimationType
Imports System.Runtime.InteropServices
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osBrushColor = System.Windows.Media.Brushes

Public Class osPopupMenuOverlay_GUI

    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80

    <DllImport("user32.dll", EntryPoint:="GetWindowLongPtrW", SetLastError:=True)>
    Private Shared Function GetWindowLongPtr(hWnd As IntPtr, nIndex As Integer) As IntPtr
    End Function

    <DllImport("user32.dll", EntryPoint:="SetWindowLongPtrW", SetLastError:=True)>
    Private Shared Function SetWindowLongPtr(hWnd As IntPtr, nIndex As Integer, dwNewLong As IntPtr) As IntPtr
    End Function

    Private objTask_Open As TaskCompletionSource(Of Boolean)
    Private objTask_Close As TaskCompletionSource(Of Boolean)

    Private objAnimation_Open As Storyboard = Nothing
    Private objAnimation_Close As Storyboard = Nothing

    Private OpenCompleteEvent As EventHandler = AddressOf OpenComplete

    Private OverlayOpacity_Tray As Double = 0
    Private OverlayOpacity_Popup As Double = 0

    Private VisualDataLocation As String = "/DataFiles/VisualData/StyleLib/StyleResources/StyleContent/uiStyleContent-Overlay.xaml"
    Private VisualDataURI As System.Uri = New System.Uri(VisualDataLocation, System.UriKind.Relative)

    Private VisualDataLocation2 As String = "/DataFiles/VisualData/StyleLib/StyleResources/StyleConfigs/osUI_StyleVisuals.xaml"
    Private VisualDataURI2 As System.Uri = New System.Uri(VisualDataLocation, System.UriKind.Relative)

    Private objScreenData As Rectangle = SystemInformation.VirtualScreen

    Private idxOverlayVisuals As New Dictionary(Of OverlayVisualType, String) From {
        {OverlayVisual_Open, "OverlayVisual_Open"},
        {OverlayVisual_Close, "OverlayVisual_Close"},
        {OverlayVisual_CloseByBtn, "OverlayVisual_CloseByBtn"},
        {OverlayVisual_CloseByCmd, "OverlayVisual_CloseByCmd"}
    }

    Public hasClosedClicked As Boolean = False

    Public Sub InitPopupMenuOverlay(objOverlayDisplay As TaskCompletionSource(Of Boolean))
        With Me
            objAnimation_Open = EstablishVisual(osOverlay, OverlayVisual_Open)

            objTask_Open = objOverlayDisplay

            AddHandler objAnimation_Open.Completed,
                    OpenCompleteEvent

            .Show()
            objAnimation_Open.Begin(osOverlay, True)
        End With
    End Sub

    Public Async Function InitOverlayOpen(objVisType As OverlayVisualType) As Task
        InitTransitionVisuals(aniOpen, objVisType)
        Await objTask_Open.Task
    End Function

    Public Sub PrepTrayMenuOverlay()
        With Me
            .WindowStyle = WindowStyle.None
            .AllowsTransparency = True
            .ShowInTaskbar = False
            .ShowActivated = False
            .Topmost = True
            .Focusable = False

            .SetBG()
        End With
    End Sub

    Public Sub PrepPopupMenuOverlay()
        objAnimation_Open = EstablishVisual(osOverlay, OverlayVisual_Open)
    End Sub

    Private Sub OpenComplete()
        Try
            RemoveHandler objAnimation_Open.Completed,
                OpenCompleteEvent
        Catch : End Try

        objAnimation_Open = Nothing
        OverlayOpenComplete(objTask_Open)
    End Sub

    Private Sub BeginClosingTask(ByRef objCloseResult As TaskCompletionSource(Of Boolean))
        objCloseResult.ResetAndInitTask()
    End Sub

    Private Sub BeginOpenTask(ByRef objOpenResult As TaskCompletionSource(Of Boolean))
        objOpenResult.ResetAndInitTask()
    End Sub

    Private Sub OverlayOpenComplete(ByRef objTask As TaskCompletionSource(Of Boolean))
        objTask.TrySetResult(True)
    End Sub

    Private Sub OverlayCloseComplete(ByRef objTask As TaskCompletionSource(Of Boolean))
        objTask.TrySetResult(True)
    End Sub

    Private Sub PrepTransitionVisuals(objAniType As AnimationType, objVisType As OverlayVisualType)
        Select Case objAniType
            Case AnimationType.aniOpen
                objAnimation_Open = EstablishVisual(osOverlay, objVisType)

                AddHandler objAnimation_Open.Completed,
                    OpenCompleteEvent
            Case aniClose
                objAnimation_Close = EstablishVisual(osOverlay, objVisType)

                AddHandler objAnimation_Close.Completed, AddressOf osHandler_UI.CloseAndRestorePopupMenu
        End Select
    End Sub

    Private Function EstablishVisual(objContainer As FrameworkElement, objVisType As OverlayVisualType) As Storyboard
        Dim objOverlayVis = TryCast(Me.Resources(GetVisualKey(objVisType)), Storyboard)
        Timeline.SetDesiredFrameRate(objOverlayVis, 45)

        Return objOverlayVis
    End Function

    Private Function GetVisualKey(objVisType As OverlayVisualType) As String
        Return idxOverlayVisuals.First(Function(visKey)
                                           Return visKey.Key = objVisType
                                       End Function).Value
    End Function

    Public Sub InitTransitionVisuals(objAniType As AnimationType, objVisType As OverlayVisualType)
        PrepTransitionVisuals(objAniType, objVisType)

        Select Case objAniType
            Case aniOpen
              '  TriggerVisuals(Me, objAnimation_Open)
            Case aniClose
                '   SetVisualMode(aniClose)
                '   TriggerVisuals(Me, objAnimation_Close)
        End Select
    End Sub

    Public Sub InitOverlayClose(objVisType As OverlayVisualType, isN As Boolean)
        InitTransitionVisuals(aniClose, objVisType)

        objAnimation_Close.Begin()
    End Sub

End Class

Partial Public Class osPopupMenuOverlay_GUI

    Private isFromTray As Boolean

    Public ReadOnly Property popupOvBoundWidth As Double
        Get
            With Forms.SystemInformation.VirtualScreen
                Return .Width
            End With
        End Get
    End Property

    Public ReadOnly Property popupOvBoundHeight As Double
        Get
            With Forms.SystemInformation.VirtualScreen
                Return .Height
            End With
        End Get
    End Property

    Public ReadOnly Property osOverlay As Shapes.Rectangle
        Get
            Return Me.OverlayBackground
        End Get
    End Property

    Public Sub New(Optional isTray As Boolean = False)
        InitializeComponent()

        With Me
            .isFromTray = isTray
        End With
    End Sub

    Public Sub SetBG()
        With Me
            .Background = osBrushColor.Transparent
            .Opacity = 1.0
        End With
    End Sub

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)

        Dim hwnd = New Interop.WindowInteropHelper(Me).Handle
        Dim ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64()

        ex = ex Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, New IntPtr(ex))
    End Sub

End Class
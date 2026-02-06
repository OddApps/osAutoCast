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
Imports System.ComponentModel

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

    Private OpenCompleteEvent As EventHandler

    Private OverlayOpacity_Tray As Double = 0
    Private OverlayOpacity_Popup As Double = 0

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
            objTask_Open = objOverlayDisplay
            .Show()
        End With
    End Sub

    'Public Sub PrepTrayMenuOverlay()
    '    With Me
    '        .WindowStyle = WindowStyle.None
    '        .AllowsTransparency = True
    '        .ShowInTaskbar = False
    '        .ShowActivated = False
    '        .Topmost = True
    '        .Focusable = False
    '    End With
    'End Sub

    Public Async Function PrepPopupMenuOverlay() As Task
        Dim visDataN = GetVisualKey(OverlayVisual_Open)
        Await InitializeVisAdapter(visDataN, Me, EstablishVisConfig(),
                                    AddressOf SetOpenEvents, osOverlay)
    End Function

    Private Function EstablishVisConfig() As VisAdapterConfig
        Return VisAdapterConfig.EnableAll
        '    Dim objVisConfig = VisAdapterConfig.EnableAll
        '    objVisConfig = objVisConfig And Not VisAdapterConfig.ResetVisualSettings

        '    Return objVisConfig
    End Function

    Private Sub SetOpenEvents()
        OpenCompleteEvent =
          Async Sub()
              RemoveHandler Me.VisDataObject.Completed, OpenCompleteEvent

              Await Task.Delay(85)
              OverlayOpenComplete(objTask_Open)
          End Sub

        AddHandler VisDataObject.Completed, OpenCompleteEvent
    End Sub

    Private Sub OverlayOpenComplete(ByRef objTask As TaskCompletionSource(Of Boolean))
        objTask.TrySetResult(True)
    End Sub

    Private Function GetVisualKey(objVisType As OverlayVisualType) As String
        Return idxOverlayVisuals.First(
            Function(visKey)
                Return visKey.Key = objVisType
            End Function).Value
    End Function

    Public Async Function InitOverlayClose(objVisType As OverlayVisualType) As Task
        Await SetCloseVisualData_WithEvent(GetVisualKey(objVisType),
                                           AddressOf osHandler_UI.CloseAndRestorePopupMenu)

        'Await Me.TriggerVisuals_Close()
    End Function

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

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)

        Dim hwnd = New Interop.WindowInteropHelper(Me).Handle
        Dim ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64()

        ex = ex Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, New IntPtr(ex))
    End Sub

End Class
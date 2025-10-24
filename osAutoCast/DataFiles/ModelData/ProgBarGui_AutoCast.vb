Imports System
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports SharpDX
Imports SharpDX.Direct3D
Imports SharpDX.Direct3D11
Imports SharpDX.DXGI
Imports SharpDX.Direct2D1
Imports SharpDX.DirectWrite
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports osFeatureLevel = SharpDX.Direct3D.FeatureLevel
Imports osFactoryType = SharpDX.Direct2D1.FactoryType
Imports osProgDeviceContext = SharpDX.Direct3D11.DeviceContext
Imports FactoryD2D = SharpDX.Direct2D1.Factory
Imports FactoryDW = SharpDX.DirectWrite.Factory
Imports FactoryDXGI = SharpDX.DXGI.Factory
Imports AlphaMode = SharpDX.Direct2D1.AlphaMode
Imports D2DPixelFormat = SharpDX.Direct2D1.PixelFormat
Imports MapFlags = SharpDX.Direct3D11.MapFlags
Imports osAutoCast.DataTypeLib.ProgStatus
Imports osRect = SharpDX.Mathematics.Interop
Imports osDraw = System.Drawing
Imports osText = SharpDX.DirectWrite
Imports osColor = System.Windows.Media
Imports osForms = System.Windows.Forms

Public Class ProgBarGui_AutoCast

    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80
    Private Const WS_EX_TRANSPARENT As Integer = &H20
    Private Const WS_EX_TOPMOST As Integer = &H8

    Private _device As osProgDevice
    Private _context As osProgDeviceContext
    Private _swapChain As SwapChain
    Private _rtv As RenderTargetView
    Private _dxgiFactory As FactoryDXGI

    Private _d2dFactory As FactoryD2D
    Private _dwriteFactory As FactoryDW
    Private _d2dTarget As RenderTarget
    Private _brushBG As SolidColorBrush
    Private _brushBar As SolidColorBrush
    Private _brushText As SolidColorBrush
    Private _textFormat As TextFormat

    Private _durationMs As Double

    Public Event ProgressSuccess As EventHandler
    Public Event ProgressFail As EventHandler

    Private ProgressText As ProgressMsg
    Private ProgressDuration As TimeSpan
    Private ProgressTimer As osForms.Timer
    Private ProgressStartTime As Long
    Private ProgressTaskSrc As TaskCompletionSource(Of Boolean)
    Private ProgressValue As Double = 0
    Private ProgressEaseFunc As Func(Of Double, Double) = AddressOf EaseInOutSine
    Private ProgressStatus As ProgStatus


    Private _cts As CancellationTokenSource

    Private _textToShow As String = Nothing

    Private _textUntilTicks As Long = 0

    Public Property BarCornerRadius As Single = 0.0F

    ' Public colors (ARGB 0..255)
    Public Property BackColorARGB As osRect.RawColor4 = New osRect.RawColor4(57 / 255.0F, 57 / 255.0F, 57 / 255.0F, 1.0F)
    Public Property BarColorARGB As osRect.RawColor4 '= New osRect.RawColor4(82 / 255.0F, 96 / 255.0F, 117 / 255.0F, 1.0F)
    Public Property TextColorARGB As osRect.RawColor4 = New osRect.RawColor4(0.0F, 0.0F, 0.0F, 1.0F)

    Private _primed As Boolean = False
    Private _lastFillPx As Integer = 0
    Private _lastTrack As osRect.RawRectangleF

    Private _displayProgressText As Boolean = False
    Public Property DisplayProgressText As Boolean
        Get
            Return _displayProgressText
        End Get
        Set(value As Boolean)
            _displayProgressText = value
        End Set
    End Property

    Private _progressH As Integer
    Public Property ProgressHeight As Integer
        Get
            Return _progressH
        End Get
        Set(value As Integer)
            _progressH = value
        End Set
    End Property

    Private _progressW As Integer
    Public Property ProgressWidth As Integer
        Get
            Return _progressW
        End Get
        Set(value As Integer)
            _progressW = value
        End Set
    End Property

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        TopMost = True
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        If _swapChain IsNot Nothing AndAlso ProgressWidth > 0 AndAlso ProgressHeight > 0 Then
            ResizeSwapChain(ProgressWidth, ProgressHeight)
            DrawBG()
        End If

        '     _primed = False
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)

        Try
            _cts?.Cancel()
        Catch ex As Exception

        End Try

        DisposeAll()
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        InitDeviceAndSwapChain()
        CreateSizeDependentResources(ProgressWidth, ProgressHeight)

        ProgressStatus = ProgStatus.Idle
    End Sub

    'Public Sub BeginPrep()
    '    InitDeviceAndSwapChain()
    '    CreateSizeDependentResources(ProgressWidth, ProgressHeight)

    '    ProgressStatus = ProgStatus.Idle
    'End Sub

    Public Sub PerformProgressEvent(doEvent As ProgressEventData)
        Select Case doEvent.evType
            Case ProgEvent.Reset
                'ResetProgress(doEvent.evTrigger)
            Case ProgEvent.MaxFill
              '  DisplayMaxVal()
            Case ProgEvent.DispMsg
                DisplayMsg(doEvent.evDispMsg, doEvent.evTrigger)
            Case ProgEvent.DispMsg_AC
                DisplayMsg(doEvent.evDispMsg, doEvent.evTrigger)
            Case ProgEvent.ClrMsg
                ClearMsg()
        End Select
    End Sub

    Private Sub InitDeviceAndSwapChain()
        If _device IsNot Nothing Then Return
        Dim flags = DeviceCreationFlags.BgraSupport ' + nothing else
        ' If you kept it conditionally, change to a safe try/fallback:

        _device = New osProgDevice(DriverType.Hardware, flags)

        _context = _device.ImmediateContext ' optional; only if you use D3D11 draws

        Dim dxgiDevice = _device.QueryInterface(Of SharpDX.DXGI.Device)()
        Dim dxgiAdapter = dxgiDevice.Adapter

        _dxgiFactory = dxgiAdapter.GetParent(Of SharpDX.DXGI.Factory)()

        Dim mode = New ModeDescription(ProgressWidth, ProgressHeight,
                                       New Rational(60, 1), SharpDX.DXGI.Format.B8G8R8A8_UNorm)

        Dim scDesc = New SwapChainDescription() With {
            .BufferCount = 2,
            .ModeDescription = mode,
            .IsWindowed = True,
            .OutputHandle = Me.Handle,
            .SampleDescription = New SampleDescription(1, 0),
            .SwapEffect = SwapEffect.Discard,     ' broad compatibility on .NET 4.8 / DXGI 1.1
            .Usage = Usage.RenderTargetOutput
        }

        _swapChain = New SwapChain(_dxgiFactory, _device, scDesc)

        dxgiDevice.Dispose()
        dxgiAdapter.Dispose()

        ' Direct2D / DirectWrite
        _d2dFactory = New FactoryD2D(osFactoryType.SingleThreaded, DebugLevel.None)
        _dwriteFactory = New FactoryDW(SharpDX.DirectWrite.FactoryType.Shared)

        CreateTargetResources()
        DrawBG()
    End Sub

    Private Sub CreateTargetResources()
        SafeDispose(_rtv)
        SafeDispose(_d2dTarget)
        SafeDispose(_brushBG)
        SafeDispose(_brushBar)
        SafeDispose(_brushText)
        SafeDispose(_textFormat)

        Using backBuffer As Texture2D = _swapChain.GetBackBuffer(Of Texture2D)(0)
            _rtv = New RenderTargetView(_device, backBuffer)

            ' Create a Direct2D RenderTarget over the swapchain's DXGI surface
            Using dxgiSurface As Surface = backBuffer.QueryInterface(Of Surface)()
                Dim props = New RenderTargetProperties(RenderTargetType.Default,
                    New D2DPixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Ignore),
                    96.0F, 96.0F, RenderTargetUsage.None, Direct2D1.FeatureLevel.Level_DEFAULT)
                _d2dTarget = New RenderTarget(_d2dFactory, dxgiSurface, props)
            End Using
        End Using

        _brushBG = New SolidColorBrush(_d2dTarget, BackColorARGB)
        _brushBar = New SolidColorBrush(_d2dTarget, BarColorARGB)
        _brushText = New SolidColorBrush(_d2dTarget, TextColorARGB)
        _textFormat = New TextFormat(_dwriteFactory, "Segoe UI", 15.0F) With {
            .TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
            .ParagraphAlignment = ParagraphAlignment.Center
        }
    End Sub

    Public Sub DisplayMsg(txtMsg As String, pType As TriggerType)
        ProgressText = New ProgressMsg(txtMsg, pType, _dwriteFactory)

        DisplayProgressText = True
        _textFormat = ProgressText.Format

        _d2dTarget.BeginDraw()
        ' _d2dTarget.FillRectangle(ComputeTrackRect(), _brushBG)

        If DisplayProgressText Then
            _d2dTarget.DrawText(ProgressText.MsgText, ProgressText.Format,
                                ProgressText.Location, _brushText, DrawTextOptions.Clip)
        End If

        _d2dTarget.EndDraw()
        _swapChain.Present(1, PresentFlags.None)
    End Sub

    Public Sub ClearMsg()
        ProgressText = Nothing
        DisplayProgressText = False

        _d2dTarget.BeginDraw()
        _d2dTarget.FillRectangle(ComputeTrackRect(), _brushBG)

        _d2dTarget.EndDraw()
        _swapChain.Present(1, PresentFlags.None)

    End Sub

    Private Sub ResizeSwapChain(w As Integer, h As Integer)
        If w <= 0 OrElse h <= 0 Then Return
        SafeDispose(_rtv)
        SafeDispose(_d2dTarget)

        _swapChain.ResizeBuffers(2, w, h, SharpDX.DXGI.Format.B8G8R8A8_UNorm, SwapChainFlags.None)
        CreateTargetResources()
    End Sub

    Private Sub CreateSizeDependentResources(width As Integer, height As Integer)
        ' nothing extra needed; swapchain size already set in init
    End Sub

    Private Sub DisposeAll()
        SafeDispose(_brushText)
        SafeDispose(_brushBar)
        SafeDispose(_brushBG)
        SafeDispose(_textFormat)
        SafeDispose(_d2dTarget)
        SafeDispose(_d2dFactory)
        SafeDispose(_dwriteFactory)
        SafeDispose(_rtv)
        SafeDispose(_swapChain)
        SafeDispose(_dxgiFactory)
        SafeDispose(_context)
        SafeDispose(_device)
    End Sub

    Private Sub SafeDispose(Of T As {Class, IDisposable})(ByRef obj As T)
        If obj IsNot Nothing Then
            Try
                obj.Dispose()
            Finally
                obj = Nothing
            End Try
        End If
    End Sub

    Private Sub PrepProgressTimer()

        Dim uiContext = SynchronizationContext.Current

        ProgressTimer = New osForms.Timer With {
            .Interval = 5,
            .Enabled = False
        }

        AddHandler ProgressTimer.Tick,
         Sub()
             RenderFrame()

             If ProgressStatus = Success Then
                 SetProgressResult(ProgResult.Completed)
             End If
         End Sub

    End Sub

    Private Sub InitiateProgress()

        ProgressTimer.Enabled = True
        ProgressTimer.Start()

        ProgressStartTime = Stopwatch.GetTimestamp()
    End Sub

    Private Sub ConfigureProgress(pDuration As TimeSpan, objAbortToken As CancellationToken, Optional pEasing As Func(Of Double, Double) = Nothing)
        SetProgressDuration(pDuration)
        ProgressEaseFunc = If(pEasing, AddressOf EaseInOutSine)

        If ProgressTaskSrc IsNot Nothing Then ProgressTaskSrc = Nothing
        ProgressTaskSrc = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                            RunContinuationsAsynchronously)

        objAbortToken.Register(
            Sub()
                SetProgressResult(ProgResult.Cancelled)
            End Sub)
    End Sub

    Private Sub SetProgressDuration(pDuration As TimeSpan)
        ProgressDuration = pDuration
        _durationMs = pDuration.TotalMilliseconds
    End Sub

    Public Async Function BeginProgress(pDuration As TimeSpan, objAbortToken As CancellationToken,
                                           Optional pEasing As Func(Of Double, Double) = Nothing) As Task(Of Boolean)
        ' If Not IsHandleCreated Then Show()

        ConfigureProgress(pDuration, objAbortToken, pEasing)

        PrepProgressTimer()
        InitiateProgress()



        Dim done = Await ProgressTaskSrc.Task.ConfigureAwait(False)


        Return done
    End Function

    Private Async Function StartProgression() As Task

        Do
            ' elapsed → eased fraction
            Dim progDuration = (Stopwatch.GetTimestamp() - ProgressStartTime) * 1000.0 / Stopwatch.Frequency

            Dim t = Math.Max(0.0, Math.Min(1.0, progDuration / _durationMs))
            ProgressValue = ProgressEaseFunc(t) ' 0..1

            Dim track As New osRect.RawRectangleF(0, 0, ProgressWidth, ProgressHeight)

            ' If ProgressValue = 0 Then Exit Sub

            Dim curFillPx As Integer = CInt(Math.Round(ProgressValue * track.GetWidth()))
            _d2dTarget.BeginDraw()

            If curFillPx <> _lastFillPx Then
                Dim x1 As Single = track.Left + Math.Min(_lastFillPx, curFillPx)
                Dim x2 As Single = track.Left + Math.Max(_lastFillPx, curFillPx)

                Dim strip As New osRect.RawRectangleF(x1, 0, x2 - x1, track.Bottom)

                _d2dTarget.FillRectangle(strip, _brushBar)
                _lastFillPx = curFillPx
            End If

            If DisplayProgressText Then
                _d2dTarget.DrawText(ProgressText.MsgText, ProgressText.Format, ProgressText.Location, _brushText,
                                DrawTextOptions.Clip)
            End If

            _d2dTarget.EndDraw()
            ' Only present when something changed (reduces CPU)
            _swapChain.Present(1, PresentFlags.None)

            If t >= 1 Then
                ProgressStatus = Success
            End If

            ' keep UI responsive; 1 ms is usually fine. Use Yield() if you want “as fast as possible”.
            Await Task.Yield()
        Loop Until Not ProgressStatus = ProgStatus.Idle

        ' Time & eased fraction


    End Function

    Public Sub SetProgressResult(Optional setResult As ProgResult = Nothing)
        ProgressTimer.Stop()

        Select Case setResult
            Case ProgResult.Cancelled
                ProgressTaskSrc?.TrySetResult(False)
                RaiseEvent ProgressFail(Me, EventArgs.Empty)
            Case ProgResult.Completed
                ProgressTaskSrc?.TrySetResult(True)
                RaiseEvent ProgressSuccess(Me, EventArgs.Empty)
        End Select

        ProgressTaskSrc = Nothing
    End Sub

    Private Sub PrimeBackground(track As osRect.RawRectangleF)
        _d2dTarget.BeginDraw()
        ' Draw ONLY the background track; do NOT clear the whole backbuffer
        _d2dTarget.FillRectangle(track, _brushBG)
        _d2dTarget.EndDraw()

        _lastFillPx = 0
        _lastTrack = track
        _primed = True
    End Sub

    Public Sub DrawBG()
        Dim track = ComputeTrackRect()

        _d2dTarget.BeginDraw()
        _d2dTarget.Clear(New osRect.RawColor4(0, 0, 0, 0))
        ' Draw ONLY the background track; do NOT clear the whole backbuffer
        _d2dTarget.FillRectangle(track, _brushBG)

        _d2dTarget.EndDraw()
        _swapChain.Present(1, PresentFlags.None)

        '_lastFillPx = 0
        '_lastTrack = track
        '_primed = True

        'If DisplayProgressText Then
        '    _d2dTarget.DrawText(ProgressText.MsgText, ProgressText.Format, ProgressText.Location, _brushText,
        '                        DrawTextOptions.Clip Or DrawTextOptions.None)
        'End If
    End Sub


    Private Sub RenderFrame()
        ' Time & eased fraction
        Dim progDuration = (Stopwatch.GetTimestamp() - ProgressStartTime) * 1000.0 / Stopwatch.Frequency

        Dim t = Math.Max(0.0, Math.Min(1.0, progDuration / _durationMs))
        ProgressValue = ProgressEaseFunc(t) ' 0..1

        Dim track As New osRect.RawRectangleF(0, 0, ProgressWidth, ProgressHeight)

        ' If ProgressValue = 0 Then Exit Sub

        Dim curFillPx As Integer = CInt(Math.Round(ProgressValue * track.GetWidth()))
        _d2dTarget.BeginDraw()

        If curFillPx <> _lastFillPx Then
            Dim x1 As Single = track.Left + Math.Min(_lastFillPx, curFillPx)
            Dim x2 As Single = track.Left + Math.Max(_lastFillPx, curFillPx)

            Dim strip As New osRect.RawRectangleF(x1, 0, x2 - x1, track.Bottom)

            _d2dTarget.FillRectangle(strip, _brushBar)
            _lastFillPx = curFillPx
        End If

        If DisplayProgressText Then
            _d2dTarget.DrawText(ProgressText.MsgText, ProgressText.Format, ProgressText.Location, _brushText,
                                DrawTextOptions.Clip)
        End If

        _d2dTarget.EndDraw()
        ' Only present when something changed (reduces CPU)
        _swapChain.Present(1, PresentFlags.None)

        If t >= 1 Then
            ProgressStatus = Success
        End If

    End Sub


    Public Sub SetProgress01(fraction As Double,
                         Optional fillFromRight As Boolean = False)
        If _swapChain Is Nothing OrElse _d2dTarget Is Nothing Then Exit Sub

        Dim f As Double = Math.Max(0.0, Math.Min(1.0, fraction))
        Dim track = ComputeTrackRect()

        ' Prime once or if layout changed
        If Not _primed OrElse track.GetWidth() <> _lastTrack.GetWidth() OrElse track.Bottom <> _lastTrack.Bottom Then
            PrimeBackground(track)
        End If

        ' Current fill width in pixels within the track
        Dim curFillPx As Integer = CInt(Math.Round(f * track.GetWidth()))

        ' If nothing changed, do nothing
        If curFillPx = _lastFillPx Then Exit Sub

        ' Build the narrow delta strip
        Dim x1 As Single, x2 As Single
        If fillFromRight Then
            ' right-anchored: convert widths to Left coord
            Dim curLeft As Single = track.Right - curFillPx
            Dim prevLeft As Single = track.Right - _lastFillPx
            x1 = Math.Min(curLeft, prevLeft)
            x2 = Math.Max(curLeft, prevLeft)
        Else
            ' left-anchored
            x1 = track.Left + Math.Min(_lastFillPx, curFillPx)
            x2 = track.Left + Math.Max(_lastFillPx, curFillPx)
        End If

        Dim strip As New osRect.RawRectangleF(x1, 0, Math.Max(0.0F, x2 - x1), track.Bottom)
        Dim StripWidth = GetWidth(strip)

        If StripWidth > 0.0F Then
            _d2dTarget.BeginDraw()
            If curFillPx > _lastFillPx Then
                ' Growing → paint new strip with bar brush
                _d2dTarget.FillRectangle(strip, _brushBar)
            Else
                ' Shrinking → erase strip with background brush
                _d2dTarget.FillRectangle(strip, _brushBG)
            End If
            _d2dTarget.EndDraw()
            _swapChain.Present(1, PresentFlags.None)
        End If

        _lastFillPx = curFillPx
    End Sub

    Public Sub SetProgressPercent(percent As Double,
                              Optional fillFromRight As Boolean = False)
        Dim f As Double = Math.Max(0.0, Math.Min(100.0, percent)) / 100.0
        SetProgress01(f, fillFromRight)
    End Sub

    Private Sub RenderProgress(progPercent As Double)
        ' Time & eased fraction

        Dim track As New osRect.RawRectangleF(0, 0, ProgressWidth, ProgressHeight)

        Dim fillRight As Single = track.Left + CSng(Math.Max(0.0, Math.Min(1.0, (progPercent / 100))) * (track.GetWidth()))
        Dim fill As New osRect.RawRectangleF(track.Left, track.Top, fillRight, track.Bottom)

        _d2dTarget.BeginDraw()
        '_d2dTarget.Clear(New osRect.RawColor4(0, 0, 0, 0)) ' transparent, overlay-safe

        '  _d2dTarget.FillRectangle(track, _brushBG)

        ' Filled portion
        _d2dTarget.FillRectangle(fill, _brushBar)

        ' Optional centered text

        If DisplayProgressText Then
            _d2dTarget.DrawText(ProgressText.MsgText, ProgressText.Format, ProgressText.Location, _brushText,
                                DrawTextOptions.Clip Or DrawTextOptions.None)
        End If

        _d2dTarget.EndDraw()

        ' Present synchronized to vblank (smooth + low CPU)
        _swapChain.Present(1, PresentFlags.None)
    End Sub

    ' Rebuilds the current progress track rectangle using your layout properties
    Private Function ComputeTrackRect() As osRect.RawRectangleF
        Return New osRect.RawRectangleF(0, 0, ProgressWidth, ProgressHeight)
    End Function

    Private Function CalcRGB(cVal As Byte) As Single
        Return cVal / 255.0F
    End Function

    Public Sub SetProgColor(pColor As osColor.Color, Optional pUpdate As Boolean = False)
        With pColor
            BarColorARGB = New osRect.RawColor4(CalcRGB(.R), CalcRGB(.G),
                                                CalcRGB(.B), 1.0F)
        End With

        If pUpdate Then
            _brushBar = New SolidColorBrush(_d2dTarget, BarColorARGB)

            With _d2dTarget
                .BeginDraw()
                .FillRectangle(ComputeTrackRect(), _brushBar)
                .EndDraw()
            End With

            _swapChain.Present(1, PresentFlags.None)
        End If
    End Sub

    Public Shared Function EaseInOutSine(x As Double) As Double
        Return -(Math.Cos(Math.PI * x) - 1.0) / 2.0
    End Function

    Public Shared Function EaseInOutExpo(x As Double) As Double
        If x = 0 Then Return 0
        If x = 1 Then Return 1
        Return If(x < 0.5, Math.Pow(2, 20 * x - 10) / 2, (2 - Math.Pow(2, -20 * x + 10)) / 2)
    End Function

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW Or WS_EX_TOPMOST
            cp.Style = cp.Style And Not &H8000000 ' WS_BORDER off
            Return cp
        End Get
    End Property

End Class
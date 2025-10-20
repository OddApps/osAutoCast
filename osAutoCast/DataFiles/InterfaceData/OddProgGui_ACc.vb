Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Interop
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Threading
Imports SharpDX.Direct3D9
Imports SharpDX
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Windows.Markup
Imports System.Xml
Imports Device = SharpDX.Direct3D9.Device

Public NotInheritable Class D3DImagePresenter
    Implements IDisposable

    Public ReadOnly Property d3Image As D3DImage            'bind this to an <Image/>
    Private ReadOnly _d3d As Direct3DEx
    Private ReadOnly _device As DeviceEx
    Private _sharedTexture As Texture
    Private _surface As Surface
    Private ReadOnly _width As Integer, _height As Integer
    Private ReadOnly _dpiScale As Double

    Public Sub New(w As Integer, h As Integer, dpiScale As Double)
        _width = w : _height = h : _dpiScale = dpiScale
        d3Image = New D3DImage()

        _d3d = New Direct3DEx()

        Dim pp As New PresentParameters() With {
            .Windowed = True,
            .SwapEffect = SwapEffect.Discard,
            .PresentationInterval = PresentInterval.Immediate,
            .DeviceWindowHandle = IntPtr.Zero
        }

        _device = New DeviceEx(_d3d, 0, DeviceType.Hardware, IntPtr.Zero,
                               CreateFlags.HardwareVertexProcessing Or CreateFlags.Multithreaded Or CreateFlags.FpuPreserve,
                               pp)

        CreateSharedTexture()

        AddHandler CompositionTarget.Rendering, AddressOf OnRendering
    End Sub

    'Capture the visual (the WPF window/control) each frame and blit it to the texture
    Public Sub Update(backVisual As Visual)
        If backVisual Is Nothing Then Return
        Dim rtb = New RenderTargetBitmap(CInt(_width * _dpiScale),
                                         CInt(_height * _dpiScale),
                                         96 * _dpiScale, 96 * _dpiScale,
                                         PixelFormats.Pbgra32)
        rtb.Render(backVisual)

        Dim stride = _width * 4
        Dim pixelData(rtb.PixelHeight * stride - 1) As Byte
        rtb.CopyPixels(pixelData, stride, 0)

        'Write into D3D texture
        Dim rect = New Mathematics.Interop.RawRectangle(0, 0, _width, _height)
        Dim dr = _surface.LockRectangle(rect, LockFlags.None)

        Using ds = New DataStream(dr.DataPointer, _height * dr.Pitch, True, True)
            ds.Write(pixelData, 0, pixelData.Length)
        End Using

        _surface.UnlockRectangle()
        'Dim dr = _surface.LockRectangle(rect, LockFlags.None)
        'Utilities.CopyMemory(dr.DataPointer, pixelData, pixelData.Length)
        '_surface.UnlockRectangle()

        'Tell WPF the back-buffer changed
        d3Image.Lock()
        d3Image.AddDirtyRect(New Int32Rect(0, 0, _width, _height))
        d3Image.Unlock()
    End Sub

    Private Sub OnRendering(sender As Object, e As EventArgs)
        If d3Image.IsFrontBufferAvailable AndAlso _surface IsNot Nothing Then
            d3Image.Lock()
            d3Image.AddDirtyRect(New Int32Rect(0, 0, _width, _height))
            d3Image.Unlock()
        End If
    End Sub

    Private Sub CreateSharedTexture()
        _sharedTexture = New Texture(_device, _width, _height, 1,
                                     Usage.RenderTarget, Format.A8R8G8B8, Pool.Default)
        _surface = _sharedTexture.GetSurfaceLevel(0)

        d3Image.Lock()
        d3Image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _surface.NativePointer)
        d3Image.Unlock()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        RemoveHandler CompositionTarget.Rendering, AddressOf OnRendering

        d3Image?.Lock()
        d3Image?.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero)
        d3Image?.Unlock()
        _surface?.Dispose()
        _sharedTexture?.Dispose()
        _device?.Dispose()
        _d3d?.Dispose()
    End Sub
End Class

''' <summary>
''' A WPF Window hosting an OddLibProgressBar via a D3DImage-backed
''' Direct3D9 surface for GPU-accelerated overlays.
''' </summary>
Public Class OddProgGui_ACc
    Inherits Window

    Private _d3d As Direct3DEx
    Private _device As DeviceEx
    Private _device2 As Device
    Private _surface As Surface
    Private _texture As Texture
    Private _d3dImage As D3DImage
    Private _imageCtrl As Image
    Private _bar As OddLib_ProgressBar
    Private _sync As Object = New Object()
    Private _sysSurface As Surface ' system-memory surface for copying

    Public Sub New(ByRef guiThread As Thread)
        ' Window setup
        'Me.Width = guiW
        'Me.Height = guiH
        guiThread = Me.Dispatcher.Thread

        'If IsNothing(ptPosData) Then
        '    WindowStartupLocation = WindowStartupLocation.CenterScreen
        'Else
        '    With osFuncLib_Progress.CalcPosData(ptPosData)
        '        Me.Left = .X
        '        Me.Top = .Y
        '    End With
        'End If

        'WindowStyle = WindowStyle.None
        'AllowsTransparency = True
        'Background = Brushes.Transparent
        'Topmost = True
        'ShowInTaskbar = False

        'Using sr As New StringReader(barXaml),
        '  xr As XmlReader = XmlReader.Create(sr)
        '    _bar = CType(XamlReader.Load(xr), OddLib_ProgressBar)
        'End Using

        '' 2) Resize/reposition as needed
        '_bar.Width = guiW
        '_bar.Height = guiH / 10
        'Dim canvas As New Canvas()
        'canvas.Children.Add(_bar)
        'Canvas.SetLeft(_bar, 0)
        'Canvas.SetTop(_bar, guiH - _bar.Height)
        'Me.Content = canvas

        '' Host Visual: Image control showing the D3DImage
        '_d3dImage = New D3DImage()
        '_imageCtrl = New Image() With {
        '    .Source = _d3dImage,
        '    .Stretch = Stretch.None,
        '    .Width = guiW,
        '    .Height = guiH
        '}
        'Content = _imageCtrl

        'AddHandler Me.Loaded, AddressOf OnLoaded
        'AddHandler Me.Unloaded, AddressOf OnUnloaded
    End Sub

    Public Sub PrepNewGui(barXaml As String, guiW As Integer, guiH As Integer, ptPosData As System.Drawing.Point)
        ' Window setup
        Me.Width = guiW
        Me.Height = guiH
        'Me.Dispatcher.Thread

        If IsNothing(ptPosData) Then
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        Else
            With osFuncLib_Progress.CalcPosData(ptPosData)
                Me.Left = .X
                Me.Top = .Y
            End With
        End If

        WindowStyle = WindowStyle.None
        AllowsTransparency = True
        Background = Brushes.Transparent
        Topmost = True
        ShowInTaskbar = False

        Using sr As New StringReader(barXaml),
          xr As XmlReader = XmlReader.Create(sr)
            _bar = CType(XamlReader.Load(xr), OddLib_ProgressBar)
        End Using

        ' 2) Resize/reposition as needed
        _bar.Width = guiW
        _bar.Height = guiH / 10
        Dim canvas As New Canvas()
        canvas.Children.Add(_bar)
        Canvas.SetLeft(_bar, 0)
        Canvas.SetTop(_bar, guiH - _bar.Height)
        Me.Content = canvas

        ' Host Visual: Image control showing the D3DImage
        _d3dImage = New D3DImage()
        _imageCtrl = New Image() With {
            .Source = _d3dImage,
            .Stretch = Stretch.None,
            .Width = guiW,
            .Height = guiH
        }
        Content = _imageCtrl

        Dim w = CInt(Me.Width), h = CInt(Me.Height)

        InitializeD3D(w, h)
        HookRenderLoop()

        AddHandler Me.Unloaded, AddressOf OnUnloaded
    End Sub


    Public Sub New(width As Integer, height As Integer, Optional ptPosData As System.Drawing.Point = Nothing)
        ' Window setup
        width = width
        height = height

        If IsNothing(ptPosData) Then
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        Else
            With osFuncLib_Progress.CalcPosData(ptPosData)
                Me.Left = .X
                Me.Top = .Y
            End With
        End If
        WindowStyle = WindowStyle.None
        AllowsTransparency = True
        Background = Brushes.Transparent
        Topmost = True
        ShowInTaskbar = False

        ' Host Visual: Image control showing the D3DImage
        _d3dImage = New D3DImage()
        _imageCtrl = New Image() With {
            .Source = _d3dImage,
            .Stretch = Stretch.None,
            .Width = width,
            .Height = height
        }
        Content = _imageCtrl


    End Sub

    'Private Sub OnLoaded(sender As Object, e As RoutedEventArgs)
    '    ' Initialize Direct3D9Ex
    '    _d3d = New Direct3DEx()
    '    Dim presentParams = New PresentParameters() With {
    '        .Windowed = True,
    '        .SwapEffect = SwapEffect.Discard,
    '        .DeviceWindowHandle = New WindowInteropHelper(Me).Handle,
    '        .PresentationInterval = PresentInterval.Immediate
    '    }
    '    _device = New DeviceEx(_d3d, 0, DeviceType.Hardware, IntPtr.Zero,
    '                           CreateFlags.HardwareVertexProcessing Or CreateFlags.Multithreaded Or CreateFlags.FpuPreserve,
    '                           presentParams)

    '    ' Create GPU render target texture
    '    _texture = New Texture(_device, CInt(Width), CInt(Height), 1,
    '                           Usage.RenderTarget, Format.A8R8G8B8, Pool.Default)
    '    _surface = _texture.GetSurfaceLevel(0)
    '    _sysSurface = Surface.CreateOffscreenPlain(_device, CInt(Width), CInt(Height), Format.A8R8G8B8, Pool.SystemMemory)

    '    ' Create system-memory surface once for UpdateSurface

    '    ' Link surface to D3DImage
    '    _d3dImage.Lock()
    '    _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _surface.NativePointer)
    '    _d3dImage.Unlock()

    '    ' Set up WPF progress bar
    '    _bar = New OddLib_ProgressBar() With {
    '        .Width = Width,
    '        .Height = Height / 10,
    '        .BarBrush = New SolidColorBrush(Color.FromRgb(0, 200, 0)).FreezeReturn(),
    '        .BackBrush = New SolidColorBrush(Color.FromRgb(30, 30, 30)).FreezeReturn(),
    '        .IsAutoPass = False
    '    }
    '    ' Position bar at bottom
    '    Canvas.SetLeft(_bar, 0)
    '    Canvas.SetTop(_bar, Height - _bar.Height)

    '    ' Use a DrawingVisual to host the bar
    '    ' We'll render this into the D3D surface each frame
    '    AddHandler CompositionTarget.Rendering, AddressOf RenderFrame
    'End Sub

    Private Sub OnLoaded(sender As Object, e As RoutedEventArgs)
        Dim w = CInt(Me.Width), h = CInt(Me.Height)

        InitializeD3D(w, h)
        HookRenderLoop()
    End Sub

    Private Sub InitializeD3D(widthPx As Integer, heightPx As Integer)
        ' 1) Create the Direct3DEx factory and DeviceEx
        _d3d = New Direct3DEx()

        Dim pp = New PresentParameters() With {
            .Windowed = True,
            .SwapEffect = SwapEffect.Discard,
            .DeviceWindowHandle = New WindowInteropHelper(Me).Handle,
            .PresentationInterval = PresentInterval.Immediate
        }

        _device = New DeviceEx(_d3d, 0, DeviceType.Hardware, IntPtr.Zero,
                               CreateFlags.HardwareVertexProcessing Or
                               CreateFlags.Multithreaded Or
                               CreateFlags.FpuPreserve, pp)

        ' 2) Allocate the GPU render target texture + surface
        _texture = New Texture(_device, widthPx, heightPx, 1,
                               Usage.RenderTarget, Format.A8R8G8B8, Pool.Default)
        _surface = _texture.GetSurfaceLevel(0)

        ' 3) Allocate the system-memory surface for UpdateSurface
        _sysSurface = Surface.CreateOffscreenPlain(_device, widthPx, heightPx,
                                                   Format.A8R8G8B8, Pool.SystemMemory)

        ' 4) Tell the D3DImage to use that GPU surface as its back buffer
        _d3dImage.Lock()
        _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9,
                                _surface.NativePointer)
        _d3dImage.Unlock()
    End Sub

    Private Sub HookRenderLoop()
        ' Connect our RenderFrame method to WPF's per-frame event
        AddHandler CompositionTarget.Rendering, AddressOf RenderFrame
    End Sub

    Private Sub UnhookRenderLoop()
        RemoveHandler CompositionTarget.Rendering, AddressOf RenderFrame
    End Sub

    Private Sub OnUnloaded(sender As Object, e As RoutedEventArgs)
        UnhookRenderLoop()

        SyncLock _sync
            _sysSurface?.Dispose()
            _surface?.Dispose()
            _texture?.Dispose()
            _device?.Dispose()
            _d3d?.Dispose()
        End SyncLock
    End Sub

    ''' <summary>
    ''' Frame callback: render the WPF bar into a bitmap, copy to D3D surface,
    ''' then notify D3DImage to present the updated region.
    ''' </summary>
    Private Sub RenderFrame(sender As Object, e As EventArgs)
        SyncLock _sync
            ' Render WPF bar into RenderTargetBitmap
            Dim widthPx = CInt(Width)
            Dim heightPx = CInt(Height)

            Dim rtb = New RenderTargetBitmap(widthPx, heightPx, 96, 96, PixelFormats.Pbgra32)
            Dim dv = New DrawingVisual()

            Using dc = dv.RenderOpen()
                dc.DrawRectangle(Brushes.Transparent, Nothing, New Rect(0, 0, Width, Height))

                Dim vb = New VisualBrush(_bar)
                dc.DrawRectangle(vb, Nothing, New Rect(0, Height - _bar.Height, _bar.Width, _bar.Height))
            End Using

            rtb.Render(dv)

            ' Copy the bitmap pixels into the system-memory surface
            Dim stride = widthPx * 4
            Dim pixelData = New Byte(stride * heightPx - 1) {}
            rtb.CopyPixels(pixelData, stride, 0)

            Dim lockedRect = _sysSurface.LockRectangle(LockFlags.None)
            Marshal.Copy(pixelData, 0, lockedRect.DataPointer, pixelData.Length)
            _sysSurface.UnlockRectangle()

            ' Copy from system-memory to GPU render target
            _device.UpdateSurface(_sysSurface, Nothing, _surface, Nothing)

            ' Notify WPF that D3DImage has new data
            _d3dImage.Lock()
            _d3dImage.AddDirtyRect(New Int32Rect(0, 0, widthPx, heightPx))
            _d3dImage.Unlock()
        End SyncLock
    End Sub

    Public Function BeginOverlaySweep(duration As TimeSpan,
                                      Optional easingFn As Func(Of Double, Double) = Nothing,
                                      Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
        Return _bar.BeginProgressAsync(duration, True, False, ct, easingFn)
    End Function

    ''' <summary>
    ''' Public API to drive the overlay progress.
    ''' </summary>
    Public Async Function BeginOverlaySweep() As Task(Of ProgResult)
        Dim sweepTask = Await _bar.BeginProgressAsync(
    osFuncLib_Progress.ProgTimeSpan,
    fromStart:=True,
    autoReset:=False,
    ct:=CoreDataLib.objCancelState,
    easingFn:=AddressOf EaseLinearThenExpoIn)

        Return ProgResult.Completed
    End Function
End Class


Imports System.Threading
Imports System.Windows.Forms
Imports osAutoCast.DataTypeLib.ProgStatus
Imports SharpDX
Imports SharpDX.D3DCompiler
Imports SharpDX.Direct2D1
Imports SharpDX.Direct3D
Imports SharpDX.Direct3D11
Imports SharpDX.DirectWrite
Imports SharpDX.DXGI
Imports AlphaMode = SharpDX.Direct2D1.AlphaMode
Imports D2DPixelFormat = SharpDX.Direct2D1.PixelFormat
Imports osColor = System.Windows.Media
Imports osDraw = System.Drawing
Imports osFormat = SharpDX.DXGI.Format
Imports osForms = System.Windows.Forms
Imports osProgBuffer = SharpDX.Direct3D11.Buffer
Imports osProgColor = SharpDX.Mathematics.Interop.RawColor4
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports osProgDeviceContext = SharpDX.Direct3D11.DeviceContext
Imports osProgFactoryD2D = SharpDX.Direct2D1.Factory
Imports osProgFactoryDW = SharpDX.DirectWrite.Factory
Imports osProgFactoryDXGI = SharpDX.DXGI.Factory
Imports osRect = SharpDX.Mathematics.Interop
Imports osViewPort = SharpDX.Mathematics.Interop.RawViewportF

Public Class ProgBarGui_AutoCast

    Private Const WAIT_OBJECT_0 As UInteger = 0UI
    Private Const INFINITE As Integer = -1

    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80
    Private Const WS_EX_TRANSPARENT As Integer = &H20
    Private Const WS_EX_TOPMOST As Integer = &H8

    Private progSwapChain As SwapChain
    Private progSwapChain1 As SwapChain1
    Private progSwapChain2 As SwapChain2

    Private progRTV As RenderTargetView

    Private frameLatencyEvent As IntPtr = IntPtr.Zero

    Private pVS As VertexShader
    Private pPS As PixelShader
    Private pCB As osProgBuffer

    Private progTarget As RenderTarget

    Private progBrush_BG As SolidColorBrush
    Private progBrush_Active As SolidColorBrush
    Private progBrush_Text As SolidColorBrush

    Private progMsg_Config As TextFormat

    Public Event ProgressSuccess As EventHandler
    Public Event ProgressFail As EventHandler

    Private _evProgComplete As EventHandler
    Private _evProgFail As EventHandler

    Private retProgResult As ProgResult = Nothing

    Private ProgressText As ProgressMsg

    Private Shared ReadOnly ProgressClock_TickCnt As Double = 1000.0 / Stopwatch.Frequency
    Private ProgressClock_Duration As TimeSpan
    Private ProgressClock_StartTime As Long
    Private ProgressClock_StepInt As Double

    Private ProgressFuse As Double

    Private ProgressTrack As osRect.RawRectangleF
    Private ProgressValue As Single = 0
    Private ProgressStatus As ProgStatus
    Private ProgressEaseFunc As Func(Of Double, Double)

    Private AutoCastComplete As Boolean

    Public Property BarCornerRadius As Single = 0.0F

    Private progColor_Active As osProgColor
    Private progColor_BG As osProgColor
    Private progColor_Text As osProgColor

    Private progStartPos As osDraw.Point

    Private _lastFillPx As Integer = 0

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

    Public ReadOnly Property progDevice As osProgDevice
        Get
            Return GraphicsHandler.pDevice
        End Get
    End Property

    Public ReadOnly Property progContext As osProgDeviceContext
        Get
            Return GraphicsHandler.pContext
        End Get
    End Property

    Public ReadOnly Property progDxgiFactory As osProgFactoryDXGI
        Get
            Return GraphicsHandler.pDxgiFactory
        End Get
    End Property

    Public ReadOnly Property progD2DFactory As osProgFactoryD2D
        Get
            Return GraphicsHandler.pD2DFactory
        End Get
    End Property

    Public ReadOnly Property progDwriteFactory As osProgFactoryDW
        Get
            Return GraphicsHandler.pDWFactory
        End Get
    End Property

    Private Const objShader_Vertex As String =
"struct VSOut { float4 pos:SV_Position; float2 uv:TEXCOORD0; };
VSOut VSMain(uint vid:SV_VertexID){
    float2 p[3] = { float2(-1,-1), float2(-1,3), float2(3,-1) };
    VSOut o; o.pos=float4(p[vid],0,1); o.uv=0.5*(p[vid]+1); return o; }"

    Private Const objShader_Pixel As String =
"cbuffer Bar : register(b0){
    float prevValue;
    float currValue;
    float flags;
    float pad;
    float4 pcoloractive;
    float4 pcolorbg;
}

struct PSIn {
	float4 pos:SV_Position;
	float2 uv:TEXCOORD0;
};

float band_mask(float a, float b, float u){
    return step(a, u) * step(u, b);
}

float4 PSMain(PSIn pin) : SV_Target {
	float u = (flags >= 1.0) ? (1.0 - pin.uv.y) : pin.uv.x;

	float a = min(prevValue, currValue);
	float b = max(prevValue, currValue);

	if (a == b) discard;

	float m = band_mask(a, b, u);

	if (m <= 0.0) discard;

	float inc = (currValue >= prevValue) ? 1.0 : 0.0;
	return lerp(pcolorbg, pcoloractive, inc);
}"

    Public Sub New(pW As Integer, pH As Integer)
        InitializeComponent()

        FormBorderStyle = osForms.FormBorderStyle.None

        ProgressWidth = pW
        ProgressHeight = pH

        BackColor = osDraw.Color.FromArgb(57, 57, 57)

        ProgressTrack = ComputeTrackRect()
        StartPosition = FormStartPosition.Manual
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        TopMost = True
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)

        If progSwapChain IsNot Nothing AndAlso ProgressWidth > 0 AndAlso ProgressHeight > 0 Then
            ResizeSwapChain(ProgressWidth, ProgressHeight)
            DrawBG()
        End If
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        ObjectDump(True)
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        InitDeviceAndSwapChain()
        CreateSizeDependentResources(ProgressWidth, ProgressHeight)

        ProgressStatus = ProgStatus.Idle
    End Sub

    Private Sub InitDeviceAndSwapChain()
        ' reset
        progSwapChain1?.Dispose()
        progSwapChain1 = Nothing
        progSwapChain2?.Dispose()
        progSwapChain2 = Nothing
        progSwapChain = Nothing
        frameLatencyEvent = IntPtr.Zero

        ' Try Flip-Model
        Try
            Dim sc1 As SwapChain1 = Nothing
            Using fac2 = progDxgiFactory.QueryInterface(Of SharpDX.DXGI.Factory2)()
                Dim desc = New SwapChainDescription1 With {
                .Width = Math.Max(1, ProgressWidth),
                .Height = Math.Max(1, ProgressHeight),
                .Format = osFormat.B8G8R8A8_UNorm,
                .BufferCount = 2,
                .Usage = Usage.RenderTargetOutput,
                .SampleDescription = New SampleDescription(1, 0),
                .Scaling = Scaling.Stretch,
                .SwapEffect = SwapEffect.FlipDiscard,
                .AlphaMode = AlphaMode.Ignore
            }
                sc1 = New SwapChain1(fac2, GraphicsHandler.pDevice, Me.Handle, desc)
            End Using

            ' hold onto sc1 — do NOT dispose here
            progSwapChain1 = sc1
            progSwapChain2 = sc1.QueryInterface(Of SwapChain2)()
            progSwapChain2.MaximumFrameLatency = 1
            frameLatencyEvent = progSwapChain2.FrameLatencyWaitableObject
        Catch
            ' fall through to legacy path
        End Try

        ' Legacy Discard if Flip failed
        If progSwapChain1 Is Nothing Then
            Dim scDesc = New SwapChainDescription With {
            .BufferCount = 2,
            .ModeDescription = New ModeDescription(Math.Max(1, ProgressWidth),
                                                   Math.Max(1, ProgressHeight),
                                                   New Rational(60, 1),
                                                   osFormat.B8G8R8A8_UNorm),
            .IsWindowed = True,
            .OutputHandle = Me.Handle,
            .SampleDescription = New SampleDescription(1, 0),
            .SwapEffect = SwapEffect.Discard,
            .Usage = Usage.RenderTargetOutput
        }
            progSwapChain = New SwapChain(GraphicsHandler.pDxgiFactory, GraphicsHandler.pDevice, scDesc)
        End If

        CreateTargetResources()
        DrawBG()
        CreateShadersAndPipeline()

        ' Prime the pacing
        If progSwapChain1 IsNot Nothing Then
            progSwapChain1.Present(1, PresentFlags.None)
        Else
            progSwapChain.Present(1, PresentFlags.None)
        End If
    End Sub


    'Private Sub InitDeviceAndSwapChain()
    '    progSwapChain = Nothing
    '    progSwapChain1 = Nothing
    '    progSwapChain2 = Nothing
    '    frameLatencyEvent = IntPtr.Zero

    '    ' Prefer Flip-Model (pacing support), fallback to Discard.
    '    Try
    '        Using fac2 = progDxgiFactory.QueryInterface(Of SharpDX.DXGI.Factory2)()
    '            Dim sc1 = New SwapChainDescription1 With {
    '            .Width = Math.Max(1, ProgressWidth),
    '            .Height = Math.Max(1, ProgressHeight),
    '            .Format = osFormat.B8G8R8A8_UNorm,
    '            .BufferCount = 2,
    '            .Usage = Usage.RenderTargetOutput,
    '            .SampleDescription = New SampleDescription(1, 0),
    '            .Scaling = Scaling.Stretch,
    '            .SwapEffect = SwapEffect.FlipDiscard,
    '            .AlphaMode = AlphaMode.Ignore
    '        }
    '            Using sc1Obj = New SwapChain1(fac2, GraphicsHandler.pDevice, Me.Handle, sc1)
    '                progSwapChain = sc1Obj.QueryInterface(Of SwapChain)()
    '                progSwapChain1 = sc1Obj
    '                progSwapChain2 = sc1Obj.QueryInterface(Of SwapChain2)()
    '                progSwapChain2.MaximumFrameLatency = 1
    '                frameLatencyEvent = progSwapChain2.FrameLatencyWaitableObject
    '            End Using
    '        End Using
    '    Catch ex As Exception
    '        ' fall through to legacy path
    '    End Try

    '    If progSwapChain Is Nothing Then
    '        Dim scDesc = New SwapChainDescription With {
    '        .BufferCount = 2,
    '        .ModeDescription = New ModeDescription(
    '            Math.Max(1, ProgressWidth),
    '            Math.Max(1, ProgressHeight),
    '            New Rational(60, 1),
    '            osFormat.B8G8R8A8_UNorm),
    '        .IsWindowed = True,
    '        .OutputHandle = Me.Handle,
    '        .SampleDescription = New SampleDescription(1, 0),
    '        .SwapEffect = SwapEffect.Discard,
    '        .Usage = Usage.RenderTargetOutput
    '    }
    '        progSwapChain = New SwapChain(GraphicsHandler.pDxgiFactory, GraphicsHandler.pDevice, scDesc)
    '    End If

    '    CreateTargetResources()
    '    DrawBG()                    ' one-time clear
    '    CreateShadersAndPipeline()  ' VS/PS/CB + bind

    '    ' Prime: show first frame so the waitable event is armed
    '    progSwapChain.Present(1, PresentFlags.None)
    'End Sub

    ' Present only the bar region if Flip-model (SwapChain1) is available.
    ' Falls back to normal Present otherwise.
    Private Sub PresentDirty()
        progTarget.EndDraw()

        Dim left As Integer = ProgressTrack.Left
        Dim top As Integer = 0
        Dim right As Integer = left + ProgressTrack.GetWidth()
        Dim bottom As Integer = ProgressTrack.Bottom

        ' make a valid rect inside the backbuffer
        Dim w = Math.Max(0, right - left)
        Dim h = Math.Max(0, bottom - top)

        If progSwapChain1 IsNot Nothing Then
            Dim pp As New SharpDX.DXGI.PresentParameters With {
            .DirtyRectangles = New osRect.RawRectangle() {New osRect.RawRectangle(left, top, w, h)}
        }
            progSwapChain1.Present(1, PresentFlags.None, pp)
        Else
            progSwapChain.Present(1, PresentFlags.None)
        End If
    End Sub



    Private scissorState As RasterizerState
    Private fullViewport As osViewPort

    Private Sub CreateScissorState()
        scissorState?.Dispose()
        scissorState = New RasterizerState(progDevice, New RasterizerStateDescription With {
        .CullMode = CullMode.None,
        .FillMode = Direct3D11.FillMode.Solid,
        .IsScissorEnabled = True
    })
    End Sub

    Private Sub CreateTargetResources()
        ' 0) Unbind + dispose previous targets
        If progContext IsNot Nothing Then
            progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))
        End If
        progRTV.SafeDispose()
        progTarget.SafeDispose()

        ' 1) RTV from backbuffer
        Using backBuffer As Texture2D = progSwapChain.GetBackBuffer(Of Texture2D)(0)
            progRTV = New RenderTargetView(progDevice, backBuffer)

            ' 2) D2D render target sharing the DXGI surface
            Using dxgiSurface As Surface = backBuffer.QueryInterface(Of Surface)()
                Dim dpiX As Single = 96.0F, dpiY As Single = 96.0F
                Using g As osDraw.Graphics = Me.CreateGraphics()
                    dpiX = g.DpiX : dpiY = g.DpiY
                End Using

                Dim d2dPixelFmt As New D2DPixelFormat(osFormat.B8G8R8A8_UNorm, Direct2D1.AlphaMode.Ignore)

                Dim props As New RenderTargetProperties(Direct2D1.RenderTargetType.Default,
                                                        d2dPixelFmt, dpiX, dpiY,
                                                        Direct2D1.RenderTargetUsage.None,
                                                        Direct2D1.FeatureLevel.Level_DEFAULT)

                progTarget = New RenderTarget(progD2DFactory, dxgiSurface, props) With {
                    .AntialiasMode = Direct2D1.AntialiasMode.Aliased,
                    .TextAntialiasMode = If(dpiX = 96.0F AndAlso dpiY = 96.0F,
                                            Direct2D1.TextAntialiasMode.Cleartype,
                                            Direct2D1.TextAntialiasMode.Grayscale)
            }
            End Using

            ' 3) Viewport from actual backbuffer size (most reliable)
            Dim bbDesc = backBuffer.Description

            fullViewport = New osViewPort With {
                .X = 0, .Y = 0,
                .Width = Math.Max(1, CSng(bbDesc.Width)),
                .Height = Math.Max(1, CSng(bbDesc.Height)),
                .MinDepth = 0.0F, .MaxDepth = 1.0F
            }
        End Using

        ' 4) Bind RTV + viewport for D3D path
        progContext.OutputMerger.SetTargets(progRTV)
        progContext.Rasterizer.SetViewport(fullViewport)

        ' 5) Ready for delta draws
        If scissorState Is Nothing Then CreateScissorState()

        InitColors()
    End Sub



    Private Sub CreateShadersAndPipeline()
        Dim objProgDevice = progDevice
        Dim objProgContext = progContext

        Using vsbc = ShaderBytecode.Compile(objShader_Vertex, "VSMain", "vs_5_0", ShaderFlags.OptimizationLevel3)
            Utilities.Dispose(pVS)
            pVS = New VertexShader(GraphicsHandler.pDevice, vsbc)
        End Using

        Using psbc = ShaderBytecode.Compile(objShader_Pixel, "PSMain", "ps_5_0", ShaderFlags.OptimizationLevel3)
            Utilities.Dispose(pPS)
            pPS = New PixelShader(GraphicsHandler.pDevice, psbc)
        End Using

        Utilities.Dispose(pCB)

        pCB = New Buffer(progDevice, Utilities.SizeOf(Of ProgBarCB)(),
                 ResourceUsage.Default, BindFlags.ConstantBuffer,
                 CpuAccessFlags.None, ResourceOptionFlags.None, 0)

        With objProgContext
            .InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList
            .VertexShader.Set(pVS)
            .PixelShader.Set(pPS)
            .PixelShader.SetConstantBuffer(0, pCB)

            .OutputMerger.SetTargets(progRTV)

            .Rasterizer.SetViewport(New osViewPort With {
                                        .X = 0, .Y = 0,
                                        .Width = Math.Max(1, CSng(Me.ClientSize.Width)),
                                        .Height = Math.Max(1, CSng(Me.ClientSize.Height)),
                                        .MinDepth = 0.0F, .MaxDepth = 1.0F
                                    })
        End With
    End Sub

    Private lastProgress As Single = 0.0F

    Private Sub DrawDelta(progress01 As Single)
        Dim tPrev = lastProgress
        Dim tCurr = Math.Max(0.0F, Math.Min(1.0F, progress01))
        If tPrev = tCurr Then Return

        Dim barW = ProgressTrack.GetWidth()
        Dim barH = ProgressTrack.Bottom
        progContext.Rasterizer.SetViewport(New osViewPort With {
        .X = ProgressTrack.Left, .Y = 0,
        .Width = Math.Max(1, CSng(barW)),
        .Height = Math.Max(1, CSng(barH)),
        .MinDepth = 0.0F, .MaxDepth = 1.0F
    })

        Dim left = ProgressTrack.Left
        Dim top = 0
        Dim right = left + ProgressTrack.GetWidth()
        Dim bottom = ProgressTrack.Bottom

        progContext.Rasterizer.State = scissorState
        progContext.Rasterizer.SetScissorRectangle(left, top, right, bottom)

        Dim cb As New ProgBarCB With {
            .prevValue = tPrev,
            .currValue = tCurr,
            .flags = 0.0F,
            .pcoloractive = CreateProgColor(ProgColorObj.Active),
            .pcolorbg = CreateProgColor(ProgColorObj.BackG)
        }

        With progContext
            .UpdateSubresource(cb, pCB)
            .OutputMerger.SetTargets(progRTV)
            .InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList
            .VertexShader.Set(pVS)
            .PixelShader.Set(pPS)
            .PixelShader.SetConstantBuffer(0, pCB)
            .Draw(3, 0)
        End With

        progContext.Rasterizer.State = Nothing

        RestoreFullViewport()
        lastProgress = tCurr
    End Sub

    Public Sub DisplayMsg(txtMsg As String, pType As TriggerType)
        ProgressText = New ProgressMsg(txtMsg, pType, progDwriteFactory, progMsg_Config)

        DisplayProgressText = True

        progTarget.BeginDraw()

        If DisplayProgressText Then
            progTarget.DrawText(ProgressText.MsgText, ProgressText.Format,
                                ProgressText.Location, progBrush_Text, DrawTextOptions.Clip)
        End If

        progTarget.EndDraw()
        progSwapChain.Present(1, PresentFlags.None)
    End Sub

    Public Sub ClearMsg()
        ProgressText = Nothing
        DisplayProgressText = False

        progTarget.BeginDraw()
        progTarget.FillRectangle(ProgressTrack, progBrush_BG)

        progTarget.EndDraw()
        progSwapChain.Present(1, PresentFlags.None)
    End Sub

    Private Sub ResizeSwapChain(w As Integer, h As Integer)
        If w <= 0 OrElse h <= 0 Then Return
        progRTV.SafeDispose()
        progTarget.SafeDispose()

        progSwapChain.ResizeBuffers(2, w, h, SharpDX.DXGI.Format.B8G8R8A8_UNorm, SwapChainFlags.None)
        CreateTargetResources()
    End Sub

    Private Sub CreateSizeDependentResources(width As Integer, height As Integer)
        ' nothing extra needed; swapchain size already set in init
    End Sub

    Private Sub ObjectDump(Optional fullDump As Boolean = False)
        ' 0) Ensure no D2D draw is open
        Try
            If progTarget IsNot Nothing Then
                progTarget.Flush() ' D2D: flush batched draws; safe even if nothing pending
            End If
        Catch
            ' ignore
        End Try
        progMsg_Config.SafeDispose()
        progBrush_Text.SafeDispose()
        progBrush_BG.SafeDispose()
        progBrush_Active.SafeDispose()
        progTarget.SafeDispose()
        pCB.SafeDispose()
        pPS.SafeDispose()
        pVS.SafeDispose()

        ' 3) Unbind RTV before resize/dispose to break references from the device context
        If progContext IsNot Nothing Then
            progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))
        End If

        ' 4) RTV
        progRTV.SafeDispose()

        ' 5) If requested, also release swapchains and pacing handle
        If fullDump Then
            If progSwapChain2 IsNot Nothing Then
                frameLatencyEvent = IntPtr.Zero
                progSwapChain2.SafeDispose()
            End If
            progSwapChain.SafeDispose()
        End If

        ' 6) Tell the driver to retire any outstanding allocations
        If progContext IsNot Nothing Then
            progContext.ClearState()
            progContext.Flush()
        End If

        ' 7) DXGI trim (helps claw back a few hundred KB held by the WDDM cache)
        Try
            Using dxgiDev3 = progDevice.QueryInterface(Of SharpDX.DXGI.Device3)()
                dxgiDev3.Trim()
            End Using
        Catch
            ' Device3 not available (older OS) — ignore
        End Try

        ' 8) Let SharpDX finalizers run now (native COM release)
        GC.Collect()
        GC.WaitForPendingFinalizers()
    End Sub

    Private Sub ResizeTargets(newW As Integer, newH As Integer)
        If progSwapChain Is Nothing Then Return

        ' Unbind current RTV
        progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))

        progRTV.SafeDispose()
        progTarget.SafeDispose()

        ' Resize swapchain buffers
        progSwapChain.ResizeBuffers(0, newW, newH, SharpDX.DXGI.Format.Unknown, SharpDX.DXGI.SwapChainFlags.None)

        ' Recreate RTV + D2D target (your existing CreateTargetResources does this)
        CreateTargetResources()
    End Sub

    Private Sub InitiateProgress()
        ProgressStatus = ProgStatus.Running
        ProgressClock_StartTime = Stopwatch.GetTimestamp()
    End Sub

    Private Sub ConfigureProgress(pDuration As TimeSpan, objAbortToken As CancellationToken, Optional pEasing As Func(Of Double, Double) = Nothing)
        SetProgressDuration(pDuration)
        ProgressEaseFunc = If(pEasing, Function(x) x)

        objAbortToken.Register(
            Sub()
                ProgressStatus = ProgStatus.Fail
            End Sub)
    End Sub

    Private Sub SetProgressDuration(pDuration As TimeSpan)
        ProgressClock_Duration = pDuration
        ProgressFuse = pDuration.TotalMilliseconds
    End Sub

    ' Private objTask_Progress As Task(Of ProgStatus)

    Public Async Function BeginProgress(pDuration As TimeSpan, objAbortToken As CancellationToken,
                                           Optional pEasing As Func(Of Double, Double) = Nothing) As Task(Of Boolean)

        ConfigureProgress(pDuration, objAbortToken, pEasing)
        InitiateProgress()

        Dim objTask_Progress = StartProgression(True)
        Dim objProgStatus = Await objTask_Progress

        SetProgressResult(objProgStatus)

        Return True
    End Function

    ' Call this when closing the window / finalizing the run
    Private Sub StopRenderLoop()
        'Try
        '    If objTask_Progress IsNot Nothing Then
        '        objTask_Progress.Wait()      ' wait for loop to exit
        '        objTask_Progress = Nothing
        '    End If
        'Catch
        '    ' swallow — we’re shutting down
        'End Try
    End Sub

    Private Sub InitColors()
        progColor_BG = ApplyColor()
        progColor_Text = ApplyColor(True)

        SetColorObj(ProgColorObj.Active, progBrush_Active)
        SetColorObj(ProgColorObj.BackG, progBrush_BG)
        SetColorObj(ProgColorObj.Msg, progBrush_Text)
    End Sub

    Private Sub SetColorObj(objColor As ProgColorObj, ByRef progObj As SolidColorBrush)
        Dim objSetColor As SolidColorBrush = Nothing

        Select Case objColor
            Case ProgColorObj.Active
                objSetColor = New SolidColorBrush(progTarget, progColor_Active)
            Case ProgColorObj.BackG
                objSetColor = New SolidColorBrush(progTarget, progColor_BG)
            Case ProgColorObj.Msg
                objSetColor = New SolidColorBrush(progTarget, progColor_Text)
        End Select

        progObj = objSetColor
    End Sub

    Private Function CreateProgColor(objColor As ProgColorObj) As osProgColor
        Select Case objColor
            Case ProgColorObj.Active
                Return New osProgColor(CalcRGB(82), CalcRGB(96), CalcRGB(117), 1.0F)
            Case ProgColorObj.BackG
                Return New osProgColor(CalcRGB(57), CalcRGB(57), CalcRGB(57), 1.0F)
            Case ProgColorObj.Msg

        End Select
    End Function

    Private Function ApplyColor(Optional isMsg As Boolean = False) As osProgColor
        Return If(isMsg, New osProgColor(0.0F, 0.0F, 0.0F, 1.0F),
            New osProgColor(57 / 255.0F, 57 / 255.0F, 57 / 255.0F, 1.0F))
    End Function

    Private Shared Function ClampInt(v As Integer, lo As Integer, hi As Integer) As Integer
        If v < lo Then Return lo
        If v > hi Then Return hi
        Return v
    End Function

    Private Sub ResetProgressTimer()
        ProgressClock_StartTime = Stopwatch.GetTimestamp()
        ProgressClock_StepInt = 1.0 / Math.Max(0.0001, ProgressFuse)

        lastProgress = 0.0F
    End Sub

    Private Sub CalculateProgress()
        Dim progDuration = (Stopwatch.GetTimestamp() - ProgressClock_StartTime) * ProgressClock_TickCnt
        Dim progVal = VerifyProgLimits(progDuration * ProgressClock_StepInt)

        ProgressValue = ProgressEaseFunc(progVal)
    End Sub

    Private Sub RenderProgress()
        Dim progDuration = (Stopwatch.GetTimestamp() - ProgressClock_StartTime) * ProgressClock_TickCnt
        Dim progVal = VerifyProgLimits(progDuration * ProgressClock_StepInt)

        ProgressValue = ProgressEaseFunc(progVal)

        progTarget.BeginDraw()
        DrawDelta(CSng(ProgressValue))
    End Sub

    Private Function VerifyProgLimits(progVal As Double) As Single
        If progVal < 0.0 Then
            Return 0.0
        ElseIf progVal > 1.0 Then
            Return 1.0
        Else
            Return progVal
        End If
    End Function

    Private Async Function StartProgression(isSnapped As Boolean) As Task(Of ProgStatus)
        progContext.ClearRenderTargetView(progRTV, progColor_BG)
        progSwapChain.Present(1, PresentFlags.None)

        ResetProgressTimer()

        Do
            If StopProgress() Then Exit Do

            If frameLatencyEvent <> IntPtr.Zero Then
                If WaitForSingleObjectEx(frameLatencyEvent, INFINITE, False) <> WAIT_OBJECT_0 Then
                    Await Task.Yield()
                End If
            Else
                Await Task.Yield()
            End If

            RenderProgress()

            If DisplayProgressText Then
                progTarget.DrawText(ProgressText.MsgText, ProgressText.Format,
                                ProgressText.Location, progBrush_Text, DrawTextOptions.Clip)
            End If

            PresentDirty()

            If ProgressValue >= 1.0F Then
                ProgressStatus = ProgStatus.Success
                Exit Do
            End If
        Loop Until ProgressStatus <> ProgStatus.Running

        Return ProgressStatus
    End Function

    Private Function StopProgress() As Boolean
        If Not ProgressStatus = ProgStatus.Running Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub SetProgressResult(progStatus As ProgStatus)
        Dim setResult = If(progStatus = ProgStatus.Success,
            ProgResult.Completed, ProgResult.Cancelled)

        Select Case setResult
            Case ProgResult.Cancelled
                RaiseEvent ProgressFail(Me, EventArgs.Empty)
            Case ProgResult.Completed
                RaiseEvent ProgressSuccess(Me, EventArgs.Empty)
        End Select

        StopRenderLoop()
    End Sub

    Public Sub DrawBG()

        With progTarget
            .BeginDraw()
            .Clear(New osProgColor(0, 0, 0, 0))

            .FillRectangle(ProgressTrack, progBrush_BG)

            .EndDraw()
        End With

        progSwapChain.Present(1, PresentFlags.None)
    End Sub

    Private Function ComputeTrackRect() As osRect.RawRectangleF
        Return New osRect.RawRectangleF(0, 0, ProgressWidth, ProgressHeight)
    End Function

    Private Function CalcRGB(cVal As Byte) As Single
        Return cVal / 255.0F
    End Function

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

    Public Sub SetProgColor(pColor As osColor.Color, Optional pUpdate As Boolean = False)
        With pColor
            progColor_Active = New osProgColor(CalcRGB(.R), CalcRGB(.G),
                                                CalcRGB(.B), 1.0F)
        End With

        If pUpdate Then
            progBrush_Active = New SolidColorBrush(progTarget, progColor_Active)

            With progTarget
                .BeginDraw()
                .FillRectangle(ComputeTrackRect(), progBrush_Active)
                .EndDraw()
            End With

            progSwapChain.Present(1, PresentFlags.None)
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

    Private Async Function AutoCast_Prep() As Task
        Await Task.Delay(20)
        DisplayMsg("Release Shift", TriggerType.AutoCast)

        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AC_Start)

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.ClrMsg)

        Await Task.Delay(375)
    End Function

    Private Sub DrawProgressFrame(progress01 As Single)
        Dim objProgContext = progContext

        Dim cb As New ProgBarCB

        With objProgContext
            .UpdateSubresource(cb, pCB)

            .OutputMerger.SetTargets(progRTV)
            .InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList
            .VertexShader.Set(pVS)
            .PixelShader.Set(pPS)
            .PixelShader.SetConstantBuffer(0, pCB)

            .ClearRenderTargetView(progRTV, progColor_Active)

            SetViewportToBar(ProgressTrack.Left, 0, ProgressTrack.GetWidth(), ProgressTrack.Bottom)

            .Draw(3, 0)
        End With
    End Sub

    ' call this each frame before Draw(3,0)
    Private Sub SetViewportToBar(x As Integer, y As Integer, w As Integer, h As Integer)
        Dim vp As New SharpDX.Mathematics.Interop.RawViewportF With {
        .X = x, .Y = y,
        .Width = Math.Max(1, CSng(w)),
        .Height = Math.Max(1, CSng(h)),
        .MinDepth = 0.0F, .MaxDepth = 1.0F
    }
        progContext.Rasterizer.SetViewport(vp)
    End Sub

    ' after drawing, restore full viewport once (e.g., on init/resize store it)
    Private Sub RestoreFullViewport()
        Dim vpFull As New SharpDX.Mathematics.Interop.RawViewportF With {
        .X = 0, .Y = 0,
        .Width = Math.Max(1, CSng(Me.ClientSize.Width)),
        .Height = Math.Max(1, CSng(Me.ClientSize.Height)),
        .MinDepth = 0.0F, .MaxDepth = 1.0F
    }
        progContext.Rasterizer.SetViewport(vpFull)
    End Sub


    Public Sub InitiateAutoCast()
        With Me
            SetProgressEvents()

            GetPosGui(progStartPos)
            Dim locProg = SetPosData(progStartPos)

            .Left = locProg.X
            .Top = locProg.Y

            .Show()
            .DrawBG()
        End With
    End Sub

    Private Sub SetProgLocation(acComplete As Boolean)
        AutoCastComplete = acComplete
    End Sub

    Public Async Function LaunchAutoCast() As Task(Of ProgResult)
        Await AutoCast_Prep()

        Try
            Await BeginProgress(osFuncLib_Progress.ProgTimeSpan,
                                           CoreDataLib.objCancelState, AddressOf EaseInOutCirc)

            Return AutoCast_HandleResult(AutoCastComplete)
        Finally
            UnsetProgressEvents()
        End Try
    End Function

    Private Sub SetAutoCastResult(acComplete As Boolean)
        AutoCastComplete = acComplete
    End Sub

    Private Sub SetProgressEvents()
        If _evProgComplete IsNot Nothing Then Exit Sub

        _evProgComplete = Sub() SetAutoCastResult(True)
        _evProgFail = Sub() SetAutoCastResult(False)

        AddHandler ProgressSuccess, _evProgComplete
        AddHandler ProgressFail, _evProgFail
    End Sub

    Private Sub UnsetProgressEvents()
        If _evProgComplete IsNot Nothing Then
            RemoveHandler ProgressSuccess, _evProgComplete
            _evProgComplete = Nothing
        End If
        If _evProgFail IsNot Nothing Then
            RemoveHandler ProgressFail, _evProgFail
            _evProgFail = Nothing
        End If
    End Sub

    Private Function AutoCast_HandleResult(acComplete As Boolean) As ProgResult
        If acComplete Then
            osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoCast,
                             ProgAction.Complete, True)
            SetProgResult(acComplete, retProgResult)
        Else
            osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoCast,
                             ProgAction.Abort, True)
            SetProgResult(acComplete, retProgResult)
        End If

        Return retProgResult
    End Function

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW Or WS_EX_TOPMOST
            cp.Style = cp.Style And Not &H8000000 ' WS_BORDER off
            Return cp
        End Get
    End Property

    <System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=False)>
    Private Shared Function WaitForSingleObjectEx(hHandle As IntPtr, dwMilliseconds As Integer, bAlertable As Boolean) As UInteger
    End Function

End Class
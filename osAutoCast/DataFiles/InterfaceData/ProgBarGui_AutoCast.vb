Imports SharpDX
Imports SharpDX.DXGI
Imports SharpDX.D3DCompiler
Imports SharpDX.Direct2D1
Imports SharpDX.Direct3D
Imports SharpDX.Direct3D11
Imports SharpDX.DirectWrite
Imports System.Threading
Imports System.Windows.Forms
Imports AlphaMode = SharpDX.Direct2D1.AlphaMode
Imports osFormat = SharpDX.DXGI.Format
Imports osPresentOpts = SharpDX.DXGI.PresentParameters
Imports osViewPort = SharpDX.Mathematics.Interop.RawViewportF
Imports osProgColor = SharpDX.Mathematics.Interop.RawColor4
Imports D2DPixelFormat = SharpDX.Direct2D1.PixelFormat
Imports osProgBuffer = SharpDX.Direct3D11.Buffer
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports osProgDeviceContext = SharpDX.Direct3D11.DeviceContext
Imports osProgFactoryD2D = SharpDX.Direct2D1.Factory
Imports osProgFactoryDW = SharpDX.DirectWrite.Factory
Imports osProgFactoryDXGI = SharpDX.DXGI.Factory
Imports osRect = SharpDX.Mathematics.Interop
Imports osDraw = System.Drawing
Imports osColor = System.Windows.Media
Imports osForms = System.Windows.Forms
Imports osAutoCast.DataTypeLib.ProgStatus
Imports osAutoCast.osFuncLib_Progress

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

    Private ProgressValue As Single = 0
    Private ProgressTrack As osRect.RawRectangleF
    Private ProgressStatus As ProgStatus
    Private ProgressEaseFunc As Func(Of Double, Double)

    Private ProgressTask As Task(Of ProgStatus)

    Private AutoCastComplete As Boolean

    Private progColor_Active As osProgColor
    Private progColor_BG As osProgColor
    Private progColor_Text As osProgColor

    Private guiColor_BG As osDraw.Color = osDraw.Color.FromArgb(57, 57, 57)

    Private _extToken As Threading.CancellationToken
    Private _extReg As Threading.CancellationTokenRegistration

    Private progStartPos As osDraw.Point

    Private lastProgress As Single = 0.0F

    Private osProgScissorState As RasterizerState
    Private osProgViewPort As osViewPort

    Private accumTex As Texture2D
    Private accumRTV As RenderTargetView

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

    Public Sub New(pW As Integer, pH As Integer, pEase As Func(Of Double, Double))
        InitializeComponent()

        FormBorderStyle = osForms.FormBorderStyle.None

        ProgressWidth = pW
        ProgressHeight = pH

        BackColor = guiColor_BG

        ProgressTrack = ComputeTrackRect()
        StartPosition = FormStartPosition.Manual

        ProgressEaseFunc = If(pEase, Function(x) x)

        GraphicsHandler.EnsureCreated()
        InitDeviceAndSwapChain()

        ProgressStatus = ProgStatus.Idle
    End Sub

    Private Sub InitDeviceAndSwapChain()
        progSwapChain1?.Dispose()
        progSwapChain1 = Nothing

        progSwapChain2?.Dispose()
        progSwapChain2 = Nothing

        progSwapChain = Nothing
        frameLatencyEvent = IntPtr.Zero

        Try
            Dim osProgSC1 As SwapChain1 = Nothing

            Using osProgFactory2 = progDxgiFactory.QueryInterface(Of SharpDX.DXGI.Factory2)()
                osProgSC1 = New SwapChain1(osProgFactory2, GraphicsHandler.pDevice,
                                               Me.Handle, New SwapChainDescription1 With {
                                                   .Width = Math.Max(1, ProgressWidth), .Height = Math.Max(1, ProgressHeight),
                                                   .Format = osFormat.B8G8R8A8_UNorm, .BufferCount = 2, .Usage = Usage.RenderTargetOutput,
                                                   .SampleDescription = New SampleDescription(1, 0), .Scaling = Scaling.None,
                                                   .SwapEffect = SwapEffect.FlipSequential, .AlphaMode = AlphaMode.Ignore, .Flags = SwapChainFlags.FrameLatencyWaitAbleObject
                                               })
            End Using

            progSwapChain1 = osProgSC1

            progSwapChain2 = osProgSC1.QueryInterface(Of SwapChain2)()
            progSwapChain2.MaximumFrameLatency = 1

            frameLatencyEvent = progSwapChain2.FrameLatencyWaitableObject
        Catch ex As Exception
            Debug.WriteLine($"[InitDeviceAndSwapChain] ")
        End Try

        If progSwapChain1 Is Nothing Then
            progSwapChain = New SwapChain(GraphicsHandler.pDxgiFactory, GraphicsHandler.pDevice,
                                          New SwapChainDescription With {
                                            .BufferCount = 2, .SwapEffect = SwapEffect.Discard,
                                            .Usage = Usage.RenderTargetOutput, .ModeDescription = SetModeDesc(),
                                            .IsWindowed = True, .OutputHandle = Me.Handle,
                                            .SampleDescription = New SampleDescription(1, 0)
                                          })
        End If

        CreateTargetResources()
        CreateShadersAndPipeline()

        If progSwapChain1 IsNot Nothing Then
            progSwapChain1.Present(1, PresentFlags.None)
        Else
            progSwapChain.Present(1, PresentFlags.None)
        End If
    End Sub

    Private Function SetModeDesc() As ModeDescription
        Return New ModeDescription(ProgressWidth, ProgressHeight,
                                   New Rational(60, 1), osFormat.B8G8R8A8_UNorm)
    End Function

    Private Sub PresentDirty(Optional isDirty As Boolean = False)
        If progSwapChain1 IsNot Nothing Then
            If isDirty Then
                Dim objPresOpts As New osPresentOpts With {
                    .DirtyRectangles = GenTrackArray()
                }

                progSwapChain1.Present(1, PresentFlags.None, objPresOpts)
            Else
                progSwapChain1.Present(1, PresentFlags.None)
            End If
        Else
            progSwapChain.Present(1, PresentFlags.None)
        End If
    End Sub

    Private Sub CreateProgScissorState()
        osProgScissorState?.Dispose()
        osProgScissorState = New RasterizerState(progDevice,
                                                 New RasterizerStateDescription With {
                                                     .CullMode = CullMode.None,
                                                     .FillMode = Direct3D11.FillMode.Solid,
                                                     .IsScissorEnabled = True
                                                 })
    End Sub

    Private Sub CreateTargetResources()
        If progContext IsNot Nothing Then
            progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))
        End If

        progRTV.SafeDispose()
        progTarget.SafeDispose()

        Using backBuffer As Texture2D = progSwapChain1.GetBackBuffer(Of Texture2D)(0)
            progRTV = New RenderTargetView(progDevice, backBuffer)

            Using dxgiSurface As Surface = backBuffer.QueryInterface(Of Surface)()
                Dim dpiX As Single = 96.0F
                Dim dpiY As Single = 96.0F

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

            Dim bbDesc = backBuffer.Description

            osProgViewPort = New osViewPort With {
                .X = 0, .Y = 0,
                .Width = Math.Max(1, CSng(bbDesc.Width)),
                .Height = Math.Max(1, CSng(bbDesc.Height)),
                .MinDepth = 0.0F, .MaxDepth = 1.0F
            }
        End Using

        accumRTV.SafeDispose()
        accumTex.SafeDispose()

        Dim bbSizeW = CInt(osProgViewPort.Width)
        Dim bbSizeH = CInt(osProgViewPort.Height)

        Dim accumDesc As New Texture2DDescription With {
            .Width = bbSizeW,
            .Height = bbSizeH,
            .MipLevels = 1,
            .ArraySize = 1,
            .Format = osFormat.B8G8R8A8_UNorm,
            .SampleDescription = New SampleDescription(1, 0),
            .Usage = ResourceUsage.Default,
            .BindFlags = BindFlags.RenderTarget Or BindFlags.ShaderResource,
            .CpuAccessFlags = CpuAccessFlags.None,
            .OptionFlags = ResourceOptionFlags.None
        }

        accumTex = New Texture2D(progDevice, accumDesc)
        accumRTV = New RenderTargetView(progDevice, accumTex)

        progContext.ClearRenderTargetView(accumRTV, progColor_BG)

        ClearAccumToBackground()

        progContext.OutputMerger.SetTargets(progRTV)
        progContext.Rasterizer.SetViewport(osProgViewPort)

        If osProgScissorState Is Nothing Then CreateProgScissorState()

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

        pCB?.Dispose()
        pCB = New osProgBuffer(progDevice, New BufferDescription With {
                                   .SizeInBytes = Utilities.SizeOf(Of ProgBarCB)(),
                                   .Usage = ResourceUsage.Dynamic,
                                   .BindFlags = BindFlags.ConstantBuffer,
                                   .CpuAccessFlags = CpuAccessFlags.Write,
                                   .OptionFlags = ResourceOptionFlags.None,
                                   .StructureByteStride = 0
                               })

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

    Private Sub DrawDeltaPrecise(progress01 As Single, vertical As Boolean)
        Dim tPrev = lastProgress
        Dim tCurr = Math.Max(0.0F, Math.Min(1.0F, progress01))

        If tPrev = tCurr Then Return

        Dim barH = ProgressTrack.Bottom
        Dim barW = ProgressTrack.GetWidth()
        If barW <= 0 Then Return

        progContext.Rasterizer.SetViewport(New osViewPort With {
                                               .X = ProgressTrack.Left, .Y = 0,
                                               .Width = Math.Max(1, CSng(barW)),
                                               .Height = Math.Max(1, CSng(barH)),
                                               .MinDepth = 0.0F, .MaxDepth = 1.0F
                                           })

        With ProgressTrack
            progContext.Rasterizer.State = osProgScissorState
            progContext.Rasterizer.SetScissorRectangle(.Left, .Top, .Right, .Bottom)
        End With

        With progContext
            Dim box = .MapSubresource(pCB, 0, MapMode.WriteDiscard,
                                             Direct3D11.MapFlags.None)
            Utilities.Write(box.DataPointer,
                        GetProgCB(tPrev, tCurr))

            .UnmapSubresource(pCB, 0)

            .InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList

            .VertexShader.Set(pVS)
            .PixelShader.Set(pPS)
            .PixelShader.SetConstantBuffer(0, pCB)

            .Draw(3, 0)
        End With

        progContext.Rasterizer.State = Nothing
        lastProgress = tCurr
    End Sub

    Private Sub PrepFullProg()
        Dim barH = ProgressTrack.Bottom
        Dim barW = ProgressTrack.GetWidth()
        If barW <= 0 Then Return

        progContext.Rasterizer.SetViewport(New osViewPort With {
                                               .X = ProgressTrack.Left, .Y = 0,
                                               .Width = Math.Max(1, CSng(barW)),
                                               .Height = Math.Max(1, CSng(barH)),
                                               .MinDepth = 0.0F, .MaxDepth = 1.0F
                                           })

        With ProgressTrack
            progContext.Rasterizer.State = osProgScissorState
            progContext.Rasterizer.SetScissorRectangle(.Left, .Top, .Right, .Bottom)
        End With

        Dim box = progContext.MapSubresource(pCB, 0, MapMode.WriteDiscard, Direct3D11.MapFlags.None)
        Dim a = GetProgCB(0.0F, 1.0F)

        Utilities.Write(box.DataPointer, GetProgCB(0.0F, 1.0F))
        progContext.UnmapSubresource(pCB, 0)

        With progContext
            .InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList
            .VertexShader.Set(pVS)
            .PixelShader.Set(pPS)
            .PixelShader.SetConstantBuffer(0, pCB)
            .Draw(3, 0)
        End With

        progContext.Rasterizer.State = Nothing
        lastProgress = 1.0F
    End Sub

    Public Sub DisplayMsg(txtMsg As String, pType As TriggerType)
        ProgressText = New ProgressMsg(txtMsg, pType,
                                       progDwriteFactory, progMsg_Config)

        DisplayProgressText = True

        progTarget.BeginDraw()

        If DisplayProgressText Then
            progTarget.DrawText(ProgressText.MsgText, ProgressText.Format,
                                ProgressText.Location, progBrush_Text, DrawTextOptions.Clip)
        End If

        progTarget.EndDraw()
        If progSwapChain1 IsNot Nothing Then
            progSwapChain1.Present(1, PresentFlags.None)
        Else
            progSwapChain.Present(1, PresentFlags.None)
        End If
    End Sub

    Public Sub PrepareMsg(txtMsg As String, pType As TriggerType)
        ProgressText = New ProgressMsg(txtMsg, pType,
                                       progDwriteFactory, progMsg_Config)

        DisplayProgressText = True
    End Sub

    Public Sub ClearMsg()
        ProgressText = Nothing
        DisplayProgressText = False

        progTarget.BeginDraw()
        progTarget.FillRectangle(ProgressTrack, progBrush_BG)

        progTarget.EndDraw()

        If progSwapChain1 IsNot Nothing Then
            progSwapChain1.Present(1, PresentFlags.None)
        Else
            progSwapChain.Present(1, PresentFlags.None)
        End If
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
        Try
            If progTarget IsNot Nothing Then
                progTarget.Flush()
            End If
        Catch ex As Exception
            Debug.WriteLine($"[ObjectDump] {ex.Source}")

        End Try

        progMsg_Config.SafeDispose()
        progBrush_Text.SafeDispose()
        progBrush_BG.SafeDispose()
        progBrush_Active.SafeDispose()
        progTarget.SafeDispose()
        pCB.SafeDispose()
        pPS.SafeDispose()
        pVS.SafeDispose()

        If progContext IsNot Nothing Then
            progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))
        End If

        progRTV.SafeDispose()

        If fullDump Then
            If progSwapChain2 IsNot Nothing Then
                frameLatencyEvent = IntPtr.Zero
                progSwapChain2.SafeDispose()
            End If

            If progSwapChain1 IsNot Nothing Then
                progSwapChain1.SafeDispose()
            End If

            progSwapChain.SafeDispose()
        End If

        If progContext IsNot Nothing Then
            progContext.ClearState()
            progContext.Flush()
        End If

        Try
            Using dxgiDev3 = progDevice.QueryInterface(Of SharpDX.DXGI.Device3)()
                dxgiDev3.Trim()
            End Using
        Catch ex As Exception
            Debug.WriteLine($"[ObjectDump2] {ex.Source}")

        End Try

        GC.Collect()
        GC.WaitForPendingFinalizers()
    End Sub

    Private Sub InitiateProgress()
        ProgressStatus = ProgStatus.Running
        ProgressClock_StartTime = Stopwatch.GetTimestamp()
    End Sub

    ' Private ProgressTaskSrc As TaskCompletionSource(Of Boolean)

    Private Sub ConfigureProgress(pDuration As TimeSpan, objAbortToken As CancellationTokenSource)
        SetProgressDuration(pDuration)
        _extReg.Dispose()

        ' Use the external token directly
        _extToken = objAbortToken.Token
        _extReg = _extToken.Register(Sub() ProgressStatus = ProgStatus.Fail)

        ' Honor “already cancelled” immediately
        If _extToken.IsCancellationRequested Then
            ProgressStatus = ProgStatus.Fail
        End If
    End Sub

    Private Sub SetProgressDuration(pDuration As TimeSpan)
        ProgressClock_Duration = pDuration
        ProgressFuse = pDuration.TotalMilliseconds
    End Sub

    Public Async Function BeginProgress(pDuration As TimeSpan, objAbortToken As CancellationTokenSource) As Task(Of Boolean)

        ConfigureProgress(pDuration, objAbortToken)
        InitiateProgress()

        ProgressTask = StartProgression(True)
        Dim objProgStatus = Await ProgressTask

        SetProgressResult(objProgStatus)

        Return True
    End Function

    Private Sub ProgressTask_Stop()
        Try
            If ProgressTask IsNot Nothing Then
                ProgressTask.Wait()
                ProgressTask = Nothing
            End If
        Catch

        End Try
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
                Dim a = New osProgColor(CalcRGB(82), CalcRGB(96), CalcRGB(117), 1.0F)
                Return New osProgColor(CalcRGB(82), CalcRGB(96), CalcRGB(117), 1.0F)
            Case ProgColorObj.BackG
                Return New osProgColor(CalcRGB(57), CalcRGB(57), CalcRGB(57), 1.0F)
            Case ProgColorObj.Msg

        End Select
    End Function

    Private Sub ClearAccumToBackground()
        If progContext Is Nothing OrElse accumRTV Is Nothing Then Exit Sub

        progContext.OutputMerger.SetTargets(accumRTV)
        progContext.ClearRenderTargetView(accumRTV, CreateProgColor(ProgColorObj.BackG))
    End Sub

    Private Function ApplyColor(Optional isMsg As Boolean = False) As osProgColor
        Return If(isMsg, New osProgColor(0.0F, 0.0F, 0.0F, 1.0F),
            New osProgColor(57 / 255.0F, 57 / 255.0F, 57 / 255.0F, 1.0F))
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
        CalculateProgress()

        progContext.OutputMerger.SetTargets(accumRTV)
        DrawDeltaPrecise(CSng(ProgressValue), vertical:=False)

        ' 2) copy accumulation → back-buffer  (source, destination)
        progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))
        Using bb As Texture2D = If(progSwapChain1 IsNot Nothing,
                           progSwapChain1.GetBackBuffer(Of Texture2D)(0),
                           progSwapChain.GetBackBuffer(Of Texture2D)(0))
            progContext.CopyResource(accumTex, bb)   ' ← correct (src: accumTex → dst: backbuffer)
        End Using

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

        ResetProgressTimer()
        ClearAccumToBackground()

        Do
            Dim objTask_ProgStatus = ProgStatusCheck()
            Dim objProgStatus = Await objTask_ProgStatus

            If Not objProgStatus Then Exit Do

            RenderProgress()
            PresentDirty(True)

            If ProgressValue >= 1.0F Then
                ProgressStatus = ProgStatus.Success
                Exit Do
            End If
        Loop Until ProgressStatus <> ProgStatus.Running

        Return ProgressStatus
    End Function

    Private Async Function ProgStatusCheck() As Task(Of Boolean)
        If StopProgress() Then Return False

        If frameLatencyEvent <> IntPtr.Zero Then
            Dim signaled = (WaitForSingleObjectEx(frameLatencyEvent, 0, False) = WAIT_OBJECT_0)
            If Not signaled Then
                Await Task.Delay(1).ConfigureAwait(True)
            End If
        Else
            Await Task.Yield()
        End If

        If StopProgress() Then Return False

        Return True
    End Function

    Private Function StopProgress() As Boolean
        If StatusCheck() Then
            ProgressStatus = ProgStatus.Fail
            Return True
        Else
            Return False
        End If
    End Function

    Private Function StatusCheck() As Boolean
        If ProgressStatus <> ProgStatus.Running OrElse _extToken.IsCancellationRequested Then
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

        ProgressTask_Stop()
    End Sub

    Public Sub DrawBG()
        With progTarget
            .BeginDraw()
            .Clear(progColor_BG)
            .EndDraw()
        End With

        progSwapChain1.Present(1, PresentFlags.None)
    End Sub

    Private Function ComputeTrackRect() As osRect.RawRectangleF
        Return New osRect.RawRectangleF(0, 0, ProgressWidth, ProgressHeight)
    End Function

    Private Function ComputeDirtyRect() As osRect.RawRectangle
        Return New osRect.RawRectangle(0, 0, ProgressWidth, ProgressHeight)
    End Function

    Private Function GenTrackArray() As osRect.RawRectangle()
        Return New osRect.RawRectangle() {ComputeDirtyRect()}
    End Function

    Private Function CalcRGB(cVal As Byte) As Single
        Return cVal / 255.0F
    End Function

    Private Function GetProgCB(valPrev As Single, valCurrent As Single) As ProgBarCB
        Return New ProgBarCB With {
            .prevValue = valPrev,
            .currValue = valCurrent,
            .invSize = 1.0F / (ProgressTrack.Right - ProgressTrack.Left),
            .flags = 0.0F,
            .pcoloractive = progColor_Active,
            .pcolorbg = progColor_BG,
            .barOffset = ProgressTrack.Left,
            .pad0 = 0.0F, .pad1 = 0.0F, .pad2 = 0.0F
        }
    End Function

    Public Sub PerformProgressEvent(doEvent As ProgressEventData)
        Select Case doEvent.evType
            Case ProgEvent.Reset
                'ResetProgress(doEvent.evTrigger)
            Case ProgEvent.MaxFill
              '  DisplayMaxVal()
            Case ProgEvent.DispMsg
                DisplayMsg(doEvent.evDispMsg, doEvent.evTrigger)
            Case ProgEvent.ClrMsg
                ClearMsg()
            Case ProgEvent.PrepMsg
                ClearMsg()
            Case ProgEvent.ShowFullMsg
                DispProgressFull()
                DisplayMsg(doEvent.evDispMsg, doEvent.evTrigger)
        End Select
    End Sub

    Private Sub DispProgressFull()
        With progContext
            .OutputMerger.SetTargets(accumRTV)

            PrepFullProg()

            .OutputMerger.SetTargets(CType(Nothing, RenderTargetView))
            Using bb As Texture2D = If(progSwapChain1 IsNot Nothing,
                progSwapChain1.GetBackBuffer(Of Texture2D)(0),
                progSwapChain.GetBackBuffer(Of Texture2D)(0))

                .CopyResource(accumTex, bb)
            End Using
        End With

        '   PresentDirty(True)
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

            If progSwapChain1 IsNot Nothing Then
                progSwapChain1.Present(1, PresentFlags.None)
            Else
                progSwapChain.Present(1, PresentFlags.None)
            End If
        End If
    End Sub

    Private Async Function AutoCast_Prep() As Task
        DisplayMsg("Release Shift", TriggerType.AutoCast)

        Await Task.Delay(10)
        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AC_Start)

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.ClrMsg)

        Await Task.Delay(375)
    End Function

    Private Sub SetViewportToBar(x As Integer, y As Integer, w As Integer, h As Integer)
        Dim vp As New SharpDX.Mathematics.Interop.RawViewportF With {
        .X = x, .Y = y,
        .Width = Math.Max(1, CSng(w)),
        .Height = Math.Max(1, CSng(h)),
        .MinDepth = 0.0F, .MaxDepth = 1.0F
    }
        progContext.Rasterizer.SetViewport(vp)
    End Sub

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

    ' optional: keep to dispose later if you want
    Private _cancelReg As Threading.CancellationTokenRegistration
    Private _cancelRegSrc As Threading.CancellationTokenRegistration


    Private Sub SetProgLocation(acComplete As Boolean)
        AutoCastComplete = acComplete
    End Sub

    Public Async Function LaunchAutoCast() As Task(Of ProgResult)
        Await AutoCast_Prep()

        Try

            Await BeginProgress(osFuncLib_Progress.ProgTimeSpan, CoreDataLib.chkActionAbort)

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
                             ProgAction.Complete)
            SetProgResult(acComplete, retProgResult)
        Else
            osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoCast,
                             ProgAction.Abort)
            SetProgResult(acComplete, retProgResult)
        End If

        Return retProgResult
    End Function

    Private Sub SetProgResult(pResult As Boolean, ByRef setProgResult As ProgResult)
        setProgResult = If(pResult, ProgResult.Completed,
            ProgResult.Cancelled)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        TopMost = True
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        ObjectDump(True)
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
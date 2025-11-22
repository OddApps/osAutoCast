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
Imports D2DPixelFormat = SharpDX.Direct2D1.PixelFormat
Imports osFormat = SharpDX.DXGI.Format
Imports osCullMode = SharpDX.Direct3D11.CullMode
Imports osFillMode = SharpDX.Direct3D11.FillMode
Imports osBlendOpts = SharpDX.Direct3D11.BlendOption
Imports osBlendOperation = SharpDX.Direct3D11.BlendOperation
Imports osColorMaskFlags = SharpDX.Direct3D11.ColorWriteMaskFlags
Imports osRenderBlendOpts = SharpDX.Direct3D11.RenderTargetBlendDescription
Imports osBlendStateDesc = SharpDX.Direct3D11.BlendStateDescription
Imports osPresentOpts = SharpDX.DXGI.PresentParameters
Imports osDepthWriteMask = SharpDX.Direct3D11.DepthWriteMask
Imports osDepthComparison = SharpDX.Direct3D11.Comparison
Imports osViewPort = SharpDX.Mathematics.Interop.RawViewportF
Imports osProgColor = SharpDX.Mathematics.Interop.RawColor4
Imports osDepthStencilStateDesc = SharpDX.Direct3D11.DepthStencilStateDescription
Imports osRasterizerState = SharpDX.Direct3D11.RasterizerState
Imports osDepthStencilState = SharpDX.Direct3D11.DepthStencilState
Imports osProgBuffer = SharpDX.Direct3D11.Buffer
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports osProgBlendState = SharpDX.Direct3D11.BlendState
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
Imports System.ComponentModel
Imports System.Reflection
Imports osAutoCast.osHandler_Shader

Public Class ProgBarGui_AutoCast

#Region "Variables"

    Private Const WAIT_OBJECT_0 As UInteger = 0UI

    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80
    Private Const WS_EX_TOPMOST As Integer = &H8

    Private progSwapChain As SwapChain
    Private progSwapChain1 As SwapChain1
    Private progSwapChain2 As SwapChain2

    Private progRTV As RenderTargetView

    Private evLatency As IntPtr = IntPtr.Zero

    Private pVS As VertexShader
    Private pPS As PixelShader
    Private pCB As osProgBuffer

    Private pIL As InputLayout
    Private pSampler As SamplerState

    Private rsScissor As osRasterizerState
    Private dsOff As osDepthStencilState

    Private bsOpaque As osProgBlendState
    Private bsOpaqueRGB As osProgBlendState
    Private bsPremul As osProgBlendState

    Private progSettingsVQ As ProgVisualQuality

    Private progTarget As RenderTarget

    Private progBrush_BG As SolidColorBrush
    Private progBrush_Active As SolidColorBrush
    Private progBrush_Text As SolidColorBrush

    Private progMsg_Config As TextFormat

    Private ProgressCompleteEvent As ManualResetEventSlim

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
    Private progColor_Clear As osProgColor

    Private guiColor_BG As osDraw.Color = osDraw.Color.FromArgb(57, 57, 57)

    Private _extToken As CancellationToken
    Private _extReg As CancellationTokenRegistration

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
            Return osHandler_Graphics.pDevice
        End Get
    End Property

    Public ReadOnly Property progContext As osProgDeviceContext
        Get
            Return osHandler_Graphics.pContext
        End Get
    End Property

    Public ReadOnly Property progDxgiFactory As osProgFactoryDXGI
        Get
            Return osHandler_Graphics.pDxgiFactory
        End Get
    End Property

    Public ReadOnly Property progD2DFactory As osProgFactoryD2D
        Get
            Return osHandler_Graphics.pD2DFactory
        End Get
    End Property

    Public ReadOnly Property progDwriteFactory As osProgFactoryDW
        Get
            Return osHandler_Graphics.pDWFactory
        End Get
    End Property

#End Region

    Public Sub New(pW As Integer, pH As Integer, pDuration As TimeSpan, pEase As Func(Of Double, Double))
        InitializeComponent(pW, pH)

        FormBorderStyle = osForms.FormBorderStyle.None

        ProgressWidth = pW
        ProgressHeight = pH

        BackColor = guiColor_BG

        ProgressTrack = ComputeTrackRect()
        StartPosition = FormStartPosition.Manual

        SetProgressDuration(pDuration)

        ProgressEaseFunc = If(pEase,
            Function(x) x)

        osHandler_Graphics.EnsureCreated()

        InitDeviceAndSwapChain()

        ProgressStatus = ProgStatus.Idle
    End Sub

    Private Sub InitDeviceAndSwapChain()
        ResetSwapChain()

        Try
            Dim osProgSC1 As SwapChain1 = Nothing

            Using osProgFactory2 = progDxgiFactory.QueryInterface(Of SharpDX.DXGI.Factory2)()
                GenerateSwapChain(osProgFactory2, osProgSC1)
            End Using

            SetSwapChain(osProgSC1)
        Catch ex As Exception
            Debug.WriteLine($"[InitDeviceAndSwapChain] ")
        End Try

        If progSwapChain1 Is Nothing Then
            progSwapChain = New SwapChain(progDxgiFactory, progDevice,
                                          GenerateSwapChainDesc(True))
        End If

        CreateTargetResources()
        CreateShadersAndPipeline()

        InitProgressStates()

        progSettingsVQ = ApplySetingsVQ()
        RenderFrame()
    End Sub

    Private Function ApplySetingsVQ() As ProgVisualQuality
        Dim objVQ = CoreDataLib.GetVisualQuality()

        Select Case objVQ
            Case ProgVisOpts.Performance
                Return New ProgVisualQuality(objVQ, bsOpaque)
            Case ProgVisOpts.Quality
                Return New ProgVisualQuality(objVQ, bsOpaqueRGB)
            Case Else
                Return Nothing
        End Select
    End Function

    Private Sub CreateTargetResources()
        ResetProgTarget()

        Using backBuffer As Texture2D = progSwapChain1.GetBackBuffer(Of Texture2D)(0)
            progRTV = New RenderTargetView(progDevice, backBuffer)

            Using dxgiSurface As Surface = backBuffer.QueryInterface(Of Surface)()

                Dim d2dPixelFmt As New D2DPixelFormat(osFormat.B8G8R8A8_UNorm, Direct2D1.AlphaMode.Ignore)

                Dim props As New RenderTargetProperties(Direct2D1.RenderTargetType.Default,
                                                        d2dPixelFmt, 96.0F, 96.0F,
                                                        Direct2D1.RenderTargetUsage.None,
                                                        Direct2D1.FeatureLevel.Level_DEFAULT)

                progTarget = New RenderTarget(progD2DFactory, dxgiSurface, props) With {
                    .AntialiasMode = Direct2D1.AntialiasMode.Aliased,
                    .TextAntialiasMode = Direct2D1.TextAntialiasMode.Cleartype
                }
            End Using

            SetProgViewPort(backBuffer)
        End Using

        accumRTV.SafeDispose()
        accumTex.SafeDispose()

        accumTex = New Texture2D(progDevice, GetTexture2D())
        accumRTV = New RenderTargetView(progDevice, accumTex)

        InitColors()

        progContext.ClearRenderTargetView(accumRTV, progColor_BG)

        ClearAccumToBackground()

        If osProgScissorState Is Nothing Then CreateProgScissorState()

    End Sub

    Private Async Sub CreateShadersAndPipeline()
        Dim objProgDevice = progDevice
        Dim objProgContext = progContext

        Dim objTask_LoadShaders = Await Task.
            WhenAll(Task.Run(Function() As Object
                                 Return FetchShader(osShaderType.sTypeVertex).sVertex
                             End Function),
                    Task.Run(Function() As Object
                                 Return FetchShader(osShaderType.sTypePixel).sPixel
                             End Function))

        pVS = objTask_LoadShaders.ToShaderVer(0)
        pPS = objTask_LoadShaders.ToShaderPx(1)

        pCB?.SafeDispose()
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
        End With

        SetViewportToBar()
    End Sub

    Private Sub InitProgressStates()

        dsOff = New osDepthStencilState(progDevice, New osDepthStencilStateDesc() With {
                                            .IsDepthEnabled = False, .IsStencilEnabled = False,
                                            .DepthWriteMask = osDepthWriteMask.Zero,
                                            .DepthComparison = osDepthComparison.Always
                                        })

        Dim bdesc = New osBlendStateDesc() With {
            .AlphaToCoverageEnable = False,
            .IndependentBlendEnable = False
        }

        bdesc.RenderTarget(0) = New osRenderBlendOpts(False, osBlendOpts.One, osBlendOpts.Zero, osBlendOperation.Add,
                                                      osBlendOpts.One, osBlendOpts.Zero, osBlendOperation.Add,
                                                      osColorMaskFlags.All)
        bsOpaque = New osProgBlendState(progDevice, bdesc)

        Dim bdescRGB = New osBlendStateDesc() With {
            .AlphaToCoverageEnable = False,
            .IndependentBlendEnable = False
        }

        bdescRGB.RenderTarget(0) = New osRenderBlendOpts(False, osBlendOpts.One, osBlendOpts.Zero, osBlendOperation.Add,
                                                         osBlendOpts.One, osBlendOpts.Zero, osBlendOperation.Add,
                                                         osColorMaskFlags.Red Or osColorMaskFlags.Green Or osColorMaskFlags.Blue)

        bsOpaqueRGB = New osProgBlendState(progDevice, bdescRGB)

        Dim pdesc = New osBlendStateDesc() With {
            .AlphaToCoverageEnable = False,
            .IndependentBlendEnable = False
        }

        pdesc.RenderTarget(0) = New osRenderBlendOpts(True, osBlendOpts.One, osBlendOpts.InverseSourceAlpha, osBlendOperation.Add,
                                                      osBlendOpts.One, osBlendOpts.InverseSourceAlpha, osBlendOperation.Add, osColorMaskFlags.All)
        bsPremul = New osProgBlendState(progDevice, pdesc)

        CreateProgScissorState()
    End Sub

    Private Sub CreateProgScissorState()
        osProgScissorState?.Dispose()
        osProgScissorState = New RasterizerState(progDevice, New RasterizerStateDescription With {
                                                     .CullMode = osCullMode.None, .FillMode = osFillMode.Solid,
                                                     .IsScissorEnabled = True, .IsMultisampleEnabled = False,
                                                     .IsAntialiasedLineEnabled = False, .DepthBias = 0,
                                                     .SlopeScaledDepthBias = 0.0F, .DepthBiasClamp = 0.0F
                                                 })
    End Sub

    Private Sub RenderFrame(Optional isDirty As Boolean = False, Optional chkAcProgress As Boolean = False)
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

        If chkAcProgress Then
            If ProgressValue >= 1.0F Then
                InvokeAutoCastSuccess()
            End If
        End If
    End Sub

    Private Sub SetProgViewPort(bBuff As Texture2D)
        Dim bbDesc = bBuff.Description

        osProgViewPort = New osViewPort With {
            .X = 0, .Y = 0,
            .Width = Math.Max(1, CSng(bbDesc.Width)),
            .Height = Math.Max(1, CSng(bbDesc.Height)),
            .MinDepth = 0.0F, .MaxDepth = 1.0F
        }
    End Sub

    Private Sub ResetProgTarget()
        If progContext IsNot Nothing Then
            progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))
        End If

        progRTV.SafeDispose()
        progTarget.SafeDispose()
    End Sub

    Private Sub CaptureTexture()
        progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))

        Using objProgTexture As Texture2D = If(progSwapChain1 IsNot Nothing,
                progSwapChain1.GetBackBuffer(Of Texture2D)(0),
                progSwapChain.GetBackBuffer(Of Texture2D)(0))

            progContext.CopyResource(accumTex, objProgTexture)
        End Using
    End Sub

    Private Function GetTexture2D() As Texture2DDescription
        Dim bbSizeW = CInt(osProgViewPort.Width)
        Dim bbSizeH = CInt(osProgViewPort.Height)

        Return New Texture2DDescription With {
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
    End Function

    Private Sub RasterizeProgress(progVal As Single)
        Dim tPrev = lastProgress
        Dim tCurr = Math.Max(0.0F, Math.Min(1.0F, progVal))

        Dim chkProgTrack = VerifyProgressTrack()

        If tPrev = tCurr OrElse chkProgTrack.pFail Then Return

        SetRasterizerState()

        With progContext

            .OutputMerger.SetDepthStencilState(dsOff)

            .OutputMerger.SetBlendState(progSettingsVQ.BlendState)

            .OutputMerger.SetTargets(accumRTV)

            Dim objMapRes = .MapSubresource(pCB, 0, MapMode.WriteDiscard,
                                             Direct3D11.MapFlags.None)
            Utilities.Write(objMapRes.DataPointer,
                        GetProgCB(tPrev, tCurr))

            .UnmapSubresource(pCB, 0)

            .VertexShader.Set(pVS)
            .PixelShader.Set(pPS)

            .VertexShader.SetConstantBuffer(0, pCB)
            .PixelShader.SetConstantBuffer(0, pCB)

            .InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList

            .Draw(3, 0)
        End With

        SetRasterizerState(True)
        lastProgress = tCurr
    End Sub

    Private Sub RasterizeProgressFull()
        With progContext
            .OutputMerger.SetTargets(accumRTV)

            Dim objMapRes = .MapSubresource(pCB, 0, MapMode.WriteDiscard,
                                             Direct3D11.MapFlags.None)
            Utilities.Write(objMapRes.DataPointer,
                        GetProgCB(0.0F, 1.0F))

            .UnmapSubresource(pCB, 0)

            .InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList

            .VertexShader.Set(pVS)
            .PixelShader.Set(pPS)
            .PixelShader.SetConstantBuffer(0, pCB)

            .Draw(3, 0)
        End With

        SetRasterizerState(True)
        lastProgress = 1.0F
    End Sub

    Private Sub DrawProgressFull()
        RasterizeProgressFull()
        CaptureTexture()
    End Sub

    Public Async Function LaunchAutoCast() As Task(Of ProgResult)
        Await AutoCast_Prep()

        Dim objGetProgRes = Await BeginProgress()
        Return ProcessResult(AutoCastComplete)
    End Function

    Private Async Function AutoCast_Prep() As Task
        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AC_Start)

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.ClrMsg)

        Await Task.Delay(375)
    End Function

    Public Async Function BeginProgress() As Task(Of Boolean)
        PrepAbortToken()
        InitiateProgress()

        ProgressTask = StartProgression()
        Dim objProgStatus = Await ProgressTask

        SetProgressResult(objProgStatus)

        Return True
    End Function

    Private Async Function StartProgression() As Task(Of ProgStatus)
        ResetProgressTimer()
        ClearAccumToBackground()

        Do
            Dim objStatusCheck = Await ValidateProgress()
            If Not objStatusCheck Then Exit Do

            DrawProgress()
            RenderFrame(True, True)
        Loop

        Return ProgressStatus
    End Function

    Private Sub InvokeAutoCastSuccess()
        ProgressStatus = ProgStatus.Success
        ProgressCompleteEvent.Set()
    End Sub

    Private Async Function ValidateProgress() As Task(Of Boolean)
        Dim objCheckResult As Boolean = True

        If evLatency <> IntPtr.Zero Then
            Dim signaled = (WaitForSingleObjectEx(evLatency, 0, False) = WAIT_OBJECT_0)
            If Not signaled Then
                Await evLatency.AwaitSignalAsync().ConfigureAwait(False)
            End If
        Else
            Await Task.Yield()
        End If

        ProgStatusCheck(objCheckResult)

        Return objCheckResult
    End Function

    Private Sub InitiateProgress()
        ProgressStatus = ProgStatus.Running
        ProgressCompleteEvent = New ManualResetEventSlim(False)

        ProgressClock_StepInt = 1.0 / ProgressFuse
        lastProgress = 0.0F
    End Sub

    Private Sub PrepAbortToken()
        _extToken = CoreDataLib.chkActionAbort.Token
        _extReg = _extToken.
            Register(Sub()
                         ProgressStatus = ProgStatus.Fail
                     End Sub)
    End Sub

    Private Sub SetProgressDuration(pDuration As TimeSpan)
        ProgressClock_Duration = pDuration
        ProgressFuse = pDuration.TotalMilliseconds
    End Sub

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

    Private Sub TerminateProgressTask()
        If ProgressTask IsNot Nothing Then
            ProgressTask.Wait()
            ProgressTask = Nothing
        End If
    End Sub

    Private Sub ResetProgressTimer()
        ProgressClock_StartTime = Stopwatch.GetTimestamp()
    End Sub

    Private Sub CalculateProgress()
        Dim progDuration = (Stopwatch.GetTimestamp() - ProgressClock_StartTime) * ProgressClock_TickCnt
        Dim progVal = VerifyProgLimits(progDuration * ProgressClock_StepInt)

        ProgressValue = ProgressEaseFunc(progVal)
    End Sub

    Private Sub DrawProgress()
        CalculateProgress()

        RasterizeProgress(CSng(ProgressValue))
        CaptureTexture()
    End Sub

    Private Function ProcessResult(acComplete As Boolean) As ProgResult
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

    Private Function VerifyProgLimits(progVal As Double) As Single
        If progVal < 0.0 Then
            Return 0.0
        ElseIf progVal > 1.0 Then
            Return 1.0
        Else
            Return progVal
        End If
    End Function

    Private Sub ProgStatusCheck(ByRef chkResult As Boolean)
        If VerifyProgStatus() Then
            chkResult = True
        Else
            If _extToken.IsCancellationRequested Then ProgressStatus = ProgStatus.Fail
            chkResult = False
        End If
    End Sub

    Private Function VerifyProgStatus() As Boolean
        If ProgressStatus <> ProgStatus.Running OrElse
            _extToken.IsCancellationRequested OrElse ProgressCompleteEvent.IsSet Then
            Return False
        Else
            Return True
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

        TerminateProgressTask()
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
            .invSize = 1.0F / ProgressTrack.GetWidth(),
            .flags = progSettingsVQ.Flag,
            .pcoloractive = progColor_Active,
            .pcolorbg = progColor_BG,
            .barOffset = ProgressTrack.Left,
            .pad0 = 0.0F, .pad1 = 0.0F, .pad2 = 0.0F
        }
    End Function

    Public Sub PerformProgressEvent(doEvent As ProgressEventData)
        Select Case doEvent.evType
            Case ProgEvent.DispMsg
                DisplayMsg(doEvent.evDispMsg, doEvent.evTrigger)
            Case ProgEvent.ClrMsg
                ClearMsg()
            Case ProgEvent.PrepMsg
                ClearMsg()
            Case ProgEvent.ShowFullMsg
                DrawProgressFull()
                DisplayMsg(doEvent.evDispMsg, doEvent.evTrigger)
        End Select
    End Sub

    Public Sub InitMsg(txtMsg As String, pType As TriggerType)
        ProgressText = New ProgressMsg(txtMsg, pType,
                                       progDwriteFactory, progMsg_Config)
        DisplayProgressText = True
    End Sub

    Public Sub DrawMsg()
        With progTarget
            .BeginDraw()

            If DisplayProgressText Then
                .DrawText(ProgressText.MsgText, ProgressText.Format,
                          ProgressText.Location, progBrush_Text, DrawTextOptions.Clip)
            End If

            .EndDraw()
        End With
    End Sub

    Public Sub DisplayMsg(txtMsg As String, pType As TriggerType)
        InitMsg(txtMsg, pType)

        DrawMsg()
        RenderFrame()
    End Sub

    Public Sub ShowFirstLoad()
        ProgressText = New ProgressMsg("Release Shift", TriggerType.AutoCast,
                                       progDwriteFactory, progMsg_Config)

        DisplayProgressText = True

        progTarget.BeginDraw()

        progTarget.FillRectangle(ProgressTrack, progBrush_BG)

        If DisplayProgressText Then
            progTarget.DrawText(ProgressText.MsgText, ProgressText.Format,
                                ProgressText.Location, progBrush_Text, DrawTextOptions.Clip)
        End If

        progTarget.EndDraw()

        RenderFrame()
    End Sub

    Public Sub ClearMsg()
        ProgressText = Nothing
        DisplayProgressText = False

        With progTarget
            .BeginDraw()
            .FillRectangle(ProgressTrack, progBrush_BG)
            .EndDraw()
        End With

        RenderFrame()
    End Sub

    Public Sub SetProgColor(pColor As osColor.Color, Optional pUpdate As Boolean = False)
        With pColor
            progColor_Active = New osProgColor(CalcRGB(.R),
                                               CalcRGB(.G),
                                               CalcRGB(.B),
                                               1.0F)
        End With

        If pUpdate Then
            progBrush_Active = New SolidColorBrush(progTarget, progColor_Active)

            With progTarget
                .BeginDraw()
                .FillRectangle(ComputeTrackRect(), progBrush_Active)
                .EndDraw()
            End With

            RenderFrame()
        End If
    End Sub

    Private Sub SetViewportToBar()
        progContext.Rasterizer.SetViewport(New osViewPort With {
                                               .X = ProgressTrack.Left, .Y = 0,
                                               .Width = Math.Max(1, ProgressTrack.GetWidth()),
                                               .Height = Math.Max(1, ProgressTrack.Bottom),
                                               .MinDepth = 0.0F, .MaxDepth = 1.0F
                                           })

        SetRasterizerState()
    End Sub

    Private Sub SetViewportToBar(pTrackW As Single)
        progContext.Rasterizer.SetViewport(New osViewPort With {
                                               .X = ProgressTrack.Left, .Y = 0,
                                               .Width = Math.Max(1, pTrackW),
                                               .Height = Math.Max(1, ProgressTrack.Bottom),
                                               .MinDepth = 0.0F, .MaxDepth = 1.0F
                                           })

        SetRasterizerState()
    End Sub

    Private Sub SetRasterizerState(Optional doClear As Boolean = False)
        Dim pFeather As Integer = 1

        With ProgressTrack
            If doClear Then
                progContext.Rasterizer.State = Nothing
            Else
                progContext.Rasterizer.State = osProgScissorState
                progContext.Rasterizer.SetScissorRectangle(.Left - pFeather, .Top - pFeather,
                                                           .Right + pFeather, .Bottom + pFeather)
            End If
        End With
    End Sub

    Public Sub InitiateAutoCast()
        With Me
            SetProgressEvents()

            GetPosGui(progStartPos)
            Dim locProg = SetPosData(progStartPos)

            .Left = locProg.X
            .Top = locProg.Y

            ShowFirstLoad()

            .Show()
        End With
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

        _extReg.Dispose()
        ProgressCompleteEvent.Dispose()
    End Sub

    Private Sub GenerateSwapChain(objProgFactory As DXGI.Factory2, ByRef objSwapChain As SwapChain1)
        objSwapChain = New SwapChain1(objProgFactory, progDevice,
                                      Me.Handle, GenerateSwapChainDesc())
    End Sub

    Private Function GenerateSwapChainDesc() As SwapChainDescription1
        Return New SwapChainDescription1 With {
            .Width = Math.Max(1, ProgressWidth), .Height = Math.Max(1, ProgressHeight), .BufferCount = 2,
            .Format = osFormat.B8G8R8A8_UNorm, .Usage = Usage.RenderTargetOutput, .Scaling = Scaling.None,
            .SampleDescription = New SampleDescription(1, 0), .SwapEffect = SwapEffect.FlipSequential,
            .AlphaMode = AlphaMode.Ignore, .Flags = SwapChainFlags.FrameLatencyWaitAbleObject
        }
    End Function

    Private Function GenerateSwapChainDesc(isSC As Boolean) As SwapChainDescription
        Return New SwapChainDescription With {
            .BufferCount = 2, .SwapEffect = SwapEffect.Discard,
            .Usage = Usage.RenderTargetOutput, .ModeDescription = SetModeDesc(),
            .IsWindowed = True, .OutputHandle = Me.Handle,
            .SampleDescription = New SampleDescription(1, 0)
        }
    End Function

    Private Sub SetSwapChain(objSwapChain As SwapChain1)
        progSwapChain1 = objSwapChain
        progSwapChain2 = objSwapChain.QueryInterface(Of SwapChain2)()

        evLatency = progSwapChain2.FrameLatencyWaitableObject
    End Sub

    Private Sub ResetSwapChain()
        progSwapChain1?.Dispose()
        progSwapChain1 = Nothing

        progSwapChain2?.Dispose()
        progSwapChain2 = Nothing

        progSwapChain = Nothing
        evLatency = IntPtr.Zero
    End Sub

    Private Function SetModeDesc() As ModeDescription
        Return New ModeDescription(ProgressWidth, ProgressHeight,
                                   New Rational(60, 1), osFormat.B8G8R8A8_UNorm)
    End Function

    Private Function VerifyProgressTrack() As (pFail As Boolean, pWidth As Single)
        Dim pW As Single = ProgressTrack.GetWidth()
        Return (pW <= 0, pW)
    End Function

    Private Sub InitColors()
        ApplyColor(ProgColorObj.BackG, progColor_BG)
        ApplyColor(ProgColorObj.Msg, progColor_Text)
        ApplyColor(ProgColorObj.Clear, progColor_Clear)

        SetColorObj(ProgColorObj.Active, progBrush_Active)
        SetColorObj(ProgColorObj.BackG, progBrush_BG)
        SetColorObj(ProgColorObj.Msg, progBrush_Text)
    End Sub

    Private Sub SetColorObj(objColor As ProgColorObj, ByRef progObj As SolidColorBrush)
        Select Case objColor
            Case ProgColorObj.Active
                progObj = New SolidColorBrush(progTarget, progColor_Active)
            Case ProgColorObj.BackG
                progObj = New SolidColorBrush(progTarget, progColor_BG)
            Case ProgColorObj.Msg
                progObj = New SolidColorBrush(progTarget, progColor_Text)
        End Select
    End Sub

    Private Sub ClearAccumToBackground()
        If progContext Is Nothing OrElse accumRTV Is Nothing Then Exit Sub

        progContext.OutputMerger.SetTargets(accumRTV)
        progContext.ClearRenderTargetView(accumRTV, progColor_BG)
    End Sub

    Private Sub ApplyColor(pColorObj As ProgColorObj, ByRef objColor As osProgColor)
        Select Case pColorObj
            Case ProgColorObj.Msg
                objColor = New osProgColor(0.0F, 0.0F, 0.0F, 1.0F)
            Case ProgColorObj.BackG
                objColor = New osProgColor(CalcRGB(57), CalcRGB(57), CalcRGB(57), 1.0F)
            Case ProgColorObj.Msg
                objColor = New osProgColor(CalcRGB(0), CalcRGB(0), CalcRGB(0), 1.0F)
            Case ProgColorObj.Clear
                objColor = New osProgColor(0.0F, 0.0F, 0.0F, 0.0F)
        End Select
    End Sub

    Private Sub ObjectDump(Optional fullDump As Boolean = False)
        progMsg_Config.SafeDispose()
        progBrush_Text.SafeDispose()
        progBrush_BG.SafeDispose()
        progBrush_Active.SafeDispose()
        progTarget.SafeDispose()

        'pCB.SafeDispose()
        'pVS.SafeDispose()

        If progContext IsNot Nothing Then
            progContext.OutputMerger.SetTargets(CType(Nothing, RenderTargetView))
        End If

        progRTV.SafeDispose()

        If fullDump Then
            If progSwapChain2 IsNot Nothing Then
                evLatency = IntPtr.Zero
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

        bsPremul?.Dispose()
        bsOpaqueRGB?.Dispose()
        bsOpaque?.Dispose()
        dsOff?.Dispose()
        rsScissor?.Dispose()

        progSettingsVQ = Nothing

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

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        TopMost = True
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        ObjectDump(True)
    End Sub

    Protected Overrides Sub OnClosing(e As CancelEventArgs)
        MyBase.OnFormClosing(e)
        UnsetProgressEvents()
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

    Private Sub ProgBarGui_AutoCast_Load(sender As Object, e As EventArgs)

    End Sub
End Class
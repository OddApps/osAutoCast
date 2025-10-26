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
Imports osProgFactoryD2D = SharpDX.Direct2D1.Factory
Imports osProgFactoryDW = SharpDX.DirectWrite.Factory
Imports osProgFactoryDXGI = SharpDX.DXGI.Factory
Imports osFormat = SharpDX.DXGI.Format
Imports AlphaMode = SharpDX.Direct2D1.AlphaMode
Imports D2DPixelFormat = SharpDX.Direct2D1.PixelFormat
Imports MapFlags = SharpDX.Direct3D11.MapFlags
Imports osAutoCast.DataTypeLib.ProgStatus
Imports osRect = SharpDX.Mathematics.Interop
Imports osDraw = System.Drawing
Imports osText = SharpDX.DirectWrite
Imports osColor = System.Windows.Media
Imports osForms = System.Windows.Forms
Imports SharpDX.D3DCompiler

Public Class ProgBarGui_AutoCast

    Private progSwapChain2 As SwapChain2        ' set when Flip-Model is used
    Private frameLatencyEvent As IntPtr = IntPtr.Zero

    ' WaitForSingleObjectEx
    <System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=False)>
    Private Shared Function WaitForSingleObjectEx(hHandle As IntPtr, dwMilliseconds As Integer, bAlertable As Boolean) As UInteger
    End Function
    Private Const WAIT_OBJECT_0 As UInteger = 0UI
    Private Const INFINITE As Integer = -1

    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80
    Private Const WS_EX_TRANSPARENT As Integer = &H20
    Private Const WS_EX_TOPMOST As Integer = &H8

    Private progSwapChain As SwapChain
    Private progRTV As RenderTargetView

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

    Private progTarget As RenderTarget

    Private progBrush_BG As SolidColorBrush
    Private progBrush_Active As SolidColorBrush
    Private progBrush_Text As SolidColorBrush

    Private progMsg_Config As TextFormat

    Public Event ProgressSuccess As EventHandler
    Public Event ProgressFail As EventHandler

    Private ProgressText As ProgressMsg

    Private ProgressFuse As Double
    Private ProgressDuration As TimeSpan
    Private ProgressStartTime As Long

    Private ProgressTrack As osRect.RawRectangleF
    Private ProgressValue As Double = 0
    Private ProgressStatus As ProgStatus
    Private ProgressEaseFunc As Func(Of Double, Double)

    Public Property BarCornerRadius As Single = 0.0F

    Private progColor_Active As osRect.RawColor4
    Private progColor_BG As osRect.RawColor4
    Private progColor_Text As osRect.RawColor4

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
        ObjectDump()
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        InitDeviceAndSwapChain()
        CreateSizeDependentResources(ProgressWidth, ProgressHeight)

        ProgressStatus = ProgStatus.Idle
    End Sub

    Private Sub InitDeviceAndSwapChain()
        Dim w = ProgressWidth
        Dim h = ProgressHeight

        If w <= 0 OrElse h <= 0 Then
            w = Math.Max(1, Me.ClientSize.Width)
            h = Math.Max(1, Me.ClientSize.Height)
        End If

        progSwapChain = Nothing
        progSwapChain2 = Nothing

        frameLatencyEvent = IntPtr.Zero

        Try
            Using objFactory2 = progDxgiFactory.QueryInterface(Of SharpDX.DXGI.Factory2)()
                Dim objSwapChainDesc = New SwapChainDescription1 With {
                    .Width = w, .Height = h,
                    .Format = osFormat.B8G8R8A8_UNorm,
                    .BufferCount = 2,
                    .Usage = Usage.RenderTargetOutput,
                    .SampleDescription = New SampleDescription(1, 0),
                    .Scaling = Scaling.Stretch,
                    .SwapEffect = SwapEffect.FlipDiscard,
                    .AlphaMode = AlphaMode.Ignore
                }

                Dim objSwapChain = New SwapChain1(objFactory2, progDevice, Me.Handle, objSwapChainDesc)

                progSwapChain = objSwapChain.QueryInterface(Of SwapChain)()

                progSwapChain2 = objSwapChain.QueryInterface(Of SwapChain2)()
                progSwapChain2.MaximumFrameLatency = 1

                frameLatencyEvent = progSwapChain2.FrameLatencyWaitableObject

                objSwapChain.Dispose()
            End Using
        Catch

        End Try

        If progSwapChain Is Nothing Then
            Dim scDesc = New SwapChainDescription With {
                .BufferCount = 2,
                .ModeDescription = New ModeDescription(w, h, New Rational(60, 1), osFormat.B8G8R8A8_UNorm),
                .IsWindowed = True,
                .OutputHandle = Me.Handle,
                .SampleDescription = New SampleDescription(1, 0),
                .SwapEffect = SwapEffect.Discard,
                .Usage = Usage.RenderTargetOutput
            }

            progSwapChain = New SwapChain(progDxgiFactory, progDevice, scDesc)
        End If

        CreateTargetResources()
        DrawBG()
    End Sub

    Private Sub CreateTargetResources()
        ' ObjectDump()

        Using backBuffer As Texture2D = progSwapChain.GetBackBuffer(Of Texture2D)(0)
            progRTV = New RenderTargetView(progDevice, backBuffer)

            Using dxgiSurface As Surface = backBuffer.QueryInterface(Of Surface)()
                Dim props = New RenderTargetProperties(RenderTargetType.Default,
                    New D2DPixelFormat(osFormat.B8G8R8A8_UNorm, AlphaMode.Ignore),
                    96.0F, 96.0F, RenderTargetUsage.None, Direct2D1.FeatureLevel.Level_DEFAULT)

                progTarget = New RenderTarget(progD2DFactory, dxgiSurface, props) With {
                    .AntialiasMode = AntialiasMode.Aliased,
                    .TextAntialiasMode = Direct2D1.TextAntialiasMode.Cleartype
                }
            End Using
        End Using

        InitColors()
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
        progRTV.SafeDispose()
        progTarget.SafeDispose()
        progBrush_BG.SafeDispose()
        progBrush_Active.SafeDispose()
        progBrush_Text.SafeDispose()
        progMsg_Config.SafeDispose()

        GraphicsHandler.FlushDevice() ' ClearState + Flush on the shared context

        If fullDump Then
            If progSwapChain2 IsNot Nothing Then
                frameLatencyEvent = IntPtr.Zero
                progSwapChain2.SafeDispose()
                progSwapChain2 = Nothing
            End If
            progSwapChain.SafeDispose()
            progSwapChain = Nothing
        End If

        GC.Collect()
        GC.WaitForPendingFinalizers()
    End Sub

    'Private Sub ObjectDump(Optional fullDump As Boolean = False)
    '    progRTV.SafeDispose()
    '    progTarget.SafeDispose()
    '    progBrush_BG.SafeDispose()
    '    progBrush_Active.SafeDispose()
    '    progBrush_Text.SafeDispose()
    '    progMsg_Config.SafeDispose()

    '    GraphicsHandler.FlushDevice()

    '    If fullDump Then
    '        progSwapChain.SafeDispose()
    '    End If

    '    GC.Collect()
    '    GC.WaitForPendingFinalizers()
    'End Sub

    Private Sub InitiateProgress()
        ProgressStatus = ProgStatus.Running
        ProgressStartTime = Stopwatch.GetTimestamp()
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
        ProgressDuration = pDuration
        ProgressFuse = pDuration.TotalMilliseconds
    End Sub

    Public Async Function BeginProgress(pDuration As TimeSpan, objAbortToken As CancellationToken,
                                           Optional pEasing As Func(Of Double, Double) = Nothing) As Task(Of Boolean)

        ConfigureProgress(pDuration, objAbortToken, pEasing)
        InitiateProgress()

        Dim objTask_Progress = StartProgression(True)
        Dim objProgStatus = Await objTask_Progress

        SetProgressResult(objProgStatus)

        Return True
    End Function

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

    Private Function ApplyColor(Optional isMsg As Boolean = False) As osRect.RawColor4
        Return If(isMsg, New osRect.RawColor4(0.0F, 0.0F, 0.0F, 1.0F),
            New osRect.RawColor4(57 / 255.0F, 57 / 255.0F, 57 / 255.0F, 1.0F))
    End Function

    Private Sub CalculateProgress()
        Dim progDuration = (Stopwatch.GetTimestamp() - ProgressStartTime) * 1000.0 / Stopwatch.Frequency
        Dim progVal = Math.Max(0.0, Math.Min(1.0, progDuration / ProgressFuse))

        ProgressValue = ProgressEaseFunc(progVal)
    End Sub

    Private Shared Function ClampInt(v As Integer, lo As Integer, hi As Integer) As Integer
        If v < lo Then Return lo
        If v > hi Then Return hi
        Return v
    End Function

    Private Async Function StartProgression(isSnapped As Boolean) As Task(Of ProgStatus)
        Do
            If StopProgress() Then Exit Do
            CalculateProgress()

            If frameLatencyEvent <> IntPtr.Zero Then
                If WaitForSingleObjectEx(frameLatencyEvent, INFINITE, False) <> WAIT_OBJECT_0 Then
                    Await Task.Yield()
                End If
            Else
                Await Task.Yield()
            End If

            Dim curFillPx As Integer = ClampInt(Math.Floor(ProgressValue * ProgressTrack.GetWidth() + 0.000001),
                                            0, ProgressTrack.GetWidth())

            progTarget.BeginDraw()

            If curFillPx <> _lastFillPx Then
                Dim x1 As Integer = ProgressTrack.Left + Math.Min(_lastFillPx, curFillPx)
                Dim x2 As Integer = ProgressTrack.Left + Math.Max(_lastFillPx, curFillPx)
                Dim strip As New osRect.RawRectangleF(x1, 0, x2 - x1, ProgressTrack.Bottom)
                progTarget.FillRectangle(strip, progBrush_Active)
                _lastFillPx = curFillPx
            End If

            If DisplayProgressText Then
                progTarget.DrawText(ProgressText.MsgText, ProgressText.Format,
                                ProgressText.Location, progBrush_Text, DrawTextOptions.Clip)
            End If

            progTarget.EndDraw()
            progSwapChain.Present(1, PresentFlags.None)

            If ProgressValue >= 1 Then
                ProgressStatus = Success
                Exit Do
            End If
        Loop Until Not ProgressStatus = ProgStatus.Running

        Return ProgressStatus
    End Function

    'Private Async Function StartProgression(isSnapped As Boolean) As Task(Of ProgStatus)

    '    Do
    '        If StopProgress() Then Exit Do

    '        CalculateProgress()

    '        Dim curFillPx As Integer = ClampInt(Math.Floor(ProgressValue * ProgressTrack.GetWidth() + 0.000001),
    '                                            0, ProgressTrack.GetWidth())

    '        progTarget.BeginDraw()

    '        If curFillPx <> _lastFillPx Then
    '            Dim x1 As Integer = ProgressTrack.Left + Math.Min(_lastFillPx, curFillPx)
    '            Dim x2 As Integer = ProgressTrack.Left + Math.Max(_lastFillPx, curFillPx)

    '            Dim strip As New osRect.RawRectangleF(x1, 0, x2 - x1, ProgressTrack.Bottom)

    '            progTarget.FillRectangle(strip, progBrush_Active)
    '            _lastFillPx = curFillPx
    '        End If

    '        If DisplayProgressText Then
    '            progTarget.DrawText(ProgressText.MsgText, ProgressText.Format,
    '                                ProgressText.Location, progBrush_Text, DrawTextOptions.Clip)
    '        End If

    '        progTarget.EndDraw()

    '        progSwapChain.Present(1, PresentFlags.None)

    '        If ProgressValue >= 1 Then
    '            ProgressStatus = Success
    '            Exit Do
    '        End If

    '        Await Task.Yield()
    '    Loop Until Not ProgressStatus = ProgStatus.Running

    '    Return ProgressStatus

    'End Function


    Private Async Function StartProgression() As Task(Of ProgStatus)

        Do
            If StopProgress() Then Exit Do

            CalculateProgress()

            Dim curFillPx As Single = Math.Round(ProgressValue * ProgressTrack.GetWidth(), 3)

            progTarget.BeginDraw()

            If curFillPx <> _lastFillPx Then
                Dim x1 As Single = ProgressTrack.Left + Math.Min(_lastFillPx, curFillPx)
                Dim x2 As Single = ProgressTrack.Left + Math.Max(_lastFillPx, curFillPx)

                Dim strip As New osRect.RawRectangleF(x1, 0, x2 - x1, ProgressTrack.Bottom)

                progTarget.FillRectangle(strip, progBrush_Active)
                _lastFillPx = curFillPx
            End If

            If DisplayProgressText Then
                progTarget.DrawText(ProgressText.MsgText, ProgressText.Format, ProgressText.Location, progBrush_Text,
                                DrawTextOptions.Clip)
            End If

            progTarget.EndDraw()
            ' Only present when something changed (reduces CPU)
            progSwapChain.Present(1, PresentFlags.None)

            If ProgressValue >= 1 Then
                ProgressStatus = Success
                Exit Do
            End If

            ' keep UI responsive; 1 ms is usually fine. Use Yield() if you want “as fast as possible”.
            Await Task.Yield()
        Loop Until Not ProgressStatus = ProgStatus.Running

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
    End Sub

    Public Sub DrawBG()

        With progTarget
            .BeginDraw()
            .Clear(New osRect.RawColor4(0, 0, 0, 0))

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
            progColor_Active = New osRect.RawColor4(CalcRGB(.R), CalcRGB(.G),
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

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW Or WS_EX_TOPMOST
            cp.Style = cp.Style And Not &H8000000 ' WS_BORDER off
            Return cp
        End Get
    End Property

    Private Async Function AutoCast_Prep() As Task
        Await Task.Delay(10)
        DisplayMsg("Release Shift", TriggerType.AutoCast)

        Await CoreDataLib.InputMonSvc.AnticipateInput(InputAction.AC_Start)

        CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.ClrMsg)

        Await Task.Delay(375)
    End Function

    Private acProgLoc As osDraw.Point

    Private AutoCastComplete As Boolean

    Public Sub InitiateAutoCast()
        With Me
            SetProgressEvents()

            GetPosGui(acProgLoc)
            Dim locProg = SetPosData(acProgLoc)

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
            Dim acProgTask = BeginProgress(osFuncLib_Progress.ProgTimeSpan, CoreDataLib.objCancelState)
            Await acProgTask
            Return AutoCast_HandleResult(AutoCastComplete)
        Finally
            UnsetProgressEvents()             ' <— important
        End Try
    End Function

    Private Sub SetAutoCastResult(acComplete As Boolean)
        AutoCastComplete = acComplete
    End Sub

    Private _evProgComplete As EventHandler
    Private _evProgFail As EventHandler

    Private Sub SetProgressEvents()
        ' If already wired for this acProgressGui, bail
        If _evProgComplete IsNot Nothing Then Exit Sub

        _evProgComplete = Sub() SetAutoCastResult(True)
        _evProgFail = Sub() SetAutoCastResult(False)

        AddHandler ProgressSuccess, _evProgComplete
        AddHandler ProgressFail, _evProgFail
    End Sub

    ' Call once when you’re done (end of LaunchAutoCast / right before disposing GUI):
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

    Private retProgResult As ProgResult = Nothing

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

    Private Sub ProgBarGui_AutoCast_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed

    End Sub
End Class
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Threading
Imports System.Diagnostics
Imports System.Threading
Imports SharpDX.Direct3D9
Imports System.Windows.Interop
Imports System.Drawing
Imports osRect = SharpDX.Mathematics.Interop
Imports osDraw = System.Drawing
Imports osColor = System.Windows.Media

Public Class OddLib_ProgressBar
    Inherits FrameworkElement

#Region "Fields / State"

    Private ProgRenderBitmap As RenderTargetBitmap
    Private ProgRenderSurface As DrawingVisual

    Private MsgRenderBitmap As RenderTargetBitmap
    Private MsgOverlaySurface As DrawingVisual

    Private _pixelWidth As Integer
    Private _pixelHeight As Integer

    Private objEdge_Prev As Integer
    Private isPendingDraw As Boolean

    Private isMsgDisplayed As Boolean = False

    Private ProgGraphic_Full As Drawing = Nothing
    Private _backgroundDrawing As Drawing = Nothing

    Private _pendingPrime As Boolean

    Public Event ProgressComplete As EventHandler
    Public Event ProgressFailed As EventHandler

    Private objProgValData As ProgressValData

    Private AutoResetProgress As Boolean
    Private ProgressDuration As TimeSpan
    Private ProgressWatch As Stopwatch = Nothing
    Private ProgressTimer As DispatcherTimer = Nothing
    Private ProgressEaseFunc As Func(Of Double, Double) = Nothing
    Private ProgressTaskSrc As TaskCompletionSource(Of Boolean) = Nothing

    Public Property BarBrush As osColor.Brush = New SolidColorBrush(osColor.Color.FromRgb(0, 200, 0)).FreezeReturn()
    Public Property BackBrush As osColor.Brush = New SolidColorBrush(osColor.Color.FromRgb(57, 57, 57)).FreezeReturn()

    Private bgBrushColor As osColor.Color = osColor.Color.FromRgb(57, 57, 57)

    Private ProgD3D As Direct3DEx
    Private ProgD3D_Device As DeviceEx
    Private ProgD3D_Surface As Surface        ' The Direct3D render target surface
    Private ProgD3D_Image As D3DImage         ' WPF ProgD3D_Image to host the surface

    Private surfaceWidth As Integer = 1
    Private surfaceHeight As Integer = 1

    Private osCurrent = Application.Current

    Private _startTicks As Long
    Private _durationMs As Double
    Private _lastPx As Integer = -1

    Shared Sub New()
        osFuncLib_Progress.ProgBG.Freeze()
    End Sub

    Public Sub New()
        SnapsToDevicePixels = True
        UseLayoutRounding = True

        RenderOptions.SetEdgeMode(Me, EdgeMode.Aliased)
        RenderOptions.SetBitmapScalingMode(Me, BitmapScalingMode.LowQuality)

        ImplementEvents()
    End Sub

    Public Sub PrimeFirstFrame(Optional chunk As Double? = Nothing)
        If IsPrimingSuspended Then Exit Sub

        If chunk.HasValue Then
            _progChunk = Math.Max(0.0, Math.Min(1.0, chunk.Value))
        End If

        If ValidateSize() Then Return

        ValidateProgDV()
        ValidateMsgDV()

        EnsureBitmapsSized()

        Dim curEdge = EdgeFromChunk(_progChunk)

        Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
            If IsAutoPass Then
                ProgRenderTarget.DrawRectangle(BgBrushOrDefault(), Nothing,
                                               New Rect(0, 0, _pixelWidth, _pixelHeight))
            End If

            If ValidateBrush(curEdge) Then
                ProgRenderTarget.DrawRectangle(_activeBrush, Nothing,
                                               New Rect(0, 0, curEdge, _pixelHeight))
            End If
        End Using

        ProgRenderBitmap.Render(ProgRenderSurface)
        objEdge_Prev = curEdge

        InvalidateVisual()
    End Sub

#End Region

#Region "Dependency Properties"

    Private _progress As Double
    Public Property Progress As Double
        Get
            Return _progress
        End Get
        Set(pDur As Double)
            If ProgressEaseFunc IsNot Nothing Then
                _progress = ProgressEaseFunc(pDur)
            Else
                _progress = pDur
            End If
        End Set
    End Property


    Public Shared ReadOnly ProgressValueProperty As DependencyProperty =
        DependencyProperty.Register("ProgressValue", GetType(Double), GetType(OddLib_ProgressBar),
                                    New PropertyMetadata(0.0, AddressOf OnProgressValueChanged))

    Public Property ProgressValue As Double
        Get
            Return CDbl(GetValue(ProgressValueProperty))
        End Get
        Set(value As Double)
            SetValue(ProgressValueProperty, value)
        End Set
    End Property

    Private Shared Sub OnProgressValueChanged(objDependency As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim objOsProgBar = DirectCast(objDependency, OddLib_ProgressBar)
        objOsProgBar.ProgressChunk = CDbl(e.NewValue)
    End Sub

    Public Shared ReadOnly BackgroundProperty As DependencyProperty =
        DependencyProperty.Register("Background", GetType(osColor.Brush), GetType(OddLib_ProgressBar),
                                    New FrameworkPropertyMetadata(osFuncLib_Progress.ProgBG, AddressOf OnBackgroundChanged))

    Public Property Background As osColor.Brush
        Get
            Return CType(GetValue(BackgroundProperty), osColor.Brush)
        End Get
        Set(value As osColor.Brush)
            SetValue(BackgroundProperty, value)
        End Set
    End Property

    Private Shared Sub OnBackgroundChanged(objDependency As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim objOsProg = SetOsProgObj(objDependency)
        Dim objOsProgFreeze = TryCast(e.NewValue, Freezable)

        If ValidateProgFreeze(objOsProgFreeze) Then objOsProgFreeze.Freeze()

        objOsProg.RebuildBackingBitmap(includeProgress:=True)
    End Sub

    Public Shared ReadOnly BorderBrushProperty As DependencyProperty =
        DependencyProperty.Register("BorderBrush", GetType(osColor.Brush), GetType(OddLib_ProgressBar),
                                    New FrameworkPropertyMetadata(osColor.Brushes.Black))

    Public Property BorderBrush As osColor.Brush
        Get
            Return CType(GetValue(BorderBrushProperty), osColor.Brush)
        End Get
        Set(value As osColor.Brush)
            SetValue(BorderBrushProperty, value)
        End Set
    End Property

    Public Shared ReadOnly BorderThicknessProperty As DependencyProperty =
        DependencyProperty.Register("BorderThickness", GetType(Double), GetType(OddLib_ProgressBar),
                                    New FrameworkPropertyMetadata(0.0))

    Public Property BorderThickness As Double
        Get
            Return CDbl(GetValue(BorderThicknessProperty))
        End Get
        Set(value As Double)
            SetValue(BorderThicknessProperty, value)
        End Set
    End Property

    Public Shared ReadOnly ProgressFlowProperty As DependencyProperty =
        DependencyProperty.Register(NameOf(ProgressFlow), GetType(ProgFlow), GetType(OddLib_ProgressBar),
                                    New PropertyMetadata(ProgFlow.Ascending))

    Public Property ProgressFlow As ProgFlow
        Get
            Return CType(GetValue(ProgressFlowProperty), ProgFlow)
        End Get
        Set(value As ProgFlow)
            SetValue(ProgressFlowProperty, value)
        End Set
    End Property

    Public Shared ReadOnly IsAutoPassProperty As DependencyProperty =
        DependencyProperty.Register(NameOf(IsAutoPass), GetType(Boolean), GetType(OddLib_ProgressBar),
                                    New PropertyMetadata(False, AddressOf OnIsAutoPassChanged))

    Public Property IsAutoPass As Boolean
        Get
            Return CType(GetValue(IsAutoPassProperty), Boolean)
        End Get
        Set(value As Boolean)
            SetValue(IsAutoPassProperty, value)
        End Set
    End Property

    Private Shared Sub OnIsAutoPassChanged(objDependency As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim objOsProg = SetOsProgObj(objDependency)
        objOsProg.ApplyOptionalClip()
    End Sub

    Public Shared ReadOnly MinDeltaProperty As DependencyProperty =
        DependencyProperty.Register(NameOf(MinDelta), GetType(Double), GetType(OddLib_ProgressBar),
                                    New PropertyMetadata(0.002))

    Public Property MinDelta As Double
        Get
            Return CDbl(GetValue(MinDeltaProperty))
        End Get
        Set(value As Double)
            SetValue(MinDeltaProperty, value)
        End Set
    End Property

#End Region

#Region "Object Properties"

    Private _activeBrush As osColor.Brush
    Public Property ActiveBrush As osColor.Brush
        Get
            Return _activeBrush
        End Get
        Set(value As osColor.Brush)
            Dim newBrush As osColor.Brush = If(value, osColor.Brushes.Transparent)

            Dim objFreezable = TryCast(newBrush, Freezable)
            EstablishProgFreeze(objFreezable)

            _activeBrush = newBrush

            If ValidateSize(True) Then
                PrimeFirstFrame()
            Else
                _pendingPrime = True
            End If
        End Set
    End Property

    Private _activeBrushColor As osColor.Color
    Public Property ActiveBrushColor As osColor.Color
        Get
            Return _activeBrushColor
        End Get
        Set(value As osColor.Color)
            _activeBrushColor = value
        End Set
    End Property

    Private _progressText As ProgMsg
    Public Property ProgressText As ProgMsg
        Get
            Return _progressText
        End Get
        Set(value As ProgMsg)
            _progressText = value
            InvalidateVisual()
        End Set
    End Property

    Private _displayProgressText As Boolean = False
    Public Property DisplayProgressText As Boolean
        Get
            Return _displayProgressText
        End Get
        Set(value As Boolean)
            _displayProgressText = value
        End Set
    End Property

#End Region

#Region "Progress API"

    Private _progChunk As Double
    Public Property ProgressChunk As Double
        Get
            Return _progChunk
        End Get
        Set(value As Double)
            Dim valProgress = ProcessProgress(value)

            PrimeIfReady()

            If ValidateProgress(valProgress) Then Return

            With CalcProgEdge(valProgress, True)
                If ValidateProgBitmap() Then
                    InvalidateVisual()
                    Return
                End If

                RequestDeltaDraw(.EdgeOld, .EdgeNew)
            End With
        End Set
    End Property

#End Region

#Region "Delta Rendering"

    Private Sub RequestDeltaDraw(oldEdge As Integer, newEdge As Integer)
        If oldEdge = newEdge Then Return

        Dim objEdgeData As New ProgEdgeObj(oldEdge, newEdge,
                                            _activeBrush, BgBrushOrDefault())

        Dim objRect = GenProgRect(objEdgeData.EdgeX, 0, objEdgeData.EdgeW, _pixelHeight)

        If isPendingDraw Then
            objEdge_Prev = newEdge
            Return
        End If

        isPendingDraw = True
        objEdge_Prev = newEdge

        RunOnUI(Sub()
                    Try
                        ValidateProgDV()
                        ValidateMsgDV()
                        EnsureBitmapsSized()

                        Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
                            ProgRenderTarget.DrawRectangle(objEdgeData.EdgeBrush, Nothing, objRect)
                        End Using

                        ProgRenderBitmap.Render(ProgRenderSurface)
                        InvalidateVisual()
                    Finally
                        isPendingDraw = False
                    End Try
                End Sub)
    End Sub

#End Region

#Region "Reset / Events / Messages"
    Private _suspendPrimeDepth As Integer = 0

    Private Sub SuspendPriming()
        _suspendPrimeDepth += 1
    End Sub

    Private Sub ResumePriming()
        If _suspendPrimeDepth > 0 Then _suspendPrimeDepth -= 1
    End Sub

    Private ReadOnly Property IsPrimingSuspended As Boolean
        Get
            Return _suspendPrimeDepth > 0
        End Get
    End Property

    Public Sub PerformProgressEvent(doEvent As ProgressEventData)
        Select Case doEvent.evType
            Case ProgEvent.Reset
                ResetProgress(doEvent.evTrigger)
            Case ProgEvent.MaxFill
                DisplayMaxVal()
            Case ProgEvent.DispMsg
                DisplayMsg(doEvent.evDispMsg, doEvent.evTrigger)
            Case ProgEvent.ClrMsg
                ClearMsg(doEvent.evTrigger)
        End Select
    End Sub

    Private Sub ResetProgress(Optional apReset As TriggerType = False)
        Dim chkApReset As Boolean = apReset = TriggerType.AutoPass

        If chkApReset Then
            _progChunk = 1.0
        Else
            _progChunk = 0.0
        End If

        ClearMsg()

        objEdge_Prev = EdgeFromChunk(_progChunk)

        _backgroundDrawing = Nothing
        ProgGraphic_Full = Nothing

        RebuildBackingBitmap(includeProgress:=True)
    End Sub

    Private Sub DisplayMaxVal()
        PrimeIfReady()

        If ValidateProgBitmap() Then Return

        Dim oldEdge As Integer = objEdge_Prev
        Dim newEdge As Integer = _pixelWidth

        If newEdge > oldEdge Then
            RequestDeltaDraw(oldEdge, newEdge)
        End If

        _progChunk = 1.0
        objEdge_Prev = newEdge
    End Sub

    Private Sub DisplayMsg(txtMsg As String, pType As TriggerType)
        SuspendPriming()

        If pType = TriggerType.AutoCast Then
            DisplayProgressText = True
            ProgressText = New ProgMsg(txtMsg, pType, False)
        Else

            Try
                AllocDispatcher().
                    BeginInvoke(DispatcherPriority.Render,
                                ComposeMsgRender(MsgRenderType.msgDisplay, txtMsg, pType))
            Finally

            End Try
        End If

        ResumePriming()
    End Sub

    Private Sub ClearMsg(Optional pType As TriggerType = Nothing)
        SuspendPriming()

        DisplayProgressText = False

        Try
            AllocDispatcher().
                BeginInvoke(DispatcherPriority.Render,
                            ComposeMsgRender(MsgRenderType.msgClear))
        Finally
            ResumePriming()
        End Try
    End Sub

    Private Sub RenderMsgContainer(pDC As DrawingContext)
        pDC.DrawRectangle(_activeBrush, Nothing,
                          New Rect(0, 0, _pixelWidth, _pixelHeight))
        isMsgDisplayed = False
    End Sub

#End Region

#Region "Rendering Surface / Layout"

    Private Sub RebuildBackingBitmap(Optional includeProgress As Boolean = False)

        If IsPrimingSuspended Then Exit Sub

        If ValidateSize() Then
            InvalidateVisual()
            Return
        End If

        VerifyBitmapSize(RenderBitmapObj.Progress)

        Dim curEdge = EdgeFromChunk(_progChunk)

        RunOnUI(Sub()
                    ValidateProgDV()
                    ValidateMsgDV()

                    EnsureBitmapsSized()

                    Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
                        If IsAutoPass Then
                            ProgRenderTarget.DrawRectangle(BgBrushOrDefault(), Nothing,
                                                       New Rect(0, 0, _pixelWidth, _pixelHeight))
                        End If

                        If ValidateProgEdge(includeProgress, curEdge) Then
                            ProgRenderTarget.DrawRectangle(_activeBrush, Nothing,
                                                           New Rect(0, 0, curEdge, _pixelHeight))
                        End If
                    End Using

                    ProgRenderBitmap.Render(ProgRenderSurface)
                    objEdge_Prev = curEdge
                    InvalidateVisual()
                End Sub)
    End Sub

    Private Sub ResetMsgBitmap()
        If ValidateSize() Then Return
        MsgRenderBitmap = New RenderTargetBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32)
    End Sub

    Private _lastFilledWidth As Integer = -1
    Private _lastWPixels As Integer = 0
    Private _lastHPixels As Integer = 0

    Protected Overrides Sub OnRender(progDC As DrawingContext)
        MyBase.OnRender(progDC)

        If IsAutoPass Then
            If ProgRenderBitmap IsNot Nothing Then
                progDC.DrawImage(ProgRenderBitmap, New Rect(0, 0, ActualWidth, ActualHeight))
            Else
                If IsAutoPass Then
                    progDC.DrawRectangle(BgBrushOrDefault(), Nothing, New Rect(0, 0, ActualWidth, ActualHeight))
                End If
            End If

            If MsgRenderBitmap IsNot Nothing AndAlso isMsgDisplayed Then
                progDC.DrawImage(MsgRenderBitmap, New Rect(0, 0, ActualWidth, ActualHeight))
            End If

            If BorderThickness > 0 AndAlso BorderBrush IsNot Nothing Then
                Dim objPen = ApplyPen(BorderBrush, BorderThickness)
                Dim objFreeze = TryCast(objPen, Freezable)

                EstablishProgFreeze(objFreeze)

                progDC.DrawRectangle(Nothing, objPen, New Rect(0.5, 0.5, Math.Max(0, ActualWidth - 1), Math.Max(0, ActualHeight - 1)))
            End If
        Else
            If ProgD3D_Image Is Nothing OrElse ProgD3D_Surface Is Nothing Then
                progDC.DrawRectangle(BackBrush, Nothing, GenProgRect(0, 0, ActualWidth, ActualHeight))

                Dim filledPixels = CInt(Math.Round(ActualWidth * Progress))
                If filledPixels > 0 Then
                    progDC.DrawRectangle(BarBrush, Nothing,
                                     New Rect(If(ProgressFlow = ProgFlow.Descending, ActualWidth - filledPixels, 0),
                                              0, filledPixels, ActualHeight))
                End If

                Return
            End If

            ' Use Direct3D to render the progress bar content
            Dim wPixels As Integer = CInt(Math.Max(1, Math.Round(ActualWidth)))
            Dim hPixels As Integer = CInt(Math.Max(1, Math.Round(ActualHeight)))

            Dim fillWidth As Integer = CInt(Math.Round(wPixels * Progress))

            ' If first frame or size changed -> full prime (clear + draw current), then track baseline
            ProgD3D_Image.Lock()
            ProgD3D_Device.SetRenderTarget(0, ProgD3D_Surface)

            Dim firstOrResized As Boolean = (_lastFilledWidth < 0) OrElse (wPixels <> _lastWPixels) OrElse (hPixels <> _lastHPixels)

            If firstOrResized Then
                ' Full background once
                DrawBackground()

                ' Draw current fill once (respect flow)
                If fillWidth > 0 Then
                    ' Left-anchored: [0, fillWidth)
                    Dim rx = New SharpDX.Mathematics.Interop.RawRectangle(0, 0, fillWidth, hPixels)

                    DrawProgress(rx)
                    ProgD3D_Image.AddDirtyRect(New Int32Rect(0, 0, fillWidth, hPixels))
                Else
                    ' nothing filled, but background changed
                    ProgD3D_Image.AddDirtyRect(New Int32Rect(0, 0, wPixels, hPixels))
                End If

                _lastFilledWidth = fillWidth
                _lastWPixels = wPixels
                _lastHPixels = hPixels

                ProgD3D_Image.Unlock()
                progDC.DrawImage(ProgD3D_Image, New Rect(0, 0, ActualWidth, ActualHeight))

                If DisplayProgressText Then
                    With ProgressText
                        progDC.DrawText(.txtComposed, .txtLocation)
                    End With
                End If
                Return
            End If

            If Progress >= 1 Then
                DrawProgress(True)
                ProgD3D_Image.AddDirtyRect(New Int32Rect(0, 0, ActualWidth, hPixels))
            Else
                If fillWidth <> _lastFilledWidth Then

                    Dim x1 As Integer = Math.Min(_lastFilledWidth, fillWidth)
                    Dim x2 As Integer = Math.Max(_lastFilledWidth, fillWidth)

                    Dim wDelta As Integer = x2 - x1

                    If wDelta > 0 Then
                        DrawProgress(x1, x2)
                        ProgD3D_Image.AddDirtyRect(New Int32Rect(x1, 0, wDelta, hPixels))
                    End If

                    _lastFilledWidth = fillWidth
                End If
            End If

            ProgD3D_Image.Unlock()

            ' Draw the updated D3DImage onto the WPF surface
            progDC.DrawImage(ProgD3D_Image, New Rect(0, 0, ActualWidth, ActualHeight))
        End If

        If DisplayProgressText Then
            With ProgressText
                progDC.DrawText(.txtComposed, .txtLocation)
            End With
        End If
    End Sub

    Private Function RawBG() As SharpDX.Mathematics.Interop.RawColorBGRA
        With bgBrushColor
            Return New SharpDX.Mathematics.Interop.RawColorBGRA(.R, .G, .B, .A)
        End With
    End Function

    Private Sub ImplementEvents()

        'AddHandler Me.Loaded,
        '   Sub()
        '       InitDrawProgress()
        '       ResizeSurface()
        '   End Sub

        AddHandler Me.Loaded,
                  Sub()
                      SetSizeData()

                      ApplyOptionalClip()
                      ValidateProgDV()
                      ValidateMsgDV()
                      EnsureBitmapsSized()

                      Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
                          If IsAutoPass Then
                              ProgRenderTarget.DrawRectangle(BgBrushOrDefault(), Nothing,
                                                             New Rect(0, 0, _pixelWidth, _pixelHeight))
                          End If

                          Dim curEdge = EdgeFromChunk(_progChunk)

                          If ValidateBrush(curEdge) Then
                              ProgRenderTarget.DrawRectangle(_activeBrush, Nothing,
                                                             New Rect(0, 0, curEdge, _pixelHeight))
                          End If
                      End Using

                      ProgRenderBitmap.Render(ProgRenderSurface)
                      objEdge_Prev = EdgeFromChunk(_progChunk)

                      InvalidateVisual()
                  End Sub

        AddHandler Me.Loaded,
                   Sub()
                       If _pendingPrime Then
                           _pendingPrime = False
                           PrimeFirstFrame()
                       End If
                   End Sub

        AddHandler Me.Loaded,
                   Sub()
                       If _pixelWidth > 0 Then
                           CalcMinDelta(IsGpuOptimized())
                       End If
                   End Sub

        AddHandler Me.SizeChanged,
                   Sub()
                       If ValidatePendingPrime() Then
                           _pendingPrime = False
                           PrimeFirstFrame()
                       End If
                       'If ProgD3D_Device IsNot Nothing Then
                       '    ResizeSurface()
                       'End If
                   End Sub

        'AddHandler Me.Unloaded, Sub()
        '                            ProgressBarUnload()
        '                        End Sub

    End Sub

    Private Sub ResizeSurface()
        surfaceWidth = Math.Max(1, CInt(Math.Ceiling(ActualWidth)))
        surfaceHeight = Math.Max(1, CInt(Math.Ceiling(ActualHeight)))

        ProgD3D_Surface?.Dispose()
        ProgD3D_Surface = Surface.CreateRenderTarget(ProgD3D_Device, surfaceWidth, surfaceHeight,
                                                Format.A8R8G8B8, MultisampleType.None, 0, True)

        ProgD3D_Image.Lock()
        ProgD3D_Image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, ProgD3D_Surface.NativePointer)
        ProgD3D_Image.Unlock()

        InvalidateVisual()
    End Sub

    Private Sub OnBackBufferAvailableChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        If ProgD3D_Image.IsFrontBufferAvailable Then
            ProgD3D_Image.Lock()
            ProgD3D_Image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, ProgD3D_Surface.NativePointer)
            ProgD3D_Image.Unlock()
            Me.InvalidateVisual()  ' re-render to ensure content is up to date
        End If
    End Sub

    Private Sub ProgressBarUnload()
        If ProgD3D_Surface IsNot Nothing Then ProgD3D_Surface.Dispose()
        If ProgD3D_Device IsNot Nothing Then ProgD3D_Device.Dispose()
        If ProgD3D IsNot Nothing Then ProgD3D.Dispose()
    End Sub

    Private Sub CreateOrResizeSurface(width As Integer, height As Integer)
        If width < 1 OrElse height < 1 Then Exit Sub  ' no surface if control not yet properly sized

        ' Dispose old surface if exists
        If ProgD3D_Surface IsNot Nothing Then ProgD3D_Surface.Dispose()

        ProgD3D_Surface = Surface.CreateRenderTarget(ProgD3D_Device, width, height,
                                                Format.A8R8G8B8, MultisampleType.None,
                                                0, True)
        ' Create a lockable render target surface for ProgD3D_Image (A8R8G8B8 format)
        ' Attach the new surface to the ProgD3D_Image
        ProgD3D_Image.Lock()
        ProgD3D_Image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, ProgD3D_Surface.NativePointer)
        ProgD3D_Image.Unlock()
    End Sub

    Private Function GenRawRect(pRight As Integer, pBottom As Integer, Optional pValue As Integer = 0) As osRect.RawRectangle
        Return New osRect.RawRectangle(pValue, 0, pRight, pBottom)
    End Function

    Private Sub DrawProgress(isFull As Boolean)
        With ActiveBrushColor
            Dim objProgColor = New osRect.RawColorBGRA(.B, .G, .R, .A)
            ProgD3D_Device.ColorFill(ProgD3D_Surface, New osRect.RawRectangle(0, 0, ActualWidth, ActualHeight), objProgColor)
        End With
    End Sub

    Private Sub DrawProgress(pStart As Integer, pVal As Integer)
        With ActiveBrushColor
            Dim objProgColor = New osRect.RawColorBGRA(.B, .G, .R, .A)
            ProgD3D_Device.ColorFill(ProgD3D_Surface, New osRect.RawRectangle(pStart, 0, pVal, ActualHeight), objProgColor)
        End With
    End Sub

    Private Sub DrawProgress(pRec As osRect.RawRectangle)
        With ActiveBrushColor
            Dim objProgColor = New osRect.RawColorBGRA(.B, .G, .R, .A)
            ProgD3D_Device.ColorFill(ProgD3D_Surface, pRec, objProgColor)
        End With
    End Sub

    Private Sub DrawBackground()
        With bgBrushColor
            Dim objBgColor = New osRect.RawColorBGRA(.B, .G, .R, .A)
            ProgD3D_Device.Clear(ClearFlags.Target, objBgColor, 1.0F, 0)
        End With
    End Sub

    Private Sub DrawBackground(isNew As Boolean)
        With bgBrushColor
            Dim objBgColor = New osRect.RawColorBGRA(.B, .G, .R, .A)
            ProgD3D_Device.ColorFill(ProgD3D_Surface, Nothing, objBgColor)
        End With
    End Sub

    Protected Overrides Sub OnRenderSizeChanged(sizeInfo As SizeChangedInfo)
        MyBase.OnRenderSizeChanged(sizeInfo)

        If IsAutoPass Then
            If ActualWidth > 0 AndAlso ActualHeight > 0 Then
                SetSizeData()

                Me.MinDelta = 1.0 / _pixelWidth

                ApplyOptionalClip()

                ValidateProgDV()
                ValidateMsgDV()
                EnsureBitmapsSized()

                RebuildBackingBitmap(includeProgress:=True)


            End If
        Else
            If ProgD3D_Image Is Nothing OrElse ProgD3D_Device Is Nothing Then Exit Sub

            Dim w As Integer = CInt(Math.Max(1, Math.Round(ActualWidth)))
            Dim h As Integer = CInt(Math.Max(1, Math.Round(ActualHeight)))

            ' Recreate render target to match new size
            If ProgD3D_Surface IsNot Nothing Then ProgD3D_Surface.Dispose()

            ProgD3D_Surface = SharpDX.Direct3D9.Surface.CreateRenderTarget(
        ProgD3D_Device, w, h,
        SharpDX.Direct3D9.Format.A8R8G8B8,
        SharpDX.Direct3D9.MultisampleType.None, 0, True)

            ' Attach new surface to the D3DImage
            ProgD3D_Image.Lock()
            ProgD3D_Image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, ProgD3D_Surface.NativePointer)

            ' Prime once: clear BG, then draw current fill (so next frames can delta-draw)
            ProgD3D_Device.SetRenderTarget(0, ProgD3D_Surface)
            DrawBackground()

            Dim fillW As Integer = Math.Max(0, Math.Min(w, CInt(Math.Round(w * Progress))))

            If fillW > 0 Then
                DrawProgress(New SharpDX.Mathematics.Interop.RawRectangle(0, 0, fillW, h))
            End If

            ' Mark updated region (whole surface is fine on size change)
            ProgD3D_Image.AddDirtyRect(New Int32Rect(0, 0, w, h))
            ProgD3D_Image.Unlock()

            ' Reset delta trackers so next OnRender only paints the small strip
            _lastFilledWidth = fillW
        End If


        If isMsgDisplayed Then
            ClearMsg()
        End If
    End Sub

#End Region

#Region "Helpers you already had (kept, trimmed to match new pipeline)"


    Public Function InitiateProgress(pDuration As TimeSpan, objAbortToken As CancellationToken,
                                     Optional pEasing As Func(Of Double, Double) = Nothing) As Task(Of Boolean)

        PrepProgressTask(pDuration, pEasing, ProgressTaskSrc)
        PrepProgressHandlers(objAbortToken)

        Return ProgressTaskSrc.Task
    End Function

    Private Sub PrepProgressHandlers(objAbortToken As CancellationToken)
        objAbortToken.Register(
            Sub()
                SetProgressResult(ProgResult.Cancelled)
            End Sub)

        AddHandler CompositionTarget.Rendering, AddressOf OnFramer
    End Sub

    Private Sub PrepProgressTask(pDuration As TimeSpan, pEasing As Func(Of Double, Double),
                            ByRef objChkResult As TaskCompletionSource(Of Boolean))

        RemoveHandler CompositionTarget.Rendering, AddressOf OnFramer

        _durationMs = pDuration.TotalMilliseconds
        ProgressEaseFunc = If(pEasing, Function(x) x)
        _lastPx = -1
        _startTicks = Stopwatch.GetTimestamp()

        If objChkResult IsNot Nothing Then objChkResult = Nothing

        objChkResult = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                            RunContinuationsAsynchronously)
    End Sub

    Public Sub SetProgressResult(Optional setResult As ProgResult = Nothing)
        RemoveHandler CompositionTarget.Rendering, AddressOf OnFramer

        Select Case setResult
            Case ProgResult.Cancelled
                ProgressTaskSrc?.TrySetResult(False)
                RaiseEvent ProgressFailed(Me, EventArgs.Empty)
            Case ProgResult.Completed
                ProgressTaskSrc?.TrySetResult(True)
                RaiseEvent ProgressComplete(Me, EventArgs.Empty)
        End Select

        ProgressTaskSrc = Nothing
    End Sub

    Private Function CalcDuration() As Double
        Dim valDur = (Stopwatch.GetTimestamp() - _startTicks) * 1000.0 / Stopwatch.Frequency
        Return Math.Max(0.0, Math.Min(1.0, valDur / _durationMs))
    End Function

    Private Sub OnFramer(sender As Object, e As EventArgs)
        Progress = CalcDuration()

        Dim w = CInt(Math.Round(ActualWidth))

        Dim newPx = CInt(Math.Round(w * Progress))

        If newPx <> _lastPx Then
            _lastPx = newPx
            InvalidateVisual()   ' triggers D3D delta strip paint in OnRender
        End If


        If Progress >= 1.0 Then
            SetProgressResult(ProgResult.Completed)
        End If
    End Sub

    Private Function ApplyProgContainer(width As Double, height As Double, radius As Double) As Geometry
        Dim objPathFigure As New PathFigure With {
            .StartPoint = New Windows.Point(0, 0),
            .Segments = GenerateProgContainer(width, height, radius),
            .IsClosed = True
        }

        Dim objPathGeometry As New PathGeometry()
        objPathGeometry.Figures.Add(objPathFigure)

        Return objPathGeometry
    End Function

    Private Function GenerateProgContainer2(width As Double, height As Double, radius As Double) As PathSegmentCollection
        Return New PathSegmentCollection(
            {
                GenSegLine(width, 0),
                GenSegLine(width, height - radius),
                GenSegArc(width - radius, height),
                GenSegLine(radius, height),
                GenSegArc(0, height - radius)
            })
    End Function

    Private Function GenerateProgContainer(width As Double, height As Double, radius As Double) As PathSegmentCollection
        Return New PathSegmentCollection({
            New LineSegment(New Windows.Point(width, 0), True),
            New LineSegment(New Windows.Point(width, height - radius), True),
            New ArcSegment(New Windows.Point(width - radius, height),
                           New Windows.Size(radius, radius), 0, False, SweepDirection.Clockwise, True),
            New LineSegment(New Windows.Point(radius, height), True),
            New ArcSegment(New Windows.Point(0, height - radius),
                           New Windows.Point(radius, radius), 0, False, SweepDirection.Clockwise, True)
        })
    End Function

    Private Function GenSegLine(segW As Double, segH As Double) As LineSegment
        Return New LineSegment(GenSegPoint(segW, segH), True)
    End Function

    Private Function GenSegArc(segW As Double, segH As Double, Optional pR As Double = 0) As ArcSegment
        Return New ArcSegment(GenSegPoint(segW, segH),
                            GenSegSize(pR), 0, False,
                            SweepDirection.Clockwise, True)
    End Function

    Private Function GenSegPoint(pX As Double, pY As Double) As Windows.Point
        Return New Windows.Point(pX, pY)
    End Function

    Private Function GenSegSize(sR As Double) As Windows.Size
        Return New Windows.Size(sR, sR)
    End Function

    Public Sub SetProgColor(pColor As osColor.Color, Optional pUpdate As Boolean = False)
        ActiveBrush = New SolidColorBrush(pColor)
        ActiveBrushColor = pColor

        If pUpdate Then InvalidateVisual()
    End Sub

    Private Function AllocDispatcher() As Dispatcher
        Return If(Not IsAutoPass, Application.Current.Dispatcher,
            osHandler_UI.osGui_AutoPass.Dispatcher)
    End Function

    Private Function ValidateProgress(pVal As Double) As Boolean
        Return Math.Abs(pVal - _progChunk) < Me.MinDelta
    End Function

    Private Function ProcessProgress(pVal As Double) As Double
        Dim valClamped = Math.Max(0.0, Math.Min(1.0, pVal))

        If _pixelWidth > 0 Then
            Dim progStep As Double = 1.0 / _pixelWidth

            valClamped = If(IsAutoPass, Math.Round(valClamped / progStep, 1) * progStep,
                Math.Round(valClamped / progStep, 1) * progStep)
        End If

        Return valClamped
    End Function

    Private Sub PrimeIfReady()
        If VerifyFramePrime() Then Return

        If IsAutoPass Then
            PrimeFirstFrame(1)
        Else
            PrimeFirstFrame()
        End If
    End Sub

    Private Function VerifyFramePrime() As Boolean
        Dim chkPrime As Boolean = False

        If Not IsLoaded Then chkPrime = True
        If _pixelWidth <= 0 OrElse _pixelHeight <= 0 Then chkPrime = True

        Return chkPrime
    End Function

    Private Sub ApplyOptionalClip()
        If Me.IsAutoPass Then
            Me.Clip = ApplyProgContainer(Math.Max(0, ActualWidth), Math.Max(0, ActualHeight), 20)
        Else
            Me.Clip = Nothing
        End If
    End Sub

    Private Sub SetSizeData()
        _pixelWidth = Math.Max(1, CInt(Math.Round(ActualWidth)))
        _pixelHeight = Math.Max(1, CInt(Math.Round(ActualHeight)))
    End Sub

    Private Function ValidatePendingPrime() As Boolean
        Return _pendingPrime AndAlso _pixelWidth > 0 AndAlso _pixelHeight > 0
    End Function

    Private Function ValidateSize() As Boolean
        Return _pixelWidth <= 0 OrElse _pixelHeight <= 0
    End Function

    Private Function ValidateSize(chkLoad As Boolean) As Boolean
        Return IsLoaded AndAlso _pixelWidth > 0 AndAlso _pixelHeight > 0
    End Function

    Private Function ValidateBrush(curEdge As Double) As Boolean
        Return _activeBrush IsNot Nothing AndAlso curEdge > 0
    End Function

    Private Function ValidateProgBitmap() As Boolean
        Return ProgRenderBitmap Is Nothing OrElse _activeBrush Is Nothing
    End Function

    Private Sub ValidateProgDV()
        If ProgRenderSurface Is Nothing Then ProgRenderSurface = New DrawingVisual()
    End Sub

    Private Function ApplyPen(brdrBrush As osColor.Brush, brdrThick As Double) As osColor.Pen
        Return New osColor.Pen(brdrBrush, brdrThick)
    End Function

    Private Function CalcProgEdge(valClamped As Double) As (valEdge_Old As Integer, valEdge_New As Integer)
        Dim oldEdge = EdgeFromChunk(_progChunk)
        _progChunk = valClamped
        Dim newEdge = EdgeFromChunk(_progChunk)

        Return (valEdge_Old:=oldEdge, valEdge_New:=newEdge)
    End Function

    Private Function CalcProgEdge(valClamped As Double, isEdgeObj As Boolean) As ProgEdgeData
        Dim objEdgeData As New ProgEdgeData(valClamped, ActualWidth, _progChunk)

        Return objEdgeData
    End Function

    Public Sub SetProgress(setProgVal As Double)
        _progChunk = setProgVal
    End Sub

    Private Function EdgeFromChunk(objChunk As Double) As Integer
        If ActualWidth <= 0 Then Return 0

        If Not IsAutoPass Then
            If objChunk >= 1.0 - Double.Epsilon Then Return _pixelWidth
        End If

        Return Math.Round(_pixelWidth * objChunk, 2)
    End Function

    Private Function GenProgRect(rX As Double, rY As Double, rW As Double, rH As Double) As Rect
        Return New Rect(rX, rY, rW, rH)
    End Function

    Private Function BgBrushOrDefault() As osColor.Brush
        Return If(Background, DirectCast(osFuncLib_Progress.ProgBG, osColor.Brush))
    End Function

    Private Sub ValidateMsgDV()
        If MsgOverlaySurface Is Nothing Then MsgOverlaySurface = New DrawingVisual()
    End Sub

    Private Function ValidateProgEdge(chkProg As Boolean, chkEdge As Double) As Boolean
        Return chkProg AndAlso _activeBrush IsNot Nothing AndAlso chkEdge > 0
    End Function

    Private Function ValidateProgRender() As Boolean
        Return ProgRenderBitmap Is Nothing OrElse
            ProgRenderBitmap.PixelWidth <> _pixelWidth OrElse
            ProgRenderBitmap.PixelHeight <> _pixelHeight
    End Function

    Private Function ValidateMsgRender() As Boolean
        Return MsgRenderBitmap Is Nothing OrElse
            MsgRenderBitmap.PixelWidth <> _pixelWidth OrElse
            MsgRenderBitmap.PixelHeight <> _pixelHeight
    End Function

    Private Sub EnsureBitmapsSized()
        If ValidateProgRender() Then
            Try
                ProgRenderBitmap = New RenderTargetBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32)
            Catch ex As Exception
                ProgRenderBitmap = New RenderTargetBitmap(110, 25, 96, 96, PixelFormats.Pbgra32)
            End Try
        End If

        If ValidateMsgRender() Then
            Try
                MsgRenderBitmap = New RenderTargetBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32)
            Catch ex As Exception
                MsgRenderBitmap = New RenderTargetBitmap(110, 25, 96, 96, PixelFormats.Pbgra32)
            End Try
        End If
    End Sub

    Private Function IsGpuOptimized() As Boolean
        Dim chkGpuOpt = (RenderCapability.Tier >> 16)
        Return chkGpuOpt >= 2
    End Function

    Private Sub CalcMinDelta(isGpuOpt As Boolean)
        If isGpuOpt Then
            Me.MinDelta = 1.0 / _pixelWidth
        Else
            Me.MinDelta = 1.0 / _pixelWidth
        End If
    End Sub

    Private Sub VerifyBitmapSize(chkRender As RenderBitmapObj)
        Select Case chkRender
            Case RenderBitmapObj.Progress
                If ValidateProgRender() Then
                    ProgRenderBitmap = New RenderTargetBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32)
                End If
            Case RenderBitmapObj.Message
                If ValidateMsgRender() Then
                    MsgRenderBitmap = New RenderTargetBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32)
                End If
        End Select
    End Sub

    Private Function ComposeMsgRender(renType As MsgRenderType, Optional txtMsg As String = Nothing, Optional pType As TriggerType = Nothing) As Action
        Select Case renType
            Case MsgRenderType.msgClear
                Return New Action(
                    Sub()
                        ResetMsgBitmap()
                        isMsgDisplayed = False
                        InvalidateVisual()
                    End Sub)
            Case MsgRenderType.msgDisplay
                Return New Action(
                    Sub()
                        ValidateMsgDV()
                        EnsureBitmapsSized()

                        ResetMsgBitmap()

                        Using MsgRenderTarget = MsgOverlaySurface.RenderOpen()
                            With New ProgMsg(txtMsg, pType, Me.IsAutoPass)
                                MsgRenderTarget.DrawText(.txtComposed, .txtLocation)
                            End With
                        End Using

                        MsgRenderBitmap.Render(MsgOverlaySurface)

                        isMsgDisplayed = True
                        InvalidateVisual()
                    End Sub)
        End Select
    End Function

    Private Sub RunOnUI(action As Action, Optional prio As DispatcherPriority = DispatcherPriority.Normal)
        Dim d = AllocDispatcher()
        If d.CheckAccess() Then
            action()
        Else
            d.Invoke(prio, action)
        End If
    End Sub

    Private Shared Function ValidateProgFreeze(objPF As Freezable) As Boolean
        If objPF IsNot Nothing AndAlso objPF.CanFreeze AndAlso Not objPF.IsFrozen Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Shared Sub EstablishProgFreeze(ByRef objPF As Freezable)
        If objPF IsNot Nothing AndAlso objPF.CanFreeze AndAlso Not objPF.IsFrozen Then
            objPF.Freeze()
        End If
    End Sub

    Private Shared Sub SetOsProgObj(objDO As DependencyObject, ByRef doProg As OddLib_ProgressBar)
        doProg = DirectCast(objDO, OddLib_ProgressBar)
    End Sub

    Private Shared Function SetOsProgObj(objDO As DependencyObject) As OddLib_ProgressBar
        Return DirectCast(objDO, OddLib_ProgressBar)
    End Function

#End Region

End Class
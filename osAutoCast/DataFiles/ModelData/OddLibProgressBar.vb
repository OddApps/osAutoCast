Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Threading
Imports System.Diagnostics
Imports System.Threading

Public Class OddLib_ProgressBar
    Inherits FrameworkElement

#Region "Fields / State"

    Private ProgRenderBitmap As RenderTargetBitmap
    Private ProgRenderSurface As DrawingVisual

    Private MsgRenderBitmap As RenderTargetBitmap
    Private MsgOverlaySurface As DrawingVisual

    Private _pixelWidth As Integer
    Private _pixelHeight As Integer

    Private _progChunk As Double
    Private objEdge_Prev As Integer
    Private isPendingDraw As Boolean

    Private isMsgDisplayed As Boolean = False

    Private ProgGraphic_Full As Drawing = Nothing
    Private _backgroundDrawing As Drawing = Nothing

    Private _pendingPrime As Boolean

    Private Shared ReadOnly DefaultBg As SolidColorBrush = New SolidColorBrush(Color.FromRgb(57, 57, 57))

    Public Event ProgressComplete As EventHandler
    Public Event ProgressFailed As EventHandler

    Private objProgValData As ProgressValData

    Private ProgressTimer As DispatcherTimer = Nothing
    Private ProgressWatch As Stopwatch = Nothing
    Private ProgressDuration As TimeSpan
    Private AutoResetProgress As Boolean
    Private ProgressTaskSrc As TaskCompletionSource(Of Boolean) = Nothing
    Private ProgressEaseFunc As Func(Of Double, Double) = Nothing

    Shared Sub New()
        DefaultBg.Freeze()
    End Sub

    Public Sub New()
        SnapsToDevicePixels = True
        UseLayoutRounding = True

        RenderOptions.SetBitmapScalingMode(Me, BitmapScalingMode.LowQuality)

        If Not IsAutoPass Then
            ProgressTimer = New DispatcherTimer(DispatcherPriority.Render) With {
                .Interval = TimeSpan.FromMilliseconds(10)
            }
            AddHandler ProgressTimer.Tick, AddressOf UpdateProgress
        End If

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
            End Sub
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
        DependencyProperty.Register("Background", GetType(Brush), GetType(OddLib_ProgressBar),
                                    New FrameworkPropertyMetadata(DefaultBg, AddressOf OnBackgroundChanged))

    Public Property Background As Brush
        Get
            Return CType(GetValue(BackgroundProperty), Brush)
        End Get
        Set(value As Brush)
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
        DependencyProperty.Register("BorderBrush", GetType(Brush), GetType(OddLib_ProgressBar),
                                    New FrameworkPropertyMetadata(Brushes.Black))

    Public Property BorderBrush As Brush
        Get
            Return CType(GetValue(BorderBrushProperty), Brush)
        End Get
        Set(value As Brush)
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

    Private _activeBrush As Brush
    Public Property ActiveBrush As Brush
        Get
            Return _activeBrush
        End Get
        Set(value As Brush)
            Dim newBrush As Brush = If(value, Brushes.Transparent)

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

#End Region

#Region "Progress API"

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

        Try
            AllocDispatcher().
                BeginInvoke(DispatcherPriority.Render,
                            ComposeMsgRender(MsgRenderType.msgDisplay, txtMsg, pType))
        Finally
            ResumePriming()
        End Try
    End Sub

    Private Sub ClearMsg(Optional pType As TriggerType = Nothing)
        SuspendPriming()

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

    Protected Overrides Sub OnRender(progDC As DrawingContext)
        MyBase.OnRender(progDC)

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
    End Sub

    Protected Overrides Sub OnRenderSizeChanged(sizeInfo As SizeChangedInfo)
        MyBase.OnRenderSizeChanged(sizeInfo)

        If ActualWidth > 0 AndAlso ActualHeight > 0 Then
            SetSizeData()

            Me.MinDelta = 1.0 / _pixelWidth

            ApplyOptionalClip()

            ValidateProgDV()
            ValidateMsgDV()
            EnsureBitmapsSized()

            RebuildBackingBitmap(includeProgress:=True)

            If isMsgDisplayed Then
                ClearMsg()
            End If
        End If
    End Sub

#End Region

#Region "Helpers you already had (kept, trimmed to match new pipeline)"

    Private Function ApplyProgContainer(width As Double, height As Double, radius As Double) As Geometry
        Dim objPathFigure As New PathFigure With {
            .StartPoint = New Point(0, 0),
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
            New LineSegment(New Point(width, 0), True),
            New LineSegment(New Point(width, height - radius), True),
            New ArcSegment(New Point(width - radius, height),
                           New Size(radius, radius), 0, False, SweepDirection.Clockwise, True),
            New LineSegment(New Point(radius, height), True),
            New ArcSegment(New Point(0, height - radius),
                           New Size(radius, radius), 0, False, SweepDirection.Clockwise, True)
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

    Private Function GenSegPoint(pX As Double, pY As Double) As Point
        Return New Point(pX, pY)
    End Function

    Private Function GenSegSize(sR As Double) As Size
        Return New Size(sR, sR)
    End Function

    Public Sub SetProgColor(pColor As Color)
        ActiveBrush = New SolidColorBrush(pColor)
    End Sub

    Private Function AllocDispatcher() As Dispatcher
        Return If(Not IsAutoPass, osHandler_GUI.osGui_AutoCast.Dispatcher,
            osHandler_GUI.osGui_AutoPass.Dispatcher)
    End Function

    Private Function ValidateProgress(pVal As Double) As Boolean
        Return Math.Abs(pVal - _progChunk) < Me.MinDelta
    End Function

    Private Function ProcessProgress(pVal As Double) As Double
        Dim valClamped = Math.Max(0.0, Math.Min(1.0, pVal))

        If _pixelWidth > 0 Then
            Dim progStep As Double = 1.0 / _pixelWidth

            valClamped = If(IsAutoPass, Math.Round(valClamped / progStep, 1) * progStep,
                Math.Round(valClamped / progStep) * progStep)
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

    Private Function ApplyPen(brdrBrush As Brush, brdrThick As Double) As Pen
        Return New Pen(brdrBrush, brdrThick)
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

    Private Function BgBrushOrDefault() As Brush
        Return If(Background, DirectCast(DefaultBg, Brush))
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
            ProgRenderBitmap = New RenderTargetBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32)
        End If

        If ValidateMsgRender() Then
            MsgRenderBitmap = New RenderTargetBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32)
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
            Case Else
                Return Nothing
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

#Region "Sweep Animation"

    Private Sub ClearProgress()
        ProgressTimer.Stop()
        ProgressWatch = Stopwatch.StartNew()
    End Sub

    Private Sub StartProgress(pDuration As TimeSpan, pEasing As Func(Of Double, Double))
        ClearProgress()

        ProgressDuration = pDuration
        AutoResetProgress = False
        ProgressEaseFunc = pEasing

        ProgressTimer.Start()
    End Sub

    Public Function BeginProgress(pDuration As TimeSpan, objAbortToken As CancellationToken,
                                  pEasing As Func(Of Double, Double)) As Task
        InitProgressTask(pDuration, pEasing, ProgressTaskSrc)

        objAbortToken.Register(
            Sub()
                SetProgressResult(ProgResult.Cancelled)
            End Sub)

        StartProgress(pDuration, pEasing)
        Return ProgressTaskSrc.Task
    End Function

    Private Sub InitProgressTask(pDuration As TimeSpan, pEasing As Func(Of Double, Double),
                                 ByRef objChkResult As TaskCompletionSource(Of Boolean))
        objProgValData = New ProgressValData(pDuration, pEasing)

        If objChkResult IsNot Nothing Then objChkResult = Nothing
        objChkResult = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.
                                                    RunContinuationsAsynchronously)
    End Sub

    Public Sub SetProgressResult(Optional setResult As ProgResult = Nothing)
        ProgressTimer.Stop()
        ProgressWatch?.Stop()

        Select Case setResult
            Case ProgResult.Cancelled
                ProgressTaskSrc?.TrySetResult(False)
                RaiseEvent ProgressFailed(Me, EventArgs.Empty)
            Case ProgResult.Completed
                ProgressTaskSrc?.TrySetResult(True)
                RaiseEvent ProgressComplete(Me, EventArgs.Empty)
        End Select

        ProgressTaskSrc = Nothing

        If objProgValData IsNot Nothing Then
            objProgValData.Dispose()
            objProgValData = Nothing
        End If
    End Sub

    Private Sub UpdateProgress(sender As Object, e As EventArgs)
        If ProgressWatch Is Nothing Then Return

        objProgValData.CalcProgress(ProgressWatch, ProgressChunk)

        If objProgValData.ProgressComplete Then
            SetProgressResult(ProgResult.Completed)
        End If
    End Sub

#End Region


#End Region

End Class
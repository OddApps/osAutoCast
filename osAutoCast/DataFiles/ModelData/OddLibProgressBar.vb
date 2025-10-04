Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Threading

'Public Class OddLib_ProgressBar
'    Inherits FrameworkElement

'    Private ProgRenderBitmap As RenderTargetBitmap
'    Private ProgRenderSurface As DrawingVisual
'    Private isMsgDisplayed As Boolean
'    Private ProgGraphic_Full As Drawing
'    Private _backgroundDrawing As Drawing
'    Private BkgRenderBitmap As RenderTargetBitmap
'    Private BkgRenderSurface As DrawingVisual
'    Private progressBrush As DrawingBrush
'    Private progEdge As Double
'    Private Shared pBarColor_BG As New SolidColorBrush(Color.FromRgb(57, 57, 57))

'    Public Shared ReadOnly BackgroundProperty As DependencyProperty =
'        DependencyProperty.Register(NameOf(Background), GetType(SolidColorBrush), GetType(OddLib_ProgressBar),
'                                     New FrameworkPropertyMetadata(pBarColor_BG, FrameworkPropertyMetadataOptions.AffectsRender))

'    Public Shared ReadOnly BorderBrushProperty As DependencyProperty =
'        DependencyProperty.Register(NameOf(BorderBrush), GetType(Brush), GetType(OddLib_ProgressBar),
'                                     New FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender))

'    Public Shared ReadOnly BorderThicknessProperty As DependencyProperty =
'        DependencyProperty.Register(NameOf(BorderThickness), GetType(Double), GetType(OddLib_ProgressBar),
'                                     New FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender))

'    Public Shared ReadOnly ProgressFlowProperty As DependencyProperty =
'        DependencyProperty.Register(NameOf(ProgressFlow), GetType(DataTypeLib.ProgFlow), GetType(OddLib_ProgressBar),
'                                     New PropertyMetadata(DataTypeLib.ProgFlow.Ascending))

'    Public Shared ReadOnly IsAutoPassProperty As DependencyProperty =
'        DependencyProperty.Register(NameOf(IsAutoPass), GetType(Boolean), GetType(OddLib_ProgressBar),
'                                     New PropertyMetadata(False))

'    Private _activeBrush As Brush
'    Private _progressFraction As Double

'    Public Sub New()
'        isMsgDisplayed = False
'        ProgGraphic_Full = Nothing
'        _backgroundDrawing = Nothing
'        _progressFraction = 0.0
'    End Sub

'    Public Property Background As Brush
'        Get
'            Return CType(GetValue(BackgroundProperty), Brush)
'        End Get
'        Set(value As Brush)
'            SetValue(BackgroundProperty, value)
'        End Set
'    End Property

'    Public Property BorderBrush As Brush
'        Get
'            Return CType(GetValue(BorderBrushProperty), Brush)
'        End Get
'        Set(value As Brush)
'            SetValue(BorderBrushProperty, value)
'        End Set
'    End Property

'    Public Property BorderThickness As Double
'        Get
'            Return CDbl(GetValue(BorderThicknessProperty))
'        End Get
'        Set(value As Double)
'            SetValue(BorderThicknessProperty, value)
'        End Set
'    End Property

'    Public Property ProgressFlow As DataTypeLib.ProgFlow
'        Get
'            Return CType(CInt(GetValue(ProgressFlowProperty)), DataTypeLib.ProgFlow)
'        End Get
'        Set(value As DataTypeLib.ProgFlow)
'            SetValue(ProgressFlowProperty, value)
'        End Set
'    End Property

'    Public Property IsAutoPass As Boolean
'        Get
'            Return CBool(GetValue(IsAutoPassProperty))
'        End Get
'        Set(value As Boolean)
'            SetValue(IsAutoPassProperty, value)
'        End Set
'    End Property

'    Public Property ActiveBrush As Brush
'        Get
'            Return _activeBrush
'        End Get
'        Set(value As Brush)
'            If value IsNot Nothing AndAlso value.CanFreeze AndAlso Not value.IsFrozen Then value.Freeze()
'            _activeBrush = value
'            PrepProgGraphic()
'            InvalidateVisual()
'        End Set
'    End Property

'    Public Property ProgressFraction As Double
'        Get
'            Return _progressFraction
'        End Get
'        Set(value As Double)
'            _progressFraction = Math.Max(0.0, Math.Min(1.0, osFuncLib_Progress.EaseInOutExpo(value)))
'            UpdateProgressViewbox()
'            InvalidateVisual()
'        End Set
'    End Property

'    Public Sub UpdateProgress(msDuration As Long)
'        ProgressFraction = CalcProgress(msDuration)
'    End Sub

'    Private Function CalcProgress(msDuration As Long) As Double
'        Return Math.Min(1.0, msDuration * osFuncLib_Progress.ProgInv)
'    End Function

'    Private Function IsFlowDesc() As Boolean
'        Return ProgressFlow = DataTypeLib.ProgFlow.Descending
'    End Function

'    Private Sub DetermineEdge()
'        progEdge = ActualWidth * _progressFraction
'    End Sub

'    Private Function PrepProgGeometry() As RectangleGeometry
'        Return New RectangleGeometry(New Rect(0.0, 0.0, progEdge, ActualHeight))
'    End Function

'    Private Sub PrepProgGraphic()
'        If _activeBrush Is Nothing Then Return
'        Dim drawingVisual As New DrawingVisual()
'        Using drawingContext As DrawingContext = drawingVisual.RenderOpen()
'            drawingContext.DrawRectangle(_activeBrush, Nothing, New Rect(0.0, 0.0, ActualWidth, 1.0))
'        End Using
'        ProgGraphic_Full = drawingVisual.Drawing
'        Dim drawingBrush As New DrawingBrush(ProgGraphic_Full) With {
'            .Stretch = Stretch.Fill,
'            .ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
'            .Viewbox = New Rect(0.0, 0.0, _progressFraction, 1.0)
'        }
'        progressBrush = drawingBrush
'    End Sub

'    Private Sub UpdateProgressViewbox()
'        If progressBrush Is Nothing Then Return
'        progressBrush.Viewbox = New Rect(0.0, 0.0, ActualWidth * _progressFraction, 1.0)
'        InvalidateVisual()
'    End Sub

'    Private Sub DrawNewProgressSlice()
'        If ProgRenderBitmap Is Nothing OrElse _activeBrush Is Nothing Then Return
'        DetermineEdge()
'        AllocDispatcher().Invoke(Sub()
'                                     Using drawingContext As DrawingContext = ProgRenderSurface.RenderOpen()
'                                         drawingContext.DrawRectangle(Nothing, Nothing, New Rect(0.0, 0.0, ActualWidth, ActualHeight))
'                                         If ProgGraphic_Full Is Nothing Then PrepProgGraphic()
'                                         If IsFlowDesc() Then
'                                             drawingContext.PushClip(New RectangleGeometry(New Rect(progEdge, 0.0, ActualWidth - progEdge, ActualHeight)))
'                                             drawingContext.DrawDrawing(_backgroundDrawing)
'                                             drawingContext.Pop()
'                                             drawingContext.PushClip(New RectangleGeometry(New Rect(0.0, 0.0, progEdge, ActualHeight)))
'                                             drawingContext.DrawDrawing(ProgGraphic_Full)
'                                             drawingContext.Pop()
'                                         Else
'                                             drawingContext.PushClip(New RectangleGeometry(New Rect(0.0, 0.0, progEdge, ActualHeight)))
'                                             drawingContext.DrawDrawing(ProgGraphic_Full)
'                                             drawingContext.Pop()
'                                         End If
'                                     End Using
'                                     ProgRenderBitmap.Clear()
'                                     ProgRenderBitmap.Render(ProgRenderSurface)
'                                     InvalidateVisual()
'                                 End Sub)
'    End Sub

'    Private Sub ResetProgress(Optional apReset As DataTypeLib.TriggerType = DataTypeLib.TriggerType.AutoCast)
'        ' Empty placeholder
'    End Sub

'    Private Sub DisplayMaxVal()
'        AllocDispatcher().Invoke(Sub()
'                                     ValidateProgDV()
'                                     Using drawingContext As DrawingContext = ProgRenderSurface.RenderOpen()
'                                         drawingContext.DrawRectangle(_activeBrush, Nothing, New Rect(0.0, 0.0, ActualWidth, ActualHeight))
'                                     End Using
'                                     ProgRenderBitmap.Render(ProgRenderSurface)
'                                     InvalidateVisual()
'                                 End Sub)
'    End Sub

'    Private Sub RenderMsgContainer(pDC As DrawingContext)
'        pDC.DrawRectangle(_activeBrush, Nothing, New Rect(0.0, 0.0, ActualWidth, ActualHeight))
'        isMsgDisplayed = False
'    End Sub

'    Private Function AllocDispatcher() As Dispatcher
'        Return If(Not IsAutoPass, osHandler_GUI.osGui_AutoCast.Dispatcher, osHandler_GUI.osGui_AutoPass.Dispatcher)
'    End Function

'    Private Sub DisplayMsg(txtMsg As String, pType As DataTypeLib.TriggerType)
'        osFuncLib_Progress.progShowMsg = True
'        AllocDispatcher().Invoke(Sub()
'                                     ValidateProgDV()
'                                     Using pDC As DrawingContext = ProgRenderSurface.RenderOpen()
'                                         If osFuncLib_Progress.progShowMsg Then
'                                             If isMsgDisplayed Then RenderMsgContainer(pDC)
'                                             Dim progMsg As New ProgMsg(txtMsg, pType, IsAutoPass)
'                                             pDC.DrawText(progMsg.txtComposed, progMsg.txtLocation)
'                                             isMsgDisplayed = True
'                                         End If
'                                     End Using
'                                     ProgRenderBitmap.Render(ProgRenderSurface)
'                                     InvalidateVisual()
'                                 End Sub)
'        osFuncLib_Progress.progShowMsg = False
'    End Sub

'    Private Sub ClearMsg(pType As DataTypeLib.TriggerType)
'        osFuncLib_Progress.progShowMsg = True
'        AllocDispatcher().Invoke(Sub()
'                                     ValidateProgDV()
'                                     Using drawingContext As DrawingContext = ProgRenderSurface.RenderOpen()
'                                         drawingContext.DrawRectangle(pBarColor_BG, Nothing, New Rect(0.0, 0.0, ActualWidth, ActualHeight))
'                                         isMsgDisplayed = False
'                                     End Using
'                                     ProgRenderBitmap.Render(ProgRenderSurface)
'                                     InvalidateVisual()
'                                 End Sub)
'        osFuncLib_Progress.progShowMsg = False
'    End Sub

'    Public Sub PerformProgressEvent(doEvent As ProgressEventData)
'        ' Placeholder for event handling
'    End Sub

'    Public Sub SetProgColor(pColor As Color)
'        ActiveBrush = New SolidColorBrush(pColor)
'    End Sub

'    Private Sub RenderProgBG(Optional apReset As DataTypeLib.TriggerType = DataTypeLib.TriggerType.AutoCast)
'        AllocDispatcher().Invoke(Sub()
'                                     ValidateProgDV()
'                                     Using drawingContext As DrawingContext = ProgRenderSurface.RenderOpen()
'                                         drawingContext.DrawRectangle(pBarColor_BG, Nothing, New Rect(0.0, 0.0, ActualWidth, ActualHeight))
'                                     End Using
'                                     ProgRenderBitmap.Render(ProgRenderSurface)
'                                     InvalidateVisual()
'                                 End Sub)
'    End Sub

'    Private Sub RenderProgBG(progDC As DrawingContext)
'        progDC.DrawRectangle(pBarColor_BG, Nothing, New Rect(0.0, 0.0, ActualWidth, ActualHeight))
'    End Sub

'    Private Sub ValidateProgDV()
'        If ProgRenderSurface IsNot Nothing Then Return
'        ProgRenderSurface = New DrawingVisual()
'    End Sub

'    Protected Overrides Sub OnRender(dc As DrawingContext)
'        MyBase.OnRender(dc)
'        dc.DrawRectangle(Brushes.Gray, Nothing, New Rect(0.0, 0.0, ActualWidth, ActualHeight))
'        If progressBrush IsNot Nothing Then
'            dc.DrawRectangle(progressBrush, Nothing, New Rect(0.0, 0.0, ActualWidth, ActualHeight))
'        End If
'    End Sub

'    Private Function ApplyProgContainer(width As Double, height As Double, radius As Double) As Geometry
'        Dim pathFigure As New PathFigure With {
'            .StartPoint = New Point(0.0, 0.0),
'            .Segments = GenerateProgContainer(width, height, radius),
'            .IsClosed = True
'        }
'        Dim pathGeometry As New PathGeometry()
'        pathGeometry.Figures.Add(pathFigure)
'        Return pathGeometry
'    End Function

'    Private Function GenerateProgContainer(width As Double, height As Double, radius As Double) As PathSegmentCollection
'        Return New PathSegmentCollection() From {
'            New LineSegment(New Point(width, 0.0), True),
'            New LineSegment(New Point(width, height - radius), True),
'            New ArcSegment(New Point(width - radius, height), New Size(radius, radius), 0.0, False, SweepDirection.Clockwise, True),
'            New LineSegment(New Point(radius, height), True),
'            New ArcSegment(New Point(0.0, height - radius), New Size(radius, radius), 0.0, False, SweepDirection.Clockwise, True)
'        }
'    End Function

'    Protected Overrides Sub OnRenderSizeChanged(sizeInfo As SizeChangedInfo)
'        MyBase.OnRenderSizeChanged(sizeInfo)
'        If ActualWidth <= 0.0 OrElse ActualHeight <= 0.0 Then Return
'        PrepProgGraphic()
'        UpdateProgressViewbox()
'        If IsAutoPass Then
'            Clip = ApplyProgContainer(ActualWidth, ActualHeight, 20.0)
'        End If
'    End Sub
'End Class

Public Class OddLib_ProgressBar
    Inherits FrameworkElement

    Private ProgRenderBitmap As RenderTargetBitmap
    Private ProgRenderSurface As DrawingVisual

    Private isMsgDisplayed As Boolean = False

    Private ProgGraphic_Full As Drawing = Nothing
    Private _backgroundDrawing As Drawing = Nothing

    Private progEdge As Double

    Private Shared pBarColor_BG As New System.Windows.Media.
        SolidColorBrush(System.Windows.Media.Color.FromRgb(57, 57, 57))

    Public Shared ReadOnly BackgroundProperty As DependencyProperty = DependencyProperty.
        Register("Background", GetType(SolidColorBrush), GetType(OddLib_ProgressBar),
                 New FrameworkPropertyMetadata(pBarColor_BG, FrameworkPropertyMetadataOptions.AffectsRender))

    Public Property Background As Brush
        Get
            Return CType(GetValue(BackgroundProperty), Brush)
        End Get
        Set(value As Brush)
            SetValue(BackgroundProperty, value)
        End Set
    End Property

    Public Shared ReadOnly BorderBrushProperty As DependencyProperty = DependencyProperty.
        Register("BorderBrush", GetType(Brush), GetType(OddLib_ProgressBar),
                 New FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender))

    Public Property BorderBrush As Brush
        Get
            Return CType(GetValue(BorderBrushProperty), Brush)
        End Get
        Set(value As Brush)
            SetValue(BorderBrushProperty, value)
        End Set
    End Property

    Public Shared ReadOnly BorderThicknessProperty As DependencyProperty = DependencyProperty.
        Register("BorderThickness", GetType(Double), GetType(OddLib_ProgressBar),
                 New FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender))

    Public Property BorderThickness As Double
        Get
            Return CDbl(GetValue(BorderThicknessProperty))
        End Get
        Set(value As Double)
            SetValue(BorderThicknessProperty, value)
        End Set
    End Property

    Public Shared ReadOnly ProgressFlowProperty As DependencyProperty = DependencyProperty.
        Register(NameOf(ProgressFlow), GetType(ProgFlow), GetType(OddLib_ProgressBar),
                 New PropertyMetadata(ProgFlow.Ascending))

    Public Property ProgressFlow As ProgFlow
        Get
            Return CType(GetValue(ProgressFlowProperty), ProgFlow)
        End Get
        Set(value As ProgFlow)
            SetValue(ProgressFlowProperty, value)
        End Set
    End Property

    Public Shared ReadOnly IsAutoPassProperty As DependencyProperty = DependencyProperty.
        Register(NameOf(IsAutoPass), GetType(Boolean), GetType(OddLib_ProgressBar),
                 New PropertyMetadata(False))

    Public Property IsAutoPass As Boolean
        Get
            Return CType(GetValue(IsAutoPassProperty), Boolean)
        End Get
        Set(value As Boolean)
            SetValue(IsAutoPassProperty, value)
        End Set
    End Property

    Private _activeBrush As Brush
    Public Property ActiveBrush As Brush
        Get
            Return _activeBrush
        End Get
        Set(value As Brush)
            If value IsNot Nothing AndAlso value.CanFreeze AndAlso Not value.IsFrozen Then
                value.Freeze()
            End If
            _activeBrush = value
            Me.InvalidateVisual()
        End Set
    End Property

    Private _progressFraction As Double = 0
    Public Property ProgressFraction As Double
        Get
            Return _progressFraction
        End Get
        Set(value As Double)
            ' Clamp input
            Dim pVal = osFuncLib_Progress.EaseInOutExpo(value)
            Dim clamped = Math.Max(0, Math.Min(1, pVal))

            ' Invert if reverse mode is active
            If ProgressFlow = ProgFlow.Descending Then
                clamped = 1 - clamped
            End If

            'If Math.Abs(clamped - _progressFraction) < 0.001 Then Return

            _progressFraction = clamped
            DrawNewProgressSlice()
        End Set
    End Property

    Public Sub UpdateProgress(msDuration As Long)
        Me.ProgressFraction = CalcProgress(msDuration)
    End Sub

    Private Function CalcProgress(msDuration As Long) As Double
        Return CDbl(Math.Min(1.0, msDuration * osFuncLib_Progress.ProgInv))
    End Function

    Private Function IsFlowDesc() As Boolean
        Return Me.ProgressFlow = ProgFlow.Descending
    End Function

    Private Sub DetermineEdge()
        progEdge = ActualWidth * _progressFraction
    End Sub

    Private Function PrepProgGeometry() As RectangleGeometry
        Return New RectangleGeometry(New Rect(0, 0, progEdge, ActualHeight))
    End Function

    Private Sub PrepProgGraphic()
        Dim ProgRenderSurface_Full As New DrawingVisual()

        Using ProgRenderTarget_Full = ProgRenderSurface_Full.RenderOpen()
            ProgRenderTarget_Full.DrawRectangle(_activeBrush, Nothing,
                                                New Rect(0, 0, ActualWidth, ActualHeight))
        End Using

        ProgGraphic_Full = ProgRenderSurface_Full.Drawing
    End Sub

    Private Sub DrawNewProgressSlice()
        If ProgRenderBitmap Is Nothing OrElse _activeBrush Is Nothing Then Return

        DetermineEdge()

        AllocDispatcher().
            Invoke(Sub()
                       ValidateProgDV()

                       Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
                           If IsFlowDesc() Then

                               If _backgroundDrawing Is Nothing Then
                                   Dim bgDV As New DrawingVisual()
                                   Using bgDc = bgDV.RenderOpen()
                                       bgDc.DrawRectangle(pBarColor_BG, Nothing,
                                       New Rect(0, 0, ActualWidth, ActualHeight))
                                   End Using
                                   _backgroundDrawing = bgDV.Drawing
                               End If

                               ProgRenderTarget.DrawDrawing(_backgroundDrawing)

                               If ProgGraphic_Full Is Nothing Then PrepProgGraphic()

                               With ProgRenderTarget
                                   .PushClip(PrepProgGeometry())
                                   .DrawDrawing(ProgGraphic_Full)
                                   .Pop()
                               End With
                           Else
                               ProgRenderTarget.DrawRectangle(_activeBrush, Nothing,
                                                              New Rect(0, 0, progEdge, ActualHeight))
                           End If
                       End Using

                       ProgRenderBitmap.Render(ProgRenderSurface)
                       InvalidateVisual()
                   End Sub)
    End Sub

    Private Sub ResetProgress(Optional apReset As TriggerType = False)

        Dim chkApReset As Boolean = apReset = TriggerType.AutoPass

        Me.ProgressFraction = If(chkApReset, 1, 0)

        _backgroundDrawing = Nothing
        ProgGraphic_Full = Nothing
        progEdge = 0

        AllocDispatcher().
            Invoke(Sub()
                       ProgRenderBitmap?.Clear()

                       ' If Not chkApReset Then
                       RenderProgBG(apReset)
                       '  End If

                       ProgRenderBitmap.Render(ProgRenderSurface)
                       InvalidateVisual()
                   End Sub)
    End Sub

    Private Sub DisplayMaxVal()
        AllocDispatcher().Invoke(Sub()
                                     ValidateProgDV()

                                     Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
                                         ProgRenderTarget.DrawRectangle(_activeBrush, Nothing,
                                                       New Rect(0, 0, ActualWidth, ActualHeight))
                                     End Using
                                     ProgRenderBitmap.Render(ProgRenderSurface)
                                     InvalidateVisual()
                                 End Sub)

    End Sub

    Private Sub RenderMsgContainer(pDC As DrawingContext)
        pDC.DrawRectangle(_activeBrush, Nothing,
                         New Rect(0, 0, ActualWidth, ActualHeight))
        isMsgDisplayed = False
    End Sub

    Private Function AllocDispatcher() As Dispatcher
        Return If(Not IsAutoPass, osHandler_GUI.osGui_AutoCast.Dispatcher, osHandler_GUI.osGui_AutoPass.Dispatcher)
    End Function

    Private Sub DisplayMsg(txtMsg As String, pType As TriggerType)
        osFuncLib_Progress.progShowMsg = True

        AllocDispatcher().Invoke(Sub()
                                     ValidateProgDV()

                                     Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
                                         If osFuncLib_Progress.progShowMsg Then
                                             If isMsgDisplayed Then RenderMsgContainer(ProgRenderTarget)

                                             With New ProgMsg(txtMsg, pType, Me.IsAutoPass)
                                                 ProgRenderTarget.DrawText(.txtComposed, .txtLocation)
                                                 isMsgDisplayed = True
                                             End With
                                         End If
                                     End Using

                                     ProgRenderBitmap.Render(ProgRenderSurface)
                                     InvalidateVisual()
                                 End Sub)
        osFuncLib_Progress.progShowMsg = False
    End Sub

    Private Sub ClearMsg(pType As TriggerType)
        osFuncLib_Progress.progShowMsg = True

        AllocDispatcher().Invoke(Sub()
                                     ValidateProgDV()

                                     Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
                                         ' redraw background or container (clears any previous text)
                                         ProgRenderTarget.DrawRectangle(pBarColor_BG, Nothing, New Rect(0, 0, ActualWidth, ActualHeight))
                                         isMsgDisplayed = False
                                     End Using

                                     ProgRenderBitmap.Render(ProgRenderSurface)
                                     InvalidateVisual()
                                 End Sub)
        osFuncLib_Progress.progShowMsg = False
    End Sub

    Public Sub PerformProgressEvent(doEvent As ProgressEventData)
        Select Case doEvent.evType
            Case ProgEvent.Reset
                ResetProgress(doEvent.evTrigger)
            Case ProgEvent.MaxFill
                ResetProgress()
                DisplayMaxVal()
            Case ProgEvent.DispMsg
                DisplayMsg(doEvent.evDispMsg, doEvent.evTrigger)
            Case ProgEvent.ClrMsg
                ClearMsg(doEvent.evTrigger)
        End Select

    End Sub

    Public Sub SetProgColor(pColor As System.Windows.Media.Color)
        ActiveBrush = New System.Windows.Media.SolidColorBrush(pColor)
    End Sub

    Private Sub RenderProgBG(Optional apReset As TriggerType = Nothing)
        AllocDispatcher().Invoke(Sub()
                                     ValidateProgDV()

                                     Using ProgRenderTarget = ProgRenderSurface.RenderOpen()
                                         ProgRenderTarget.DrawRectangle(If(apReset = TriggerType.AutoPass, pBarColor_BG, pBarColor_BG), Nothing,
                                                       New Rect(0, 0, ActualWidth, ActualHeight))
                                     End Using

                                     ProgRenderBitmap.Render(ProgRenderSurface)
                                     InvalidateVisual()
                                 End Sub)
    End Sub

    Private Sub RenderProgBG(progDC As DrawingContext)
        progDC.DrawRectangle(pBarColor_BG, Nothing,
                             New Rect(0, 0, ActualWidth, ActualHeight))

    End Sub

    Private Sub ValidateProgDV()
        If ProgRenderSurface Is Nothing Then ProgRenderSurface = New DrawingVisual()
    End Sub

    Protected Overrides Sub OnRender(ProgRenderTarget As DrawingContext)
        MyBase.OnRender(ProgRenderTarget)

        Dim rect As New Rect(0, 0,
                             ActualWidth, ActualHeight)

        'If Background IsNot Nothing Then
        '    ProgRenderTarget.DrawRoundedRectangle(Background, Nothing, rect, 10, 10)
        'End If

        ' If Not Me.IsAutoPass Then
        If Background IsNot Nothing Then
            ProgRenderTarget.DrawRectangle(Background, Nothing, rect)
        End If
        '  End If


        If ProgRenderBitmap IsNot Nothing Then
            ProgRenderTarget.DrawImage(ProgRenderBitmap, New Rect(0, 0, ActualWidth, ActualHeight))
        End If
        If Me.IsAutoPass Then
            'osGui_AutoPass.Dispatcher.Invoke(Sub()
            '                                     osGui_AutoPass.apBorder.InvalidateVisual()
            '                                 End Sub)
        End If
        ' ProgRenderTarget.DrawRectangle(Nothing, pBrush_Border, rect)
    End Sub

    Private Function ApplyProgContainer(width As Double, height As Double, radius As Double) As Geometry
        Dim figure As New PathFigure With {
            .StartPoint = New Point(0, 0),
            .Segments = GenerateProgContainer(width, height, radius),
            .IsClosed = True
        }

        Dim geometry As New PathGeometry()
        geometry.Figures.Add(figure)

        Return geometry

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

    Protected Overrides Sub OnRenderSizeChanged(sizeInfo As SizeChangedInfo)
        MyBase.OnRenderSizeChanged(sizeInfo)

        If ActualWidth > 0 AndAlso ActualHeight > 0 Then
            ProgRenderBitmap = New RenderTargetBitmap(CInt(ActualWidth), CInt(ActualHeight),
                                          96, 96, PixelFormats.Pbgra32)

            If Me.IsAutoPass Then
                Me.Clip = ApplyProgContainer(ActualWidth, ActualHeight, 20)
            End If
        End If
    End Sub

End Class


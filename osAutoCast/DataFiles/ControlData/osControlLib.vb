Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls.Primitives
Imports System.Windows.Data
Imports System.Windows.Markup
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Media.Effects
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.LoadingProgStatus
Imports osAutoCast.DataTypeLib.MenuProperty
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.DataTypeLib.PrefUI_State
Imports osAutoCast.DataTypeLib.RenderStateAction
Imports osAutoCast.DataTypeLib.VisualEasing
Imports osCmdsUD = osAutoCast.osControls.osUpDownTextBoxCmds
Imports osColor = System.Windows.Media
Imports osPoint = System.Windows.Point
Imports osSize = System.Windows.Size

Namespace osControls

    Public Class osPanel
        Inherits ContentControl

        Shared Sub New()
            DefaultStyleKeyProperty.OverrideMetadata(GetType(osPanel),
                                                     New FrameworkPropertyMetadata(GetType(osPanel)))
        End Sub

        Public Shared ReadOnly HeaderTextProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(HeaderText), GetType(String), GetType(osPanel),
                     New PropertyMetadata(String.Empty))

        Public Property HeaderText As String
            Get
                Return CStr(GetValue(HeaderTextProperty))
            End Get
            Set(value As String)
                SetValue(HeaderTextProperty, value)
            End Set
        End Property

        Public Shared ReadOnly HeaderColorProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(HeaderColor), GetType(Brush), GetType(osPanel),
                     New PropertyMetadata(Brushes.Black))

        Public Property HeaderColor As Brush
            Get
                Return CType(GetValue(HeaderColorProperty), Brush)
            End Get
            Set(value As Brush)
                SetValue(HeaderColorProperty, value)
            End Set
        End Property

    End Class

    Public Class osPrefHeader
        Inherits Control

        Shared Sub New()
            DefaultStyleKeyProperty.OverrideMetadata(
            GetType(osPrefHeader),
            New FrameworkPropertyMetadata(GetType(osPrefHeader)))
        End Sub

        Public Property Text As String
            Get
                Return CStr(GetValue(TextProperty))
            End Get
            Set(value As String)
                SetValue(TextProperty, value)
            End Set
        End Property

        Public Shared ReadOnly TextProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Text), GetType(String), GetType(osPrefHeader))

        Public Property LineBrush As Brush
            Get
                Return CType(GetValue(LineBrushProperty), Brush)
            End Get
            Set(value As Brush)
                SetValue(LineBrushProperty, value)
            End Set
        End Property

        Public Shared ReadOnly LineBrushProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(LineBrush), GetType(Brush), GetType(osPrefHeader),
                      New PropertyMetadata(New SolidColorBrush(Color.FromRgb(&H57, &H57, &H57))))

        Public Property LineThickness As Double
            Get
                Return CDbl(GetValue(LineThicknessProperty))
            End Get
            Set(value As Double)
                SetValue(LineThicknessProperty, value)
            End Set
        End Property

        Public Shared ReadOnly LineThicknessProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(LineThickness), GetType(Double), GetType(osPrefHeader),
                     New PropertyMetadata(1.0))

        Public Property LineSpacing As Double
            Get
                Return CDbl(GetValue(LineSpacingProperty))
            End Get
            Set(value As Double)
                SetValue(LineSpacingProperty, value)
            End Set
        End Property

        Public Shared ReadOnly LineSpacingProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(LineSpacing), GetType(Double), GetType(osPrefHeader),
                      New PropertyMetadata(8.0))
    End Class

    Public Class osBorder
        Inherits Border

        Private _cachedChildClip As Geometry
        Private _cachedOutline As Geometry
        Private _clipGeometry As StreamGeometry

        Private _oldChildClip As Object
        Private _isRenderingHooked As Boolean

        Private _BorderBrush As Pen = Nothing

        Public Sub New()
            UseLayoutRounding = True
            SnapsToDevicePixels = True
        End Sub

        Public Shared ReadOnly ClipInflationProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(ClipInflation), GetType(Double), GetType(osBorder),
                     New FrameworkPropertyMetadata(0.75, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property ClipInflation As Double
            Get
                Return CDbl(GetValue(ClipInflationProperty))
            End Get
            Set(value As Double)
                SetValue(ClipInflationProperty, value)
            End Set
        End Property

        Public ReadOnly Property BorderWidth As Double
            Get
                Return (BorderThickness.Left + BorderThickness.Top +
                    BorderThickness.Right + BorderThickness.Bottom) / 4.0
            End Get
        End Property

        Public Overrides Property Child As UIElement
            Get
                Return MyBase.Child
            End Get
            Set(value As UIElement)
                If MyBase.Child Is value Then Return

                If MyBase.Child IsNot Nothing Then
                    MyBase.Child.SetValue(UIElement.ClipProperty, _oldChildClip)
                End If

                If value IsNot Nothing Then
                    _oldChildClip = value.ReadLocalValue(UIElement.ClipProperty)
                Else
                    _oldChildClip = Nothing
                End If

                MyBase.Child = value
            End Set
        End Property

        Protected Overrides Sub OnVisualParentChanged(oldParent As DependencyObject)
            MyBase.OnVisualParentChanged(oldParent)

            If VisualParent Is Nothing Then
                SetRenderHook(UnhookRender)
            Else
                SetRenderHook(HookRender)
            End If
        End Sub

        Private Sub SetRenderHook(objRenderAction As RenderStateAction)
            Select Case objRenderAction
                Case HookRender
                    If _isRenderingHooked Then Return

                    AddHandler CompositionTarget.Rendering, AddressOf OnRendering
                    _isRenderingHooked = True
                Case UnhookRender
                    If Not _isRenderingHooked Then Return

                    RemoveHandler CompositionTarget.Rendering, AddressOf OnRendering
                    _isRenderingHooked = False
            End Select
        End Sub

        Private Sub OnRendering(sender As Object, e As EventArgs)
            UpdateChildClip()
        End Sub

        Private Function ApplyPen(brdrBrush As osColor.Brush, brdrThick As Double) As osColor.Pen
            Return New osColor.Pen(brdrBrush, brdrThick) With {
                .LineJoin = PenLineJoin.Round,
                .StartLineCap = PenLineCap.Round,
                .EndLineCap = PenLineCap.Round
            }
        End Function

        Private Sub EstablishProgFreeze(ByRef objPF As Freezable)
            If objPF IsNot Nothing AndAlso objPF.CanFreeze AndAlso Not objPF.IsFrozen Then
                objPF.Freeze()
            End If
        End Sub

        Protected Overrides Sub OnRender(dc As DrawingContext)
            MyBase.OnRender(dc)

            If _BorderBrush Is Nothing Then
                _BorderBrush = ApplyPen(BorderBrush, BorderWidth)
                Dim objFreeze = TryCast(_BorderBrush, Freezable)

                EstablishProgFreeze(objFreeze)
            End If

            If _cachedOutline IsNot Nothing Then
                dc.DrawGeometry(Nothing, _BorderBrush, _cachedOutline)
            End If
        End Sub

        Private Function SetPoint(pX As Double, pY As Double) As osPoint
            Return New osPoint(pX, pY)
        End Function

        Private Function SetRect(rX As Double, rY As Double, rW As Double, rH As Double) As Rect
            Return New Rect(rX, rY, rW, rH)
        End Function

        Private Function SetRect(rectLoc As osPoint, rectSize As osSize) As Rect
            Return New Rect(rectLoc, rectSize)
        End Function

        Private Function SetSize(sW As Double, sH As Double) As osSize
            Return New osSize(sW, sH)
        End Function

        Private Sub UpdateChildClip()
            Dim objChild = Me.Child

            If objChild Is Nothing Then Return

            Dim objContainerSize = objChild.RenderSize

            If objContainerSize.Width <= 0 OrElse objContainerSize.Height <= 0 Then Return

            Dim childRect = SetRect(0, 0, objContainerSize.Width, objContainerSize.Height)
            Dim objBrdrEdge As New osBorderEdge(CornerRadius, BorderWidth, ClipInflation)

            Dim childGeo = CreateRoundedGeometry(childRect, objBrdrEdge)

            If TypeOf childGeo Is Freezable Then
                Dim f = CType(childGeo, Freezable)
                If f.CanFreeze AndAlso Not f.IsFrozen Then f.Freeze()
            End If

            objChild.Clip = childGeo
            _cachedChildClip = childGeo

            Dim childTopLeft As Point = objChild.
                    TransformToAncestor(Me).Transform(SetPoint(0, 0))

            Dim outlineRect = SetRect(childTopLeft, objContainerSize)
            Dim outlineGeo = CreateRoundedGeometry(outlineRect, objBrdrEdge)

            If TypeOf outlineGeo Is Freezable Then
                Dim f2 = CType(outlineGeo, Freezable)
                If f2.CanFreeze AndAlso Not f2.IsFrozen Then f2.Freeze()
            End If

            _cachedOutline = outlineGeo

            InvalidateVisual()
        End Sub

        Private Function CreateRoundedGeometry(rect As Rect, objBrdrEdge As osBorderEdge) As StreamGeometry
            Dim geo As New StreamGeometry()

            With objBrdrEdge.CalcByRect(rect)
                Using ctx = geo.Open()
                    ctx.BeginFigure(SetPoint(rect.Left + .TopLeft, rect.Top), True, True)

                    ctx.LineTo(SetPoint(rect.Right - .TopRight, rect.Top), True, False)
                    If .TopRight > 0 Then
                        ctx.ArcTo(SetPoint(rect.Right, rect.Top + .TopRight),
                                  SetSize(.TopRight, .TopRight), 0, False,
                                  SweepDirection.Clockwise, True, False)
                    End If

                    ctx.LineTo(SetPoint(rect.Right, rect.Bottom - .BottomRight), True, False)
                    If .BottomRight > 0 Then
                        ctx.ArcTo(SetPoint(rect.Right - .BottomRight, rect.Bottom),
                                  SetSize(.BottomRight, .BottomRight), 0, False,
                                  SweepDirection.Clockwise, True, False)
                    End If

                    ctx.LineTo(SetPoint(rect.Left + .BottomLeft, rect.Bottom), True, False)
                    If .BottomLeft > 0 Then
                        ctx.ArcTo(SetPoint(rect.Left, rect.Bottom - .BottomLeft),
                                  SetSize(.BottomLeft, .BottomLeft), 0, False,
                                  SweepDirection.Clockwise, True, False)
                    End If

                    ctx.LineTo(SetPoint(rect.Left, rect.Top + .TopLeft), True, False)
                    If .TopLeft > 0 Then
                        ctx.ArcTo(SetPoint(rect.Left + .TopLeft, rect.Top),
                                  SetSize(.TopLeft, .TopLeft), 0, False,
                                  SweepDirection.Clockwise, True, False)
                    End If
                End Using

                geo.Freeze()
                Return geo
            End With
        End Function

    End Class

    Public NotInheritable Class osUpDownTextBoxCmds
        Public Shared ReadOnly Increase As New RoutedCommand()
        Public Shared ReadOnly Decrease As New RoutedCommand()
    End Class

    Public Class osUpDownTextBox
        Inherits TextBox

        Shared Sub New()
            DefaultStyleKeyProperty.OverrideMetadata(
            GetType(osUpDownTextBox),
            New FrameworkPropertyMetadata(GetType(osUpDownTextBox)))
        End Sub

        Protected Overrides Sub OnInitialized(e As EventArgs)
            MyBase.OnInitialized(e)

            CommandBindings.Add(New CommandBinding(osCmdsUD.Increase,
                                                   Sub() ChangeValue(+Increment)))

            CommandBindings.Add(New CommandBinding(osCmdsUD.Decrease,
                                                   Sub() ChangeValue(-Increment)))
        End Sub

        Public Shared ReadOnly ValueProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Value), GetType(Double), GetType(osUpDownTextBox),
                      New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.
                      BindsTwoWayByDefault, AddressOf OnValueChanged))

        Public Property Value As Double
            Get
                Return CDbl(GetValue(ValueProperty))
            End Get
            Set(value As Double)
                SetValue(ValueProperty, value)
            End Set
        End Property

        Public Shared ReadOnly MinimumProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Minimum), GetType(Double), GetType(osUpDownTextBox),
                      New PropertyMetadata(0.0))

        Public Property Minimum As Double
            Get
                Return CDbl(GetValue(MinimumProperty))
            End Get
            Set(value As Double)
                SetValue(MinimumProperty, value)
            End Set
        End Property

        Public Shared ReadOnly MaximumProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Maximum), GetType(Double), GetType(osUpDownTextBox),
                      New PropertyMetadata(100.0))

        Public Property Maximum As Double
            Get
                Return CDbl(GetValue(MaximumProperty))
            End Get
            Set(value As Double)
                SetValue(MaximumProperty, value)
            End Set
        End Property

        Public Shared ReadOnly IncrementProperty As DependencyProperty =
        DependencyProperty.Register(
            NameOf(Increment),
            GetType(Double),
            GetType(osUpDownTextBox),
            New PropertyMetadata(1.0))

        Public Property Increment As Double
            Get
                Return CDbl(GetValue(IncrementProperty))
            End Get
            Set(value As Double)
                SetValue(IncrementProperty, value)
            End Set
        End Property

        Private Shared Sub OnValueChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = CType(d, osUpDownTextBox)
            ctrl.Text = ctrl.Value.ToString(CultureInfo.CurrentCulture)
        End Sub

        Protected Overrides Sub OnPreviewTextInput(e As Input.TextCompositionEventArgs)
            If Not IsNumericInput(e.Text) Then
                e.Handled = True
            End If
            MyBase.OnPreviewTextInput(e)
        End Sub

        Protected Overrides Sub OnLostFocus(e As RoutedEventArgs)
            ParseText()
            MyBase.OnLostFocus(e)
        End Sub

        Protected Overrides Sub OnMouseWheel(e As Input.MouseWheelEventArgs)
            If e.Delta > 0 Then
                ChangeValue(+Increment)
            Else
                ChangeValue(-Increment)
            End If
            e.Handled = True
        End Sub

        Protected Overrides Sub OnPreviewKeyDown(e As Input.KeyEventArgs)
            Select Case e.Key
                Case Input.Key.Up
                    ChangeValue(+Increment)
                    e.Handled = True
                Case Input.Key.Down
                    ChangeValue(-Increment)
                    e.Handled = True
            End Select

            MyBase.OnPreviewKeyDown(e)
        End Sub

        Protected Overrides Sub OnPreviewMouseWheel(e As MouseWheelEventArgs)
            MyBase.OnPreviewMouseWheel(e)

            If e.Delta > 0 Then
                ChangeValue(+Increment)
            Else
                ChangeValue(-Increment)
            End If

            e.Handled = True
        End Sub

        Private Sub ParseText()
            Dim val As Double
            If Double.TryParse(Text, val) Then
                Value = Coerce(val)
            Else
                Text = Value.ToString()
            End If
        End Sub

        Private Sub ChangeValue(delta As Double)
            Value = Coerce(Value + delta)
            CaretIndex = Text.Length
        End Sub

        Private Function Coerce(val As Double) As Double
            Return Math.Max(Minimum, Math.Min(Maximum, val))
        End Function

        Private Function IsNumericInput(input As String) As Boolean
            Return Double.TryParse(input, Nothing)
        End Function

    End Class

    Public Class osProgressBar
        Inherits FrameworkElement

        Public Sub New()
            With Me
                Me.EasingFunction = New ExponentialEase() With {
                    .EasingMode = EasingMode.EaseOut
                }
            End With
        End Sub

#Region "Fields for fast/slow animation & cancellation"

        Private _animVersion As Integer = 0

        Private _cts As CancellationTokenSource = Nothing
        Private ReadOnly _lockObj As New Object()
        Private _currentSlowTarget As Double? = Nothing

        Public Event evtProgComplete As EventHandler

        Public Property onLastTask As Boolean = False

        Private _renderingActive As Boolean = False
        Private _renderSw As Stopwatch = New Stopwatch()
        Private _renderFrom As Double = 0
        Private _renderTo As Double = 0
        Private _renderDurationSeconds As Double = 0
        Private _renderEasing As IEasingFunction = New ExponentialEase() With {
            .EasingMode = EasingMode.EaseOut
        }
        Private _renderVersion As Integer = 0
        Private _renderCompletion As Action = Nothing

#End Region

#Region "Dependency Properties"

        Public Shared ReadOnly IsAutoPassProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(IsAutoPass), GetType(Boolean), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(False, FrameworkPropertyMetadataOptions.AffectsRender))

        Private Shared _IsAutoPass As Boolean
        Public Property IsAutoPass As Boolean
            Get
                Return CType(GetValue(IsAutoPassProperty), Boolean)
            End Get
            Set(value As Boolean)
                SetValue(IsAutoPassProperty, value)
                _IsAutoPass = value
            End Set
        End Property

        Public Shared ReadOnly DisplayTextProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(DisplayText), GetType(String), GetType(osProgressBar),
                      New FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.
                      AffectsRender, AddressOf OnDisplayTextChanged))

        Private Shared Sub OnDisplayTextChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = DirectCast(d, osProgressBar)
            Dim newText = TryCast(e.NewValue, String)

            If Not String.IsNullOrEmpty(newText) Then
                If ctrl.DisplayProgressText Then
                    ctrl.ProgressText = Nothing
                    ctrl.InvalidateVisual()
                End If
                ctrl.ProgressText = New ProgMsg(newText, TriggerType.AutoPass, True)
                ctrl.DisplayProgressText = True
            Else
                ctrl.ProgressText = Nothing
                ctrl.DisplayProgressText = False
            End If

            ctrl.InvalidateVisual()
        End Sub

        Public Property DisplayText As String
            Get
                Return CStr(GetValue(DisplayTextProperty))
            End Get
            Set(value As String)
                SetValue(DisplayTextProperty, value)
            End Set
        End Property

        Private _progressText As ProgMsg
        Public Property ProgressText As ProgMsg
            Get
                Return _progressText
            End Get
            Set(value As ProgMsg)
                _progressText = value
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

        Public Shared ReadOnly MinimumProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Minimum), GetType(Double), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property Minimum As Double
            Get
                Return CDbl(GetValue(MinimumProperty))
            End Get
            Set(value As Double)
                SetValue(MinimumProperty, value)
            End Set
        End Property

        Public Shared ReadOnly MaximumProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Maximum), GetType(Double), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property Maximum As Double
            Get
                Return CDbl(GetValue(MaximumProperty))
            End Get
            Set(value As Double)
                SetValue(MaximumProperty, value)
            End Set
        End Property

        Public Shared ReadOnly ProgressProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Progress), GetType(Double), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender,
                                                   AddressOf OnValueChanged, AddressOf CoerceProgress))

        Private Shared Function CoerceProgress(d As DependencyObject, baseValue As Object) As Object
            Dim ctrl = DirectCast(d, osProgressBar)
            Dim v = CDbl(baseValue)
            If Double.IsNaN(v) Then Return ctrl.Minimum
            If v < ctrl.Minimum Then Return ctrl.Minimum
            If v > ctrl.Maximum Then Return ctrl.Maximum
            Return v
        End Function

        Private Shared Sub OnValueChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)

        End Sub

        Public Property Progress As Double
            Get
                Return CDbl(GetValue(ProgressProperty))
            End Get
            Set(value As Double)
                SetValue(ProgressProperty, value)
            End Set
        End Property

        Public Shared ReadOnly AnimatedProgressProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(AnimatedProgress), GetType(Double), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender,
                                                   AddressOf OnAnimatedProgressChanged))

        Private Shared Sub OnAnimatedProgressChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = DirectCast(d, osProgressBar)
            ctrl.InvalidateVisual()
        End Sub

        Public Property AnimatedProgress As Double
            Get
                Return CDbl(GetValue(AnimatedProgressProperty))
            End Get
            Private Set(value As Double)
                SetValue(AnimatedProgressProperty, value)
            End Set
        End Property

        Public Shared ReadOnly FillColorProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(FillColor), GetType(Brush), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property FillColor As Brush
            Get
                Return DirectCast(GetValue(FillColorProperty), Brush)
            End Get
            Set(value As Brush)
                Dim newBrush As osColor.Brush = If(value, osColor.Brushes.Transparent)

                Dim objFreezable = TryCast(newBrush, Freezable)
                EstablishProgFreeze(objFreezable)

                SetValue(FillColorProperty, newBrush)
            End Set
        End Property

        Public Shared ReadOnly TrackBrushProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(TrackBrush), GetType(Brush), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(New SolidColorBrush(Color.FromRgb(&HEE, &HEE, &HEE)),
                                                                      FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property TrackBrush As Brush
            Get
                Return DirectCast(GetValue(TrackBrushProperty), Brush)
            End Get
            Set(value As Brush)
                SetValue(TrackBrushProperty, value)
            End Set
        End Property

        Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(CornerRadius), GetType(CornerRadius), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(New CornerRadius(4.0), FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property CornerRadius As CornerRadius
            Get
                Return CType(GetValue(CornerRadiusProperty), CornerRadius)
            End Get
            Set(value As CornerRadius)
                SetValue(CornerRadiusProperty, value)
            End Set
        End Property

        Public Shared ReadOnly TrackCornerRadiusProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(TrackCornerRadius), GetType(CornerRadius), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(New CornerRadius(0), FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property TrackCornerRadius As CornerRadius
            Get
                Return CType(GetValue(TrackCornerRadiusProperty), CornerRadius)
            End Get
            Set(value As CornerRadius)
                SetValue(TrackCornerRadiusProperty, value)
            End Set
        End Property

        Public Shared ReadOnly FillCornerRadiusProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(FillCornerRadius), GetType(CornerRadius), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(New CornerRadius(0), FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property FillCornerRadius As CornerRadius
            Get
                Return CType(GetValue(FillCornerRadiusProperty), CornerRadius)
            End Get
            Set(value As CornerRadius)
                SetValue(FillCornerRadiusProperty, value)
            End Set
        End Property

        Public Shared ReadOnly AnimationDurationProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(AnimationDuration), GetType(TimeSpan), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(TimeSpan.FromMilliseconds(400)))

        Public Property AnimationDuration As TimeSpan
            Get
                Return CType(GetValue(AnimationDurationProperty), TimeSpan)
            End Get
            Set(value As TimeSpan)
                SetValue(AnimationDurationProperty, value)
            End Set
        End Property

        Public Shared ReadOnly EasingFunctionProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(EasingFunction), GetType(IEasingFunction), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(Nothing))

        Public Property EasingFunction As IEasingFunction
            Get
                Return DirectCast(GetValue(EasingFunctionProperty), IEasingFunction)
            End Get
            Set(value As IEasingFunction)
                SetValue(EasingFunctionProperty, value)
            End Set
        End Property

        Public Shared ReadOnly BorderBrushProperty As DependencyProperty = DependencyProperty.
            Register("BorderBrush", GetType(osColor.Brush), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(osColor.Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property BorderBrush As osColor.Brush
            Get
                Return CType(GetValue(BorderBrushProperty), osColor.Brush)
            End Get
            Set(value As osColor.Brush)
                SetValue(BorderBrushProperty, value)
            End Set
        End Property

        Public Shared ReadOnly BorderThicknessProperty As DependencyProperty = DependencyProperty.
            Register("BorderThickness", GetType(Thickness), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(New Thickness(0), FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property BorderThickness As Thickness
            Get
                Return CType(GetValue(BorderThicknessProperty), Thickness)
            End Get
            Set(value As Thickness)
                SetValue(BorderThicknessProperty, value)
            End Set
        End Property

        Public Shared ReadOnly FillPixelToleranceProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(FillPixelTolerance), GetType(Double), GetType(osProgressBar),
                     New FrameworkPropertyMetadata(0.5))

        Public Property FillPixelTolerance As Double
            Get
                Return CDbl(GetValue(FillPixelToleranceProperty))
            End Get
            Set(value As Double)
                SetValue(FillPixelToleranceProperty, value)
            End Set
        End Property

        Private _filledSignaled As Boolean = False
        Public ReadOnly Property IsFilled As Boolean
            Get
                Return _filledSignaled
            End Get
        End Property

#End Region

#Region "Convenience API (direct percent-based setters)"

        Private Sub CheckAndRaiseFilled(pWidth As Double)
            Dim w = Me.RenderSize.Width
            If w <= 0 Then Return

            Dim isNowFull As Boolean = (pWidth >= (w - FillPixelTolerance))

            If isNowFull AndAlso Not _filledSignaled Then
                _filledSignaled = True

                Try
                    RaiseEvent evtProgComplete(Me, EventArgs.Empty)
                Catch ex As Exception : End Try

            ElseIf Not isNowFull AndAlso _filledSignaled Then
                _filledSignaled = False
            End If
        End Sub

#End Region

#Region "Layout & Rendering"

        Protected Overrides Function MeasureOverride(availableSize As Size) As Size
            Dim w = If(Double.IsInfinity(availableSize.Width), 280, availableSize.Width)
            Dim h = If(Double.IsInfinity(availableSize.Height), 28, availableSize.Height)

            Return New Size(w, h)
        End Function

        Protected Overrides Function ArrangeOverride(finalSize As Size) As Size
            Return finalSize
        End Function

        Private Function GetProgRatio() As Double
            Dim valProgRatio = (Progress - Minimum) / (Maximum - Minimum)
            Dim ratio = Math.Max(0.0, Math.Min(1.0, valProgRatio))

            ' If isAutoPass is True, invert the ratio so progress goes down.
            If IsAutoPass Then
                ratio = 1.0 - ratio
            End If

            Return ratio
        End Function

        Private Shared Function ProgressType() As TriggerType
            Return If(_IsAutoPass, TriggerType.AutoPass,
                TriggerType.AutoCast)
        End Function

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
            Me.Progress = 0
        End Sub

        Private Sub DisplayMaxVal()
            Me.Progress = Me.Maximum
        End Sub

        Private Sub DisplayMsg(txtMsg As String, pType As TriggerType)
            DisplayProgressText = True
            ProgressText = New ProgMsg(txtMsg, pType, False)
        End Sub

        Private Sub ClearMsg(Optional pType As TriggerType = Nothing)
            DisplayProgressText = False
        End Sub

        Protected Overrides Sub OnRender(dc As DrawingContext)
            MyBase.OnRender(dc)

            Dim w = Me.RenderSize.Width
            Dim h = Me.RenderSize.Height

            Dim pMax = Me.Maximum

            If w <= 0 OrElse h <= 0 Then Return

            Dim progTrack_Rect As New Rect(0, 0, w, h)
            Dim progTrack_CornerRadius = GetClampedCornerRadius(Me.TrackCornerRadius, w, h)

            Dim progTrack_Geometry = CreateRoundRectGeometry(progTrack_Rect, progTrack_CornerRadius)
            dc.DrawGeometry(If(TrackBrush, Brushes.LightGray), Nothing, progTrack_Geometry)

            EstablishProgFreeze(TryCast(progTrack_Geometry, Freezable))
            dc.PushClip(progTrack_Geometry)

            ' Get (possibly inverted) ratio
            Dim progressRatio = GetProgRatio()

            Dim fillWidth = ActualWidth * progressRatio
            Dim fillP = progressRatio * 100

            If fillWidth > 0.0001 Then
                ' If isAutoPass is True, draw the fill anchored to the right.
                Dim startX As Double = If(IsAutoPass, ActualWidth - fillWidth, 0.0)
                Dim progFill_Rect As New Rect(0, 0, fillWidth, h)
                Dim progFill_CornerRadius As CornerRadius

                If fillP >= 100.0 - 2.5 Then
                    progFill_CornerRadius = GetClampedCornerRadius(Me.FillCornerRadius, fillWidth, h)
                Else
                    progFill_CornerRadius = New CornerRadius(Me.FillCornerRadius.TopLeft, 0.0,
                                                             0.0, Me.FillCornerRadius.BottomLeft)
                    progFill_CornerRadius = GetClampedCornerRadius(progFill_CornerRadius, fillWidth, h)
                End If

                Dim progFill_Geometry = CreateRoundRectGeometry(progFill_Rect, progFill_CornerRadius)
                dc.DrawGeometry(If(FillColor, Brushes.DodgerBlue), Nothing, progFill_Geometry)

                dc.Pop()
            End If

            If BorderBrush IsNot Nothing AndAlso
        (BorderThickness.Left > 0 OrElse BorderThickness.Top > 0 OrElse
         BorderThickness.Right > 0 OrElse BorderThickness.Bottom > 0) Then

                Dim objBorder_Thickness = BorderThickness
                Dim objBorder_Width = ActualWidth
                Dim objBorder_Height = ActualHeight

                If objBorder_Thickness.Left > 0 Then
                    Dim objBorder_Left = ApplyPen(BorderBrush, objBorder_Thickness.Left)
                    EstablishProgFreeze(TryCast(objBorder_Left, Freezable))

                    dc.DrawLine(objBorder_Left, New Point(objBorder_Thickness.Left / 2, 0),
                        New Point(objBorder_Thickness.Left / 2, objBorder_Height))
                End If

                If objBorder_Thickness.Top > 0 Then
                    Dim objBorder_Top = ApplyPen(BorderBrush, objBorder_Thickness.Top)
                    EstablishProgFreeze(TryCast(objBorder_Top, Freezable))

                    dc.DrawLine(objBorder_Top, New Point(0, objBorder_Thickness.Top / 2),
                        New Point(objBorder_Width, objBorder_Thickness.Top / 2))
                End If

                If objBorder_Thickness.Right > 0 Then
                    Dim objBorder_Right = ApplyPen(BorderBrush, objBorder_Thickness.Right)
                    EstablishProgFreeze(TryCast(objBorder_Right, Freezable))

                    dc.DrawLine(objBorder_Right, New Point(objBorder_Width - (objBorder_Thickness.Right / 2), 0),
                        New Point(objBorder_Width - (objBorder_Thickness.Right / 2), objBorder_Height))
                End If

                If objBorder_Thickness.Bottom > 0 Then
                    Dim objBorder_Bottom = ApplyPen(BorderBrush, objBorder_Thickness.Bottom)
                    EstablishProgFreeze(TryCast(objBorder_Bottom, Freezable))

                    dc.DrawLine(objBorder_Bottom, New Point(0, objBorder_Height - (objBorder_Thickness.Bottom / 2)),
                        New Point(objBorder_Width, objBorder_Height - (objBorder_Thickness.Bottom / 2)))
                End If
            End If

            If DisplayProgressText Then
                With ProgressText
                    dc.DrawText(.txtComposed, .txtLocation)
                End With
            End If

            CheckAndRaiseFilled(fillWidth)

        End Sub

        Public Sub SetProgColor(pColor As osColor.Color, Optional pUpdate As Boolean = False)
            If pUpdate Then InvalidateVisual()
        End Sub

        Public Sub SetProgress(setProgVal As Double)

        End Sub

        Private Function ApplyPen(brdrBrush As osColor.Brush, brdrThick As Double) As osColor.Pen
            Return New osColor.Pen(brdrBrush, brdrThick)
        End Function

        Private Shared Sub EstablishProgFreeze(ByRef objPF As Freezable)
            If objPF IsNot Nothing AndAlso objPF.CanFreeze AndAlso Not objPF.IsFrozen Then
                objPF.Freeze()
            End If
        End Sub

#End Region

#Region "Round rect geometry helpers"

        Private Function GetClampedCornerRadius(cr As CornerRadius, w As Double, h As Double) As CornerRadius
            Dim maxR = Math.Min(w, h) / 2.0

            Return New CornerRadius(
                Math.Max(0, Math.Min(cr.TopLeft, maxR)),
                Math.Max(0, Math.Min(cr.TopRight, maxR)),
                Math.Max(0, Math.Min(cr.BottomRight, maxR)),
                Math.Max(0, Math.Min(cr.BottomLeft, maxR))
            )
        End Function

        Private Function CreateRoundRectGeometry(r As Rect, cr As CornerRadius) As StreamGeometry
            Dim g As New StreamGeometry()

            Using ctx = g.Open()
                ctx.BeginFigure(New Point(r.X + cr.TopLeft, r.Y), True, True)

                ctx.LineTo(New Point(r.Right - cr.TopRight, r.Y), True, False)
                If cr.TopRight > 0 Then
                    ctx.ArcTo(New Point(r.Right, r.Y + cr.TopRight),
                              New Size(cr.TopRight, cr.TopRight), 0, False,
                              SweepDirection.Clockwise, True, False)
                End If

                ctx.LineTo(New Point(r.Right, r.Bottom - cr.BottomRight), True, False)
                If cr.BottomRight > 0 Then
                    ctx.ArcTo(New Point(r.Right - cr.BottomRight, r.Bottom),
                      New Size(cr.BottomRight, cr.BottomRight), 0, False,
                      SweepDirection.Clockwise, True, False)
                End If

                ctx.LineTo(New Point(r.X + cr.BottomLeft, r.Bottom), True, False)
                If cr.BottomLeft > 0 Then
                    ctx.ArcTo(New Point(r.X, r.Bottom - cr.BottomLeft),
                      New Size(cr.BottomLeft, cr.BottomLeft), 0, False,
                      SweepDirection.Clockwise, True, False)
                End If

                ctx.LineTo(New Point(r.X, r.Y + cr.TopLeft), True, False)
                If cr.TopLeft > 0 Then
                    ctx.ArcTo(New Point(r.X + cr.TopLeft, r.Y),
                      New Size(cr.TopLeft, cr.TopLeft), 0, False,
                      SweepDirection.Clockwise, True, False)
                End If
            End Using

            g.Freeze()
            Return g
        End Function

#End Region

    End Class

    Partial Public Class osRestartProgress
        Inherits UserControl

        Public Sub New()
            InitializeComponent()

            AddHandler Me.Loaded, AddressOf Spinner_Loaded
            AddHandler Me.Unloaded, AddressOf Spinner_Unloaded
        End Sub

        Private Sub Spinner_Loaded(sender As Object, e As RoutedEventArgs)
            UpdateStoryboardState()
        End Sub

        Private Sub Spinner_Unloaded(sender As Object, e As RoutedEventArgs)
            StopStoryboard()
        End Sub

        ' DependencyProperty: IsActive (start/stop animation)
        Public Shared ReadOnly IsActiveProperty As DependencyProperty =
                DependencyProperty.Register("IsActive", GetType(Boolean), GetType(osRestartProgress),
                                            New PropertyMetadata(True, AddressOf OnIsActiveChanged))

        Public Property IsActive As Boolean
            Get
                Return CBool(GetValue(IsActiveProperty))
            End Get
            Set(value As Boolean)
                SetValue(IsActiveProperty, value)
            End Set
        End Property

        Private Shared Sub OnIsActiveChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = DirectCast(d, osRestartProgress)
            ctrl.UpdateStoryboardState()
        End Sub

        ' DependencyProperty: SpinnerSize
        Public Shared ReadOnly SpinnerSizeProperty As DependencyProperty =
                DependencyProperty.Register("SpinnerSize", GetType(Double), GetType(osRestartProgress),
                                            New PropertyMetadata(24.0))

        Public Property SpinnerSize As Double
            Get
                Return CDbl(GetValue(SpinnerSizeProperty))
            End Get
            Set(value As Double)
                SetValue(SpinnerSizeProperty, value)
            End Set
        End Property

        ' DependencyProperty: StrokeThickness
        Public Shared ReadOnly StrokeThicknessProperty As DependencyProperty =
                DependencyProperty.Register("StrokeThickness", GetType(Double), GetType(osRestartProgress),
                                            New PropertyMetadata(6.0))

        Public Property StrokeThickness As Double
            Get
                Return CDbl(GetValue(StrokeThicknessProperty))
            End Get
            Set(value As Double)
                SetValue(StrokeThicknessProperty, value)
            End Set
        End Property

        ' DependencyProperty: SpinnerBrush
        Public Shared ReadOnly SpinnerBrushProperty As DependencyProperty =
                DependencyProperty.Register("SpinnerBrush", GetType(Brush), GetType(osRestartProgress),
                                            New PropertyMetadata(Brushes.DodgerBlue))

        Public Property SpinnerBrush As Brush
            Get
                Return CType(GetValue(SpinnerBrushProperty), Brush)
            End Get
            Set(value As Brush)
                SetValue(SpinnerBrushProperty, value)
            End Set
        End Property

        Private Function GetRotateStoryboard() As Storyboard
            Return TryCast(Me.Resources("RotateStoryboard"), Storyboard)
        End Function

        Private Sub UpdateStoryboardState()
            If Not Me.IsLoaded Then Return
            If IsActive Then
                StartStoryboard()
            Else
                StopStoryboard()
            End If
        End Sub

        Private Sub StartStoryboard()
            Dim sb = GetRotateStoryboard()
            If sb IsNot Nothing Then
                sb.Begin(Me, True) ' controllable
            End If
        End Sub

        Private Sub StopStoryboard()
            Dim sb = GetRotateStoryboard()
            If sb IsNot Nothing Then
                sb.Stop(Me)
            End If
        End Sub
    End Class

End Namespace
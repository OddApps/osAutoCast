Imports System.Windows.Markup
Imports System.Globalization
Imports System.Windows.Media.Effects
Imports osAutoCast.DataTypeLib.MenuProperty
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.DataTypeLib.VisualEasing
Imports osAutoCast.DataTypeLib.PrefUI_State
Imports System.Windows.Media.Animation
Imports System.Windows.Controls.Primitives
Imports System
Imports System.Windows
Imports System.Windows.Data
Imports System.Windows.Media
Imports System.ComponentModel

Namespace osStyle

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

        Private ReadOnly _clipGeometry As New RectangleGeometry()
        Private _oldChildClip As Object
        Private _isRenderingHooked As Boolean

        ' DependencyProperty so you can tweak inflation if needed (default 0.75)
        Public Shared ReadOnly ClipInflationProperty As DependencyProperty =
        DependencyProperty.Register("ClipInflation", GetType(Double), GetType(osBorder),
            New FrameworkPropertyMetadata(0.75, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property ClipInflation As Double
            Get
                Return CDbl(GetValue(ClipInflationProperty))
            End Get
            Set(value As Double)
                SetValue(ClipInflationProperty, value)
            End Set
        End Property

        Public Sub New()
            ' Improve pixel alignment to reduce subpixel artifacts
            Me.SetValue(FrameworkElement.UseLayoutRoundingProperty, True)
            Me.SetValue(UIElement.SnapsToDevicePixelsProperty, True)
            'UseLayoutRounding = True
            'SnapsToDevicePixels = True
        End Sub

        ' Preserve and restore child's original Clip if child changes
        Public Overrides Property Child As UIElement
            Get
                Return MyBase.Child
            End Get
            Set(value As UIElement)
                If Me.Child IsNot value Then
                    ' restore old child's clip (if we replaced it)
                    If Me.Child IsNot Nothing Then
                        Me.Child.SetValue(UIElement.ClipProperty, _oldChildClip)
                    End If

                    If value IsNot Nothing Then
                        _oldChildClip = value.ReadLocalValue(UIElement.ClipProperty)
                    Else
                        _oldChildClip = Nothing
                    End If

                    MyBase.Child = value
                End If
            End Set
        End Property

        Protected Overrides Sub OnVisualParentChanged(oldParent As DependencyObject)
            MyBase.OnVisualParentChanged(oldParent)
            If VisualParent Is Nothing Then
                UnhookRendering()
            Else
                HookRendering()
            End If
        End Sub

        Private Sub HookRendering()
            If _isRenderingHooked Then Return
            AddHandler CompositionTarget.Rendering, AddressOf OnRendering
            _isRenderingHooked = True
        End Sub

        Private Sub UnhookRendering()
            If Not _isRenderingHooked Then Return
            RemoveHandler CompositionTarget.Rendering, AddressOf OnRendering
            _isRenderingHooked = False
        End Sub

        Private Sub OnRendering(sender As Object, e As EventArgs)
            UpdateChildClip()
        End Sub

        'Protected Overrides Sub OnRender(dc As DrawingContext)
        '    MyBase.OnRender(dc)
        '    UpdateChildClip()
        'End Sub

        'Protected Overrides Sub OnRenderSizeChanged(sizeInfo As SizeChangedInfo)
        '    MyBase.OnRenderSizeChanged(sizeInfo)
        '    UpdateChildClip()
        'End Sub

        Private Sub UpdateChildClip()
            Dim child = Me.Child
            If child Is Nothing Then Return

            ' Ensure layout rounding has occurred so RenderSize is device aligned
            Dim w As Double = Math.Max(0.0, child.RenderSize.Width)
            Dim h As Double = Math.Max(0.0, child.RenderSize.Height)


            Dim size = child.RenderSize
            If size.Width <= 0 OrElse size.Height <= 0 Then Return
            ' Clip inflation to cover anti-aliasing / stroke half-width
            Dim inflation As Double = Math.Max(0.0, Me.ClipInflation)

            ' Build rect starting at 0,0 (child coordinates). Inflate slightly.
            Dim rect As New Rect(0, 0, w, h)
            rect.Inflate(inflation, inflation)

            _clipGeometry.Rect = rect

            ' Compute radius: compensate for BorderThickness (stroke centered on edge)
            ' and add inflation so the clip slightly 'rounds' a bit larger than inner edge.
            Dim baseRadius As Double = 0.0
            ' Use TopLeft radius as representative; can be extended for per-corner radii.
            baseRadius = Me.CornerRadius.TopLeft

            ' borderThickness/2 is the inward part of stroke; subtract it so clip aligns to inner edge
            Dim halfStroke As Double = (Me.BorderThickness.Left + Me.BorderThickness.Top + Me.BorderThickness.Right + Me.BorderThickness.Bottom) / 4.0
            ' More correct approach would consider each edge; using average is fine for most cases.

            Dim radius As Double = Math.Max(0.0, baseRadius - (halfStroke / 2.0) + inflation)
            _clipGeometry.RadiusX = radius
            _clipGeometry.RadiusY = radius

            ' Assign the same RectangleGeometry instance to child's Clip (no new allocations each frame)
            child.Clip = _clipGeometry

            ' Optional: force child to snap to pixels (helps some artefacts)
            '   child.SetValue(UIElement.SnapsToDevicePixelsProperty, True)

            ' NOTE: If you prefer crisp edges you can force aliasing (jagged) by uncommenting:
            ' RenderOptions.SetEdgeMode(child, EdgeMode.Aliased)
        End Sub
    End Class

    Public Class OsRoundedForm
        Inherits ContentControl

        Private _path As Path

        Private ReadOnly _clipGeometry As New RectangleGeometry()
        Private _isRenderingHooked As Boolean
        Private _transformHost As FrameworkElement

        Shared Sub New()
            DefaultStyleKeyProperty.OverrideMetadata(
                GetType(OsRoundedForm), New FrameworkPropertyMetadata(GetType(OsRoundedForm)))
        End Sub

        Public Sub New()
            AddHandler Loaded, Sub()
                                   UpdateGeometry()
                               End Sub
        End Sub

        Public Shared ReadOnly ClipInflationProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(ClipInflation), GetType(Double), GetType(OsRoundedForm),
                      New FrameworkPropertyMetadata(0.75, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property ClipInflation As Double
            Get
                Return CDbl(GetValue(ClipInflationProperty))
            End Get
            Set(value As Double)
                SetValue(ClipInflationProperty, value)
            End Set
        End Property

        ' Private _LayoutScale As New ScaleTransform(0, 0.025)
        Public ReadOnly Property LayoutScale As ScaleTransform
            Get
                Return DirectCast(ContentResource.LayoutTransform, ScaleTransform)
            End Get
        End Property

        Private Shared Property _ContentResource As ContentPresenter
        Public Property ContentResource As ContentPresenter
            Get
                Return _ContentResource
            End Get
            Set(value As ContentPresenter)
                _ContentResource = value
            End Set
        End Property

        Public Property CornerRadius As Double
            Get
                Return CDbl(GetValue(CornerRadiusProperty))
            End Get
            Set(value As Double)
                SetValue(CornerRadiusProperty, value)
            End Set
        End Property

        Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(CornerRadius), GetType(Double), GetType(OsRoundedForm),
                      New PropertyMetadata(12.0, AddressOf OnVisualChanged))

        Public Overloads Property BorderBrush As Brush
            Get
                Return CType(GetValue(BorderBrushProperty), Brush)
            End Get
            Set(value As Brush)
                SetValue(BorderBrushProperty, value)
            End Set
        End Property

        Public Shared Shadows ReadOnly BorderBrushProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(BorderBrush), GetType(Brush), GetType(OsRoundedForm),
                      New FrameworkPropertyMetadata(Brushes.Gray,
                                                    FrameworkPropertyMetadataOptions.AffectsRender))

        Public Shared Shadows ReadOnly BorderThicknessProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(BorderThickness), GetType(Thickness), GetType(OsRoundedForm),
                      New FrameworkPropertyMetadata(New Thickness(1),
                                                    FrameworkPropertyMetadataOptions.AffectsRender))

        Public Shadows Property BorderThickness As Thickness
            Get
                Return CType(GetValue(BorderThicknessProperty), Thickness)
            End Get
            Set(value As Thickness)
                SetValue(BorderThicknessProperty, value)
            End Set
        End Property

        Public Overloads Property Background As Brush
            Get
                Return CType(GetValue(BackgroundProperty), Brush)
            End Get
            Set(value As Brush)
                SetValue(BackgroundProperty, value)
            End Set
        End Property

        Public Shared Shadows ReadOnly BackgroundProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Background), GetType(Brush), GetType(OsRoundedForm),
                     New FrameworkPropertyMetadata(Brushes.LightGray,
                                                    FrameworkPropertyMetadataOptions.AffectsRender))

        Private Sub UpdateGeometry()

            If _path Is Nothing Then Return

            ' --- DESIGN-TIME SAFE SIZE ---
            Dim w As Double = If(ActualWidth > 0, ActualWidth, Width)
            Dim h As Double = If(ActualHeight > 0, ActualHeight, Height)

            ' Designer still hasn't measured
            'If Double.IsNaN(w) OrElse Double.IsNaN(h) OrElse w <= 0 OrElse h <= 0 Then
            '    w = 300
            '    h = 200
            'End If

            Dim r As Double = Math.Min(CornerRadius, Math.Min(w, h) / 2)

            Dim geo As New StreamGeometry()
            Using ctx = geo.Open()

                ctx.BeginFigure(New Point(r, 0), True, True)

                ctx.LineTo(New Point(w - r, 0), True, False)
                ctx.ArcTo(New Point(w, r), New Size(r, r), 0, False,
                  SweepDirection.Clockwise, True, False)

                ctx.LineTo(New Point(w, h - r), True, False)
                ctx.ArcTo(New Point(w - r, h), New Size(r, r), 0, False,
                  SweepDirection.Clockwise, True, False)

                ctx.LineTo(New Point(r, h), True, False)
                ctx.ArcTo(New Point(0, h - r), New Size(r, r), 0, False,
                  SweepDirection.Clockwise, True, False)

                ctx.LineTo(New Point(0, r), True, False)
                ctx.ArcTo(New Point(r, 0), New Size(r, r), 0, False,
                  SweepDirection.Clockwise, True, False)
            End Using

            geo.Freeze()

            _path.Data = geo
            Clip = geo
        End Sub

        Private Sub UpdateInnerClip()

            If _transformHost Is Nothing Then Return

            Dim size = _transformHost.RenderSize
            If size.Width <= 0 OrElse size.Height <= 0 Then Return

            Dim inflation As Double = Math.Max(0.0, ClipInflation)

            Dim rect As New Rect(0, 0, size.Width, size.Height)
            rect.Inflate(inflation, inflation)

            _clipGeometry.Rect = rect

            ' --- radius compensation ---
            Dim baseRadius As Double = CornerRadius

            Dim avgStroke As Double =
        (BorderThickness.Left +
         BorderThickness.Top +
         BorderThickness.Right +
         BorderThickness.Bottom) / 4.0

            Dim radius As Double =
        Math.Max(0.0, baseRadius - (avgStroke / 2.0) + inflation)

            _clipGeometry.RadiusX = radius
            _clipGeometry.RadiusY = radius

            _transformHost.Clip = _clipGeometry
        End Sub

        Protected Overrides Sub OnRenderSizeChanged(sizeInfo As SizeChangedInfo)
            MyBase.OnRenderSizeChanged(sizeInfo)

            UpdateGeometry()

        End Sub

        Public Overrides Sub OnApplyTemplate()
            MyBase.OnApplyTemplate()

            _transformHost = TryCast(GetTemplateChild("PART_TransformHost"), FrameworkElement)
            ContentResource = TryCast(Me.GetTemplateChild("PART_TransformHost"), ContentPresenter)

            UpdateInnerClip()
        End Sub

        Protected Overrides Sub OnVisualParentChanged(oldParent As DependencyObject)
            MyBase.OnVisualParentChanged(oldParent)

            If VisualParent Is Nothing Then
                UnhookRendering()
            Else : HookRendering() : End If
        End Sub

        Private Sub HookRendering()
            If _isRenderingHooked Then Return

            AddHandler CompositionTarget.Rendering, AddressOf OnRendering
            _isRenderingHooked = True
        End Sub

        Private Sub UnhookRendering()
            If Not _isRenderingHooked Then Return

            RemoveHandler CompositionTarget.Rendering, AddressOf OnRendering
            _isRenderingHooked = False
        End Sub

        Private Sub OnRendering(sender As Object, e As EventArgs)
            UpdateInnerClip()
        End Sub

        Private Shared Sub OnVisualChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            CType(d, OsRoundedForm).UpdateGeometry()
        End Sub

        Private Sub OsRoundedForm_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            ApplyTemplate()
        End Sub

    End Class

    Public Class osStyles
        Inherits DependencyObject

        Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("CornerRadius", GetType(CornerRadius), GetType(osStyles),
                             New PropertyMetadata(New CornerRadius(0)))

        Public Shared ReadOnly BorderThicknessProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("BorderThickness", GetType(Thickness), GetType(osStyles),
                             New PropertyMetadata(New Thickness(1)))

        Public Shared ReadOnly BorderBrushProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("BorderBrush", GetType(Brush), GetType(osStyles),
                             New PropertyMetadata(Brushes.Transparent))

        Public Shared ReadOnly BackgroundProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("Background", GetType(Brush), GetType(osStyles),
                             New PropertyMetadata(Brushes.Transparent))

        Public Shared ReadOnly DefaultBackgroundProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("DefaultBackground", GetType(Brush), GetType(osStyles),
                             New FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Shared ReadOnly TemplateProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("Template", GetType(ControlTemplate), GetType(osStyles),
                             New PropertyMetadata(Nothing, AddressOf UpdateView))

        Public Shared Sub SetCornerRadius(ByVal objBorder As UIElement, ByVal valRadius As CornerRadius)
            objBorder.SetValue(CornerRadiusProperty, valRadius)
        End Sub

        Public Shared Function GetCornerRadius(ByVal objBorder As UIElement) As CornerRadius
            Return CType(objBorder.GetValue(CornerRadiusProperty), CornerRadius)
        End Function

        Public Shared Sub SetBorderThickness(ByVal objBorder As UIElement, ByVal valThickness As Thickness)
            objBorder.SetValue(BorderThicknessProperty, valThickness)
        End Sub

        Public Shared Function GetBorderThickness(ByVal objBorder As UIElement) As Thickness
            Return CType(objBorder.GetValue(BorderThicknessProperty), Thickness)
        End Function

        Public Shared Sub SetBorderBrush(ByVal objBorder As UIElement, ByVal valBorderColor As Brush)
            objBorder.SetValue(BorderBrushProperty, valBorderColor)
        End Sub

        Public Shared Function GetBorderBrush(ByVal objBorder As UIElement) As Brush
            Return CType(objBorder.GetValue(BorderBrushProperty), Brush)
        End Function

        Public Shared Sub SetBackground(ByVal objButton As UIElement, ByVal valBackground As Brush)
            objButton.SetValue(BackgroundProperty, valBackground)
        End Sub

        Public Shared Function GetBackground(ByVal objButton As UIElement) As Brush
            Return CType(objButton.GetValue(BackgroundProperty), Brush)
        End Function

        Public Shared Sub SetDefaultBackground(ByVal objButton As UIElement, ByVal valDefaultBackground As Brush)
            objButton.SetValue(DefaultBackgroundProperty, valDefaultBackground)
        End Sub

        Public Shared Function GetDefaultBackground(ByVal objButton As UIElement) As Brush
            Return CType(objButton.GetValue(DefaultBackgroundProperty), Brush)
        End Function

        Public Shared Sub SetTemplate(ByVal objTemplate As DependencyObject, ByVal valTemplate As ControlTemplate)
            objTemplate.SetValue(TemplateProperty, valTemplate)
        End Sub

        Public Shared Function GetTemplate(ByVal objTemplate As DependencyObject) As ControlTemplate
            Return CType(objTemplate.GetValue(TemplateProperty), ControlTemplate)
        End Function

        Private Shared Sub UpdateView(depObj As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim objTemplate = FindTemplate(depObj)
            If objTemplate Is Nothing Then Return

            Dim objView = InitView(e.NewValue)
            objTemplate.Template = objView
        End Sub

        Private Shared Function FindTemplate(depObj As DependencyObject) As Control
            Return TryCast(depObj, Control)
        End Function

        Private Shared Function InitView(objV As Object) As ControlTemplate
            Return TryCast(objV, ControlTemplate)
        End Function

    End Class

    Public Class osContainerLayout

        Private Const SeparatorTag As String = "Content_Separator"

        Public Shared ReadOnly RowsProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("Rows", GetType(String), GetType(osContainerLayout),
                             New PropertyMetadata("", AddressOf UpdateContainerRows))

        Public Shared ReadOnly SeparatorBrushProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("SeparatorBrush", GetType(Brush), GetType(osContainerLayout),
                             New PropertyMetadata(Brushes.LightGray, AddressOf UpdateContentSeperator))

        Public Shared Sub SetSeparatorBrush(objSeparator As DependencyObject, value As Brush)
            objSeparator.SetValue(SeparatorBrushProperty, value)
        End Sub

        Public Shared Function GetSeparatorBrush(objSeparator As DependencyObject) As Brush
            Return CType(objSeparator.GetValue(SeparatorBrushProperty), Brush)
        End Function

        Public Shared ReadOnly SeparatorThicknessProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("SeparatorThickness", GetType(Double), GetType(osContainerLayout),
                             New PropertyMetadata(1.0, AddressOf UpdateContentSeperator))

        Public Shared Sub SetSeparatorThickness(objSeparator As DependencyObject, value As Double)
            objSeparator.SetValue(SeparatorThicknessProperty, value)
        End Sub

        Public Shared Function GetSeparatorThickness(objSeparator As DependencyObject) As Double
            Return CType(objSeparator.GetValue(SeparatorThicknessProperty), Double)
        End Function

        Private Shared Sub UpdateContentSeperator(objCont As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim objContainer = FindContainer(objCont)
            If objContainer Is Nothing Then Return

            If objContainer.IsLoaded Then
                GenerateContainer(objContainer)
            Else
                RemoveHandler objContainer.Loaded, AddressOf Grid_Loaded
                AddHandler objContainer.Loaded, AddressOf Grid_Loaded
            End If
        End Sub

        Private Shared Sub UpdateContainerRows(objCont As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim objContainer = FindContainer(objCont)
            If objContainer Is Nothing Then Return

            If objContainer.IsLoaded Then
                GenerateContainer(objContainer)
            Else
                RemoveHandler objContainer.Loaded, AddressOf Grid_Loaded
                AddHandler objContainer.Loaded, AddressOf Grid_Loaded
            End If
        End Sub

        Private Shared Sub Grid_Loaded(sender As Object, e As RoutedEventArgs)
            Dim objContainer = FindContainer(sender)
            If objContainer Is Nothing Then Return

            RemoveHandler objContainer.Loaded, AddressOf Grid_Loaded
            GenerateContainer(objContainer)
        End Sub

        Private Shared Function FindContainer(depObj As DependencyObject) As Grid
            Return TryCast(depObj, Grid)
        End Function

        Private Shared Function FindContainer(sObj As Object) As Grid
            Return TryCast(sObj, Grid)
        End Function

        Public Shared Sub SetRows(objRows As DependencyObject, value As String)
            objRows.SetValue(RowsProperty, value)
        End Sub

        Public Shared Function GetRows(objRows As DependencyObject) As String
            Return CType(objRows.GetValue(RowsProperty), String)
        End Function

        Private Shared Sub GenerateContainer(grid As Grid)
            Dim rowsSpec = GetRows(grid)
            If rowsSpec Is Nothing Then rowsSpec = ""

            Dim parts = rowsSpec.Split(New Char() {","c},
                                       StringSplitOptions.RemoveEmptyEntries)

            Dim contentCount = parts.Length
            If contentCount = 0 Then
                Return
            End If

            For i As Integer = grid.Children.Count - 1 To 0 Step -1
                Dim fe = TryCast(grid.Children(i), FrameworkElement)

                If fe IsNot Nothing AndAlso fe.Tag IsNot Nothing AndAlso fe.Tag.ToString() = SeparatorTag Then
                    grid.Children.RemoveAt(i)
                End If
            Next

            Dim childMeta As New List(Of Tuple(Of UIElement, Integer, Integer))()

            For Each chObj As UIElement In grid.Children
                Dim fe = TryCast(chObj, FrameworkElement)

                If fe Is Nothing Then Continue For
                If fe.Tag IsNot Nothing AndAlso fe.Tag.ToString() = SeparatorTag Then Continue For

                Dim origRow As Integer = Grid.GetRow(chObj)
                Dim origRowSpan As Integer = Grid.GetRowSpan(chObj)

                If origRowSpan < 1 Then origRowSpan = 1

                If origRow < 0 Then origRow = 0
                If origRow >= contentCount Then origRow = contentCount - 1

                If origRow + origRowSpan > contentCount Then
                    origRowSpan = Math.Max(1, contentCount - origRow)
                End If

                childMeta.Add(Tuple.Create(chObj, origRow, origRowSpan))
            Next

            childMeta = childMeta.OrderBy(Function(t) t.Item2).ThenBy(Function(t) t.Item1.GetHashCode()).ToList()

            grid.RowDefinitions.Clear()

            For i As Integer = 0 To contentCount - 1
                Dim token = parts(i).Trim()
                Dim defContent As New RowDefinition()

                If String.Equals(token, "auto", StringComparison.OrdinalIgnoreCase) Then
                    defContent.Height = GridLength.Auto
                ElseIf token.EndsWith("*"c) Then
                    Dim starPart = token.TrimEnd("*"c)
                    Dim value As Double = 1.0

                    If Not String.IsNullOrEmpty(starPart) Then
                        Double.TryParse(starPart, value)
                    End If

                    defContent.Height = New GridLength(value, GridUnitType.Star)
                Else
                    Dim px As Double = 0

                    If Double.TryParse(token, px) Then
                        defContent.Height = New GridLength(px, GridUnitType.Pixel)
                    Else
                        defContent.Height = GridLength.Auto
                    End If
                End If

                grid.RowDefinitions.Add(defContent)

                If i < contentCount - 1 Then
                    Dim sepThickness = GetSeparatorThickness(grid)
                    Dim defSep As New RowDefinition()

                    If sepThickness <= 0 Then
                        defSep.Height = New GridLength(0, GridUnitType.Pixel)
                    Else
                        defSep.Height = New GridLength(sepThickness, GridUnitType.Pixel)
                    End If

                    grid.RowDefinitions.Add(defSep)
                End If
            Next

            Dim totalRows = grid.RowDefinitions.Count

            For Each tup In childMeta
                Dim ch = tup.Item1
                Dim origRow = tup.Item2
                Dim origSpan = tup.Item3

                Dim newRow = Math.Max(0, origRow * 2)
                Dim newSpan = Math.Max(1, origSpan * 2 - 1)

                If newRow > totalRows - 1 Then
                    newRow = Math.Max(0, totalRows - 1)
                End If

                If newRow + newSpan > totalRows Then
                    newSpan = Math.Max(1, totalRows - newRow)
                End If

                Grid.SetRow(ch, newRow)
                Grid.SetRowSpan(ch, newSpan)
            Next

            Dim sepBrush = GetSeparatorBrush(grid)
            Dim sepThicknessFinal = GetSeparatorThickness(grid)

            If sepThicknessFinal > 0 AndAlso contentCount > 1 Then
                Dim colSpan As Integer = If(grid.ColumnDefinitions.Count > 0, grid.ColumnDefinitions.Count, 1)

                For i As Integer = 0 To contentCount - 2
                    Dim sepRowIndex = i * 2 + 1

                    Dim rect As New Rectangle() With {
                        .HorizontalAlignment = HorizontalAlignment.Stretch,
                        .VerticalAlignment = VerticalAlignment.Stretch,
                        .Fill = If(sepBrush, Brushes.LightGray),
                        .IsHitTestVisible = False,
                        .Tag = SeparatorTag,
                        .SnapsToDevicePixels = True
                    }

                    Grid.SetRow(rect, sepRowIndex)
                    Grid.SetColumn(rect, 0)
                    Grid.SetColumnSpan(rect, colSpan)
                    Grid.SetZIndex(rect, 10000)

                    grid.Children.Add(rect)
                Next
            End If

            Dim wnd = Window.GetWindow(grid)

            If wnd IsNot Nothing Then
                If wnd.SizeToContent = SizeToContent.Manual Then
                    wnd.UpdateLayout()
                    wnd.Height = Double.NaN
                    wnd.UpdateLayout()
                Else
                    wnd.UpdateLayout()
                End If
            Else
                grid.UpdateLayout()
            End If
        End Sub

    End Class

    Public Class osPopupScale

        Public Shared ReadOnly PopupScaleProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("PopupScale", GetType(Double), GetType(osPopupScale),
                             New PropertyMetadata(0.001, AddressOf OnPopupScaleChanged))

        Public Shared Sub SetPopupScale(objPopupScale As DependencyObject, valScale As Double)
            objPopupScale.SetValue(PopupScaleProperty, valScale)
        End Sub

        Public Shared Function GetPopupScale(objPopupScale As DependencyObject) As Double
            Return CDbl(objPopupScale.GetValue(PopupScaleProperty))
        End Function

        Private Shared Sub OnPopupScaleChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim objContainerUI = TryCast(d, UIElement)
            If objContainerUI Is Nothing Then Return

            Dim valScale As Double = CDbl(e.NewValue)

            With EnsureScaleTransform(objContainerUI)
                .ScaleX = valScale
                .ScaleY = valScale
            End With
        End Sub

        Private Shared Function EnsureScaleTransform(objContainer As UIElement) As ScaleTransform
            Dim objVisualTransform = objContainer.RenderTransform

            If objVisualTransform Is Nothing Then
                Dim objVisualScale = New ScaleTransform(0.001, 0.001)
                objContainer.RenderTransform = objVisualScale
                Return objVisualScale
            End If

            Dim objVerifyVisual = TryCast(objVisualTransform, ScaleTransform)
            If objVerifyVisual IsNot Nothing Then Return objVerifyVisual

            Dim chkTransformGroup = TryCast(objVisualTransform, TransformGroup)
            If chkTransformGroup IsNot Nothing Then
                Dim chkVisualGroup = chkTransformGroup.Children.OfType(Of ScaleTransform)().FirstOrDefault()
                If chkVisualGroup IsNot Nothing Then Return chkVisualGroup

                chkVisualGroup = New ScaleTransform(0.001, 0.001)
                chkTransformGroup.Children.Insert(0, chkVisualGroup)
                Return chkVisualGroup
            End If

            Dim objVisualGroup As New TransformGroup()
            Dim objVisualScaler As New ScaleTransform(0.001, 0.001)

            objVisualGroup.Children.Add(objVisualScaler)
            objVisualGroup.Children.Add(objVisualTransform)

            objContainer.RenderTransform = objVisualGroup

            Return objVisualScaler
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

            CommandBindings.Add(
            New CommandBinding(
                osStyle.osUpDownTextBoxCmds.Increase,
                Sub() ChangeValue(+Increment)))

            CommandBindings.Add(
            New CommandBinding(
                osStyle.osUpDownTextBoxCmds.Decrease,
                Sub() ChangeValue(-Increment)))
        End Sub

        '========================
        ' Dependency Properties
        '========================

        Public Shared ReadOnly ValueProperty As DependencyProperty =
        DependencyProperty.Register(
            NameOf(Value),
            GetType(Double),
            GetType(osUpDownTextBox),
            New FrameworkPropertyMetadata(
                0.0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                AddressOf OnValueChanged))

        Public Property Value As Double
            Get
                Return CDbl(GetValue(ValueProperty))
            End Get
            Set(value As Double)
                SetValue(ValueProperty, value)
            End Set
        End Property

        Public Shared ReadOnly MinimumProperty As DependencyProperty =
        DependencyProperty.Register(
            NameOf(Minimum),
            GetType(Double),
            GetType(osUpDownTextBox),
            New PropertyMetadata(0.0))

        Public Property Minimum As Double
            Get
                Return CDbl(GetValue(MinimumProperty))
            End Get
            Set(value As Double)
                SetValue(MinimumProperty, value)
            End Set
        End Property

        Public Shared ReadOnly MaximumProperty As DependencyProperty =
        DependencyProperty.Register(
            NameOf(Maximum),
            GetType(Double),
            GetType(osUpDownTextBox),
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

        '========================
        ' Value Sync
        '========================

        Private Shared Sub OnValueChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = CType(d, osUpDownTextBox)
            ctrl.Text = ctrl.Value.ToString(CultureInfo.CurrentCulture)
        End Sub

        '========================
        ' Input Handling
        '========================

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


End Namespace
Imports System.Windows.Media
Imports System.Windows
Imports System.Windows.Media.Effects
Imports System.IO
Imports System.Globalization
Imports System.Windows.Data
Imports osAutoCast.DataTypeLib.MenuProperty
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.osHandler_Shader
Imports System.Windows.Controls

Public Class osEffect
    Inherits ShaderEffect

    'Private Shared ReadOnly _shader As New PixelShader() With {
    '    .UriSource = New Uri("/osAutoCast;component/DataFiles/StyleData/EffectLib/osShader_Text.ps", UriKind.Relative)
    '}

    'Public Sub New()
    '    With Me
    '        .PixelShader = _shader

    '        .PaddingLeft = 6
    '        .PaddingRight = 6

    '        .PaddingTop = 3
    '        .PaddingBottom = 3
    '    End With

    '    UpdateShaderValue(InputProperty)
    '    UpdateShaderValue(TexelSizeProperty)
    '    UpdateShaderValue(ThicknessProperty)
    '    UpdateShaderValue(SpreadProperty)
    '    UpdateShaderValue(FadeProperty)
    '    UpdateShaderValue(GlowColorProperty)
    '    UpdateShaderValue(StrokeStrengthProperty)
    '    UpdateShaderValue(GlowStrengthProperty)
    'End Sub

    Public Sub New()
        With Me
            .PixelShader = FetchShader(sTypeText).sText

            .PaddingLeft = 6
            .PaddingRight = 6

            .PaddingTop = 3
            .PaddingBottom = 3
        End With

        UpdateShaderValue(InputProperty)
        UpdateShaderValue(TexelSizeProperty)
        UpdateShaderValue(ThicknessProperty)
        UpdateShaderValue(SpreadProperty)
        UpdateShaderValue(FadeProperty)
        UpdateShaderValue(GlowColorProperty)
        UpdateShaderValue(StrokeStrengthProperty)
        UpdateShaderValue(GlowStrengthProperty)
    End Sub

    Public Shared ReadOnly InputProperty As DependencyProperty = ShaderEffect.
        RegisterPixelShaderSamplerProperty("Input", GetType(osEffect), 0)
    Public Property Input As Brush
        Get
            Return CType(GetValue(InputProperty), Brush)
        End Get
        Set(value As Brush)
            SetValue(InputProperty, value)
        End Set
    End Property

    Public Shared ReadOnly TexelSizeProperty As DependencyProperty = GenProp(propTexel)
    Public Property TexelSize As Point
        Get
            Return CType(GetValue(TexelSizeProperty), Point)
        End Get
        Set(value As Point)
            SetValue(TexelSizeProperty, value)
        End Set
    End Property

    Public Shared ReadOnly ThicknessProperty As DependencyProperty = GenProp(propThickness)
    Public Property Thickness As Double
        Get
            Return CDbl(GetValue(ThicknessProperty))
        End Get
        Set(value As Double)
            SetValue(ThicknessProperty, value)
        End Set
    End Property

    Public Shared ReadOnly SpreadProperty As DependencyProperty = GenProp(propSpread)
    Public Property Spread As Double
        Get
            Return CDbl(GetValue(SpreadProperty))
        End Get
        Set(value As Double)
            SetValue(SpreadProperty, value)
        End Set
    End Property

    Public Shared ReadOnly FadeProperty As DependencyProperty = GenProp(propFade)
    Public Property Fade As Double
        Get
            Return CDbl(GetValue(FadeProperty))
        End Get
        Set(value As Double)
            SetValue(FadeProperty, value)
        End Set
    End Property

    Public Shared ReadOnly GlowColorProperty As DependencyProperty = GenProp(propGlowColor)
    Public Property GlowColor As Color
        Get
            Return CType(GetValue(GlowColorProperty), Color)
        End Get
        Set(value As Color)
            SetValue(GlowColorProperty, value)
        End Set
    End Property

    Public Shared ReadOnly StrokeStrengthProperty As DependencyProperty = GenProp(propStroke)
    Public Property StrokeStrength As Double
        Get
            Return CDbl(GetValue(StrokeStrengthProperty))
        End Get
        Set(value As Double)
            SetValue(StrokeStrengthProperty, value)
        End Set
    End Property

    Public Shared ReadOnly GlowStrengthProperty As DependencyProperty = GenProp(propGlow)
    Public Property GlowStrength As Double
        Get
            Return CDbl(GetValue(GlowStrengthProperty))
        End Get
        Set(value As Double)
            SetValue(GlowStrengthProperty, value)
        End Set
    End Property

    Private Shared Function GenProp(menuProp As MenuProperty) As DependencyProperty
        Select Case menuProp
            Case propTexel
                Return DependencyProperty.Register("TexelSize", GetType(Point), GetType(osEffect),
                                           New UIPropertyMetadata(New Point(0.01, 0.01),
                                                                  PixelShaderConstantCallback(0)))
            Case propThickness
                Return DependencyProperty.Register("Thickness", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(2.0, PixelShaderConstantCallback(1)))
            Case propSpread
                Return DependencyProperty.Register("Spread", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(3.0, PixelShaderConstantCallback(2)))
            Case propFade
                Return DependencyProperty.Register("Fade", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(0.0, PixelShaderConstantCallback(3)))
            Case propGlowColor
                Return DependencyProperty.Register("GlowColor", GetType(Color), GetType(osEffect),
                                                   New UIPropertyMetadata(Color.FromArgb(&HCC, &HFF, &HBF, &H0),
                                                                          PixelShaderConstantCallback(4)))
            Case propStroke
                Return DependencyProperty.Register("StrokeStrength", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(1.0, PixelShaderConstantCallback(5)))
            Case propGlow
                Return DependencyProperty.Register("GlowStrength", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(1.0, PixelShaderConstantCallback(6)))
        End Select
    End Function

End Class

Namespace osStyle

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

        Private Const SeparatorTag As String = "Content_Separator"

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
            Dim parts = rowsSpec.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries)
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
                             New PropertyMetadata(1.0, AddressOf OnPopupScaleChanged))

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
                Dim objVisualScale = New ScaleTransform(1.0, 1.0)
                objContainer.RenderTransform = objVisualScale
                Return objVisualScale
            End If

            Dim objVerifyVisual = TryCast(objVisualTransform, ScaleTransform)
            If objVerifyVisual IsNot Nothing Then Return objVerifyVisual

            Dim chkTransformGroup = TryCast(objVisualTransform, TransformGroup)
            If chkTransformGroup IsNot Nothing Then
                Dim chkVisualGroup = chkTransformGroup.Children.OfType(Of ScaleTransform)().FirstOrDefault()
                If chkVisualGroup IsNot Nothing Then Return chkVisualGroup

                chkVisualGroup = New ScaleTransform(1.0, 1.0)
                chkTransformGroup.Children.Insert(0, chkVisualGroup)
                Return chkVisualGroup
            End If

            Dim objVisualGroup As New TransformGroup()
            Dim objVisualScaler As New ScaleTransform(1.0, 1.0)

            objVisualGroup.Children.Add(objVisualScaler)
            objVisualGroup.Children.Add(objVisualTransform)

            objContainer.RenderTransform = objVisualGroup

            Return objVisualScaler
        End Function

    End Class

End Namespace

Namespace osEffectConv

    Public Class TexelSizeConverter
        Implements IMultiValueConverter

        Public Function Convert(values() As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IMultiValueConverter.Convert
            Dim w As Double = 0.0
            Dim h As Double = 0.0

            If values IsNot Nothing AndAlso values.Length >= 2 Then
                If values(0) IsNot Nothing Then Double.TryParse(values(0).ToString(), w)
                If values(1) IsNot Nothing Then Double.TryParse(values(1).ToString(), h)
            End If

            If w <= 0 OrElse h <= 0 Then
                Return New Point(0.01, 0.01)
            End If

            Return New Point(1.0 / w, 1.0 / h)
        End Function

        Public Function ConvertBack(value As Object, targetTypes() As Type, parameter As Object,
                                    culture As CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function

    End Class

End Namespace
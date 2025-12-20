Imports System.Windows.Markup
Imports System.Globalization
Imports System.Windows.Media.Effects
Imports osAutoCast.DataTypeLib.MenuProperty
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.DataTypeLib.VisualEasing
Imports System.Windows.Media.Animation
Imports System.ComponentModel

Public Class osEffectManager

End Class

Namespace osEffectLibs

    Public Class osEffect_Stroke
        Inherits ShaderEffect

        Private Shared ReadOnly _shader As New PixelShader() With {
        .UriSource = New Uri("/osAutoCast;component/DataFiles/VisualData/EffectsLib/EffectResources/osShader_TextStroke.ps", UriKind.Relative)
    }

        Public Sub New()
            With Me
                .PixelShader = _shader 'FetchShader(sTypeText_S).sText_S

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
            UpdateShaderValue(StrokeColorProperty)
            UpdateShaderValue(StrokeStrengthProperty)
        End Sub

        Public Shared ReadOnly InputProperty =
            RegisterPixelShaderSamplerProperty("Input", GetType(osEffect_Stroke), 0)

        Public Property Input As Brush
            Get
                Return GetValue(InputProperty)
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

        Public Shared ReadOnly StrokeColorProperty As DependencyProperty = GenProp(propStrokeColor)
        Public Property StrokeColor As Color
            Get
                Return CType(GetValue(StrokeColorProperty), Color)
            End Get
            Set(value As Color)
                SetValue(StrokeColorProperty, value)
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

        Private Shared Function GenProp(menuProp As MenuProperty) As DependencyProperty
            Select Case menuProp
                Case propTexel
                    Return DependencyProperty.Register("TexelSize", GetType(Point), GetType(osEffect_Stroke),
                                                       New UIPropertyMetadata(New Point(1, 1), PixelShaderConstantCallback(0)))
                Case propThickness
                    Return DependencyProperty.Register("Thickness", GetType(Double), GetType(osEffect_Stroke),
                                                       New UIPropertyMetadata(2.0, PixelShaderConstantCallback(1)))
                Case propSpread
                    Return DependencyProperty.Register("Spread", GetType(Double), GetType(osEffect_Stroke),
                                                       New UIPropertyMetadata(3.0, PixelShaderConstantCallback(2)))
                Case propFade
                    Return DependencyProperty.Register("Fade", GetType(Double), GetType(osEffect_Stroke),
                                                       New UIPropertyMetadata(0.0, PixelShaderConstantCallback(3)))
                Case propStrokeColor
                    Return DependencyProperty.Register("StrokeColor", GetType(Color), GetType(osEffect_Stroke),
                                                       New UIPropertyMetadata(Color.FromArgb(&HCC, &HFF, &HBF, &H0),
                                                                              PixelShaderConstantCallback(4)))
                Case propStroke
                    Return DependencyProperty.Register("StrokeStrength", GetType(Double), GetType(osEffect_Stroke),
                                                       New UIPropertyMetadata(1.0, PixelShaderConstantCallback(5)))
            End Select
        End Function
    End Class

    Public Class osEffect_Glow
        Inherits ShaderEffect
        Private Shared ReadOnly _shader As New PixelShader() With {
        .UriSource = New Uri("/osAutoCast;component/DataFiles/VisualData/EffectsLib/EffectResources/osShader_TextGlow.ps", UriKind.Relative)
    }

        Public Sub New()
            With Me
                .PixelShader = _shader 'FetchShader(sTypeText_G).sText_G

                .PaddingLeft = 6
                .PaddingRight = 6

                .PaddingTop = 3
                .PaddingBottom = 3
            End With

            UpdateShaderValue(InputProperty)
            UpdateShaderValue(TexelSizeProperty)
            UpdateShaderValue(ThicknessProperty)
            UpdateShaderValue(FadeProperty)
            UpdateShaderValue(GlowColorProperty)
            UpdateShaderValue(GlowStrengthProperty)
            UpdateShaderValue(VerticalGlowProperty)
        End Sub

        Public Shared ReadOnly InputProperty =
            RegisterPixelShaderSamplerProperty("Input", GetType(osEffect_Glow), 0)

        Public Property Input As Brush
            Get
                Return GetValue(InputProperty)
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

        Public Shared ReadOnly GlowStrengthProperty As DependencyProperty = GenProp(propGlow)
        Public Property GlowStrength As Double
            Get
                Return CDbl(GetValue(GlowStrengthProperty))
            End Get
            Set(value As Double)
                SetValue(GlowStrengthProperty, value)
            End Set
        End Property

        Public Shared ReadOnly VerticalGlowProperty As DependencyProperty = GenProp(propVerticalGlow)
        Public Property VerticalGlow As Double
            Get
                Return CDbl(GetValue(VerticalGlowProperty))
            End Get
            Set(value As Double)
                SetValue(VerticalGlowProperty, value)
            End Set
        End Property

        Private Shared Function GenProp(menuProp As MenuProperty) As DependencyProperty
            Select Case menuProp
                Case propTexel
                    Return DependencyProperty.Register("TexelSize", GetType(Point), GetType(osEffect_Glow),
                                                       New UIPropertyMetadata(New Point(1, 1), PixelShaderConstantCallback(0)))
                Case propThickness
                    Return DependencyProperty.Register("Thickness", GetType(Double), GetType(osEffect_Glow),
                                                       New UIPropertyMetadata(2.0, PixelShaderConstantCallback(1)))
                Case propFade
                    Return DependencyProperty.Register("Fade", GetType(Double), GetType(osEffect_Glow),
                                                       New UIPropertyMetadata(0.0, PixelShaderConstantCallback(2)))
                Case propGlowColor
                    Return DependencyProperty.Register("GlowColor", GetType(Color), GetType(osEffect_Glow),
                                                       New UIPropertyMetadata(Color.FromArgb(&HCC, &HFF, &HBF, &H0),
                                                                              PixelShaderConstantCallback(3)))
                Case propGlow
                    Return DependencyProperty.Register("GlowStrength", GetType(Double), GetType(osEffect_Glow),
                                                       New UIPropertyMetadata(1.0, PixelShaderConstantCallback(4)))
                Case propVerticalGlow
                    Return DependencyProperty.Register("VerticalGlow", GetType(Double), GetType(osEffect_Glow),
                                                       New UIPropertyMetadata(1.0, PixelShaderConstantCallback(5)))
            End Select
        End Function
    End Class

End Namespace

Namespace osVisConfigSettings

    Public Class osVisConfig
        Inherits MarkupExtension

        Public Property visDur As Integer

        Public Overrides Function ProvideValue(serviceProvider As IServiceProvider) As Object
            Return KeyTime.FromTimeSpan(SetDuration(visDur))
        End Function

        Private Function SetDuration(durMS As Integer) As TimeSpan
            Return TimeSpan.FromMilliseconds(durMS)
        End Function

    End Class

    Public Class osVisualEasing
        Inherits MarkupExtension

        Public Property visEasing As VisualEasing
        Public Property visEaseMode As EasingMode? = Nothing

        Public Sub New()
        End Sub

        Public Sub New(objVisEasing As VisualEasing)
            Me.visEasing = objVisEasing
        End Sub

        Public Overrides Function ProvideValue(serviceProvider As IServiceProvider) As Object
            Return GetVisualEase(visEasing)
        End Function

    End Class

End Namespace

Namespace osEffectConv

    Public Class TexelSizeConverter
        Implements IMultiValueConverter

        Public Function Convert(values() As Object, targetType As Type,
                                parameter As Object, culture As CultureInfo) As Object Implements IMultiValueConverter.Convert
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

Namespace osLoaderConv

    Public Class CornerRadiusMinusThicknessConverter
        Implements IValueConverter

        Public Function Convert(value As Object,
                                targetType As Type,
                                parameter As Object,
                                culture As Globalization.CultureInfo) As Object _
                                Implements IValueConverter.Convert

            ' ---- SAFETY: Always initialize variables first ----
            Dim cr As New CornerRadius(0)
            Dim thickness As Double = 0

            ' ---- Safely read CornerRadius ----
            If value IsNot Nothing AndAlso TypeOf value Is CornerRadius Then
                cr = CType(value, CornerRadius)
            End If

            ' ---- Safely read thickness (from parameter or binding) ----
            If parameter IsNot Nothing Then
                Double.TryParse(parameter.ToString(), thickness)
            End If

            ' ---- Subtract safely & clamp to 0 ----
            Return New CornerRadius(
                Math.Max(0, cr.TopLeft - thickness),
                Math.Max(0, cr.TopRight - thickness),
                Math.Max(0, cr.BottomRight - thickness),
                Math.Max(0, cr.BottomLeft - thickness)
            )
        End Function

        Public Function ConvertBack(value As Object,
                                    targetType As Type,
                                    parameter As Object,
                                    culture As Globalization.CultureInfo) As Object _
                                    Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace
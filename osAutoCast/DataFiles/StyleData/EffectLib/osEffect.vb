Imports System.Windows.Media
Imports System.Windows
Imports System.Windows.Media.Effects
Imports System.IO
Imports System.Globalization
Imports System.Windows.Data
Imports osAutoCast.DataTypeLib.MenuProperty

Public Class osEffect
    Inherits ShaderEffect

    Private Shared ReadOnly _shader As New PixelShader() With {
        .UriSource = New Uri("/osAutoCast;component/DataFiles/StyleData/EffectLib/osEffect2.ps", UriKind.Relative)
    }

    Public Sub New()
        PixelShader = _shader

        UpdateShaderValue(InputProperty)
        UpdateShaderValue(TexelSizeProperty)
        UpdateShaderValue(ThicknessProperty)
        UpdateShaderValue(SpreadProperty)
        UpdateShaderValue(FadeProperty)
        UpdateShaderValue(GlowColorProperty)
        UpdateShaderValue(StrokeStrengthProperty)
        UpdateShaderValue(GlowStrengthProperty)
    End Sub

    Public Shared ReadOnly InputProperty As DependencyProperty = ShaderEffect.RegisterPixelShaderSamplerProperty(
        "Input", GetType(osEffect), 0)

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
            Case propStroke
                Return DependencyProperty.Register("StrokeStrength", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(1.0, PixelShaderConstantCallback(5)))
            Case propSpread
                Return DependencyProperty.Register("Spread", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(3.0, PixelShaderConstantCallback(2)))
            Case propFade
                Return DependencyProperty.Register("Fade", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(0.0, PixelShaderConstantCallback(3)))
            Case propGlowColor
                Return DependencyProperty.Register("GlowColor", GetType(Color), GetType(osEffect),
                                                   New UIPropertyMetadata(Color.FromArgb(&HCC, &HFF, &HBF, &H0), PixelShaderConstantCallback(4)))
            Case propGlow
                Return DependencyProperty.Register("GlowStrength", GetType(Double), GetType(osEffect),
                                                   New UIPropertyMetadata(1.0, PixelShaderConstantCallback(6)))
            Case Else
                Return Nothing
        End Select
    End Function

End Class

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

        Public Function ConvertBack(value As Object, targetTypes() As Type, parameter As Object, culture As CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class

End Namespace
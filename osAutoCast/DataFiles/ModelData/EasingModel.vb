Imports System.Windows
Imports System.Windows.Media.Animation

''' <summary>
''' Custom EaseInOutExpo easing function (like your manual function).
''' </summary>
Public Class EaseInOutExpoEase
    Inherits EasingFunctionBase

    Protected Overrides Function EaseInCore(normalizedTime As Double) As Double
        ' normalizedTime is in range [0,1]
        If normalizedTime = 0 Then Return 0
        If normalizedTime = 1 Then Return 1

        If normalizedTime < 0.5 Then
            Return Math.Pow(2, 20 * normalizedTime - 10) / 2
        Else
            Return (2 - Math.Pow(2, -20 * normalizedTime + 10)) / 2
        End If
    End Function

    Protected Overrides Function CreateInstanceCore() As Freezable
        ' required for WPF animation system
        Return New EaseInOutExpoEase()
    End Function
End Class
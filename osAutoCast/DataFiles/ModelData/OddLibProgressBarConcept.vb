Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Threading

''' <summary>
''' Ultra-light custom progress bar that repaints only the pixels that changed.
''' </summary>
Public Class OddLibProgressBarConcept
    Inherits FrameworkElement

#Region "Configurable brushes"

    ' Public Property BarBrush As Brush = New SolidColorBrush(Color.FromRgb(0, 200, 0)).FreezeReturn()
    Public Property BackBrush As Brush = New SolidColorBrush(Color.FromRgb(57, 57, 57)).FreezeReturn()

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
        End Set
    End Property

    Private Shared Sub EstablishProgFreeze(ByRef objPF As Freezable)
        If objPF IsNot Nothing AndAlso objPF.CanFreeze AndAlso Not objPF.IsFrozen Then
            objPF.Freeze()
        End If
    End Sub

    Public Sub SetProgColor(pColor As Color)
        ActiveBrush = New SolidColorBrush(pColor)
    End Sub

#End Region

#Region "Dependency property – Value (0-100)"

    Public Shared ReadOnly ValueProperty As DependencyProperty =
        DependencyProperty.Register(
            "Value", GetType(Double), GetType(OddLibProgressBarConcept),
            New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender,
                                          AddressOf OnValueChanged, AddressOf CoerceValue))

    Public Property Value As Double
        Get
            Return CDbl(GetValue(ValueProperty))
        End Get
        Set(ByVal v As Double)
            SetValue(ValueProperty, v)
        End Set
    End Property

    Private Overloads Shared Function CoerceValue(d As DependencyObject, baseValue As Object) As Object
        Dim v = CDbl(baseValue)
        Return Math.Max(0, Math.Min(100, v))
    End Function

    Private Shared Sub OnValueChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim bar = DirectCast(d, OddLibProgressBarConcept)
        bar._target = CDbl(e.NewValue)
    End Sub

#End Region

#Region "Internals"

    Private _display As Double            'What we are currently showing
    Private _target As Double             'Where we need to be after easing
    Private ReadOnly _timer As DispatcherTimer

    Private _animSw As Stopwatch = Nothing
    Private _animDuration As TimeSpan = TimeSpan.Zero
    Private _autoReset As Boolean

    Public Sub New()
        SnapsToDevicePixels = True
        UseLayoutRounding = True
        _timer = New DispatcherTimer(DispatcherPriority.Render) With {.Interval = TimeSpan.FromMilliseconds(33)} '≈30 Hz
        AddHandler _timer.Tick, AddressOf OnTick
        _timer.Start()
    End Sub

    Private Sub OnTick(sender As Object, e As EventArgs)
        Const ease As Double = 0.18        'Critically damped, feels smooth without overshoot
        _display += (_target - _display) * ease

        If _animSw IsNot Nothing Then
            Dim frac = _animSw.Elapsed.TotalMilliseconds / _animDuration.TotalMilliseconds
            Value = Math.Min(100, frac * 100)
            If frac >= 1.0 Then
                _animSw = Nothing
                If _autoReset Then Value = 0
            End If
        End If

        If Math.Abs(_target - _display) < 0.1 Then _display = _target
        InvalidateVisual()               'Re-render only the dirty stripe
    End Sub

    Public Sub BeginSweep(duration As TimeSpan,
                      Optional autoResetToZero As Boolean = False)
        _animDuration = duration
        _animSw = Stopwatch.StartNew()
        _autoReset = autoResetToZero
        Value = 0
    End Sub

#End Region

#Region "Rendering"

    Protected Overrides Sub OnRender(dc As DrawingContext)
        MyBase.OnRender(dc)

        Dim w = ActualWidth
        Dim h = ActualHeight
        If w <= 0 OrElse h <= 0 Then Return

        'Background — single fill
        dc.DrawRectangle(BackBrush, Nothing, New Rect(0, 0, w, h))

        'Foreground — only the filled portion
        Dim filledWidth = w * (_display / 100.0)
        If filledWidth > 0 Then
            dc.DrawRectangle(ActiveBrush, Nothing, New Rect(0, 0, filledWidth, h))
        End If
    End Sub

#End Region

End Class

''' <summary>Convenience extension that freezes the brush and returns it, so you can chain calls.</summary>
Module BrushHelpers
    <Runtime.CompilerServices.Extension>
    Public Function FreezeReturn(Of T As Freezable)(item As T) As T
        If item.CanFreeze Then item.Freeze()
        Return item
    End Function
End Module

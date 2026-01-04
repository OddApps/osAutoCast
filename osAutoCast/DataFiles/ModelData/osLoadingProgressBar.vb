Imports System
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports osColor = System.Windows.Media
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.LoadingProgStatus
Imports System.Diagnostics

Namespace osLoadingElements

    Public Class osLoadingProgressBar
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

        Private ReadOnly _fastDuration As TimeSpan = TimeSpan.FromMilliseconds(410)
        Private ReadOnly _slowDuration As TimeSpan = TimeSpan.FromSeconds(4)

        Private ProgDuration_Set As Duration = New Duration(_fastDuration)
        Private ProgDuration_Next As Duration = New Duration(_slowDuration)

        Public Event LoadProgComplete As EventHandler
        Private idxLoadAniTasks As New List(Of TaskCompletionSource(Of Boolean))

        Public Property onLastTask As Boolean = False

        Private ReadOnly idxProgLoadNextValues As New Dictionary(Of LoadingProgStatus, osLoadProgData) From
            {
                {LoadStatus_StartUp, New osLoadProgData(5, 25)},
                {LoadStatus_Init, New osLoadProgData(30, 65)},
                {LoadStatus_PrefPrep, New osLoadProgData(70, 105)},
                {LoadStatus_LoadingUI, New osLoadProgData(110, 145)},
                {LoadStatus_ApplyConfig, New osLoadProgData(150, 185)},
                {LoadStatus_StartingSvc, New osLoadProgData(190, 225)},
                {LoadStatus_Starting, New osLoadProgData(230, 250, True)}
        }

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

        Public Shared ReadOnly MinimumProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(Minimum), GetType(Double), GetType(osLoadingProgressBar),
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
            Register(NameOf(Maximum), GetType(Double), GetType(osLoadingProgressBar),
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
            Register(NameOf(Progress), GetType(Double), GetType(osLoadingProgressBar),
                     New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender,
                                                   AddressOf OnValueChanged, AddressOf CoerceProgress))

        Private Shared Function CoerceProgress(d As DependencyObject, baseValue As Object) As Object
            Dim ctrl = DirectCast(d, osLoadingProgressBar)
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
            Register(NameOf(AnimatedProgress), GetType(Double), GetType(osLoadingProgressBar),
                     New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender,
                                                   AddressOf OnAnimatedProgressChanged))

        Private Shared Sub OnAnimatedProgressChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = DirectCast(d, osLoadingProgressBar)
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

        Public Shared ReadOnly FillBrushProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(FillBrush), GetType(Brush), GetType(osLoadingProgressBar),
                     New FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property FillBrush As Brush
            Get
                Return DirectCast(GetValue(FillBrushProperty), Brush)
            End Get
            Set(value As Brush)
                SetValue(FillBrushProperty, value)
            End Set
        End Property

        Public Shared ReadOnly TrackBrushProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(TrackBrush), GetType(Brush), GetType(osLoadingProgressBar),
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

        ' Changed to CornerRadius type so it accepts corner per-corner values like "0,0,0,0"
        Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(CornerRadius), GetType(CornerRadius), GetType(osLoadingProgressBar),
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
            Register(NameOf(TrackCornerRadius), GetType(CornerRadius), GetType(osLoadingProgressBar),
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
            Register(NameOf(FillCornerRadius), GetType(CornerRadius), GetType(osLoadingProgressBar),
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
            Register(NameOf(AnimationDuration), GetType(TimeSpan), GetType(osLoadingProgressBar),
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
            Register(NameOf(EasingFunction), GetType(IEasingFunction), GetType(osLoadingProgressBar),
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
            Register("BorderBrush", GetType(osColor.Brush), GetType(osLoadingProgressBar),
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
            Register("BorderThickness", GetType(Thickness), GetType(osLoadingProgressBar),
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
            Register(NameOf(FillPixelTolerance), GetType(Double), GetType(osLoadingProgressBar),
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
                    RaiseEvent LoadProgComplete(Me, EventArgs.Empty)
                Catch ex As Exception : End Try

            ElseIf Not isNowFull AndAlso _filledSignaled Then
                _filledSignaled = False
            End If
        End Sub

        Public Function MonitorLoadProgress(Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            If _filledSignaled Then
                Return Task.FromResult(True) : End If

            Dim tcs = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.RunContinuationsAsynchronously)

            SyncLock idxLoadAniTasks
                idxLoadAniTasks.Add(tcs)
            End SyncLock

            If Not ct = Nothing AndAlso ct.CanBeCanceled Then
                Dim reg = ct.Register(
                    Sub()
                        SyncLock idxLoadAniTasks
                            If idxLoadAniTasks.Remove(tcs) Then
                                tcs.TrySetCanceled()
                            End If
                        End SyncLock
                    End Sub)

                tcs.Task.ContinueWith(
                    Sub()
                        reg.Dispose()
                    End Sub, TaskScheduler.Default)
            End If

            Return tcs.Task
        End Function

#End Region

#Region "Layout & Rendering"

        Protected Overrides Function MeasureOverride(availableSize As Size) As Size
            Dim w = If(Double.IsInfinity(availableSize.Width), 200, availableSize.Width)
            Dim h = If(Double.IsInfinity(availableSize.Height), 20, availableSize.Height)
            Return New Size(w, h)
        End Function

        Protected Overrides Function ArrangeOverride(finalSize As Size) As Size
            Return finalSize
        End Function

        Private Function GetProgRatio() As Double
            Dim valProgRatio = (Progress - Minimum) / (Maximum - Minimum)
            Return Math.Max(0.0, Math.Min(1.0, valProgRatio))
        End Function

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

            Dim progressRatio = GetProgRatio()

            Dim fillWidth = ActualWidth * progressRatio
            Dim fillP = progressRatio * 100

            If fillWidth > 0.0001 Then
                Dim progFill_Rect As New Rect(0, 0, fillWidth, h)
                Dim progFill_CornerRadius As CornerRadius

                If fillP >= 100.0 - 2.5 Then
                    progFill_CornerRadius = GetClampedCornerRadius(Me.FillCornerRadius, fillWidth, h)
                Else
                    progFill_CornerRadius = New CornerRadius(Me.FillCornerRadius.TopLeft, Me.FillCornerRadius.TopRight,
                                                             0.0, Me.FillCornerRadius.BottomLeft)

                    progFill_CornerRadius = GetClampedCornerRadius(progFill_CornerRadius, fillWidth, h)
                End If

                Dim progFill_Geometry = CreateRoundRectGeometry(progFill_Rect, progFill_CornerRadius)
                dc.DrawGeometry(If(FillBrush, Brushes.DodgerBlue), Nothing, progFill_Geometry)

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

            If onLastTask Then
                CheckAndRaiseFilled(fillWidth) : End If

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

End Namespace

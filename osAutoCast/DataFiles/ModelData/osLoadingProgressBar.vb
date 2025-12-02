Imports System
Imports System.Globalization
Imports System.Threading
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports osColor = System.Windows.Media
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.LoadingProgStatus

Namespace osLoadingElements

    Public Class osLoadingProgressBar
        Inherits FrameworkElement

        Public Sub New()
            With Me
                Me.FillBrush = New SolidColorBrush(Colors.Red)
                Me.TrackBrush = New SolidColorBrush(Color.FromRgb(&HE, &HE, &HE))

                Me.CornerRadius = 0.0
                Me.AnimationDuration = TimeSpan.FromMilliseconds(400)

                Me.EasingFunction = New CubicEase() With {
                .EasingMode = EasingMode.EaseOut
            }

                Me.Minimum = 0
                Me.Maximum = 100
            End With
        End Sub

#Region "Fields for fast/slow animation & cancellation"

        Private _animVersion As Integer = 0

        Private _cts As CancellationTokenSource = Nothing
        Private ReadOnly _lockObj As New Object()
        Private _currentSlowTarget As Double? = Nothing

        Private ReadOnly _fastDuration As TimeSpan = TimeSpan.FromMilliseconds(350)
        Private ReadOnly _slowDuration As TimeSpan = TimeSpan.FromSeconds(2)

        Private ProgDuration_Set As Duration = New Duration(_fastDuration)
        Private ProgDuration_Next As Duration = New Duration(_slowDuration)

        Public Event LoadProgComplete As EventHandler
        Private idxLoadAniTasks As New List(Of TaskCompletionSource(Of Boolean))

        Private onLastTask As Boolean = False

        Private ReadOnly idxProgLoadNextValues As New Dictionary(Of LoadingProgStatus, osLoadProgData) From
            {
                {LoadStatus_StartUp, New osLoadProgData(5, 10)},
                {LoadStatus_Init, New osLoadProgData(45, 50)},
                {LoadStatus_PrefPrep, New osLoadProgData(85, 95)},
                {LoadStatus_LoadingUI, New osLoadProgData(125, 135)},
                {LoadStatus_ApplyConfig, New osLoadProgData(175, 185)},
                {LoadStatus_StartingSvc, New osLoadProgData(210, 215)},
                {LoadStatus_Starting, New osLoadProgData(240, 250, True)}
        }

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
                                                   AddressOf OnProgressChanged, AddressOf CoerceProgress))

        Private Shared Function CoerceProgress(d As DependencyObject, baseValue As Object) As Object
            Dim ctrl = DirectCast(d, osLoadingProgressBar)
            Dim v = CDbl(baseValue)
            If Double.IsNaN(v) Then Return ctrl.Minimum
            If v < ctrl.Minimum Then Return ctrl.Minimum
            If v > ctrl.Maximum Then Return ctrl.Maximum
            Return v
        End Function

        Private Shared Sub OnProgressChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = DirectCast(d, osLoadingProgressBar)
            Dim min = ctrl.Minimum
            Dim max = ctrl.Maximum
            Dim newVal As Double = CDbl(e.NewValue)
            Dim percent = If(max - min = 0, 0, (newVal - min) / (max - min) * 100.0)
            ctrl.StartProgress(percent)
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

        Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.
            Register(NameOf(CornerRadius), GetType(Double), GetType(osLoadingProgressBar),
                     New FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property CornerRadius As Double
            Get
                Return CDbl(GetValue(CornerRadiusProperty))
            End Get
            Set(value As Double)
                SetValue(CornerRadiusProperty, value)
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
            Register("BorderThickness", GetType(Double), GetType(osLoadingProgressBar),
                     New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Property BorderThickness As Double
            Get
                Return CDbl(GetValue(BorderThicknessProperty))
            End Get
            Set(value As Double)
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

#Region "Fast/Slow algorithm (adapted to percent backing DP)"

        Private pVal_Set As Double
        Private pVal_Next As Double

        Private Function FetchLoadProgData(objLoadingProgStatus As LoadingProgStatus) As osLoadProgData
            Return idxProgLoadNextValues.First(
            Function(objLoadStatus) objLoadStatus.
                Key = objLoadingProgStatus).Value
        End Function

        Public Sub UpdateLoadProgress(objLoadingProgStatus As LoadingProgStatus)
            With FetchLoadProgData(objLoadingProgStatus)
                pVal_Set = (.SetProgVal / 250) * 100
                pVal_Next = (.NextProgVal / 250) * 100

                onLastTask = .LastTaskVal
            End With

            StartProgress(pVal_Set, pVal_Next)
        End Sub

        Private Sub StartProgress(newPct As Double, Optional nextPct As Double? = Nothing)
            Dim nextVal As Double = If(nextPct.HasValue,
                nextPct.Value, newPct)

            Dim myVersion As Integer = Interlocked.Increment(_animVersion)

            SyncLock _lockObj
                If _cts IsNot Nothing AndAlso _currentSlowTarget.HasValue Then
                    Dim prevTarget = _currentSlowTarget.Value

                    _cts.Cancel()
                    _cts.Dispose()
                    _cts = Nothing
                    _currentSlowTarget = Nothing

                    Dim currAnimated As Double = CDbl(GetValue(AnimatedProgressProperty))
                    SetValue(AnimatedProgressProperty, currAnimated)

                    If Math.Abs(currAnimated - prevTarget) < 0.0001 Then
                        RunFastThenSlow(newPct, nextVal, myVersion)
                    Else
                        Dim interimAnim As New DoubleAnimation(currAnimated, prevTarget, ProgDuration_Set) With {
                            .EasingFunction = New QuadraticEase With {.EasingMode = EasingMode.EaseInOut},
                            .FillBehavior = FillBehavior.HoldEnd
                        }

                        Dim interimHandler As EventHandler = Nothing
                        interimHandler =
                            Sub(s, e)
                                If myVersion <> _animVersion Then
                                    RemoveHandler interimAnim.Completed, interimHandler
                                    Return
                                End If

                                Me.Dispatcher.Invoke(Sub()
                                                         Me.BeginAnimation(AnimatedProgressProperty, Nothing)

                                                         SetValue(AnimatedProgressProperty, prevTarget)
                                                     End Sub)
                                RemoveHandler interimAnim.Completed, interimHandler

                                RunFastThenSlow(newPct, nextVal, myVersion)
                            End Sub

                        AddHandler interimAnim.Completed, interimHandler

                        Me.Dispatcher.Invoke(
                            Sub()
                                Me.BeginAnimation(AnimatedProgressProperty, interimAnim)
                            End Sub, DispatcherPriority.Normal)
                    End If

                    Return
                End If
            End SyncLock

            RunFastThenSlow(newPct, nextVal, myVersion)
        End Sub

        Private Sub RunFastThenSlow(newPct As Double, nextVal As Double, myVersion As Integer)

            Me.Dispatcher.Invoke(
                Sub()
                    Me.BeginAnimation(AnimatedProgressProperty, Nothing)
                End Sub)

            Dim fastAnim As New DoubleAnimation(newPct, ProgDuration_Set) With {
                .EasingFunction = New QuadraticEase With {.EasingMode = EasingMode.EaseOut},
                .FillBehavior = FillBehavior.HoldEnd
            }

            Dim fastHandler As EventHandler = Nothing
            fastHandler =
                Sub(s, e2)
                    If myVersion <> _animVersion Then
                        RemoveHandler fastAnim.Completed, fastHandler
                        Return
                    End If

                    Me.Dispatcher.Invoke(
                        Sub()
                            Me.BeginAnimation(AnimatedProgressProperty, Nothing)
                            SetValue(AnimatedProgressProperty, newPct)
                        End Sub)

                    RemoveHandler fastAnim.Completed, fastHandler

                    StartSlowEase(newPct, nextVal, myVersion)
                End Sub

            AddHandler fastAnim.Completed, fastHandler

            Me.Dispatcher.Invoke(
                Sub()
                    Me.BeginAnimation(AnimatedProgressProperty, fastAnim)
                End Sub, DispatcherPriority.Normal)
        End Sub

        Private Sub StartSlowEase(fromVal As Double, toVal As Double, myVersion As Integer)
            SyncLock _lockObj
                If _cts IsNot Nothing Then
                    _cts.Cancel()
                    _cts.Dispose()
                End If
                _cts = New CancellationTokenSource()
                _currentSlowTarget = toVal
            End SyncLock

            Dim token = _cts.Token

            Dim slowAnim As New DoubleAnimation(toVal, CalcNextDuration(fromVal, toVal)) With {
                .FillBehavior = FillBehavior.HoldEnd,
                .EasingFunction = New ExponentialEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Dim reg As CancellationTokenRegistration

            Dim cbCancel As Action =
                Sub()
                    If myVersion <> _animVersion Then
                        reg.Dispose()
                        Return
                    End If

                    Me.Dispatcher.Invoke(
                        Sub()
                            Dim curr As Double = CDbl(GetValue(AnimatedProgressProperty))
                            If curr > _currentSlowTarget.Value Then
                                SetValue(AnimatedProgressProperty, curr)

                            End If
                            Me.BeginAnimation(AnimatedProgressProperty, Nothing)
                        End Sub)
                End Sub

            Dim completedHandler As EventHandler = Nothing
            completedHandler =
                Sub(s, e)
                    If myVersion <> _animVersion Then
                        RemoveHandler slowAnim.Completed, completedHandler
                        reg.Dispose()
                        Return
                    End If

                    SyncLock _lockObj
                        If _cts IsNot Nothing Then
                            Me.Dispatcher.Invoke(
                                Sub()
                                    Me.BeginAnimation(AnimatedProgressProperty, Nothing)
                                    SetValue(AnimatedProgressProperty, toVal)
                                End Sub)

                            _cts.Dispose()
                            _cts = Nothing
                            _currentSlowTarget = Nothing
                        End If
                    End SyncLock

                    RemoveHandler slowAnim.Completed, completedHandler
                    reg.Dispose()
                End Sub
            reg = token.Register(cbCancel)
            AddHandler slowAnim.Completed, completedHandler

            Me.Dispatcher.Invoke(
                Sub()
                    If myVersion = _animVersion Then
                        Me.BeginAnimation(AnimatedProgressProperty, slowAnim)
                    End If
                End Sub, DispatcherPriority.Normal)
        End Sub

        Public Sub SnapToCurrentSlowTarget()
            SyncLock _lockObj
                If _currentSlowTarget.HasValue Then
                    Dim target = _currentSlowTarget.Value

                    If _cts IsNot Nothing Then
                        _cts.Cancel()
                        _cts.Dispose()
                        _cts = Nothing
                    End If

                    Dim snapVersion = _animVersion

                    Me.Dispatcher.Invoke(
                        Sub()
                            If snapVersion = _animVersion Then
                                Me.BeginAnimation(AnimatedProgressProperty, Nothing)
                                SetValue(AnimatedProgressProperty, target)
                            End If
                        End Sub)

                    _currentSlowTarget = Nothing
                End If
            End SyncLock
        End Sub

        Private Function CalcNextDuration(fromVal As Double, toVal As Double) As Duration
            Dim dist = Math.Abs(toVal - fromVal) / 100
            Dim minSec As Double = 0.9
            Dim maxSec As Double = 3.0
            Dim secs = minSec + (maxSec - minSec) * dist
            Return New Duration(TimeSpan.FromSeconds(secs))
        End Function

#End Region

#Region "Convenience API (direct percent-based setters)"

        Private Sub CheckAndRaiseFilled()
            Dim w = Me.RenderSize.Width
            If w <= 0 Then Return

            Dim fillWidth = (AnimatedProgress / 100.0) * w
            Dim tolerance = Math.Max(0.0, FillPixelTolerance)

            Dim isNowFull As Boolean = (fillWidth >= (w - tolerance))

            If isNowFull AndAlso Not _filledSignaled Then
                _filledSignaled = True
                Try
                    RaiseEvent LoadProgComplete(Me, EventArgs.Empty)
                Catch ex As Exception
                End Try

                SyncLock idxLoadAniTasks
                    For Each tcs In idxLoadAniTasks
                        Try
                            tcs.TrySetResult(True)
                        Catch
                        End Try
                    Next
                    idxLoadAniTasks.Clear()
                End SyncLock
            ElseIf Not isNowFull AndAlso _filledSignaled Then
                _filledSignaled = False
            End If
        End Sub

        Public Function MonitorLoadProgress(Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            If _filledSignaled Then
                Return Task.FromResult(True) : End If

            Dim tcs = New TaskCompletionSource(Of Boolean)(
                TaskCreationOptions.RunContinuationsAsynchronously)
            SyncLock idxLoadAniTasks
                idxLoadAniTasks.Add(tcs)
            End SyncLock

            If Not ct = Nothing AndAlso ct.CanBeCanceled Then
                Dim reg = ct.Register(Sub()
                                          SyncLock idxLoadAniTasks
                                              If idxLoadAniTasks.Remove(tcs) Then
                                                  tcs.TrySetCanceled()
                                              End If
                                          End SyncLock
                                      End Sub)
                tcs.Task.ContinueWith(Sub()
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

        Protected Overrides Sub OnRender(dc As DrawingContext)
            MyBase.OnRender(dc)

            Dim w = Me.RenderSize.Width
            Dim h = Me.RenderSize.Height
            If w <= 0 OrElse h <= 0 Then Return

            Dim radius = Math.Min(CornerRadius, Math.Min(w, h) / 2)
            Dim trackRect As New Rect(0, 0, w, h)

            If TrackBrush IsNot Nothing Then
                dc.DrawRoundedRectangle(TrackBrush, Nothing, trackRect, radius, radius)
            Else
                dc.DrawRoundedRectangle(Brushes.LightGray, Nothing, trackRect, radius, radius)
            End If

            Dim fillWidth = (AnimatedProgress / 100.0) * w
            If fillWidth > 0.0001 Then
                Dim fillRect As New Rect(0, 0, fillWidth, h)

                Dim fillRadius = radius
                If fillWidth < radius Then
                    fillRadius = Math.Max(0.0, fillWidth / 2.0)
                End If

                If FillBrush IsNot Nothing Then
                    dc.DrawRoundedRectangle(FillBrush, Nothing, fillRect, fillRadius, fillRadius)
                Else
                    dc.DrawRoundedRectangle(Brushes.DodgerBlue, Nothing, fillRect, fillRadius, fillRadius)
                End If
            End If

            If BorderThickness > 0 AndAlso BorderBrush IsNot Nothing Then
                Dim objPen = ApplyPen(BorderBrush, BorderThickness)
                Dim objFreeze = TryCast(objPen, Freezable)

                EstablishProgFreeze(objFreeze)

                dc.DrawRectangle(Nothing, objPen, New Rect(0.5, 0.5, Math.Max(0, ActualWidth - 1), Math.Max(0, ActualHeight - 1)))
            End If

            If onLastTask Then
                CheckAndRaiseFilled() : End If

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

    End Class

End Namespace

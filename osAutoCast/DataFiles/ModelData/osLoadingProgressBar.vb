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

        Private onLastTask As Boolean = False

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

        Private Shared Sub OnProgressChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = DirectCast(d, osLoadingProgressBar)
            Dim min = ctrl.Minimum
            Dim max = ctrl.Maximum
            Dim newVal As Double = CDbl(e.NewValue)
            Dim percent = If(max - min = 0, 0, (newVal - min) / (max - min) * 100.0)
            ctrl.StartProgress(percent)
        End Sub

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

#Region "Fast/Slow algorithm (adapted to percent backing DP)"

        Private Function FetchLoadProgData(objLoadingProgStatus As LoadingProgStatus) As osLoadProgData
            Return idxProgLoadNextValues.First(
            Function(objLoadStatus) objLoadStatus.
                Key = objLoadingProgStatus).Value
        End Function

        Public Sub UpdateLoadProgress(objLoadingProgStatus As LoadingProgStatus)
            With FetchLoadProgData(objLoadingProgStatus)
                Dim vSet = (.SetProgVal / 250) * 100
                Dim vNext = (.NextProgVal / 250) * 100

                onLastTask = .LastTaskVal
                ' If clocker IsNot Nothing Then clocker.Controller.Pause()


                ' Dim objTask_DispLoadMsg = objInMon.DisplayLoadMsg(isTxt)
                ' If clocker IsNot Nothing Then clocker.Controller.Resume()
                StartProgress(vSet, vNext)
            End With
        End Sub

        Private Sub StartFrameTween(fromVal As Double, toVal As Double, duration As TimeSpan, easing As IEasingFunction, myVersion As Integer, Optional completion As Action = Nothing)
            _renderVersion = myVersion
            _renderFrom = fromVal
            _renderTo = toVal
            _renderDurationSeconds = duration.TotalMilliseconds
            _renderEasing = easing
            _renderCompletion = completion

            _renderSw.Restart()

            If Not _renderingActive Then
                AddHandler CompositionTarget.Rendering, AddressOf OnCompositionTargetRendering
                _renderingActive = True
            End If
        End Sub

        Private Sub OnCompositionTargetRendering(sender As Object, e As EventArgs)
            Me.Dispatcher.Invoke(
                Sub()
                    Try
                        If _renderVersion <> _animVersion Then
                            StopRenderingLoop()
                        End If

                        Dim elapsed = _renderSw.Elapsed.TotalMilliseconds
                        Dim valTotal = Math.Min(1.0, elapsed / _renderDurationSeconds)

                        Dim easedT As Double

                        If _renderEasing IsNot Nothing Then
                            Try
                                easedT = _renderEasing.Ease(valTotal)
                            Catch
                                easedT = valTotal
                            End Try
                        Else
                            easedT = valTotal
                        End If

                        Dim curVal = _renderFrom + (_renderTo - _renderFrom) * easedT

                        SetValue(AnimatedProgressProperty, curVal)
                        InvalidateVisual()

                        If valTotal >= 1.0 Then
                            BeginAnimation(AnimatedProgressProperty, Nothing)
                            SetValue(AnimatedProgressProperty, _renderTo)
                            InvalidateVisual()

                            Dim comp = _renderCompletion
                            StopRenderingLoop()

                            If _renderVersion = _animVersion AndAlso comp IsNot Nothing Then
                                Try
                                    comp.Invoke()
                                Catch
                                End Try
                            End If
                        End If

                    Catch ex As Exception
                        StopRenderingLoop()
                    End Try
                End Sub)

        End Sub

        Private Sub StopRenderingLoop()
            If _renderingActive Then
                Try
                    RemoveHandler CompositionTarget.Rendering, AddressOf OnCompositionTargetRendering
                Catch : End Try

                _renderingActive = False
                _renderSw.Stop()
                _renderCompletion = Nothing
            End If
        End Sub

        Public clocker As AnimationClock

        Private _isPaused As Boolean = False
        Private _pausedTo As Double? = Nothing
        Private _pausedRemainingMs As Double = 0
        Private _pausedEasing As IEasingFunction = Nothing
        Private _pausedWasRendering As Boolean = False

        ' Call this to pause the visual progress immediately.
        Public Sub PauseProgress()
            ' If already paused, nothing to do
            If _isPaused Then Return

            ' Grab current visible value and current animation timing
            Dim currVal As Double = 0
            Me.Dispatcher.Invoke(
        Sub()
            currVal = CDbl(GetValue(AnimatedProgressProperty))
        End Sub)

            ' Capture any currently-targeted slow-ease target so resume heads toward it.
            Dim curTarget As Double? = Nothing
            Dim remainingMs As Double = 0
            Dim easing As IEasingFunction = Nothing
            Dim wasRendering As Boolean = False

            SyncLock _lockObj
                wasRendering = _renderingActive

                ' store pending slow target if there is one
                If _currentSlowTarget.HasValue Then
                    curTarget = _currentSlowTarget.Value
                Else
                    ' if there was an active tween, use _renderTo (StartFrameTween stored it)
                    If _renderingActive Then
                        curTarget = _renderTo
                    End If
                End If

                ' compute remaining time based on render stopwatch/duration
                If _renderingActive AndAlso _renderDurationSeconds > 0 Then
                    Dim elapsed = _renderSw.Elapsed.TotalMilliseconds
                    remainingMs = Math.Max(0, _renderDurationSeconds - elapsed)
                    easing = _renderEasing
                Else
                    remainingMs = 0
                    easing = _renderEasing
                End If

                ' Cancel any slow token which will also stop its background registration (this will
                ' cause the reg callback to freeze the value where it is).
                If _cts IsNot Nothing Then
                    Try
                        _cts.Cancel()
                        _cts.Dispose()
                    Catch : End Try
                    _cts = Nothing
                    _currentSlowTarget = Nothing
                End If
            End SyncLock

            ' Stop the frame loop to freeze visual updates.
            StopRenderingLoop()

            ' Ensure the AnimatedProgressProperty is exactly the current value and not animating
            Me.Dispatcher.Invoke(
        Sub()
            Me.BeginAnimation(AnimatedProgressProperty, Nothing)
            SetValue(AnimatedProgressProperty, currVal)
            InvalidateVisual()
        End Sub)

            ' Save paused state for resume
            _pausedTo = curTarget
            _pausedRemainingMs = remainingMs
            _pausedEasing = easing
            _pausedWasRendering = wasRendering
            _isPaused = True
        End Sub

        ' Call this to resume after PauseProgress()
        Public Sub ResumeProgress()
            If Not _isPaused Then Return

            ' Clear paused flag early so nested calls of StartProgress behave normally.
            _isPaused = False

            Dim target As Double? = _pausedTo
            Dim remainingMs As Double = _pausedRemainingMs
            Dim easing As IEasingFunction = _pausedEasing

            ' Reset paused fields
            _pausedTo = Nothing
            _pausedRemainingMs = 0
            _pausedEasing = Nothing
            _pausedWasRendering = False

            ' If we had no meaningful target to continue to, do nothing
            If Not target.HasValue Then Return

            ' Start a continuation tween from current visible value to the paused target
            Dim currVisible As Double = 0
            Me.Dispatcher.Invoke(
        Sub()
            currVisible = CDbl(GetValue(AnimatedProgressProperty))
        End Sub)

            ' If there's effectively no remaining time, snap to target
            If remainingMs <= 1 Then
                Me.Dispatcher.Invoke(
            Sub()
                Me.BeginAnimation(AnimatedProgressProperty, Nothing)
                SetValue(AnimatedProgressProperty, target.Value)
                InvalidateVisual()
            End Sub)
                Return
            End If

            ' Use a new animation version so old callbacks won't interfere
            Dim myVersion = Interlocked.Increment(_animVersion)

            ' Use the same easing if available, otherwise fallback to your normal easing
            Dim resumeEasing As IEasingFunction = If(easing, New ExponentialEase() With {.EasingMode = EasingMode.EaseInOut})

            ' Start a frame tween that will finish the remaining portion
            StartFrameTween(currVisible, target.Value, TimeSpan.FromMilliseconds(remainingMs),
                    resumeEasing, myVersion, Sub()
                                                 ' on completion, clear any slow target bookkeeping (like StartSlowEase does)
                                                 SyncLock _lockObj
                                                     If _cts IsNot Nothing Then
                                                         Try
                                                             _cts.Dispose()
                                                         Catch : End Try
                                                         _cts = Nothing
                                                     End If
                                                     _currentSlowTarget = Nothing
                                                 End SyncLock
                                             End Sub)
        End Sub

        Private Sub StartProgress(newPct As Double, Optional nextPct As Double? = Nothing)
            Dim nextVal = If(nextPct.HasValue,
                nextPct.Value, newPct)

            Dim myVersion = Interlocked.Increment(_animVersion)

            SyncLock _lockObj
                If _cts IsNot Nothing AndAlso _currentSlowTarget.HasValue Then
                    Dim prevTarget = _currentSlowTarget.Value

                    _cts.Cancel()
                    _cts.Dispose()
                    _cts = Nothing

                    _currentSlowTarget = Nothing

                    Dim currAnimated As Double = 0

                    Me.Dispatcher.Invoke(
                        Sub()
                            currAnimated = CDbl(GetValue(AnimatedProgressProperty))
                        End Sub)

                    Dim interimAnim As New DoubleAnimation(currAnimated, prevTarget, ProgDuration_Set) With {
                        .EasingFunction = New QuadraticEase With {
                            .EasingMode = EasingMode.EaseInOut
                        }, .FillBehavior = FillBehavior.Stop
                    }

                    Dim interimHandler As EventHandler = Nothing

                    interimHandler =
                        Sub(s, e)
                            If myVersion <> _animVersion Then
                                RemoveHandler interimAnim.Completed, interimHandler
                                Return
                            End If

                            Me.Dispatcher.Invoke(
                                Sub()
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
                        End Sub)

                    Return
                End If
            End SyncLock

            RunFastThenSlow(newPct, nextVal, myVersion)
        End Sub

        Private Sub RunFastThenSlow(newPct As Double, nextVal As Double, myVersion As Integer)
            Dim currVisible As Double = 0

            Me.Dispatcher.Invoke(
                Sub()
                    currVisible = CDbl(GetValue(AnimatedProgressProperty))
                End Sub)

            Me.BeginAnimation(AnimatedProgressProperty, Nothing)

            Dim fastDuration = ProgDuration_Set.TimeSpan
            Dim fastEasing As IEasingFunction = New QuadraticEase() With {
                .EasingMode = EasingMode.EaseOut
            }

            Dim afterFast As Action =
                Sub()
                    If myVersion <> _animVersion Then Return
                    StartSlowEase(newPct, nextVal, myVersion)
                End Sub

            StartFrameTween(currVisible, newPct, fastDuration,
                            fastEasing, myVersion, afterFast)
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

            Dim completion As Action =
                Sub()
                    If myVersion <> _animVersion Then Return

                    SyncLock _lockObj
                        If _cts IsNot Nothing Then
                            _cts.Dispose()
                            _cts = Nothing

                            _currentSlowTarget = Nothing
                        End If
                    End SyncLock
                End Sub

            Dim reg = token.Register(
                Sub()
                    Me.Dispatcher.Invoke(
                        Sub()
                            Dim aniCurVal = CDbl(GetValue(AnimatedProgressProperty))

                            StopRenderingLoop()
                            BeginAnimation(AnimatedProgressProperty, Nothing)
                            SetValue(AnimatedProgressProperty, aniCurVal)
                        End Sub)
                End Sub)

            Dim curVal = CDbl(GetValue(AnimatedProgressProperty))
            Dim nextDur = CalcNextDuration(curVal, toVal)

            Dim slowEasing As IEasingFunction = New ExponentialEase() With {
                .EasingMode = EasingMode.EaseInOut
            }

            Dim wrappedCompletion As Action =
                Sub()
                    Try
                        completion.Invoke()
                    Finally : reg.Dispose() : End Try
                End Sub

            StartFrameTween(fromVal, toVal, nextDur.TimeSpan,
                            slowEasing, myVersion, wrappedCompletion)
        End Sub

        Public Function SnapToCurrentSlowTarget(Optional keepTarget As Boolean = False) As Double?
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

                    If Not keepTarget Then
                        _currentSlowTarget = Nothing

                    End If

                    Return target
                End If
            End SyncLock

            Return Nothing
        End Function

        Private Function CalcNextDuration(fromVal As Double, toVal As Double) As Duration
            Dim diff As Double = Math.Abs(toVal - fromVal)
            Dim diffN As Double = Math.Min(1.0, diff / 100)

            Dim minSec As Double = 250
            Dim maxSec As Double = 350
            Dim secs = minSec + (maxSec - minSec) * diffN

            Return New Duration(TimeSpan.FromMilliseconds(secs))
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
                Catch ex As Exception : End Try

                SyncLock idxLoadAniTasks
                    For Each tcs In idxLoadAniTasks
                        Try
                            tcs.TrySetResult(True)
                        Catch : End Try
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

            Dim progressRatio =
            (Progress - Minimum) / (Maximum - Minimum)

            progressRatio = Math.Max(0.0, Math.Min(1.0, progressRatio))

            Dim fillWidth = ActualWidth * progressRatio

            ' Dim fillWidth = (Progress / 100.0) * w

            If fillWidth > 0.0001 Then
                Dim progFill_Rect As New Rect(0, 0, fillWidth, h)

                Dim progFill_CornerRadius As CornerRadius

                If AnimatedProgress >= 100.0 - 2.5 Then
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

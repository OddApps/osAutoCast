Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.PrefSaveState
Imports osAutoCast.DataTypeLib.PrefUI_State
Imports osAutoCast.DataTypeLib.PromptResponse
Imports osAutoCast.DataTypeLib.LoadSpinColors
Imports osAutoCast.DataTypeLib.TriggerLoadSpinVis
Imports osAutoCast.osControls
Imports osAutoCast.osVisualAdapter
Imports osKeyTime = System.Windows.Media.Animation.KeyTime
Imports osPrefData = osAutoCast.osPrefLib.osPreferenceLib
Imports osColor = System.Windows.Media.Color

#Disable Warning BC42353
#Disable Warning BC42104
#Disable Warning BC42024

Public Class osPrefs_GUI

    Private Function GetVisualState(objVisType As PrefUI_State) As String
        Return idxOsPrefVisuals.First(
            Function(visKey)
                Return visKey.Key = objVisType
            End Function).Value
    End Function

    Private Sub BufferPrefWin()
        Me.Show()
        Me.Hide()
    End Sub

    Private Function EstablishVisConfig() As VisAdapterConfig
        Return VisAdapterConfig.EnableAll
    End Function

    Public Async Function osPrefs_InitUi() As Task
        Await InitializeVisAdapter(
            GetVisualState(PrefUI_Open), Me, EstablishVisConfig(),
             Sub()
                 BufferPrefWin()
                 InitLoadSpinner()
             End Sub,
                Sub()
                    SetVisualMode(PrefUI_Open)
                    ActivatePrefTracker()
                End Sub, SetVisTargets())
    End Function

    Private Function SetVisTargets() As UIElement()
        Return {prefContainer, osPrefsContentContainer,
            osTitleCover, osContentContainer}
    End Function

    Public Sub SetCloseMonitor(objAwaitClose As TaskCompletionSource(Of Boolean))
        objCloseMonitor = objAwaitClose
    End Sub

    Private Sub SetOpenEvents()
        evtComplete_Open =
            Sub()
                RemoveHandler VisDataObject.Completed, evtComplete_Open

                SetVisualMode(PrefUI_Open)
                ActivatePrefTracker()
            End Sub

        AddHandler VisDataObject.Completed, evtComplete_Open
    End Sub

    Private Sub SetVisualMode(valPrefState As PrefUI_State)
        Select Case valPrefState
            Case PrefUI_Open
                osTitleCover.Visibility = Visibility.Collapsed
            Case PrefUI_Close
                osTitleCover.Visibility = Visibility.Visible
        End Select
    End Sub

    Private Async Function SetCloseEvents() As Task
        Await SetCloseVisualData_WithTask(
            GetVisualState(PrefUI_Close),
            Sub() Me.Close())

        SetVisualMode(PrefUI_Close)
    End Function

    Public Sub osPrefsIU_Present()
        Me.Show()

        Dim objHwnd = New WindowInteropHelper(Me).Handle
        SetWindowPos(objHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)

        Me.Topmost = True
    End Sub

    Private Async Sub osPrefsBtnClk_SavePrefs(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Save.Click
        If objOsPrefTracker.prefsChanged Then
            Dim chkDoSave = GetResponse(PromptType.Prefs_Save)

            If chkDoSave = isYes Then
                Await objTask_InitSpin
                Await TriggerPrefSave()
            Else
                DisposeLoadSpinner()
            End If
        End If
    End Sub

    Private Sub InitLoadSpinner()
        Dim uiScheduler = TaskScheduler.FromCurrentSynchronizationContext()
        Dim uiTaskFactory = New TaskFactory(uiScheduler)

        Dim objTask_InitLoadSpinner As New Task(
            Sub()
                objTask_InitSpin = InitializeLoadSpinner(uiScheduler)
            End Sub)

        objTask_InitLoadSpinner.Start(uiScheduler)

        'Await PrepDispatcher().InvokeAsync(
        '    Sub()
        '        Dim objTask_InitLoadSpinner As New Task(
        '            Sub()
        '                objTask_InitSpin = InitializeLoadSpinner(uiScheduler)
        '            End Sub)

        '        objTask_InitLoadSpinner.Start(uiScheduler)
        '    End Sub, DispatcherPriority.Background)
    End Sub

    Private Async Function InitializeLoadSpinner(objTaskSchedule As TaskScheduler) As Task(Of Boolean)
        Dim InitLoadSpin_TaskArray As Action() = {
            Sub()
                objLoadSpinner = New osControls.osLoadSpinner With {
                    .SpinnerSize = 50, .StrokeThickness = 18, .IsSpinning = False,
                    .SpinnerBrush = GenerateLoadSpinColor(LoadColor_Spinner)
                }

                osSpinLoadMsg = ComposeLoadMsgElement()

                Dim objTask_InitLoadMsg = InitLoadMsgFadeVis()
            End Sub,
            Sub()
                osSpinLoadContainer = New Grid With {
                    .Name = "osSpinLoadContainer",
                    .IsHitTestVisible = True,
                    .Visibility = Visibility.Visible, .Opacity = 0,
                    .Background = GenerateLoadSpinColor(LoadColor_Container),
                    .HorizontalAlignment = HorizontalAlignment.Stretch,
                    .VerticalAlignment = VerticalAlignment.Stretch
                }

                Grid.SetRow(osSpinLoadContainer, 0)
                Grid.SetRowSpan(osSpinLoadContainer, 2)

                Panel.SetZIndex(osSpinLoadContainer, 999)
            End Sub,
            Sub()
                objLoadSpinContent = New StackPanel With {
                    .Orientation = Orientation.Vertical,
                    .HorizontalAlignment = HorizontalAlignment.Stretch,
                    .VerticalAlignment = VerticalAlignment.Stretch,
                    .Margin = New Thickness(8, 4, 8, 2),
                    .MinWidth = 82
                }

                objLoadSpinContainer = New Border With {
                    .Background = GenerateLoadSpinColor(LoadColor_SpinContainer),
                    .BorderBrush = GenerateLoadSpinColor(LoadColor_SpinContainerBorder),
                    .HorizontalAlignment = HorizontalAlignment.Center, .VerticalAlignment = VerticalAlignment.Center,
                    .Margin = New Thickness(0, 0, 0, 20), .Padding = New Thickness(4, 8, 4, 4), .Opacity = 0,
                    .CornerRadius = New CornerRadius(8), .BorderThickness = New Thickness(8)
                }
            End Sub,
            Sub()
                GenVis_ShowLoadOverlay()
            End Sub,
            Sub()
                objLoadSpinContent.Children.Add(objLoadSpinner)
                objLoadSpinContent.Children.Add(osSpinLoadMsg)

                objLoadSpinContainer.Child = objLoadSpinContent
                osSpinLoadContainer.Children.Add(objLoadSpinContainer)
            End Sub
        }

        Dim LoadSpin_InitTasks = InitLoadSpin_TaskArray.
            Take(InitLoadSpin_TaskArray.Length - 1).Select(
                Function(objTaskInit)
                    Dim objCreateTask = CreateTask(objTaskInit)
                    objCreateTask.Start(objTaskSchedule)

                    Return objCreateTask
                End Function)

        Await Task.WhenAll(LoadSpin_InitTasks)

        Dim objTask_PrepLoadSpinner =
            CreateTask(InitLoadSpin_TaskArray.Last())

        objTask_PrepLoadSpinner.Start(objTaskSchedule)
        Await objTask_PrepLoadSpinner

        Return True
    End Function

    Private Function ComposeLoadMsgElement() As TextBlock
        Dim a As New TextBlock With {
            .Text = "Please Wait", .FontSize = 16,
            .Foreground = Brushes.White, .Opacity = 0,
            .Margin = New Thickness(0, 8, 0, 0),
             .SnapsToDevicePixels = True,
            .HorizontalAlignment = HorizontalAlignment.Center
        }

        TextOptions.SetTextRenderingMode(a, TextRenderingMode.ClearType)

        Dim glower As New osEffectManager.osEffect_Glow() With {
            .Thickness = 0.75,
            .Fade = 1,
            .GlowStrength = 0.25,
            .GlowColor = Color.FromArgb(&HA1, &HC1, &HD, &HD)
        }

        AddHandler a.SizeChanged, Sub(sender, e)
                                      glower.TexelSize = New Size(a.ActualWidth, a.ActualHeight)
                                  End Sub

        a.Effect = glower

        Return a
    End Function

    Private Function UpdateLoadSpinMsg() As Task
        Return ValidateDispatch(
            Sub()
                objVisLoadMsg_FadeOut.Begin(osSpinLoadMsg)
            End Sub)
    End Function

    Private Function CreateLoadMsgVis(Optional isFadeIn As Boolean = True) As DoubleAnimation
        Return New DoubleAnimation() With {
            .From = If(isFadeIn, 0, 1), .[To] = If(isFadeIn, 1, 0),
            .FillBehavior = FillBehavior.HoldEnd,
            .Duration = SetVisDuration(115),
            .EasingFunction = New QuadraticEase() With {
                .EasingMode = EasingMode.EaseIn
            }
        }
    End Function

    Private Function CreateLoadMsgDisplayVis() As DoubleAnimation
        Return New DoubleAnimation() With {
            .From = 0, .[To] = 1,
            .FillBehavior = FillBehavior.HoldEnd,
            .Duration = SetVisDuration(175),
            .EasingFunction = New ExponentialEase With {
                .EasingMode = EasingMode.EaseIn,
                    .Exponent = 3.8
            }
        }
    End Function

    Private Async Function InitLoadMsgFadeVis() As Task
        Await Task.WhenAll(PrepLoadMsgFadeVis(True, objVisLoadMsg_FadeIn),
                           PrepLoadMsgFadeVis(False, objVisLoadMsg_FadeOut),
                           PrepLoadMsgFadeDisplayVis())

        Dim evtVisOverlayMsg_FadeOut As EventHandler =
                Sub()
                    RemoveHandler objVisLoadMsg_FadeOut.Completed, evtVisOverlayMsg_FadeOut

                    osSpinLoadMsg.Text = "Complete"
                    objVisLoadMsg_FadeIn.Begin(osSpinLoadMsg)
                End Sub

        AddHandler objVisLoadMsg_FadeOut.Completed, evtVisOverlayMsg_FadeOut
    End Function

    Private Function PrepLoadMsgFadeVis(isFadeIn As Boolean, ByRef objFadeVisData As Storyboard) As Task
        Dim objVisLoadMsgFade = CreateLoadMsgVis(isFadeIn)

        objFadeVisData = New Storyboard()
        objFadeVisData.Children.Add(objVisLoadMsgFade)

        Storyboard.SetTarget(objVisLoadMsgFade, osSpinLoadMsg)
        Storyboard.SetTargetProperty(objVisLoadMsgFade, New PropertyPath(TextBlock.OpacityProperty))

        Return Task.CompletedTask
    End Function

    Private Function PrepLoadMsgFadeDisplayVis() As Task
        Dim objVisLoadMsgFade = CreateLoadMsgDisplayVis()

        objVisLoadMsg_Display = New Storyboard()
        objVisLoadMsg_Display.Children.Add(objVisLoadMsgFade)

        Storyboard.SetTarget(objVisLoadMsgFade, osSpinLoadMsg)
        Storyboard.SetTargetProperty(objVisLoadMsgFade, New PropertyPath(TextBlock.OpacityProperty))

        Dim evtVisOverlayMsg_Display As EventHandler =
               Async Sub()
                   RemoveHandler objVisLoadMsg_Display.Completed, evtVisOverlayMsg_Display
                   Await Task.Delay(85)
                   objLoadSpinDispMsgMonitor.SetResult(True)
               End Sub

        AddHandler objVisLoadMsg_Display.Completed, evtVisOverlayMsg_Display

        Return Task.CompletedTask
    End Function

    Public Function TriggerLoadSpinVisuals(visTrigger As TriggerLoadSpinVis) As Task
        Select Case visTrigger
            Case TriggerVis_Display
                osPrefsContentContainer.Children.Add(osSpinLoadContainer)

                objLoadSpinDisplayMonitor.ResetAndInitTask()

                objVis_ShowLoadSpin.Begin(osSpinLoadContainer)
                Return objLoadSpinDisplayMonitor.Task
            Case TriggerVis_DisplayMsg
                objLoadSpinDispMsgMonitor.ResetAndInitTask()

                objVisLoadMsg_Display.Begin(osSpinLoadMsg)
                Return objLoadSpinDispMsgMonitor.Task
            Case TriggerVis_DisplaySpinner
                objLoadSpinMonitor.ResetAndInitTask()

                objLoadSpinner.SetLoadSpinStartMonitor(objLoadSpinMonitor)
                objLoadSpinner.IsSpinning = True

                Return objLoadSpinMonitor.Task
        End Select
    End Function

    Public Async Function ComposeLoadOverlay() As Task
        InputUI_Prevent()

        Await TriggerLoadSpinVisuals(TriggerVis_Display)

        Await Task.Delay(75)
        Await TriggerLoadSpinVisuals(TriggerVis_DisplayMsg)
        Await Task.Delay(75)
        Await TriggerLoadSpinVisuals(TriggerVis_DisplaySpinner)


    End Function

    Private Function ComposeShowContentVis(Optional isContainer As Boolean = True) As DoubleAnimation
        Dim valVisDur = If(isContainer, 325, 325)

        Return New DoubleAnimation() With {
            .From = 0, .[To] = 1, .FillBehavior = FillBehavior.HoldEnd,
            .Duration = SetVisDuration(valVisDur),
            .EasingFunction = New QuadraticEase() With {
                .EasingMode = EasingMode.EaseIn
            }
        }
    End Function

    Public Sub GenVis_ShowLoadOverlay()
        Dim objVisShowLoad = ComposeShowContentVis()
        Dim objVisShowLoadContent = ComposeShowContentVis(False)

        objVis_ShowLoadSpin = New Storyboard()

        objVis_ShowLoadSpin.Children.Add(objVisShowLoad)
        objVis_ShowLoadSpin.Children.Add(objVisShowLoadContent)

        Storyboard.SetTarget(objVisShowLoad, osSpinLoadContainer)
        Storyboard.SetTargetProperty(objVisShowLoad, GetPropPath(True))

        Storyboard.SetTarget(objVisShowLoadContent, objLoadSpinContainer)
        Storyboard.SetTargetProperty(objVisShowLoadContent, GetPropPath(False))

        Dim evtDispComplete As EventHandler = Nothing
        evtDispComplete =
            Async Sub()
                RemoveHandler objVis_ShowLoadSpin.Completed, evtDispComplete

                Await Task.Delay(115)
                objLoadSpinDisplayMonitor.SetResult(True)
            End Sub

        AddHandler objVis_ShowLoadSpin.Completed, evtDispComplete
    End Sub

    Public Function GenVis_CloseLoadOverlay() As DoubleAnimation
        Dim objVis_Overlay As New DoubleAnimation() With {
            .[To] = 0, .Duration = SetVisDuration(375),
            .EasingFunction = New QuadraticEase() With {
                .EasingMode = EasingMode.EaseIn
            }
        }

        Dim evtVisOverlay As EventHandler =
            Sub()
                RemoveHandler objVis_Overlay.Completed, evtVisOverlay

                objLoadSpinContainer.Visibility = Visibility.Collapsed
                osPrefsContentContainer.Children.Remove(osSpinLoadContainer)

                DisposeLoadSpinner()
            End Sub

        AddHandler objVis_Overlay.Completed, evtVisOverlay

        Return objVis_Overlay
    End Function

    Private Sub DisposeLoadSpinner()
        osSpinLoadContainer.Children.Clear()

        objLoadSpinContent = Nothing
        objLoadSpinner = Nothing
        objLoadSpinContainer = Nothing
        osSpinLoadContainer = Nothing

        objVisLoadMsg_FadeIn = Nothing
        objVisLoadMsg_FadeOut = Nothing

        osSpinLoadMsg = Nothing

        objVis_ShowLoadSpin.Stop()
        objVis_ShowLoadSpin.Children.Clear()
        objVis_ShowLoadSpin = Nothing
    End Sub

    Public Function ComposeLoadCompleteVis() As DoubleAnimation
        Dim objLoadCompVis As New DoubleAnimation() With {
            .From = 0, .To = 100, .FillBehavior = FillBehavior.HoldEnd,
            .Duration = SetVisDuration(1325),
            .EasingFunction = New QuadraticEase With {
                .EasingMode = EasingMode.EaseInOut
            }
        }

        Return objLoadCompVis
    End Function

    Private Function ComposeLoadCompleteColorVis() As ColorAnimation
        Return New ColorAnimation(
            GenerateLoadSpinColor(LoadColor_SpinnerLoadComplete).Color, SetVisDuration(1325)) With {
                .FillBehavior = FillBehavior.HoldEnd,
                .EasingFunction = New QuadraticEase With {
                    .EasingMode = EasingMode.EaseInOut
                }
            }
    End Function

    Private Function ComposeLoadCompleteStrokeVis() As DoubleAnimation
        Return New DoubleAnimation() With {
                .FillBehavior = FillBehavior.HoldEnd,
                .AutoReverse = True,
                .EasingFunction = New QuadraticEase With {
                    .EasingMode = EasingMode.EaseInOut
                }
            }
    End Function

    Private Sub PrepSpinLoadCompleteColorData(objLoadSpinnerBrush As Path)
        Dim objLoadSpinnerBrushColor = TryCast(objLoadSpinnerBrush.Stroke, SolidColorBrush)

        objLoadSpinnerBrushColor = objLoadSpinnerBrushColor.CloneCurrentValue()
        objLoadSpinnerBrush.Stroke = objLoadSpinnerBrushColor
    End Sub

    Public Async Function TriggerLoadComplete() As Task
        Dim objLoadSpinBrush = objLoadSpinner.GetSpinnerBrush()
        PrepSpinLoadCompleteColorData(objLoadSpinBrush)

        Dim objLoadComplete = ComposeLoadCompleteVis()
        Dim objLoadCompleteColor = ComposeLoadCompleteColorVis()

        Dim objVis_CloseLoadOverlay = New Storyboard()
        objVis_CloseLoadOverlay.Children.Add(objLoadComplete)
        objVis_CloseLoadOverlay.Children.Add(objLoadCompleteColor)

        Storyboard.SetTarget(objLoadComplete, objLoadSpinner)
        Storyboard.SetTargetProperty(objLoadComplete, New PropertyPath(osControls.osLoadSpinner.LoadProgressProperty))

        Storyboard.SetTarget(objLoadCompleteColor, objLoadSpinBrush)
        Storyboard.SetTargetProperty(objLoadCompleteColor, New PropertyPath("(Shape.Stroke).(SolidColorBrush.Color)"))

        AddHandler objVis_CloseLoadOverlay.Completed,
            Async Sub()
                Await Task.Delay(750)
                Await HideOverlay()
            End Sub

        objLoadSpinner.IsSpinning = False

        Await UpdateLoadSpinMsg()
        objVis_CloseLoadOverlay.Begin(objLoadSpinner)
    End Function

    Private Sub HideOverlay_UI()
        SyncLock objLoadSpinLock
            If objLoadSpinContainer Is Nothing Then Return

            Dim visFadeOut = GenVis_CloseLoadOverlay()

            osSpinLoadContainer.BeginAnimation(Grid.OpacityProperty, visFadeOut)

            SetSaveState(PrefSaveState.Prefs_Saved)
            InputUI_Allow()
        End SyncLock
    End Sub

    Private objTask_LoadSpinComplete As TaskCompletionSource(Of Boolean)

    Public Async Function DisplayLoadTask(taskReload As Func(Of Task)) As Task
        Await ComposeLoadOverlay()

        objTask_LoadSpinComplete.ResetAndInitTask()
        Await PrepDispatcher().InvokeAsync(
            Async Function()
                Await taskReload()
            End Function, DispatcherPriority.Background)

        Await objTask_LoadSpinComplete.Task
        Await TriggerLoadComplete()
    End Function

    Private Async Function TriggerPrefSave(Optional closeOnSave As Boolean = False) As Task
        Await DisplayLoadTask(AddressOf ReloadPrefs)

        If closeOnSave Then
            Await osPrefs_InitClose()
        End If
    End Function

    Public Function GenTaskDuration() As Double
        Dim rndTaskDur As New Random()

        Return TaskDur_Min + (rndTaskDur.
            NextDouble() * (TaskDur_Max - TaskDur_Min))
    End Function

    Private Async Function ReloadPrefs() As Task
        Await osPrefDataIdx.SavePrefsFileAsync()

        Await PrepDispatcher().InvokeAsync(
            Async Function()
                Await osHandler_AutoCast.StopAutoCastAsync()

                Await Task.Delay(GenTaskDuration())
                Await osHandler_AutoCast.StartAutoCastAsync()
                Await Task.Delay(GenTaskDuration())
            End Function, DispatcherPriority.SystemIdle).Task.Unwrap()

        'Await Task.Run(
        '    Async Function()
        '        Await osHandler_AutoCast.StopAutoCastAsync()

        '        Await Task.Delay(GenTaskDuration())
        '        Await osHandler_AutoCast.StartAutoCastAsync()
        '        Await Task.Delay(GenTaskDuration())
        '    End Function)

        ActivatePrefTracker(True)
        RefreshVisQuality()

        objTask_LoadSpinComplete.SetResult(True)
    End Function

    Private Sub RefreshVisQuality()
        _VisQualitySetting = "n/a"
        OnPropertyChanged(NameOf(VisQualitySetting))
    End Sub

    Private Async Function osPrefs_InitClose() As Task
        Await SetCloseEvents()
        objCloseMonitor.TrySetResult(True)
    End Function

    Public Async Function HideOverlay() As Task
        Await Dispatcher.InvokeAsync(AddressOf HideOverlay_UI)
        Await Dispatcher.Yield(DispatcherPriority.Render)
    End Function

    Public Function GetLoadSpinContainer() As StackPanel
        Return DirectCast(objLoadSpinContainer.Child, StackPanel)
    End Function

    Public Function GetLoadSpinner() As osControls.osLoadSpinner
        Return DirectCast(GetLoadSpinContainer().Children(0), osControls.osLoadSpinner)
    End Function

    Private Async Sub osPrefsBtnClk_Close(sender As Object, e As RoutedEventArgs) Handles osPrefsBtn_Close.Click
        Select Case GetPrefSaveState()
            Case Prefs_NoChanges
                Await osPrefs_InitClose()
            Case Prefs_Saved
                Await osPrefs_InitClose()
            Case Prefs_NotSaved
                '   InitLoadSpinner()

                Select Case GetResponse(PromptType.Prefs_Close)
                    Case isYes
                        Await objTask_InitSpin
                        Await TriggerPrefSave(True)
                    Case isNo
                        objTask_InitSpin = Nothing
                        DisposeLoadSpinner()

                        RevertPrefSettings()
                    Case isCancel
                        objTask_InitSpin = Nothing
                        DisposeLoadSpinner()

                        Exit Sub
                End Select
        End Select
    End Sub

    Private Sub osPreferenceLib_PropertyChanged(sender As Object, e As PropertyChangedEventArgs)
        If prefsReverted Then
            SetSaveState(PrefSaveState.Prefs_NoChanges)
            prefsReverted = False
        Else
            SetSaveState(PrefSaveState.Prefs_NotSaved)
        End If
    End Sub

    Private Sub SetSaveState(valSaveState As PrefSaveState)
        Select Case valSaveState
            Case PrefSaveState.Prefs_NotSaved
                isSaved = False
                IsSaveEnabled = True
            Case PrefSaveState.Prefs_Saved
                isSaved = True
                IsSaveEnabled = False
            Case PrefSaveState.Prefs_NoChanges
                isSaved = True
                IsSaveEnabled = False
        End Select
    End Sub

End Class

Partial Public Class osPrefs_GUI
    Implements INotifyPropertyChanged

    Private objCloseMonitor As TaskCompletionSource(Of Boolean)

    Private evtComplete_Open As EventHandler
    Private evtComplete_Close As EventHandler

    Private isSaved As Boolean = False
    Private prefsReverted As Boolean = False

    Private objOsPrefTracker As osPrefLib.osPrefMonitor(Of osPrefData)

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOACTIVATE As UInteger = &H10

    Private TaskDur_Min As Double = 2225
    Private TaskDur_Max As Double = 2750

    Private objLoadSpinMonitor As TaskCompletionSource(Of Boolean)
    Private objLoadSpinDisplayMonitor As TaskCompletionSource(Of Boolean)
    Private objLoadSpinDispMsgMonitor As TaskCompletionSource(Of Boolean)

    Private objLoadSpinContainer As Border
    Private osSpinLoadContainer As Grid
    Private objLoadSpinContent As StackPanel
    Private objTask_InitSpin As Task(Of Boolean)
    Private objLoadSpinner As osControls.osLoadSpinner

    Private objVis_ShowLoadSpinContent As Storyboard = Nothing
    Private objVis_ShowLoadSpin As Storyboard = Nothing

    Private objVisLoadMsg_Display As Storyboard = Nothing

    Private objVisLoadMsg_FadeIn As Storyboard = Nothing
    Private objVisLoadMsg_FadeOut As Storyboard = Nothing

    Private osSpinLoadMsg As TextBlock
    Private osSpinLoadMsgText As Border

    Private ReadOnly objLoadSpinLock As New Object()

    Private idxOsPrefVisuals As New Dictionary(Of PrefUI_State, String) From {
        {PrefUI_Open, "osPrefsVis_Disp"},
        {PrefUI_Close, "osPrefsVis_Close"}
    }

    Private _VisQualitySetting As String = "n/a"
    Public ReadOnly Property VisQualitySetting As String
        Get
            If _VisQualitySetting = "n/a" Then
                _VisQualitySetting = osPrefData.Data.GenOpts_VisualQuality
            End If

            If isSaved Then
                _VisQualitySetting = osPrefData.Data.GenOpts_VisualQuality
            End If

            Return _VisQualitySetting
        End Get
    End Property

    Private _saveEnabled As Boolean = False
    Public Property IsSaveEnabled As Boolean
        Get
            Return _saveEnabled
        End Get
        Set(ByVal canSave As Boolean)
            _saveEnabled = canSave
            OnPropertyChanged(NameOf(IsSaveEnabled))
        End Set
    End Property

    Public ReadOnly Property prefContainer As osBorder
        Get
            Return Me.osPrefsMainContainer
        End Get
    End Property


    Public ReadOnly Property osContentContainer As Grid
        Get
            Return Me.BottomContainer
        End Get
    End Property

    Public ReadOnly Property osPrefsContentContainer As Grid
        Get
            Return Me.osPrefsContentContain
        End Get
    End Property

    Public ReadOnly Property osTitleCover As Border
        Get
            Return Me.osPrefsTitlePanel
        End Get
    End Property

    Public ReadOnly Property osScaleRender As ScaleTransform
        Get
            Return Me.osPrefsOutlineRender
        End Get
    End Property

    Private ReadOnly Property osPrefDataIdx As osPrefData.osPrefIndex
        Get
            Return osPrefData.Data.objOsPrefIdx
        End Get
    End Property

    Private ReadOnly Property osVisDataArray As IEnumerable(Of DoubleAnimationUsingKeyFrames)
        Get
            Return VisDataObject.Children.OfType(Of DoubleAnimationUsingKeyFrames)
        End Get
    End Property

    Private Function CreatePrefMonitor() As osPrefLib.osPrefMonitor(Of osPrefData)
        Return New osPrefLib.osPrefMonitor(Of osPrefData)
    End Function

    Private Sub InitializePrefMonitor()
        objOsPrefTracker.Attach(DirectCast(Me.DataContext, osPrefData))
    End Sub

    Public Sub ActivatePrefTracker(Optional doReset As Boolean = False)
        If doReset Then
            objOsPrefTracker.Detach()
            objOsPrefTracker = Nothing
        End If

        objOsPrefTracker = CreatePrefMonitor()
        InitializePrefMonitor()
    End Sub

    Public Sub RevertPrefSettings()
        prefsReverted = True
        objOsPrefTracker.Revert()
    End Sub

    Private Function isMouseDown(e As MouseButtonEventArgs) As Boolean
        Return e.LeftButton = MouseButtonState.Pressed
    End Function

    Private Function HasPrefChanges() As Boolean
        Return objOsPrefTracker.prefsChanged
    End Function

    Private Function HasUnsavedChanges() As Boolean
        Return HasPrefChanges() And Not isSaved
    End Function

    Private Function HasNoChanges() As Boolean
        Return Not HasPrefChanges() AndAlso Not isSaved
    End Function

    Private Function GetPrefSaveState() As PrefSaveState
        If HasUnsavedChanges() Then
            Return Prefs_NotSaved : End If

        Select Case True
            Case HasNoChanges() : Return Prefs_NoChanges
            Case isSaved : Return Prefs_Saved
        End Select
    End Function

    Private Sub DragMovePrefWin(sender As Object, e As MouseButtonEventArgs) Handles osPrefs_TitleBar.MouseLeftButtonDown
        If isMouseDown(e) Then
            DragMove()
        End If
    End Sub

    Private Function SetVisDuration(valDur As Double) As Duration
        Return New Duration(TimeSpan.FromMilliseconds(valDur))
    End Function

    Private Function SetVisKeyTime(valDur As Double) As osKeyTime
        Return osKeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(valDur))
    End Function

    Private Function SetVisAttr() As PropertyPath
        Return New PropertyPath("(TextBlock.Foreground).(SolidColorBrush.Color)")
    End Function

    Private Function ComposeFrame_Start(valColorData As osColor) As ColorKeyFrame
        Return New EasingColorKeyFrame(valColorData, SetVisKeyTime(0))
    End Function

    Private Function ComposeFrame_End(valColorData As osColor) As ColorKeyFrame
        Return New EasingColorKeyFrame(valColorData, SetVisKeyTime(75), SetVisEasing())
    End Function

    Private Function SetVisEasing() As IEasingFunction
        Return New QuadraticEase With {
            .EasingMode = EasingMode.EaseIn
        }
    End Function

    Private Function CalcRGB(vR As Byte, vG As Byte, vB As Byte, Optional vA As Byte = 255) As osColor
        Return Color.FromArgb(vA, vR, vG, vB)
    End Function

    Private Function GenColor(vR As Byte, vG As Byte, vB As Byte, Optional vA As Byte = 255) As SolidColorBrush
        Return New SolidColorBrush(Color.FromArgb(vA, vR, vG, vB)).FreezeReturn()
    End Function

    Private Function GetPropPath(isGrid As Boolean) As PropertyPath
        If isGrid Then
            Return New PropertyPath(Grid.OpacityProperty)
        Else
            Return New PropertyPath(Border.OpacityProperty)
        End If
    End Function

    Private Function GenerateLoadSpinColor(objLoadSpinColors As LoadSpinColors) As SolidColorBrush
        Dim objOut_Color As SolidColorBrush

        Select Case objLoadSpinColors
            Case LoadColor_Spinner
                objOut_Color = GenColor(80, 15, 15)
            Case LoadColor_SpinContainer
                objOut_Color = GenColor(26, 26, 26, 240)
            Case LoadColor_Container
                objOut_Color = GenColor(10, 10, 10, 115)
            Case LoadColor_SpinContainerBorder
                objOut_Color = GenColor(18, 18, 18)
            Case LoadColor_SpinnerLoadComplete
                objOut_Color = GenColor(57, 128, 57)
        End Select

        Return objOut_Color.Clone()
    End Function

    Private Sub YieldVisuals() : End Sub

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer,
                                         Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub OnPropertyChanged(Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

End Class
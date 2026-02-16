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
Imports osAutoCast.DataTypeLib.TerminateUiType
Imports osAutoCast.osControls
Imports osAutoCast.osHandler_UI
Imports osAutoCast.osVisualAdapter
Imports osAutoCast.osEffectManager
Imports osKeyTime = System.Windows.Media.Animation.KeyTime
Imports osPrefData = osAutoCast.osPrefLib.osPreferenceLib
Imports osColor = System.Windows.Media.Color
Imports System.Windows.Media.Effects

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
            osTitleCover, osBottomContentContainer, osBottomContent}
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
            End If
        End If
    End Sub

    Private Sub InitLoadSpinner()
        Dim uiScheduler = TaskScheduler.FromCurrentSynchronizationContext()

        Dim objTask_InitLoadSpinner As New Task(
            Sub()
                objTask_InitSpin = InitializeLoadSpinner(uiScheduler)
            End Sub)

        Dim objTask_PrepLoadSpinner = Task.Run(
            Sub()
                objTask_InitLoadSpinner.Start(uiScheduler)
            End Sub)
    End Sub

    Private Async Function InitializeLoadSpinner(objTaskSchedule As TaskScheduler) As Task(Of Boolean)
        Dim InitLoadSpin_TaskArray As Action() = {
            Sub()
                objLoadSpinner = New osControls.osLoadSpinner With {
                    .SpinnerSize = 50, .StrokeThickness = 18, .IsSpinning = False,
                    .SpinnerBrush = GenerateLoadSpinBrush(LoadColor_Spinner, True),
                    .Margin = New Thickness(6, 4, 6, 0)
                }

                osSpinLoadMsg = New TextBlock With {
                    .Name = "txtSpinLoadMsg", .Text = "Please Wait", .FontSize = 18, .Opacity = 1,
                    .Foreground = GenerateLoadSpinBrush(LoadColor_MsgText).FreezeReturn(),
                    .Margin = New Thickness(0, 8, 0, 2), .Padding = New Thickness(0, 4, 1, 4),
                    .HorizontalAlignment = HorizontalAlignment.Stretch,
                    .FontFamily = New FontFamily("Segoe UI Black"),
                    .FontWeight = FontWeights.Bold
                }

                RenderOptions.SetBitmapScalingMode(osSpinLoadMsg, BitmapScalingMode.HighQuality)

                TextOptions.SetTextRenderingMode(osSpinLoadMsg, TextRenderingMode.ClearType)
                TextOptions.SetTextFormattingMode(osSpinLoadMsg, TextFormattingMode.Ideal)

                Dim txtLoadMsg_Glow As New osEffect_Glow() With {
                    .Fade = 1.0, .GlowStrength = 1.75,
                    .VerticalGlow = -2.0, .Thickness = 0.58,
                    .GlowColor = GenerateLoadSpinColor(LoadColor_MsgTextGlow)
                }

                PrepEffectBinding(txtLoadMsg_Glow)
                osSpinLoadMsg.Effect = txtLoadMsg_Glow

                osSpinLoadMsgText = New Border() With {
                     .Name = "osSpinLoadMsgText",
                    .HorizontalAlignment = HorizontalAlignment.Center,
                    .VerticalAlignment = VerticalAlignment.Center,
                    .Child = osSpinLoadMsg, .Opacity = 0,
                    .Padding = New Thickness(0)
                }

                RenderOptions.SetBitmapScalingMode(osSpinLoadMsgText, BitmapScalingMode.HighQuality)

                TextOptions.SetTextRenderingMode(osSpinLoadMsgText, TextRenderingMode.ClearType)
                TextOptions.SetTextFormattingMode(osSpinLoadMsgText, TextFormattingMode.Ideal)

                Dim txtLoadMsg_Stroke As New osEffect_Stroke() With {
                    .Thickness = 0.875, .Spread = 4.75,
                    .Fade = 1.0, .StrokeStrength = 0.85,
                    .StrokeColor = GenerateLoadSpinColor(LoadColor_MsgTextStroke)
                }

                PrepEffectBinding(txtLoadMsg_Stroke)
                osSpinLoadMsgText.Effect = txtLoadMsg_Stroke

                Dim objTask_InitLoadMsg = InitLoadMsgFadeVis()
            End Sub,
            Sub()
                osSpinLoadContainer = New Grid With {
                    .Name = "osSpinLoadContainer",
                    .Visibility = Visibility.Visible, .Opacity = 0,
                    .VerticalAlignment = VerticalAlignment.Stretch,
                    .HorizontalAlignment = HorizontalAlignment.Stretch,
                    .Background = GenerateLoadSpinBrush(LoadColor_Container)
                }

                Grid.SetRow(osSpinLoadContainer, 0)
                Grid.SetRowSpan(osSpinLoadContainer, 2)

                Panel.SetZIndex(osSpinLoadContainer, 999)
            End Sub,
            Sub()
                objLoadSpinContent = New StackPanel With {
                    .Orientation = Orientation.Vertical, .MinWidth = 82,
                    .HorizontalAlignment = HorizontalAlignment.Stretch,
                    .VerticalAlignment = VerticalAlignment.Stretch
                }

                objLoadSpinContainer = New Border With {
                    .Name = "objLoadSpinContainer",
                    .Background = GenerateLoadSpinBrush(LoadColor_SpinContainer),
                    .BorderBrush = GenerateLoadSpinBrush(LoadColor_SpinContainerBorder),
                    .HorizontalAlignment = HorizontalAlignment.Center, .VerticalAlignment = VerticalAlignment.Center,
                    .Margin = New Thickness(0, 0, 0, 20), .Opacity = 0, .CornerRadius = New CornerRadius(8),
                    .BorderThickness = New Thickness(8), .Padding = New Thickness(4, 8, 4, 6),
                    .CacheMode = New BitmapCache(1.0), .Width = 136
                }

                Dim SpinContentBrdrGlow As New osEffect_Glow() With {
                    .Fade = 1, .GlowStrength = 3.35, .VerticalGlow = 10,
                    .GlowColor = GenerateLoadSpinColor(LoadColor_SpinContainerBorderGlow),
                    .Thickness = 1.775, .TexelSize = New Point(0.0031, 0.0031)
                }

                objLoadSpinContainer.Effect = SpinContentBrdrGlow
            End Sub,
            Sub()
                GenVis_ShowLoadOverlay()
            End Sub,
            Sub()
                objLoadSpinContent.Children.Add(objLoadSpinner)
                objLoadSpinContent.Children.Add(osSpinLoadMsgText)

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

    Private Function GenEffectBinding(Optional isHeight As Boolean = False) As Binding
        Return New Binding(
            $"Actual{If(isHeight, "Height", "Width")}") With {
                .Source = osSpinLoadMsg
            }
    End Function

    Private Sub PrepEffectBinding(ByRef objEffect As DependencyObject)
        Dim isStroke = TypeOf objEffect Is osEffect_Stroke

        With New MultiBinding()
            .Converter = CType(Me.Resources("TexelSizeConverter"), IMultiValueConverter)

            .Bindings.Add(GenEffectBinding())
            .Bindings.Add(GenEffectBinding(True))

            Dim propEffect = If(isStroke, osEffect_Stroke.
                TexelSizeProperty, osEffect_Glow.TexelSizeProperty)

            BindingOperations.SetBinding(objEffect, propEffect, .GetObj())
        End With
    End Sub

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
            .Duration = SetVisDuration(225),
            .EasingFunction = New QuadraticEase() With {
                .EasingMode = EasingMode.EaseIn
            }
        }
    End Function

    Private Function CreateLoadMsgDisplayVis() As DoubleAnimation
        Return New DoubleAnimation() With {
            .From = 0, .[To] = 1,
            .FillBehavior = FillBehavior.HoldEnd,
            .Duration = SetVisDuration(225),
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

        Storyboard.SetTarget(objVisLoadMsgFade, osSpinLoadMsgText)
        Storyboard.SetTargetProperty(objVisLoadMsgFade, New PropertyPath(Border.OpacityProperty))

        Return Task.CompletedTask
    End Function

    Private Function PrepLoadMsgFadeDisplayVis() As Task
        Dim objVisLoadMsgFade = CreateLoadMsgDisplayVis()

        objVisLoadMsg_Display = New Storyboard()
        objVisLoadMsg_Display.Children.Add(objVisLoadMsgFade)

        Storyboard.SetTarget(objVisLoadMsgFade, osSpinLoadMsgText)
        Storyboard.SetTargetProperty(objVisLoadMsgFade, New PropertyPath(Border.OpacityProperty))

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
                InputUI_Prevent()

                osPrefsContentContainer.Children.Add(osSpinLoadContainer)
                objLoadSpinDisplayMonitor.ResetAndInitTask()

                GetVisual_ContentBlur().Begin()
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
        Await TriggerLoadSpinVisuals(TriggerVis_Display)

        Await Task.Delay(75)
        Await TriggerLoadSpinVisuals(TriggerVis_DisplayMsg)

        Await Task.Delay(115)
        Await TriggerLoadSpinVisuals(TriggerVis_DisplaySpinner)
    End Function

    Private Function ComposeShowContentVis() As DoubleAnimation
        Return New DoubleAnimation() With {
            .From = 0, .[To] = 1, .FillBehavior = FillBehavior.HoldEnd,
            .Duration = SetVisDuration(325),
            .EasingFunction = New QuadraticEase() With {
                .EasingMode = EasingMode.EaseIn
            }
        }
    End Function

    Private Function GetVisual_ContentBlur(Optional isUnBlur As Boolean = False) As Storyboard
        Return TryCast(Me.Resources(If(isUnBlur, "osPrefsVis_ContentUnBlur",
                                    "osPrefsVis_ContentBlur")), Storyboard)
    End Function

    Public Sub GenVis_ShowLoadOverlay()
        Dim objVisShowLoad = ComposeShowContentVis()
        Dim objVisShowLoadContent = ComposeShowContentVis()

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

                osPrefsContentBrdr.CacheMode = New BitmapCache(1.0)
                osBottomContainer.CacheMode = New BitmapCache(1.0)

                Await Task.Delay(115)
                objLoadSpinDisplayMonitor.SetResult(True)
            End Sub

        AddHandler objVis_ShowLoadSpin.Completed, evtDispComplete
    End Sub

    Public Function GenVis_CloseLoadOverlay() As DoubleAnimation
        Return New DoubleAnimation() With {
            .[To] = 0, .Duration = SetVisDuration(375),
            .EasingFunction = New QuadraticEase() With {
                .EasingMode = EasingMode.EaseIn
            }
        }
    End Function

    Private Sub TerminateVisData(ByRef objVisData As Storyboard)
        With objVisData
            Try
                .Stop()
            Catch ex As Exception : End Try

            If .Children.Count > 0 Then
                .Children.Clear()
            End If
        End With

        objVisData = Nothing
    End Sub

    Private Sub TerminateMonitor(ByRef objMonitor As TaskCompletionSource(Of Boolean))
        objMonitor.ResetTask()
    End Sub

    Public Function GenerateTerminateTasks(typeTerminate As TerminateUiType) As Task
        Dim objTaskSchedule = TaskScheduler.FromCurrentSynchronizationContext()

        Dim idxTasks_DestroyAll = FetchTerminateData(typeTerminate)
        Dim UiData_DestroyAll = idxTasks_DestroyAll.Select(
            Function(objTaskDestroy)
                Dim objTerminateTask = CreateTask(objTaskDestroy)
                objTerminateTask.Start(objTaskSchedule)

                Return objTerminateTask
            End Function)

        Return Task.WhenAll(UiData_DestroyAll)
    End Function

    Public Function FetchTerminateData(typeTerminate As TerminateUiType) As Action()
        Dim idxTerminateTasks As Action()

        Select Case typeTerminate
            Case TerminateUi_Visuals
                idxTerminateTasks = {
                    Sub() TerminateVisData(objVis_ShowLoadSpin), Sub() TerminateVisData(objVis_HideOverlay),
                    Sub() TerminateVisData(objVisLoadMsg_Display), Sub() TerminateVisData(objVisLoadMsg_FadeIn),
                    Sub() TerminateVisData(objVisLoadMsg_FadeOut)
                }
            Case TerminateUi_Monitors
                idxTerminateTasks = {
                    Sub() TerminateMonitor(objLoadSpinMonitor), Sub() TerminateMonitor(objLoadSpinCompleteMonitor),
                    Sub() TerminateMonitor(objLoadSpinDisplayMonitor), Sub() TerminateMonitor(objLoadSpinDispMsgMonitor)
                }
            Case TerminateUi_Objects
                idxTerminateTasks = {
                    Sub() TerminateObject(objLoadSpinContainer), Sub() TerminateObject(osSpinLoadContainer),
                    Sub() TerminateObject(objLoadSpinContent), Sub() TerminateObject(osSpinLoadMsg),
                    Sub() TerminateObject(osSpinLoadMsgText), Sub() TerminateObject(objLoadSpinner)
                }
        End Select

        Return idxTerminateTasks
    End Function

    Private Sub TerminateObject(objUI As FrameworkElement)
        Select Case True
            Case TypeOf objUI Is Border
                Select Case objUI.Name
                    Case "objLoadSpinContainer"
                        objLoadSpinContainer = Nothing
                    Case "osSpinLoadMsgText"
                        osSpinLoadMsgText = Nothing
                End Select
            Case TypeOf objUI Is Grid
                osSpinLoadContainer.Children.Clear()
                osSpinLoadContainer = Nothing
            Case TypeOf objUI Is StackPanel
                objLoadSpinContent = Nothing
            Case TypeOf objUI Is TextBlock
                osSpinLoadMsg = Nothing
            Case TypeOf objUI Is osControls.osLoadSpinner
                objLoadSpinner = Nothing
        End Select
    End Sub

    Public Function GetUiObjects() As List(Of FrameworkElement)
        Return New List(Of FrameworkElement) From {
            osSpinLoadContainer, objLoadSpinContainer,
            objLoadSpinContent, objLoadSpinner,
            osSpinLoadMsg, osSpinLoadMsgText
        }
    End Function

    Private Async Function RecapMemory() As Task
        Await Task.Delay(65)

        RecaptureResources()
        Await Task.Delay(175)
    End Function

    Private Async Function DisposeLoadSpinner() As Task
        objTerminateMonitor.ResetAndInitTask()

        objTask_InitSpin.SafeDispose()

        Await PrepDispatcher().InvokeAsync(
           Async Function()
               Await Task.WhenAll(GenerateTerminateTasks(TerminateUi_Visuals),
                                  GenerateTerminateTasks(TerminateUi_Monitors),
                                  GenerateTerminateTasks(TerminateUi_Objects))

               Await RecapMemory()
               objTerminateMonitor.SetResult(True)
           End Function, DispatcherPriority.SystemIdle)

        Await objTerminateMonitor.Task
        objTerminateMonitor.ResetTask()

        InitLoadSpinner()
    End Function

    Public Function ComposeLoadCompleteVis() As DoubleAnimation
        Return New DoubleAnimation() With {
            .From = 20, .To = 100, .FillBehavior = FillBehavior.HoldEnd,
            .Duration = SetVisDuration(1025),
            .EasingFunction = New ExponentialEase With {
                .EasingMode = EasingMode.EaseInOut,
                .Exponent = 2.75
            }
        }
    End Function

    Private Function ComposeLoadCompleteColorVis() As ColorAnimation
        Return New ColorAnimation(
            GenerateLoadSpinBrush(LoadColor_SpinnerLoadComplete).Color, SetVisDuration(1025)) With {
                .FillBehavior = FillBehavior.HoldEnd,
                .EasingFunction = New QuadraticEase With {
                    .EasingMode = EasingMode.EaseInOut
                }
            }
    End Function

    Public Async Function TriggerLoadComplete(isN As Boolean) As Task
        Await UpdateLoadSpinMsg()
        Await objLoadSpinner.TriggerLoadSpinComplete()

        Await HideOverlay()
    End Function

    Public Async Function TriggerLoadComplete() As Task
        Dim objLoadSpinBrush = objLoadSpinner.GetSpinnerBrush()

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
        Await objLoadSpinner.LoadSmoothMonitor.Task

        objVis_CloseLoadOverlay.Begin(objLoadSpinner)
    End Function

    Private Sub ComposeHideOverlayVis()
        Dim visFadeOut = GenVis_CloseLoadOverlay()

        objVis_HideOverlay = New Storyboard()
        objVis_HideOverlay.Children.Add(visFadeOut)

        Storyboard.SetTarget(visFadeOut, osSpinLoadContainer)
        Storyboard.SetTargetProperty(visFadeOut, GetPropPath(True))

        Dim evtVisHideOverlay As EventHandler = Nothing
        evtVisHideOverlay =
          Async Sub()
              RemoveHandler objVis_HideOverlay.Completed, evtVisHideOverlay

              objLoadSpinContainer.Visibility = Visibility.Collapsed
              osPrefsContentContainer.Children.Remove(osSpinLoadContainer)

              Await DisposeLoadSpinner()
          End Sub

        AddHandler objVis_HideOverlay.Completed, evtVisHideOverlay

        osPrefsContentBrdr.CacheMode = Nothing
        osBottomContainer.CacheMode = Nothing
    End Sub

    Private Sub HideOverlay_UI()
        If objLoadSpinContainer Is Nothing Then Return
        ComposeHideOverlayVis()

        GetVisual_ContentBlur(True).Begin()
        objVis_HideOverlay.Begin(osSpinLoadContainer)

        SetSaveState(PrefSaveState.Prefs_Saved)
        InputUI_Allow()
    End Sub

    Private objTask_LoadSpinComplete As TaskCompletionSource(Of Boolean)

    Public Async Function DisplayLoadTask(taskReload As Func(Of Task)) As Task
        Await ComposeLoadOverlay()

        objTask_LoadSpinComplete.ResetAndInitTask()
        Await PrepDispatcher().InvokeAsync(
            Async Function()
                Await Task.Delay(115)
                Await taskReload()
            End Function, DispatcherPriority.SystemIdle)

        Await objTask_LoadSpinComplete.Task
        Await TriggerLoadComplete(True)
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

    Public Function GenTaskDuration(isShowDuration As Boolean) As Double
        Dim rndTaskDur As New Random()

        Dim valMin = TaskDur_Min / 2
        Dim valMax = TaskDur_Max / 2

        Return (valMin) + (rndTaskDur.
            NextDouble() * (valMax - valMin))
    End Function

    Public Async Function DestoryAll() As Task
        'Dim idxTasks_DestroyAll As Task() = {
        '    DestroyUI(osPopupMenu), DestroyUI(osPopupMenuOverlay),
        '    DestroyUI(osTrayMenu), DestroyUI(osGui_AutoPass),
        '    osHandler_AutoCast.StopAutoCastAsync()
        '}

        Await PrepDispatcher().InvokeAsync(
            Async Function()
                Await Task.WhenAll(
                    DestroyUI(osPopupMenu), DestroyUI(osPopupMenuOverlay),
                    DestroyUI(osTrayMenu), DestroyUI(osGui_AutoPass),
                    osHandler_AutoCast.StopAutoCastAsync())
            End Function, DispatcherPriority.Background)
    End Function

    Private Sub TerminateUI(uiWin As Window)
        Select Case True
            Case TypeOf uiWin Is osPopupMenu_GUI
                _osPopupMenu = Nothing
            Case TypeOf uiWin Is osPopupMenuOverlay_GUI
                _osPopupMenuOverlay = Nothing
            Case TypeOf uiWin Is osTrayMenu_GUI
                _osTrayMenu = Nothing
            Case TypeOf uiWin Is progUI_AutoPass
                _autoPass = Nothing
        End Select
    End Sub

    Private Async Function DestroyUI(uiWin As Window) As Task
        If uiWin Is Nothing Then Return

        Await uiWin.Dispatcher.InvokeAsync(
            Sub()
                With uiWin
                    .DataContext = Nothing
                    .Content = Nothing

                    .Close()
                End With

                TerminateUI(uiWin)
            End Sub, DispatcherPriority.Background)
    End Function

    Public Async Function ReloadData_Prefs() As Task
        Await osPrefData.Data.ClearPrefData()

        Await osPrefData.Data.PreparePrefData()
        Await osPrefData.Data.ApplyPrefs()
    End Function

    Public Async Function ReloadUI_PopupMenu() As Task
        _osPopupMenuOverlay = New osPopupMenuOverlay_GUI
        _osPopupMenu = New osPopupMenu_GUI

        osPopupMenu.PrepPopupMenu()

        Await osPopupMenuOverlay.PrepPopupMenuOverlay()
        Await osPopupMenu.InitPopupMenuVis()
    End Function

    Public Async Function ReloadUI_TrayMenu() As Task
        _osTrayMenu = New osTrayMenu_GUI
        Await osTrayMenu.PrepTrayMenuInit()
    End Function

    Public Async Function ReloadUI_AutoCast() As Task
        Await osHandler_AutoCast.StartAutoCastAsync()
        Await Task.Delay(GenTaskDuration(True))
    End Function

    Public Function ReloadUI_AutoPass() As Task
        osHandler_UI._autoPass = osHandler_UI.PrepUI_AutoPass()
        osHandler_UI.osGui_AutoPass.PrepAutoPass()

        Return Task.CompletedTask
    End Function

    Private Async Function ReloadPrefs() As Task
        Await osPrefDataIdx.SavePrefsFileAsync()
        Await ReloadData_Prefs()

        Await PrepDispatcher().InvokeAsync(
            Async Function()
                Await DestoryAll()

                Await Task.Delay(GenTaskDuration())

                Await ReloadUI_PopupMenu()
                Await ReloadUI_TrayMenu()

                Await Task.Delay(GenTaskDuration(True))

                Await ReloadUI_AutoPass()
                Await ReloadUI_AutoCast()
            End Function, DispatcherPriority.SystemIdle).Task.Unwrap()

        objTask_LoadSpinComplete.SetResult(True)

        ActivatePrefTracker(True)
        RefreshVisQuality()
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
                Select Case GetResponse(PromptType.Prefs_Close)
                    Case isYes
                        Await objTask_InitSpin
                        Await TriggerPrefSave(True)
                    Case isNo
                        RevertPrefSettings()
                    Case isCancel
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

    Private objTerminateMonitor As TaskCompletionSource(Of Boolean) = Nothing
    Private objLoadSpinMonitor As TaskCompletionSource(Of Boolean)
    Private objLoadSpinCompleteMonitor As TaskCompletionSource(Of Boolean)
    Private objLoadSpinDisplayMonitor As TaskCompletionSource(Of Boolean)
    Private objLoadSpinDispMsgMonitor As TaskCompletionSource(Of Boolean)

    Private objLoadSpinContainer As Border
    Private osSpinLoadContainer As Grid
    Private objLoadSpinContent As StackPanel
    Private objLoadSpinner As osControls.osLoadSpinner
    Private osSpinLoadMsg As TextBlock
    Private osSpinLoadMsgText As Border

    Private objTask_InitSpin As Task(Of Boolean)
    Private objVis_ShowLoadSpinContent As Storyboard = Nothing
    Private objVis_ShowLoadSpin As Storyboard = Nothing
    Private objVis_HideOverlay As Storyboard = Nothing

    Private objVisLoadMsg_Display As Storyboard = Nothing

    Private objVisLoadMsg_FadeIn As Storyboard = Nothing
    Private objVisLoadMsg_FadeOut As Storyboard = Nothing


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

    Public ReadOnly Property osBottomContent As Grid
        Get
            Return Me.BottomContent
        End Get
    End Property

    Public ReadOnly Property osBottomContainer As Border
        Get
            Return Me.BottomContainer
        End Get
    End Property

    Public ReadOnly Property osBottomContentContainer As Border
        Get
            Return Me.BottomContentContainer
        End Get
    End Property

    Public ReadOnly Property osPrefsContentContainer As Grid
        Get
            Return Me.osPrefsContentContain
        End Get
    End Property

    Public ReadOnly Property osPrefsContentBrdr As Border
        Get
            Return Me.BottomContentContainer
        End Get
    End Property

    Public ReadOnly Property osTitleCover As Border
        Get
            Return Me.osPrefsTitlePanel
        End Get
    End Property

    Private ReadOnly Property osPrefDataIdx As osPrefData.osPrefIndex
        Get
            Return osPrefData.Data.objOsPrefIdx
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

    Private Function SetVisEasing() As IEasingFunction
        Return New QuadraticEase With {
            .EasingMode = EasingMode.EaseIn
        }
    End Function

    Private Function SetColor(strColor As String) As osColor
        Return CType(ColorConverter.ConvertFromString(strColor), osColor)
    End Function

    Private Function CalcRGB(vR As Byte, vG As Byte, vB As Byte, Optional vA As Byte = 255) As osColor
        Return Color.FromArgb(vA, vR, vG, vB)
    End Function

    Private Function GenBrushColor(vR As Byte, vG As Byte, vB As Byte, Optional vA As Byte = 255) As SolidColorBrush
        Return New SolidColorBrush(Color.FromArgb(vA, vR, vG, vB)).FreezeReturn()
    End Function

    Private Function GenColor(isRGB As Boolean, vR As Byte, vG As Byte, vB As Byte, Optional vA As Byte = 255) As osColor
        Return Color.FromArgb(vA, vR, vG, vB)
    End Function

    Private Function GetPropPath(isGrid As Boolean) As PropertyPath
        If isGrid Then
            Return New PropertyPath(Grid.OpacityProperty)
        Else
            Return New PropertyPath(Border.OpacityProperty)
        End If
    End Function

    Private Function GenerateLoadSpinColor(objLoadSpinColors As LoadSpinColors) As osColor
        Select Case objLoadSpinColors
            Case LoadColor_MsgTextGlow
                Return GenColor(True, 255, 255, 255, 105)
            Case LoadColor_MsgTextStroke
                Return GenColor(True, 255, 255, 255, 80)
            Case LoadColor_SpinContainerBorderGlow
                Return GenColor(True, 181, 181, 181, 118)
        End Select
    End Function

    Private Function GenerateLoadSpinBrush(objLoadSpinColors As LoadSpinColors, Optional noFreeze As Boolean = False) As SolidColorBrush
        Dim objOut_Color As SolidColorBrush

        Select Case objLoadSpinColors
            Case LoadColor_Spinner
                objOut_Color = GenBrushColor(80, 15, 15)
            Case LoadColor_SpinContainer
                objOut_Color = GenBrushColor(26, 26, 26, 240)
            Case LoadColor_Container
                objOut_Color = GenBrushColor(0, 0, 0, 138)
            Case LoadColor_SpinContainerBorder
                objOut_Color = GenBrushColor(13, 13, 13)
            Case LoadColor_SpinnerLoadComplete
                objOut_Color = GenBrushColor(57, 128, 57)
            Case LoadColor_MsgText
                objOut_Color = GenBrushColor(0, 0, 0)
            Case LoadColor_SpinContentContainer
                objOut_Color = GenBrushColor(26, 26, 26, 240)
        End Select

        Return If(noFreeze, objOut_Color.Clone(),
            objOut_Color.Clone().FreezeReturn())
    End Function

    Private Sub YieldVisuals() : End Sub

    Private visPriority As DispatcherPriority =
        DispatcherPriority.Render

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer,
                                         Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub OnPropertyChanged(Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

End Class
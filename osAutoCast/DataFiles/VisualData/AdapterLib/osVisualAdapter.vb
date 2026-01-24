Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.VisAdapterConfig

Namespace osVisualAdapter

    Public Class VisAdapterUI
        Inherits Window
        Implements INotifyPropertyChanged

        Public Event LifecycleSignal As EventHandler

        Protected Sub RaiseLifecycleSignal()
            RaiseEvent LifecycleSignal(Me, EventArgs.Empty)
        End Sub

        Public Event VisOutlineUpdated As EventHandler(Of VisOutlineUpdatedEventArgs)

        Private _visDataObject As Storyboard
        Public Overridable Property VisDataObject As Storyboard
            Get
                Return _visDataObject
            End Get
            Set(value As Storyboard)
                If ReferenceEquals(_visDataObject, value) Then Return
                _visDataObject = If(value Is Nothing, Nothing, value.Clone())
                OnPropertyChanged(NameOf(VisDataObject))
            End Set
        End Property

        Public Sub PrepVisAdapter(objWinUI As VisAdapterUI)

        End Sub

        Public Function TriggerVisuals_Open(doAsync As Boolean, startVis As Boolean) As Task
            VisAdapter.ApplyVisualReset()

            If doAsync Then
                Dim objTask_SetVisuals = VisAdapter.ApplyVisuals(True)
            Else
                Dim objTask_SetVisuals = VisAdapter.ApplyVisuals()
            End If

            VisDataObject.Begin(VisAdapter.GetStartingVis(), True)

            Return Task.CompletedTask
        End Function

        'Public Overridable Function TriggerVisuals_Close(Optional doAsync As Boolean = True) As Task
        '    If doAsync Then
        '        Dim objTask_SetVisuals = VisAdapter.ApplyVisuals(True)
        '    Else
        '        Dim objTask_SetVisuals = VisAdapter.ApplyVisuals()
        '    End If

        '    VisDataObject.Begin(VisAdapter.GetStartingVis(), True)
        '    Return Task.CompletedTask
        'End Function

        Public Overridable Function TriggerVisuals_Open(Optional doAsync As Boolean = True, Optional objAwaitClose As TaskCompletionSource(Of Boolean) = Nothing) As Task
            VisAdapter.ApplyVisualReset()

            If doAsync Then
                Dim objTask_SetVisuals = VisAdapter.ApplyVisuals(True)
            Else
                Dim objTask_SetVisuals = VisAdapter.ApplyVisuals()
            End If

            VisDataObject.Begin(VisAdapter.GetStartingVis(), True)

            Return Task.CompletedTask
        End Function

        Public Overridable Async Function TriggerVisuals_Close(Optional doAsync As Boolean = True) As Task
            If doAsync Then
                Await VisAdapter.ApplyVisuals(True)
            Else
                Dim objTask_SetVisuals = VisAdapter.ApplyVisuals()
            End If

            VisDataObject.Begin(VisAdapter.GetStartingVis(), True)
            Await Task.CompletedTask
        End Function

        Private _visAdapter As VisQualityAdapter
        Public Property VisAdapter As VisQualityAdapter
            Get
                Return _visAdapter
            End Get
            Set(value As VisQualityAdapter)
                _visAdapter = If(value Is Nothing, Nothing, value)
            End Set
        End Property

        Protected Sub OnVisOutlineUpdated(newSb As Storyboard)
            RaiseEvent VisOutlineUpdated(Me, New VisOutlineUpdatedEventArgs(newSb))
        End Sub

        Public Class VisOutlineUpdatedEventArgs
            Inherits EventArgs

            Public ReadOnly Property Storyboard As Storyboard

            Public Sub New(sb As Storyboard)
                Storyboard = sb
            End Sub
        End Class

        Public Event PropertyChanged As PropertyChangedEventHandler _
            Implements INotifyPropertyChanged.PropertyChanged

        Protected Overridable Sub OnPropertyChanged(propName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
        End Sub

    End Class

    Public Class VisQualityAdapter

        Private evtVisComplete As EventHandler = AddressOf ResetVisuals

        Private Const renderAtScale As Double = 1.0

        Private VisAdapterConfiguration As VisAdapterConfig
        Private VisAdapterConfigIdx As Dictionary(Of VisConfigItem, Boolean)

        Private VisConfigDataList As New List(Of VisConfigItem) From {
            VisConfigItem.ApplyVisuals,
            VisConfigItem.ObjectReset,
            VisConfigItem.UpdateAsync_OnLoad,
            VisConfigItem.UpdateAsync_OnReset,
            VisConfigItem.UpdateAsync_OnDispose
        }

        Private VisCloseTask As Func(Of Task)
        Private VisCloseTrigger As Action

        Private VisObjectIdx As UIElement()
        Private VisObjectCnt As Integer

        Private ReadOnly VisAdapter_UI As VisAdapterUI
        Private ReadOnly VisAdapter_Dispatch As Dispatcher

        Private VisDataOutline As Storyboard

        Private ReadOnly Property VisOutline As Storyboard
            Get
                Return VisAdapter_UI.VisDataObject
            End Get
        End Property

        Public Sub New()
        End Sub

        'Public Sub New(objVisAdapterUI As VisAdapterUI, setVisConfig As VisAdapterConfig, ParamArray lstVisTargets() As UIElement)
        '    VisObjectIdx = lstVisTargets
        '    VisObjectCnt = VisObjectIdx.Count - 1

        '    VisAdapter_UI = objVisAdapterUI
        '    VisAdapter_Dispatch = objVisAdapterUI.Dispatcher

        '    VisAdapterConfiguration = setVisConfig
        '    ProcessVisConfig()

        '    If ValidateConfig(VisAdapterConfig.ObjectReset) Then
        '        ApplyVisualReset()
        '    End If

        '    If ValidateConfig(VisAdapterConfig.ApplyVisuals) Then
        '        If ValidateConfig(VisAdapterConfig.UpdateAsync_OnLoad) Then
        '            Dim objTask_SetVisuals = ApplyVisuals(True)
        '        Else
        '            Dim objTask_SetVisuals = ApplyVisuals()
        '        End If
        '    End If
        'End Sub

        Public Sub New(objVisAdapterUI As VisAdapterUI, setVisConfig As VisAdapterConfig, ParamArray lstVisTargets() As UIElement)
            VisObjectIdx = lstVisTargets
            VisObjectCnt = VisObjectIdx.Count - 1

            VisAdapter_UI = objVisAdapterUI
            VisAdapter_Dispatch = objVisAdapterUI.Dispatcher

            ' VisCloseTask = objCloseTask
            'AddHandler VisAdapter_UI.LifecycleSignal, Sub(s, e)
            '                                              Dim aa = OnLifecycleSignal()
            '                                          End Sub

            VisAdapterConfiguration = setVisConfig
            ProcessVisConfig()


            'If FetchConfig(VisAdapterConfig.ApplyVisuals) Then
            '    Dim aa = InitVisualAdapter()
            'End If

            'If FetchConfig(VisAdapterConfig.ObjectReset) Then
            '    ApplyVisualReset()
            'End If

            'If FetchConfig(VisAdapterConfig.ApplyVisuals) Then
            '    If FetchConfig(VisAdapterConfig.UpdateAsync_OnLoad) Then
            '        Dim objTask_SetVisuals = ApplyVisuals(True)
            '    Else
            '        Dim objTask_SetVisuals = ApplyVisuals()
            '    End If
            'End If
        End Sub

        Public Async Function InitVisualAdapter() As Task
            If FetchConfig(VisAdapterConfig.ObjectReset) Then
                ApplyVisualReset()
            End If

            If FetchConfig(VisAdapterConfig.ApplyVisuals) Then
                If FetchConfig(VisAdapterConfig.UpdateAsync_OnLoad) Then
                    Await ApplyVisuals(True)
                Else
                    Dim objTask_SetVisuals = ApplyVisuals()
                End If
            End If
        End Function

        Public Sub New(objVisAdapterUI As VisAdapterUI, setVisConfig As VisAdapterConfig, objCloseTrigger As Action, isTrigger As Boolean, ParamArray lstVisTargets() As UIElement)
            VisObjectIdx = lstVisTargets
            VisObjectCnt = VisObjectIdx.Count - 1

            VisAdapter_UI = objVisAdapterUI
            VisAdapter_Dispatch = objVisAdapterUI.Dispatcher

            VisCloseTrigger = objCloseTrigger
            AddHandler VisAdapter_UI.LifecycleSignal, Sub(s, e)
                                                          Dim aa = OnLifecycleSignal()
                                                      End Sub


            VisAdapterConfiguration = setVisConfig
            ProcessVisConfig()

            If FetchConfig(VisAdapterConfig.ObjectReset) Then
                ApplyVisualReset()
            End If

            If FetchConfig(VisAdapterConfig.ApplyVisuals) Then
                If FetchConfig(VisAdapterConfig.UpdateAsync_OnLoad) Then
                    Dim objTask_SetVisuals = ApplyVisuals(True)
                Else
                    Dim objTask_SetVisuals = ApplyVisuals()
                End If
            End If
        End Sub

        Private Async Function OnLifecycleSignal() As Task
            If VisCloseTask Is Nothing Then
                If FetchConfig(VisAdapterConfig.UpdateAsync_OnDispose) Then
                    Dim objDisposeTask_ApplyVis = ApplyVisuals(True)
                    Dim objDisposeTask_TriggerEvent = TriggerVisClose(VisCloseTrigger)

                    Await objDisposeTask_TriggerEvent()
                Else
                    Dim objDisposeTask_ApplyVis = ApplyVisuals()
                    Dim objDisposeTask_TriggerEvent = TriggerVisClose(VisCloseTrigger)()
                End If
            Else
                If FetchConfig(VisAdapterConfig.UpdateAsync_OnDispose) Then
                    Dim objDisposeTask_ApplyVis = ApplyVisuals(True)
                    Await VisCloseTask()
                Else
                    Dim objDisposeTask_ApplyVis = ApplyVisuals()
                    Dim objDisposeTask_TriggerEvent = VisCloseTask()
                End If
            End If
        End Function

        Public Sub SetConfiguration(setVisConfig As VisAdapterConfig)
            VisAdapterConfiguration = setVisConfig
        End Sub

        Public Function TriggerVisClose(action As Action) As Func(Of Task)
            Return Function()
                       action()
                       Return Task.CompletedTask
                   End Function
        End Function

        Public Function GetStartingVis() As FrameworkElement
            Return CType(VisObjectIdx(0), FrameworkElement)
        End Function

        Private Sub OnVisOutlineUpdate(sender As Object, e As PropertyChangedEventArgs)
            If e.PropertyName <> NameOf(VisAdapterUI.VisDataObject) Then Return

            If VisDataOutline IsNot Nothing Then
                If FetchConfig(VisConfigItem.UpdateAsync_OnReset) Then
                    RemoveHandler VisDataOutline.Completed, AddressOf ResetVisuals_Async
                Else
                    RemoveHandler VisDataOutline.Completed, AddressOf ResetVisuals
                End If
            End If

            Try
                If VisAdapter_UI.VisDataObject IsNot Nothing Then
                    VisDataOutline = VisAdapter_UI.VisDataObject
                Else
                    VisDataOutline = Nothing
                End If
            Catch ex As Exception
                VisDataOutline = Nothing
            End Try

            If VisDataOutline IsNot Nothing Then
                If FetchConfig(VisConfigItem.UpdateAsync_OnReset) Then
                    AddHandler VisDataOutline.Completed, AddressOf ResetVisuals_Async
                Else
                    AddHandler VisDataOutline.Completed, AddressOf ResetVisuals
                End If
            End If
        End Sub

        Private Sub ProcessVisConfig()
            VisAdapterConfigIdx = New Dictionary(Of VisConfigItem, Boolean)

            For Each visConfigItem In VisConfigDataList
                VisAdapterConfigIdx.Add(visConfigItem, ValidateConfig(visConfigItem))
            Next
        End Sub

        Private Function FetchConfig(objVisConfigItem As VisConfigItem) As Boolean
            Return VisAdapterConfigIdx.First(
                Function(objVisConfigData)
                    Return objVisConfigData.Key = objVisConfigItem
                End Function).Value
        End Function

        Private Sub SetConfigDefaults()
            VisAdapterConfiguration = VisAdapterConfig.ApplyVisuals Or VisAdapterConfig.UpdateAsync_OnLoad Or
                VisAdapterConfig.ObjectReset

        End Sub

        Private Sub ApplyVisConfig(Optional chkReset As Boolean = False)
            If chkReset Then
                If FetchConfig(VisConfigItem.ObjectReset) Then
                    ApplyVisualReset()
                End If
            End If

            If FetchConfig(VisConfigItem.ApplyVisuals) Then
                If FetchConfig(VisConfigItem.UpdateAsync_OnLoad) Then
                    Dim objTask_SetVisuals = ApplyVisuals(True)
                Else
                    Dim objTask_SetVisuals = ApplyVisuals()
                End If
            End If
        End Sub

        Private Function ValidateConfig(chkVisConfig As VisAdapterConfig) As Boolean
            Return (VisAdapterConfiguration And chkVisConfig) <> 0
        End Function

        Private Function isVisPerformance() As Boolean
            Return CoreDataLib.GetVisualQuality() = ProgVisOpts.Performance
        End Function

        Public Function ApplyVisuals() As Task
            If isVisPerformance() Then
                Dim objTask_SetVisConfig = PrepDispatcher().InvokeAsync(
                    Sub()
                        Dim visBitMapCache As New BitmapCache(1.0)

                        For objVis = 0 To VisObjectCnt
                            SetVisQuality(VisObjectIdx(objVis), visBitMapCache)
                        Next
                    End Sub, DispatcherPriority.Background).Task
            End If

            Return Task.CompletedTask
        End Function

        Public Async Function ApplyVisuals(isAsync As Boolean) As Task
            If isVisPerformance() Then
                Await PrepDispatcher().InvokeAsync(
                    Sub()
                        Dim visBitMapCache As New BitmapCache(1.0)

                        For objVis = 0 To VisObjectCnt
                            SetVisQuality(VisObjectIdx(objVis), visBitMapCache)
                        Next
                    End Sub, DispatcherPriority.Render).Task
            End If
        End Function

        Public Sub ApplyVisualReset()
            If isVisPerformance() Then
                If FetchConfig(VisAdapterConfig.ObjectReset) Then
                    PropertyChangedEventManager.AddHandler(VisAdapter_UI,
                                                       AddressOf OnVisOutlineUpdate,
                                                       NameOf(VisAdapterUI.VisDataObject))

                    OnVisOutlineUpdate(VisAdapter_UI,
                                   New PropertyChangedEventArgs(NameOf(
                                   VisAdapterUI.VisDataObject)))
                End If
            End If
        End Sub

        Private Sub ResetVisuals(sender As Object, e As EventArgs)
            '    RemoveHandler VisDataOutline.Completed, evtVisComplete

            Dim objTask_VisComplete = PrepDispatcher().InvokeAsync(
               Sub()
                   For objVis = 0 To VisObjectCnt
                       ResetVisQuality(VisObjectIdx(objVis))
                   Next
               End Sub, DispatcherPriority.Background)
        End Sub

        Private Async Sub ResetVisuals_Async(sender As Object, e As EventArgs)
            '    RemoveHandler VisDataOutline.Completed, evtVisComplete
            Await PrepDispatcher().InvokeAsync(
               Sub()
                   For objVis = 0 To VisObjectCnt
                       ResetVisQuality(VisObjectIdx(objVis))
                   Next
               End Sub, DispatcherPriority.Background)
        End Sub

        Public Sub SetVisQuality(objVisTarget As UIElement, objBitMapCache As CacheMode)
            RenderOptions.SetBitmapScalingMode(objVisTarget, BitmapScalingMode.LowQuality)
            RenderOptions.SetEdgeMode(objVisTarget, EdgeMode.Aliased)

            TextOptions.SetTextRenderingMode(objVisTarget, TextRenderingMode.Aliased)
            TextOptions.SetTextFormattingMode(objVisTarget, TextFormattingMode.Display)

            objVisTarget.CacheMode = objBitMapCache

            Dim objVisElement = TryCast(objVisTarget, FrameworkElement)

            If objVisElement IsNot Nothing Then
                objVisElement.UseLayoutRounding = True
                objVisElement.SnapsToDevicePixels = True
            End If
        End Sub

        Public Sub ResetVisQuality(objVis As UIElement)
            RenderOptions.SetBitmapScalingMode(objVis, BitmapScalingMode.HighQuality)
            RenderOptions.SetEdgeMode(objVis, EdgeMode.Unspecified)

            TextOptions.SetTextRenderingMode(objVis, TextRenderingMode.Auto)
            TextOptions.SetTextFormattingMode(objVis, TextFormattingMode.Ideal)

            objVis.CacheMode = Nothing

            Dim objVisElement = TryCast(objVis, FrameworkElement)

            If objVisElement IsNot Nothing Then
                objVisElement.UseLayoutRounding = False
                objVisElement.SnapsToDevicePixels = False
            End If
        End Sub

        Public Sub Dispose()
            '            RemoveHandler VisAdapter_UI.VisOutlineUpdated, AddressOf VisOutlineUpdated
        End Sub

    End Class

End Namespace

Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.VisAdapterConfig
Imports osNotify = System.ComponentModel.INotifyPropertyChanged

Namespace osVisualAdapter

    Public Class VisAdapterUI
        Inherits Window : Implements osNotify

        Private VisAdapaterInitiated As Boolean = False

        Private VisDataOutlineBuffer As Storyboard
        Private VisDataOutline_onComplete As EventHandler

        Private _visDataObject As Storyboard
        Public Overridable Property VisDataObject As Storyboard
            Get
                Return _visDataObject
            End Get
            Set(objVisObject As Storyboard)
                If ReferenceEquals(_visDataObject, objVisObject) Then
                    Return
                Else
                    _visDataObject = If(
                        objVisObject.Clone(), Nothing)

                    OnVisDataOutlineChanged(NameOf(VisDataObject))
                End If
            End Set
        End Property

        Private _visAdapter As VisQualityAdapter
        Public Property VisAdapter As VisQualityAdapter
            Get
                Return _visAdapter
            End Get
            Set(objVisAdapter As VisQualityAdapter)
                _visAdapter = If(
                    objVisAdapter, Nothing)
            End Set
        End Property

        Public Sub AttachVisOutline(vDataName As String, Optional vDataTask As Action = Nothing)
            VisDataOutlineBuffer = PrepareVisOutline(Me.Resources(vDataName))

            If vDataTask IsNot Nothing Then
                VisDataOutline_onComplete =
                    Sub(s, e)
                        RemoveHandler VisDataObject.Completed, VisDataOutline_onComplete
                        vDataTask.Invoke()
                    End Sub
            End If
        End Sub

        Private Function PrepareVisOutline(visObject As Object) As Storyboard
            Dim objVisData = TryCast(visObject, Storyboard)
            objVisData.FreezeReturn()

            Return objVisData
        End Function

        Public Sub InitializeVisAdapter(objVisAdapterUI As VisAdapterUI,
                                        setVisConfig As VisAdapterConfig, ParamArray lstVisTargets() As UIElement)

            '        If Not VisAdapaterInitiated Then
            VisAdapter = New VisQualityAdapter(objVisAdapterUI, setVisConfig, lstVisTargets)

            Dim objTask_ProcessConfig = VisAdapter.ProcessVisConfig()
            '        VisAdapaterInitiated = True
            '  End If
        End Sub

        Public Overridable Async Function TriggerVisuals_Open() As Task
            Dim isTaskAsync As Boolean
            Dim objTask_SetVisuals = Me.VisAdapter.TriggerApplyVisuals(isTaskAsync)

            '   If isTaskAsync Then
            Await objTask_SetVisuals
            '  End If

            VisDataObject.Begin(Me.VisAdapter.GetStartingVis(), True)
            ' Await Task.CompletedTask

        End Function

        Public Overridable Async Function TriggerVisuals_Close() As Task
            Dim isTaskAsync As Boolean
            Dim objTask_SetVisuals = Me.VisAdapter.TriggerApplyVisuals(True, isTaskAsync)

            'If isTaskAsync Then
            Await objTask_SetVisuals
            'End If

            VisDataObject.Begin(Me.VisAdapter.GetStartingVis(), True)

            '    Await Task.CompletedTask
            'If Not isTaskAsync Then
            '    Await Task.CompletedTask : End If
        End Function

        Public Event VisDataOutlineChanged As _
            PropertyChangedEventHandler Implements osNotify.PropertyChanged

        Protected Overridable Sub OnVisDataOutlineChanged(propName As String)
            RaiseEvent VisDataOutlineChanged(Me, New PropertyChangedEventArgs(propName))
        End Sub

    End Class

    Public Class VisQualityAdapter

        Private ReadOnly VisAdapter_UI As VisAdapterUI

        Private VisObjectIdx As UIElement()
        Private VisObjectCnt As Integer

        Private evtResetVisuals As EventHandler

        Private VisAdapterConfiguration As VisAdapterConfig
        Private VisAdapterConfigIdx As New Dictionary(Of VisAdapterConfig, Boolean) From {
            {None, False},
            {ResetVisualSettings, False},
            {UpdateAsync_OnLoad, False},
            {UpdateAsync_OnReset, False},
            {UpdateAsync_OnDispose, False}
        }

        Private ReadOnly Property VisDataOutline As Storyboard
            Get
                Return VisAdapter_UI.VisDataObject
            End Get
        End Property

        Public Sub New(objVisAdapterUI As VisAdapterUI, setVisConfig As VisAdapterConfig, ParamArray lstVisTargets() As UIElement)
            VisObjectIdx = lstVisTargets
            VisObjectCnt = VisObjectIdx.Count - 1

            VisAdapter_UI = objVisAdapterUI

            VisAdapterConfiguration = setVisConfig
        End Sub

        Private Sub DetermineResetEvent()
            Dim visConfig_AsyncReset = FetchConfig(UpdateAsync_OnReset)

            evtResetVisuals = Nothing

            If visConfig_AsyncReset Then
                evtResetVisuals =
                    Async Sub(sender As Object, e As EventArgs)
                        Await ResetVisuals_Async()
                    End Sub
            Else
                evtResetVisuals =
                    Sub(sender As Object, e As EventArgs)
                        ResetVisuals()
                    End Sub
            End If
        End Sub

        Private Sub OnVisOutlineUpdate(sender As Object, e As PropertyChangedEventArgs)
            If e.PropertyName <> NameOf(VisAdapterUI.VisDataObject) Then Return

            DetermineResetEvent()

            If ValidateVisDataOutline() Then
                SyncVisDataOutline()
            End If
        End Sub

        Private Sub SyncVisDataOutline()
            RemoveHandler VisDataOutline.Completed, evtResetVisuals
            AddHandler VisDataOutline.Completed, evtResetVisuals
        End Sub

        Private Function ValidateVisDataObject() As Boolean
            Return VisAdapter_UI.VisDataObject IsNot Nothing
        End Function

        Private Function ValidateVisDataOutline() As Boolean
            Return VisDataOutline IsNot Nothing
        End Function

        Public Function GetStartingVis() As FrameworkElement
            Return CType(VisObjectIdx(0), FrameworkElement)
        End Function

        Private Function isVisPerformance() As Boolean
            Return CoreDataLib.GetVisualQuality() = ProgVisOpts.Performance
        End Function

        Public Async Function ProcessVisConfig() As Task
            Await PrepDispatcher().InvokeAsync(
                Sub()
                    For Each visConfig In VisAdapterConfigIdx.Keys.ToList()
                        UpdateConfigSetting(visConfig)
                    Next
                End Sub, DispatcherPriority.Background)
        End Function

        Private Sub UpdateConfigSetting(VisConfigSetting As VisAdapterConfig)
            VisAdapterConfigIdx(VisConfigSetting) = ValidateConfig(VisConfigSetting)
        End Sub

        Private Function ValidateConfig(chkVisConfig As VisAdapterConfig) As Boolean
            Return (VisAdapterConfiguration And chkVisConfig) <> 0
        End Function

        Private Function FetchConfig(objVisConfigItem As VisAdapterConfig) As Boolean
            Return VisAdapterConfigIdx.First(
                Function(objVisConfigData)
                    Return objVisConfigData.Key = objVisConfigItem
                End Function).Value
        End Function

        Public Function TriggerApplyVisuals() As Task
            If isVisPerformance() Then
                Return If(FetchConfig(UpdateAsync_OnLoad),
                    ApplyVisuals(True), ApplyVisuals())
            End If
        End Function

        Public Function GetQualityConfig(isClose As Boolean) As Boolean
            Return If(isClose, FetchConfig(UpdateAsync_OnDispose), FetchConfig(UpdateAsync_OnLoad))
        End Function

        Public Function TriggerApplyVisuals(Optional ByRef isAsync As Boolean = False) As Task
            If isVisPerformance() Then
                isAsync = FetchConfig(UpdateAsync_OnLoad)

                Return If(isAsync,
                    ApplyVisuals(True), ApplyVisuals())
            End If
        End Function

        Public Function TriggerApplyVisuals(verifyCloseConfig As Boolean, Optional ByRef isAsync As Boolean = False) As Task
            If isVisPerformance() Then
                isAsync = FetchConfig(UpdateAsync_OnDispose)

                Return If(isAsync,
                    ApplyVisuals(True), ApplyVisuals())
            End If
        End Function

        Public Function ApplyVisuals() As Task
            Dim objTask_ApplyVisuals = PrepDispatcher().InvokeAsync(
                Sub()
                    Dim visBitMapCache As New BitmapCache(1.0)

                    For objVis = 0 To VisObjectCnt
                        SetVisQuality(VisObjectIdx(objVis), visBitMapCache)
                    Next
                End Sub, DispatcherPriority.Render).Task

            Return objTask_ApplyVisuals
        End Function

        Public Async Function ApplyVisuals(isAsync As Boolean) As Task
            Await PrepDispatcher().InvokeAsync(
                Function()
                    Dim visBitMapCache As New BitmapCache(1.0)

                    For objVis = 0 To VisObjectCnt
                        SetVisQuality(VisObjectIdx(objVis), visBitMapCache)
                    Next

                    Return Task.CompletedTask
                End Function, DispatcherPriority.Render)
        End Function

        Public Sub ApplyVisualReset()
            If isVisPerformance() Then
                If FetchConfig(ResetVisualSettings) Then
                    PropertyChangedEventManager.RemoveHandler(VisAdapter_UI,
                                                              AddressOf OnVisOutlineUpdate,
                                                              NameOf(VisAdapterUI.VisDataObject))

                    PropertyChangedEventManager.AddHandler(VisAdapter_UI,
                                                           AddressOf OnVisOutlineUpdate,
                                                           NameOf(VisAdapterUI.VisDataObject))

                    OnVisOutlineUpdate(VisAdapter_UI,
                                   New PropertyChangedEventArgs(NameOf(
                                   VisAdapterUI.VisDataObject)))
                End If
            End If
        End Sub

        Private Sub ResetVisuals()
            Dim objTask_VisComplete = PrepDispatcher().InvokeAsync(
               Sub()
                   For objVis = 0 To VisObjectCnt
                       ResetVisQuality(VisObjectIdx(objVis))
                   Next
               End Sub, DispatcherPriority.Background)
        End Sub

        Private Async Function ResetVisuals_Async() As Task
            Await PrepDispatcher().InvokeAsync(
               Sub()
                   For objVis = 0 To VisObjectCnt
                       ResetVisQuality(VisObjectIdx(objVis))
                   Next
               End Sub, DispatcherPriority.Background)
        End Function

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

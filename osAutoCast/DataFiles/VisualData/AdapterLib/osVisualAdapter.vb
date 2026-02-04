Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.VisAdapterConfig
Imports osNotify = System.ComponentModel.INotifyPropertyChanged

Namespace osVisualAdapter

    Public Class VisAdapterUI
        Inherits Window : Implements osNotify

        Private VisDataOutline_OnComplete As EventHandler
        Private VisDataOutline_SideboardComplete As EventHandler

        Private VisDataOutline_ResetSideboard As EventHandler
        Private VisSideboard_HasReset As Boolean = False

        Private VisQualitySet As Boolean = False
        Private VisQualityDuration As Integer = 0

#Region "Visual Data Outlines"

        ' *** -[ Main Visual Outline ]- ***

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

        ' *** -[ Sideboard Visual Outline ]- ***

        Private _visDataSideboard As Storyboard
        Public Overridable Property VisDataSideboard As Storyboard
            Get
                Return _visDataSideboard
            End Get
            Set(objVisObject As Storyboard)
                If ReferenceEquals(_visDataSideboard, objVisObject) Then
                    Return
                Else
                    _visDataSideboard = If(
                        objVisObject.Clone(), Nothing)
                End If
            End Set
        End Property

#End Region

        Private _visAdapter As VisQualityAdapter
        Public Property VisAdapter As VisQualityAdapter
            Get
                Return _visAdapter
            End Get
            Set(objVisAdapter As VisQualityAdapter)
                If objVisAdapter IsNot Nothing Then
                    _visAdapter = objVisAdapter
                Else

                End If
            End Set
        End Property

        Public ReadOnly Property VisRootObject As FrameworkElement
            Get
                Return Me.VisAdapter.VisRootElement
            End Get
        End Property

        Public Async Function InitializeVisAdapter(visDataName As String, objVisAdapterUI As VisAdapterUI, setVisConfig As VisAdapterConfig,
                                                   postLoadTask As Action, ParamArray lstVisTargets() As UIElement) As Task

            Await InitVisualDataOutline(visDataName)
            PrepareVisAdapter(objVisAdapterUI, setVisConfig, lstVisTargets)

            Await VisAdapter.ProcessVisConfig()
            Await Task.Delay(85)

            If postLoadTask IsNot Nothing Then
                Await PrepDispatcher().InvokeAsync(
                    postLoadTask, DispatcherPriority.Background) : End If
        End Function

        Public Async Function InitializeVisAdapter(visDataName As String, objVisAdapterUI As VisAdapterUI, setVisConfig As VisAdapterConfig,
                                                   postLoadTask As Action, onVisCompleteTask As Action, ParamArray lstVisTargets() As UIElement) As Task

            Await InitVisualDataOutline(visDataName)
            PrepareVisAdapter(objVisAdapterUI, setVisConfig, lstVisTargets)

            Await VisAdapter.ProcessVisConfig()
            Await Task.Delay(85)

            If onVisCompleteTask IsNot Nothing Then

                VisDataOutline_OnComplete =
                    Async Sub()
                        RemoveHandler VisDataObject.Completed, VisDataOutline_OnComplete

                        Await PrepDispatcher().
                            InvokeAsync(onVisCompleteTask,
                                        DispatcherPriority.Background)
                    End Sub

                AddHandler VisDataObject.Completed, VisDataOutline_OnComplete
            End If

            If postLoadTask IsNot Nothing Then
                Await PrepDispatcher().InvokeAsync(
                    postLoadTask, DispatcherPriority.Background) : End If

        End Function

        Public Async Function InitializeVisAdapter(visDataName As String, objVisAdapterUI As VisAdapterUI, setVisConfig As VisAdapterConfig,
                                                   postLoadTask As Action, onVisCompleteTask As Action, onVisCompleteFunc As Func(Of Task), ParamArray lstVisTargets() As UIElement) As Task

            Await InitVisualDataOutline(visDataName)
            PrepareVisAdapter(objVisAdapterUI, setVisConfig, lstVisTargets)

            Await VisAdapter.ProcessVisConfig()
            Await Task.Delay(85)

            If onVisCompleteTask IsNot Nothing Then
                VisDataOutline_OnComplete =
                    Async Sub()
                        RemoveHandler VisDataObject.Completed, VisDataOutline_OnComplete

                        Await PrepDispatcher().
                            InvokeAsync(onVisCompleteTask,
                                        DispatcherPriority.Background)
                    End Sub

                AddHandler VisDataObject.Completed, VisDataOutline_OnComplete
            End If

            If postLoadTask IsNot Nothing Then
                Await PrepDispatcher().InvokeAsync(
                    postLoadTask, DispatcherPriority.Background) : End If

            If onVisCompleteFunc IsNot Nothing Then
                Await onVisCompleteFunc() : End If

        End Function

        Public Async Function InitializeVisAdapter(visDataName As String, objVisAdapterUI As VisAdapterUI,
                                                    setVisConfig As VisAdapterConfig, ParamArray lstVisTargets() As UIElement) As Task

            Await InitVisualDataOutline(visDataName)
            PrepareVisAdapter(objVisAdapterUI, setVisConfig, lstVisTargets)

            Await VisAdapter.ProcessVisConfig()
        End Function

        Private Sub PrepareVisAdapter(objVisAdapterUI As VisAdapterUI,
                                     setVisConfig As VisAdapterConfig, ParamArray lstVisTargets() As UIElement)

            VisQualityDuration = CInt(lstVisTargets.Length * 25)
            VisAdapter = New VisQualityAdapter(objVisAdapterUI, setVisConfig, lstVisTargets)

        End Sub

        Private Async Function InitVisualDataOutline(visDataName As String) As Task
            Dim objVisOutline = Await LoadVisualDataOutline(visDataName)
            VisDataObject = objVisOutline
        End Function

        Private Async Function UpdateVisualDataOutline(visDataName As String) As Task
            Dim objVisOutline = Await LoadVisualDataOutline(visDataName)
            VisDataObject = objVisOutline
        End Function

        Private Async Function UpdateVisualDataOutline2(visDataName As String) As Task
            Dim objVisOutline = Await LoadVisualDataOutline(visDataName)
            VisDataSideboard = objVisOutline
        End Function

        Private Async Function LoadVisualDataOutline(visDataName As String) As Task(Of Storyboard)
            Dim objTask_VisLoadComplete As New TaskCompletionSource(Of DispatcherOperation(Of Storyboard))()
            Dim objTask_VisLoader As New BackgroundWorker()

            AddHandler objTask_VisLoader.DoWork,
                Sub(sender As Object, e As DoWorkEventArgs)
                    Dim objVisDataName = TryCast(e.Argument, String)

                    Dim objVisPrep = PrepDispatcher().InvokeAsync(
                        Function() As Storyboard
                            Return FetchPrefVis(objVisDataName)
                        End Function, DispatcherPriority.Render)

                    e.Result = objVisPrep
                End Sub

            AddHandler objTask_VisLoader.RunWorkerCompleted,
                Sub(sender As Object, e As RunWorkerCompletedEventArgs)
                    objTask_VisLoadComplete.SetResult(DirectCast(e.Result, DispatcherOperation(Of Storyboard)))
                    objTask_VisLoader.Dispose()
                End Sub

            objTask_VisLoader.RunWorkerAsync(visDataName)

            Dim objVis_DataOutline = Await objTask_VisLoadComplete.Task
            Return Await objVis_DataOutline
        End Function

        Public Async Function ApplyCloseVisualData(visDataName As String) As Task
            Dim objVisOutline = Await LoadVisualDataOutline(visDataName)
            VisDataObject = objVisOutline
        End Function

        Public Async Function ApplyCloseVisualData(visDataName As String, Optional closeTask As Action = Nothing,
                                                   Optional evtCloseHandler As EventHandler = Nothing, Optional visApply As Boolean = False) As Task
            Await UpdateVisualDataOutline(visDataName)

            If closeTask IsNot Nothing Then
                VisDataOutline_OnComplete =
                    Async Sub()
                        RemoveHandler VisDataObject.Completed,
                                                                VisDataOutline_OnComplete

                        Await PrepDispatcher().InvokeAsync(closeTask,
                                                           DispatcherPriority.Render)
                    End Sub

                AddHandler VisDataObject.Completed, VisDataOutline_OnComplete
            End If

            If evtCloseHandler IsNot Nothing Then
                AddHandler VisDataObject.Completed, evtCloseHandler
            End If

            If visApply Then
                Dim isTaskAsync As Boolean
                Dim objTask_SetVisuals = VisAdapter.TriggerApplyVisuals(True, isTaskAsync)

                Await objTask_SetVisuals

                If isTaskAsync Then
                    Await Task.Delay(VisQualityDuration)
                End If

                VisQualitySet = True
            End If
        End Function

        Public Sub SetCloseVisualData(visDataName As String)
            Dim objVisOutline = ApplyCloseVisualData(visDataName)
        End Sub

        Public Async Function SetCloseVisualData_WithTask(visDataName As String, closeTask As Action) As Task
            Await ApplyCloseVisualData(visDataName, closeTask:=closeTask)
        End Function

        Public Async Function SetCloseVisualData_WithTask(visDataName As String, closeTask As Action, visApply As Boolean) As Task
            Await ApplyCloseVisualData(visDataName, closeTask:=closeTask)
        End Function

        Public Async Function SetCloseVisualData_WithEvent(visDataName As String, evtCloseHandler As EventHandler) As Task
            Await ApplyCloseVisualData(visDataName, evtCloseHandler:=evtCloseHandler)
        End Function

        Public Async Function SetVisSideboard(visDataName As String, Optional resetOnComplete As Boolean = False) As Task
            Dim objVisOutline = Await LoadVisualDataOutline(visDataName)
            VisDataSideboard = objVisOutline

            VisSideboard_HasReset = resetOnComplete
        End Function

        Public Overridable Async Function TriggerVisuals_Open() As Task
            Dim isTaskAsync As Boolean
            Dim objTask_SetVisuals = VisAdapter.TriggerApplyVisuals(isTaskAsync)

            Await objTask_SetVisuals

            If isTaskAsync Then
                Await Task.Delay(VisQualityDuration)
            End If

            If PrepDispatcher().CheckAccess() Then
                VisDataObject.Begin(VisRootObject, False)
            Else
                Await PrepDispatcher().InvokeAsync(
                 Sub()
                     VisDataObject.Begin(VisRootObject, False)
                 End Sub, DispatcherPriority.Render)
            End If

            '   VisDataObject.Begin(VisRootObject, False)
        End Function

        Public Overridable Async Function TriggerVisuals_Close() As Task
            Dim isTaskAsync As Boolean = False

            If Not VisQualitySet Then
                Dim objTask_SetVisuals = VisAdapter.TriggerApplyVisuals(True, isTaskAsync)
                Await objTask_SetVisuals
            End If

            If isTaskAsync Then
                Await Task.Delay(VisQualityDuration)
            End If

            VisQualitySet = False

            If PrepDispatcher().CheckAccess() Then
                VisDataObject.Begin(VisRootObject, False)
            Else
                Await PrepDispatcher().InvokeAsync(
                 Sub()
                     VisDataObject.Begin(VisRootObject, False)
                 End Sub, DispatcherPriority.Render)
            End If
        End Function

        Public Async Function TriggerVisuals_Sideboard() As Task
            Dim isTaskAsync As Boolean
            Dim objTask_SetVisuals = VisAdapter.TriggerSideboardVisuals(isTaskAsync)

            Await objTask_SetVisuals

            If isTaskAsync Then
                Await Task.Delay(VisQualityDuration)
            End If

            VisDataSideboard.Begin(VisRootObject, False)
        End Function

        Public Async Function TriggerVisuals_Sideboard(setVisOnComplete As String, Optional resetOnComplete As Boolean = False) As Task
            Dim isTaskAsync As Boolean
            Dim objTask_SetVisuals = VisAdapter.TriggerSideboardVisuals(isTaskAsync)

            Await objTask_SetVisuals

            If isTaskAsync Then
                Await Task.Delay(VisQualityDuration)
            End If

            VisDataOutline_SideboardComplete =
                Async Sub()
                    RemoveHandler VisDataSideboard.Completed, VisDataOutline_SideboardComplete

                    If VisSideboard_HasReset Then
                        Await VisAdapter.ResetVisSideboard()

                        VisSideboard_HasReset = False
                    End If

                    Await SetVisSideboard(setVisOnComplete, resetOnComplete)
                End Sub

            AddHandler VisDataSideboard.Completed, VisDataOutline_SideboardComplete

            VisDataSideboard.Begin(VisRootObject, False)
        End Function

        Public Async Function TriggerVisuals_Sideboard(setVisOnComplete As String, visCompleteTask As Action, Optional resetOnComplete As Boolean = False) As Task
            Dim isTaskAsync As Boolean
            Dim objTask_SetVisuals = VisAdapter.TriggerSideboardVisuals(isTaskAsync)

            Await objTask_SetVisuals

            If isTaskAsync Then
                Await Task.Delay(VisQualityDuration)
            End If

            VisDataOutline_SideboardComplete =
                Async Sub()
                    RemoveHandler VisDataSideboard.Completed, VisDataOutline_SideboardComplete

                    If VisSideboard_HasReset Then
                        Await VisAdapter.ResetVisSideboard()

                        VisSideboard_HasReset = False
                    End If

                    visCompleteTask.Invoke()

                    Await SetVisSideboard(setVisOnComplete, resetOnComplete)
                End Sub

            AddHandler VisDataSideboard.Completed, VisDataOutline_SideboardComplete

            VisDataSideboard.Begin(VisRootObject, False)
        End Function

        Private Function ComposeVisData(visObject As Object) As Task(Of Storyboard)
            Return DirectCast(visObject, Task(Of Storyboard))
        End Function

        Private Function AllocVis(visObject As Object) As Storyboard
            Return TryCast(visObject, Storyboard)
        End Function

        Private Function FetchPrefVis(objVisType As String) As Storyboard
            Return AllocVis(Me.Resources(objVisType))
        End Function

        Public Event VisDataOutlineChanged As _
            PropertyChangedEventHandler Implements osNotify.PropertyChanged

        Protected Overridable Sub OnVisDataOutlineChanged(propName As String)
            RaiseEvent VisDataOutlineChanged(Me, New PropertyChangedEventArgs(propName))
        End Sub

    End Class

    Public Class VisQualityAdapter
        Implements IDisposable

        Private _disposed As Boolean = False
        Private disposedValue As Boolean

        Private ReadOnly VisAdapter_UI As VisAdapterUI

        Private VisObjectIdx As UIElement()
        Private VisObjectCnt As Integer

        Private ReadOnly evtResetVisuals As EventHandler

        Public VisRootElement As FrameworkElement

        Private VisAdapterConfiguration As VisAdapterConfig

        Private VisAdapterConfigIdx As New Dictionary(
            Of VisAdapterConfig, Boolean) From {
                {None, False},
                {ResetVisualSettings, False},
                {ModifyLayout, False},
                {UpdateAsync_OnLoad, False},
                {UpdateAsync_OnReset, False},
                {UpdateAsync_OnDispose, False},
                {UpdateAsync_OnSideboard, False}
        }

        Private ReadOnly Property VisDataOutline As Storyboard
            Get
                Return VisAdapter_UI.VisDataObject
            End Get
        End Property

        Public Sub New(objVisAdapterUI As VisAdapterUI, setVisConfig As VisAdapterConfig, ParamArray lstVisTargets() As UIElement)
            VisAdapter_UI = objVisAdapterUI
            VisObjectIdx = lstVisTargets
            VisObjectCnt = VisObjectIdx.Length - 1

            VisAdapterConfiguration = setVisConfig
            VisRootElement = GetStartingVis()

            evtResetVisuals = AddressOf OnVisOutlineCompleted
            AddHandler VisDataOutline.Completed, evtResetVisuals

            PropertyChangedEventManager.AddHandler(VisAdapter_UI,
                                                   AddressOf OnVisOutlineUpdate,
                                                   NameOf(VisAdapterUI.VisDataObject))
        End Sub

        Private Async Sub OnVisOutlineCompleted(sender As Object, e As EventArgs)
            If isVisPerformance() Then
                If FetchConfig(ResetVisualSettings) Then
                    If FetchConfig(UpdateAsync_OnReset) Then
                        Await ResetVisuals_Async()
                    Else
                        ResetVisuals()
                    End If
                End If
            End If
        End Sub

        Public Async Function ResetVisSideboard() As Task
            If isVisPerformance() Then
                If FetchConfig(UpdateAsync_OnReset) Then
                    Await ResetVisuals_Async()
                Else
                    ResetVisuals()
                End If
            Else
                Await Task.CompletedTask
            End If
        End Function

        Private Sub OnVisOutlineUpdate(sender As Object, e As PropertyChangedEventArgs)
            If e.PropertyName <> NameOf(VisAdapterUI.VisDataObject) Then Return
            If ValidateVisDataOutline() Then SyncVisDataOutline()
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

        Private Function isVisQuality() As Boolean
            Return Not CoreDataLib.GetVisualQuality() = ProgVisOpts.Performance
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
            Else
                Return Task.CompletedTask
            End If
        End Function

        Public Function CanSkipLayout() As Boolean
            Dim chkModifyLayout = FetchConfig(ModifyLayout)
            Return Not chkModifyLayout
        End Function

        Public Function CanModifyLayout() As Boolean
            Dim chkModifyLayout = FetchConfig(ModifyLayout)
            Return chkModifyLayout
        End Function

        Public Function GetQualityConfig(isClose As Boolean) As Boolean
            Return If(isClose, FetchConfig(UpdateAsync_OnDispose), FetchConfig(UpdateAsync_OnLoad))
        End Function

        Public Function TriggerApplyVisuals(Optional ByRef isAsync As Boolean = False) As Task
            If isVisPerformance() Then
                isAsync = FetchConfig(UpdateAsync_OnLoad)

                Return If(isAsync,
                    ApplyVisuals(True), ApplyVisuals())
            Else
                Return Task.CompletedTask
            End If
        End Function

        Public Function TriggerApplyVisuals(verifyCloseConfig As Boolean, Optional ByRef isAsync As Boolean = False) As Task
            If isVisPerformance() Then
                isAsync = FetchConfig(UpdateAsync_OnDispose)

                Return If(isAsync,
                    ApplyVisuals(True), ApplyVisuals())
            Else
                Return Task.CompletedTask
            End If
        End Function

        Public Function TriggerSideboardVisuals(Optional ByRef isAsync As Boolean = False) As Task
            If isVisPerformance() Then
                isAsync = FetchConfig(UpdateAsync_OnSideboard)

                Return If(isAsync,
                    ApplyVisuals(True), ApplyVisuals())
            Else
                Return Task.CompletedTask
            End If
        End Function

        Public Function ApplyVisuals() As Task
            Dim chkModifyLayout = CanModifyLayout()
            Dim visBitMapCache As New BitmapCache(1.0)

            Dim objTask_ApplyVisuals = ValidateDispatch(
                Sub()
                    For objVis = 0 To VisObjectCnt
                        SetVisQuality(VisObjectIdx(objVis), visBitMapCache, chkModifyLayout)
                    Next
                End Sub)

            Return objTask_ApplyVisuals
        End Function

        Public Async Function ApplyVisuals(isAsync As Boolean) As Task
            Dim chkModifyLayout = CanModifyLayout()
            Dim visBitMapCache As New BitmapCache(1.0)

            Await ValidateDispatch(
                Sub()
                    For objVis = 0 To VisObjectCnt
                        SetVisQuality(VisObjectIdx(objVis), visBitMapCache, chkModifyLayout)
                    Next
                End Sub)
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
            Dim chkModifyLayout = CanModifyLayout()

            Dim objTask_ApplyVisuals = ValidateDispatch(
                Sub()
                    For objVis = 0 To VisObjectCnt
                        ResetVisQuality(VisObjectIdx(objVis), chkModifyLayout)
                    Next
                End Sub)
        End Sub

        Private Async Function ResetVisuals_Async() As Task
            Dim chkModifyLayout = CanModifyLayout()
            Dim visBitMapCache As New BitmapCache(1.0)

            Await ValidateDispatch(
                Sub()
                    For objVis = 0 To VisObjectCnt
                        ResetVisQuality(VisObjectIdx(objVis), chkModifyLayout)
                    Next
                End Sub)
        End Function

        Public Sub SetVisQuality(objVisTarget As UIElement, objBitMapCache As CacheMode, Optional doLayout As Boolean = True)
            RenderOptions.SetBitmapScalingMode(objVisTarget, BitmapScalingMode.LowQuality)
            RenderOptions.SetEdgeMode(objVisTarget, EdgeMode.Aliased)

            TextOptions.SetTextRenderingMode(objVisTarget, TextRenderingMode.Aliased)
            TextOptions.SetTextFormattingMode(objVisTarget, TextFormattingMode.Display)

            objVisTarget.CacheMode = objBitMapCache

            If doLayout Then
                Dim objVisElement = TryCast(objVisTarget, FrameworkElement)

                If objVisElement IsNot Nothing Then
                    objVisElement.UseLayoutRounding = True
                    objVisElement.SnapsToDevicePixels = True
                End If
            End If
        End Sub

        Public Sub ResetVisQuality(objVis As UIElement, Optional doLayout As Boolean = True)
            RenderOptions.SetBitmapScalingMode(objVis, BitmapScalingMode.HighQuality)
            RenderOptions.SetEdgeMode(objVis, EdgeMode.Unspecified)

            TextOptions.SetTextRenderingMode(objVis, TextRenderingMode.Auto)
            TextOptions.SetTextFormattingMode(objVis, TextFormattingMode.Ideal)

            objVis.CacheMode = Nothing

            If doLayout Then
                Dim objVisElement = TryCast(objVis, FrameworkElement)

                If objVisElement IsNot Nothing Then
                    objVisElement.UseLayoutRounding = False
                    objVisElement.SnapsToDevicePixels = False
                End If
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If _disposed Then Return

            If disposing Then
                If evtResetVisuals IsNot Nothing AndAlso ValidateVisDataOutline() Then
                    RemoveHandler VisDataOutline.Completed, evtResetVisuals
                End If

                If VisAdapter_UI IsNot Nothing Then
                    PropertyChangedEventManager.RemoveHandler(VisAdapter_UI,
                                                          AddressOf OnVisOutlineUpdate,
                                                          NameOf(VisAdapterUI.VisDataObject))
                End If

                If VisObjectIdx IsNot Nothing Then
                    For Each objVis In VisObjectIdx
                        ResetVisQuality(objVis)
                    Next
                End If

                VisObjectIdx = Nothing
                VisRootElement = Nothing
            End If

            _disposed = True
        End Sub

        Protected Overrides Sub Finalize()
            Try
                Dispose(False)
            Finally
                MyBase.Finalize()
            End Try
        End Sub
    End Class

End Namespace

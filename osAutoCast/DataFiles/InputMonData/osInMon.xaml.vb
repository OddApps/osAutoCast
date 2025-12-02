Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports System
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports osProgLoad = osAutoCast.osLoadingElements.osLoadingProgressBar
Imports osAutoCast.DataTypeLib.LoadContentData
Imports osAutoCast.DataTypeLib.LoadingProgStatus

#Disable Warning BC42353

Public Class osInMon

    Public Event osLoadComplete(sender As Object, e As EventArgs)
    Private evtLoadComplete As EventHandler = AddressOf TriggerCompleteEvent

    Private ReadOnly idxLoadTasks As New Dictionary(Of LoadContentData, Func(Of Task)) From
        {
            {isPrefPrep, Function() PrepPrefs()},
            {isLoadingUI, Function() osHandler_UI.PrepAndLoadUI},
            {isApplyConfig, Function() osMenu_Init()},
            {isStartingSvc, Function() InputMonitor_Start()},
            {isStarting, Function() LoadFinalize()}
        }

    Private ReadOnly idxProgressTasks As New Dictionary(Of LoadContentData, LoadingProgStatus) From
        {
            {isLoading, LoadStatus_StartUp},
            {isInit, LoadStatus_Init},
            {isPrefPrep, LoadStatus_PrefPrep},
            {isLoadingUI, LoadStatus_LoadingUI},
            {isApplyConfig, LoadStatus_ApplyConfig},
            {isStartingSvc, LoadStatus_StartingSvc},
            {isStarting, LoadStatus_Starting}
        }

    Public ReadOnly Property objLoadProg As osProgLoad
        Get
            Return Me.LoadingProgressBar
        End Get
    End Property

    Private Async Function TriggerProgress(objLoadingProgStatus As LoadingProgStatus,
                                           Optional objTask As Task = Nothing) As Task
        With objLoadProg
            Dim aa = PrepDispatcher().InvokeAsync(Sub()
                                                      .UpdateLoadProgress(objLoadingProgStatus)
                                                  End Sub)
        End With
        Await Task.Run(Async Function()
                           With objLoadProg

                               If ChkFinalTask(objLoadingProgStatus) Then
                                   Await .MonitorLoadProgress() : End If

                               If ValidateLoadTask(objTask) Then
                                   Await objTask
                               End If
                           End With
                       End Function)
                       End Function

    Private Function ValidateLoadTask(objTask As Task) As Boolean
        Return If(objTask Is Nothing,
            False, True)
    End Function

    Private Function ChkFinalTask(objLoadingProgStatus As LoadingProgStatus) As Boolean
        Return objLoadingProgStatus = LoadStatus_Starting
    End Function

    Private Sub TriggerCompleteEvent(s As Object, e As EventArgs)
        RaiseEvent osLoadComplete(s, e)
        RemoveHandler objLoadProg.LoadProgComplete, evtLoadComplete
    End Sub

    Private Function GetLoadTaskData(objLoadContent As LoadContentData) As LoadingProgStatus
        Return idxProgressTasks.First(
            Function(objLoadTask) objLoadTask.
                Key = objLoadContent).Value
    End Function

    Public Async Function SetLoadText(txtLoad As LoadContentData) As Task
        Await PrepDispatcher().InvokeAsync(
            Sub()
                Select Case txtLoad
                    Case isLoading : Me.LoadingText.Text = "Launching"
                    Case isInit : Me.LoadingText.Text = "Initializing Data"
                    Case isPrefPrep : Me.LoadingText.Text = "Loading Preferences"
                    Case isLoadingUI : Me.LoadingText.Text = "Loading Interface"
                    Case isApplyConfig : Me.LoadingText.Text = "Applying Configuration"
                    Case isStarting : Me.LoadingText.Text = "Starting osAutoCast"
                    Case isStartingSvc : Me.LoadingText.Text = "Activating Service"
                End Select
            End Sub, DispatcherPriority.Background)
    End Function

    Private Function InitLoadTask(objFunc As Func(Of Task)) As Task
        Return objFunc.Invoke()
    End Function

    Private Async Function InitializeContentLoad() As Task
        AddHandler objLoadProg.LoadProgComplete, evtLoadComplete

        Await DispLoadMsg(isLoading)
        Await TriggerProgress(LoadStatus_StartUp)
        Await Task.Delay(GetLoadDuration(isLoading))

        Await DispLoadMsg(isInit)
        Await TriggerProgress(LoadStatus_Init)
        Await Task.Delay(GetLoadDuration(isInit))
    End Function

    Private Async Function LoadDataContent(objLoadContent As LoadContentData,
                                           Optional preDelay As Integer = 0) As Task
        Await DispLoadMsg(objLoadContent)
        Await EvalDelay(preDelay)

        With PrepLoadData(objLoadContent)
            Await TriggerProgress(.LoadProgStatus,
                                  objTask:=InitLoadTask(.LoadProcess))
            Await Task.Delay(.LoadDuration)
        End With
    End Function

    Private Async Function EvalDelay(preDelay As Integer) As Task
        If preDelay > 0 Then
            Await Task.Delay(preDelay)
        End If
    End Function

    Private Async Function DispLoadMsg(objLoadContent As LoadContentData) As Task
        Await Task.Run(
            Async Function()
                Await Me.SetLoadText(objLoadContent)
            End Function)
    End Function

    Private Async Function PrepPrefs() As Task
        Await Task.Run(
            Async Function()
                Dim objTask_PrepPrefs = PrepDispatcher().InvokeAsync(
                    Async Function()
                        Using osPrefManager As New osHandler_Prefs()
                            CoreDataLib.osPrefIndex = Await osPrefManager.LoadPrefs()
                            Await osPrefManager.LoadPrefsAsync(CoreDataLib.osPrefIndex)
                        End Using
                    End Function)

                Await objTask_PrepPrefs.Task.Unwrap()
            End Function)
    End Function

    Private Async Function LoadFinalize() As Task
        Await Task.Run(
            Async Function()
                Await Task.Delay(250)
            End Function)
    End Function

    Private Function FetchTask(objLoadContent As LoadContentData) As Func(Of Task)
        Return idxLoadTasks(objLoadContent)
    End Function

    Private Function PrepLoadData(objLoadContent As LoadContentData) As osLoadData
        Return New osLoadData(FetchTask(objLoadContent),
                              GetLoadDuration(objLoadContent),
                              GetLoadTaskData(objLoadContent))
    End Function

    Private Function PrepLoadScreen() As osInMon
        Dim objLoaderScreen As New osInMon()
        objLoaderScreen.Show()

        Return objLoaderScreen
    End Function

    Public Async Function ProvisionApp() As Task
        Await InitializeContentLoad()

        Await Task.Run(
            Async Function()
                Await LoadDataContent(isPrefPrep)
                Await LoadDataContent(isLoadingUI)
                Await LoadDataContent(isApplyConfig)
                Await LoadDataContent(isStartingSvc)
                Await LoadDataContent(isStarting, 500)
                'Await LoadDataContent(isPrefPrep).ConfigureAwait(False)
                'Await LoadDataContent(isLoadingUI).ConfigureAwait(False)
                'Await LoadDataContent(isApplyConfig).ConfigureAwait(False)
                'Await LoadDataContent(isStartingSvc).ConfigureAwait(False)
                'Await LoadDataContent(isStarting, 500).ConfigureAwait(False)
            End Function)
    End Function

    Public Shared Sub RestartMonitor()
        DoInitTriggerMonitor()
        CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
    End Sub

    Public Async Function InputMonitor_Start() As Task
        Await Task.Run(
            Sub()
                InitTriggerMonitor()
                CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
            End Sub)
    End Function

    Private Sub InitTriggerMonitor()
        DoInitTriggerMonitor()
    End Sub

    Private Shared Sub DoInitTriggerMonitor()
        If CoreDataLib.InputMonSvc IsNot Nothing Then
            CoreDataLib.InputMonSvc = Nothing
        End If

        CoreDataLib.InputMonSvc = New InputMonitorService()
    End Sub

    Private Function GetLoadDuration(objLoadContent As LoadContentData) As Integer
        Select Case objLoadContent
            Case isInit : Return 600
            Case isLoading : Return 600
            Case isPrefPrep : Return 750
            Case isLoadingUI : Return 800
            Case isApplyConfig : Return 700
            Case isStartingSvc : Return 700
            Case isStarting : Return 500
        End Select
    End Function

End Class

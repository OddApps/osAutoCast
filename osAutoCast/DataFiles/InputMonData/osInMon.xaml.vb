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
Imports osAutoCast.DataTypeLib.LoadStep

#Disable Warning BC42353

Public Class osInMon

    Public Event osLoadComplete(sender As Object, e As EventArgs)

    Private evtLoadComplete As EventHandler = AddressOf TriggerCompleteEvent

    Private isTaskValid As Boolean
    Private objValidTask As Task

    Private ReadOnly LoadDataIdx As New List(Of osLoadData) From
        {
            {New osLoadData(LoadStart, isLoading, LoadStatus_StartUp, 450, "Launching")},
            {New osLoadData(LoadInit, isInit, LoadStatus_Init, 450, "Initializing Data", objPreLoadDuration:=150)},
            {New osLoadData(LoadPrefs, isPrefPrep, LoadStatus_PrefPrep, 425, "Loading Preferences", Function() PrepPrefs(), 115)},
            {New osLoadData(LoadUI, isLoadingUI, LoadStatus_LoadingUI, 375, "Loading Interface", Function() osHandler_UI.PrepAndLoadUI(), 125)},
            {New osLoadData(LoadConfig, isApplyConfig, LoadStatus_ApplyConfig, 500, "Applying Configuration", Function() osMenu_Init(), 125)},
            {New osLoadData(LoadService, isStartingSvc, LoadStatus_StartingSvc, 550, "Activating Service", Function() InputMonitor_Start(), 100)},
            {New osLoadData(LoadComplete, isStarting, LoadStatus_Starting, 550, "Starting osAutoCast", Function() LoadFinalize(), 500)}
        }

    Public ReadOnly Property objLoadProg As osProgLoad
        Get
            Return Me.LoadingProgressBar
        End Get
    End Property

    Public ReadOnly Property objLoadText As TextBlock
        Get
            Return Me.LoadingText
        End Get
    End Property

    Private Sub ValidateTaskAndRun(objTask As Func(Of Task), ByRef objValidTask As Task, ByRef isValid As Boolean)
        If objTask Is Nothing Then
            isValid = False
            objValidTask = Nothing
        Else
            isValid = True
            objValidTask = objTask.Invoke()
        End If
    End Sub

    Private Function ChkFinalTask(objLoadingProgStatus As LoadingProgStatus) As Boolean
        Return objLoadingProgStatus = LoadStatus_Starting
    End Function

    Private Sub TriggerCompleteEvent(s As Object, e As EventArgs)
        RaiseEvent osLoadComplete(s, e)
        RemoveHandler objLoadProg.LoadProgComplete, evtLoadComplete
    End Sub

    Private Sub ApplyLoadText(txtLoad As String)
        objLoadText.Text = txtLoad
    End Sub

    Public Function DisplayLoadMsg(txtLoad As String) As Task
        Return PrepDispatcher().InvokeAsync(
            Sub()
                ApplyLoadText(txtLoad)
            End Sub, DispatcherPriority.Normal).Task
    End Function

    Private Async Function InitializeContentLoad() As Task
        AddHandler objLoadProg.LoadProgComplete, evtLoadComplete

        Await PerformLoadStep(LoadStart)

        Await Task.Delay(500)

        Await PerformLoadStep(LoadInit)
    End Function

    Private Function FetchLoadData(getLoadStep As LoadStep) As osLoadData
        Return LoadDataIdx.First(
            Function(objLoadStep)
                Return objLoadStep.LoadingStep = getLoadStep
            End Function)
    End Function

    Private Async Function PerformLoadStep(objLoadStep As LoadStep) As Task
        With FetchLoadData(objLoadStep)

            Dim objTask_DispLoadMsg = DisplayLoadMsg(.LoadMsg)

            Dim objTask_UpProg = PrepDispatcher().InvokeAsync(
                Sub() objLoadProg.UpdateLoadProgress(.LoadProgStatus),
                    DispatcherPriority.Send)

            Await EvalDelay(.PreLoadDuration)

            If ChkFinalTask(.LoadProgStatus) Then
                Await objLoadProg.MonitorLoadProgress() : End If

            ValidateTaskAndRun(.LoadProcess, objValidTask,
                               isTaskValid)

            If isTaskValid Then
                Await objValidTask : End If

            Await Task.Delay(.LoadDuration)
            Await objTask_UpProg.Task

        End With
    End Function

    Private Async Function EvalDelay(preDelay As Integer) As Task
        If preDelay > 0 Then
            Await Task.Delay(preDelay)
        End If
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

    Public Async Function ProvisionApp() As Task
        Await InitializeContentLoad()

        Await PerformLoadStep(LoadPrefs)
        Await PerformLoadStep(LoadUI)
        Await PerformLoadStep(LoadConfig)
        Await PerformLoadStep(LoadService)
        Await PerformLoadStep(LoadComplete)
    End Function

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

End Class

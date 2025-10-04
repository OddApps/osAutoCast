Imports System.Reactive.Linq
Imports System.Threading

Public Class osInMon
    Private Shared InputMon_Support As IDisposable
    Private InputMonitorAbortSrc As CancellationTokenSource

    Private Async Sub MainWindow_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        Await Task.Delay(5)
        osInitialize()

    End Sub

    Private Sub osInitialize()
        PrepPrefs()

        Dim a As New osInputMonitor
        Dim b = a.Handle

        osHandler_GUI.PreloadForms(a, Me)

        osMenu_Init(Me)

        ' osHandler_GUI.osGui_InputMonitor2 = Me
        InputMonitor_Launch()
    End Sub

    Private Async Sub InputMonitor_Init(Optional isRestart As Boolean = False)
        If isRestart Then
            Me.InputMonitor_Prep()
        End If
        Await Task.Delay(10)
        'If osFuncLib_InputScan.isMonitorActive() Then Return
        'If osFuncLib_InputScan.isMonitorInStartup() Then
        '    osFuncLib_InputScan.ActivateMonitor()
        'End If
        Me.InputMonitor_Start()
    End Sub

    Private Sub InitTriggerMonitor()
        DoInitTriggerMonitor()
    End Sub

    Private Shared Sub DoInitTriggerMonitor()
        If CoreDataLib.InputMonSvc IsNot Nothing Then
            CoreDataLib.InputMonSvc = Nothing
        End If
        CoreDataLib.InputMonSvc = New InputMonitorService()
    End Sub

    Public Sub InputMonitor_Start()
        Me.InitTriggerMonitor()
        CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
    End Sub

    Public Shared Sub StopMonitoring()
        If InputMon_Support Is Nothing Then Return
        InputMon_Support.Dispose()
        InputMon_Support = Nothing
    End Sub

    Private Sub InputMonitor_Prep()
        ' osFuncLib_InputScan.SetMonitorState(DataTypeLib.MonitorStatus.Starting)
        Me.InputMonitorAbortSrc = New CancellationTokenSource()
    End Sub

    Private Sub InputMonitor_Launch(Optional isRestart As Boolean = False)
        Me.InputMonitor_Prep()
        Me.InputMonitor_Init()
    End Sub

    Public Sub RestartMonitor()
        InputMonitor_Init(isRestart:=True)
    End Sub

    Private Sub PrepPrefs()
        Me.Visibility = Visibility.Hidden
        Me.Hide()
        Using loader As New osPrefLoader(CoreDataLib.osPrefIndex)
            loader.ProcessPrefIndex(CoreDataLib.osPrefIndex)
        End Using
        osFuncLib_Progress.InitProgColors()
    End Sub

End Class

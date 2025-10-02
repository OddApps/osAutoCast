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
        PreloadForms(Me)

        osMenu_Init(Me)

        osGui_InputMonitor = Me
        InputMonitor_Launch()
    End Sub

    Private Async Sub InputMonitor_Init(Optional isRestart As Boolean = False)

        ' If isRestart Then InputMonitor_Prep()
        Await Task.Delay(10)

        'If osFuncLib_InputScan.isMonitorActive() Then Return

        'If osFuncLib_InputScan.isMonitorInStartup() Then osFuncLib_InputScan.ActivateMonitor()

        InputMonitor_Start()
    End Sub

    Private Sub InitTriggerMonitor()
        If InputMonSvc IsNot Nothing Then
            InputMonSvc = Nothing
        End If

        InputMonSvc = New InputMonitorService
    End Sub

    Public Sub InputMonitor_Start()
        InitTriggerMonitor()
        InputMonSvc.LaunchTriggerMonitor()
    End Sub

    Public Shared Sub StopMonitoring()
        If InputMon_Support IsNot Nothing Then
            InputMon_Support.Dispose()
            InputMon_Support = Nothing
        End If
    End Sub

    Private Sub InputMonitor_Prep()
        '  osFuncLib_InputScan.SetMonitorState(MonitorStatus.Starting)

        InputMonitorAbortSrc = New CancellationTokenSource()
    End Sub

    Private Sub InputMonitor_Launch(Optional isRestart As Boolean = False)
        InputMonitor_Prep()
        InputMonitor_Init()
    End Sub

    Public Sub RestartMonitor()
        InputMonitor_Init(isRestart:=True)
    End Sub

    Private Sub PrepPrefs()
        Me.Visibility = Visibility.Hidden
        Me.Hide()

        Using osPrefManager As New osPrefLoader(osPrefIndex)
            osPrefManager.ProcessPrefIndex(osPrefIndex)
        End Using

        InitProgColors()
    End Sub

End Class

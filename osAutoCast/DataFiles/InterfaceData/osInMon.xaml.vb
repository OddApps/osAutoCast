Imports System.Reactive.Linq
Imports System.Threading

Public Class osInMon
    Private Shared InputMon_Support As IDisposable
    Private InputMonitorAbortSrc As CancellationTokenSource

    Private Async Sub MainWindow_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        Await Task.Delay(1000)
        osInitialize()
    End Sub

    Private Sub DoAppPrep()
        PrepPrefs()
        osHandler_GUI.PreloadForms(Me)
        osMenu_Init(Me)
    End Sub

    Private Async Sub osInitialize()
        SetLoadText(LoadTextContent.isInit)

        DoAppPrep()

        Await Task.Delay(1000)

        SetLoadText(LoadTextContent.isApplyConfig)
        InputMonitor_Launch()
    End Sub

    Private Async Sub InputMonitor_Init(Optional isRestart As Boolean = False)
        If isRestart Then
            InputMonitor_Prep()
        End If

        Await Task.Delay(1250)

        SetLoadText(LoadTextContent.isStartingSvc)

        Await Task.Delay(10)

        InputMonitor_Start()

        Await Task.Delay(1000)

        SetLoadText(LoadTextContent.isStarting)
        Await Task.Delay(500)

        Visibility = Visibility.Hidden
        Hide()
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
        InitTriggerMonitor()
        CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
    End Sub

    Public Shared Sub StopMonitoring()
        If InputMon_Support Is Nothing Then Return
        InputMon_Support.Dispose()
        InputMon_Support = Nothing
    End Sub

    Private Sub InputMonitor_Prep()
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
        Using osPrefManager As New osPrefLoader(CoreDataLib.osPrefIndex)
            osPrefManager.ProcessPrefIndex(CoreDataLib.osPrefIndex)
        End Using

        osFuncLib_Progress.InitProgColors()
    End Sub

    Private Sub SetLoadText(txtLoad As LoadTextContent)
        Select Case txtLoad
            Case LoadTextContent.isLoading
                lblLoadContent.Text = "Loading"
            Case LoadTextContent.isStarting
                lblLoadContent.Text = "Starting osAutoCast"
            Case LoadTextContent.isInit
                lblLoadContent.Text = "Initializing"
            Case LoadTextContent.isStartingSvc
                lblLoadContent.Text = "Starting Service"
            Case LoadTextContent.isApplyConfig
                lblLoadContent.Text = "Applying Configuration"
        End Select
    End Sub

End Class

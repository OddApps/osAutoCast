Imports System.Reactive.Linq
Imports System.Threading
Imports System.Windows.Forms

Class Application

    Private Sub Application_Exit(sender As Object, e As ExitEventArgs) Handles Me.[Exit]
        osHandler_Graphics.DisposeAll()
        CoreDataLib.osTrayIcon.Visible = False
    End Sub

    Private Async Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Dim objLoadScreen = PrepLoadScreen()
        Await Task.Delay(1000)

        Dim objTask_Provision = ProvisionApp(objLoadScreen)
        Await objTask_Provision

        objLoadScreen.Close()

        osHandler_UI.RecaptureResources()
    End Sub

    Private Async Function DoLoad_AppPrep(objLoadWin As osInMon) As Task
        PrepPrefs()

        osHandler_UI.PreloadForms()
        osMenu_Init()

        Await Task.Delay(1000)
    End Function

    Private Async Function DoLoad_ApplyConfig() As Task
        Await Task.Delay(1250)
    End Function

    Private Async Function DoLoad_Finalize(objLoadWin As osInMon) As Task
        Await Task.Delay(500)
        objLoadWin.ShowInTaskbar = False
    End Function

    Private Async Function DoLoad_StartMonitor() As Task
        Await Task.Delay(10)

        InputMonitor_Start()

        Await Task.Delay(1000)
    End Function

    Private Sub PrepPrefs()
        Using osPrefManager As New osHandler_Prefs(CoreDataLib.osPrefIndex)
            osPrefManager.ProcessPrefIndex(CoreDataLib.osPrefIndex)
        End Using
    End Sub

    Private Function PrepLoadScreen() As osInMon
        Dim objLoaderScreen As New osInMon()
        objLoaderScreen.Show()

        Return objLoaderScreen
    End Function

    Private Async Function ProvisionApp(objLoadWin As osInMon) As Task
        objLoadWin.SetLoadText(LoadTextContent.isInit)
        Await Task.Delay(1000)

        Await DoLoad_AppPrep(objLoadWin)

        objLoadWin.SetLoadText(LoadTextContent.isApplyConfig)

        Await DoLoad_ApplyConfig()

        objLoadWin.SetLoadText(LoadTextContent.isStartingSvc)

        Await DoLoad_StartMonitor()

        objLoadWin.SetLoadText(LoadTextContent.isStarting)

        Await DoLoad_Finalize(objLoadWin)
    End Function

    Public Shared Sub RestartMonitor()
        DoInitTriggerMonitor()
        CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
    End Sub

    Public Sub InputMonitor_Start()
        InitTriggerMonitor()
        CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
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

End Class

Imports osAutoCast.DataTypeLib.LoadContentData

#Disable Warning BC42353
Class Application

    Private objTask_LoadComplete As TaskCompletionSource(Of Boolean) = Nothing

    Private Sub osAutoCast_Exit(sender As Object, e As ExitEventArgs) Handles Me.[Exit]
        osHandler_Graphics.DisposeAll()
        CoreDataLib.osTrayIcon.Visible = False
    End Sub

    Private Async Sub osAutoCast_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Dim objLoadScreen As osInMon = PrepLoadScreen()

        AddHandler objLoadScreen.osLoadComplete,
           Async Sub()
               Await Task.Delay(325)
               objLoadScreen.Close()
           End Sub

        Await objLoadScreen.ProvisionApp()
    End Sub

    Private Function PrepLoadScreen() As osInMon
        Dim objLoaderScreen As New osInMon()
        objLoaderScreen.Show()

        Return objLoaderScreen
    End Function

    Private Sub TaskWaitForLoadComplete()
        objTask_LoadComplete.ResetAndInitTask()
    End Sub

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
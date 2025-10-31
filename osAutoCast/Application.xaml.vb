Class Application
    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        ' RenderOptions.ProcessRenderMode = Interop.RenderMode.SoftwareOnly
    End Sub

    Private Sub Application_Exit(sender As Object, e As ExitEventArgs) Handles Me.[Exit]
        GraphicsHandler.DisposeAll()
    End Sub

End Class

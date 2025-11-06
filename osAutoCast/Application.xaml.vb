Class Application

    Private Sub Application_Exit(sender As Object, e As ExitEventArgs) Handles Me.[Exit]
        osHandler_Graphics.DisposeAll()
    End Sub

End Class

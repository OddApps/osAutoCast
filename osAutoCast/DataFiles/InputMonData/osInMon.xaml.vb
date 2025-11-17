
Public Class osInMon

    Public Sub SetLoadText(txtLoad As LoadTextContent)
        Select Case txtLoad
            Case LoadTextContent.isLoading
                Me.lblLoadContent.Text = "Loading"
            Case LoadTextContent.isStarting
                Me.lblLoadContent.Text = "Starting osAutoCast"
            Case LoadTextContent.isInit
                Me.lblLoadContent.Text = "Initializing"
            Case LoadTextContent.isStartingSvc
                Me.lblLoadContent.Text = "Starting Service"
            Case LoadTextContent.isApplyConfig
                Me.lblLoadContent.Text = "Applying Configuration"
        End Select
    End Sub

End Class

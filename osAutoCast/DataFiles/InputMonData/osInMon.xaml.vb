
Public Class osInMon

    Public Sub SetLoadText(txtLoad As LoadContentData)
        Select Case txtLoad
            Case LoadContentData.isLoading
                Me.lblLoadContent.Text = "Launching"
            Case LoadContentData.isInit
                Me.lblLoadContent.Text = "Initializing Data"
            Case LoadContentData.isPrefPrep
                Me.lblLoadContent.Text = "Loading Preferences"
            Case LoadContentData.isLoadingUI
                Me.lblLoadContent.Text = "Loading Interface"
            Case LoadContentData.isApplyConfig
                Me.lblLoadContent.Text = "Applying Configuration"
            Case LoadContentData.isStarting
                Me.lblLoadContent.Text = "Starting osAutoCast"
            Case LoadContentData.isStartingSvc
                Me.lblLoadContent.Text = "Activating Service"
        End Select
    End Sub

End Class

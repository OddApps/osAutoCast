<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ProgBarGui_AutoCast
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent(pW As Integer, pH As Integer)
        With Me
            .SuspendLayout()
            '
            'ProgBarGui_AutoCast
            '
            .FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
            .Name = "ProgBarGui_AutoCast"
            .ClientSize = New System.Drawing.Size(pW, pH)

            Me.ResumeLayout(False)
        End With

    End Sub
End Class

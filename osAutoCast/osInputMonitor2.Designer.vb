<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class osInputMonitor2
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        tmrInit = New Forms.Timer(components)
        osMenuObj = New Forms.ContextMenuStrip(components)
        osMenu_EnDis = New Forms.ToolStripMenuItem()
        ToolStripSeparator2 = New Forms.ToolStripSeparator()
        osMenu_GameOpts = New Forms.ToolStripMenuItem()
        osGameMenu_Play = New Forms.ToolStripMenuItem()
        osGameMenu_Leave = New Forms.ToolStripMenuItem()
        ToolStripSeparator1 = New Forms.ToolStripSeparator()
        osMenu_Opts = New Forms.ToolStripMenuItem()
        osMenuExit = New Forms.ToolStripMenuItem()
        osMenuObj.SuspendLayout()
        SuspendLayout()
        ' 
        ' tmrInit
        ' 
        tmrInit.Interval = 10
        ' 
        ' osMenuObj
        ' 
        osMenuObj.Font = New System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CByte(0))
        osMenuObj.Items.AddRange(New Forms.ToolStripItem() {osMenu_EnDis, ToolStripSeparator2, osMenu_GameOpts, ToolStripSeparator1, osMenu_Opts, osMenuExit})
        osMenuObj.Name = "osMenuObj"
        osMenuObj.RenderMode = Forms.ToolStripRenderMode.Professional
        osMenuObj.ShowCheckMargin = True
        osMenuObj.Size = New System.Drawing.Size(179, 112)
        ' 
        ' osMenu_EnDis
        ' 
        osMenu_EnDis.Checked = True
        osMenu_EnDis.CheckState = Forms.CheckState.Checked
        osMenu_EnDis.Name = "osMenu_EnDis"
        osMenu_EnDis.Size = New System.Drawing.Size(178, 24)
        osMenu_EnDis.Text = "Enabled"
        ' 
        ' ToolStripSeparator2
        ' 
        ToolStripSeparator2.Name = "ToolStripSeparator2"
        ToolStripSeparator2.Size = New System.Drawing.Size(175, 6)
        ' 
        ' osMenu_GameOpts
        ' 
        osMenu_GameOpts.DropDownItems.AddRange(New Forms.ToolStripItem() {osGameMenu_Play, osGameMenu_Leave})
        osMenu_GameOpts.Name = "osMenu_GameOpts"
        osMenu_GameOpts.Size = New System.Drawing.Size(178, 24)
        osMenu_GameOpts.Text = "MTG Menu"
        ' 
        ' osGameMenu_Play
        ' 
        osGameMenu_Play.Name = "osGameMenu_Play"
        osGameMenu_Play.Size = New System.Drawing.Size(161, 24)
        osGameMenu_Play.Text = "Play Game"
        ' 
        ' osGameMenu_Leave
        ' 
        osGameMenu_Leave.Name = "osGameMenu_Leave"
        osGameMenu_Leave.Size = New System.Drawing.Size(161, 24)
        osGameMenu_Leave.Text = "Leave Game"
        ' 
        ' ToolStripSeparator1
        ' 
        ToolStripSeparator1.Name = "ToolStripSeparator1"
        ToolStripSeparator1.Size = New System.Drawing.Size(175, 6)
        ' 
        ' osMenu_Opts
        ' 
        osMenu_Opts.Name = "osMenu_Opts"
        osMenu_Opts.Size = New System.Drawing.Size(178, 24)
        osMenu_Opts.Text = "Preferences"
        ' 
        ' osMenuExit
        ' 
        osMenuExit.Name = "osMenuExit"
        osMenuExit.Size = New System.Drawing.Size(178, 24)
        osMenuExit.Text = "Exit"
        ' 
        ' osInputMonitor
        ' 
        AutoScaleDimensions = New System.Drawing.SizeF(7.0F, 15.0F)
        AutoScaleMode = Forms.AutoScaleMode.Font
        ClientSize = New System.Drawing.Size(34, 21)
        ControlBox = False
        FormBorderStyle = Forms.FormBorderStyle.None
        Margin = New Forms.Padding(4, 3, 4, 3)
        Name = "osInputMonitor"
        ShowIcon = False
        ShowInTaskbar = False
        Text = "OddScript"
        osMenuObj.ResumeLayout(False)
        ResumeLayout(False)

    End Sub

    Friend WithEvents tmrInit As Forms.Timer
    Friend WithEvents osMenu_EnDis As Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As Forms.ToolStripSeparator
    Friend WithEvents osMenu_Opts As Forms.ToolStripMenuItem
    Friend WithEvents osMenuExit As Forms.ToolStripMenuItem
    Friend WithEvents osMenuObj As Forms.ContextMenuStrip
    Friend WithEvents ToolStripSeparator2 As Forms.ToolStripSeparator
    Friend WithEvents osMenu_GameOpts As Forms.ToolStripMenuItem
    Friend WithEvents osGameMenu_Play As Forms.ToolStripMenuItem
    Friend WithEvents osGameMenu_Leave As Forms.ToolStripMenuItem
    Friend WithEvents ContextMenuStrip1 As Forms.ContextMenuStrip
End Class

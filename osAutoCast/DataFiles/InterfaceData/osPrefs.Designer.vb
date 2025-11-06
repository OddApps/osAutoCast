<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class osPrefs
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
        Me.grpAutoCast = New System.Windows.Forms.GroupBox()
        Me.objContainer_AutoCast = New System.Windows.Forms.TableLayoutPanel()
        Me.txtAutoCastFuse = New System.Windows.Forms.NumericUpDown()
        Me.lblFuse = New System.Windows.Forms.Label()
        Me.chkAutoCastRTC = New System.Windows.Forms.CheckBox()
        Me.btnSavePrefs = New System.Windows.Forms.Button()
        Me.objContentContainer = New System.Windows.Forms.TableLayoutPanel()
        Me.grpGenOpts = New System.Windows.Forms.GroupBox()
        Me.objContainer_GeneralOpts = New System.Windows.Forms.TableLayoutPanel()
        Me.lblVisualQuality = New System.Windows.Forms.Label()
        Me.lstVisualQuality = New System.Windows.Forms.ComboBox()
        Me.grpAutoPass = New System.Windows.Forms.GroupBox()
        Me.objContainer_AutoPass = New System.Windows.Forms.TableLayoutPanel()
        Me.txtAutoPassSafetyTimer = New System.Windows.Forms.NumericUpDown()
        Me.lblSafetyTimer = New System.Windows.Forms.Label()
        Me.grpAutoCast.SuspendLayout()
        Me.objContainer_AutoCast.SuspendLayout()
        CType(Me.txtAutoCastFuse, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.objContentContainer.SuspendLayout()
        Me.grpGenOpts.SuspendLayout()
        Me.objContainer_GeneralOpts.SuspendLayout()
        Me.grpAutoPass.SuspendLayout()
        Me.objContainer_AutoPass.SuspendLayout()
        CType(Me.txtAutoPassSafetyTimer, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'grpAutoCast
        '
        Me.grpAutoCast.AutoSize = True
        Me.grpAutoCast.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.grpAutoCast.Controls.Add(Me.objContainer_AutoCast)
        Me.grpAutoCast.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grpAutoCast.Font = New System.Drawing.Font("Tahoma", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.grpAutoCast.Location = New System.Drawing.Point(4, 4)
        Me.grpAutoCast.Margin = New System.Windows.Forms.Padding(4)
        Me.grpAutoCast.MaximumSize = New System.Drawing.Size(0, 90)
        Me.grpAutoCast.Name = "grpAutoCast"
        Me.grpAutoCast.Padding = New System.Windows.Forms.Padding(4, 2, 4, 4)
        Me.grpAutoCast.Size = New System.Drawing.Size(202, 86)
        Me.grpAutoCast.TabIndex = 0
        Me.grpAutoCast.TabStop = False
        Me.grpAutoCast.Text = "AutoCast"
        '
        'objContainer_AutoCast
        '
        Me.objContainer_AutoCast.AutoSize = True
        Me.objContainer_AutoCast.ColumnCount = 2
        Me.objContainer_AutoCast.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104.0!))
        Me.objContainer_AutoCast.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.objContainer_AutoCast.Controls.Add(Me.txtAutoCastFuse, 1, 0)
        Me.objContainer_AutoCast.Controls.Add(Me.lblFuse, 0, 0)
        Me.objContainer_AutoCast.Controls.Add(Me.chkAutoCastRTC, 0, 1)
        Me.objContainer_AutoCast.Dock = System.Windows.Forms.DockStyle.Fill
        Me.objContainer_AutoCast.Location = New System.Drawing.Point(4, 18)
        Me.objContainer_AutoCast.Margin = New System.Windows.Forms.Padding(0)
        Me.objContainer_AutoCast.Name = "objContainer_AutoCast"
        Me.objContainer_AutoCast.RowCount = 2
        Me.objContainer_AutoCast.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.objContainer_AutoCast.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.objContainer_AutoCast.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.objContainer_AutoCast.Size = New System.Drawing.Size(194, 64)
        Me.objContainer_AutoCast.TabIndex = 6
        '
        'txtAutoCastFuse
        '
        Me.txtAutoCastFuse.AutoSize = True
        Me.txtAutoCastFuse.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txtAutoCastFuse.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtAutoCastFuse.Increment = New Decimal(New Integer() {5, 0, 0, 0})
        Me.txtAutoCastFuse.Location = New System.Drawing.Point(107, 3)
        Me.txtAutoCastFuse.Maximum = New Decimal(New Integer() {1000, 0, 0, 0})
        Me.txtAutoCastFuse.Name = "txtAutoCastFuse"
        Me.txtAutoCastFuse.Size = New System.Drawing.Size(84, 26)
        Me.txtAutoCastFuse.TabIndex = 3
        Me.txtAutoCastFuse.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'lblFuse
        '
        Me.lblFuse.AutoSize = True
        Me.lblFuse.BackColor = System.Drawing.Color.Transparent
        Me.lblFuse.Dock = System.Windows.Forms.DockStyle.Left
        Me.lblFuse.Font = New System.Drawing.Font("Segoe UI Semibold", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblFuse.Location = New System.Drawing.Point(3, 3)
        Me.lblFuse.Margin = New System.Windows.Forms.Padding(3)
        Me.lblFuse.Name = "lblFuse"
        Me.lblFuse.Size = New System.Drawing.Size(39, 26)
        Me.lblFuse.TabIndex = 1
        Me.lblFuse.Text = "Fuse:"
        Me.lblFuse.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'chkAutoCastRTC
        '
        Me.chkAutoCastRTC.AutoSize = True
        Me.chkAutoCastRTC.CheckAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.objContainer_AutoCast.SetColumnSpan(Me.chkAutoCastRTC, 2)
        Me.chkAutoCastRTC.Dock = System.Windows.Forms.DockStyle.Fill
        Me.chkAutoCastRTC.Font = New System.Drawing.Font("Segoe UI Semibold", 9.75!, System.Drawing.FontStyle.Bold)
        Me.chkAutoCastRTC.Location = New System.Drawing.Point(3, 35)
        Me.chkAutoCastRTC.Name = "chkAutoCastRTC"
        Me.chkAutoCastRTC.Size = New System.Drawing.Size(188, 26)
        Me.chkAutoCastRTC.TabIndex = 4
        Me.chkAutoCastRTC.Text = "Release To Cast"
        Me.chkAutoCastRTC.UseVisualStyleBackColor = True
        '
        'btnSavePrefs
        '
        Me.btnSavePrefs.AutoSize = True
        Me.btnSavePrefs.Dock = System.Windows.Forms.DockStyle.Fill
        Me.btnSavePrefs.Font = New System.Drawing.Font("Trebuchet MS", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSavePrefs.Location = New System.Drawing.Point(3, 225)
        Me.btnSavePrefs.Name = "btnSavePrefs"
        Me.btnSavePrefs.Size = New System.Drawing.Size(204, 38)
        Me.btnSavePrefs.TabIndex = 1
        Me.btnSavePrefs.Text = "Save"
        Me.btnSavePrefs.UseVisualStyleBackColor = True
        '
        'objContentContainer
        '
        Me.objContentContainer.AutoSize = True
        Me.objContentContainer.ColumnCount = 1
        Me.objContentContainer.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.objContentContainer.Controls.Add(Me.grpGenOpts, 0, 2)
        Me.objContentContainer.Controls.Add(Me.grpAutoPass, 0, 1)
        Me.objContentContainer.Controls.Add(Me.btnSavePrefs, 0, 3)
        Me.objContentContainer.Controls.Add(Me.grpAutoCast, 0, 0)
        Me.objContentContainer.Dock = System.Windows.Forms.DockStyle.Fill
        Me.objContentContainer.Location = New System.Drawing.Point(0, 0)
        Me.objContentContainer.Margin = New System.Windows.Forms.Padding(0)
        Me.objContentContainer.Name = "objContentContainer"
        Me.objContentContainer.RowCount = 4
        Me.objContentContainer.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.objContentContainer.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.objContentContainer.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.objContentContainer.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34.0!))
        Me.objContentContainer.Size = New System.Drawing.Size(210, 266)
        Me.objContentContainer.TabIndex = 2
        '
        'grpGenOpts
        '
        Me.grpGenOpts.AutoSize = True
        Me.grpGenOpts.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.grpGenOpts.Controls.Add(Me.objContainer_GeneralOpts)
        Me.grpGenOpts.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grpGenOpts.Font = New System.Drawing.Font("Tahoma", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.grpGenOpts.Location = New System.Drawing.Point(4, 162)
        Me.grpGenOpts.Margin = New System.Windows.Forms.Padding(4, 4, 4, 6)
        Me.grpGenOpts.MaximumSize = New System.Drawing.Size(0, 60)
        Me.grpGenOpts.Name = "grpGenOpts"
        Me.grpGenOpts.Padding = New System.Windows.Forms.Padding(4, 2, 4, 4)
        Me.grpGenOpts.Size = New System.Drawing.Size(202, 54)
        Me.grpGenOpts.TabIndex = 4
        Me.grpGenOpts.TabStop = False
        Me.grpGenOpts.Text = "General"
        '
        'objContainer_GeneralOpts
        '
        Me.objContainer_GeneralOpts.AutoSize = True
        Me.objContainer_GeneralOpts.ColumnCount = 2
        Me.objContainer_GeneralOpts.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104.0!))
        Me.objContainer_GeneralOpts.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.objContainer_GeneralOpts.Controls.Add(Me.lblVisualQuality, 0, 0)
        Me.objContainer_GeneralOpts.Controls.Add(Me.lstVisualQuality, 1, 0)
        Me.objContainer_GeneralOpts.Dock = System.Windows.Forms.DockStyle.Fill
        Me.objContainer_GeneralOpts.Location = New System.Drawing.Point(4, 18)
        Me.objContainer_GeneralOpts.Margin = New System.Windows.Forms.Padding(0)
        Me.objContainer_GeneralOpts.Name = "objContainer_GeneralOpts"
        Me.objContainer_GeneralOpts.RowCount = 1
        Me.objContainer_GeneralOpts.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.objContainer_GeneralOpts.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.objContainer_GeneralOpts.Size = New System.Drawing.Size(194, 32)
        Me.objContainer_GeneralOpts.TabIndex = 7
        '
        'lblVisualQuality
        '
        Me.lblVisualQuality.AutoSize = True
        Me.lblVisualQuality.BackColor = System.Drawing.Color.Transparent
        Me.lblVisualQuality.Dock = System.Windows.Forms.DockStyle.Left
        Me.lblVisualQuality.Font = New System.Drawing.Font("Segoe UI Semibold", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblVisualQuality.Location = New System.Drawing.Point(3, 3)
        Me.lblVisualQuality.Margin = New System.Windows.Forms.Padding(3)
        Me.lblVisualQuality.Name = "lblVisualQuality"
        Me.lblVisualQuality.Size = New System.Drawing.Size(93, 26)
        Me.lblVisualQuality.TabIndex = 1
        Me.lblVisualQuality.Text = "Visual Quality:"
        Me.lblVisualQuality.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'lstVisualQuality
        '
        Me.lstVisualQuality.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lstVisualQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.lstVisualQuality.Font = New System.Drawing.Font("Trebuchet MS", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lstVisualQuality.FormattingEnabled = True
        Me.lstVisualQuality.Location = New System.Drawing.Point(107, 3)
        Me.lstVisualQuality.MaxDropDownItems = 2
        Me.lstVisualQuality.Name = "lstVisualQuality"
        Me.lstVisualQuality.Size = New System.Drawing.Size(84, 26)
        Me.lstVisualQuality.TabIndex = 2
        '
        'grpAutoPass
        '
        Me.grpAutoPass.AutoSize = True
        Me.grpAutoPass.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.grpAutoPass.Controls.Add(Me.objContainer_AutoPass)
        Me.grpAutoPass.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grpAutoPass.Font = New System.Drawing.Font("Tahoma", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.grpAutoPass.Location = New System.Drawing.Point(4, 98)
        Me.grpAutoPass.Margin = New System.Windows.Forms.Padding(4, 4, 4, 6)
        Me.grpAutoPass.MaximumSize = New System.Drawing.Size(0, 60)
        Me.grpAutoPass.Name = "grpAutoPass"
        Me.grpAutoPass.Padding = New System.Windows.Forms.Padding(4, 2, 4, 4)
        Me.grpAutoPass.Size = New System.Drawing.Size(202, 54)
        Me.grpAutoPass.TabIndex = 3
        Me.grpAutoPass.TabStop = False
        Me.grpAutoPass.Text = "AutoPass"
        '
        'objContainer_AutoPass
        '
        Me.objContainer_AutoPass.AutoSize = True
        Me.objContainer_AutoPass.ColumnCount = 2
        Me.objContainer_AutoPass.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104.0!))
        Me.objContainer_AutoPass.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.objContainer_AutoPass.Controls.Add(Me.txtAutoPassSafetyTimer, 0, 0)
        Me.objContainer_AutoPass.Controls.Add(Me.lblSafetyTimer, 0, 0)
        Me.objContainer_AutoPass.Dock = System.Windows.Forms.DockStyle.Fill
        Me.objContainer_AutoPass.Location = New System.Drawing.Point(4, 18)
        Me.objContainer_AutoPass.Margin = New System.Windows.Forms.Padding(0)
        Me.objContainer_AutoPass.Name = "objContainer_AutoPass"
        Me.objContainer_AutoPass.RowCount = 1
        Me.objContainer_AutoPass.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.objContainer_AutoPass.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.objContainer_AutoPass.Size = New System.Drawing.Size(194, 32)
        Me.objContainer_AutoPass.TabIndex = 7
        '
        'txtAutoPassSafetyTimer
        '
        Me.txtAutoPassSafetyTimer.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txtAutoPassSafetyTimer.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtAutoPassSafetyTimer.Increment = New Decimal(New Integer() {5, 0, 0, 0})
        Me.txtAutoPassSafetyTimer.Location = New System.Drawing.Point(107, 3)
        Me.txtAutoPassSafetyTimer.Maximum = New Decimal(New Integer() {3000, 0, 0, 0})
        Me.txtAutoPassSafetyTimer.Name = "txtAutoPassSafetyTimer"
        Me.txtAutoPassSafetyTimer.Size = New System.Drawing.Size(84, 26)
        Me.txtAutoPassSafetyTimer.TabIndex = 3
        Me.txtAutoPassSafetyTimer.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'lblSafetyTimer
        '
        Me.lblSafetyTimer.AutoSize = True
        Me.lblSafetyTimer.BackColor = System.Drawing.Color.Transparent
        Me.lblSafetyTimer.Dock = System.Windows.Forms.DockStyle.Left
        Me.lblSafetyTimer.Font = New System.Drawing.Font("Segoe UI Semibold", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblSafetyTimer.Location = New System.Drawing.Point(3, 3)
        Me.lblSafetyTimer.Margin = New System.Windows.Forms.Padding(3)
        Me.lblSafetyTimer.Name = "lblSafetyTimer"
        Me.lblSafetyTimer.Size = New System.Drawing.Size(87, 26)
        Me.lblSafetyTimer.TabIndex = 1
        Me.lblSafetyTimer.Text = "Safety Timer:"
        Me.lblSafetyTimer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'osPrefs
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSize = True
        Me.ClientSize = New System.Drawing.Size(210, 268)
        Me.Controls.Add(Me.objContentContainer)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "osPrefs"
        Me.Padding = New System.Windows.Forms.Padding(0, 0, 0, 2)
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Settings"
        Me.TopMost = True
        Me.grpAutoCast.ResumeLayout(False)
        Me.grpAutoCast.PerformLayout()
        Me.objContainer_AutoCast.ResumeLayout(False)
        Me.objContainer_AutoCast.PerformLayout()
        CType(Me.txtAutoCastFuse, System.ComponentModel.ISupportInitialize).EndInit()
        Me.objContentContainer.ResumeLayout(False)
        Me.objContentContainer.PerformLayout()
        Me.grpGenOpts.ResumeLayout(False)
        Me.grpGenOpts.PerformLayout()
        Me.objContainer_GeneralOpts.ResumeLayout(False)
        Me.objContainer_GeneralOpts.PerformLayout()
        Me.grpAutoPass.ResumeLayout(False)
        Me.grpAutoPass.PerformLayout()
        Me.objContainer_AutoPass.ResumeLayout(False)
        Me.objContainer_AutoPass.PerformLayout()
        CType(Me.txtAutoPassSafetyTimer, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents grpAutoCast As System.Windows.Forms.GroupBox
    Friend WithEvents btnSavePrefs As System.Windows.Forms.Button
    Friend WithEvents objContentContainer As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents grpAutoPass As System.Windows.Forms.GroupBox
    Friend WithEvents objContainer_AutoCast As Forms.TableLayoutPanel
    Friend WithEvents txtAutoCastFuse As Forms.NumericUpDown
    Friend WithEvents lblFuse As Forms.Label
    Friend WithEvents chkAutoCastRTC As Forms.CheckBox
    Friend WithEvents objContainer_AutoPass As Forms.TableLayoutPanel
    Friend WithEvents txtAutoPassSafetyTimer As Forms.NumericUpDown
    Friend WithEvents lblSafetyTimer As Forms.Label
    Friend WithEvents grpGenOpts As Forms.GroupBox
    Friend WithEvents objContainer_GeneralOpts As Forms.TableLayoutPanel
    Friend WithEvents lblVisualQuality As Forms.Label
    Friend WithEvents lstVisualQuality As Forms.ComboBox
End Class

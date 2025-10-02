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
        GroupBox1 = New System.Windows.Forms.GroupBox()
        chkAutoCastRTC = New System.Windows.Forms.CheckBox()
        txtAutoCastFuse = New System.Windows.Forms.NumericUpDown()
        Label1 = New System.Windows.Forms.Label()
        btnSavePrefs = New System.Windows.Forms.Button()
        TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
        GroupBox2 = New System.Windows.Forms.GroupBox()
        txtAutoPassSafetyTimer = New System.Windows.Forms.NumericUpDown()
        Label2 = New System.Windows.Forms.Label()
        GroupBox1.SuspendLayout()
        CType(txtAutoCastFuse, ComponentModel.ISupportInitialize).BeginInit()
        TableLayoutPanel1.SuspendLayout()
        GroupBox2.SuspendLayout()
        CType(txtAutoPassSafetyTimer, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top
        GroupBox1.AutoSize = True
        GroupBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        GroupBox1.Controls.Add(chkAutoCastRTC)
        GroupBox1.Controls.Add(txtAutoCastFuse)
        GroupBox1.Controls.Add(Label1)
        GroupBox1.Font = New System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CByte(0))
        GroupBox1.Location = New System.Drawing.Point(5, 5)
        GroupBox1.Margin = New System.Windows.Forms.Padding(2, 5, 2, 9)
        GroupBox1.MaximumSize = New System.Drawing.Size(0, 104)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Padding = New System.Windows.Forms.Padding(0)
        GroupBox1.Size = New System.Drawing.Size(215, 104)
        GroupBox1.TabIndex = 0
        GroupBox1.TabStop = False
        GroupBox1.Text = "AutoCast"
        ' 
        ' chkAutoCastRTC
        ' 
        chkAutoCastRTC.CheckAlign = System.Drawing.ContentAlignment.MiddleRight
        chkAutoCastRTC.Font = New System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold)
        chkAutoCastRTC.Location = New System.Drawing.Point(4, 67)
        chkAutoCastRTC.Margin = New System.Windows.Forms.Padding(0, 3, 0, 3)
        chkAutoCastRTC.Name = "chkAutoCastRTC"
        chkAutoCastRTC.Size = New System.Drawing.Size(208, 28)
        chkAutoCastRTC.TabIndex = 3
        chkAutoCastRTC.Text = "Release To Cast"
        chkAutoCastRTC.UseVisualStyleBackColor = True
        ' 
        ' txtAutoCastFuse
        ' 
        txtAutoCastFuse.Font = New System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CByte(0))
        txtAutoCastFuse.Increment = New Decimal(New Integer() {5, 0, 0, 0})
        txtAutoCastFuse.Location = New System.Drawing.Point(120, 24)
        txtAutoCastFuse.Margin = New System.Windows.Forms.Padding(4, 3, 4, 3)
        txtAutoCastFuse.Maximum = New Decimal(New Integer() {1000, 0, 0, 0})
        txtAutoCastFuse.Name = "txtAutoCastFuse"
        txtAutoCastFuse.Size = New System.Drawing.Size(91, 26)
        txtAutoCastFuse.TabIndex = 2
        txtAutoCastFuse.TextAlign = HorizontalAlignment.Center
        ' 
        ' Label1
        ' 
        Label1.BackColor = System.Drawing.Color.Transparent
        Label1.Font = New System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CByte(0))
        Label1.Location = New System.Drawing.Point(0, 24)
        Label1.Margin = New System.Windows.Forms.Padding(0)
        Label1.Name = "Label1"
        Label1.Padding = New System.Windows.Forms.Padding(5, 0, 0, 0)
        Label1.Size = New System.Drawing.Size(58, 27)
        Label1.TabIndex = 0
        Label1.Text = "Fuse:"
        Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        ' 
        ' btnSavePrefs
        ' 
        btnSavePrefs.AutoSize = True
        btnSavePrefs.Dock = System.Windows.Forms.DockStyle.Fill
        btnSavePrefs.Font = New System.Drawing.Font("Trebuchet MS", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CByte(0))
        btnSavePrefs.Location = New System.Drawing.Point(4, 201)
        btnSavePrefs.Margin = New System.Windows.Forms.Padding(4, 3, 4, 3)
        btnSavePrefs.Name = "btnSavePrefs"
        btnSavePrefs.Size = New System.Drawing.Size(217, 51)
        btnSavePrefs.TabIndex = 1
        btnSavePrefs.Text = "Save"
        btnSavePrefs.UseVisualStyleBackColor = True
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.ColumnCount = 1
        TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0F))
        TableLayoutPanel1.Controls.Add(GroupBox2, 0, 1)
        TableLayoutPanel1.Controls.Add(btnSavePrefs, 0, 2)
        TableLayoutPanel1.Controls.Add(GroupBox1, 0, 0)
        TableLayoutPanel1.Location = New System.Drawing.Point(10, 6)
        TableLayoutPanel1.Margin = New System.Windows.Forms.Padding(4, 3, 4, 3)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 3
        TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle())
        TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle())
        TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39.0F))
        TableLayoutPanel1.Size = New System.Drawing.Size(225, 255)
        TableLayoutPanel1.TabIndex = 2
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Anchor = System.Windows.Forms.AnchorStyles.Top
        GroupBox2.AutoSize = True
        GroupBox2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        GroupBox2.Controls.Add(txtAutoPassSafetyTimer)
        GroupBox2.Controls.Add(Label2)
        GroupBox2.Font = New System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CByte(0))
        GroupBox2.Location = New System.Drawing.Point(5, 120)
        GroupBox2.Margin = New System.Windows.Forms.Padding(2, 2, 2, 9)
        GroupBox2.MaximumSize = New System.Drawing.Size(0, 69)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Padding = New System.Windows.Forms.Padding(0)
        GroupBox2.Size = New System.Drawing.Size(215, 69)
        GroupBox2.TabIndex = 3
        GroupBox2.TabStop = False
        GroupBox2.Text = "AutoPass"
        ' 
        ' txtAutoPassSafetyTimer
        ' 
        txtAutoPassSafetyTimer.Font = New System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CByte(0))
        txtAutoPassSafetyTimer.Increment = New Decimal(New Integer() {5, 0, 0, 0})
        txtAutoPassSafetyTimer.Location = New System.Drawing.Point(120, 24)
        txtAutoPassSafetyTimer.Margin = New System.Windows.Forms.Padding(4, 3, 4, 3)
        txtAutoPassSafetyTimer.Maximum = New Decimal(New Integer() {3000, 0, 0, 0})
        txtAutoPassSafetyTimer.Name = "txtAutoPassSafetyTimer"
        txtAutoPassSafetyTimer.Size = New System.Drawing.Size(91, 26)
        txtAutoPassSafetyTimer.TabIndex = 2
        txtAutoPassSafetyTimer.TextAlign = HorizontalAlignment.Center
        ' 
        ' Label2
        ' 
        Label2.BackColor = System.Drawing.Color.Transparent
        Label2.Font = New System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CByte(0))
        Label2.Location = New System.Drawing.Point(0, 24)
        Label2.Margin = New System.Windows.Forms.Padding(0)
        Label2.Name = "Label2"
        Label2.Padding = New System.Windows.Forms.Padding(5, 0, 0, 0)
        Label2.Size = New System.Drawing.Size(117, 27)
        Label2.TabIndex = 0
        Label2.Text = "Safety Timer:"
        Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        ' 
        ' osPrefs
        ' 
        AutoScaleDimensions = New System.Drawing.SizeF(7.0F, 15.0F)
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        AutoSize = True
        ClientSize = New System.Drawing.Size(245, 264)
        Controls.Add(TableLayoutPanel1)
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Margin = New System.Windows.Forms.Padding(4, 3, 4, 3)
        MaximizeBox = False
        MinimizeBox = False
        Name = "osPrefs"
        Padding = New System.Windows.Forms.Padding(7, 2, 7, 6)
        ShowIcon = False
        ShowInTaskbar = False
        SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Text = "Settings"
        TopMost = True
        GroupBox1.ResumeLayout(False)
        CType(txtAutoCastFuse, ComponentModel.ISupportInitialize).EndInit()
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel1.PerformLayout()
        GroupBox2.ResumeLayout(False)
        CType(txtAutoPassSafetyTimer, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)

    End Sub

    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents chkAutoCastRTC As System.Windows.Forms.CheckBox
    Friend WithEvents btnSavePrefs As System.Windows.Forms.Button
    Friend WithEvents txtAutoCastFuse As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents txtAutoPassSafetyTimer As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label2 As System.Windows.Forms.Label
End Class

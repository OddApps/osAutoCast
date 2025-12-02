Imports System.Data
Imports System.Windows.Forms
Imports osAutoCast.DataTypeLib.PromptResponse

Public Class osPrefs

    Private isSaved As Boolean = False

    Private objPrefTracker As osPrefTracker(Of osPrefStore)

    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WM_MOUSEACTIVATE As Integer = &H21
    Private Const MA_NOACTIVATE As Integer = 3
    Private Const WS_EX_TOOLWINDOW As Integer = &H80

    Private objPrefVQ_DataSet As DataSet

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            If Not DesignMode Then
                cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
            End If
            Return cp
        End Get
    End Property

    Protected Overrides Sub WndProc(ByRef m As Message)
        If DetectGameUI.FocusMTGA() Then
            If Not DesignMode AndAlso m.Msg = WM_MOUSEACTIVATE Then
                m.Result = CType(MA_NOACTIVATE, IntPtr)
                Return
            End If
        End If

        MyBase.WndProc(m)
    End Sub

    Public Sub osPrefsPrep()

        Dim lstPrefVQ As New osPref_DataTable

        With lstVisualQuality
            .DisplayMember = "vqName"
            .ValueMember = "vqIdx"
            .DataSource = lstPrefVQ.osPrefVQ_DT
        End With

        txtAutoCastFuse.DataBindings.
            Add(New Binding(CoreDataLib.osPrefStoreData.GetPrefBindDefs("acFuse").ControlProp,
                            CoreDataLib.osPrefStoreData, CoreDataLib.osPrefStoreData.GetPrefBindDefs("acFuse").DataProp,
                            False, DataSourceUpdateMode.OnPropertyChanged))

        chkAutoCastRTC.DataBindings.
            Add(New Binding(CoreDataLib.osPrefStoreData.GetPrefBindDefs("acRTC").ControlProp,
                            CoreDataLib.osPrefStoreData, CoreDataLib.osPrefStoreData.GetPrefBindDefs("acRTC").DataProp,
                            False, DataSourceUpdateMode.OnPropertyChanged))

        txtAutoPassSafetyTimer.DataBindings.
            Add(New Binding(CoreDataLib.osPrefStoreData.GetPrefBindDefs("apSafetyTimer").ControlProp,
                            CoreDataLib.osPrefStoreData, CoreDataLib.osPrefStoreData.GetPrefBindDefs("apSafetyTimer").DataProp,
                            False, DataSourceUpdateMode.OnPropertyChanged))

        lstVisualQuality.DataBindings.
            Add(New Binding(CoreDataLib.osPrefStoreData.GetPrefBindDefs("goVisualQuality").ControlProp,
                            CoreDataLib.osPrefStoreData, CoreDataLib.osPrefStoreData.GetPrefBindDefs("goVisualQuality").DataProp,
                            False, DataSourceUpdateMode.OnPropertyChanged))

        objPrefTracker = New osPrefTracker(Of osPrefStore)(CoreDataLib.osPrefStoreData)

    End Sub

    Private Sub SavePrefs(sender As Object, e As EventArgs) Handles btnSavePrefs.Click
        If objPrefTracker.HasChanges Then
            Dim chkDoSave = GetResponse(PromptType.Prefs_Save)

            If chkDoSave = isYes Then
                CoreDataLib.osPrefIndex.SavePrefsFile()
                objPrefTracker.HasChanges()
                isSaved = True

                Me.Close()
            Else
                objPrefTracker.Revert()
            End If
        End If
    End Sub

    Private Sub ClosePrefs(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If objPrefTracker.HasChanges Then
            If Not isSaved Then
                Select Case GetResponse(PromptType.Prefs_Close)
                    Case isYes
                        CoreDataLib.osPrefIndex.SavePrefsFile()
                    Case isNo
                        objPrefTracker.Revert()
                    Case isCancel
                        e.Cancel = True
                End Select
            End If
        End If
    End Sub

    Private Sub txtAutoCastFuse_MouseWheel(sender As Object, e As MouseEventArgs) Handles txtAutoCastFuse.MouseWheel
        Dim objVal_ACF = DirectCast(sender, NumericUpDown)

        If e.Delta > 0 Then
            objVal_ACF.Value = Math.Min(objVal_ACF.Maximum, objVal_ACF.Value + 10D)
        ElseIf e.Delta < 0 Then
            objVal_ACF.Value = Math.Max(objVal_ACF.Minimum, objVal_ACF.Value - 10D)
        End If

        CType(e, HandledMouseEventArgs).Handled = True
    End Sub

End Class
Imports System.Data
Imports System.Windows.Forms
Imports System.Windows.Threading
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

    'Public Function BuildPrefBindingsAsync() As Task(Of List(Of BindingDef))
    '    Return Task.Run(
    '        Function()
    '            Dim objPrefStoreData = CoreDataLib.osPrefStoreData

    '            Return New List(Of BindingDef) From {
    '                ComposeBindingDef(objPrefStoreData, txtAutoCastFuse, "acFuse", objPrefStoreData),
    '                ComposeBindingDef(objPrefStoreData, chkAutoCastRTC, "acRTC", objPrefStoreData),
    '                ComposeBindingDef(objPrefStoreData, txtAutoPassSafetyTimer, "apSafetyTimer", objPrefStoreData),
    '                ComposeBindingDef(objPrefStoreData, lstVisualQuality, "goVisualQuality", objPrefStoreData)
    '           }
    '        End Function)
    'End Function

    Public Function BuildPrefBindingInfoAsync() As Task(Of List(Of String))
        Return Task.Run(
        Function()
            Return New List(Of String) From {
                "acFuse",
                "acRTC",
                "apSafetyTimer",
                "goVisualQuality"
            }
        End Function)
    End Function


    Private _prefControlMap As New Dictionary(Of String, Control) From {
    {"acFuse", txtAutoCastFuse},
    {"acRTC", chkAutoCastRTC},
    {"apSafetyTimer", txtAutoPassSafetyTimer},
    {"goVisualQuality", lstVisualQuality}
}
    Public Async Function osPrefsPrepAsync(
    prefNamesTask As Task(Of List(Of String))
) As Task

        Dim prefNames = Await prefNamesTask

        If Me.InvokeRequired Then
            Await SwitchToUiThreadAsync()
            Await osPrefsPrepAsync(Task.FromResult(prefNames))
            Return
        End If

        Await osPrefsPrepAsync_UI(prefNames)
    End Function

    Private Function SwitchToUiThreadAsync() As Task
        Dim tcs As New TaskCompletionSource(Of Boolean)

        Me.BeginInvoke(
        Sub()
            tcs.SetResult(True)
        End Sub)

        Return tcs.Task
    End Function

    Private Async Function osPrefsPrepAsync_UI(prefNames As List(Of String)) As Task

        Me.SuspendLayout()

        Try
            Dim lstPrefVQ As New osPref_DataTable

            With lstVisualQuality
                .DisplayMember = "vqName"
                .ValueMember = "vqIdx"
                .DataSource = lstPrefVQ.osPrefVQ_DT
            End With

            For Each prefName In prefNames
                Dim def = CoreDataLib.osPrefStoreData.GetPrefBindDefs(prefName)
                Dim ctrl = ResolveControl(prefName)

                ctrl.DataBindings.Add(
                New Binding(def.ControlProp,
                            CoreDataLib.osPrefStoreData,
                            def.DataProp,
                            False,
                            DataSourceUpdateMode.OnPropertyChanged))
            Next

            objPrefTracker =
            New osPrefTracker(Of osPrefStore)(
                CoreDataLib.osPrefStoreData)

        Finally
            Me.ResumeLayout()
        End Try

        Await Task.CompletedTask
    End Function




    Private Function ResolveControl(prefName As String) As Control
        Return _prefControlMap(prefName)
    End Function

    Private Function ComposeBindingDef(objPrefStore As osPrefStore, objCtrl As Control,
                                       objPrefName As String, objDataSrc As Object) As BindingDef
        Return New BindingDef With {.Control = objCtrl, .DataSource = objDataSrc,
            .ControlProp = objPrefStore.GetPrefBindDefs(objPrefName).ControlProp,
            .DataProp = objPrefStore.GetPrefBindDefs(objPrefName).DataProp
        }
    End Function

    Public Async Function osPrefsPrepAsync2(objTask_BindPrefLst As Task(Of List(Of BindingDef))) As Task
        Dim objBindPrefLst = Await objTask_BindPrefLst
        Me.SuspendLayout()

        Try

            Dim lstPrefVQ As New osPref_DataTable

            With lstVisualQuality
                .DisplayMember = "vqName"
                .ValueMember = "vqIdx"
                .DataSource = lstPrefVQ.osPrefVQ_DT
            End With

            For Each prefDef In objBindPrefLst
                With prefDef
                    .Control.DataBindings.Add(
                        New Binding(.ControlProp, .DataSource, .DataProp,
                                    False, DataSourceUpdateMode.OnPropertyChanged))
                End With
            Next

            objPrefTracker = New osPrefTracker(Of
                osPrefStore)(CoreDataLib.osPrefStoreData)
        Finally
            Me.ResumeLayout()
        End Try

    End Function

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

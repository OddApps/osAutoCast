Imports System.ComponentModel
Imports System.Windows.Forms

Public Class osPrefs

    Private isSaved As Boolean = False

    Private objPrefTracker As osPrefTracker(Of osPrefStore)

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            If Not DesignMode Then
                cp.ExStyle = cp.ExStyle Or &H8000000 Or &H80 ' WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
            End If
            Return cp
        End Get
    End Property

    Protected Overrides Sub WndProc(ByRef m As Message)
        Const WM_MOUSEACTIVATE As Integer = &H21
        Const MA_NOACTIVATE As Integer = 3

        If Not DesignMode AndAlso m.Msg = WM_MOUSEACTIVATE Then
            m.Result = CType(MA_NOACTIVATE, IntPtr)
            Return
        End If

        MyBase.WndProc(m)
    End Sub

    Public Sub osPrefsPrep()

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

        objPrefTracker = New osPrefTracker(Of osPrefStore)(CoreDataLib.osPrefStoreData)

    End Sub

    Private Sub btnSavePrefs_Click(sender As Object, e As EventArgs) Handles btnSavePrefs.Click
        If objPrefTracker.HasChanges Then
            Dim chkDoSave = ConfirmPromptResponse(PromptType.isSave)

            If chkDoSave = DialogResult.Yes Then
                CoreDataLib.osPrefIndex.SavePrefsFile()
                objPrefTracker.HasChanges()
                isSaved = True

                Me.Close()
            Else
                objPrefTracker.Revert()
            End If
        End If
    End Sub

    Private Sub osPrefs_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If objPrefTracker.HasChanges Then
            If Not isSaved Then
                Dim chkDoClose = ConfirmPromptResponse(PromptType.isClose)

                Select Case chkDoClose
                    Case DialogResult.Yes
                        CoreDataLib.osPrefIndex.SavePrefsFile()
                    Case DialogResult.No
                        objPrefTracker.Revert()
                    Case DialogResult.Cancel
                        e.Cancel = True
                End Select

            End If
        End If
    End Sub

    Private Function ConfirmPromptResponse(pType As PromptType) As DialogResult
        With New PromptData(pType)
            Dim chkPromptResponse = NoActivateMsgBox.ShowNoActivate(.Msg, .Title, .MsgType)
            Return chkPromptResponse
        End With
    End Function



End Class
Imports System.ComponentModel
Imports System.Windows.Forms

Public Class osPrefs

    Private prefOld_RTC As Boolean
    Private prefOld_Fuse As Integer

    Private isSaved As Boolean = False

    Private objPrefTracker As osPrefTracker(Of osPrefStore)

    Private Sub osPrefs_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        txtAutoCastFuse.DataBindings.Add(New Binding(CoreDataLib.osPrefStoreData.GetPrefBindDefs("acFuse").ControlProp,
                                                     CoreDataLib.osPrefStoreData, CoreDataLib.osPrefStoreData.GetPrefBindDefs("acFuse").DataProp,
                                                     False, DataSourceUpdateMode.OnPropertyChanged))

        chkAutoCastRTC.DataBindings.Add(New Binding(CoreDataLib.osPrefStoreData.GetPrefBindDefs("acRTC").ControlProp,
                                                    CoreDataLib.osPrefStoreData, CoreDataLib.osPrefStoreData.GetPrefBindDefs("acRTC").DataProp,
                                                    False, DataSourceUpdateMode.OnPropertyChanged))

        txtAutoPassSafetyTimer.DataBindings.Add(New Binding(CoreDataLib.osPrefStoreData.GetPrefBindDefs("apSafetyTimer").ControlProp,
                                                            CoreDataLib.osPrefStoreData, CoreDataLib.osPrefStoreData.GetPrefBindDefs("apSafetyTimer").DataProp,
                                                            False, DataSourceUpdateMode.OnPropertyChanged))

        objPrefTracker = New osPrefTracker(Of osPrefStore)(CoreDataLib.osPrefStoreData)

    End Sub

    Private Sub btnSavePrefs_Click(sender As Object, e As EventArgs) Handles btnSavePrefs.Click
        If objPrefTracker.HasChanges Then
            Dim chkDoSave = MsgBox("Confirm Saving To Preferences?", vbYesNo, "Save Preferences")

            If chkDoSave = vbYes Then
                CoreDataLib.osPrefIndex.SavePrefsFile()
                objPrefTracker.HasChanges()
                isSaved = True
                Me.Close()
            Else
                objPrefTracker.Revert()
            End If
        End If
    End Sub

    Private Sub osPrefs_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing

        Dim noClose As Boolean = False

        If objPrefTracker.HasChanges Then
            If Not isSaved Then

                Select Case MsgBox("Settings have been changed... Save Changes?",
                               vbYesNoCancel, "Save Changes")
                    Case vbYes
                        CoreDataLib.osPrefIndex.SavePrefsFile()
                    Case vbNo
                        objPrefTracker.Revert()
                    Case vbCancel
                        e.Cancel = True
                End Select

            End If
        End If

        e.Cancel = True
        Me.Hide()
    End Sub

End Class
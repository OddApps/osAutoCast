Imports System.ComponentModel
Imports System.Data
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.PromptResponse

Public Class osPrefs_GUI

    Private isSaved As Boolean = False

    Private objPrefTracker As osPrefTracker(Of osPrefStore)

    Private Sub osPrefs_GUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '   osPreferences.PopulateDataVQ()
        Me.DataContext = CoreDataLib.osPrefStoreData
    End Sub




    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
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

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        CoreDataLib.SetWinOpts(CoreDataLib.GetWinHwnd(Me))
    End Sub

    Private Sub osPrefs_TitleBar_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles osPrefs_TitleBar.MouseLeftButtonDown
        If e.LeftButton = MouseButtonState.Pressed Then
            DragMove()
        End If
    End Sub
End Class

Partial Public Class osPrefs_GUI
    Implements INotifyPropertyChanged


    Private _osPrefVQ_DT As DataTable

    Public ReadOnly Property OsPrefsVQ_DT As DataTable
        Get
            Return _osPrefVQ_DT
        End Get
    End Property

    Public Sub PopulateDataVQ()
        Dim lstPrefVQ As New osPref_DataTable

        _osPrefVQ_DT = lstPrefVQ.osPrefVQ_DT
    End Sub


    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub OnPropertyChanged(<CallerMemberName> Optional name As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
    End Sub

End Class


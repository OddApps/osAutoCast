Imports System.ComponentModel
Imports System.Data
Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms
Imports osAutoCast.DataTypeLib.PrefBinder

Public Class osPrefStore
    Implements INotifyPropertyChanged

    Public osPrefStoreBindings As Dictionary(Of String, Binding)
    Private _AutoPass_SafetyTimer As Integer
    Private _AutoCast_Fuse As Integer
    Private _AutoCast_RTC As Boolean
    Private _MainOpts_apProgH As Integer
    Private _MainOpts_apProgW As Integer
    Private _MainOpts_apUiH As Integer
    Private _MainOpts_apUiW As Integer
    Private _MainOpts_acProgH As Integer
    Private _MainOpts_acProgW As Integer
    Private _GenOpts_VisualQuality As String

    Private PrefBinderIdx As New Dictionary(Of PrefBinder, PrefBindData) From {
        {AC_Fuse, New PrefBindData("Text", "AutoCast_Fuse")},
        {AC_RTC, New PrefBindData("Checked", "AutoCast_RTC")},
        {AP_SafetyTimer, New PrefBindData("Text", "AutoPass_SafetyTimer")},
        {GO_VisualQuality, New PrefBindData("SelectedValue", "GenOpts_VisualQuality")}
    }



    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub OnPropertyChanged(Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Public Function GetPrefBindDefs() As Dictionary(Of String, PrefBindingDef)
        Return New Dictionary(Of String, PrefBindingDef) From {
            {"acFuse", New PrefBindingDef With {.ControlProp = PrefBinderIdx(AC_Fuse).BindType, .DataProp = PrefBinderIdx(AC_Fuse).BindRecord}},
            {"apSafetyTimer", New PrefBindingDef With {.ControlProp = PrefBinderIdx(AP_SafetyTimer).BindType, .DataProp = PrefBinderIdx(AP_SafetyTimer).BindRecord}},
            {"acRTC", New PrefBindingDef With {.ControlProp = PrefBinderIdx(AC_RTC).BindType, .DataProp = PrefBinderIdx(AC_RTC).BindRecord}},
            {"goVisualQuality", New PrefBindingDef With {.ControlProp = PrefBinderIdx(GO_VisualQuality).BindType, .DataProp = PrefBinderIdx(GO_VisualQuality).BindRecord}}
        }
    End Function

    'Private Function PopulateBinding(pBinder As PrefBinder) As Binding
    '    With PrefBinderIdx(pBinder)
    '        Return New Binding(.BindType, CoreDataLib.osPrefStoreData,
    '                           .BindRecord, False, DataSourceUpdateMode.OnPropertyChanged)
    '    End With
    'End Function

    Public Sub GenPrefBinds()
        osPrefStoreBindings = New Dictionary(Of String, Binding) From {
            {"acFuse", PopulateBinding(AC_Fuse)},
            {"apSafetyTimer", PopulateBinding(AP_SafetyTimer)},
            {"acRTC", PopulateBinding(AC_RTC)},
            {"goVisualQuality", PopulateBinding(GO_VisualQuality)}
        }
    End Sub

    Private Function PopulateBinding(pBinder As PrefBinder) As System.Windows.Forms.Binding
        With PrefBinderIdx(pBinder)
            ' System.Windows.Forms.Binding(propertyName, dataSource, dataMember, formattingEnabled, DataSourceUpdateMode)
            Return New System.Windows.Forms.Binding(.BindType, CoreDataLib.osPrefStoreData,
                                               .BindRecord, False, DataSourceUpdateMode.OnPropertyChanged)
        End With
    End Function

    ' ---- Create a dictionary of bindings (call on UI thread) ----
    Public Function CreatePrefBindings() As Dictionary(Of String, System.Windows.Forms.Binding)
        Return New Dictionary(Of String, System.Windows.Forms.Binding) From {
        {"acFuse", PopulateBinding(AC_Fuse)},
        {"apSafetyTimer", PopulateBinding(AP_SafetyTimer)},
        {"acRTC", PopulateBinding(AC_RTC)},
        {"goVisualQuality", PopulateBinding(GO_VisualQuality)}
    }
    End Function

    Public Sub UpdatePrefStore()
        Try
            For Each pBind As Binding In osPrefStoreBindings.Values
                With GenPrefObj(pBind)
                    CoreDataLib.osPrefIndex.SavePref(.pType, .pName, Convert.ToString(.pVal))
                End With
            Next
        Catch ex As Exception

        End Try
    End Sub

    Public Function GetBindingValue(pBind As Binding) As Object
        Dim propertyInfo As PropertyInfo = pBind.DataSource.
            GetType().GetProperty(pBind.BindingMemberInfo.BindingField)

        If propertyInfo Is Nothing Then Return Nothing
        Return propertyInfo.GetValue(pBind.DataSource)
    End Function

    Public Function GenPrefObj(pBind As Binding) As PrefStoreData
        Dim strArray As String() = pBind.BindingMemberInfo.BindingField.Split("_"c)
        Return New PrefStoreData(strArray(0), strArray(1), GetBindingValue(pBind))
    End Function

    Public Property AutoPass_SafetyTimer As Integer
        Get
            Return _AutoPass_SafetyTimer
        End Get
        Set(value As Integer)
            If _AutoPass_SafetyTimer = value Then Return
            _AutoPass_SafetyTimer = value
            OnPropertyChanged(NameOf(AutoPass_SafetyTimer))
        End Set
    End Property

    Public Property AutoCast_Fuse As Integer
        Get
            Return _AutoCast_Fuse
        End Get
        Set(value As Integer)
            If _AutoCast_Fuse = value Then Return
            _AutoCast_Fuse = value
            OnPropertyChanged(NameOf(AutoCast_Fuse))
        End Set
    End Property

    Public Property AutoCast_RTC As Boolean
        Get
            Return _AutoCast_RTC
        End Get
        Set(value As Boolean)
            If _AutoCast_RTC = value Then Return
            _AutoCast_RTC = value
            OnPropertyChanged(NameOf(AutoCast_RTC))
        End Set
    End Property

    Public Property MainOpts_apProgH As Integer
        Get
            Return _MainOpts_apProgH
        End Get
        Set(value As Integer)
            If _MainOpts_apProgH = value Then Return
            _MainOpts_apProgH = value
            OnPropertyChanged(NameOf(MainOpts_apProgH))
        End Set
    End Property

    Public Property MainOpts_apProgW As Integer
        Get
            Return _MainOpts_apProgW
        End Get
        Set(value As Integer)
            If _MainOpts_apProgW = value Then Return
            _MainOpts_apProgW = value
            OnPropertyChanged(NameOf(MainOpts_apProgW))
        End Set
    End Property

    Public Property MainOpts_apUiH As Integer
        Get
            Return _MainOpts_apUiH
        End Get
        Set(value As Integer)
            If _MainOpts_apUiH = value Then Return
            _MainOpts_apUiH = value
            OnPropertyChanged(NameOf(MainOpts_apUiH))
        End Set
    End Property

    Public Property MainOpts_apUiW As Integer
        Get
            Return _MainOpts_apUiW
        End Get
        Set(value As Integer)
            If _MainOpts_apUiW = value Then Return
            _MainOpts_apUiW = value
            OnPropertyChanged(NameOf(MainOpts_apUiW))
        End Set
    End Property

    Public Property MainOpts_acProgH As Integer
        Get
            Return _MainOpts_acProgH
        End Get
        Set(value As Integer)
            If _MainOpts_acProgH = value Then Return
            _MainOpts_acProgH = value
            OnPropertyChanged(NameOf(MainOpts_acProgH))
        End Set
    End Property

    Public Property MainOpts_acProgW As Integer
        Get
            Return _MainOpts_acProgW
        End Get
        Set(value As Integer)
            If _MainOpts_acProgW = value Then Return
            _MainOpts_acProgW = value
            OnPropertyChanged(NameOf(MainOpts_acProgW))
        End Set
    End Property

    Public Property GenOpts_VisualQuality As String
        Get
            Return _GenOpts_VisualQuality
        End Get
        Set(value As String)
            If _GenOpts_VisualQuality = value Then Return
            _GenOpts_VisualQuality = value
            OnPropertyChanged(NameOf(GenOpts_VisualQuality))
        End Set
    End Property

    Public Class PrefBindingDef
        Public Property ControlProp As String
        Public Property DataProp As String
    End Class

    Public Class ProgVisualQualityData

        Public Property vqIdx As Integer
        Public Property vqName As String

        Public Sub New()

        End Sub

        Public Sub New(vIdx As Integer, vName As String)
            vqIdx = vIdx
            vqName = vName
        End Sub

    End Class

    Public Class PrefStoreData
        Public Property pType As String
        Public Property pName As String

        Private _pVal As Object
        Public Property pVal As Object
            Get
                Return _pVal
            End Get
            Set(value As Object)
                _pVal = value
            End Set
        End Property

        Public Sub New()
        End Sub

        Public Sub New(pT As String, pN As String, pV As Object)
            Me.pType = pT
            Me.pName = pN
            Me.pVal = pV
        End Sub
    End Class

End Class

Public Class osPrefTracker(Of T As {Class, INotifyPropertyChanged})

    Private ReadOnly _prefTarget As T
    Private _originalSnapshot As Dictionary(Of String, Object)

    Public Sub New(target As T)
        _prefTarget = target
        PreservePrefs()
    End Sub

    Public Sub PreservePrefs()
        _originalSnapshot = GetPropertySnapshot(_prefTarget)
    End Sub

    Public Sub Revert()
        If _originalSnapshot Is Nothing Then Exit Sub

        For Each kvp In _originalSnapshot
            Dim prop = _prefTarget.GetType().GetProperty(kvp.Key)
            If prop IsNot Nothing AndAlso prop.CanWrite Then
                prop.SetValue(_prefTarget, kvp.Value)
            End If
        Next
    End Sub

    Public Function HasChanges() As Boolean
        Dim current = GetPropertySnapshot(_prefTarget)

        For Each kvp In _originalSnapshot
            If Not Equals(kvp.Value, current(kvp.Key)) Then Return True
        Next

        Return False
    End Function

    Private Function GetPropertySnapshot(instance As T) As Dictionary(Of String, Object)
        Dim dict As New Dictionary(Of String, Object)

        For Each prop In ListPrefProps(instance)
            If prop.CanRead Then dict(prop.Name) = prop.GetValue(instance)
        Next

        Return dict
    End Function

    Private Function ListPrefProps(instance As T) As PropertyInfo()
        Return instance.GetType().GetProperties(BindingFlags.Public Or BindingFlags.Instance)
    End Function

End Class

Class osHandler_Prefs
    Implements IDisposable

    Public Property objPrefIndex As PrefRecordIndex

    Public Sub New()
    End Sub

    Public Async Function LoadPrefs() As Task(Of PrefRecordIndex)
        If Not DoPrefsExist() Then CreateDefaultPrefs()

        CoreDataLib.osPrefStoreData = New osPrefStore
        Return Await PopulatePrefData()
    End Function

    Private Function DoPrefsExist() As Boolean
        Return File.Exists(CoreDataLib.osPrefFile)
    End Function

    Private Sub CreateDefaultPrefs()
        Directory.CreateDirectory(CoreDataLib.osPrefDir)

        File.WriteAllLines(CoreDataLib.osPrefFile, GenerateDefaultData())
    End Sub

    Private Function GenerateDefaultData() As List(Of String)
        Dim prefLines As String() = {
            "PrefCatalog_",
            "|AutoCast-", "RTC:True", "Fuse:450", "-AutoCast|",
            "|AutoPass-", "SafetyTimer:750", "-AutoPass|",
            "|MainOpts-",
            "acProgW:105", "acProgH:22",
            "apUiW:320", "apUiH:105",
            "apProgW:320", "apProgH:28",
            "-MainOpts|",
            "|GenOpts-", "VisualQuality:0", "-GenOpts|",
            "_PrefCatalog"
        }

        Return prefLines.ToList()
    End Function

    Private Async Function PopulatePrefData() As Task(Of PrefRecordIndex)
        Return Await Task.Run(
            Function()
                Dim pRecIdxObj As New PrefRecordIndex

                Dim inCatalog As Boolean = False

                Dim currentData As New List(Of PrefRecordData)
                Dim currentType As String = Nothing

                Dim pFileData = IO.File.ReadAllLines(CoreDataLib.osPrefFile).ToList()

                For Each prefLineData In pFileData.Select(Function(l) l.Trim())
                    If isPrefHeader(prefLineData) Then
                        inCatalog = True
                    ElseIf prefLineData = "_PrefCatalog" Then
                        inCatalog = False
                    ElseIf inCatalog Then
                        If isPrefType(prefLineData) Then
                            currentType = FormatPrefType(prefLineData)
                            currentData = New List(Of PrefRecordData)
                        ElseIf isPrefType(prefLineData, True) Then
                            If VerifyRecordType(currentType, prefLineData) Then
                                pRecIdxObj.CreateRecord(currentType, currentData.ToArray())
                                currentType = Nothing
                            End If
                        ElseIf isPrefData(currentType, prefLineData) Then
                            currentData.Add(New PrefRecordData(prefLineData))
                        End If
                    End If
                Next

                Return pRecIdxObj
            End Function)
    End Function

    Public Async Function LoadPrefsAsync(prefRecIdx As PrefRecordIndex) As Task
        If prefRecIdx Is Nothing Then Return

        Dim objPrefData = Await Task.
            WhenAll(prefRecIdx.RecIdx.SelectMany(
            Function(pRec) pRec.PrefRecord,
            Function(pRec, pRecData)
                Dim objPropInfo = Me.PrefStoreProp(pRec, pRecData)

                Return Task.Run(
                    Function() (objPropInfo,
                        Me.PrepPref(pRecData, objPropInfo.PropertyType)))
            End Function))

        Await PrepDispatcher.InvokeAsync(
            Sub()
                Dim objPrefStore = CoreDataLib.osPrefStoreData

                For Each objPref In objPrefData
                    objPref.Item1.SetValue(objPrefStore, objPref.Item2, Nothing)
                Next

                objPrefStore.GenPrefBinds()
            End Sub)

    End Function

    Private Function PrepPref(pRecData As PrefRecordData, valType As Type) As Object
        Return Convert.ChangeType(pRecData.PrefVal, valType)
    End Function

    Private Function VerifyRecordType(chkType As String, strPrefLine As String) As Boolean
        Return chkType = FormatPrefType(strPrefLine)
    End Function

    Private Function FormatPrefType(strPrefLine As String) As String
        If isPrefType(strPrefLine, True) Then
            Dim dashIdx = strPrefLine.IndexOf("-"c)
            Dim pipeIdx = strPrefLine.IndexOf("|"c)

            If dashIdx = -1 OrElse pipeIdx = -1 OrElse pipeIdx <= dashIdx Then
                Return ""
            End If

            Return strPrefLine.Substring(dashIdx + 1,
                                         pipeIdx - dashIdx - 1)
        Else
            Return strPrefLine.Substring(1, strPrefLine.
                                         IndexOf("-"c) - 1)
        End If

    End Function

    Private Function FormatPrefType(strPrefLine As String, endHeader As Boolean) As String
        Return strPrefLine.Substring(1, strPrefLine.Length - 2)
    End Function

    Private Function isPrefHeader(strPrefLine As String) As Boolean
        Return strPrefLine.EndsWith("_")
    End Function

    Private Function isPrefType(strData As String) As Boolean
        Return strData.StartsWith("|") AndAlso
            strData.Contains("-")
    End Function

    Private Function isPrefType(strData As String, chkClose As Boolean) As Boolean
        Return strData.StartsWith("-") AndAlso
            strData.Contains("|")
    End Function

    Private Function isPrefData(pType As String, pLineData As String) As Boolean
        Return pType IsNot Nothing AndAlso
            pLineData.Contains(":")
    End Function

    Private Function PrefStoreTypes() As Type
        Return CoreDataLib.osPrefStoreData.GetType()
    End Function

    Private Function PrefStoreProp(pRecord As PrefRecord, pRecData As PrefRecordData) As PropertyInfo
        Return PrefStoreTypes().
            GetProperty(FetchPrefVar(pRecord.PrefType, pRecData.PrefName),
                        BindingFlags.Public Or BindingFlags.Instance)
    End Function

    Private Function FetchPrefVar(recType As String, recName As String) As String
        Return $"{recType}_{recName}"
    End Function

#Region "IDisposable Support"
    Private disposedValue As Boolean

    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        Me.disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class
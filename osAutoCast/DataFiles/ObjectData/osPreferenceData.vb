Imports System.IO
Imports osAutoCast.DataTypeLib.PrefType
Imports osAutoCast.DataTypeLib.PrefBinder
Imports osAutoCast.DataTypeLib.PrefSetting
Imports System.Text
Imports System.Reflection
Imports System.ComponentModel
Imports osPrefBind = System.Windows.Data.Binding
Imports System.Windows.Threading
Imports osAutoCast.osControls

Namespace osPrefLib

#Disable Warning BC42353

    Public Class osPreferenceLib
        Implements INotifyPropertyChanged

        Public ReadOnly Property PrefTables As New osPref_DataTable()

        Public Property vqList As New List(Of osPref_DataVQ) From {
            New osPref_DataVQ(0, "Performance"),
            New osPref_DataVQ(1, "Quality")
        }

        Public osPrefBindPrefIdx As Dictionary(Of PrefBinder, osPref_BindDef)
        Public osPrefDataBindings As Dictionary(Of String, Binding)

        Public Sub osPref_GenBinding()
            osPrefDataBindings = New Dictionary(Of String, Binding) From {
            {"acFuse", PopulateBinding(AC_Fuse)},
            {"apSafetyTimer", PopulateBinding(AP_SafetyTimer)},
            {"acRTC", PopulateBinding(AC_RTC)},
            {"goVisualQuality", PopulateBinding(GO_VisualQuality)}
        }
        End Sub

        Public idxPrefBindDeps As New Dictionary(Of String, DependencyProperty) From {
                {"acFuse", osControls.osUpDownTextBox.ValueProperty},
                {"acRTC", CheckBox.IsCheckedProperty},
                {"apSafetyTimer", osControls.osUpDownTextBox.ValueProperty},
                {"goVisualQuality", ComboBox.SelectedValueProperty}
            }

        Public idxPrefBindRecords As New Dictionary(Of PrefBinder, osPref_BindRecord) From {
                {AC_Fuse, New osPref_BindRecord(osControls.osUpDownTextBox.ValueProperty, "AutoCast_Fuse")},
                {AC_RTC, New osPref_BindRecord(CheckBox.IsCheckedProperty, "AutoCast_RTC")},
                {AP_SafetyTimer, New osPref_BindRecord(osControls.osUpDownTextBox.ValueProperty, "AutoPass_SafetyTimer")},
                {GO_VisualQuality, New osPref_BindRecord(ComboBox.SelectedValueProperty, "GenOpts_VisualQuality")}
            }

        Private Function FetchBindRecord(pBinder As PrefBinder) As osPref_BindRecord
            Return idxPrefBindRecords.First(
                Function(bRec)
                    Return bRec.Key = pBinder
                End Function).Value
        End Function

        Public Sub SetBindDef(pBinder As PrefBinder, objBindCtrl As Control)
            With FetchBindRecord(pBinder)
                Dim objPrefB = ComposeBinding(.BindPrefName)

                osPrefBindPrefIdx.Add(pBinder, New osPref_BindDef(.BindProperty, objBindCtrl, objPrefB))
                objBindCtrl.SetBinding(.BindProperty, objPrefB)
            End With
        End Sub

        Private PrefBinderIdx As New Dictionary(Of PrefBinder, osPref_BindDef)

        Private Function PopulateBinding(pBinder As PrefBinder) As osPrefBind
            With idxPrefBindRecords(pBinder)
                Return New osPrefBind(.BindPrefName) With {
                    .Source = Me.Data,
                    .Mode = BindingMode.TwoWay,
                    .UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            End With
        End Function

        Private Function ComposeBinding(pBindName As String) As osPrefBind
            Return New osPrefBind() With {
                .Source = Me.Data,
                .Mode = BindingMode.TwoWay,
                .UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            }
        End Function

        Public Shared idxPrefRecords As PrefRecordIndex


        Private Shared _Data As osPreferenceLib
        Public Shared ReadOnly Property Data As osPreferenceLib
            Get
                If _Data Is Nothing Then
                    _Data = New osPreferenceLib()
                End If
                Return _Data
            End Get
        End Property

        Private Shared _prefsSet As Boolean = False
        Public Property prefsSet As Boolean
            Get
                Return _prefsSet
            End Get
            Set(ByVal value As Boolean)
                _prefsSet = value
            End Set
        End Property

        Private Shared _AutoPass_SafetyTimer As Integer
        Public Property AutoPass_SafetyTimer As Integer
            Get
                If prefsSet Then
                    Return GetPrefValue(Pref_AutoPass_SafetyTimer)
                Else
                    Return _AutoPass_SafetyTimer
                End If
            End Get
            Set(value As Integer)
                If prefsSet Then
                    If GetPrefValue(Pref_AutoPass_SafetyTimer) = value Then Return
                    SetPrefValue(Pref_AutoPass_SafetyTimer, value)
                Else
                    If _AutoPass_SafetyTimer = value Then Return
                    _AutoPass_SafetyTimer = value
                End If

                _AutoPass_SafetyTimer = value
                OnPropertyChanged(NameOf(AutoPass_SafetyTimer))
            End Set
        End Property

        Private Shared _AutoCast_Fuse As Integer
        Public Property AutoCast_Fuse As Integer
            Get
                If prefsSet Then
                    Return GetPrefValue(Pref_AutoCast_Fuse)
                Else
                    Return _AutoCast_Fuse
                End If
            End Get
            Set(value As Integer)
                If prefsSet Then
                    If GetPrefValue(Pref_AutoCast_Fuse) = value Then Return
                    SetPrefValue(Pref_AutoCast_Fuse, value)
                Else
                    If _AutoCast_Fuse = value Then Return
                    _AutoCast_Fuse = value
                End If

                _AutoCast_Fuse = value
                OnPropertyChanged(NameOf(AutoCast_Fuse))
            End Set
        End Property

        Private Shared _AutoCast_RTC As Boolean
        Public Property AutoCast_RTC As Boolean
            Get
                If prefsSet Then
                    Return GetPrefValue(Pref_AutoCast_RTC)
                Else
                    Return _AutoCast_RTC
                End If
            End Get
            Set(value As Boolean)
                If prefsSet Then
                    If GetPrefValue(Pref_AutoCast_RTC) = value Then Return
                    SetPrefValue(Pref_AutoCast_RTC, value)
                Else
                    If _AutoCast_RTC = value Then Return
                    _AutoCast_RTC = value
                End If

                _AutoCast_RTC = value
                OnPropertyChanged(NameOf(AutoCast_RTC))
            End Set
        End Property

        Private Shared _MainOpts_apProgH As Integer = 28
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

        Private Shared _MainOpts_apProgW As Integer = 280
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

        Private Shared _MainOpts_apUiH As Integer = 28
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

        Private Shared _MainOpts_apUiW As Integer = 288
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

        Private Shared _MainOpts_acProgH As Integer
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

        Private Shared _MainOpts_acProgW As Integer
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

        Private Shared _GenOpts_VisualQuality As String
        Public Property GenOpts_VisualQuality As String
            Get
                If prefsSet Then
                    Return GetPrefValue(Pref_GenOpts_VisualQuality)
                Else
                    Return _GenOpts_VisualQuality
                End If
            End Get
            Set(value As String)
                If prefsSet Then
                    If GetPrefValue(Pref_GenOpts_VisualQuality) = value Then Return
                    SetPrefValue(Pref_GenOpts_VisualQuality, value)
                Else
                    If _GenOpts_VisualQuality = value Then Return
                    _GenOpts_VisualQuality = value
                End If

                _GenOpts_VisualQuality = value
                OnPropertyChanged(NameOf(GenOpts_VisualQuality))
            End Set
        End Property

        Private Function FindPreference(pDetails As osPrefDetails) As String
            With pDetails
                Return Me.objOsPrefIdx.PrefRecords.
                    FirstOrDefault(Function(pRec) pRec.RecordType = .prefType).
                        RecordData.FirstOrDefault(
                            Function(recData) recData.PrefName.ToLower() =
                                .prefName.ToLower()).PrefVal
            End With
        End Function

        Private Function SetPreference(pDetails As osPrefDetails, pVal As Object) As String
            With pDetails
                Dim objRecData = Me.objOsPrefIdx.PrefRecords.
                    FirstOrDefault(Function(pRec) pRec.RecordType = .prefType).
                        RecordData.FirstOrDefault(
                            Function(recData) recData.PrefName.ToLower() =
                                .prefName.ToLower())

                objRecData.PrefVal = pVal
            End With
        End Function

        Private Function GetPrefName(prefType As PrefSetting) As String
            With Me.objOsPrefIdx
                Select Case prefType
                    Case Pref_AutoCast_RTC
                        Return "RTC"
                    Case Pref_AutoCast_Fuse
                        Return "Fuse"
                    Case Pref_AutoPass_SafetyTimer
                        Return "SafetyTimer"
                    Case Pref_GenOpts_VisualQuality
                        Return "VisualQuality"
                End Select
            End With
        End Function

        Private Function GetPrefType(prefType As PrefSetting) As PrefType
            With Me.objOsPrefIdx
                Select Case prefType
                    Case Pref_AutoCast_RTC
                        Return Pref_AutoCast
                    Case Pref_AutoCast_Fuse
                        Return Pref_AutoCast
                    Case Pref_AutoPass_SafetyTimer
                        Return Pref_AutoPass
                    Case Pref_GenOpts_VisualQuality
                        Return Pref_GenOpts
                End Select
            End With
        End Function

        Private Function GetPrefDetails(prefType As PrefSetting) As osPrefDetails
            Dim objPrefType = GetPrefType(prefType)
            Dim objPrefName = GetPrefName(prefType)

            Return New osPrefDetails(objPrefType, objPrefName)
        End Function

        Private Function GetPrefValue(prefType As PrefSetting) As Object
            Return FindPreference(GetPrefDetails(prefType))
        End Function

        Private Sub SetPrefValue(prefType As PrefSetting, pVal As Object)
            SetPreference(GetPrefDetails(prefType), pVal)
        End Sub

        Private Shared _objOsPrefIdx As osPrefIndex
        Public Property objOsPrefIdx As osPrefIndex
            Get
                Return _objOsPrefIdx
            End Get
            Set(newStatus As osPrefIndex)
                _objOsPrefIdx = newStatus
            End Set
        End Property

        Public Async Function PreparePrefData() As Task
            objOsPrefIdx = Await Task.Run(
                Async Function()
                    Dim objTask_BuildPrefIdx = BuildPrefIndexAsync()
                    Return Await objTask_BuildPrefIdx

                    '  Return objTask_PrefIdx
                End Function)
        End Function

        Public Async Function BuildPrefIndexAsync() As Task(Of osPrefIndex)
            Dim pRecIdxObj As New osPrefIndex()

            Using fs As New FileStream(CoreDataLib.osPrefFile, FileMode.Open,
                                       FileAccess.Read, FileShare.Read, 1028, True)
                Using sr As New StreamReader(fs, True)

                    Dim inCatalog As Boolean = False
                    Dim currentData As New List(Of PrefDataRecord)()
                    Dim currentType As String = Nothing

                    Dim rawLine As String = Await sr.ReadLineAsync()

                    While rawLine IsNot Nothing
                        Dim prefLineData As String = rawLine.Trim()

                        If isPrefHeader(prefLineData) Then
                            inCatalog = True
                        ElseIf prefLineData = "_PrefCatalog" Then
                            inCatalog = False
                        ElseIf inCatalog Then
                            If isPrefType(prefLineData) Then
                                currentType = FormatPrefType(prefLineData)
                                currentData = New List(Of PrefDataRecord)()
                            ElseIf isPrefType(prefLineData, True) Then
                                If VerifyRecordType(currentType, prefLineData) Then
                                    pRecIdxObj.CreateRecord(currentType, currentData.ToArray())
                                    currentType = Nothing
                                End If
                            ElseIf isPrefData(currentType, prefLineData) Then
                                currentData.Add(New PrefDataRecord(prefLineData))
                            End If
                        End If

                        rawLine = Await sr.ReadLineAsync()
                    End While
                End Using
            End Using

            Return pRecIdxObj
        End Function

        Private Function PrepPref(pRecData As PrefDataRecord, valType As Type) As Object
            Return Convert.ChangeType(pRecData.PrefVal, valType)
        End Function

        Public Async Function ApplyPrefs() As Task
            prefsSet = Await Task.Run(
                Async Function()
                    Dim objTask_ApplyPrefs =
                        From pRec In objOsPrefIdx.PrefRecords
                        From pRecData In pRec.RecordData
                        Select Task.Run(Sub() ApplySetting(
                            osPreferenceLib.Data, pRec, pRecData))

                    Await Task.WhenAll(objTask_ApplyPrefs)

                    Return True
                End Function)
        End Function

        Private Sub ApplySetting(target As Object, pRecord As osPrefRecord, pRecData As PrefDataRecord)

            Dim prop = target.GetType().GetProperty(FetchPrefVar(pRecord.RecordType, pRecData.PrefName), BindingFlags.Public Or BindingFlags.Instance)
            If prop Is Nothing OrElse Not prop.CanWrite Then Return

            Dim targetType = Nullable.GetUnderlyingType(prop.PropertyType)
            If targetType Is Nothing Then
                targetType = prop.PropertyType
            End If

            Dim converted = Convert.ChangeType(pRecData.PrefVal, targetType)
            prop.SetValue(target, converted)
        End Sub

        Private Function PrefStoreTypes() As Type
            Return osPreferenceLib.Data.GetType()
        End Function

        Private Function PrefStoreProp(pRecord As osPrefRecord, pRecData As PrefDataRecord) As PropertyInfo
            Return PrefStoreTypes().
            GetProperty(FetchPrefVar(pRecord.RecordType, pRecData.PrefName),
                        BindingFlags.Public Or BindingFlags.Instance)
        End Function

        Private Function FetchPrefVar(recType As PrefType, recName As String) As String
            Return $"{recType.ToString().Replace("Pref_", "")}_{recName}"
        End Function

        Private Function isPrefHeader(strPrefLine As String) As Boolean
            Return strPrefLine.EndsWith("_")
        End Function

        Private Function isPrefType(strData As String) As Boolean
            Return strData.StartsWith("|") AndAlso
            strData.Contains("-")
        End Function

        Private Function FormatPrefType(strPrefLine As String) As PrefType
            If isPrefType(strPrefLine, True) Then
                Dim dashIdx = strPrefLine.IndexOf("-"c)
                Dim pipeIdx = strPrefLine.IndexOf("|"c)

                If dashIdx = -1 OrElse pipeIdx = -1 OrElse pipeIdx <= dashIdx Then
                    Return ""
                End If

                Dim objTypePref = strPrefLine.Substring(dashIdx + 1,
                                         pipeIdx - dashIdx - 1)
                Dim result As PrefType

                If [Enum].TryParse($"Pref_{objTypePref}", True, result) Then
                    Return result
                End If

            Else
                Dim objTypePref = strPrefLine.Substring(1, strPrefLine.
                                         IndexOf("-"c) - 1)
                Dim result As PrefType

                If [Enum].TryParse($"Pref_{objTypePref}", True, result) Then
                    Return result
                End If
            End If

        End Function

        Private Function isPrefData(pType As String, pLineData As String) As Boolean
            Return pType IsNot Nothing AndAlso
            pLineData.Contains(":")
        End Function

        Private Function isPrefType(strData As String, chkClose As Boolean) As Boolean
            Return strData.StartsWith("-") AndAlso
            strData.Contains("|")
        End Function

        Private Function VerifyRecordType(chkType As PrefType, strPrefLine As String) As Boolean
            Return chkType = FormatPrefType(strPrefLine)
        End Function

        Public Class osPrefIndex

            Public Class osPref_StoreRecord
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

            Public Property PrefRecords As List(Of osPrefRecord)

            Public Sub New()
                PrefRecords = New List(Of osPrefRecord)()
            End Sub

            Public Sub CreateRecord(pRecType As PrefType, ParamArray pRecord() As PrefDataRecord)
                Me.PrefRecords.Add(New osPrefRecord(pRecType, pRecord.ToArray()))
            End Sub

            Public Sub UpdatePrefStore()
                Try
                    For Each pBind As Binding In Data.osPrefDataBindings.Values
                        With GenPrefObj(pBind)
                            Data.objOsPrefIdx.SavePref(.pType, .pName, Convert.ToString(.pVal))
                        End With
                    Next
                Catch ex As Exception

                End Try
            End Sub

            Public Function GetBindingValue(pBind As osPrefBind) As Object
                If pBind.Source Is Nothing OrElse pBind.Path Is Nothing Then
                    Return Nothing
                End If

                Dim sourceObj As Object = pBind.Source
                Dim propName As String = pBind.Path.Path

                Dim propInfo As PropertyInfo =
        sourceObj.GetType().GetProperty(propName,
            BindingFlags.Public Or BindingFlags.Instance)

                If propInfo Is Nothing Then Return Nothing

                Return propInfo.GetValue(sourceObj)
            End Function

            Public Function GenPrefObj(pBind As Binding) As osPref_StoreRecord
                If pBind.Path Is Nothing Then Return Nothing

                Dim path As String = pBind.Path.Path
                Dim strArray As String() = path.Split("_"c)

                Return New osPref_StoreRecord(
        strArray(0),
        strArray(1),
        GetBindingValue(pBind)
    )
            End Function

            Public Function FetchPref(pRecType As PrefType, pName As String) As String
                Return PrefRecords.
                    FirstOrDefault(Function(pRec) pRec.RecordType =
                    pRecType).RecordData.
                    FirstOrDefault(Function(recData)
                                       Return recData.PrefName.ToLower() = pName.ToLower()
                                   End Function).PrefVal
            End Function

            Public Sub SavePref(pType As String, pName As String, pNewVal As String)
                With GetRecordData(RetrieveRecord(pType), pName)
                    .PrefVal = pNewVal
                End With
            End Sub

            Private Function RetrieveRecord(pType As PrefType) As osPrefRecord
                Return PrefRecords.
                    FirstOrDefault(Function(r)
                                       Return r.RecordType = pType
                                   End Function)
            End Function

            Private Function GetRecordData(pRecord As osPrefRecord, pName As String) As PrefDataRecord
                Return pRecord.RecordData.FirstOrDefault(
                    Function(d)
                        Return String.Equals(d.PrefName, pName,
                                             StringComparison.OrdinalIgnoreCase)
                    End Function)
            End Function

            Public Sub SavePrefsFile()

                Data.objOsPrefIdx.UpdatePrefStore()

                Using pWriter As New System.IO.StreamWriter(CoreDataLib.osPrefFile, False)
                    pWriter.WriteLine("PrefCatalog_")

                    For Each prefRec As osPrefRecord In Me.PrefRecords
                        WritePrefRecords(prefRec, pWriter)
                    Next

                    pWriter.WriteLine("_PrefCatalog")
                End Using
            End Sub

            Private Function GetPrefType(objPref As PrefType) As String
                Return objPref.ToString().Replace("Pref_", "")
            End Function

            Private Sub WritePrefRecords(pRecord As osPrefRecord, ByRef objPrefWriter As StreamWriter)
                objPrefWriter.WriteLine($"|{GetPrefType(pRecord.RecordType)}-")

                For Each prefRec In pRecord.RecordData
                    objPrefWriter.WriteLine(FormatPrefData(prefRec))
                Next

                objPrefWriter.WriteLine($"-{GetPrefType(pRecord.RecordType)}|")
            End Sub

            Private Function FormatPrefData(prefRec As PrefDataRecord) As String
                Return $"{prefRec.PrefName}:{prefRec.PrefVal}"
            End Function

        End Class

        Public Class osPref_BindRecord
            Public Property BindProperty As DependencyProperty
            Public Property BindPrefName As String

            Public Sub New()
            End Sub

            Public Sub New(objBProp As DependencyProperty, objBPrefN As String)
                BindProperty = objBProp
                BindPrefName = objBPrefN
            End Sub

        End Class

        Public Class osPref_BindDef
            Public Property BindProperty As DependencyProperty
            Public Property BindCtrl As Control
            Public Property BindPref As osPrefBind

            Public Sub New()
            End Sub

            Public Sub New(objBProp As DependencyProperty, objBC As Control, objBPref As osPrefBind)
                BindProperty = objBProp
                BindCtrl = objBC
                BindPref = objBPref
            End Sub

        End Class

        Public Class osPrefRecord

            Public Property RecordType As PrefType
            Public Property RecordData As List(Of PrefDataRecord)

            Public Sub New(pType As PrefType)
                Me.RecordType = pType
                Me.RecordData = New List(Of PrefDataRecord)()
            End Sub

            Public Sub New(pType As PrefType, ParamArray pRecord() As PrefDataRecord)
                Me.RecordType = pType
                Me.RecordData = New List(Of PrefDataRecord)(pRecord)
            End Sub

        End Class

        Public Class PrefDataRecord

            Public Property PrefName As String
            Public Property PrefVal As String

            Public Sub New()
            End Sub

            Public Sub New(prefLine As String)
                With prefLine.Split({":"c}, 2).ToList()
                    Me.PrefName = .Item(0).Trim()
                    Me.PrefVal = .Item(1).Trim()
                End With
            End Sub

            Public Sub New(pName As String, pVal As String)
                Me.PrefName = pName
                Me.PrefVal = pVal
            End Sub

        End Class

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private Sub OnPropertyChanged(Optional propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub

    End Class

End Namespace



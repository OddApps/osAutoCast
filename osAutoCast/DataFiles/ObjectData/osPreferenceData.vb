Imports System.IO
Imports osAutoCast.DataTypeLib.PrefType
Imports System.Text
Imports System.Reflection
Imports System.ComponentModel

Namespace osPrefLib


    Public Class osPreferenceLib
        Implements INotifyPropertyChanged

        Private Shared _Data As osPreferenceLib
        Public Shared ReadOnly Property Data As osPreferenceLib
            Get
                If _Data Is Nothing Then
                    _Data = New osPreferenceLib()
                End If
                Return _Data
            End Get
        End Property

        Public ReadOnly Property PrefTables As New osPrefStore.osPref_DataTabl()

        Public Shared idxPrefRecords As PrefRecordIndex
        Private Shared _AutoPass_SafetyTimer As Integer
        Private Shared _AutoCast_Fuse As Integer
        Private Shared _AutoCast_RTC As Boolean
        Private Shared _MainOpts_apProgH As Integer
        Private Shared _MainOpts_apProgW As Integer
        Private Shared _MainOpts_apUiH As Integer
        Private Shared _MainOpts_apUiW As Integer
        Private Shared _MainOpts_acProgH As Integer
        Private Shared _MainOpts_acProgW As Integer
        Private Shared _GenOpts_VisualQuality As String

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



        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private Sub OnPropertyChanged(Optional propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub

        Public Async Function PrepLoadPrefs() As Task
            Dim aa = Await LoadPrefFile()
            objOsPrefIdx = Await PrepPrefData(aa)
        End Function

        Private Shared _objOsPrefIdx As osPrefIndex
        Public Property objOsPrefIdx As osPrefIndex
            Get
                Return _objOsPrefIdx
            End Get
            Set(newStatus As osPrefIndex)
                _objOsPrefIdx = newStatus
            End Set
        End Property

        Public Async Function LoadPrefFile() As Task(Of List(Of String))
            Dim lstPrefData As New List(Of String)

            Using objPrefReader As New StreamReader(CoreDataLib.osPrefFile)
                While Not objPrefReader.EndOfStream
                    Dim prefLine = Await objPrefReader.ReadLineAsync()
                    If prefLine IsNot Nothing Then
                        lstPrefData.Add(prefLine)
                    End If
                End While
            End Using

            Return lstPrefData
        End Function

        Public Function PrepPrefData(lstPrefData As List(Of String)) As Task(Of osPrefIndex)
            Return Task.Run(Function()
                                Dim pRecIdxObj As New osPrefIndex

                                Dim inCatalog As Boolean = False

                                Dim currentData As New List(Of PrefDataRecord)
                                Dim currentType As String = Nothing


                                For Each prefLineData In lstPrefData.Select(Function(l) l.Trim())
                                    If isPrefHeader(prefLineData) Then
                                        inCatalog = True
                                    ElseIf prefLineData = "_PrefCatalog" Then
                                        inCatalog = False
                                    ElseIf inCatalog Then
                                        If isPrefType(prefLineData) Then
                                            currentType = FormatPrefType(prefLineData)
                                            currentData = New List(Of PrefDataRecord)
                                        ElseIf isPrefType(prefLineData, True) Then
                                            If VerifyRecordType(currentType, prefLineData) Then
                                                pRecIdxObj.CreateRecord(currentType, currentData.ToArray())
                                                currentType = Nothing
                                            End If
                                        ElseIf isPrefData(currentType, prefLineData) Then
                                            currentData.Add(New PrefDataRecord(prefLineData))
                                        End If
                                    End If
                                Next

                                Return pRecIdxObj
                            End Function)

        End Function

        Private Function PrepPref(pRecData As PrefDataRecord, valType As Type) As Object
            Return Convert.ChangeType(pRecData.PrefVal, valType)
        End Function

        Public Async Function ApplyPrefs(prefRecIdx As osPrefIndex) As Task
            If prefRecIdx Is Nothing Then Return

            Dim objPrefData = Await Task.
            WhenAll(prefRecIdx.PrefRecords.SelectMany(
                Function(pRec) pRec.RecordData,
                    Function(pRec, pRecData)
                        Dim objPropInfo = Me.PrefStoreProp(pRec, pRecData)
                        ApplySetting(osPreferenceLib.Data,)
                        Return Task.Run(
                            Function() (objPropInfo,
                                Me.PrepPref(pRecData, objPropInfo.PropertyType)))
                    End Function))

            Await PrepDispatcher.InvokeAsync(
            Sub()
                Dim objPrefStore = osPreferenceLib.Data

                For Each objPref In objPrefData
                    objPref.Item1.SetValue(objPrefStore, objPref.Item2, Nothing)
                Next

            End Sub)

            Await Task.Delay(175)

        End Function

        Private Sub ApplySetting(target As Object, pRecord As osPrefRecord, pRecData As PrefDataRecord)

            Dim prop = target.GetType().GetProperty(propertyName)
            If prop Is Nothing OrElse Not prop.CanWrite Then Return

            Dim targetType = Nullable.GetUnderlyingType(prop.PropertyType)
            If targetType Is Nothing Then
                targetType = prop.PropertyType
            End If

            Dim converted = Convert.ChangeType(value, targetType)
            prop.SetValue(target, converted)
        End Sub

        Private Function PrefStoreTypes() As Type
            Return CoreDataLib.osPrefStoreData.GetType()
        End Function

        Private Function PrefStoreProp(pRecord As osPrefRecord, pRecData As PrefDataRecord) As PropertyInfo
            Return PrefStoreTypes().
            GetProperty(FetchPrefVar(pRecord.RecordType, pRecData.PrefName),
                        BindingFlags.Public Or BindingFlags.Instance)
        End Function

        Private Function FetchPrefVar(recType As PrefType, recName As String) As String
            Return $"{recType}_{recName}"
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

            Public Property PrefRecords As List(Of osPrefRecord)

            Public Sub New()
                PrefRecords = New List(Of osPrefRecord)()
            End Sub

            Public Sub CreateRecord(pRecType As PrefType, ParamArray pRecord() As PrefDataRecord)
                Me.PrefRecords.Add(New osPrefRecord(pRecType, pRecord.ToArray()))
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

    End Class

End Namespace


'    Public Class osPreferences
'        Implements INotifyPropertyChanged

'        Private Shared _Data As osPreferences
'        Public Shared ReadOnly Property Data As osPreferences
'            Get
'                If _Data Is Nothing Then
'                    _Data = New osPreferences()
'                End If
'                Return _Data
'            End Get
'        End Property

'        Public ReadOnly Property PrefTables As New osPrefStore.osPref_DataTabl()

'        Public idxPrefRecords As PrefRecordIndex
'        Private Shared _AutoPass_SafetyTimer As Integer
'        Private Shared _AutoCast_Fuse As Integer
'        Private Shared _AutoCast_RTC As Boolean
'        Private Shared _MainOpts_apProgH As Integer
'        Private Shared _MainOpts_apProgW As Integer
'        Private Shared _MainOpts_apUiH As Integer
'        Private Shared _MainOpts_apUiW As Integer
'        Private Shared _MainOpts_acProgH As Integer
'        Private Shared _MainOpts_acProgW As Integer
'        Private Shared _GenOpts_VisualQuality As String

'        Public Property AutoPass_SafetyTimer As Integer
'            Get
'                Return _AutoPass_SafetyTimer
'            End Get
'            Set(value As Integer)
'                If _AutoPass_SafetyTimer = value Then Return
'                _AutoPass_SafetyTimer = value
'                OnPropertyChanged(NameOf(AutoPass_SafetyTimer))
'            End Set
'        End Property

'        Public Property AutoCast_Fuse As Integer
'            Get
'                Return _AutoCast_Fuse
'            End Get
'            Set(value As Integer)
'                If _AutoCast_Fuse = value Then Return
'                _AutoCast_Fuse = value
'                OnPropertyChanged(NameOf(AutoCast_Fuse))
'            End Set
'        End Property

'        Public Property AutoCast_RTC As Boolean
'            Get
'                Return _AutoCast_RTC
'            End Get
'            Set(value As Boolean)
'                If _AutoCast_RTC = value Then Return
'                _AutoCast_RTC = value
'                OnPropertyChanged(NameOf(AutoCast_RTC))
'            End Set
'        End Property

'        Public Property MainOpts_apProgH As Integer
'            Get
'                Return _MainOpts_apProgH
'            End Get
'            Set(value As Integer)
'                If _MainOpts_apProgH = value Then Return
'                _MainOpts_apProgH = value
'                OnPropertyChanged(NameOf(MainOpts_apProgH))
'            End Set
'        End Property

'        Public Property MainOpts_apProgW As Integer
'            Get
'                Return _MainOpts_apProgW
'            End Get
'            Set(value As Integer)
'                If _MainOpts_apProgW = value Then Return
'                _MainOpts_apProgW = value
'                OnPropertyChanged(NameOf(MainOpts_apProgW))
'            End Set
'        End Property

'        Public Property MainOpts_apUiH As Integer
'            Get
'                Return _MainOpts_apUiH
'            End Get
'            Set(value As Integer)
'                If _MainOpts_apUiH = value Then Return
'                _MainOpts_apUiH = value
'                OnPropertyChanged(NameOf(MainOpts_apUiH))
'            End Set
'        End Property

'        Public Property MainOpts_apUiW As Integer
'            Get
'                Return _MainOpts_apUiW
'            End Get
'            Set(value As Integer)
'                If _MainOpts_apUiW = value Then Return
'                _MainOpts_apUiW = value
'                OnPropertyChanged(NameOf(MainOpts_apUiW))
'            End Set
'        End Property

'        Public Property MainOpts_acProgH As Integer
'            Get
'                Return _MainOpts_acProgH
'            End Get
'            Set(value As Integer)
'                If _MainOpts_acProgH = value Then Return
'                _MainOpts_acProgH = value
'                OnPropertyChanged(NameOf(MainOpts_acProgH))
'            End Set
'        End Property

'        Public Property MainOpts_acProgW As Integer
'            Get
'                Return _MainOpts_acProgW
'            End Get
'            Set(value As Integer)
'                If _MainOpts_acProgW = value Then Return
'                _MainOpts_acProgW = value
'                OnPropertyChanged(NameOf(MainOpts_acProgW))
'            End Set
'        End Property

'        Public Property GenOpts_VisualQuality As String
'            Get
'                Return _GenOpts_VisualQuality
'            End Get
'            Set(value As String)
'                If _GenOpts_VisualQuality = value Then Return
'                _GenOpts_VisualQuality = value
'                OnPropertyChanged(NameOf(GenOpts_VisualQuality))
'            End Set
'        End Property





'        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

'        Private Sub OnPropertyChanged(Optional propertyName As String = Nothing)
'            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
'        End Sub

'    End Class

'    Public Class PrefRecordIndex

'        Public Property RecIdx As List(Of PrefRecord)

'        Public Sub New()
'            RecIdx = New List(Of PrefRecord)
'        End Sub

'        Public Sub CreateRecord(pRecType As String, ParamArray pRecord() As PrefRecordData)
'            Me.RecIdx.Add(New PrefRecord(pRecType, pRecord.ToArray()))
'        End Sub

'        Public Function FetchPref(pRecType As String, pName As String) As String
'            Return RecIdx.
'            FirstOrDefault(Function(pRec) pRec.PrefType.ToLower() =
'            pRecType.ToLower()).PrefRecord.
'            FirstOrDefault(Function(recData)
'                               Return recData.PrefName.ToLower() = pName.ToLower()
'                           End Function).PrefVal
'        End Function

'        Public Sub SavePref(pType As String, pName As String, pNewVal As String)
'            With GetRecordData(RetrieveRecord(pType), pName)
'                .PrefVal = pNewVal
'            End With
'        End Sub

'        Private Function RetrieveRecord(pType As String) As PrefRecord
'            Return RecIdx.
'            FirstOrDefault(Function(r)
'                               Return r.PrefType.ToLower() = pType.ToLower()
'                           End Function)
'        End Function

'        Private Function GetRecordData(pRecord As PrefRecord, pName As String) As PrefRecordData
'            Return pRecord.PrefRecord.
'            FirstOrDefault(Function(d)
'                               Return d.PrefName.ToLower() = pName.ToLower()
'                           End Function)
'        End Function

'        Public Sub SavePrefsFile()

'            CoreDataLib.osPrefStoreData.UpdatePrefStore()

'            Using pWriter As New System.IO.StreamWriter(CoreDataLib.osPrefFile, False)
'                pWriter.WriteLine("PrefCatalog_")

'                For Each prefRec As PrefRecord In Me.RecIdx
'                    WritePrefRecords(prefRec, pWriter)
'                Next

'                pWriter.WriteLine("_PrefCatalog")
'            End Using
'        End Sub

'        Private Sub SavePrefsToFile()
'            Using pWriter As New System.IO.StreamWriter(CoreDataLib.osPrefFile, False)
'                pWriter.WriteLine("PrefCatalog_")

'                For Each prefRec As PrefRecord In Me.RecIdx
'                    WritePrefRecords(prefRec, pWriter)
'                Next

'                pWriter.WriteLine("_PrefCatalog")
'            End Using
'        End Sub

'        Private Sub WritePrefRecords(pRecord As PrefRecord, ByRef objPrefWriter As StreamWriter)
'            objPrefWriter.WriteLine($"|{pRecord.PrefType}-")

'            For Each prefRec In pRecord.PrefRecord
'                objPrefWriter.WriteLine(FormatPrefData(prefRec))
'            Next

'            objPrefWriter.WriteLine($"-{pRecord.PrefType}|")
'        End Sub

'        Private Function FormatPrefData(prefRec As PrefRecordData) As String
'            Return $"{prefRec.PrefName}:{prefRec.PrefVal}"
'        End Function

'        Private Function PrepPref(pRecData As PrefRecordData, valType As Type) As Object
'            Return Convert.ChangeType(pRecData.PrefVal, valType)
'        End Function

'        Private Function GetPrefTypes() As Type
'            Return GetType(CoreDataLib)
'        End Function

'        Private Function FetchPrefVar(recType As String, recName As String) As String
'            Return $"{recType}_{recName}"
'        End Function

'    End Class

'    Public Class PrefRecord

'        Public Property PrefType As String
'        Public Property PrefRecord As List(Of PrefRecordData)

'        Public Sub New()
'        End Sub

'        Public Sub New(pType As String)
'            Me.PrefType = pType
'            Me.PrefRecord = New List(Of PrefRecordData)
'        End Sub

'        Public Sub New(pType As String, ParamArray pRecord() As PrefRecordData)
'            Me.PrefType = pType
'            Me.PrefRecord = New List(Of PrefRecordData)(pRecord)
'        End Sub

'        Public Sub AddRecordData(pRecData As PrefRecordData)
'            Me.PrefRecord.Add(pRecData)
'        End Sub

'    End Class

'    Public Class PrefRecordData

'        Public Property PrefName As String
'        Public Property PrefVal As String

'        Public Sub New()
'        End Sub

'        Public Sub New(prefLine As String)
'            With prefLine.Split({":"c}, 2).ToList()
'                Me.PrefName = .Item(0).Trim()
'                Me.PrefVal = .Item(1).Trim()
'            End With
'        End Sub

'        Public Sub New(pName As String, pVal As String)
'            Me.PrefName = pName
'            Me.PrefVal = pVal
'        End Sub

'    End Class
'End Class
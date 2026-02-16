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
Imports System.Globalization
Imports osWriter = System.IO.StreamWriter

Namespace osPrefLib

#Disable Warning BC42353

    Public Class osPreferenceLib
        Implements INotifyPropertyChanged

        Public ReadOnly Property PrefTables As New osPref_DataTable()

        Public Property vqList As New List(Of osPref_DataVQ) From {
            New osPref_DataVQ(0, "Performance"),
            New osPref_DataVQ(1, "Quality")
        }

        Private Function ComposeBinding(pBindName As String) As osPrefBind
            Return New osPrefBind() With {
                .Source = Me.Data,
                .Mode = BindingMode.TwoWay,
                .UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            }
        End Function

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

        Private Shared _objOsPrefIdx As osPrefIndex
        Public Property objOsPrefIdx As osPrefIndex
            Get
                Return _objOsPrefIdx
            End Get
            Set(newStatus As osPrefIndex)
                _objOsPrefIdx = newStatus
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

        Private Shared _MainOpts_apProgH As Integer
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

        Private Shared _MainOpts_apProgW As Integer
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

        Private Shared _MainOpts_apUiH As Integer
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

        Private Shared _MainOpts_apUiW As Integer
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

        Private Shared _MainOpts_acProgB As Integer
        Public Property MainOpts_acProgB As Integer
            Get
                Return _MainOpts_acProgB
            End Get
            Set(value As Integer)
                If _MainOpts_acProgB = value Then Return
                _MainOpts_acProgB = value
                OnPropertyChanged(NameOf(MainOpts_acProgB))
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

        Public Function ClearPrefData() As Task
            objOsPrefIdx = Nothing
            Return Task.CompletedTask
        End Function

        Public Async Function PreparePrefData() As Task
            objOsPrefIdx = Await BuildPrefIndexAsync()
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

        Public Async Function ApplyPrefs(Optional token As CancellationToken = Nothing, Optional progress As IProgress(Of Integer) = Nothing) As Task
            Dim changes As New List(Of Action)

            For Each pRec In objOsPrefIdx.PrefRecords
                For Each pRecData In pRec.RecordData
                    Dim rec = pRec
                    Dim data = pRecData

                    changes.Add(
                        Sub()
                            ApplySetting(osPreferenceLib.Data, rec, data)
                            progress?.Report(1)
                        End Sub)
                Next
            Next

            For Each apply In changes
                Await PrepDispatcher().InvokeAsync(
                    apply, DispatcherPriority.Background)
            Next

            prefsSet = True
        End Function

        Private Shared ReadOnly _propCache As New Concurrent.ConcurrentDictionary(Of String, PropertyInfo)
        Private Const osBindFlags As BindingFlags = BindingFlags.Public Or BindingFlags.Instance

        Private Sub ApplySetting(target As Object, pRecord As osPrefRecord, pRecData As PrefDataRecord)
            If target Is Nothing Then Return

            Dim propName = FetchPrefVar(pRecord.RecordType, pRecData.PrefName)
            If String.IsNullOrWhiteSpace(propName) Then Return

            Dim key = target.GetType().FullName & "|" & propName

            Dim prop = _propCache.GetOrAdd(
                key, Function()
                         Return target.GetType().GetProperty(propName, osBindFlags)
                     End Function)

            If prop Is Nothing OrElse Not prop.CanWrite Then Return

            Dim targetType = Nullable.GetUnderlyingType(prop.PropertyType)
            If targetType Is Nothing Then targetType = prop.PropertyType

            Dim converted = Convert.ChangeType(
                pRecData.PrefVal, targetType, CultureInfo.InvariantCulture)

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

            Public Async Function SavePrefsFileAsync() As Task
                Try
                    Using pWriter As New osWriter(CoreDataLib.osPrefFile, False)
                        Await pWriter.WriteLineAsync("PrefCatalog_").ConfigureAwait(False)

                        For Each prefRec As osPrefRecord In Me.PrefRecords
                            Await WritePrefRecordsAsync(prefRec, pWriter).ConfigureAwait(False)
                        Next

                        Await pWriter.WriteLineAsync("_PrefCatalog").ConfigureAwait(False)
                    End Using
                Catch ex As Exception
                    Debug.WriteLine($"SavePrefsFileAsync failed: {ex}")
                    Throw
                End Try
            End Function

            Private Async Function WritePrefRecordsAsync(pRecord As osPrefRecord, objPrefWriter As osWriter) As Task
                Await objPrefWriter.WriteLineAsync($"|{GetPrefType(pRecord.RecordType)}-").ConfigureAwait(False)

                For Each prefRec In pRecord.RecordData
                    Await objPrefWriter.WriteLineAsync(FormatPrefData(prefRec)).ConfigureAwait(False)
                Next

                Await objPrefWriter.WriteLineAsync($"-{GetPrefType(pRecord.RecordType)}|").ConfigureAwait(False)
            End Function

            Private Function GetPrefType(objPref As PrefType) As String
                Return objPref.ToString().Replace("Pref_", "")
            End Function

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

    Public Class osPrefMonitor(Of T As Class)
        Implements IDisposable

        Private _prefsChanged As Boolean
        Private _target As T
        Private _snapshot As Dictionary(Of String, Object)
        Private _disposed As Boolean

        Public ReadOnly Property prefsChanged As Boolean
            Get
                Return _prefsChanged
            End Get
        End Property

        Public Sub Attach(target As T)
            If _disposed Then Throw New ObjectDisposedException(NameOf(osPrefMonitor(Of T)))

            Detach() ' detach previous if any
            _target = target
            _prefsChanged = False
            CaptureSnapshot()

            Dim inpc = TryCast(_target, INotifyPropertyChanged)
            If inpc IsNot Nothing Then
                AddHandler inpc.PropertyChanged, AddressOf OnPropertyChanged
            Else
                Throw New InvalidOperationException("Target does not implement INotifyPropertyChanged.")
            End If
        End Sub

        Public Sub Detach()
            If _target Is Nothing Then Return

            Dim inpc = TryCast(_target, INotifyPropertyChanged)
            If inpc IsNot Nothing Then
                RemoveHandler inpc.PropertyChanged, AddressOf OnPropertyChanged
            End If

            _target = Nothing
            _snapshot = Nothing
            _prefsChanged = False
        End Sub

        Public Sub ResetDirty()
            _prefsChanged = False
        End Sub

        Public Sub Revert()
            If _target Is Nothing OrElse _snapshot Is Nothing Then Return

            Dim tType = GetType(T)
            For Each kvp In _snapshot
                Try
                    Dim pi As PropertyInfo = tType.GetProperty(kvp.Key, BindingFlags.Instance Or BindingFlags.Public)
                    If pi IsNot Nothing AndAlso pi.CanWrite AndAlso pi.GetIndexParameters().Length = 0 Then
                        pi.SetValue(_target, kvp.Value)
                    End If
                Catch ex As Exception
                End Try
            Next

            _prefsChanged = False

            CaptureSnapshot()
        End Sub

        Public Sub RevertProperty(propName As String)
            If _target Is Nothing OrElse _snapshot Is Nothing Then Return
            If Not _snapshot.ContainsKey(propName) Then Return

            Dim tType = GetType(T)
            Try
                Dim pi As PropertyInfo = tType.GetProperty(propName, BindingFlags.Instance Or BindingFlags.Public)
                If pi IsNot Nothing AndAlso pi.CanWrite AndAlso pi.GetIndexParameters().Length = 0 Then
                    pi.SetValue(_target, _snapshot(propName))
                    _prefsChanged = False
                    _snapshot(propName) = _snapshot(propName)
                End If
            Catch
            End Try
        End Sub

        Public Sub UpdateSnapshot()
            If _target Is Nothing Then Return
            CaptureSnapshot()
            _prefsChanged = False
        End Sub

        Private Sub CaptureSnapshot()
            _snapshot = New Dictionary(Of String, Object)()
            If _target Is Nothing Then Return

            Dim tType = GetType(T)
            For Each pi In tType.GetProperties(BindingFlags.Instance Or BindingFlags.Public)
                If pi.CanRead AndAlso pi.GetIndexParameters().Length = 0 Then
                    Try
                        Dim val = pi.GetValue(_target)
                        _snapshot(pi.Name) = val
                    Catch
                    End Try
                End If
            Next
        End Sub

        Private Sub OnPropertyChanged(sender As Object, e As PropertyChangedEventArgs)
            _prefsChanged = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _disposed Then Return
            Detach()
            _disposed = True
            GC.SuppressFinalize(Me)
        End Sub
    End Class

End Namespace

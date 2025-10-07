Imports System.Data
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Reflection
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Threading

Public Module DataTypeLib

    Public Enum DetectOpts
        MonitorMouse
        MonitorMouseR
        MonitorShift
        MonitorOpts
        MonitorAll
    End Enum

    Public Enum UpdateStatus
        ToEnabled
        ToDisabled
        CancelUpdate
    End Enum

    Public Enum ProgStatus
        Idle
        Running
        Success
        Fail
        StartAP
    End Enum

    Public Enum ProgAction
        Abort
        Activate
        Complete
        Reset
    End Enum

    Public Enum ProgEvent
        Reset
        MaxFill
        DispMsg
        ClrMsg
    End Enum

    Public Enum ProgResult
        Completed
        Cancelled
    End Enum

    Public Enum ProgFlow
        Ascending
        Descending
    End Enum

    Public Enum ProgMode
        AutoPass
        AutoCast
    End Enum

    Public Enum MonitorStatus
        Watching
        Paused
        InCmd
        Starting
    End Enum

    Public Enum TriggerAction
        AutoCast
        AutoPass
        ShowOpts
        InputShift
        InputClick
        None
    End Enum

    Public Enum SimulClick
        SingleClk
    End Enum

    Public Enum TriggerType
        AutoCast
        AutoPass
        ShowPrefs
    End Enum

    Public Enum InjectType
        Shift_D
        Shift_U
        Enter_D
        Enter_U
    End Enum

    Public Enum InputAction
        AC_Start
        AC_RTC
        AP_Start
        AP_Exec
    End Enum

    Public Enum LoadTextContent
        isLoading
        isInit
        isStartingSvc
        isStarting
        isApplyConfig
    End Enum

End Module

Public Class InjectInputData
    Implements IDisposable

    Private disposedValue As Boolean

    Public Property KeyType As UShort
    Public Property InjectData As UInteger

    Public Sub New()

    End Sub

    Public Sub New(iType As InjectType)
        Select Case iType
            Case InjectType.Shift_D
                Me.KeyType = &HA1
                Me.InjectData = 0
            Case InjectType.Shift_U
                Me.KeyType = &HA1
                Me.InjectData = &H2
            Case InjectType.Enter_D
                Me.KeyType = &HD
                Me.InjectData = 0
            Case InjectType.Enter_U
                Me.KeyType = &HD
                Me.InjectData = &H2
        End Select
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects)
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class

Public Class ProgTextPos
    Public Property txtX As Integer
    Public Property txtY As Integer

    Public Sub New()
        ' Default constructor
    End Sub

    Public Sub New(pGraphics As System.Drawing.Graphics, pGUI As System.Windows.Forms.Form)
        Dim sizeF = pGraphics.MeasureString(osFuncLib_Progress.progDispMsg, osFuncLib_Progress.progFont_AC)
        Me.txtX = CInt(Math.Round(pGUI.Width - sizeF.Width) / 2)
        Me.txtY = CInt(Math.Round(pGUI.Height - sizeF.Height) / 2)
    End Sub

    Public Sub New(pGraphics As System.Drawing.Graphics, pObj As Control)
        Dim sizeF = pGraphics.MeasureString(osFuncLib_Progress.progDispMsg, osFuncLib_Progress.progFont_AP)
        Dim size As System.Drawing.Size = osFuncLib_Progress.CalcProgSize(DataTypeLib.TriggerType.AutoPass)
        Me.txtX = CInt(Math.Round(size.Width - sizeF.Width) / 2)
        Me.txtY = CInt(Math.Round(size.Height - sizeF.Height) / 2)
    End Sub
End Class

Public Class ProgMsg

    Public Property txtComposed As FormattedText
    Public Property txtLocation As Point

    Public Sub New()
        txtComposed = GenFormattedText()

        With CoreDataLib.FetchProgSizeReport(TriggerType.AutoCast)
            txtLocation = New Point((.Item("pW") - txtComposed.Width) \ 2,
                                    (.Item("pH") - txtComposed.Height) \ 2)
        End With
    End Sub

    Public Sub New(txtMsg As String, pType As TriggerType, Optional isAP As Boolean = False)
        txtComposed = GenFormattedText(txtMsg, isAP)

        With CoreDataLib.FetchProgSizeReport(pType)
            txtLocation = New Point((.Item("pW") - txtComposed.Width) \ 2,
                                    (.Item("pH") - txtComposed.Height) \ 2)
        End With
    End Sub

    Private Function GenFormattedText() As FormattedText
        Return New FormattedText(osFuncLib_Progress.progDispMsg,
                                 Globalization.CultureInfo.CurrentCulture,
                                 FlowDirection.LeftToRight,
                                 New Typeface(New FontFamily("Segoe UI"),
                                              FontStyles.Normal,
                                              FontWeights.Bold,
                                              FontStretches.Normal), 14, Brushes.Black, 1.0)
    End Function

    Private Function GenFormattedText(txtMsg As String, Optional isAP As Boolean = False) As FormattedText
        Return New FormattedText(txtMsg, Globalization.CultureInfo.CurrentCulture,
                                 FlowDirection.LeftToRight, ComposeTypeFace(),
                                 DetermineFontSize(txtMsg, isAP), Brushes.Black, 1.0)
    End Function

    Private Function DetermineFontSize(txtMsg As String, isAP As Boolean) As Double
        If isAP Then
            Return If(txtMsg.Length > 25, 12, 15)
        Else
            Return 14
        End If
    End Function

    Private Function ComposeTypeFace() As Typeface
        Return New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal,
                            FontWeights.Bold, FontStretches.Normal)
    End Function

End Class

Public Class ProgTimeData

    Public Property Starting As Long
    Public Property Ending As Long

    Public Sub New(vFuse As Integer)
        Starting = Environment.TickCount
        Ending = Starting + vFuse
    End Sub

End Class

Public Class ProgressEvent

    Public Property evDispatch As Dispatcher
    Public evAction As Action(Of ProgressEventData)

    Public Sub New(pEventElement As OddLib_ProgressBar)
        Me.evDispatch = pEventElement.Parent.Dispatcher
        Me.evAction = AddressOf pEventElement.PerformProgressEvent
    End Sub

    Public Sub New()

    End Sub

End Class

Public Class ProgressEventData

    Public Property evType As ProgEvent
    Public Property evTrigger As TriggerType
    Public Property evDispMsg As String

    Public Sub New()

    End Sub

    Public Sub New(eType As ProgEvent, evTrig As TriggerType, Optional evMsg As String = Nothing)
        Me.evType = eType
        Me.evTrigger = evTrig
        Me.evDispMsg = evMsg
    End Sub

End Class

Public Class PrefRecordIndex

    Public Property RecIdx As List(Of PrefRecord)
    Public Property RecDataTable As DataTable

    Public Sub New()
        RecIdx = New List(Of PrefRecord)
    End Sub

    Public Sub CreateRecord(pRecType As String, ParamArray pRecord() As PrefRecordData)
        Me.RecIdx.Add(New PrefRecord(pRecType, pRecord.ToArray()))
    End Sub

    Public Function FetchPref(pRecType As String, pName As String) As String
        Return RecIdx.
            FirstOrDefault(Function(pRec) pRec.PrefType.ToLower() =
            pRecType.ToLower()).PrefRecord.
            FirstOrDefault(Function(recData)
                               Return recData.PrefName.ToLower() = pName.ToLower()
                           End Function).PrefVal
    End Function

    Public Sub SavePref(pType As String, pName As String, pNewVal As String)
        With GetRecordData(RetrieveRecord(pType), pName)
            .PrefVal = pNewVal
        End With
    End Sub

    Private Function RetrieveRecord(pType As String) As PrefRecord
        Return RecIdx.
            FirstOrDefault(Function(r)
                               Return r.PrefType.ToLower() = pType.ToLower()
                           End Function)
    End Function

    Private Function GetRecordData(pRecord As PrefRecord, pName As String) As PrefRecordData
        Return pRecord.PrefRecord.
            FirstOrDefault(Function(d)
                               Return d.PrefName.ToLower() = pName.ToLower()
                           End Function)
    End Function

    Public Sub SavePrefsFile()

        CoreDataLib.osPrefStoreData.UpdatePrefStore()

        Using pWriter As New IO.StreamWriter(CoreDataLib.osPrefFile, False)
            pWriter.WriteLine("PrefCatalog_")

            For Each prefRec As PrefRecord In Me.RecIdx
                WritePrefRecords(prefRec, pWriter)
            Next

            pWriter.WriteLine("_PrefCatalog")
        End Using
    End Sub


    Private Sub SavePrefsToFile()
        Using pWriter As New IO.StreamWriter(CoreDataLib.osPrefFile, False)
            pWriter.WriteLine("PrefCatalog_")

            For Each prefRec As PrefRecord In Me.RecIdx
                WritePrefRecords(prefRec, pWriter)
            Next

            pWriter.WriteLine("_PrefCatalog")
        End Using
    End Sub

    Private Sub WritePrefRecords(pRecord As PrefRecord, ByRef objPrefWriter As StreamWriter)
        objPrefWriter.WriteLine($"|{pRecord.PrefType}-")

        For Each prefRec In pRecord.PrefRecord
            objPrefWriter.WriteLine(FormatPrefData(prefRec))
        Next

        objPrefWriter.WriteLine($"-{pRecord.PrefType}|")
    End Sub

    Private Function FormatPrefData(prefRec As PrefRecordData) As String
        Return $"{prefRec.PrefName}:{prefRec.PrefVal}"
    End Function

    Private Function PrepPref(pRecData As PrefRecordData, valType As Type) As Object
        Return Convert.ChangeType(pRecData.PrefVal, valType)
    End Function

    Private Function GetPrefTypes() As Type
        Return GetType(CoreDataLib)
    End Function

    Private Function FetchPrefVar(recType As String, recName As String) As String
        Return $"{recType}_{recName}"
    End Function

End Class

Public Class PrefRecord

    Public Property PrefType As String
    Public Property PrefRecord As List(Of PrefRecordData)

    Public Sub New()
    End Sub

    Public Sub New(pType As String)
        Me.PrefType = pType
        Me.PrefRecord = New List(Of PrefRecordData)
    End Sub

    Public Sub New(pType As String, ParamArray pRecord() As PrefRecordData)
        Me.PrefType = pType
        Me.PrefRecord = New List(Of PrefRecordData)(pRecord)
    End Sub

    Public Sub AddRecordData(pRecData As PrefRecordData)
        Me.PrefRecord.Add(pRecData)
    End Sub

End Class

Public Class PrefRecordData

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

Public Class osFuncData

    Public Property osFunc_GetStatus As Func(Of Boolean)
    Public Property osFunc_ConfirmStatus As Func(Of Boolean, osMenuFuncBinder.UpdateStatus)
    Public Property osFunc_ApplyStatus As Action(Of Boolean)
    Public Property osFunc_UpdateIcon As Action(Of Boolean)

    Public Sub New(objFunc_GetStatus As Func(Of Boolean),
                   objFunc_ConfirmStatus As Func(Of Boolean, osMenuFuncBinder.UpdateStatus),
                   objFunc_ApplyStatus As Action(Of Boolean),
                   objFunc_UpdateIcon As Action(Of Boolean))

        Me.osFunc_GetStatus = objFunc_GetStatus
        Me.osFunc_ConfirmStatus = objFunc_ConfirmStatus
        Me.osFunc_ApplyStatus = objFunc_ApplyStatus
        Me.osFunc_UpdateIcon = objFunc_UpdateIcon

    End Sub

End Class

Public Class objTriggerHandler
    Public Property HandleAction As TriggerAction
    Public Property HandleEvent As Func(Of Task)
End Class
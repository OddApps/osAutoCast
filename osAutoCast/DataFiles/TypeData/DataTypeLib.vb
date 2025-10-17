Imports System.Data
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Reflection
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Threading
Imports osForms = System.Windows.Forms
Imports osDraw = System.Drawing
Imports osIcons = System.Drawing.SystemIcons

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

    Public Enum ActionState
        inIdle
        inInit
        inProgress
        inComplete
        inFailed
    End Enum

    Public Enum ProgAction
        Abort
        Activate
        Complete
        ResetProgress
    End Enum

    Public Enum ProgEvent
        Reset
        MaxFill
        DispMsg
        ClrMsg
        Starter
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

    Public Enum ProgGUI
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
        ShowMenu
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
        ShowMenu
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

    Public Enum RenderBitmapObj
        Progress
        Message
    End Enum

    Public Enum MsgRenderType
        msgClear
        msgDisplay
    End Enum

    Public Enum MsgBoxType
        isQuestion
        isAlert
    End Enum

    Public Enum MsgBtnType
        isYes
        isNo
        isCancel
    End Enum

    Public Enum PromptType
        Prefs_Save
        Prefs_Close
        GameMenu_Leave
        CloseApp
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
            Return CalculateFontSize(txtMsg.Length)
        Else
            Return 14
        End If
    End Function

    Private Function CalculateFontSize(txtLength As Integer) As Double
        Select Case txtLength
            Case < 30
                Return 15
            Case > 40
                Return 12
            Case Else
                Return 13.5
        End Select
    End Function

    Private Function ComposeTypeFace() As Typeface
        Return New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal,
                            FontWeights.Bold, FontStretches.Normal)
    End Function

End Class

Public Class ProgEdgeObj

    Public Property EdgeX As Integer
    Public Property EdgeW As Integer
    Public Property EdgeBrush As System.Windows.Media.Brush

    Public Sub New()

    End Sub

    Public Sub New(edgeOld As Integer, edgeNew As Integer, activeBrush As System.Windows.Media.Brush, GetBrush As System.Windows.Media.Brush)
        If edgeNew > edgeOld Then
            EdgeX = edgeOld
            EdgeW = edgeNew - edgeOld
            EdgeBrush = activeBrush
        Else
            EdgeX = edgeNew
            EdgeW = edgeOld - edgeNew
            EdgeBrush = GetBrush
        End If
    End Sub

    Public Sub New(eX As Integer, eW As Integer, eB As System.Windows.Media.Brush)

    End Sub

End Class

Public Class ProgEdgeData

    Public Property EdgeOld As Integer
    Public Property EdgeNew As Integer

    Public Sub New()

    End Sub

    Public Sub New(progVal As Double, progWidth As Double, ByRef objChunk As Double)
        EdgeOld = EdgeFromChunk(objChunk, progWidth)
        objChunk = progVal
        EdgeNew = EdgeFromChunk(objChunk, progWidth)
    End Sub

    Private Function EdgeFromChunk(chunkVal As Double, progW As Double) As Integer
        If progW <= 0 Then Return 0
        Return CInt(Math.Round(progW * chunkVal))
    End Function

End Class

Public Class GUI_PrepData
    Implements IDisposable

    Private disposedValue As Boolean

    Public Property guiAction As Action
    Public Property guiDispatch As Dispatcher
    Public Property guiIsLoaded As Boolean

    Public Sub New()

    End Sub

    Public Sub New(objGUI As Window)
        guiAction = Sub()
                        Try
                            If objGUI.IsLoaded Then
                                objGUI.IsHitTestVisible = False
                                objGUI.Opacity = 0
                                objGUI.Close()
                            End If
                        Catch

                        End Try
                    End Sub

        guiDispatch = objGUI.Dispatcher
        guiIsLoaded = guiDispatch IsNot Nothing AndAlso Not guiDispatch.HasShutdownStarted
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                If guiAction IsNot Nothing Then
                    guiAction = Nothing
                End If

                If guiDispatch IsNot Nothing Then
                    guiDispatch = Nothing
                End If
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

Public Class osMenuFuncData

    Public Property osMenuFunc_GetStatus As Func(Of Boolean)
    Public Property osMenuFunc_ApplyStatus As Action(Of Boolean)

    Public Sub New(objFunc_GetStatus As Func(Of Boolean),
                   objFunc_ApplyStatus As Action(Of Boolean))

        Me.osMenuFunc_GetStatus = objFunc_GetStatus
        Me.osMenuFunc_ApplyStatus = objFunc_ApplyStatus

    End Sub

End Class

Public Class objTriggerHandler
    Public Property HandleAction As TriggerAction
    Public Property HandleEvent As Func(Of Task)
End Class

Public Module TriggerInvoker

    Public cmdDispatcher As Dispatcher

    Public Sub SetDispatcher()
        cmdDispatcher = Application.Current.Dispatcher
    End Sub

End Module

Public Class ResponseBox
    Inherits osForms.Form

    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80
    Private Const WM_MOUSEACTIVATE As Integer = &H21
    Private Const MA_NOACTIVATE As Integer = 3

    Private Const IconSize As Integer = 48

    Private lblMsg As New osForms.Label
    Private picIcon As New osForms.PictureBox

    Private ReadOnly objContentContainer As New osForms.TableLayoutPanel()
    Private ReadOnly objBtnPanel As New osForms.FlowLayoutPanel()

    Public Sub New(msgText As String, msgTitle As String, msgType As MsgBoxType)
        PrepContentContainer()

        GenContentContainer()

        PrepMsgIcon(msgType)
        PrepMsg(msgText)
        PrepBtnContainer(msgType)
        PrepPopupGui(msgTitle)

        InitContentContainer()
    End Sub

    Private Sub PrepContentContainer()
        objContentContainer.SuspendLayout()

        CType(picIcon, System.ComponentModel.ISupportInitialize).BeginInit()
        objBtnPanel.SuspendLayout()

        Me.SuspendLayout()
    End Sub

    Private Sub InitContentContainer()
        objContentContainer.ResumeLayout(False)
        objContentContainer.PerformLayout()
        CType(picIcon, System.ComponentModel.ISupportInitialize).EndInit()
        objBtnPanel.ResumeLayout(False)
        Me.ResumeLayout(False)
    End Sub

    Private Sub GenContentContainer()
        With objContentContainer
            .AutoSizeMode = osForms.AutoSizeMode.GrowAndShrink
            .ColumnCount = 2
            .ColumnStyles.Add(GenContainerCol(True))
            .ColumnStyles.Add(GenContainerCol())
            .Controls.Add(picIcon, 0, 0)
            .Controls.Add(lblMsg, 1, 0)
            .Controls.Add(objBtnPanel, 0, 1)
            .Dock = osForms.DockStyle.Fill
            .Location = New osDraw.Point(0, 0)
            .Margin = New osForms.Padding(4)
            .Name = "objContentContainer"
            .RowCount = 2
            .RowStyles.Add(GenContainerRow(True))
            .RowStyles.Add(GenContainerRow())
            '.Size = New osDraw.Size(447, 125)
            .TabIndex = 0
        End With
    End Sub

    Private Sub PrepMsgIcon(msgType As MsgBoxType)
        With picIcon
            .Size = New osDraw.Size(IconSize, IconSize)
            .Margin = New osForms.Padding(2)
            .SizeMode = osForms.PictureBoxSizeMode.StretchImage
            .Anchor = CType((osForms.AnchorStyles.Left Or osForms.AnchorStyles.Right), osForms.AnchorStyles)
            .Image = SetMsgIcon(msgType)
        End With
    End Sub

    Private Sub PrepPopupGui(msgTitle As String)
        Text = msgTitle
        FormBorderStyle = osForms.FormBorderStyle.FixedDialog
        StartPosition = osForms.FormStartPosition.CenterScreen
        ControlBox = False
        ShowInTaskbar = False
        TopMost = True
        AutoScaleMode = osForms.AutoScaleMode.Font
        AutoScaleDimensions = New osDraw.SizeF(6.0!, 13.0!)
        DoubleBuffered = True
        MinimizeBox = False
        MaximizeBox = False
        Size = New osDraw.Size(375, 164)
        Controls.Add(objContentContainer)
    End Sub

    Private Sub PrepBtnContainer(msgType As MsgBoxType)
        objContentContainer.SetColumnSpan(objBtnPanel, 2)

        AddButtons(msgType)
        With objBtnPanel
            .Dock = osForms.DockStyle.Fill
            .FlowDirection = osForms.FlowDirection.RightToLeft
            .MaximumSize = New osDraw.Size(0, 44)
            .MinimumSize = New osDraw.Size(0, 44)
            .Name = "btnContainer"
            .TabIndex = 2
        End With
    End Sub

    Private Sub PrepMsg(msgText As String)
        With lblMsg
            .AutoSize = True
            .Dock = osForms.DockStyle.Fill
            .Font = New osDraw.Font("Segoe UI", 9.75!, osDraw.FontStyle.Regular, osDraw.GraphicsUnit.Point, CType(0, Byte))
            .Margin = New osForms.Padding(6)
            .Name = "txtMsg"
            .TabIndex = 1
            .Text = msgText
        End With
    End Sub

    Private Sub AddButtons(msgType As MsgBoxType)
        Select Case msgType
            Case MsgBoxType.isAlert
                AddButton("No", osForms.DialogResult.No, False)
                AddButton("Yes", osForms.DialogResult.Yes, True)
            Case MsgBoxType.isQuestion
                AddButton("Cancel", osForms.DialogResult.Cancel, False)
                AddButton("No", osForms.DialogResult.No, False)
                AddButton("Yes", osForms.DialogResult.Yes, True)
        End Select
    End Sub

    Private Sub AddButton(btnText As String, btnResult As osForms.DialogResult, Optional isDefault As Boolean = False)
        Dim objBtn = New osForms.Button() With {
            .Text = btnText,
            .DialogResult = btnResult,
            .Size = New osDraw.Size(75, 38),
            .FlatStyle = osForms.FlatStyle.Popup,
            .UseVisualStyleBackColor = True
        }

        objBtnPanel.Controls.Add(objBtn)

        If isDefault Then AcceptButton = objBtn
        If btnResult = osForms.DialogResult.Cancel Then CancelButton = objBtn
    End Sub

    Private Function SetMsgIcon(mType As MsgBoxType) As osDraw.Bitmap
        If mType = MsgBoxType.isAlert Then
            Return osIcons.Warning.ToBitmap()
        Else
            Return osIcons.Question.ToBitmap()
        End If
    End Function

    Private Function GenContainerCol(Optional isIconCol As Boolean = False) As osForms.ColumnStyle
        If isIconCol Then
            Return New osForms.ColumnStyle(osForms.SizeType.Absolute, 52.0!)
        Else
            Return New osForms.ColumnStyle(osForms.SizeType.Percent, 100.0!)
        End If
    End Function

    Private Function GenContainerRow(Optional isBtnRow As Boolean = False) As osForms.RowStyle
        If isBtnRow Then
            Return New osForms.RowStyle(osForms.SizeType.Absolute, 75.0!)
        Else
            Return New osForms.RowStyle(osForms.SizeType.Absolute, 44.0!)
        End If
    End Function

    Protected Overrides ReadOnly Property CreateParams() As osForms.CreateParams
        Get
            Dim cp As osForms.CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
            Return cp
        End Get
    End Property

    Protected Overrides Sub WndProc(ByRef m As osForms.Message)
        If m.Msg = WM_MOUSEACTIVATE Then
            m.Result = CType(MA_NOACTIVATE, IntPtr)
            Return
        End If
        MyBase.WndProc(m)
    End Sub

    Public Shared Function ShowNoActivate(msg As String, title As String, mtype As MsgBoxType) As osForms.DialogResult
        Using dlg As New ResponseBox(msg, title, mtype)
            Return dlg.ShowDialog()
        End Using
    End Function

End Class

Public Class PromptData

    Public Property Title As String
    Public Property Msg As String
    Public Property MsgType As MsgBoxType

    Public Sub New()

    End Sub

    Public Sub New(pType As PromptType)
        Select Case pType
            Case PromptType.Prefs_Save
                Msg = "Confirm Saving To Preferences?"
                Title = "Save Preferences"
                MsgType = MsgBoxType.isAlert
            Case PromptType.Prefs_Close
                Msg = "Settings have been changed... Save Changes?"
                Title = "Save Changes"
                MsgType = MsgBoxType.isQuestion
            Case PromptType.GameMenu_Leave
                Msg = "Are you sure you want to close MTG Arena?"
                Title = "Exit Game"
                MsgType = MsgBoxType.isQuestion
            Case PromptType.CloseApp
                Msg = "Are you sure you want to exit osAutoCast?"
                Title = "Close osAutoCast"
                MsgType = MsgBoxType.isAlert
        End Select
    End Sub

End Class

Public Class ProgressValData
    Implements IDisposable

    Private disposedValue As Boolean

    Private ProgressDuration As TimeSpan
    Private ProgressEase As Func(Of Double, Double)

    Public Property ProgressComplete As Boolean

    Public Sub New()

    End Sub

    Public Sub New(pDur As TimeSpan, Optional pEasing As Func(Of Double, Double) = Nothing)
        ProgressDuration = pDur
        ProgressEase = pEasing

        ProgressComplete = False
    End Sub

    Public Sub CalcProgress(pElapsed As Stopwatch, ByRef objProgVal As Double)
        Dim progElapsed = pElapsed.Elapsed
        Dim progVal = Math.Min(1.0, progElapsed.TotalMilliseconds / ProgressDuration.TotalMilliseconds)

        objProgVal = If(ProgressEase Is Nothing, progVal, ProgressEase(progVal))
        If progVal >= 1.0 Then ProgressComplete = True
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ProgressDuration = Nothing
                ProgressEase = Nothing
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
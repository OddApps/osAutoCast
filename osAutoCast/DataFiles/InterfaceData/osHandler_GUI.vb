Imports System.Windows.Forms
Imports System.Windows.Threading

Public NotInheritable Class osHandler_GUI
    Private Shared ReadOnly _autoPass As New Lazy(Of progGui_AutoPass)(Function() New progGui_AutoPass())
    Private Shared ReadOnly _autoCast As New Lazy(Of progGui_AutoCast)(Function() New progGui_AutoCast())

    Public Shared ReadOnly Property osGui_Prefs As osPrefs = New osPrefs()

    Public Shared Property osGui_InputMonitor As Form
    Public Shared Property osGui_InputMonitor2 As Window

    Public Shared ReadOnly Property osGui_AutoPass As progGui_AutoPass
        Get
            Return _autoPass.Value
        End Get
    End Property

    Public Shared ReadOnly Property osGui_AutoCast As progGui_AutoCast
        Get
            Return _autoCast.Value
        End Get
    End Property

    Public Shared Sub PreloadForms(guiInputMon As Form)
        Dim handle As IntPtr = osGui_Prefs.Handle
        osGui_AutoPass.BeginPrep()
        osGui_AutoPass.ApplyTemplate()
        osGui_InputMonitor = guiInputMon
        osGui_AutoCast.BeginPrep()
    End Sub

    Public Shared Sub PreloadForms(guiInputMon As Window)
        Dim handle As IntPtr = osGui_Prefs.Handle

        Dim objOsInputMon As New osInputMonitor
        Dim tmpHandle = objOsInputMon.Handle

        osGui_InputMonitor = objOsInputMon
        osGui_InputMonitor2 = guiInputMon

        osGui_AutoCast.BeginPrep()
        osGui_AutoPass.BeginPrep()
    End Sub


    Public Shared Sub DisplayGUI(guiType As DataTypeLib.TriggerType, Optional ptPosData As System.Drawing.Point = Nothing)
        If guiType = DataTypeLib.TriggerType.AutoCast Then
            osGui_AutoCast.Dispatcher.Invoke(Sub()
                                                 Dim osGuiAutoCast As progGui_AutoCast = osGui_AutoCast
                                                 Dim point As System.Drawing.Point = osFuncLib_Progress.CalcPosData(ptPosData)
                                                 Dim size As System.Drawing.Size = osFuncLib_Progress.CalcProgSize(guiType)
                                                 osGuiAutoCast.Left = point.X
                                                 osGuiAutoCast.Top = point.Y
                                                 osGuiAutoCast.Show()
                                                 osGuiAutoCast.Width = size.Width
                                                 osGuiAutoCast.Height = size.Height
                                             End Sub)
        ElseIf guiType = DataTypeLib.TriggerType.AutoPass Then
            osGui_AutoPass.Dispatcher.Invoke(Sub()
                                                 osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoPass, ProgAction.Activate)
                                                 CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.Reset)
                                                 '  osGui_AutoPass.OddProgBar_AP.SetProgColor(osFuncLib_Progress.progColorData)
                                                 osGui_AutoPass.OddProgBar_AP.ProgressChunk = 1
                                                 osGui_AutoPass.Show()
                                             End Sub)
        End If
    End Sub
End Class

'Public Module osHandler_GUI
'    Public ReadOnly Property osGui_Prefs As New osPrefs()
'    '  Public ReadOnly Property osGui_AutoCast As New AutoCastGUI()
'    '  Public ReadOnly Property osGui_AutoCast As New progGui_AutoCast()
'    '   Public ReadOnly Property osGui_AutoPass As New AutoPassGUI()
'    '   Public ReadOnly Property osGui_AutoPass As New progGui_AutoPass()
'    Public Property osGui_InputMonitor As Window

'    Private ReadOnly _autoPass As New Lazy(Of progGui_AutoPass)(Function() New progGui_AutoPass())
'    Private ReadOnly _autoCast As New Lazy(Of progGui_AutoCast)(Function() New progGui_AutoCast())

'    Public ReadOnly Property osGui_AutoPass As progGui_AutoPass
'        Get
'            Return _autoPass.Value
'        End Get
'    End Property

'    Public ReadOnly Property osGui_AutoCast As progGui_AutoCast
'        Get
'            Return _autoCast.Value
'        End Get
'    End Property

'    ' Optional: preload them into memory explicitly
'    Public Sub PreloadForms(guiInputMon As Window)
'        Dim tmp = osGui_Prefs.Handle

'        osGui_AutoPass.BeginPrep()
'        osGui_AutoPass.ApplyTemplate()

'        osGui_InputMonitor = guiInputMon

'        osGui_AutoCast.BeginPrep()
'    End Sub

'    Public Sub DisplayGUI(guiType As TriggerType, Optional ptPosData As System.Drawing.Point = Nothing)
'        If guiType = TriggerType.AutoCast Then
'            osGui_AutoCast.Dispatcher.
'                Invoke(Sub()
'                           With osGui_AutoCast
'                               Dim getProgLoc = osFuncLib_Progress.CalcPosData(ptPosData)
'                               Dim getProgSize = osFuncLib_Progress.CalcProgSize(guiType)
'                               .Left = getProgLoc.X
'                               .Top = getProgLoc.Y
'                               .Show()
'                               .Width = getProgSize.Width
'                               .Height = getProgSize.Height
'                           End With
'                       End Sub)
'        ElseIf guiType = TriggerType.AutoPass Then
'            osGui_AutoPass.Dispatcher.
'                Invoke(Sub()
'                           With osGui_AutoPass
'                               .Show()
'                           End With
'                       End Sub)
'        End If
'    End Sub

'End Module

Public Class BasePersistentForm
    Inherits Form

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Me.Hide()
        End If
        MyBase.OnFormClosing(e)
    End Sub
End Class
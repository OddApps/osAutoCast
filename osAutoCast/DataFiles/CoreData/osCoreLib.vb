Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Reactive.Linq
Imports System.Reactive.Subjects
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Forms.Design.AxImporter
Imports System.Windows.Threading
Imports OddScripTxX.osMenuFuncBinder

Public NotInheritable Class osFuncLib_InputScan

    <DllImport("user32.dll")>
    Private Shared Function GetCursorPos(ByRef lpPoint As Point) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Integer) As Short
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetKeyState(nVirtKey As Integer) As Short
    End Function

    <DllImport("user32.dll")>
    Public Shared Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As UInteger, vk As UInteger) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Shared Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
    End Function



    Private Const VK_LBUTTON As Integer = &H1
    Private Const VK_RBUTTON As Integer = &H2

    Private Const VK_SHIFT As Integer = Keys.ShiftKey
    Private Const VK_ALT As Integer = &H12
    Private Const VK_O As Integer = Keys.O
    Private Const VK_C As Integer = Keys.C

    Public Shared curMonitorStatus As MonitorStatus

    Public Shared isActionComplete As Boolean

    Public Sub New()
    End Sub

    Public Function GetMonitorState() As MonitorStatus
        Return curMonitorStatus
    End Function

    Public Shared Sub SetMonitorState(setMonStatus As MonitorStatus)
        curMonitorStatus = setMonStatus
    End Sub

    Public Shared Sub ActivateMonitor()
        SetMonitorState(MonitorStatus.Watching)
        osFuncLib_InputScan.isActionComplete = False
    End Sub

    Public Shared Function isMonitorInStartup() As Boolean
        Return curMonitorStatus = MonitorStatus.Starting
    End Function

    Public Shared Function isMonitorActive() As Boolean
        Return curMonitorStatus = MonitorStatus.Watching
    End Function

    Public Shared Function isActionTriggered() As Boolean
        Return curMonitorStatus = MonitorStatus.InCmd
    End Function

    Public Shared Function FindProgPosition(ByRef pt As Point) As Boolean
        Return GetCursorPos(pt)
    End Function

    'Private Shared Sub ResetStatus()
    '    If osFuncLib_InputScan.isActionComplete Then osFuncLib_InputScan.isActionComplete = False
    '    osFuncLib_InputScan.SetMonitorState(MonitorStatus.Watching)
    '    osInputMonitor.InputMonitor_Start()
    'End Sub

End Class

Public NotInheritable Class osFuncLib_Progress


    Public Shared progValue As Double = 0.0F

    Public Shared progDispMsg As String = "noStatus"
    Public Shared progShowMsg As Boolean = False

    Public Shared progContObj As Control

    Public Shared progSteps As Integer = 200

    Public Shared ProgDuration As Integer
    Public Shared ProgInv As Double

    Public Shared progBlock_W As Single
    Public Shared progBlock_H As Integer = 25

    Public Shared progCurStatus As ProgStatus

    Public Shared progColor As Color
    Public Shared progColor_AutoCast As System.Windows.Media.Color

    Public Shared progColorData As System.Windows.Media.Color

    Public Shared ProgBrush_BG As SolidBrush
    Public Shared ProgBrush_Active As SolidBrush
    Public Shared ProgBrush_Border As Pen

    Public Shared pBrush_BG As New System.Windows.Media.
        SolidColorBrush(System.Windows.Media.Color.FromRgb(57, 57, 57))

    Public Shared pBrush_Active As System.Windows.Media.SolidColorBrush
    Public Shared pBrush_Border As System.Windows.Media.Pen = New System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2)

    Public Shared ProgContainer As Rectangle
    Public Shared ProgContainerBorder As Rectangle

    Public Shared progFont_AC As New Font("Segoe UI", 9, FontStyle.Bold)
    Public Shared progFont_AP As New Font("Segoe UI", 10, FontStyle.Bold)

    Public Shared objAutoPassProg As SmoothProgressBarr = Nothing

    Private Shared ProgStatusColors As New Dictionary(Of ProgStatus, Color) From {
        {ProgStatus.Idle, Color.White},
        {ProgStatus.Running, Color.FromArgb(82, 96, 117)},
        {ProgStatus.Success, Color.ForestGreen},
        {ProgStatus.Fail, Color.Maroon},
        {ProgStatus.StartAP, Color.Maroon}
    }

    Private Shared ReadOnly ProgStatusColors_AutoCast As New Dictionary(Of ProgStatus, System.Windows.Media.Color) From {
        {ProgStatus.Idle, System.Windows.Media.Color.FromRgb(57, 57, 57)},
        {ProgStatus.Running, System.Windows.Media.Color.FromRgb(82, 96, 117)},
        {ProgStatus.Success, System.Windows.Media.Color.FromRgb(34, 139, 34)},
        {ProgStatus.Fail, System.Windows.Media.Color.FromRgb(97, 20, 20)},
        {ProgStatus.StartAP, System.Windows.Media.Color.FromRgb(97, 20, 20)}
    }

    Private Shared ReadOnly ProgStatusColorsIndex As New Dictionary(Of ProgStatus, System.Windows.Media.Color) From {
        {ProgStatus.Idle, System.Windows.Media.Color.FromRgb(57, 57, 57)},
        {ProgStatus.Running, System.Windows.Media.Color.FromRgb(82, 96, 117)},
        {ProgStatus.Success, System.Windows.Media.Color.FromRgb(34, 139, 34)},
        {ProgStatus.Fail, System.Windows.Media.Color.FromRgb(97, 20, 20)},
        {ProgStatus.StartAP, System.Windows.Media.Color.FromRgb(97, 20, 20)}
    }

    Public Shared Sub SetProgContainer(pType As DataTypeLib.TriggerType)
        ProgContainer = New Rectangle(0, 0, CoreDataLib.GetProgSize(pType), CoreDataLib.GetProgSize(pType, True))
        ProgContainerBorder = New Rectangle(0, 0, CoreDataLib.GetProgSize(pType) - 1, CoreDataLib.GetProgSize(pType, True) - 1)
    End Sub

    Public Shared Sub SetProgState(newStatus As DataTypeLib.ProgStatus)
        progCurStatus = newStatus
        pBrush_BG = New SolidColorBrush()
    End Sub

    Public Shared Sub SetProgState(optStatus As String)
        Select Case optStatus.ToLower()
            Case "i"
                progCurStatus = DataTypeLib.ProgStatus.Idle
            Case "r"
                progCurStatus = DataTypeLib.ProgStatus.Running
            Case "s"
                progCurStatus = DataTypeLib.ProgStatus.Success
            Case "f"
                progCurStatus = DataTypeLib.ProgStatus.Fail
            Case "sap"
                progCurStatus = DataTypeLib.ProgStatus.StartAP
        End Select
    End Sub

    Public Shared Function GetProgState() As DataTypeLib.ProgStatus
        Return progCurStatus
    End Function

    Public Shared Function ShowProgFull() As Boolean
        Return progCurStatus = DataTypeLib.ProgStatus.Success OrElse progCurStatus = DataTypeLib.ProgStatus.Fail
    End Function

    Public Shared Function IsProgSuccess() As DataTypeLib.ProgStatus
        Return If(progCurStatus = DataTypeLib.ProgStatus.Success, CType(-1, DataTypeLib.ProgStatus), CType(0, DataTypeLib.ProgStatus))
    End Function

    Public Shared Sub SetProgStatus(setAction As DataTypeLib.ProgAction, pType As DataTypeLib.TriggerType, Optional progGUI As Form = Nothing)
        If pType = DataTypeLib.TriggerType.AutoCast Then
            Select Case setAction
                Case DataTypeLib.ProgAction.Abort
                    SetProgState("f")
                    progValue = 1.0
                Case DataTypeLib.ProgAction.Activate
                    progValue = 0.0
                    SetProgState("r")
                Case DataTypeLib.ProgAction.Complete
                    SetProgState("s")
                    progValue = 1.0
                Case DataTypeLib.ProgAction.Reset
                    SetProgState("i")
                    progValue = 0.0
            End Select
            SetProgColor(pType)
        Else
            Select Case setAction
                Case DataTypeLib.ProgAction.Abort
                    SetProgState("f")
                    progValue = 1.0
                Case DataTypeLib.ProgAction.Activate
                    SetProgState("sap")
                    progValue = 1.0
                Case DataTypeLib.ProgAction.Complete
                    SetProgState("s")
                    progValue = 1.0
                Case DataTypeLib.ProgAction.Reset
                    SetProgState("i")
                    progValue = 0.0
            End Select
            SetProgColor(pType)
        End If
    End Sub

    Public Shared Sub DisplayProgText(progTxt As String, Optional guiUpdate As Boolean = False, Optional guiForm As Form = Nothing, Optional guiProg As Control = Nothing)
        If progTxt = "" Then
            progShowMsg = False
            progDispMsg = ""
        Else
            progShowMsg = True
            progDispMsg = progTxt
            If guiUpdate Then
                guiForm?.Invalidate()
                guiProg?.Invalidate()
            End If
        End If
    End Sub

    Public Shared Sub DisplayProgText(progTxt As String, showProgMsg As Boolean)
        If progTxt = "" Then
            progShowMsg = False
            progDispMsg = ""
        Else
            progShowMsg = True
            progDispMsg = progTxt
        End If
    End Sub

    Public Shared Function CalcTargetTime(sTime As Long, valDuration As Integer, repCnt As Integer, repRate As Double) As Long
        Return sTime + CLng(Math.Round(valDuration * repCnt * repRate))
    End Function

    Public Shared Sub ApplyActiveColor(optColor As System.Windows.Media.Color)
        pBrush_Active = New SolidColorBrush(optColor)
    End Sub

    Private Shared Sub SetProgColor(pType As DataTypeLib.TriggerType)
        Select Case pType
            Case DataTypeLib.TriggerType.AutoCast
                osHandler_GUI.osGui_AutoCast.Dispatcher.Invoke(Sub()
                                                                   progColorData = FetchProgColor(GetProgState(), True)
                                                                   ApplyActiveColor(progColorData)
                                                                   osHandler_GUI.osGui_AutoCast.OddProgBar1.SetProgColor(progColorData)
                                                               End Sub)
            Case DataTypeLib.TriggerType.AutoPass
                osHandler_GUI.osGui_AutoPass.Dispatcher.Invoke(Sub()
                                                                   progColorData = FetchProgColor(GetProgState(), True)
                                                                   ApplyActiveColor(progColorData)
                                                                   osHandler_GUI.osGui_AutoPass.OddProgBar_AP.SetProgColor(progColorData)
                                                               End Sub)
        End Select
    End Sub

    Public Shared Sub ConstructProgContainer(progG As Graphics)
        progG.SmoothingMode = SmoothingMode.AntiAlias
        progG.Clear(Color.FromArgb(22, 22, 22))
        progG.FillRectangle(ProgBrush_BG, ProgContainer)
    End Sub

    Public Shared Sub ConstructProgBorder(progG As Graphics, Optional noFill As Boolean = False)
        progG.SmoothingMode = SmoothingMode.AntiAlias
        If Not noFill Then progG.Clear(Color.FromArgb(22, 22, 22))
        progG.DrawRectangle(ProgBrush_Border, ProgContainerBorder)
    End Sub

    Public Shared Sub ConstructProgFull(progG As Graphics, Optional noFill As Boolean = False)
        progG.SmoothingMode = SmoothingMode.AntiAlias
        If Not noFill Then progG.Clear(Color.FromArgb(22, 22, 22))
        progG.DrawRectangle(ProgBrush_Border, ProgContainerBorder)
    End Sub

    Public Shared Sub InitProgColors()
        ProgBrush_BG = New SolidBrush(Color.FromArgb(40, 40, 40))
        ProgBrush_Active = New SolidBrush(Color.DeepSkyBlue)
        ProgBrush_Border = New Pen(Color.Black, 2.0F)
    End Sub

    Public Shared Function FetchProgColor(pStatus As DataTypeLib.ProgStatus) As Color
        Return ProgStatusColors(pStatus)
    End Function

    Public Shared Function FetchProgColor(pStatus As DataTypeLib.ProgStatus, idxColors As Boolean) As System.Windows.Media.Color
        Return ProgStatusColorsIndex(pStatus)
    End Function

    Public Shared Function CalcPosData(ptPos As Point) As Point
        Return New Point(ptPos.X - CInt(Math.Round(CoreDataLib.GetProgSize(DataTypeLib.TriggerType.AutoCast) / 2.0)),
                         ptPos.Y - CoreDataLib.GetProgSize(DataTypeLib.TriggerType.AutoCast, True) - 22)
    End Function

    Public Shared Function CalcProgSize() As System.Drawing.Size
        Return New System.Drawing.Size(CoreDataLib.GetProgSize(DataTypeLib.TriggerType.AutoCast), CoreDataLib.GetProgSize(DataTypeLib.TriggerType.AutoCast, True))
    End Function

    Public Shared Function CalcProgSize(pType As DataTypeLib.TriggerType) As System.Drawing.Size
        Return New System.Drawing.Size(CoreDataLib.GetProgSize(pType), CoreDataLib.GetProgSize(pType, True))
    End Function

    Public Shared Sub SetProgBlockData(trigType As DataTypeLib.TriggerType)
        ProgDuration = If(trigType = DataTypeLib.TriggerType.AutoCast, CoreDataLib.GetFuse(), CoreDataLib.GetSafetyTimer())
        ProgInv = 1.0 / ProgDuration
    End Sub

    Public Shared Sub DisplayProgress(guiAutoCast As Form, ptPos As Point)
        guiAutoCast.Location = CalcPosData(ptPos)
        guiAutoCast.Show()
        guiAutoCast.Size = CalcProgSize()
    End Sub

    Public Shared Sub DisplayProgress(guiAutoPass As Form, isAutoPass As Boolean)
        guiAutoPass.Show()
        guiAutoPass.Invalidate()
    End Sub



    ' -- Easing functions --

    Public Shared Function EaseInOutExpo(x As Double) As Double
        If x = 0.0 Then Return 0.0
        If x = 1.0 Then Return 1.0
        Return If(x < 0.5, Math.Pow(2, 20 * x - 10) / 2, (2 - Math.Pow(2, -20 * x + 10)) / 2)
    End Function

    Public Shared Function EaseInOutCustom(x As Double) As Double
        Dim num As Double
        If x < 0.4 Then
            Dim t = x / 0.4
            num = 0.32 * t * t
        ElseIf x >= 0.8 Then
            num = 0.88 + (1.0 - Math.Pow(1.0 - (x - 0.8) / 0.2, 2.0)) * 0.12
        Else
            num = 0.32 + (x - 0.4) / 0.4 * 0.56
        End If
        Return num
    End Function

    Public Shared Function EaseInOutSine(x As Double) As Double
        Return -(Math.Cos(Math.PI * x) - 1.0) / 2.0
    End Function

    Public Shared Function EaseInOutCube(x As Double) As Double
        Return If(x >= 0.5, 1.0 - Math.Pow(-2 * x + 2, 3) / 2.0, 4 * x * x * x)
    End Function

    Public Shared Function EaseOutCubic(t As Double) As Double
        Return 1.0 - Math.Pow(1.0 - t, 3)
    End Function

    'Public Sub SetProgContainer(pType As TriggerType)
    '    ProgContainer = New Rectangle(0, 0, CoreDataLib.CoreDataLib.GetProgSize(pType), CoreDataLib.CoreDataLib.GetProgSize(pType, True))
    '    ProgContainerBorder = New Rectangle(0, 0, CoreDataLib.CoreDataLib.GetProgSize(pType) - 1, CoreDataLib.CoreDataLib.GetProgSize(pType, True) - 1)
    'End Sub

    'Public Sub SetProgState(newStatus As ProgStatus)
    '    progCurStatus = newStatus
    '    pBrush_BG = New System.Windows.Media.SolidColorBrush()
    'End Sub

    'Public Sub SetProgState(optStatus As String)
    '    Select Case optStatus.ToLower()
    '        Case "i"
    '            progCurStatus = ProgStatus.Idle
    '        Case "r"
    '            progCurStatus = ProgStatus.Running
    '        Case "s"
    '            progCurStatus = ProgStatus.Success
    '        Case "f"
    '            progCurStatus = ProgStatus.Fail
    '        Case "sap"
    '            progCurStatus = ProgStatus.StartAP
    '    End Select
    'End Sub

    'Public Function GetProgState() As ProgStatus
    '    Return progCurStatus
    'End Function

    'Public Function ShowProgFull() As Boolean
    '    Return progCurStatus = ProgStatus.Success OrElse progCurStatus = ProgStatus.Fail
    'End Function

    'Public Function IsProgSuccess() As ProgStatus
    '    Return progCurStatus = ProgStatus.Success
    'End Function

    'Public Sub SetProgStatus(setAction As ProgAction, pType As TriggerType, Optional progGUI As Form = Nothing)
    '    If pType = TriggerType.AutoCast Then
    '        Select Case setAction
    '            Case ProgAction.Abort
    '                SetProgState("f")

    '                progValue = 1.0F
    '             '   DisplayProgText("Cancelled")
    '            Case ProgAction.Activate
    '                progValue = 0.0F
    '                SetProgState("r")
    '            Case ProgAction.Complete
    '                SetProgState("s")

    '                progValue = 1.0F
    '               ' DisplayProgText(If(isRTC(), "Release To Cast", "Casting"), True)
    '            Case ProgAction.Reset
    '                SetProgState("i")

    '                progValue = 0.0F
    '                '   DisplayProgText("")

    '        End Select

    '        SetProgColor(pType)
    '    Else
    '        Select Case setAction
    '            Case ProgAction.Abort
    '                SetProgState("f")

    '                progValue = 1.0F
    '              '  DisplayProgText("AutoPass Cancelled")
    '            Case ProgAction.Activate
    '                SetProgState("sap")

    '                progValue = 1.0F
    '            Case ProgAction.Complete
    '                SetProgState("s")

    '                progValue = 1.0F
    '              '  DisplayProgText("Release Shift To AutoPass | Press C To Cancel")
    '            Case ProgAction.Reset
    '                SetProgState("i")

    '                progValue = 0.0F
    '                ' DisplayProgText("")
    '        End Select

    '        SetProgColor(pType)
    '    End If

    'End Sub

    'Public Sub DisplayProgText(progTxt As String, Optional guiUpdate As Boolean = False,
    '                           Optional guiForm As Form = Nothing, Optional guiProg As Control = Nothing)

    '    If progTxt = "" Then
    '        progShowMsg = False
    '        progDispMsg = ""

    '        Exit Sub
    '    End If

    '    progShowMsg = True
    '    progDispMsg = progTxt

    '    If guiUpdate Then
    '        If guiForm IsNot Nothing Then guiForm.Invalidate()
    '        If guiProg IsNot Nothing Then guiProg.Invalidate()
    '    End If

    'End Sub

    'Public Sub DisplayProgText(progTxt As String, showProgMsg As Boolean)
    '    If progTxt = "" Then
    '        progShowMsg = False
    '        progDispMsg = ""

    '        Exit Sub
    '    End If

    '    progShowMsg = True
    '    progDispMsg = progTxt
    'End Sub

    'Public Function CalcTargetTime(sTime As Long, valDuration As Integer, repCnt As Integer, repRate As Double) As Long
    '    Return sTime + CLng(valDuration * (repCnt * repRate))
    'End Function

    'Public Sub ApplyActiveColor(optColor As System.Windows.Media.Color)
    '    pBrush_Active = New System.Windows.Media.SolidColorBrush(optColor)
    'End Sub

    'Private Sub SetProgColor(pType As TriggerType)
    '    Select Case pType
    '        Case TriggerType.AutoCast
    '            osGui_AutoCast.Dispatcher.Invoke(Sub()
    '                                                 progColorData = FetchProgColor(GetProgState(), True)
    '                                                 ApplyActiveColor(progColorData)

    '                                                 osGui_AutoCast.OddProgBar1.SetProgColor(progColorData)
    '                                             End Sub)
    '        Case TriggerType.AutoPass
    '            osGui_AutoPass.Dispatcher.Invoke(Sub()
    '                                                 progColorData = FetchProgColor(GetProgState(), True)
    '                                                 ApplyActiveColor(progColorData)

    '                                                 osGui_AutoPass.OddProgBar_AP.SetProgColor(progColorData)
    '                                             End Sub)
    '    End Select

    'End Sub

    'Public Sub ConstructProgContainer(progG As Graphics)
    '    progG.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

    '    progG.Clear(Color.FromArgb(22, 22, 22))
    '    progG.FillRectangle(ProgBrush_BG, ProgContainer)
    'End Sub

    'Public Sub ConstructProgBorder(progG As Graphics, Optional noFill As Boolean = False)
    '    progG.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

    '    If Not noFill Then progG.Clear(Color.FromArgb(22, 22, 22))
    '    progG.DrawRectangle(ProgBrush_Border, ProgContainerBorder)
    'End Sub

    'Public Sub ConstructProgFull(progG As Graphics, Optional noFill As Boolean = False)
    '    progG.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

    '    If Not noFill Then progG.Clear(Color.FromArgb(22, 22, 22))
    '    progG.DrawRectangle(ProgBrush_Border, ProgContainerBorder)
    'End Sub

    'Public Sub InitProgColors()
    '    ProgBrush_BG = New SolidBrush(Color.FromArgb(40, 40, 40))
    '    ProgBrush_Active = New SolidBrush(Color.DeepSkyBlue)

    '    ProgBrush_Border = New Pen(Color.Black, 2)
    'End Sub

    'Public Function FetchProgColor(pStatus As ProgStatus) As Color
    '    Return ProgStatusColors(pStatus)
    'End Function

    'Public Function FetchProgColor(pStatus As ProgStatus, idxColors As Boolean) As System.Windows.Media.Color
    '    Return ProgStatusColorsIndex(pStatus)
    'End Function

    'Public Function CalcPosData(ptPos As Point) As Point
    '    Return New Point(ptPos.X - CInt(CoreDataLib.GetProgSize(TriggerType.AutoCast) / 2),
    '                     ptPos.Y - CInt(CoreDataLib.GetProgSize(TriggerType.AutoCast, True)) - 22)
    'End Function

    'Private Function CalcProgSize() As System.Drawing.Size
    '    Return New System.Drawing.Size(CoreDataLib.GetProgSize(TriggerType.AutoCast),
    '                            CoreDataLib.GetProgSize(TriggerType.AutoCast, True))
    'End Function

    'Public Function CalcProgSize(pType As TriggerType) As System.Drawing.Size
    '    Return New System.Drawing.Size(CoreDataLib.GetProgSize(pType),
    '                            CoreDataLib.GetProgSize(pType, True))
    'End Function

    'Public Sub SetProgBlockData(trigType As TriggerType)
    '    ProgDuration = If(trigType = TriggerType.AutoCast,
    '        GetFuse(), GetSafetyTimer())
    '    ProgInv = 1.0 / ProgDuration
    'End Sub

    'Public Sub DisplayProgress(guiAutoCast As Form, ptPos As Point)
    '    With guiAutoCast
    '        .Location = CalcPosData(ptPos)
    '        .Show()

    '        .Size = CalcProgSize()
    '    End With
    'End Sub

    'Public Sub DisplayProgress(guiAutoPass As Form, isAutoPass As Boolean)
    '    ' GenAutoPassProg()

    '    With guiAutoPass
    '        .Show()
    '        '  .Controls.Add(objAutoPassProg)
    '    End With

    '    guiAutoPass.Invalidate()
    'End Sub

    'Private Sub GenAutoPassProg()
    '    objAutoPassProg = Nothing

    '    objAutoPassProg = New SmoothProgressBarr(GetSafetyTimer()) With {
    '        .Size = CalcProgSize(TriggerType.AutoPass),
    '        .BackColor = Color.FromArgb(22, 22, 22),
    '        .Dock = DockStyle.Bottom
    '    }

    '    progContObj = objAutoPassProg
    'End Sub

    'Public Function EaseInOutExpo(x As Double) As Double
    '    If x = 0 Then Return 0
    '    If x = 1 Then Return 1

    '    If x < 0.5 Then
    '        Return Math.Pow(2, 20 * x - 10) / 2
    '    Else
    '        Return (2 - Math.Pow(2, -20 * x + 10)) / 2
    '    End If
    'End Function

    'Public Function EaseInOutCustom(x As Double) As Double
    '    If x < 0.4 Then
    '        Dim ezScale As Double = x / 0.4
    '        Return 0.32 * ezScale * ezScale
    '    ElseIf x < 0.8 Then
    '        Dim ezScale As Double = (x - 0.4) / 0.4
    '        Return 0.32 + ezScale * 0.56
    '    Else
    '        Dim ezScale As Double = (x - 0.8) / 0.2
    '        Return 0.88 + (1 - Math.Pow(1 - ezScale, 2)) * 0.12
    '    End If
    'End Function

    'Public Function EaseInOutSine(x As Double) As Double
    '    Return -(Math.Cos(Math.PI * x) - 1) / 2
    'End Function

    'Public Function EaseInOutCube(x As Double) As Double
    '    If x < 0.5 Then
    '        Return 4 * x * x * x
    '    Else
    '        Return 1 - Math.Pow(-2 * x + 2, 3) / 2
    '    End If
    'End Function

    'Public Function EaseOutCubic(t As Double) As Double
    '    Return 1 - Math.Pow(1 - t, 3)
    'End Function

End Class

Public NotInheritable Class osFuncLib_AutoCast

    Private Shared ptPos As Point
    Private Shared objFuncLib_InputScan As New osFuncLib_InputScan()

    <DllImport("user32.dll", EntryPoint:="mouse_event", SetLastError:=True)>
    Private Shared Sub InvokeMouse(dwFlags As UInteger, dx As UInteger, dy As UInteger, cButtons As UInteger, dwExtraInfo As IntPtr)
    End Sub

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function EnableWindow(hWnd As IntPtr, bEnable As Boolean) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function GetModuleHandle(lpModuleName As String) As IntPtr
    End Function

    Public Shared Sub ToggleInputBlock(doBlock As Boolean)
        EnableWindow(DetectGameUI.FetchHwndMTGA(), Not doBlock)
    End Sub

    Public Shared Async Sub InvokeAutoCast()
        If CoreDataLib.isRTC() Then
            Await Task.Delay(75)
            Await osHandler_Input.SuppressInput()
            ExecClicker(True)
            HoldInputs(True)
        Else
            LiberateLeftClick()
            Await osHandler_Input.SuppressInput()
            ExecClicker(True)
            HoldInputs(True)
        End If
    End Sub

    Public Shared Async Sub EngageAutoCast()
        If CoreDataLib.isRTC() Then
            Await Task.Delay(75)
            ExecClicker(True)
            HoldInputs(True)
        Else
            LiberateLeftClick()
            Await Task.Delay(75)
            ExecClicker(True)
            HoldInputs(True)
        End If
    End Sub

    Public Shared Async Function ExecuteAutoCast() As Task
        Dim acResult As DataTypeLib.ProgResult = DataTypeLib.ProgResult.Completed

        Await TaskExtensions.Unwrap(osHandler_GUI.osGui_AutoCast.Dispatcher.InvokeAsync(Of Task)(Async Function()
                                                                                                     If Not osFuncLib_InputScan.FindProgPosition(ptPos) Then
                                                                                                         Return
                                                                                                     End If
                                                                                                     osFuncLib_Progress.SetProgContainer(DataTypeLib.TriggerType.AutoCast)
                                                                                                     osHandler_GUI.DisplayGUI(DataTypeLib.TriggerType.AutoCast, ptPos)
                                                                                                     osFuncLib_Progress.SetProgBlockData(DataTypeLib.TriggerType.AutoCast)

                                                                                                     ' Closure variable (compiler-generated in C#)
                                                                                                     Dim closure90 As Object = Nothing
                                                                                                     closure90 = Await osHandler_GUI.osGui_AutoCast.LaunchAutoCast()
                                                                                                 End Function).Task)

        Await ProcessResult(acResult)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Shared Async Function ProcessResult(acResult As DataTypeLib.ProgResult) As Task
        Try
            Select Case acResult
                Case DataTypeLib.ProgResult.Completed
                    If CoreDataLib.isRTC() Then
                        CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoCast, DataTypeLib.ProgEvent.DispMsg, "Release To Cast")
                        Dim num As Integer = If(Await CoreDataLib.InputMonSvc.AnticipateInput(DataTypeLib.InputAction.AC_RTC), 1, 0)
                        Await Task.Delay(10)
                    End If

                    Dim dispatcher1 As Dispatcher = osHandler_GUI.osGui_AutoCast.Dispatcher
                    Dim callback1 As Func(Of Task) = Async Function()
                                                         Await Task.Delay(150)
                                                         CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoCast, DataTypeLib.ProgEvent.DispMsg, "Casting")
                                                     End Function
                    Await dispatcher1.InvokeAsync(callback1, DispatcherPriority.ApplicationIdle)
                    EngageAutoCast()

                Case DataTypeLib.ProgResult.Cancelled
                    Dim dispatcher2 As Dispatcher = osHandler_GUI.osGui_AutoCast.Dispatcher
                    Dim callback2 As Func(Of Task) = Async Function()
                                                         Await Task.Delay(150)
                                                         CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoCast, DataTypeLib.ProgEvent.DispMsg, "Cancelled")
                                                     End Function
                    Await dispatcher2.InvokeAsync(callback2, DispatcherPriority.ApplicationIdle)
            End Select

            Await FinalizeAutoCast()
        Catch ex As Exception

        End Try
    End Function

    Private Shared Async Function FinalizeAutoCast() As Task
        Await Task.Delay(750)
        Dim dispatcher As Dispatcher = osHandler_GUI.osGui_AutoCast.Dispatcher
        Dim callback As Action = Sub() osHandler_GUI.osGui_AutoCast.Close()
        dispatcher.Invoke(callback)
    End Function

    Private Shared Async Sub HoldInputs(doAsync As Boolean)
        Await Task.Delay(1250)
        Await osHandler_Input.RestoreInput()
    End Sub

    Private Shared Async Sub ExecClicker(Optional doDbl As Boolean = False)
        PerformLeftClk()
        If doDbl Then
            Await Task.Delay(105)
            PerformLeftClk()
        End If
    End Sub

    Private Shared Sub PerformLeftClk()
        InvokeMouse(2UI, 0UI, 0UI, 0UI, IntPtr.Zero)
        InvokeMouse(4UI, 0UI, 0UI, 0UI, IntPtr.Zero)
    End Sub

    Private Shared Sub LiberateLeftClick()
        InvokeMouse(4UI, 0UI, 0UI, 0UI, IntPtr.Zero)
    End Sub

End Class

'Module osFuncLib_AutoCast

'    <Runtime.CompilerServices.Extension()>
'    Public Function FirstOrDefault(Of TSource)(source As IEnumerable(Of TSource), predicate As Func(Of TSource, Boolean), defaultValue As TSource) As TSource

'    End Function

'    Private ptPos As Point
'    Private objFuncLib_InputScan As New osFuncLib_InputScan()

'    ' -- P/Invoke declarations --
'    <DllImport("user32.dll", EntryPoint:="mouse_event", SetLastError:=True)>
'    Private Sub InvokeMouse(dwFlags As UInteger, dx As UInteger, dy As UInteger, cButtons As UInteger, dwExtraInfo As IntPtr)
'    End Sub

'    <DllImport("user32.dll", SetLastError:=True)>
'    Private Function EnableWindow(hWnd As IntPtr, bEnable As Boolean) As Boolean
'    End Function

'    <DllImport("kernel32.dll", SetLastError:=True)>
'    Private Function GetModuleHandle(lpModuleName As String) As IntPtr
'    End Function

'    ' -- Methods --

'    Public Sub ToggleInputBlock(doBlock As Boolean)
'        EnableWindow(DetectGameUI.FetchHwndMTGA(), Not doBlock)
'    End Sub

'    Public Async Sub InvokeAutoCast()
'        If CoreDataLib.isRTC() Then
'            Await Task.Delay(75)
'            Await osHandler_Input.SuppressInput()
'            ExecClicker(True)
'            HoldInputs(True)
'        Else
'            LiberateLeftClick()
'            Await osHandler_Input.SuppressInput()
'            ExecClicker(True)
'            HoldInputs(True)
'        End If
'    End Sub

'    Public Async Sub EngageAutoCast()
'        If CoreDataLib.isRTC() Then
'            Await Task.Delay(75)
'            ExecClicker(True)
'            HoldInputs(True)
'        Else
'            LiberateLeftClick()
'            Await Task.Delay(75)
'            ExecClicker(True)
'            HoldInputs(True)
'        End If
'    End Sub

'    Public Async Function ExecuteAutoCast() As Task
'        Dim acResult As DataTypeLib.ProgResult = DataTypeLib.ProgResult.Completed
'        Await osHandler_GUI.osGui_AutoCast.Dispatcher.InvokeAsync(Function()
'                                                                      If Not objFuncLib_InputScan.FindProgPosition(ptPos) Then
'                                                                          Return Task.CompletedTask
'                                                                      End If
'                                                                      osFuncLib_Progress.SetProgContainer(DataTypeLib.TriggerType.AutoCast)
'                                                                      osHandler_GUI.DisplayGUI(DataTypeLib.TriggerType.AutoCast, ptPos)
'                                                                      osFuncLib_Progress.SetProgBlockData(DataTypeLib.TriggerType.AutoCast)
'                                                                      ' Launch AutoCast GUI asynchronously
'                                                                      Return osHandler_GUI.osGui_AutoCast.LaunchAutoCast()
'                                                                  End Function).Task
'        Await ProcessResult(acResult)
'        osFuncLib_InputScan.isActionComplete = True
'    End Function

'    Private Async Function ProcessResult(acResult As DataTypeLib.ProgResult) As Task
'        Try
'            Select Case acResult
'                Case DataTypeLib.ProgResult.Completed
'                    If CoreDataLib.isRTC() Then
'                        CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoCast, DataTypeLib.ProgEvent.DispMsg, "Release To Cast")
'                        Await CoreDataLib.InputMonSvc.AnticipateInput(DataTypeLib.InputAction.AC_RTC)
'                        Await Task.Delay(10)
'                    End If
'                    Await osHandler_GUI.osGui_AutoCast.Dispatcher.InvokeAsync(Async Function()
'                                                                                  Await Task.Delay(150)
'                                                                                  CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoCast, DataTypeLib.ProgEvent.DispMsg, "Casting")
'                                                                              End Function, DispatcherPriority.ApplicationIdle)
'                    EngageAutoCast()
'                Case DataTypeLib.ProgResult.Cancelled
'                    Await osHandler_GUI.osGui_AutoCast.Dispatcher.InvokeAsync(Async Function()
'                                                                                  Await Task.Delay(150)
'                                                                                  CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoCast, DataTypeLib.ProgEvent.DispMsg, "Cancelled")
'                                                                              End Function, DispatcherPriority.ApplicationIdle)
'            End Select

'            Await FinalizeAutoCast()
'        Catch ex As Exception
'            Debug.WriteLine($"ProcessResult error: {ex}")
'        End Try
'    End Function

'    Private Async Function FinalizeAutoCast() As Task
'        Await Task.Delay(750)
'        osHandler_GUI.osGui_AutoCast.Dispatcher.Invoke(Sub() osHandler_GUI.osGui_AutoCast.Close())
'    End Function

'    Private Async Sub HoldInputs(doAsync As Boolean)
'        Await Task.Delay(1250)
'        Await osHandler_Input.RestoreInput()
'    End Sub

'    Private Async Sub ExecClicker(Optional doDbl As Boolean = False)
'        PerformLeftClk()
'        If doDbl Then
'            Await Task.Delay(105)
'            PerformLeftClk()
'        End If
'    End Sub

'    Private Sub PerformLeftClk()
'        InvokeMouse(&H2UI, 0UI, 0UI, 0UI, IntPtr.Zero)
'        InvokeMouse(&H4UI, 0UI, 0UI, 0UI, IntPtr.Zero)
'    End Sub

'    Private Sub LiberateLeftClick()
'        InvokeMouse(&H4UI, 0UI, 0UI, 0UI, IntPtr.Zero)
'    End Sub

'End Module
Public NotInheritable Class osFuncLib_ShowOpts

    Private Shared chkCloseSettings As TaskCompletionSource(Of Boolean)

    Public Shared Async Function ExecuteDispOpts() As Task
        CoreDataLib.PrepTrigger(TriggerType.ShowPrefs)

        osHandler_GUI.osGui_InputMonitor2.Dispatcher.Invoke(Sub()
                                                                With osHandler_GUI.osGui_Prefs
                                                                    .Show()
                                                                    .Focus()
                                                                    osPrefs_PrepHandlers()
                                                                End With
                                                            End Sub)

        Await chkCloseSettings.Task

        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Shared Sub osPrefs_PrepHandlers()
        chkCloseSettings = New TaskCompletionSource(Of Boolean)()
        Dim osGuiPrefs As osPrefs = osHandler_GUI.osGui_Prefs

        RemoveHandler osGuiPrefs.VisibleChanged, Nothing
        AddHandler osGuiPrefs.VisibleChanged, Sub(sender As Object, e As EventArgs)
                                                  If osGuiPrefs.Visible Then
                                                      Return
                                                  End If
                                                  chkCloseSettings.TrySetResult(True)
                                              End Sub
    End Sub

    Private Shared Sub ResetStatus()
        If osFuncLib_InputScan.isActionComplete Then
            osFuncLib_InputScan.isActionComplete = False
        End If
        osFuncLib_InputScan.SetMonitorState(DataTypeLib.MonitorStatus.Watching)
    End Sub

End Class
'Module osFuncLib_ShowOpts

'    Private chkCloseSettings As TaskCompletionSource(Of Boolean)

'    Public Async Function ExecuteDispOpts() As Task
'        PrepTrigger(TriggerType.ShowPrefs)

'        osGui_InputMonitor.Dispatcher.Invoke(Sub()
'                                                 With osGui_Prefs
'                                                     .Show()
'                                                     .Focus()
'                                                     osPrefs_PrepHandlers()
'                                                 End With
'                                             End Sub)

'        Await chkCloseSettings.Task

'        osFuncLib_InputScan.isActionComplete = True
'    End Function

'    Private Sub osPrefs_PrepHandlers()
'        chkCloseSettings = New TaskCompletionSource(Of Boolean)

'        With osGui_Prefs
'            RemoveHandler .VisibleChanged, Nothing
'            AddHandler .VisibleChanged, Sub(sender, e)
'                                            If Not .Visible Then
'                                                chkCloseSettings.TrySetResult(True)
'                                            End If
'                                        End Sub
'        End With
'    End Sub

'    Private Sub ResetStatus()
'        If osFuncLib_InputScan.isActionComplete Then osFuncLib_InputScan.isActionComplete = False
'        osFuncLib_InputScan.SetMonitorState(MonitorStatus.Watching)
'        '  osInputMonitor.InputMonitor_Start()
'    End Sub

'End Module
Public NotInheritable Class osFuncLib_AutoPass

    Public Shared Async Sub InvokeAutoPass()
        CoreDataLib.SetGameFocus()
        Await Task.Delay(100)
        osHandler_Input.InjectAutoPassInputs()
    End Sub

    Public Shared Async Function ExecuteAutoPass() As Task
        Dim acResult As DataTypeLib.ProgResult = DataTypeLib.ProgResult.Completed

        ' Display GUI and set progress
        Await osHandler_GUI.osGui_AutoPass.Dispatcher.InvokeAsync(Async Function()
                                                                      osHandler_GUI.DisplayGUI(DataTypeLib.TriggerType.AutoPass)
                                                                      osFuncLib_Progress.SetProgBlockData(DataTypeLib.TriggerType.AutoPass)
                                                                      Await Task.Delay(50)
                                                                      Await osHandler_GUI.osGui_AutoPass.LaunchAutoPass()
                                                                  End Function)

        Await ProcessResult(acResult)
        osFuncLib_InputScan.isActionComplete = True
    End Function

    Private Shared Async Function AnticipateLaunchAP() As Task(Of Boolean)
        Dim flag As Boolean = Await CoreDataLib.InputMonSvc.AnticipateInput(DataTypeLib.InputAction.AP_Exec)
        If Not flag Then
            osFuncLib_Progress.SetProgStatus(DataTypeLib.ProgAction.Abort, DataTypeLib.TriggerType.AutoPass)
            CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoPass, DataTypeLib.ProgEvent.DispMsg, "AutoPass Cancelled")
        End If
        Return flag
    End Function

    Private Shared Async Function ProcessResult(acResult As DataTypeLib.ProgResult) As Task
        Try
            Select Case acResult
                Case DataTypeLib.ProgResult.Completed
                    CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoPass, DataTypeLib.ProgEvent.DispMsg, "Release Shift To AutoPass | Press C To Cancel")
                    If Await AnticipateLaunchAP() Then
                        CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoPass, DataTypeLib.ProgEvent.DispMsg, "AutoPassing")
                        InvokeAutoPass()
                    Else
                        osFuncLib_Progress.SetProgStatus(DataTypeLib.ProgAction.Abort, DataTypeLib.TriggerType.AutoPass)
                        CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoPass, DataTypeLib.ProgEvent.DispMsg, "AutoPass Cancelled")
                    End If

                Case DataTypeLib.ProgResult.Cancelled
                    osFuncLib_Progress.SetProgStatus(DataTypeLib.ProgAction.Abort, DataTypeLib.TriggerType.AutoPass)
                    CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoPass, DataTypeLib.ProgEvent.DispMsg, "AutoPass Cancelled")
            End Select

            Await FinalizeAutoPass()
        Catch ex As Exception
        End Try
    End Function

    Private Shared Async Function FinalizeAutoPass() As Task
        Await Task.Delay(750)
        Await osHandler_GUI.osGui_AutoPass.Dispatcher.InvokeAsync(Async Function()
                                                                      CoreDataLib.ProcessProgressEvent(DataTypeLib.ProgMode.AutoPass, DataTypeLib.ProgEvent.Reset)
                                                                      Await Task.Delay(100)
                                                                      osHandler_GUI.osGui_AutoPass.Close()
                                                                  End Function)
    End Function

End Class
'Module osFuncLib_AutoPass

'    Public Async Sub InvokeAutoPass()
'        SetGameFocus()
'        Await Task.Delay(100)

'        osHandler_Input.InjectAutoPassInputs()
'    End Sub

'    Public Async Function ExecuteAutoPass() As Task

'        Dim retProgResult As ProgResult = Nothing

'        Dim isTask_AutoPass = osGui_AutoPass.
'            Dispatcher.InvokeAsync(Async Function()
'                                       DisplayGUI(TriggerType.AutoPass)
'                                       SetProgBlockData(TriggerType.AutoPass)

'                                       Await Task.Delay(50)

'                                       retProgResult = Await osGui_AutoPass.LaunchAutoPass()
'                                   End Function)

'        Await isTask_AutoPass.Task.Unwrap()

'        Await ProcessResult(retProgResult)
'        osFuncLib_InputScan.isActionComplete = True

'    End Function

'    Private Async Function AnticipateLaunchAP() As Task(Of Boolean)
'        Dim chkInput As Boolean = Await InputMonSvc.AnticipateInput(InputAction.AP_Exec)

'        If Not chkInput Then
'            SetProgStatus(ProgAction.Abort, TriggerType.AutoPass)
'            ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
'        End If

'        Return chkInput
'    End Function

'    Private Async Function ProcessResult(acResult As ProgResult) As Task

'        Try
'            Select Case acResult
'                Case ProgResult.Completed
'                    ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "Release Shift To AutoPass | Press C To Cancel")

'                    Dim apProceed = Await AnticipateLaunchAP()

'                    If apProceed Then
'                        ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPassing")
'                        InvokeAutoPass()
'                    Else
'                        SetProgStatus(ProgAction.Abort, TriggerType.AutoPass)
'                        ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
'                    End If
'                Case ProgResult.Cancelled
'                    SetProgStatus(ProgAction.Abort, TriggerType.AutoPass)
'                    ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "AutoPass Cancelled")
'            End Select

'            Await FinalizeAutoPass()
'        Catch ex As Exception

'        End Try

'    End Function

'    Private Async Function FinalizeAutoPass() As Task
'        Await Task.Delay(750)

'        Await osGui_AutoPass.Dispatcher.InvokeAsync(Async Function()
'                                                        ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.Reset)
'                                                        Await Task.Delay(100)
'                                                        osGui_AutoPass.Close()
'                                                    End Function)
'    End Function

'End Module

Module osFuncLib_TrayMenu

    Private Property osIsEnabled As Boolean

    Private osTrayIcon As New NotifyIcon

    Private objInputMon As osInMon

    Public Function GetEnabledStatus() As Boolean
        Return osIsEnabled
        '   osTrayIcon.
    End Function

    Public Function IsDisabled() As Boolean
        Return osIsEnabled = False
    End Function

    Private Function ConfirmStatusChange(newStatus As Boolean) As UpdateStatus
        If newStatus = False Then
            Select Case MsgBox("Disable OddScript?", vbYesNo,
                                   "Confirm...")
                Case vbYes
                    osFuncLib_InputScan.SetMonitorState(MonitorStatus.Paused)
                    Return UpdateStatus.ToDisabled
                Case Else
                    Return UpdateStatus.CancelUpdate
            End Select
        Else
            osFuncLib_InputScan.SetMonitorState(MonitorStatus.Starting)
            objInputMon.RestartMonitor()

            Return UpdateStatus.ToEnabled
        End If
    End Function

    Private Sub SetNewStatus(setStatus As Boolean)
        osIsEnabled = setStatus
    End Sub

    Private Sub UpdateTrayIcon(chkStatus As Boolean)
        osTrayIcon.Icon = If(chkStatus, My.Resources.osIcon,
            My.Resources.osIcon_Disabled)
    End Sub

    Private Sub PrepTrayMenu(objOsMenu As ContextMenuStrip)
        With osTrayIcon
            .Icon = My.Resources.osIcon
            .Text = "OddMTGA | Enabled"
            .Visible = True

            .ContextMenuStrip = objOsMenu
        End With
    End Sub

    Public Function CreateTrayMenu() As ContextMenuStrip
        Dim osMenuComponents As New System.ComponentModel.Container()

        Dim osMenuObj As New ContextMenuStrip(osMenuComponents)
        Dim osMenu_EnDis As New ToolStripMenuItem("Enabled") With {.Checked = True, .CheckState = CheckState.Checked, .Name = "osMenuEnDis"}
        Dim ToolStripSeparator2 As New ToolStripSeparator()
        Dim osMenu_GameOpts As New ToolStripMenuItem("MTG Menu")
        Dim osGameMenu_Play As New ToolStripMenuItem("Play Game")
        Dim osGameMenu_Leave As New ToolStripMenuItem("Leave Game")
        Dim ToolStripSeparator1 As New ToolStripSeparator()
        Dim osMenu_Opts As New ToolStripMenuItem("Preferences")
        Dim osMenuExit As New ToolStripMenuItem("Exit")

        osMenu_GameOpts.DropDownItems.AddRange(New ToolStripItem() {osGameMenu_Play, osGameMenu_Leave})
        osMenuObj.Items.AddRange(New ToolStripItem() {osMenu_EnDis, ToolStripSeparator2, osMenu_GameOpts, ToolStripSeparator1, osMenu_Opts, osMenuExit})

        osMenuObj.Font = New Font("Trebuchet MS", 11.25F, FontStyle.Regular)
        osMenuObj.RenderMode = ToolStripRenderMode.Professional
        osMenuObj.ShowCheckMargin = True

        'AddHandler osMenu_EnDis.Click, Sub() osMenu_EnDis.Checked = Not osMenu_EnDis.Checked
        'AddHandler osGameMenu_Play.Click, Sub() MessageBox.Show("Play Game clicked")
        'AddHandler osGameMenu_Leave.Click, Sub() MessageBox.Show("Leave Game clicked")

        AddHandler osMenu_Opts.Click, Async Sub()
                                          osFuncLib_InputScan.SetMonitorState(MonitorStatus.InCmd)
                                          Await osFuncLib_ShowOpts.ExecuteDispOpts()
                                      End Sub

        AddHandler osMenuExit.Click, Sub()
                                         Dim chkConfirmExit = MsgBox("Are you sure you want to exit OddScript?",
                                    vbYesNo, "Confirm Close")

                                         If chkConfirmExit = vbNo Then Exit Sub

                                         End
                                     End Sub

        Return osMenuObj
    End Function


    Public Sub osMenu_Init(objInMon As osInMon)

        CoreDataLib.osTrayMenu = CreateTrayMenu()
        PrepTrayMenu(CoreDataLib.osTrayMenu)

        osIsEnabled = True

        objInputMon = objInMon

        With New osFuncData(AddressOf GetEnabledStatus, AddressOf ConfirmStatusChange,
                            AddressOf SetNewStatus, AddressOf UpdateTrayIcon)

            osMenuFuncBinder.BindChecked(CoreDataLib.osTrayMenu.Items.Item("osMenuEnDis"),
                                         .osFunc_GetStatus, .osFunc_ApplyStatus,
                                         .osFunc_ConfirmStatus, .osFunc_UpdateIcon)
        End With

    End Sub

End Module

Module osFuncLib_Timer

    Private Const INFINITE As UInteger = &HFFFFFFFFUI

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function CreateWaitableTimer(lpTimerAttributes As IntPtr, bManualReset As Boolean, lpTimerName As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function SetWaitableTimer(hTimer As IntPtr, ByRef lpDueTime As Long, lPeriod As Integer,
    pfnCompletionRoutine As IntPtr, lpArgToCompletionRoutine As IntPtr, fResume As Boolean) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function WaitForSingleObject(hHandle As IntPtr, dwMilliseconds As UInteger) As UInteger
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function CancelWaitableTimer(hTimer As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function CloseHandle(hObject As IntPtr) As Boolean
    End Function

    Public Async Function ProgressDelay(delayInMilliseconds As Double) As Task
        Await Task.Run(Sub()
                           ProgWait(delayInMilliseconds)
                       End Sub)
    End Function

    Public Sub ProgWait(delayInMilliseconds As Double)
        Dim hTimer As IntPtr = CreateWaitableTimer(IntPtr.Zero, True, Nothing)

        If hTimer = IntPtr.Zero Then
            Throw New Exception("Failed to create timer.")
        End If

        Dim dueTime As Long = -CLng(delayInMilliseconds * 10000)

        If Not SetWaitableTimer(hTimer, dueTime, 0, IntPtr.Zero, IntPtr.Zero, False) Then
            Throw New Exception("Failed to set timer.")
        End If

        WaitForSingleObject(hTimer, INFINITE)

        CancelWaitableTimer(hTimer)
        CloseHandle(hTimer)
    End Sub

End Module

Module osFuncLib_UI

    Public Sub SetRoundedCorners(panel As Panel, radius As Integer)
        Dim path As New GraphicsPath()

        path.StartFigure()
        path.AddArc(New Rectangle(0, 0, radius, radius), 180, 90)
        path.AddArc(New Rectangle(panel.Width - radius, 0, radius, radius), 270, 90)
        path.AddArc(New Rectangle(panel.Width - radius, panel.Height - radius, radius, radius), 0, 90)
        path.AddArc(New Rectangle(0, panel.Height - radius, radius, radius), 90, 90)
        path.CloseFigure()

        panel.Region = New Region(path)
    End Sub

    Private Function GetRoundedRectangle(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        path.StartFigure()
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90)
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90)
        path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90)
        path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Public Sub ApplySmoothRoundedPanel(ctrlPanel As Panel)
        Dim borderPanelPath As GraphicsPath = GetRoundedRectangle(ctrlPanel.ClientRectangle, 22)
        ctrlPanel.Region = New Region(borderPanelPath)

        AddHandler ctrlPanel.Paint, Sub(sender, e)
                                        Using BorderPanelStylus As New Pen(Color.Black, 4)
                                            BorderPanelStylus.Alignment = Drawing2D.PenAlignment.Outset
                                            e.Graphics.SmoothingMode = SmoothingMode.HighQuality
                                            e.Graphics.DrawPath(BorderPanelStylus, borderPanelPath)
                                        End Using
                                    End Sub
        ctrlPanel.Invalidate()
    End Sub

End Module

Public Class osMenuFuncBinder

    Public Enum UpdateStatus
        ToEnabled
        ToDisabled
        CancelUpdate
    End Enum

    Public Shared Sub BindChecked(objMenuItem As ToolStripMenuItem,
                                  DoFunc_FetchStatus As Func(Of Boolean),
                                  DoFunc_ApplyStatus As Action(Of Boolean),
                                  DoFunc_ConfirmStatus As Func(Of Boolean, osMenuFuncBinder.UpdateStatus),
                                  DoFunc_UpdateIcon As Action(Of Boolean))
        With objMenuItem
            .Checked = DoFunc_FetchStatus()

            AddHandler .Click, Sub()
                                   Dim chkCurStatus = .Checked
                                   Dim chkNewStatus = Not chkCurStatus

                                   If DoFunc_ConfirmStatus(chkNewStatus) =
                                   UpdateStatus.CancelUpdate Then Return

                                   .Checked = chkNewStatus
                                   DoFunc_UpdateIcon(chkNewStatus)
                                   DoFunc_ApplyStatus(chkNewStatus)
                               End Sub
        End With
    End Sub

End Class

Module ControlExtensions
    <Extension()>
    Public Function InvokeAsync(ctrl As Control, action As Action) As Task
        Dim tcs As New TaskCompletionSource(Of Object)()

        If ctrl.InvokeRequired Then
            ctrl.BeginInvoke(Sub()
                                 Try
                                     action()
                                     tcs.SetResult(Nothing)
                                 Catch ex As Exception
                                     tcs.SetException(ex)
                                 End Try
                             End Sub)
        Else
            ' Already on UI thread
            Try
                action()
                tcs.SetResult(Nothing)
            Catch ex As Exception
                tcs.SetException(ex)
            End Try
        End If

        Return tcs.Task
    End Function
End Module
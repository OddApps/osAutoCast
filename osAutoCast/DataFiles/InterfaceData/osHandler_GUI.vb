Imports System.Windows.Forms
Imports System.Windows.Threading
Imports System.Threading
Imports osDraw = System.Drawing

Public NotInheritable Class osHandler_GUI

    Public Shared Property osGui_InputMonitor As Form
    Public Shared Property osGui_InputMonitor2 As Window

    Private Shared _osPrefs As New Lazy(Of osPrefs)(
    Function() New osPrefs(), LazyThreadSafetyMode.ExecutionAndPublication)
    Public Shared ReadOnly Property osGui_Prefs As osPrefs
        Get
            Return _osPrefs.Value
        End Get
    End Property

    Private Shared _autoPass As Lazy(Of progGui_AutoPass)
    Public Shared ReadOnly Property osGui_AutoPass As progGui_AutoPass
        Get
            Return _autoPass.Value
        End Get
    End Property

    Private Shared _autoCastProgress As ProgBarGui_AutoCast
    Public Shared ReadOnly Property osGui_AutoCastProgress As ProgBarGui_AutoCast
        Get
            Return _autoCastProgress
        End Get
    End Property

    Private Shared Sub GenerateGUI(objGenGui As TriggerAction)
        Select Case objGenGui
            Case TriggerAction.AutoPass
                _autoPass = New Lazy(Of progGui_AutoPass)(
                    Function()
                        Return Application.Current.Dispatcher.
                        Invoke(Function()
                                   Return New progGui_AutoPass()
                               End Function)
                    End Function, LazyThreadSafetyMode.ExecutionAndPublication)
            Case TriggerAction.AutoCast
                With CoreDataLib.GetProgSizeReport(TriggerType.AutoCast)
                    _autoCastProgress = New ProgBarGui_AutoCast(.pWidth, .pHeight,
                                                                osFuncLib_Progress.ProgTimeSpan, AddressOf EaseProgress)
                End With

                Dim guiLoad = _autoCastProgress.Handle
        End Select
    End Sub

    Public Shared Sub PreloadForms(guiInputMon As Window)
        Dim handle As IntPtr = osGui_Prefs.Handle
        osGui_Prefs.osPrefsPrep()

        Dim objOsInputMon As New osInputMonitor
        Dim tmpHandle = objOsInputMon.Handle

        osGui_InputMonitor = objOsInputMon
        osGui_InputMonitor2 = guiInputMon
    End Sub

    Public Shared Async Function LaunchGui(progGui As TriggerAction) As Task
        Select Case progGui
            Case TriggerAction.AutoCast
                GenerateGUI(progGui)
            Case TriggerAction.AutoPass
                Dim guiTask = Task.Run(
                    Sub()
                        GenerateGUI(progGui)
                        Dim guiReset = _autoPass.Value

                        guiReset.BeginPrep()
                    End Sub)

                Await guiTask
        End Select
    End Function

    Public Shared Sub DisplayGUI(guiType As DataTypeLib.TriggerType, Optional ptPosData As osDraw.Point = Nothing)
        If guiType = DataTypeLib.TriggerType.AutoCast Then
            Application.Current.Dispatcher.Invoke(
                Sub()
                    osGui_AutoCastProgress.InitiateAutoCast()
                End Sub)
        ElseIf guiType = DataTypeLib.TriggerType.AutoPass Then
            osGui_AutoPass.Dispatcher.Invoke(
                Sub()
                    osFuncLib_Progress.UpdateProgStatus(TriggerAction.AutoPass, ProgAction.Activate)
                    CoreDataLib.ProcessProgressEvent(ProgMode.AutoPass, ProgEvent.DispMsg, "Release Mouse To Begin")

                    osGui_AutoPass.Show()
                End Sub)
        End If
    End Sub

    Public Shared Sub ResetOptsUI()
        _osPrefs = New Lazy(Of osPrefs)(
            Function() New osPrefs(), LazyThreadSafetyMode.ExecutionAndPublication)

        Dim handle As IntPtr = osGui_Prefs.Handle
        osGui_Prefs.osPrefsPrep()
    End Sub

    Public Shared Sub ResetUI(guiType As TriggerAction, Optional forceCreateNew As Boolean = False)
        Dim guiReset As Object


        If guiType = TriggerAction.AutoCast Then

            Application.Current.Dispatcher.
                Invoke(Sub()
                           osGui_AutoCastProgress.Close()
                           osGui_AutoCastProgress.Dispose()
                       End Sub)

            _autoCastProgress = Nothing

            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()

            Exit Sub
        Else
            guiReset = _autoPass.Value
        End If

        Using objPrepData As New GUI_PrepData(guiType, guiReset)
            If objPrepData.guiIsLoaded Then
                If objPrepData.guiDispatch.CheckAccess() Then
                    objPrepData.guiAction.Invoke(guiReset)
                Else
                    objPrepData.guiDispatch.Invoke(objPrepData.guiAction,
                                                    DispatcherPriority.Normal, guiReset)
                End If
            End If
        End Using

        If forceCreateNew Then
            GenerateGUI(guiType)
            guiReset.BeginPrep()
        End If
    End Sub

End Class

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

Public Class NonActivatingForm
    Inherits Form

    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80
    Private Const WM_MOUSEACTIVATE As Integer = &H21
    Private Const MA_NOACTIVATE As Integer = 3

    Protected Overrides ReadOnly Property CreateParams() As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams

            ' Skip NOACTIVATE style in the designer so it can render properly
            If Not DesignMode Then
                cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
            End If

            Return cp
        End Get
    End Property

    Protected Overrides Sub WndProc(ByRef m As Message)
        If Not DesignMode AndAlso m.Msg = WM_MOUSEACTIVATE Then
            m.Result = CType(MA_NOACTIVATE, IntPtr)
            Return
        End If
        MyBase.WndProc(m)
    End Sub
End Class
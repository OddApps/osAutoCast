Imports System.Windows.Forms
Imports System.Windows.Threading
Imports System.Threading

Public NotInheritable Class osHandler_GUI

    Private Shared ReadOnly _autoCast As New Lazy(Of progGui_AutoCast)(Function() New progGui_AutoCast())

    Public Shared ReadOnly Property osGui_Prefs As osPrefs = New osPrefs()

    Public Shared Property osGui_InputMonitor As Form
    Public Shared Property osGui_InputMonitor2 As Window

    Private Shared _autoPass As New Lazy(Of progGui_AutoPass)(
    Function() New progGui_AutoPass(), LazyThreadSafetyMode.ExecutionAndPublication)

    Public Shared ReadOnly Property osGui_AutoPass As progGui_AutoPass
        Get
            Return _autoPass.Value
        End Get
    End Property

    Private Shared Function GenGUI_AP() As Lazy(Of progGui_AutoPass)
        Return New Lazy(Of progGui_AutoPass)(Function()
                                                 Return New progGui_AutoPass()
                                             End Function, LazyThreadSafetyMode.ExecutionAndPublication)
    End Function

    Public Shared ReadOnly Property osGui_AutoCast As progGui_AutoCast
        Get
            Return _autoCast.Value
        End Get
    End Property

    Public Shared Sub PreloadForms(guiInputMon As Window)
        Dim handle As IntPtr = osGui_Prefs.Handle

        Dim objOsInputMon As New osInputMonitor
        Dim tmpHandle = objOsInputMon.Handle

        osGui_InputMonitor = objOsInputMon
        osGui_InputMonitor2 = guiInputMon

        osGui_AutoCast.BeginPrep()
        osGui_AutoPass.BeginPrep()
    End Sub

    Public Shared Sub ResetGUI(guiType As TriggerType)
        If guiType = TriggerType.AutoPass Then

        End If
    End Sub


    Public Shared Sub DisplayGUI(guiType As DataTypeLib.TriggerType, Optional ptPosData As System.Drawing.Point = Nothing)
        If guiType = DataTypeLib.TriggerType.AutoCast Then
            osGui_AutoCast.Dispatcher.Invoke(
                Sub()
                    Dim osGuiAutoCast As progGui_AutoCast = osGui_AutoCast
                    Dim point As System.Drawing.Point = osFuncLib_Progress.CalcPosData(ptPosData)
                    Dim size As System.Drawing.Size = osFuncLib_Progress.CalcProgSize(guiType)
                    osGuiAutoCast.Left = point.X
                    osGuiAutoCast.Top = point.Y

                    CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.Reset)
                    CoreDataLib.ProcessProgressEvent(ProgMode.AutoCast, ProgEvent.DispMsg, "Release Shift")

                    osGuiAutoCast.Show()
                    osGuiAutoCast.Width = size.Width
                    osGuiAutoCast.Height = size.Height
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

    Public Shared Sub ResetUI(guiType As TriggerAction, Optional forceCreateNew As Boolean = False)
        Select Case guiType
            Case TriggerAction.AutoPass
                If _autoPass.IsValueCreated Then
                    Using objPrepData As New GUI_PrepData(_autoPass.Value)
                        If objPrepData.guiIsLoaded Then
                            If objPrepData.guiDispatch.CheckAccess() Then
                                objPrepData.guiAction.Invoke()
                            Else
                                objPrepData.guiDispatch.Invoke(objPrepData.guiAction,
                                                    DispatcherPriority.Normal)
                            End If
                        Else

                        End If
                    End Using
                End If

                _autoPass = GenGUI_AP()

                If forceCreateNew Then
                    Dim newAP = _autoPass.Value
                    newAP.BeginPrep()
                End If
        End Select
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
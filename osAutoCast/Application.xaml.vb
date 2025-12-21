Imports System.Windows.Media.Animation
Imports osAutoCast.DataTypeLib.LoadContentData
Imports osAutoCast.DataTypeLib.LoadEventType
Imports osAutoCast.osLoader_UI
Imports osAutoCast.osShaderDataLib
Imports SharpDX.D3DCompiler
Imports SharpDX.Direct3D11
Imports osAsm = System.Reflection.Assembly
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports pxShader_Pixel = SharpDX.Direct3D11.PixelShader
Imports pxShader_Text = System.Windows.Media.Effects.PixelShader
Imports pxShader_Vertex = SharpDX.Direct3D11.VertexShader
Imports System.Collections.Concurrent
Imports System.IO

#Disable Warning BC42353
Class Application

    Private objLoaderScreen As osLoader_UI

    Private objTask_LoadComplete As TaskCompletionSource(Of Boolean) = Nothing
    Private objTask_VisualComplete As TaskCompletionSource(Of Boolean) = Nothing

    Private evLoad_LoadComplete As osLoaderCompleteEventHandler
    Private evLoad1_LoadComplete

    Private evLoad_ShowWin As EventHandler
    Private evLoad_HideWin As EventHandler

    Private objAni_LoadScreenFadeIn As Storyboard
    Private objAni_LoadScreenFadeOut As Storyboard

    Private idxLoadScreenVisuals As New Dictionary(Of LoadEventType, String) From {
        {LoadEv_Show, "LoadScreenFadeIn"},
        {LoadEv_Close, "LoadScreenFadeOut"}
    }

    Private Async Sub osAutoCast_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Await PrepLoadScreen()
        Await objLoaderScreen.ProvisionApp()
    End Sub

    Private Function LoadVis_Select() As Style
        Return objLoaderScreen.Style
    End Function

    Private Function LoadVis_Set(objVisResource As Style, objVisType As LoadEventType) As Storyboard
        Return TryCast(objVisResource.
            Resources(GetVisualKey(objVisType)), Storyboard)
    End Function

    Private Function EstablishVisual(objVisType As LoadEventType) As Storyboard
        Dim objLoadVis = LoadVis_Set(LoadVis_Select(), objVisType)
        Return objLoadVis.Clone()
    End Function

    Private Function GetVisualKey(objVisType As LoadEventType) As String
        Return idxLoadScreenVisuals.First(
            Function(visKey)
                Return visKey.Key = objVisType
            End Function).Value
    End Function

    Private Async Function PrepLoadScreen() As Task
        InitLoadVisual()

        Await objTask_VisualComplete.Task
    End Function

    Private Sub InitLoadVisual()
        'objLoaderScreen = New osInMon
        objLoaderScreen = New osLoader_UI

        objLoaderScreen.Opacity = 0
        objLoaderScreen.Show()

        PrepareVisual(LoadEv_Show)
        ApplyEvents(LoadEv_Show)

        TriggerVisual(LoadEv_Show)
    End Sub

    Private Sub PrepareVisual(evType As LoadEventType)
        Select Case evType
            Case LoadEv_Show
                objAni_LoadScreenFadeIn = EstablishVisual(LoadEv_Show)
                objTask_VisualComplete.ResetAndInitTask()
            Case LoadEv_Close
                objAni_LoadScreenFadeOut = EstablishVisual(LoadEv_Close)
                objTask_VisualComplete.ResetAndInitTask()
        End Select
    End Sub

    Private Sub TriggerVisual(evType As LoadEventType)
        Select Case evType
            Case LoadEv_Show
                objAni_LoadScreenFadeIn.Begin(objLoaderScreen, True)
            Case LoadEv_Close
                objAni_LoadScreenFadeOut.Begin(objLoaderScreen, True)
        End Select
    End Sub

    Private Sub InitCloseVisual()
        PrepareVisual(LoadEv_Close)
        ApplyEvents(LoadEv_Close)

        TriggerVisual(LoadEv_Close)
    End Sub

    Private Sub ApplyEvents(evType As LoadEventType)
        Select Case evType
            Case LoadEv_Show
                evLoad_ShowWin =
                    Async Sub()
                        RemoveHandler objAni_LoadScreenFadeIn.Completed, evLoad_ShowWin

                        Await Task.Delay(350)
                        objLoaderScreen.objLoadText.Text = ""
                        Await Task.Delay(150)

                        ApplyEvents(LoadEv_Complete)

                        objTask_VisualComplete.TrySetResult(True)
                        objTask_VisualComplete = Nothing
                    End Sub

                AddHandler objAni_LoadScreenFadeIn.Completed, evLoad_ShowWin
            Case LoadEv_Close
                evLoad_HideWin =
                    Sub()
                        RemoveHandler objAni_LoadScreenFadeOut.Completed, evLoad_HideWin

                        objLoaderScreen.Opacity = 0

                        objTask_VisualComplete.TrySetResult(True)
                        objTask_VisualComplete = Nothing
                    End Sub

                AddHandler objAni_LoadScreenFadeOut.Completed, evLoad_HideWin
            Case LoadEv_Complete
                evLoad_LoadComplete =
                    Async Sub()
                        RemoveHandler objLoaderScreen.osLoaderComplete, evLoad_LoadComplete

                        Await Task.Delay(750)

                        InitCloseVisual()

                        Await objTask_VisualComplete.Task
                        objTask_VisualComplete = Nothing

                        objLoaderScreen.Close()
                        osHandler_UI.RecaptureResources()
                    End Sub

                AddHandler objLoaderScreen.osLoaderComplete, evLoad_LoadComplete
        End Select
    End Sub

    Private Function GetVisual(visShow As Boolean) As Storyboard
        Return If(visShow, CType(objLoaderScreen.Resources("LoadScreenFadeIn"), Storyboard),
            CType(objLoaderScreen.Resources("LoadScreenFadeOut"), Storyboard))
    End Function

    Public Shared Sub RestartMonitor()
        DoInitTriggerMonitor()
        CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
    End Sub

    Private Shared Sub DoInitTriggerMonitor()
        If CoreDataLib.InputMonSvc IsNot Nothing Then
            CoreDataLib.InputMonSvc = Nothing
        End If

        CoreDataLib.InputMonSvc = New InputMonitorService()
    End Sub

    Private Sub osAutoCast_Exit(sender As Object, e As ExitEventArgs) Handles Me.[Exit]
        osHandler_Graphics.DisposeAll()
        CoreDataLib.osTrayIcon.Visible = False
    End Sub

End Class
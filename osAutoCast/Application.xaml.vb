Imports System.Reactive.Linq
Imports System.Threading
Imports System.Windows.Forms
Imports osAutoCast.DataTypeLib.LoadContentData
Imports System.Resources
Imports System.Globalization
Imports osResDict = System.Collections.DictionaryEntry
Imports System.Reflection

Class Application

    Private ReadOnly idxLoadTasks As New Dictionary(Of LoadContentData, Func(Of osInMon, Task)) From
        {
            {isApplyConfig, RunTask_UI(AddressOf osMenu_Init)},
            {isStartingSvc, RunTask_BG(AddressOf InputMonitor_Start)},
            {isStarting, RunTask_UI(AddressOf LoadFinalize)},
            {isPrefPrep, RunTask_BG(AddressOf PrepPrefs)},
            {isLoadingUI, RunTask_UI(AddressOf osHandler_UI.PrepAndLoadUI)}
        }

    Private Sub osAutoCast_Exit(sender As Object, e As ExitEventArgs) Handles Me.[Exit]
        osHandler_Graphics.DisposeAll()
        CoreDataLib.osTrayIcon.Visible = False
    End Sub

    Private Async Sub osAutoCast_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Dim objLoadScreen As osInMon = PrepLoadScreen()

        Await ProvisionApp(objLoadScreen)
        osHandler_UI.RecaptureResources()
    End Sub

    Private Async Function InvokeLoadFunc(objFunc As Task) As Task
        Await objFunc
    End Function

    Private Async Function InitializeContentLoad(objLoadWin As osInMon) As Task
        DispLoadMsg(isLoading, objLoadWin)
        Await Task.Delay(GetLoadDuration(isLoading))

        DispLoadMsg(isInit, objLoadWin)
        Await Task.Delay(GetLoadDuration(isInit))
    End Function

    Private Async Function LoadDataContent(objLoadContent As LoadContentData, objLoadWin As osInMon,
                                           Optional preDelay As Integer = 0) As Task
        DispLoadMsg(objLoadContent, objLoadWin)
        Await EvalDelay(preDelay)

        With PrepLoadData(objLoadContent, objLoadWin)
            Await InvokeLoadFunc(.LoadProcess)
            Await Task.Delay(.LoadDuration)
        End With
    End Function

    Private Async Function EvalDelay(preDelay As Integer) As Task
        If preDelay > 0 Then
            Await Task.Delay(preDelay)
        End If
    End Function

    Private Sub DispLoadMsg(objLoadContent As LoadContentData, objLoadWin As osInMon)
        objLoadWin.SetLoadText(objLoadContent)
    End Sub

    Private Sub PrepPrefs()
        Using osPrefManager As New osHandler_Prefs(CoreDataLib.osPrefIndex)
            osPrefManager.ProcessPrefIndex(CoreDataLib.osPrefIndex)
        End Using
    End Sub

    Private Sub LoadFinalize(objLoadWin As osInMon)
        objLoadWin.Close()
    End Sub

    Private Function FetchTask(objLoadContent As LoadContentData, objLoadWin As osInMon) As Task
        Return idxLoadTasks(objLoadContent)(objLoadWin)
    End Function

    Private Function PrepLoadData(objLoadContent As LoadContentData, objLoadWin As osInMon) As osLoadData
        Return New osLoadData(FetchTask(objLoadContent, objLoadWin),
                              GetLoadDuration(objLoadContent))
    End Function

    Private Function PrepLoadScreen() As osInMon
        Dim objLoaderScreen As New osInMon()
        objLoaderScreen.Show()

        Return objLoaderScreen
    End Function

    Private Async Function ProvisionApp(objLoadWin As osInMon) As Task
        Await InitializeContentLoad(objLoadWin)

        Await LoadDataContent(isPrefPrep, objLoadWin)
        Await LoadDataContent(isLoadingUI, objLoadWin)
        Await LoadDataContent(isApplyConfig, objLoadWin)
        Await LoadDataContent(isStartingSvc, objLoadWin)
        Await LoadDataContent(isStarting, objLoadWin, 500)
    End Function

    Public Shared Sub RestartMonitor()
        DoInitTriggerMonitor()
        CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
    End Sub

    Public Sub InputMonitor_Start()
        InitTriggerMonitor()
        CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
    End Sub

    Private Sub InitTriggerMonitor()
        DoInitTriggerMonitor()
    End Sub

    Private Shared Sub DoInitTriggerMonitor()
        If CoreDataLib.InputMonSvc IsNot Nothing Then
            CoreDataLib.InputMonSvc = Nothing
        End If

        CoreDataLib.InputMonSvc = New InputMonitorService()
    End Sub

    Private Function GetLoadDuration(objLoadContent As LoadContentData) As Integer
        Select Case objLoadContent
            Case isInit : Return 500
            Case isLoading : Return 500
            Case isApplyConfig : Return 1250
            Case isStartingSvc : Return 1000
            Case isStarting : Return 10
            Case isPrefPrep : Return 625
            Case isLoadingUI : Return 625
            Case Else : Return 10
        End Select
    End Function

    Private Function RunTask_UI(objTaskUI As Action) As Func(Of osInMon, Task)
        Return Function()
                   Return WrapTask_UI(objTaskUI)
               End Function
    End Function

    Private Function WrapTask_UI(objTaskUI As Action) As Task
        Return PrepDispatcher().InvokeAsync(objTaskUI).Task
    End Function

    Private Function RunTask_UI(objTaskUI As Action(Of osInMon)) As Func(Of osInMon, Task)
        Return Function(objLoadUI As osInMon)
                   Return WrapTask_UI(objTaskUI, objLoadUI)
               End Function
    End Function

    Private Function WrapTask_UI(objTaskUI As Action(Of osInMon), objLoadUI As osInMon) As Task
        Return PrepDispatcher().
            InvokeAsync(Sub()
                            objTaskUI(objLoadUI)
                        End Sub).Task
    End Function

    Private Function RunTask_BG(objTaskBG As Action) As Func(Of osInMon, Task)
        Return Function()
                   Return WrapTask_BG(objTaskBG)
               End Function
    End Function

    Private Function WrapTask_BG(objTaskBG As Action) As Task
        Return Task.Run(objTaskBG)
    End Function

End Class
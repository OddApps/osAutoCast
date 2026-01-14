Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports System
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports osProgLoad = osAutoCast.osLoadingElements.osLoadingProgressBar
Imports osAutoCast.DataTypeLib.LoadStep
Imports osAutoCast.DataTypeLib.LoadTextVisual
Imports osAutoCast.DataTypeLib.LoadContentData
Imports osAutoCast.DataTypeLib.LoadingProgStatus
Imports osAutoCast.DataTypeLib.LoadTextVisualType
Imports osAutoCast.DataTypeLib.LoaderEasing
Imports osAutoCast.DataTypeLib.LoadTaskType
Imports osAutoCast.osHandler_UI
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports osColor = System.Windows.Media.Color
Imports repTS = osAutoCast.TaskStatusReport
Imports osPefs = osAutoCast.osPrefLib.osPreferenceLib

Public Module osLoadTaskLib

    Public Property objLoadUI As osLoader_UI

    Private ReadOnly Property uiLoadProgBar As osProgLoad
        Get
            Return objLoadUI.uiLoad_ProgressBar
        End Get
    End Property

    Private ReadOnly Property uiPrefHandler As osHandler_Prefs
        Get
            Return objLoadUI.osPrefManager
        End Get
    End Property

    Private Property uiTextVis As Storyboard
        Get
            Return objLoadUI.objAnimation_LoadTextVis
        End Get
        Set(value As Storyboard)
            objLoadUI.objAnimation_LoadTextVis = value
        End Set
    End Property

    Private ReadOnly Property uiTextEvtTask As TaskCompletionSource(Of Boolean)
        Get
            Return objLoadUI.visLoadTextEventTask
        End Get
    End Property

    Public Async Function LoadTask_Init(progress As IProgress(Of Double), token As CancellationToken) As Task
        Dim objTask_Init As Boolean

        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            objTask_Init = Await Task.Run(
                      Async Function()
                          Await Task.Delay(370)
                          Return True
                      End Function)
        Finally
            pump.Cancel()
        End Try
        progress.Report(100)
    End Function

    Public Async Function LoadTask_PrefsLoad(progress As IProgress(Of Double), token As CancellationToken) As Task

        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            Await osPefs.Data.PreparePrefData()
            Await Task.Delay(220)

            Await osPefs.Data.ApplyPrefs()
            Await Task.Delay(225)
        Finally
            pump.Cancel()
        End Try


        progress.Report(100)
    End Function


    Public Async Function LoadTask_PrefsApply(progress As IProgress(Of Double), token As CancellationToken) As Task

        progress.Report(0)

        Await osPefs.Data.ApplyPrefs()

        progress.Report(100)
    End Function


    Public Async Function LoadTask_LoadMenus(progress As IProgress(Of Double), token As CancellationToken) As Task
        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            Await osUI_Loader.LoadUI_Menus()
            Await Task.Delay(395)
        Finally
            pump.Cancel()
        End Try


        progress.Report(100)
    End Function

    Public Async Function LoadTask_PrepMenus(progress As IProgress(Of Double), token As CancellationToken) As Task
        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            Await osUI_Loader.LoadUI_InitMenus()
            Await Task.Delay(395)
        Finally
            pump.Cancel()
        End Try


        progress.Report(100)
    End Function

    Public Async Function LoadTask_InitActions(progress As IProgress(Of Double), token As CancellationToken) As Task

        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            Await osUI_Loader.LoadUI_TriggerHandlers()
            Await Task.Delay(445)
        Finally
            pump.Cancel()
        End Try


        progress.Report(100)
    End Function

    Public Async Function LoadTask_PrepActions(progress As IProgress(Of Double), token As CancellationToken) As Task

        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            Await osUI_Loader.LoadUI_PrepHandlers()
            Await Task.Delay(445)
        Finally
            pump.Cancel()
        End Try


        progress.Report(100)
    End Function

    Public Async Function PrepPopupMenuUI(progress As IProgress(Of Double), token As CancellationToken) As Task

        progress.Report(0)

        Await osUI_Loader.LoadUI_Menus()

        progress.Report(100)
    End Function

    Public Async Function LoadAllShaders(progress As IProgress(Of Double), token As CancellationToken) As Task

        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            Await osHandler_Graphics.EnsureCreated()
            Await Task.Delay(110)
            Await BuildShaderCatalog(True)
            Await Task.Delay(130)

            Await PreloadShaderCatalog()
            Await Task.Delay(105)

            Await CoreDataLib.ComposeShaderIdx()
            Await Task.Delay(100)
        Finally
            pump.Cancel()
        End Try

        progress.Report(100)
    End Function

    Public Async Function LoadTask_StartInMon(progress As IProgress(Of Double), token As CancellationToken) As Task

        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            Await Task.Run(
                Sub()
                    objLoadUI.InitTriggerMonitor()
                    CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
                End Sub)
            Await Task.Delay(445)
        Finally
            pump.Cancel()
        End Try


        progress.Report(100)
    End Function

    Public Async Function LoadTask_Finalize(progress As IProgress(Of Double), token As CancellationToken) As Task

        progress.Report(0)
        Dim pump = StartProgressPump(progress, token)

        Try
            Await Task.Run(
                 Sub()
                     uiLoadProgBar.onLastTask = True

                     PrepDispatcher().Invoke(
                         Sub()
                             PrepTrayMenu()
                             isAppLoaded = True
                         End Sub)

                     uiTextEvtTask.ResetTask()
                 End Sub)
            Await Task.Delay(450)
        Finally
            progress.Report(100)
            pump.Cancel()
        End Try

    End Function

    Private Function GetProgressPump(ByRef objProgress As IProgress(Of Double), token As CancellationToken) As CancellationTokenSource
        objProgress.Report(0)
        Return StartProgressPump(objProgress, token)
    End Function

    Private Function StartProgressPump(progress As IProgress(Of Double), token As CancellationToken) As CancellationTokenSource
        Dim intervalMs As Integer = 100
        Dim maxPump As Double = 92

        Dim pumpCts = CancellationTokenSource.CreateLinkedTokenSource(token)

        Task.Run(
        Sub()
            Dim value As Double = 0

            Try
                While Not pumpCts.Token.IsCancellationRequested AndAlso value < maxPump
                    Threading.Thread.Sleep(intervalMs)

                    value += 10
                    progress.Report(value)
                End While
            Catch
            End Try
        End Sub,
        pumpCts.Token)

        Return pumpCts
    End Function

    Private Function StartProgressPump(
progress As IProgress(Of Double),
token As CancellationToken, valLoadSteps As Integer, durLoad As Double,
Optional intervalMs As Integer = 75,
Optional maxPump As Double = 92) As CancellationTokenSource

        Dim pumpCts = CancellationTokenSource.CreateLinkedTokenSource(token)



        Task.Run(
        Sub()
            Dim value As Double = 0

            Try
                While Not pumpCts.Token.IsCancellationRequested AndAlso value < maxPump
                    Threading.Thread.Sleep(intervalMs)

                    value += 6
                    progress.Report(value)
                End While
            Catch
            End Try
        End Sub,
        pumpCts.Token)

        Return pumpCts
    End Function

#Region "Old Load Tasks"

    Public Async Function LoadTask_Init(objTaskStatus As TaskStatusReport) As Task
        Dim objTask_Init As Boolean

        objTask_Init = Await Task.Run(
            Async Function()
                Await Task.Delay(420)
                Return True
            End Function)

        objTaskStatus.SetTaskComplete()
    End Function

    Public Async Function LoadTask_PrefsLoad(objTaskStatus As TaskStatusReport) As Task
        Await osPefs.Data.PreparePrefData()
        objTaskStatus.SetTaskComplete()
    End Function

    Public Async Function LoadTask_PrefsApply(objTaskStatus As TaskStatusReport) As Task
        Await osPefs.Data.ApplyPrefs()
        objTaskStatus.SetTaskComplete()
    End Function

    Public Async Function LoadOptsUI(objTaskStatus As TaskStatusReport) As Task
        Await CreateOptsUI()
        Await DispatcherHelpers.YieldToRenderAsync()
        Await LoadGUI()
        objTaskStatus.SetTaskComplete()
    End Function

    Public Async Function PrepPopupMenuUI(objTaskStatus As TaskStatusReport) As Task
        Await osUI_Loader.LoadUI_Menus()

        objTaskStatus.SetTaskComplete()
    End Function

    Public Async Function InitShaderDevices(objTaskStatus As TaskStatusReport) As Task
        Await osHandler_Graphics.EnsureCreated()
        objTaskStatus.SetTaskComplete()
    End Function

    Public Async Function LoadAllShaders(objTaskStatus As TaskStatusReport) As Task
        Await BuildShaderCatalog(True)
        Await DispatcherHelpers.YieldToRenderAsync()
        Await PreloadShaderCatalog()
        Await DispatcherHelpers.YieldToRenderAsync()
        Await CoreDataLib.ComposeShaderIdx()
        Await DispatcherHelpers.YieldToRenderAsync()
        Await CreateOptsUI()
        Await DispatcherHelpers.YieldToRenderAsync()
        Await LoadGUI()
        Await DispatcherHelpers.YieldToRenderAsync()
        objTaskStatus.SetTaskComplete()
    End Function

    Public Async Function LoadTask_StartInMon(objTaskStatus As TaskStatusReport) As Task
        Await Task.Run(
            Sub()
                objLoadUI.InitTriggerMonitor()
                CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
            End Sub)

        Await Task.Delay(125)

        objTaskStatus.SetTaskComplete()
    End Function

    Public Async Function LoadTask_Finalize(objTaskStatus As TaskStatusReport) As Task
        Await Task.Run(
             Sub()
                 uiLoadProgBar.onLastTask = True

                 PrepDispatcher().Invoke(
                     Sub()
                         PrepTrayMenu()
                         isAppLoaded = True
                     End Sub)

                 uiTextEvtTask.ResetTask()
             End Sub)

        objTaskStatus.SetTaskComplete()
    End Function
#End Region


















End Module

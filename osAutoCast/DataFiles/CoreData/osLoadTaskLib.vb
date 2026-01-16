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

    Public Async Function LoadTask_Init(objTaskAbort As CancellationToken) As Task

        Try
            Await Task.Delay(425)
        Finally
        End Try
    End Function

    Public Async Function LoadTask_PrefsLoad(objTaskAbort As CancellationToken) As Task

        Try
            Await osPefs.Data.PreparePrefData()

            Await Task.Delay(250)

            Await osPefs.Data.ApplyPrefs()
            Await Task.Delay(250)
        Finally
        End Try
    End Function

    Public Async Function LoadTask_LoadMenus(objTaskAbort As CancellationToken) As Task
        Try
            Await osUI_Loader.LoadUI_PopupMenu()
            Await Task.Delay(225)
            Await osUI_Loader.LoadUI_TrayMenu()
            Await Task.Delay(225)
        Finally

        End Try
    End Function

    Public Async Function LoadTask_PrepMenus(objTaskAbort As CancellationToken) As Task
        Try
            '    Await Task.Delay(110)
            '    Await osUI_Loader.LoadUI_InitPopupMenu()
            ''  Await DispatcherHelpers.YieldToRenderAsync()
            Await osUI_Loader.LoadUI_InitMenus()
            Await Task.Delay(225)
            Await osUI_Loader.LoadUI_InitTrayMenu()
            Await Task.Delay(230)
            'Await DispatcherHelpers.YieldToRenderAsync()
            'Await Task.Delay(125)
            '       Await DispatcherHelpers.YieldToRenderAsync()
        Finally
        End Try
    End Function

    Public Async Function LoadTask_InitActions(objTaskAbort As CancellationToken) As Task

        Try
            Await Task.Delay(225)
            Await osUI_Loader.LoadUI_TriggerHandlers()
            Await Task.Delay(225)
        Finally
        End Try
    End Function

    Public Async Function LoadTask_PrepActions(objTaskAbort As CancellationToken) As Task
        Try
            Await Task.Delay(150)
            Await osUI_Loader.LoadUI_PrepHandlers()
            Await Task.Delay(350)
        Finally
        End Try
    End Function

    Public Async Function LoadAllShaders(objTaskAbort As CancellationToken) As Task

        Try
            Await osHandler_Graphics.EnsureCreated()
            Await Task.Delay(120)
            Await BuildShaderCatalog(True)
            Await Task.Delay(130)

            Await PreloadShaderCatalog()
            Await Task.Delay(130)

            Await CoreDataLib.ComposeShaderIdx()
            Await Task.Delay(120)
        Finally
        End Try

    End Function

    Public Async Function LoadTask_StartInMon(objTaskAbort As CancellationToken) As Task

        Try
            Await Task.Run(
                Sub()
                    objLoadUI.InitTriggerMonitor()
                    CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
                End Sub)
            Await Task.Delay(445)
        Finally
        End Try
    End Function

    Public Async Function LoadTask_Finalize(objTaskAbort As CancellationToken) As Task

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
            Await Task.Delay(445)
        Finally
        End Try
    End Function

End Module

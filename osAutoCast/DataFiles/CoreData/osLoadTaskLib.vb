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

    Private ReadOnly Property uiTextEvtTask As TaskCompletionSource(Of Boolean)
        Get
            Return objLoadUI.visLoadTextEventTask
        End Get
    End Property

    Public Async Function LoadTask_Init(objTaskAbort As CancellationToken) As Task
        Await Task.Delay(475)
    End Function

    Public Async Function LoadTask_PrefsLoad(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(SetLoadDelays(Load_PrefPrep),
                                  SetLoadSequence(Function() osPefs.Data.PreparePrefData(),
                                                  Function() osPefs.Data.ApplyPrefs(True)))
    End Function

    Public Async Function LoadTask_LoadMenus(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(SetLoadDelays(Load_AllMenus),
                                  SetLoadSequence(Function() osUI_Loader.LoadUI_PopupMenu(),
                                                  Function() osUI_Loader.LoadUI_PrefTrayMenus()))
    End Function

    Public Async Function LoadTask_PrepMenus(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(SetLoadDelays(Load_InitMenus),
                                  SetLoadSequence(Function() osUI_Loader.LoadUI_InitMenus(),
                                                  Function() osUI_Loader.LoadUI_InitPrefTrayMenus()))
    End Function


    Public Async Function LoadTask_InitActions(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(SetLoadDelays(Load_InitActions),
                                  SetLoadSequence(Function() osUI_Loader.LoadUI_TriggerHandlers()))
    End Function

    Public Async Function LoadTask_PrepActions(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(SetLoadDelays(Load_Actions),
                                  SetLoadSequence(Function() osUI_Loader.LoadUI_PrepHandlers()))
    End Function

    Public Async Function LoadAllShaders(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(SetLoadDelays(Load_InitShaders),
                                  SetLoadSequence(Function() osHandler_Graphics.EnsureCreated(),
                                                  Function() BuildShaderCatalog(),
                                                  Function() PreloadShaderCatalog(),
                                                  Function() CoreDataLib.ComposeShaderIdx()))
    End Function

    Public Async Function LoadTask_StartInMon(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(SetLoadDelays(Load_StartingSvc),
                                  SetLoadSequence(Function() osUI_Loader.InitializeTriggerMonitor()))
    End Function

    Public Async Function LoadTask_Finalize(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(SetLoadDelays(Load_Starting),
                                  SetLoadSequence(Function() osUI_Loader.LoadTasks_WrapUp(uiLoadProgBar, uiTextEvtTask)))
    End Function

End Module

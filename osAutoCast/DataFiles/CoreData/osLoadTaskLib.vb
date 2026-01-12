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
        Await Task.WhenAll(CreateOptsUI(), LoadGUI())
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
        Await PreloadShaderCatalog()

        Await CoreDataLib.ComposeShaderIdx(objTaskStatus)
        '  Await CoreDataLib.ComposeShaderIdxAsync(objTaskStatus)
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

End Module

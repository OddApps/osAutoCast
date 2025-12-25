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

Public Module osLoadTaskLib

    Private objTask_PreloadShaders As New List(Of Task)
    Private objTask_ListShaders As New List(Of Task)

    Public objBuildShader As Task(Of Dictionary(Of String, Byte())) = Nothing

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
        AddHandler uiLoadProgBar.LoadProgComplete, objLoadUI.evtLoaderComplete

        objLoadUI.InitHandlerPref()
        Await Task.Delay(250)
        objTaskStatus.SetTaskComplete()
    End Function

    Public Function LoadTask_PrefsLoad(objTaskStatus As TaskStatusReport) As Task
        Return Task.Run(
            Sub()
                Dim objTask_LoadPrefs = uiPrefHandler.LoadPrefs(objTaskStatus)
            End Sub)
    End Function

    'Public Async Function LoadTask_PrefsLoad(objTaskStatus As TaskStatusReport) As Task
    '    Await osPrefLib.osPreferenceLib.Data.PrepLoadPrefs()
    '    Return Task.Run(
    '        Async Function()
    '            '     Dim objTask_LoadPrefs = uiPrefHandler.LoadPrefs(objTaskStatus)
    '            Await osPrefLib.osPreferenceLib.Data.PrepLoadPrefs()
    '        End Function)
    'End Function

    Public Function LoadTask_PrefsApply(objTaskStatus As TaskStatusReport) As Task
        Return Task.Run(
            Sub()
                Dim objTask_ApplyPrefs = uiPrefHandler.ApplyPrefs(CoreDataLib.osPrefIndex, objTaskStatus)
            End Sub)
    End Function

    Public Function LoadOptsUI() As Task
        Return Task.Run(Async Function()
                            Await CreateOptsUI()

                            Dim objPrefLst = osGui_Prefs.BuildPrefBindingInfoAsync()

                            Await osGui_Prefs.osPrefsPrepAsync(objPrefLst)
                        End Function)
    End Function

    Public Async Function PrepShaderData() As Task
        Await Task.WhenAll(osHandler_Graphics.EnsureCreated(),
                           CoreDataLib.ComposeShaderIdx())
    End Function

    Public Function PrepPopupMenuUI(objTaskStatus As TaskStatusReport) As Task
        Return Task.Run(
            Sub()
                Dim objTask_InitMenus = PrepDispatcher().InvokeAsync(
                    Sub()
                        Dim objTask_LoadOverlay = EnsurePopupMenuOverlayAsync()
                        Dim objTask_LoadPopupMenu = EnsurePopupMenuAsync()
                        Dim objTask_LoadTrayMenu = EnsureTrayMenuAsync()

                        objTaskStatus.SetTaskComplete()
                    End Sub, DispatcherPriority.Normal)
            End Sub)
    End Function

    Public Function LoadTask_PrepUI(objTaskStatus As TaskStatusReport) As Task
        Return Task.Run(
            Sub()
                Dim objTask_LoadPrefs = LoadOptsUI()
            End Sub)
    End Function

    Public Function LoadAllShaders(objTaskStatus As TaskStatusReport) As Task
        Return Task.Run(
            Async Function()
                Await PrepShaderData()

                Debug.WriteLine("before")
                '   Dim objLoadedShaders = Await BuildShaderCatalog()
                Await PreloadShaderCatalog()
                Debug.WriteLine("after")


                Await Task.Run(Sub()
                                   objTask_ListShaders.Clear()
                                   For Each objShader In ShaderDetailsIdx
                                       objTask_ListShaders.Add(AddShaderToIdx(objShader))
                                   Next

                                   Dim objTask_LoadShaders = Task.
                    WhenAll(objTask_ListShaders).ContinueWith(
                        Sub()
                            objTaskStatus.SetTaskComplete()
                        End Sub)
                               End Sub)


            End Function)
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

End Module

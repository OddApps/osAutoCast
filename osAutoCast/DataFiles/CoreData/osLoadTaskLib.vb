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
        Await Task.Delay(475)
    End Function

    Public Async Function LoadTask_PrefsLoad(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(450, SetLoadDelays(225, 225),
                                  SetLoadSequence(Function() osPefs.Data.PreparePrefData(),
                                                  Function() osPefs.Data.ApplyPrefs()))
    End Function

    Public Async Function LoadTask_LoadMenus(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(450, SetLoadDelays(225, 225),
                                  SetLoadSequence(Function() osUI_Loader.LoadUI_PopupMenu(),
                                                  Function() osUI_Loader.LoadUI_TrayMenu()))
    End Function

    Public Async Function LoadTask_PrepMenus(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(480, SetLoadDelays(240, 240),
                                  SetLoadSequence(Function() osUI_Loader.LoadUI_InitMenus(),
                                                  Function() osUI_Loader.LoadUI_InitTrayMenu()))
        'Await ProcessLoadSequence(475, SetLoadDelays(475),
        '                          SetLoadSequence(Function() osUI_Loader.LoadUI_InitMenus()))
    End Function

    Public Async Function LoadTask_InitActions(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(475, SetLoadDelays(475),
                                  SetLoadSequence(Function() osUI_Loader.LoadUI_TriggerHandlers()))
    End Function

    Public Async Function LoadTask_PrepActions(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(475, SetLoadDelays(475),
                                  SetLoadSequence(Function() osUI_Loader.LoadUI_PrepHandlers()))
    End Function

    Public Async Function LoadAllShaders(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(500, SetLoadDelays(120, 130, 130, 120),
                                  SetLoadSequence(Function() osHandler_Graphics.EnsureCreated(),
                                                  Function() BuildShaderCatalog(True),
                                                  Function() PreloadShaderCatalog(),
                                                  Function() CoreDataLib.ComposeShaderIdx()))
    End Function

    Public Async Function LoadTask_StartInMon(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(475, SetLoadDelays(475),
                                  SetLoadSequence(Function() osUI_Loader.InitializeTriggerMonitor()))
    End Function

    Public Async Function LoadTask_Finalize(objTaskAbort As CancellationToken) As Task
        Await ProcessLoadSequence(475, SetLoadDelays(475),
                                  SetLoadSequence(
                                        Async Function()
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
                                        End Function))
    End Function

    Public Async Function EnsureMinTotalDuration(action As Func(Of CancellationToken, Task), preMs As Integer, postMs As Integer, ct As CancellationToken) As Task
        If preMs > 0 Then Await Task.Delay(preMs, ct)
        Await action(ct)
        If postMs > 0 Then Await Task.Delay(postMs, ct)
    End Function

    Public Async Function WithSurroundingDelay(preMs As Integer,
                                           postMs As Integer,
                                           ct As CancellationToken,
                                           ParamArray actions() As Func(Of CancellationToken, Task)) As Task
        If preMs > 0 Then
            Await Task.Delay(preMs, ct)
        End If

        Try
            For Each act In actions
                ct.ThrowIfCancellationRequested()
                Await act(ct)
            Next

        Catch
        End Try
        If postMs > 0 Then
                ' run post delay even if an earlier action faulted (will throw if cancelled)
                Await Task.Delay(postMs, ct)
            End If

    End Function

End Module

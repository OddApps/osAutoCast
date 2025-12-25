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

#Disable Warning BC42353
#Disable Warning BC42104

Public Class osLoader_UI

    Private Async Function InitializeContentLoad(objTaskStatus As TaskStatusReport) As Task
        AddHandler objLoadProgBar.LoadProgComplete, evtLoaderComplete

        osPrefManager = New osHandler_Prefs()
        Await Task.Delay(250)

        objTaskStatus.SetTaskComplete()
    End Function

    Public Sub InitHandlerPref()
        osPrefManager = New osHandler_Prefs()
    End Sub

    Private Sub ImplementLoadVisEvents(ByRef objVisTask As TaskCompletionSource(Of Boolean))
        Dim _objVisTask = objVisTask

        With objVis_TextFadeIn
            evtLoadText_FadeIn =
                Sub()
                    RemoveHandler .Completed, evtLoadText_FadeIn

                    objTextBrush.BeginAnimation(SolidColorBrush.ColorProperty, Nothing)

                    With _objVisTask
                        .TrySetResult(True) : .ResetTask()
                    End With
                End Sub

            AddHandler .Completed, evtLoadText_FadeIn
        End With
    End Sub

    Private Sub ImplementLoadVisEvents()
        With objVis_TextFadeOut
            evtLoadText_FadeOutComplete =
                Sub()
                    RemoveHandler .Completed, evtLoadText_FadeOutComplete
                    objTextBrush.BeginAnimation(SolidColorBrush.ColorProperty, Nothing)
                End Sub

            AddHandler .Completed, evtLoadText_FadeOutComplete
        End With
    End Sub

    Private Function VerifyTextUpdate(valTaskType As LoadTaskType, ByRef txtMsg As String) As Boolean
        txtMsg = GetLoadTaskMsg(valTaskType)
        Return Not txtMsg = "skip"
    End Function

    Private Async Function FadeLoadTextIn(valTaskType As LoadTaskType) As Task
        Dim txtMsg As String = ""

        If VerifyTextUpdate(valTaskType, txtMsg) Then
            With objTextBrush
                visLoadTextEventTask.ResetAndInitTask()

                ImplementLoadVisEvents(visLoadTextEventTask)

                Await PrepDispatcher().InvokeAsync(
            Sub()
                ApplyLoadText(txtMsg)
                .BeginAnimation(objVisTxtColor, objVis_TextFadeIn)
            End Sub)

                Await visLoadTextEventTask.Task
            End With
        End If
    End Function

    Private Async Function FadeLoadTextOut(valTaskType As LoadTaskType) As Task
        Dim txtMsg As String = ""

        If VerifyTextUpdate(valTaskType, txtMsg) Then
            With objTextBrush
                ImplementLoadVisEvents()

                Await PrepDispatcher().InvokeAsync(
            Sub()
                .BeginAnimation(objVisTxtColor, objVis_TextFadeOut)
            End Sub)
            End With
        End If
    End Function

    Private Async Function LoadFinalize(objTaskStatus As TaskStatusReport) As Task
        Await Task.Run(
             Sub()
                 objLoadProgBar.onLastTask = True
                 PrepDispatcher().Invoke(
                     Sub()
                         objAnimation_LoadTextVis.Children.Clear()
                         objAnimation_LoadTextVis = Nothing

                         objTextBrush = Nothing

                         PrepTrayMenu()
                         isAppLoaded = True
                     End Sub)

                 visLoadTextEventTask.ResetTask()
             End Sub)
    End Function

    'Private Function PrepPrefs_Load(objTaskStatus As TaskStatusReport) As Task
    '    Return Task.Run(
    '        Sub()
    '            osPrefManager = New osHandler_Prefs()
    '            Dim objTask_LoadPrefs = osPrefManager.LoadPrefs(objTaskStatus)
    '        End Sub)
    'End Function

    'Private Function PrepPrefs_Apply(objTaskStatus As TaskStatusReport) As Task
    '    Return Task.Run(
    '        Sub()
    '            Dim objTask_ApplyPrefs = osPrefManager.ApplyPrefs(CoreDataLib.osPrefIndex, objTaskStatus)
    '        End Sub)
    'End Function

    Public Async Function ProvisionApp() As Task
        osLoadTaskLib.objLoadUI = Me

        Dim objProcessLoadStages = InitLoadHandler()
        Await objProcessLoadStages.BeginLoadStage(True)
    End Function

    'Public Async Function InputMonitor_Start(objTaskStatus As TaskStatusReport) As Task
    '    Await Task.Run(
    '        Sub()
    '            InitTriggerMonitor()
    '            CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
    '        End Sub)
    '    Await Task.Delay(125)

    '    objTaskStatus.SetTaskComplete()
    'End Function

    Public Sub InitTriggerMonitor()
        DoInitTriggerMonitor()
    End Sub

    Private Shared Sub DoInitTriggerMonitor()
        If CoreDataLib.InputMonSvc IsNot Nothing Then
            CoreDataLib.InputMonSvc = Nothing
        End If

        CoreDataLib.InputMonSvc = New InputMonitorService()
    End Sub

End Class

Partial Class osLoader_UI

    Public Event osLoaderComplete(sender As Object, e As EventArgs)

    Public evtLoaderComplete As EventHandler = AddressOf TriggerCompleteEvent

    Private isTaskValid As Boolean
    Private objValidTask As Task

    Public objAnimation_LoadTextVis As Storyboard = Nothing

    Public visLoadTextEventTask As TaskCompletionSource(Of Boolean)

    Private evtLoadText_FadeIn As EventHandler
    Private evtLoadText_FadeOutComplete As EventHandler

    Private objTextBrush As SolidColorBrush

    Private Const ContentBorder_Radius As Double = 12

    Public osPrefManager As osHandler_Prefs

    Private idxLoadStageData As New Dictionary(Of LoadTaskType, osLoadStageData) From {
        {Load_Init, CreateLoadStageData(0, 25, 500)},
        {Load_PrefPrep, CreateLoadStageData(30, 65, 475)},
        {Load_PrefApply, CreateLoadStageData(70, 95, 325)},
        {Load_Opts, CreateLoadStageData(100, 130, 475)},
        {Load_Shaders, CreateLoadStageData(140, 165, 475)},
        {Load_PopupMenu, CreateLoadStageData(170, 195, 425)},
        {Load_StartingSvc, CreateLoadStageData(200, 225, 425)},
        {Load_Starting, CreateLoadStageData(230, 250, 450)}
    }

    Private idxLoadTasks As New List(Of osLoadTaskData) From {
        {CreateLoadTaskData(Load_Init, AddressOf osLoadTaskLib.LoadTask_Init)},
        {CreateLoadTaskData(Load_PrefPrep, AddressOf osLoadTaskLib.LoadTask_PrefsLoad)},
        {CreateLoadTaskData(Load_PrefApply, AddressOf osLoadTaskLib.LoadTask_PrefsApply)},
        {CreateLoadTaskData(Load_Opts, AddressOf osLoadTaskLib.LoadOptsUI)},
        {CreateLoadTaskData(Load_Shaders, AddressOf osLoadTaskLib.LoadAllShaders)},
        {CreateLoadTaskData(Load_PopupMenu, AddressOf osLoadTaskLib.PrepPopupMenuUI)},
        {CreateLoadTaskData(Load_StartingSvc, AddressOf osLoadTaskLib.LoadTask_StartInMon)},
        {CreateLoadTaskData(Load_Starting, AddressOf LoadFinalize)}
    }

    Private idxLoadStageMsg As New Dictionary(Of LoadTaskType, String) From {
        {Load_Init, "Initializing Data"},
        {Load_PrefPrep, "Loading Preferences"},
        {Load_PrefApply, "skip"},
        {Load_Opts, "Loading Interface"},
        {Load_Shaders, "skip"},
        {Load_PopupMenu, "Loading Menus"},
        {Load_ApplyConfig, "Applying Configuration"},
        {Load_StartingSvc, "Activating Service"},
        {Load_Starting, "Starting osAutoCast"}
}

    Public Property osAutoCastVersion As String = "Ver 3.0"

    Public ReadOnly Property osAutoCastTitle As String
        Get
            Return $"osAutoCast {osAutoCastVersion} | By OddSoft"
        End Get
    End Property

    Public ReadOnly Property objLoadProgBar As osProgLoad
        Get
            Return Me.uiLoad_ProgressBar
        End Get
    End Property

    Public ReadOnly Property objLoadText As TextBlock
        Get
            Return Me.LoadingText
        End Get
    End Property

    Public ReadOnly Property objOutline As Grid
        Get
            Return Me.LoadingContainerOutline
        End Get
    End Property

    Public ReadOnly Property objVis_TextFadeOut As Timeline
        Get
            Return objAnimation_LoadTextVis.Children(0)
        End Get
    End Property

    Public ReadOnly Property objVis_TextFadeIn As Timeline
        Get
            Return objAnimation_LoadTextVis.Children(1)
        End Get
    End Property

    Public ReadOnly Property objVisTxtColor As DependencyProperty
        Get
            Return SolidColorBrush.ColorProperty
        End Get
    End Property

    Public Sub New()
        InitializeComponent()
        DataContext = Me

        ComposeVisual()
    End Sub

    Private Sub SetProgress(pVal As Double)
        objLoadProgBar.Progress = pVal
    End Sub

    Private Function CreateLoadStageData(valStart As Double, valEnd As Double, intDuration As Double) As osLoadStageData
        Return New osLoadStageData(valStart, valEnd, intDuration)
    End Function

    Private Function CreateLoadTaskData(objLoadTaskType As LoadTaskType, objTask As Func(Of TaskStatusReport, Task)) As osLoadTaskData
        Return New osLoadTaskData(objLoadTaskType, objTask)
    End Function

    Private Function GenerateLoadStages() As osLoaderStageIdx
        Dim objLoadStages As osLoader_Stage() = {
            ComposeLoadStage(Load_Init),
            ComposeLoadStage(Load_PrefPrep),
            ComposeLoadStage(Load_PrefApply),
            ComposeLoadStage(Load_Opts),
            ComposeLoadStage(Load_Shaders),
            ComposeLoadStage(Load_PopupMenu),
            ComposeLoadStage(Load_StartingSvc),
            ComposeLoadStage(Load_Starting)
        }

        Return New osLoaderStageIdx(objLoadStages)
    End Function

    Private Function InitLoadHandler() As osHandler_Loader
        Return New osHandler_Loader(GenerateLoadStages(), objLoadProgBar.Maximum,
                                    Sub(pVal) SetProgress(pVal), AddressOf FadeLoadTextOut, AddressOf FadeLoadTextIn)
    End Function

    Private Function ComposeLoadStage(valTaskType As LoadTaskType) As osLoader_Stage
        Return New osLoader_Stage(GetLoadStageData(valTaskType),
                                  GetLoadTaskData(valTaskType))
    End Function

    Private Function GetLoadTaskData(valTaskType As LoadTaskType) As osLoadTaskData
        Return idxLoadTasks.First(
            Function(objLoadTask)
                Return objLoadTask.LoadType = valTaskType
            End Function)
    End Function

    Private Function GetLoadTaskMsg(valTaskType As LoadTaskType) As String
        Return idxLoadStageMsg.First(
            Function(objLoadTask)
                Return objLoadTask.Key = valTaskType
            End Function).Value
    End Function

    Private Function GetLoadStageData(valTaskType As LoadTaskType) As osLoadStageData
        Return idxLoadStageData.First(
            Function(objLoadTask)
                Return objLoadTask.Key = valTaskType
            End Function).Value
    End Function

    Private Function CreateVisArray() As TimelineCollection
        With New osLoadTextColors(objLoadText, objTextBrush)
            Return New TimelineCollection() From {
                {New ColorAnimation(.txtShown, .txtHidden,
                                    TimeSpan.FromMilliseconds(150), FillBehavior.HoldEnd)},
                {New ColorAnimation(.txtHidden, .txtShown,
                                    TimeSpan.FromMilliseconds(150), FillBehavior.HoldEnd)}}
        End With
    End Function

    Private Sub ComposeVisual()
        objAnimation_LoadTextVis = New Storyboard() With {
            .Children = CreateVisArray()
        }
    End Sub

    Private Sub TriggerCompleteEvent(s As Object, e As EventArgs)
        RaiseEvent osLoaderComplete(s, e)
        RemoveHandler objLoadProgBar.LoadProgComplete, evtLoaderComplete
    End Sub

    Private Sub ApplyLoadText(txtLoad As String)
        objLoadText.Text = txtLoad
    End Sub

    Private Sub ComposeOutline(sender As Object, e As RoutedEventArgs) Handles LoadingContainerOutline.Loaded
        EstablishOutline(objOutline, ContentBorder_Radius)
    End Sub

End Class
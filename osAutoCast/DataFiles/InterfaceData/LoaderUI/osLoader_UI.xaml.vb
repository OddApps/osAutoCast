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
Imports osAutoCast.osLoadingObjects
Imports osLoad = osAutoCast.osLoadingObjects
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports osColor = System.Windows.Media.Color
Imports repTS = osAutoCast.TaskStatusReport
Imports System.ComponentModel
Imports osKeyTime = System.Windows.Media.Animation.KeyTime

#Disable Warning BC42353
#Disable Warning BC42104

Public Class osLoader_UI

    Private Function FetchLoadText(valTaskType As LoadTaskType) As String
        Return idxLoadStageMsg.First(
            Function(objLoadTask)
                Return objLoadTask.Key = valTaskType
            End Function).Value
    End Function

    Public Function FadeLoadTextIn(valTaskType As LoadTaskType) As Task
        Dim objWorker_FadeText As New BackgroundWorker()

        Dim uiScheduler = TaskScheduler.FromCurrentSynchronizationContext()
        Dim uiTaskFactory = New TaskFactory(uiScheduler)

        AddHandler objWorker_FadeText.DoWork,
            Sub(sender As Object, e As DoWorkEventArgs)
                Dim taskTypeLocal = DirectCast(e.Argument, LoadTaskType)
                Dim txtMsg = FetchLoadText(taskTypeLocal)

                uiTaskFactory.StartNew(
                    Sub()
                        Dim objTask_RenderLoadText = PrepDispatcher().InvokeAsync(
                            Sub()
                                ApplyLoadText(txtMsg)

                                objAnimation_LoadTextVis.Stop(objLoadText)
                                objAnimation_LoadTextVis.Begin(objLoadText, True)
                            End Sub, DispatcherPriority.Render)
                    End Sub)

                e.Result = True
            End Sub

        AddHandler objWorker_FadeText.RunWorkerCompleted,
            Sub(sender As Object, e As RunWorkerCompletedEventArgs)
                objWorker_FadeText.Dispose()
            End Sub

        objWorker_FadeText.RunWorkerAsync(valTaskType)
        Return Task.CompletedTask
    End Function

    Public Async Function ProvisionApp() As Task
        osLoadTaskLib.objLoadUI = Me
        objProcessLoadStages = InitLoadHandler()

        AddHandler objLoadProgBar.LoadProgComplete,
            evtLoaderComplete

        Await objProcessLoadStages.BeginLoadProcess()
    End Function

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
    Public visLoadTextEventTask As TaskCompletionSource(Of Boolean)

    Public objAnimation_LoadTextVis As Storyboard = Nothing

    Public objTextBrush As SolidColorBrush

    Private Const ContentBorder_Radius As Double = 11

    Private idxLoadStageMsg As New Dictionary(Of LoadTaskType, String) From {
        {Load_Init, "Initializing Data"},
        {Load_PrefPrep, "Loading Preferences"},
        {Load_InitShaders, "Initializing Interface"},
        {Load_AllMenus, "Loading Menus"},
        {Load_InitMenus, "Preparing Menu UI"},
        {Load_InitActions, "Loading User Interface"},
        {Load_Actions, "Preparing Actions"},
        {Load_StartingSvc, "Activating Service"},
        {Load_Starting, "Starting osAutoCast"}
    }

    Public objProcessLoadStages As osHandler_Loader

    Public Property osAutoCastVersion As String = "Ver 3.1"

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

    Public ReadOnly Property objLoadTextHost As Grid
        Get
            Return Me.LoadingTextHost
        End Get
    End Property

    Public ReadOnly Property objOutline As Grid
        Get
            Return Me.LoadingContainerOutline
        End Get
    End Property

    Public Function ConstructLoadIdx() As osLoad
        Dim objLoadIdx As New osLoad

        With objLoadIdx
            .LoadStageIdx = New List(Of LoadStage) From {
                .BuildLoadStage(Load_Init, AddressOf LoadTask_Init),
                .BuildLoadStage(Load_PrefPrep, AddressOf LoadTask_PrefsLoad),
                .BuildLoadStage(Load_InitShaders, AddressOf LoadAllShaders),
                .BuildLoadStage(Load_AllMenus, AddressOf LoadTask_LoadMenus),
                .BuildLoadStage(Load_InitMenus, AddressOf LoadTask_PrepMenus),
                .BuildLoadStage(Load_InitActions, AddressOf LoadTask_InitActions),
                .BuildLoadStage(Load_Actions, AddressOf LoadTask_PrepActions),
                .BuildLoadStage(Load_StartingSvc, AddressOf LoadTask_StartInMon),
                .BuildLoadStage(Load_Starting, AddressOf LoadTask_Finalize)
            }.ToArray()
        End With

        Return objLoadIdx
    End Function

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

    Private Function GetObjLoadProgBar() As osProgLoad
        Return Me.objLoadProgBar
    End Function

    Private Function InitLoadHandler() As osHandler_Loader
        Return New osHandler_Loader(ConstructLoadIdx(), objLoadProgBar, AddressOf GetObjLoadProgBar,
                                    AddressOf FadeLoadTextIn)
    End Function

    Private Function GetLoadTaskMsg(valTaskType As LoadTaskType) As String
        Return idxLoadStageMsg.First(
            Function(objLoadTask)
                Return objLoadTask.Key = valTaskType
            End Function).Value
    End Function

    Private Function CreateVisData() As Storyboard
        With New osLoadTextColors(objLoadText, objTextBrush)
            Dim objVisFrameArray As New ColorKeyFrameCollection() From {
                ComposeFrame_Start(.txtHidden), ComposeFrame_End(.txtShown)
            }

            Dim objVisFrameData As New ColorAnimationUsingKeyFrames() With {
                .KeyFrames = objVisFrameArray, .FillBehavior = FillBehavior.HoldEnd,
                .Duration = SetVisDuration(), .BeginTime = SetVisStart()
            }

            Dim objLoadTextVisData As New Storyboard()
            objLoadTextVisData.Children.Add(objVisFrameData)

            Storyboard.SetTarget(objVisFrameData, objLoadText)
            Storyboard.SetTargetProperty(objVisFrameData, SetVisAttr())

            Return objLoadTextVisData
        End With
    End Function

    Private Function SetVisAttr() As PropertyPath
        Return New PropertyPath("(TextBlock.Foreground).(SolidColorBrush.Color)")
    End Function

    Private Function ComposeFrame_Start(valColorData As osColor) As ColorKeyFrame
        Return New EasingColorKeyFrame(valColorData, SetVisDuration(0))
    End Function

    Private Function ComposeFrame_End(valColorData As osColor) As ColorKeyFrame
        Return New EasingColorKeyFrame(valColorData, SetVisDuration(75), SetVisEasing())
    End Function

    Private Function SetVisStart() As TimeSpan
        Return TimeSpan.FromMilliseconds(0)
    End Function

    Private Function SetVisDuration(valDur As Double) As osKeyTime
        Return osKeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(valDur))
    End Function

    Private Function SetVisDuration() As Duration
        Return New Duration(TimeSpan.FromMilliseconds(75))
    End Function

    Private Function SetVisEasing() As IEasingFunction
        Return New QuadraticEase With {
            .EasingMode = EasingMode.EaseIn
        }
    End Function

    Private Sub ComposeVisual()
        objAnimation_LoadTextVis = CreateVisData()
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
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

#Disable Warning BC42353
#Disable Warning BC42104

Public Class osLoader_UI

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

    Public Async Function FadeLoadTextIn(valTaskType As LoadTaskType) As Task
        Dim txtMsg As String = ""

        If VerifyTextUpdate(valTaskType, txtMsg) Then
            With objTextBrush
                visLoadTextEventTask.ResetAndInitTask()

                ImplementLoadVisEvents(visLoadTextEventTask)

                Dim a = PrepDispatcher().InvokeAsync(
                        Sub()
                            ApplyLoadText(txtMsg)
                            .BeginAnimation(objVisTxtColor, objVis_TextFadeIn)
                        End Sub, DispatcherPriority.Render)

                Await visLoadTextEventTask.Task
            End With
        End If
    End Function

    Public Function FadeLoadTextOut(valTaskType As LoadTaskType) As Task
        Dim txtMsg As String = ""

        If VerifyTextUpdate(valTaskType, txtMsg) Then
            With objTextBrush
                ImplementLoadVisEvents()

                PrepDispatcher().InvokeAsync(
                    Sub()
                        .BeginAnimation(objVisTxtColor, objVis_TextFadeOut)
                    End Sub, DispatcherPriority.Render)
            End With
        End If
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

    Public objAnimation_LoadTextVis As Storyboard = Nothing
    Public visLoadTextEventTask As TaskCompletionSource(Of Boolean)

    Private evtLoadText_FadeIn As EventHandler
    Private evtLoadText_FadeOutComplete As EventHandler

    Public objTextBrush As SolidColorBrush

    Private Const ContentBorder_Radius As Double = 11

    Public osPrefManager As osHandler_Prefs

    Private idxLoadStageMsg As New Dictionary(Of LoadTaskType, String) From {
        {Load_Init, "Initializing Data"},
        {Load_PrefPrep, "Loading Preferences"},
        {Load_InitShaders, "Initializing Interface"},
        {Load_PopupMenu, "Loading Menus"},
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

    Public Function ConstructLoadIdx() As osLoad
        Dim objLoadIdx As New osLoad

        With objLoadIdx
            .LoadStageIdx = New List(Of LoadStage) From {
                .BuildLoadStage(Load_Init, AddressOf LoadTask_Init),
                .BuildLoadStage(Load_PrefPrep, AddressOf LoadTask_PrefsLoad),
                .BuildLoadStage(Load_InitShaders, AddressOf LoadAllShaders),
                .BuildLoadStage(Load_PopupMenu, AddressOf LoadTask_LoadMenus),
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
                                    AddressOf FadeLoadTextOut, AddressOf FadeLoadTextIn)
    End Function

    Private Function GetLoadTaskMsg(valTaskType As LoadTaskType) As String
        Return idxLoadStageMsg.First(
            Function(objLoadTask)
                Return objLoadTask.Key = valTaskType
            End Function).Value
    End Function

    Private Function CreateVisArray() As TimelineCollection
        With New osLoadTextColors(objLoadText, objTextBrush)
            Return New TimelineCollection() From {
                {New ColorAnimation(.txtShown, .txtHidden,
                                    TimeSpan.FromMilliseconds(50), FillBehavior.HoldEnd)},
                {New ColorAnimation(.txtHidden, .txtShown,
                                    TimeSpan.FromMilliseconds(75), FillBehavior.HoldEnd)}}
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

    Private Sub SetLoadText(valTaskType As LoadTaskType)
        Dim txtMsg As String = ""

        If VerifyTextUpdate(valTaskType, txtMsg) Then
            objLoadText.Text = txtMsg
        End If
    End Sub

    Private Sub ComposeOutline(sender As Object, e As RoutedEventArgs) Handles LoadingContainerOutline.Loaded
        EstablishOutline(objOutline, ContentBorder_Radius)
    End Sub

End Class
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
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports osColor = System.Windows.Media.Color

#Disable Warning BC42353
#Disable Warning BC42104

Public Class osInMon

    Private Async Function InitializeContentLoad() As Task
        AddHandler objLoadProg.LoadProgComplete, evtLoadComplete

        Await PerformLoadStep(LoadStart)
        Await Task.Delay(500)
        Await PerformLoadStep(LoadInit)
    End Function

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

    Private Async Function TriggerLoadTextVis(LoadVisType As LoadTextVisualType, Optional txtLoadMsg As String = "") As Task
        With objTextBrush
            Select Case LoadVisType
                Case LoadTextFade_In
                    visLoadTextEventTask.ResetAndInitTask()

                    ImplementLoadVisEvents(visLoadTextEventTask)

                    Dim objVisTask_In = PrepDispatcher().InvokeAsync(
                        Sub()
                            ApplyLoadText(txtLoadMsg)
                            .BeginAnimation(objVisTxtColor, objVis_TextFadeIn)
                        End Sub)

                    Await visLoadTextEventTask.Task
                Case LoadTextFade_Out
                    ImplementLoadVisEvents()

                    Dim objVisTask_Out = PrepDispatcher().InvokeAsync(
                        Sub()
                            .BeginAnimation(objVisTxtColor, objVis_TextFadeOut)
                        End Sub)
            End Select

        End With
    End Function

    Private Async Function PerformLoadStep(objLoadStep As LoadStep) As Task
        With FetchLoadData(objLoadStep)

            Dim objTask_UpProg As DispatcherOperation

            Dim objTask_Load = PrepDispatcher().InvokeAsync(
                Async Function()
                    Dim objTask_VisOut =
                            TriggerLoadTextVis(LoadTextFade_Out)

                    objTask_UpProg = PrepDispatcher().InvokeAsync(
                        Sub()
                            objLoadProg.UpdateLoadProgress(.LoadProgStatus)
                        End Sub, DispatcherPriority.Render)

                    Await TriggerLoadTextVis(LoadTextFade_In, .LoadMsg)
                End Function, DispatcherPriority.Render)

            Await EvalDelay(.PreLoadDuration)

            If ChkFinalTask(.LoadProgStatus) Then
                Await objLoadProg.MonitorLoadProgress() : End If

            ValidateTaskAndRun(.LoadProcess, objValidTask,
                               isTaskValid)

            If isTaskValid Then
                Await objValidTask : End If

            Await Task.Delay(.LoadDuration)
            Await objTask_UpProg.Task

        End With
    End Function

    Private Async Function EvalDelay(preDelay As Integer) As Task
        If preDelay > 0 Then
            Await Task.Delay(preDelay)
        End If
    End Function

    Private Function PrepPrefs() As Task
        Return Task.Run(
            Async Function()
                Using osPrefManager As New osHandler_Prefs()
                    CoreDataLib.osPrefIndex = Await osPrefManager.LoadPrefs()
                    Await osPrefManager.LoadPrefsAsync(CoreDataLib.osPrefIndex)
                End Using
            End Function)
    End Function

    Private Async Function LoadFinalize() As Task
        Await Task.Run(
            Async Function()
                Await Task.Delay(250)

                PrepDispatcher().Invoke(
                    Sub()
                        objAnimation_LoadTextVis.Children.Clear()
                        objAnimation_LoadTextVis = Nothing

                        objTextBrush = Nothing

                        isAppLoaded = True
                    End Sub)

                visLoadTextEventTask.ResetTask()

            End Function)
    End Function

    Public Async Function ProvisionApp() As Task
        Await InitializeContentLoad()

        Await PerformLoadStep(LoadPrefs)
        Await PerformLoadStep(LoadUI)
        Await PerformLoadStep(LoadConfig)
        Await PerformLoadStep(LoadService)
        Await PerformLoadStep(LoadComplete)
    End Function

    Public Async Function InputMonitor_Start() As Task
        Await Task.Run(
            Sub()
                InitTriggerMonitor()
                CoreDataLib.InputMonSvc.LaunchTriggerMonitor()
            End Sub)
    End Function

    Private Sub InitTriggerMonitor()
        DoInitTriggerMonitor()
    End Sub

    Private Shared Sub DoInitTriggerMonitor()
        If CoreDataLib.InputMonSvc IsNot Nothing Then
            CoreDataLib.InputMonSvc = Nothing
        End If

        CoreDataLib.InputMonSvc = New InputMonitorService()
    End Sub

End Class

Partial Class osInMon

    Public Event osLoadComplete(sender As Object, e As EventArgs)

    Private evtLoadComplete As EventHandler = AddressOf TriggerCompleteEvent

    Private isTaskValid As Boolean
    Private objValidTask As Task

    Private objAnimation_LoadTextVis As Storyboard = Nothing

    Private visLoadTextEventTask As TaskCompletionSource(Of Boolean)

    Private evtLoadText_FadeIn As EventHandler
    Private evtLoadText_FadeOutComplete As EventHandler

    Private objTextBrush As SolidColorBrush

    Private Const ContentBorder_Radius As Double = 12

    Private ReadOnly LoadDataIdx As New List(Of osLoadData) From
        {
            {New osLoadData(LoadStart, isLoading, LoadStatus_StartUp, 500, "Launching", objPreLoadDuration:=125)},
            {New osLoadData(LoadInit, isInit, LoadStatus_Init, 500, "Initializing Data", objPreLoadDuration:=150)},
            {New osLoadData(LoadPrefs, isPrefPrep, LoadStatus_PrefPrep, 500, "Loading Preferences", Function() PrepPrefs(), 220)},
            {New osLoadData(LoadUI, isLoadingUI, LoadStatus_LoadingUI, 450, "Loading Interface", Function() osHandler_UI.PrepAndLoadUI(), 350)},
            {New osLoadData(LoadConfig, isApplyConfig, LoadStatus_ApplyConfig, 475, "Applying Configuration", Function() osMenu_Init(), 220)},
            {New osLoadData(LoadService, isStartingSvc, LoadStatus_StartingSvc, 450, "Activating Service", Function() InputMonitor_Start(), 220)},
            {New osLoadData(LoadComplete, isStarting, LoadStatus_Starting, 450, "Starting osAutoCast", Function() LoadFinalize(), 500)}
        }

    Public Property osAutoCastVersion As String = "Ver 3.0"

    Public ReadOnly Property osAutoCastTitle As String
        Get
            Return $"osAutoCast {osAutoCastVersion} | By OddSoft"
        End Get
    End Property

    Public ReadOnly Property objLoadProg As osProgLoad
        Get
            Return Me.LoadingProgressBar
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

    Private Function FetchLoadData(getLoadStep As LoadStep) As osLoadData
        Return LoadDataIdx.First(
            Function(objLoadStep)
                Return objLoadStep.LoadingStep = getLoadStep
            End Function)
    End Function

    Private Function CreateVisArray() As TimelineCollection
        With New osLoadTextColors(objLoadText, objTextBrush)
            Return New TimelineCollection() From {
                {New ColorAnimation(.txtShown, .txtHidden,
                                    TimeSpan.FromMilliseconds(110), FillBehavior.HoldEnd)},
                {New ColorAnimation(.txtHidden, .txtShown,
                                    TimeSpan.FromMilliseconds(110), FillBehavior.HoldEnd)}}
        End With
    End Function

    Private Sub ComposeVisual()
        objAnimation_LoadTextVis = New Storyboard() With {
            .Children = CreateVisArray()
        }
    End Sub

    Private Sub ValidateTaskAndRun(objTask As Func(Of Task), ByRef objValidTask As Task, ByRef isValid As Boolean)
        If objTask Is Nothing Then
            isValid = False
            objValidTask = Nothing
        Else
            isValid = True
            objValidTask = objTask.Invoke()
        End If
    End Sub

    Private Function ChkFinalTask(objLoadingProgStatus As LoadingProgStatus) As Boolean
        Return objLoadingProgStatus = LoadStatus_Starting
    End Function

    Private Sub TriggerCompleteEvent(s As Object, e As EventArgs)
        RaiseEvent osLoadComplete(s, e)
        RemoveHandler objLoadProg.LoadProgComplete, evtLoadComplete
    End Sub

    Private Sub ApplyLoadText(txtLoad As String)
        objLoadText.Text = txtLoad
    End Sub

    Private Sub ComposeOutline(sender As Object, e As RoutedEventArgs) Handles LoadingContainerOutline.Loaded
        EstablishOutline(objOutline, ContentBorder_Radius)
    End Sub

End Class
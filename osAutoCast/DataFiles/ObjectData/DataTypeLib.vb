Imports System.Data
Imports System.IO
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports osAutoCast.DataTypeLib.AnimationObject
Imports osAutoCast.DataTypeLib.AnimationType
Imports osAutoCast.DataTypeLib.LoadStep
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.DataTypeLib.OverlayVisualType
Imports osAutoCast.DataTypeLib.VisualEasing
Imports osAutoCast.DataTypeLib.TriggerAction
Imports osAutoCast.DataTypeLib.TrayMenuVisuals
Imports osAutoCast.DataTypeLib.LoadTextVisualType
Imports osAutoCast.DataTypeLib.LoadTaskStatus
Imports System.Threading.Tasks.TaskCreationOptions
Imports osAutoCast.osShaderDataLib
Imports osDraw = System.Drawing
Imports osForms = System.Windows.Forms
Imports osIcons = System.Drawing.SystemIcons
Imports osProgBlendState = SharpDX.Direct3D11.BlendState
Imports osProgColor = SharpDX.Mathematics.Interop.RawColor4
Imports osRect = SharpDX.Mathematics.Interop
Imports osText = SharpDX.DirectWrite
Imports pxShader_Pixel = SharpDX.Direct3D11.PixelShader
Imports pxShader_Text = System.Windows.Media.Effects.PixelShader
Imports pxShader_Vertex = SharpDX.Direct3D11.VertexShader
Imports osColor = System.Windows.Media.Color
Imports osVisibility = System.Windows.Visibility

#Disable Warning BC42353

Public Module DataTypeLib

#Region "Input Montitor Operation Types"

    Public Enum MonitorStatus
        Watching
        Paused
        InCmd
        Starting
    End Enum

    Public Enum DetectOpts
        MonitorMouse
        MonitorMouseR
        MonitorShift
        MonitorOpts
        MonitorPopup
        MonitorAll
        MonitorAutoPassAbort
    End Enum

    Public Enum UpdateStatus
        ToEnabled
        ToDisabled
        CancelUpdate
    End Enum

    Public Enum UpdateStatusAction
        SetStatus_Enabled
        SetStatus_Disabled
        RetainStatus
    End Enum

    Public Enum InjectType
        Shift_D
        Shift_U
        Enter_D
        Enter_U
    End Enum

    Public Enum InputAction
        AC_Start
        AC_RTC
        AP_Start
        AP_Exec
    End Enum

#End Region

#Region "Progress Types"

    Public Enum ProgStatus
        Idle
        Running
        Success
        Fail
        StartAP
    End Enum

    Public Enum ProgAction
        Abort
        Activate
        Complete
        ResetProgress
    End Enum

    Public Enum ProgEvent
        Reset
        ClrMsg
        MaxFill
        Starter
        PrepMsg
        DispMsg
        ShowFull
        ShowFullMsg
    End Enum

    Public Enum ProgResult
        Completed
        Cancelled
    End Enum

    Public Enum ProgFlow
        Ascending
        Descending
    End Enum

    Public Enum ProgressMode
        ProgMode_AutoPass
        ProgMode_AutoCast
    End Enum

    Public Enum ProgColorObj
        BackG
        Active
        Msg
        Abort
        Clear
    End Enum

    Public Enum ProgVisOpts
        Performance
        Quality
    End Enum

    Public Enum MsgRenderType
        msgClear
        msgDisplay
    End Enum

    Public Enum ProgEaseVals
        eVal_x1 = 0.42
        eVal_y1 = 0.12
        eVal_x2 = 0.49
        eVal_y2 = 0.95
    End Enum

    <Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)>
    Public Structure ProgBarCB
        Public prevValue As Single
        Public currValue As Single
        Public invSize As Single
        Public flags As Single
        Public pcoloractive As osProgColor
        Public pcolorbg As osProgColor
        Public barOffset As Single
        Public pad0 As Single
        Public pad1 As Single
        Public pad2 As Single
    End Structure

#End Region

#Region "Trigger Types"

    Public Enum TriggerAction
        TriggerAutoCast
        TriggerAutoPass
        TriggerShowOpts
        TriggerShowMenu
        TriggerShowTrayMenu
        NoTrigger
    End Enum

    Public Enum TriggerType
        AutoCast
        AutoPass
        ShowPrefs
        ShowMenu
        ShowTrayMenu
    End Enum

    Public Enum TriggerValidation
        ValidAutoCast
        ValidAutoPass
        ValidTrigger
        ValidUtility
        InvalidTrigger
    End Enum

#End Region

#Region "UI Types"

    Public Enum RenderBitmapObj
        Progress
        Message
    End Enum

#End Region

#Region "General Types"

    Public Enum MsgBoxType
        isQuestion
        isAlert
    End Enum

    Public Enum PromptType
        Prefs_Save
        Prefs_Close
        GameMenu_Leave
        GameMenu_Restart
        CloseApp
        DisableService
    End Enum

    Public Enum PromptResponse
        isYes
        isNo
        isCancel
    End Enum

    Public Enum PrefBinder
        AC_Fuse
        AC_RTC
        AP_SafetyTimer
        GO_VisualQuality
    End Enum

    Public Enum BarLayoutMode
        StretchToWindow
        FixedSizeCentered
        FixedAt
    End Enum

    Public Enum MenuProperty
        propTexel
        propThickness
        propStroke
        propGlow
        propFade
        propSpread
        propStrokeColor
        propGlowColor
        propVerticalGlow
    End Enum

    Public Enum AnimationType
        aniOpen
        aniClose
    End Enum

    Public Enum AnimationObject
        aniPopup
        aniOverlay
    End Enum

    Public Enum AnimationVisual
        isScale
        isOpacity
    End Enum

    Public Enum PopupVisualType
        PopupVisual_Open
        PopupVisual_Close
        PopupVisual_CloseByBtn
        PopupVisual_CloseByCmd
    End Enum

    Public Enum OverlayVisualType
        OverlayVisual_Open
        OverlayVisual_Close
        OverlayVisual_CloseByBtn
        OverlayVisual_CloseByCmd
    End Enum

    Public Enum PopupCloseAction
        ClosePopup_Default
        ClosePopup_ByBtn
        ClosePopup_ByCmd
    End Enum

    Public Enum VisualAction
        VisualStarted
        VisualComplete
    End Enum

    Public Enum VisualEasing
        PopupOpen_Scale
        PopupOpen_ScalePrimary
        PopupOpen_Fade
        PopupClose
        PopupClose_ByBtn
        PopupClose_ByCmd
        OverlayOpen
        OverlayClose
        OverlayClose_ByBtn
        LoadText_Fade
    End Enum

    Public Enum TrayMenuVisuals
        TrayMenuVis
        GameMenuToggleVis
        StatusToggleVis
        MenuItemVis
        MenuItemBorderVis
        GameMenuItemVis
    End Enum

    Public Structure TrayMenuItemType
        Const MenuItemType_Status = "StatusToggleVis"
        Const MenuItemType_Game = "GameMenuToggleVis"
        Const MenuItemType_Default = "MenuItemVis"
        Const MenuItemType_Border = "MenuItemBorderVis"
        Const MenuItemType_GameItem = "GameMenuItemVis"
    End Structure

    Public Enum TrayMenuState
        TrayMenu_Close
        TrayMenu_Open
    End Enum

    Public Enum GameMenuState
        GameMenu_Close
        GameMenu_Open
    End Enum

    Public Enum GameMenuVisuals
        GameMenuVis_Height
        GameMenuVis_Opacity
        GameMenuVis_Visible
        GameMenuVis_Position
    End Enum

    Public Enum LoadingProgStatus
        LoadStatus_StartUp
        LoadStatus_Init
        LoadStatus_PrefPrep
        LoadStatus_LoadingUI
        LoadStatus_StartingSvc
        LoadStatus_Starting
        LoadStatus_ApplyConfig
    End Enum

    Public Enum LoadContentData
        isLoading
        isInit
        isPrefPrep
        isLoadingUI
        isStartingSvc
        isStarting
        isApplyConfig
    End Enum

    Public Enum LoadStep
        LoadStart
        LoadInit
        LoadPrefs
        LoadUI
        LoadConfig
        LoadService
        LoadComplete
    End Enum

    Public Enum LoadEventType
        LoadEv_Show
        LoadEv_Close
        LoadEv_Complete
    End Enum

    Public Enum LoaderEasing
        Linear
        EaseIn
        EaseOut
        EaseInOut
        SmoothStep
    End Enum

    Public Enum LoadTaskStatus
        TaskRunning
        TaskComplete
    End Enum

    Public Enum LoadTaskType
        Load_StartUp
        Load_Init
        Load_PrefPrep
        Load_PrefApply
        Load_Opts
        Load_InitShaders
        Load_Shaders
        Load_PopupMenu
        Load_ApplyConfig
        Load_StartingSvc
        Load_Starting
    End Enum

    Public Structure LoadTextVisual
        Const LoadTxt_In = "LoadTextVisuals_FadeIn"
        Const LoadTxt_Out = "LoadTextVisuals_FadeOut"
    End Structure

    Public Enum LoadTextVisualType
        LoadTextFade_In
        LoadTextFade_Out
    End Enum

    Public Enum osShaderType
        sTypePixel
        sTypeVertex
        sTypeText_G
        sTypeText_S
    End Enum

    Public Enum PrefType
        Pref_AutoCast
        Pref_AutoPass
        Pref_MainOpts
        Pref_GenOpts
    End Enum

    'Public Enum PreferenceType
    '    Pref_AutoCast_RTC
    '    Pref_AutoCast_Fuse
    '    Pref_AutoPass_SafetyTimer
    '    Pref_MainOpts_AP_W
    '    Pref_MainOpts_AP_H
    '    Pref_MainOpts_AP_UiW
    '    Pref_MainOpts_AP_UiH
    '    Pref_MainOpts_AC_W
    '    Pref_MainOpts_AC_H
    '    Pref_GenOpts_VisualQuality
    'End Enum

    Public Enum PrefSetting
        Pref_AutoCast_RTC
        Pref_AutoCast_Fuse
        Pref_AutoPass_SafetyTimer
        Pref_GenOpts_VisualQuality
    End Enum

    Public Enum PrefUI_State
        PrefUI_Open
        PrefUI_Close
    End Enum

    Public Enum PrefUI_VisStage
        PrefUI_VisInit
        PrefUI_VisFinish
    End Enum

#End Region

End Module

Public Class GridLengthAnimation
    Inherits AnimationTimeline

    Public Property From As GridLength?
    Public Property [To] As GridLength
    Public Property EasingFunction As IEasingFunction

    Public Overrides ReadOnly Property TargetPropertyType As Type
        Get
            Return GetType(GridLength)
        End Get
    End Property

    Public Overrides Function GetCurrentValue(
        defaultOriginValue As Object,
        defaultDestinationValue As Object,
        animationClock As AnimationClock) As Object

        Dim startVal As Double =
            If(From.HasValue, From.Value.Value, CType(defaultOriginValue, GridLength).Value)

        Dim endVal As Double = [To].Value

        Dim progress = animationClock.CurrentProgress.GetValueOrDefault()

        If EasingFunction IsNot Nothing Then
            progress = EasingFunction.Ease(progress)
        End If

        Dim value = startVal + (endVal - startVal) * progress
        Return New GridLength(value, GridUnitType.Pixel)
    End Function

    Protected Overrides Function CreateInstanceCore() As Freezable
        Return New GridLengthAnimation()
    End Function
End Class

Public Class osPrefVisData

    Public Property VisStage As PrefUI_VisStage
    Public Property VisData As String

    Public Sub New()
    End Sub

    Public Sub New(vStage As PrefUI_VisStage, vData As String)
        VisStage = vStage
        VisData = vData
    End Sub

End Class

Public Class osPrefDetails

    Public Property prefType As PrefType
    Public Property prefName As String

    Public Sub New()
    End Sub

    Public Sub New(pType As PrefType, pName As String)
        prefType = pType
        prefName = pName
    End Sub

End Class

Public NotInheritable Class TaskStatusReport

    Private ReadOnly _objTaskStatus As New TaskCompletionSource(
        Of LoadTaskStatus)(RunContinuationsAsynchronously)

    Public ReadOnly Property GetStatus As LoadTaskStatus
        Get
            Return If(_objTaskStatus.Task.IsCompleted,
                TaskComplete, TaskRunning)
        End Get
    End Property

    Public ReadOnly Property Completed As Task
        Get
            Return _objTaskStatus.Task
        End Get
    End Property

    Public ReadOnly Property IsCompleted As LoadTaskStatus
        Get
            Return If(_objTaskStatus.Task.IsCompleted, TaskComplete, TaskRunning)
        End Get
    End Property

    Public ReadOnly Property IsRunning As LoadTaskStatus
        Get
            Return If(Not _objTaskStatus.Task.IsCompleted, TaskRunning, TaskComplete)
        End Get
    End Property

    Friend Sub SetTaskComplete()
        _objTaskStatus.TrySetResult(TaskComplete)
    End Sub

    Friend Sub Cancel()
        _objTaskStatus.TrySetCanceled()
    End Sub

End Class

Public Class osLoader_Stage

    Public Property LoadStageData As osLoadStageData
    Public Property LoadTaskData As osLoadTaskData

    Public Sub New()
    End Sub

    Public Sub New(objStageData As osLoadStageData, objTaskData As osLoadTaskData)
        LoadStageData = objStageData
        LoadTaskData = objTaskData
    End Sub

End Class

Public Class osLoaderStageIdx

    Public Property LoadStages As osLoader_Stage()

    Public Sub New()
    End Sub

    Public Sub New(objStageData As osLoader_Stage())
        LoadStages = objStageData
    End Sub

End Class

Public Class osLoadTaskData

    Public Property LoadType As LoadTaskType
    Public Property LoadTask As Func(Of TaskStatusReport, Task)

    Public Sub New()
    End Sub

    Public Sub New(taskType As LoadTaskType, taskLoad As Func(Of TaskStatusReport, Task))
        LoadType = taskType
        LoadTask = taskLoad
    End Sub

End Class

Public Class osLoadStageData

    Public Property StartValue As Double
    Public Property EndValue As Double
    Public Property Duration As TimeSpan
    Public Property Easing As LoaderEasing

    Public Sub New()
    End Sub

    Public Sub New(sVal As Double, eVal As Double, pDur As Double)
        StartValue = sVal
        EndValue = eVal
        Duration = TimeSpan.FromMilliseconds(pDur)
        Easing = LoaderEasing.EaseInOut
    End Sub

End Class

Public Class GameMenuVisData

    Public Property visStart As Double = Nothing
    Public Property visEnd As Double = Nothing

    Public Property visVisibility As osVisibility = Nothing

    Public Sub New()
    End Sub

    Public Sub New(sVal As Double, eVal As Double, Optional vVal As osVisibility = Nothing)
        visStart = sVal
        visEnd = eVal
        visVisibility = vVal
    End Sub

End Class

Public Class osLoadTextColors

    Public Property txtHidden As osColor
    Public Property txtShown As osColor

    Public Property visStart As osColor
    Public Property visTarget As osColor

    Public Sub New()
    End Sub

    Public Sub New(ByRef objTextArea As TextBlock, ByRef objTextBrush As SolidColorBrush)
        objTextBrush = TryCast(objTextArea.Foreground, SolidColorBrush)

        With objTextBrush
            If .IsFrozen Then
                objTextBrush = .CloneCurrentValue()
                objTextArea.Foreground = objTextBrush
            End If

            With .Color
                txtHidden = osColor.FromArgb(0, .R, .G, .B)
                txtShown = osColor.FromArgb(255, .R, .G, .B)
            End With
        End With
    End Sub

    Public Sub New(LoadVisType As LoadTextVisualType, ByRef objTextArea As TextBlock, ByRef objTextBrush As SolidColorBrush)
        objTextBrush = TryCast(objTextArea.Foreground, SolidColorBrush)

        With objTextBrush
            If .IsFrozen Then
                objTextBrush = .CloneCurrentValue()
                objTextArea.Foreground = objTextBrush
            End If

            With .Color
                visStart = osColor.FromArgb(If(LoadVisType = LoadTextFade_In, 0, 255), .R, .G, .B)
                visTarget = osColor.FromArgb(If(LoadVisType = LoadTextFade_In, 255, 0), .R, .G, .B)
            End With
        End With
    End Sub

End Class

Public Class LoadDataObject

    Public Property SetProgVal As Func(Of Task)

End Class

Public Class osLoadProgData

    Public Property SetProgVal As Double
    Public Property NextProgVal As Double

    Public Property LastTaskVal As Boolean

    Public Sub New()
    End Sub

    Public Sub New(sVal As Double, nVal As Double, Optional tVal As Boolean = False)
        SetProgVal = sVal
        NextProgVal = nVal
        LastTaskVal = tVal
    End Sub

End Class

Public Class osVisualEaseDetails

    Public Property EaseName As VisualEasing
    Public Property EaseData As EasingFunctionBase

    Public Sub New()
    End Sub

    Public Sub New(eName As VisualEasing)
        EaseName = eName
        EaseData = SetVisualEase(eName)
    End Sub

    Public Sub New(eName As VisualEasing, eData As EasingFunctionBase)
        EaseName = eName
        EaseData = eData
    End Sub

End Class

Public Module osVisEaseData

    Public VisualEaseIdx As New List(Of osVisualEaseDetails) From {
        {New osVisualEaseDetails(PopupOpen_Scale)}, {New osVisualEaseDetails(PopupOpen_ScalePrimary)},
        {New osVisualEaseDetails(PopupOpen_Fade)}, {New osVisualEaseDetails(PopupClose)},
        {New osVisualEaseDetails(PopupClose_ByCmd)}, {New osVisualEaseDetails(PopupClose_ByBtn)},
        {New osVisualEaseDetails(OverlayOpen)}, {New osVisualEaseDetails(OverlayClose)},
        {New osVisualEaseDetails(OverlayClose_ByBtn)}, {New osVisualEaseDetails(LoadText_Fade)}
    }

    Public Function GetVisualEase(objVisE As VisualEasing) As EasingFunctionBase
        Return VisualEaseIdx.First(Function(selVis)
                                       Return selVis.EaseName = objVisE
                                   End Function).EaseData
    End Function

    Public Function SetVisualEase(objVisE As VisualEasing) As EasingFunctionBase
        Select Case objVisE
            Case PopupOpen_Scale : Return New ExponentialEase() With {
                    .EasingMode = EasingMode.EaseIn
                }
            Case PopupOpen_ScalePrimary : Return New BackEase() With {
                    .EasingMode = EasingMode.EaseOut,
                    .Amplitude = 2
                }
            Case PopupOpen_Fade : Return New ExponentialEase() With {
                    .EasingMode = EasingMode.EaseIn
                }
            Case PopupClose : Return New ExponentialEase() With {
                    .EasingMode = EasingMode.EaseInOut,
                    .Exponent = 6
                }
            Case PopupClose_ByCmd : Return New ExponentialEase() With {
                    .EasingMode = EasingMode.EaseInOut,
                    .Exponent = 6
                }
            Case PopupClose_ByBtn : Return New ExponentialEase() With {
                    .EasingMode = EasingMode.EaseInOut,
                    .Exponent = 5
                }
            Case OverlayOpen : Return New QuadraticEase() With {
                    .EasingMode = EasingMode.EaseInOut
                }
            Case OverlayClose : Return New QuadraticEase() With {
                    .EasingMode = EasingMode.EaseInOut
                }
            Case OverlayClose_ByBtn : Return New QuadraticEase() With {
                    .EasingMode = EasingMode.EaseOut
                }
            Case LoadText_Fade : Return New CubicEase() With {
                    .EasingMode = EasingMode.EaseOut
                }
        End Select
    End Function

End Module

Namespace GameMenuOpts

    Public Enum GameMenuItem
        ShowStart
        ShowClose
    End Enum

End Namespace

Public Class osShaderDataLib

    Public Interface iPxShader
        ReadOnly Property iShaderType As osShaderType
        Function sText_G() As pxShader_Text
        Function sText_S() As pxShader_Text
        Function sPixel() As pxShader_Pixel
        Function sVertex() As pxShader_Vertex
    End Interface

    Public Class pxShaderPixel
        Implements iPxShader

        Public Property pxShaderObj As pxShader_Pixel

        Public Sub New(ShaderObj As pxShader_Pixel)
            Me.pxShaderObj = ShaderObj
        End Sub

        Public ReadOnly Property iShaderType As osShaderType Implements iPxShader.iShaderType
            Get
                Return sTypePixel
            End Get
        End Property

        Public Function isShaderVertex() As pxShader_Vertex Implements iPxShader.sVertex
            Return Nothing
        End Function

        Public Function isShaderText_G() As pxShader_Text Implements iPxShader.sText_G
            Return Nothing
        End Function

        Public Function isShaderText_S() As pxShader_Text Implements iPxShader.sText_S
            Return Nothing
        End Function

        Public Function isShaderPixel() As pxShader_Pixel Implements iPxShader.sPixel
            Return pxShaderObj
        End Function

    End Class

    Public Class pxShaderText_G
        Implements iPxShader

        Public Property pxShaderEff As pxShader_Text

        Public Sub New(ShaderEff As pxShader_Text)
            Me.pxShaderEff = ShaderEff
        End Sub

        Public ReadOnly Property iShaderType As osShaderType Implements iPxShader.iShaderType
            Get
                Return sTypeText_G
            End Get
        End Property

        Public Function isShaderObject() As pxShader_Pixel Implements iPxShader.sPixel
            Return Nothing
        End Function

        Public Function isShaderVertex() As pxShader_Vertex Implements iPxShader.sVertex
            Return Nothing
        End Function

        Public Function isShaderText_G() As pxShader_Text Implements iPxShader.sText_G
            Return pxShaderEff
        End Function

        Public Function isShaderText_S() As pxShader_Text Implements iPxShader.sText_S
            Return Nothing
        End Function

    End Class

    Public Class pxShaderText_S
        Implements iPxShader

        Public Property pxShaderEff As pxShader_Text

        Public Sub New(ShaderEff As pxShader_Text)
            Me.pxShaderEff = ShaderEff
        End Sub

        Public ReadOnly Property iShaderType As osShaderType Implements iPxShader.iShaderType
            Get
                Return sTypeText_S
            End Get
        End Property

        Public Function isShaderObject() As pxShader_Pixel Implements iPxShader.sPixel
            Return Nothing
        End Function

        Public Function isShaderVertex() As pxShader_Vertex Implements iPxShader.sVertex
            Return Nothing
        End Function

        Public Function isShaderText_G() As pxShader_Text Implements iPxShader.sText_G
            Return Nothing
        End Function

        Public Function isShaderText_S() As pxShader_Text Implements iPxShader.sText_S
            Return pxShaderEff
        End Function

    End Class

    Public Class pxShaderVertex
        Implements iPxShader

        Public Property pxShaderVer As pxShader_Vertex

        Public Sub New(ShaderVer As pxShader_Vertex)
            Me.pxShaderVer = ShaderVer
        End Sub

        Public ReadOnly Property iShaderType As osShaderType Implements iPxShader.iShaderType
            Get
                Return sTypeVertex
            End Get
        End Property

        Public Function isShaderObject() As pxShader_Pixel Implements iPxShader.sPixel
            Return Nothing
        End Function

        Public Function isShaderVertex() As pxShader_Vertex Implements iPxShader.sVertex
            Return pxShaderVer
        End Function

        Public Function isShaderText_G() As pxShader_Text Implements iPxShader.sText_G
            Return Nothing
        End Function

        Public Function isShaderText_S() As pxShader_Text Implements iPxShader.sText_S
            Return Nothing
        End Function

    End Class

End Class

Public Class idxShaderRecord

    Public Property ID As String
    Public Property ShaderData As iPxShader

    Public Sub New()
    End Sub

    Public Sub New(idxID As String, idxShader As iPxShader)
        ID = idxID
        ShaderData = idxShader
    End Sub

End Class

Public Module CoreDataLibe
    Public ReadOnly Property PrefTables As New osPref_DataTable()
End Module

Public Class osShaderDetails

    Public Property ShaderName As String
    Public Property ShaderType As osShaderType

    Public Sub New()
    End Sub

    Public Sub New(sName As String, sType As osShaderType)
        ShaderName = sName
        ShaderType = sType
    End Sub

End Class

Public Class osPref_DataVQ
    Public Property vqID As String
    Public Property vqN As String

    Public Sub New()
    End Sub

    Public Sub New(vID As String, vN As String)
        vqID = vID
        vqN = vN
    End Sub

End Class

Public Class osPref_DataTable

    Private _osPrefVQ_DT As DataTable
    Public Property osPrefVQ_DT As DataTable
        Get
            Return _osPrefVQ_DT
        End Get
        Set(ByVal value As DataTable)
            _osPrefVQ_DT = value
        End Set
    End Property

    Public Sub New()
        osPrefVQ_DT = New DataTable()

        PopulateVQ_Cols()
        PopulateDataVQ()
    End Sub

    Public Sub PopulateVQ_Cols()
        osPrefVQ_DT.Columns.Add("vqIdx", GetType(Integer))
        osPrefVQ_DT.Columns.Add("vqName", GetType(String))
    End Sub

    Private Sub PopulateDataVQ()
        osPrefVQ_DT.Rows.Add(0, "Performance")
        osPrefVQ_DT.Rows.Add(1, "Quality")


    End Sub

End Class

Public Class ProgVisualQuality

    Public Property Flag As Single
    Public Property BlendState As osProgBlendState

    Public Sub New()

    End Sub

    Public Sub New(vQualitySetting As ProgVisOpts, ByRef objBlendState As osProgBlendState)
        Select Case vQualitySetting
            Case ProgVisOpts.Performance
                Flag = 0.0F
                BlendState = objBlendState
            Case ProgVisOpts.Quality
                Flag = 4.0F
                BlendState = objBlendState
        End Select
    End Sub

End Class

Public Class ProgVisualQualityData

    Public Property vqIdx As Integer
    Public Property vqName As String

    Public Sub New()

    End Sub

    Public Sub New(vIdx As Integer, vName As String)
        Me.vqIdx = vIdx
        Me.vqName = vName
    End Sub

End Class

Public Class InjectInputData
    Implements IDisposable

    Private disposedValue As Boolean

    Public Property KeyType As UShort
    Public Property InjectData As UInteger

    Public Sub New()

    End Sub

    Public Sub New(iType As InjectType)
        Select Case iType
            Case InjectType.Shift_D
                Me.KeyType = &HA1
                Me.InjectData = 0
            Case InjectType.Shift_U
                Me.KeyType = &HA1
                Me.InjectData = &H2
            Case InjectType.Enter_D
                Me.KeyType = &HD
                Me.InjectData = 0
            Case InjectType.Enter_U
                Me.KeyType = &HD
                Me.InjectData = &H2
        End Select
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
            End If

            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class

Public Class PrefBindData

    Public Property BindType As String
    Public Property BindRecord As String

    Public Sub New()

    End Sub

    Public Sub New(bType As String, bRecord As String)
        BindType = bType
        BindRecord = bRecord
    End Sub

End Class

Public Class ProgTextPos
    Public Property txtX As Integer
    Public Property txtY As Integer

    Public Sub New()
    End Sub

    Public Sub New(pGraphics As System.Drawing.Graphics, pGUI As System.Windows.Forms.Form)
        Dim sizeF = pGraphics.MeasureString(osFuncLib_Progress.progDispMsg, osFuncLib_Progress.progFont_AC)
        Me.txtX = CInt(Math.Round(pGUI.Width - sizeF.Width) / 2)
        Me.txtY = CInt(Math.Round(pGUI.Height - sizeF.Height) / 2)
    End Sub

    Public Sub New(pGraphics As System.Drawing.Graphics, pObj As Control)
        Dim sizeF = pGraphics.MeasureString(osFuncLib_Progress.progDispMsg, osFuncLib_Progress.progFont_AP)
        Dim size As System.Drawing.Size = osFuncLib_Progress.CalcProgSize(DataTypeLib.TriggerType.AutoPass)
        Me.txtX = CInt(Math.Round(size.Width - sizeF.Width) / 2)
        Me.txtY = CInt(Math.Round(size.Height - sizeF.Height) / 2)
    End Sub
End Class

Public Class ProgSizeReport
    Public Property pWidth As Integer
    Public Property pHeight As Integer

    Public Sub New()

    End Sub

    Public Sub New(pW As Integer, pH As Integer)
        pWidth = pW
        pHeight = pH
    End Sub

End Class

Public Class ProgMsg

    Public Property txtComposed As FormattedText
    Public Property txtLocation As Point

    Public Sub New(txtMsg As String, pType As TriggerType, Optional isAP As Boolean = False)
        txtComposed = GenFormattedText(txtMsg, isAP)

        With CoreDataLib.FetchProgSizeReport(pType)
            txtLocation = New Point((.Item("pW") - txtComposed.Width) \ 2,
                                    (.Item("pH") - txtComposed.Height) \ 2)
        End With
    End Sub

    Private Function GenFormattedText(txtMsg As String, Optional isAP As Boolean = False) As FormattedText
        Return New FormattedText(txtMsg, Globalization.CultureInfo.CurrentCulture,
                                 FlowDirection.LeftToRight, ComposeTypeFace(isAP),
                                 DetermineFontSize(txtMsg, isAP), Brushes.Black, 1.0)
    End Function

    Private Function DetermineFontSize(txtMsg As String, isAP As Boolean) As Double
        If isAP Then
            Return CalculateFontSize(txtMsg.Length)
        Else
            Return 14
        End If
    End Function

    Private Function CalculateFontSize(txtLength As Integer) As Double
        Select Case txtLength
            Case < 40 : Return 15
            Case >= 40 : Return 12
        End Select
    End Function

    Private Function ComposeTypeFace(Optional isAP As Boolean = False) As Typeface
        If isAP Then
            Return New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal,
                                FontWeights.Bold, FontStretches.Normal)
        Else
            Return New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal,
                            FontWeights.Bold, FontStretches.Normal)
        End If
    End Function

End Class

Public Class ProgressMsg

    Public Property MsgText As String
    Public Property Format As osText.TextFormat
    Public Property Location As osRect.RawRectangleF

    Public Sub New()

    End Sub

    Public Sub New(txtMsg As String, pType As TriggerType,
                   ByRef pWriteFactory As osText.Factory, ByRef pFormat As osText.TextFormat)

        MsgText = txtMsg

        Format = ApplyMsgFormat(pWriteFactory)
        Location = SetMsgLocation(pType)

        pFormat = Format
    End Sub

    Private Function ApplyMsgFormat(ByRef pWriteFactory As osText.Factory) As osText.TextFormat
        Try
            Return New osText.TextFormat(pWriteFactory, "Trebuchet MS",
                                         osText.FontWeight.Bold, osText.FontStyle.Normal,
                                         osText.FontStretch.Condensed, 14.0F) With {
                                             .TextAlignment = osText.TextAlignment.Center,
                                             .ParagraphAlignment = osText.ParagraphAlignment.Center
                                        }
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Function SetMsgLocation(pType As TriggerType) As osRect.RawRectangleF
        With CoreDataLib.FetchProgSizeReport(pType)
            Return New osRect.RawRectangleF(0, 0, .Item("pW"), .Item("pH"))
        End With
    End Function

End Class

Public Class ProgEdgeObj

    Public Property EdgeX As Integer
    Public Property EdgeW As Integer
    Public Property EdgeBrush As System.Windows.Media.Brush

    Public Sub New()

    End Sub

    Public Sub New(edgeOld As Integer, edgeNew As Integer, activeBrush As System.Windows.Media.Brush, GetBrush As System.Windows.Media.Brush)
        If edgeNew > edgeOld Then
            EdgeX = edgeOld
            EdgeW = edgeNew - edgeOld
            EdgeBrush = activeBrush
        Else
            EdgeX = edgeNew
            EdgeW = edgeOld - edgeNew
            EdgeBrush = GetBrush
        End If
    End Sub

End Class

Public Class ProgEdgeData

    Public Property EdgeOld As Integer
    Public Property EdgeNew As Integer

    Public Sub New()
    End Sub

    Public Sub New(progVal As Double, progWidth As Double, ByRef objChunk As Double)
        EdgeOld = EdgeFromChunk(objChunk, progWidth)
        objChunk = progVal
        EdgeNew = EdgeFromChunk(objChunk, progWidth)
    End Sub

    Private Function EdgeFromChunk(chunkVal As Double, progW As Double) As Integer
        If progW <= 0 Then Return 0
        Return CInt(Math.Round(progW * chunkVal))
    End Function

End Class

Public Class osLoadData

    Public Property LoadingStep As LoadStep
    Public Property LoadContent As LoadContentData
    Public Property LoadProgStatus As LoadingProgStatus

    Public Property LoadMsg As String

    Public Property LoadDuration As Integer
    Public Property PreLoadDuration As Integer

    Public Property LoadProcess As Func(Of Task)

    Public Sub New()
    End Sub

    Public Sub New(objLoadProcess As Func(Of Task),
                   objLoadDuration As Integer,
                   objLoadProgStatus As LoadingProgStatus)

        Me.LoadProcess = objLoadProcess
        Me.LoadDuration = objLoadDuration
        Me.LoadProgStatus = objLoadProgStatus

    End Sub

    Public Sub New(objLoadingStep As LoadStep, objLoadContent As LoadContentData,
                   objLoadProgStatus As LoadingProgStatus, objLoadDuration As Integer, objLoadMsg As String,
                   Optional objLoadProcess As Func(Of Task) = Nothing, Optional objPreLoadDuration As Integer = 0)

        Me.LoadingStep = objLoadingStep

        Me.LoadContent = objLoadContent
        Me.LoadProgStatus = objLoadProgStatus

        Me.LoadMsg = objLoadMsg

        Me.LoadProcess = objLoadProcess
        Me.LoadDuration = objLoadDuration

        Me.PreLoadDuration = objPreLoadDuration

    End Sub

End Class

Public Module osPrefsLib

    Public DisposeUI_Prefs As Action(Of osPrefs_GUI) =
        Sub(objGui_Prefs As osPrefs_GUI)
            If objGui_Prefs IsNot Nothing Then
                With objGui_Prefs
                    Try
                        If .IsLoaded Then
                            .Opacity = 0
                            .DataContext = Nothing
                            .IsHitTestVisible = False
                            .Close()
                        End If
                    Catch : End Try
                End With
            End If
    End Sub

End Module

Public Module osPopupMenuLib

    Public DisposeUI_PopupMenu As Action(Of osPopupMenu_GUI) =
        Sub(objGui_PopupMenu As osPopupMenu_GUI)
            If objGui_PopupMenu IsNot Nothing Then
                With objGui_PopupMenu
                    Try
                        If .IsLoaded Then
                            .IsHitTestVisible = False : .ShowInTaskbar = False
                            .DataContext = Nothing : .Opacity = 0
                        End If
                    Catch : End Try
                End With
            End If
        End Sub

    Public DisposeUI_PopupMenuOverlay As Action(Of MenuOverlayWindow) =
        Sub(objGui_PopupMenuOverlay As MenuOverlayWindow)
            If objGui_PopupMenuOverlay IsNot Nothing Then
                With objGui_PopupMenuOverlay
                    Try
                        RemoveHandler .MouseDown, osHandler_UI.pmFunc_TerminatePopupMenu

                        If .IsLoaded Then
                            .Opacity = 0
                            .IsHitTestVisible = False
                            .ShowInTaskbar = False
                            .Close()
                        End If
                    Catch : End Try
                End With
            End If
        End Sub

    Public DisposeUI_TrayOverlay As Action(Of MenuOverlayWindow) =
        Sub(objGui_PopupMenuOverlay As MenuOverlayWindow)
            If objGui_PopupMenuOverlay IsNot Nothing Then
                With objGui_PopupMenuOverlay
                    Try
                        If .IsLoaded Then
                            .Opacity = 0
                            .IsHitTestVisible = False
                            .ShowInTaskbar = False
                            .Close()
                        End If
                    Catch : End Try
                End With
            End If
        End Sub

    Private Sub ExecPrepUI_TrayMenuOverlay(objGui_PopupMenuOverlay As MenuOverlayWindow)
        With objGui_PopupMenuOverlay
            .PrepTrayMenuOverlay()
            .Show()
        End With
    End Sub

    Public Function GeneratePopupMenuGUI() As Func(Of osPopupMenu_GUI)
        Return Function()
                   Return PrepDispatcher().Invoke(
                       Function()
                           Dim objWin_PopupMenu = New osPopupMenu_GUI()
                           AddHandler objWin_PopupMenu.Closed, AddressOf osHandler_UI.PrepDispatch

                           Return objWin_PopupMenu
                       End Function)
               End Function
    End Function

    Public Async Function GeneratePopupMenuGUIAsync() As Task(Of osPopupMenu_GUI)
        Return Await PrepDispatcher().InvokeAsync(
        Function()
            Dim objWin_PopupMenu = New osPopupMenu_GUI()
            AddHandler objWin_PopupMenu.Closed,
                       AddressOf osHandler_UI.PrepDispatch
            Return objWin_PopupMenu
        End Function,
        DispatcherPriority.Background
    )
    End Function


    Public Function GeneratePopupMenuOverlayGUI(Optional isFromTray As Boolean = False) As Func(Of MenuOverlayWindow)
        Return Function()
                   Return PrepDispatcher().Invoke(
                       Function()
                           Return New MenuOverlayWindow(isFromTray)
                       End Function)
               End Function
    End Function

    Public Async Function GeneratePopupMenuOverlayGUIAsync(
    Optional isFromTray As Boolean = False
) As Task(Of MenuOverlayWindow)

        Return Await PrepDispatcher().InvokeAsync(
        Function()
            Return New MenuOverlayWindow(isFromTray)
        End Function,
        DispatcherPriority.Background
    )
    End Function


    Public Function GenerateTrayMenuGUI() As Func(Of osTrayMenu_GUI)
        Return Function()
                   Return PrepDispatcher().Invoke(
                       Function()
                           Return New osTrayMenu_GUI()
                       End Function)
               End Function
    End Function

    Public Async Function GenerateTrayMenuGUIAsync() As Task(Of osTrayMenu_GUI)
        Return Await PrepDispatcher().InvokeAsync(
        Function()
            Return New osTrayMenu_GUI()
        End Function,
        DispatcherPriority.Background
    )
    End Function


    Public DisplayUI_TrayOverlay As Action(Of MenuOverlayWindow) = AddressOf ExecPrepUI_TrayMenuOverlay

End Module

Public Class GUI_PrepData
    Implements IDisposable

    Private disposedValue As Boolean

    Public Property guiAction As Action(Of Window)
    Public Property guiDispatch As Dispatcher
    Public Property guiIsLoaded As Boolean

    Public Sub New()
    End Sub

    Private Sub guiAction_AutoPass(objGui As progGui_AutoPass)
        With objGui
            Try
                If .IsLoaded Then
                    .IsHitTestVisible = False
                    .Opacity = 0
                    .Close()
                End If
            Catch

            End Try
        End With
    End Sub

    Private Sub guiAction_PopupMenu(objGui_PopupMenu As osPopupMenu_GUI)
        With objGui_PopupMenu
            Try
                If .IsLoaded Then
                    .IsHitTestVisible = False
                    .Opacity = 0
                    .DataContext = Nothing
                    .Close()
                End If
            Catch

            End Try
        End With
    End Sub

    Private Sub guiAction_PopupMenuOverlay(objGui_PopupMenuOverlay As MenuOverlayWindow)
        With objGui_PopupMenuOverlay
            Try
                If .IsLoaded Then
                    .Opacity = 0
                    .IsHitTestVisible = False
                    .ShowInTaskbar = False
                    .Close()
                End If
            Catch

            End Try
        End With
    End Sub

    Public Sub New(objGUI As Window)
        guiAction = Sub()
                        Try
                            If objGUI.IsLoaded Then
                                objGUI.IsHitTestVisible = False
                                objGUI.Opacity = 0
                                objGUI.Close()
                            End If
                        Catch

                        End Try
                    End Sub

        guiDispatch = objGUI.Dispatcher
        guiIsLoaded = guiDispatch IsNot Nothing AndAlso Not guiDispatch.HasShutdownStarted
    End Sub

    Public Sub New(guiTrigger As TriggerAction, objWin As Window, Optional isMenuOverlay As Boolean = False)

        Select Case guiTrigger
            Case TriggerAutoCast
                guiDispatch = objWin.Dispatcher
            Case TriggerAutoPass
                guiAction = AddressOf guiAction_AutoPass
                guiDispatch = objWin.Dispatcher
            Case TriggerShowMenu
                If isMenuOverlay Then
                    guiAction = AddressOf guiAction_PopupMenuOverlay
                    guiDispatch = objWin.Dispatcher
                Else
                    guiAction = AddressOf guiAction_PopupMenu
                    guiDispatch = objWin.Dispatcher
                End If
        End Select

        guiIsLoaded = guiDispatch IsNot Nothing AndAlso Not guiDispatch.HasShutdownStarted
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                If guiAction IsNot Nothing Then
                    guiAction = Nothing
                End If

                If guiDispatch IsNot Nothing Then
                    guiDispatch = Nothing
                End If
            End If

            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class

Public Class ProgTimeData

    Public Property Starting As Long
    Public Property Ending As Long

    Public Sub New(vFuse As Integer)
        Starting = Environment.TickCount
        Ending = Starting + vFuse
    End Sub

End Class

Public Class ProgressEvent

    Public Property evDispatch As Dispatcher
    Public evAction As Action(Of ProgressEventData)

    Public Sub New(pEventElement As OddLib_ProgressBar)
        Me.evDispatch = pEventElement.Parent.Dispatcher
        Me.evAction = AddressOf pEventElement.PerformProgressEvent
    End Sub

    Public Sub New(pEventElement As ProgBarGui_AutoCast)
        Me.evDispatch = Application.Current.Dispatcher
        Me.evAction = AddressOf pEventElement.PerformProgressEvent
    End Sub

    Public Sub New()

    End Sub

End Class

Public Class TriggerHandlerIdx

    Public Property TriggerHandlerIdxData As TriggerData()

    Public Sub New()

    End Sub

    Public Sub New(ParamArray objTriggerData() As TriggerData)
        For Each objTriggerHandleData In objTriggerData
            TriggerHandlerIdxData.Append(objTriggerHandleData)
        Next
    End Sub

    Public Sub New(ParamArray objHandle As (objHandleAction As TriggerAction, objHandleEvent As Func(Of Task))())
        For Each objTriggerHandleData In objHandle
            With objTriggerHandleData
                TriggerHandlerIdxData.Append(New TriggerData(.objHandleAction, .objHandleEvent))
            End With
        Next
    End Sub

End Class

Public Class TriggerData

    Public Property HandleAction As TriggerAction
    Public Property HandleEvent As Func(Of Task)

    Public Sub New()

    End Sub

    Public Sub New(objHandleAction As TriggerAction, objHandleEvent As Func(Of Task))
        Me.HandleAction = objHandleAction
        Me.HandleEvent = objHandleEvent
    End Sub

End Class

Public Class ProgressEventData

    Public Property evType As ProgEvent
    Public Property evTrigger As TriggerType
    Public Property evDispMsg As String

    Public Sub New()

    End Sub

    Public Sub New(eType As ProgEvent, evTrig As TriggerType, Optional evMsg As String = Nothing)
        Me.evType = eType
        Me.evTrigger = evTrig
        Me.evDispMsg = evMsg
    End Sub

End Class

Public Class BindingDef
    Public Property Control As osForms.Control
    Public Property ControlProp As String
    Public Property DataSource As Object
    Public Property DataProp As String
End Class

Public Class PrefRecordIndex

    Public Property RecIdx As List(Of PrefRecord)
    Public Property RecDataTable As DataTable

    Public Sub New()
        RecIdx = New List(Of PrefRecord)
    End Sub

    Public Sub CreateRecord(pRecType As String, ParamArray pRecord() As PrefRecordData)
        Me.RecIdx.Add(New PrefRecord(pRecType, pRecord.ToArray()))
    End Sub

    Public Function FetchPref(pRecType As String, pName As String) As String
        Return RecIdx.
            FirstOrDefault(Function(pRec) pRec.PrefType.ToLower() =
            pRecType.ToLower()).PrefRecord.
            FirstOrDefault(Function(recData)
                               Return recData.PrefName.ToLower() = pName.ToLower()
                           End Function).PrefVal
    End Function

    Public Sub SavePref(pType As String, pName As String, pNewVal As String)
        With GetRecordData(RetrieveRecord(pType), pName)
            .PrefVal = pNewVal
        End With
    End Sub

    Private Function RetrieveRecord(pType As String) As PrefRecord
        Return RecIdx.
            FirstOrDefault(Function(r)
                               Return r.PrefType.ToLower() = pType.ToLower()
                           End Function)
    End Function

    Private Function GetRecordData(pRecord As PrefRecord, pName As String) As PrefRecordData
        Return pRecord.PrefRecord.
            FirstOrDefault(Function(d)
                               Return d.PrefName.ToLower() = pName.ToLower()
                           End Function)
    End Function

    Public Sub SavePrefsFile()

        CoreDataLib.osPrefStoreData.UpdatePrefStore()

        Using pWriter As New System.IO.StreamWriter(CoreDataLib.osPrefFile, False)
            pWriter.WriteLine("PrefCatalog_")

            For Each prefRec As PrefRecord In Me.RecIdx
                WritePrefRecords(prefRec, pWriter)
            Next

            pWriter.WriteLine("_PrefCatalog")
        End Using
    End Sub

    Private Sub SavePrefsToFile()
        Using pWriter As New System.IO.StreamWriter(CoreDataLib.osPrefFile, False)
            pWriter.WriteLine("PrefCatalog_")

            For Each prefRec As PrefRecord In Me.RecIdx
                WritePrefRecords(prefRec, pWriter)
            Next

            pWriter.WriteLine("_PrefCatalog")
        End Using
    End Sub

    Private Sub WritePrefRecords(pRecord As PrefRecord, ByRef objPrefWriter As StreamWriter)
        objPrefWriter.WriteLine($"|{pRecord.PrefType}-")

        For Each prefRec In pRecord.PrefRecord
            objPrefWriter.WriteLine(FormatPrefData(prefRec))
        Next

        objPrefWriter.WriteLine($"-{pRecord.PrefType}|")
    End Sub

    Private Function FormatPrefData(prefRec As PrefRecordData) As String
        Return $"{prefRec.PrefName}:{prefRec.PrefVal}"
    End Function

    Private Function PrepPref(pRecData As PrefRecordData, valType As Type) As Object
        Return Convert.ChangeType(pRecData.PrefVal, valType)
    End Function

    Private Function GetPrefTypes() As Type
        Return GetType(CoreDataLib)
    End Function

    Private Function FetchPrefVar(recType As String, recName As String) As String
        Return $"{recType}_{recName}"
    End Function

End Class

Public Class PrefRecord

    Public Property PrefType As String
    Public Property PrefRecord As List(Of PrefRecordData)

    Public Sub New()
    End Sub

    Public Sub New(pType As String)
        Me.PrefType = pType
        Me.PrefRecord = New List(Of PrefRecordData)
    End Sub

    Public Sub New(pType As String, ParamArray pRecord() As PrefRecordData)
        Me.PrefType = pType
        Me.PrefRecord = New List(Of PrefRecordData)(pRecord)
    End Sub

    Public Sub AddRecordData(pRecData As PrefRecordData)
        Me.PrefRecord.Add(pRecData)
    End Sub

End Class

Public Class PrefRecordData

    Public Property PrefName As String
    Public Property PrefVal As String

    Public Sub New()
    End Sub

    Public Sub New(prefLine As String)
        With prefLine.Split({":"c}, 2).ToList()
            Me.PrefName = .Item(0).Trim()
            Me.PrefVal = .Item(1).Trim()
        End With
    End Sub

    Public Sub New(pName As String, pVal As String)
        Me.PrefName = pName
        Me.PrefVal = pVal
    End Sub

End Class

Public Class osMenuFuncData

    Public Property osMenuFunc_GetStatus As Func(Of Boolean)
    Public Property osMenuFunc_ApplyStatus As Action(Of Boolean)

    Public Sub New(objFunc_GetStatus As Func(Of Boolean),
                   objFunc_ApplyStatus As Action(Of Boolean))

        Me.osMenuFunc_GetStatus = objFunc_GetStatus
        Me.osMenuFunc_ApplyStatus = objFunc_ApplyStatus

    End Sub

End Class

Public Class objTriggerHandler
    Public Property HandleAction As TriggerAction
    Public Property HandleEvent As Func(Of Task)
End Class

Public NotInheritable Class TriggerInvoker

    Public Shared cmdDispatcher As Dispatcher

    Public Shared Sub SetDispatcher()
        cmdDispatcher = Nothing
        cmdDispatcher = Application.Current.Dispatcher
    End Sub

End Class

Public Class ResponseBox
    Inherits osForms.Form

    Private Const WS_EX_NOACTIVATE As Integer = &H8000000
    Private Const WS_EX_TOOLWINDOW As Integer = &H80
    Private Const WM_MOUSEACTIVATE As Integer = &H21
    Private Const MA_NOACTIVATE As Integer = 3

    Private Const IconSize As Integer = 48

    Private lblMsg As New osForms.Label
    Private picIcon As New osForms.PictureBox

    Private ReadOnly objContentContainer As New osForms.TableLayoutPanel()
    Private ReadOnly objBtnPanel As New osForms.FlowLayoutPanel()

    Public Sub New(msgText As String, msgTitle As String, msgType As MsgBoxType)
        PrepContentContainer()

        GenContentContainer()

        PrepMsgIcon(msgType)
        PrepMsg(msgText)
        PrepBtnContainer(msgType)
        PrepPopupGui(msgTitle)

        InitContentContainer()
    End Sub

    Private Sub PrepContentContainer()
        objContentContainer.SuspendLayout()

        CType(picIcon, System.ComponentModel.ISupportInitialize).BeginInit()
        objBtnPanel.SuspendLayout()

        Me.SuspendLayout()
    End Sub

    Private Sub InitContentContainer()
        objContentContainer.ResumeLayout(False)
        objContentContainer.PerformLayout()
        CType(picIcon, System.ComponentModel.ISupportInitialize).EndInit()
        objBtnPanel.ResumeLayout(False)
        Me.ResumeLayout(False)
    End Sub

    Private Sub GenContentContainer()
        With objContentContainer
            .AutoSizeMode = osForms.AutoSizeMode.GrowAndShrink
            .ColumnCount = 2
            .ColumnStyles.Add(GenContainerCol(True))
            .ColumnStyles.Add(GenContainerCol())
            .Controls.Add(picIcon, 0, 0)
            .Controls.Add(lblMsg, 1, 0)
            .Controls.Add(objBtnPanel, 0, 1)
            .Dock = osForms.DockStyle.Fill
            .Location = New osDraw.Point(0, 0)
            .Margin = New osForms.Padding(4)
            .Name = "objContentContainer"
            .RowCount = 2
            .RowStyles.Add(GenContainerRow(True))
            .RowStyles.Add(GenContainerRow())
            .TabIndex = 0
        End With
    End Sub

    Private Sub PrepMsgIcon(msgType As MsgBoxType)
        With picIcon
            .Size = New osDraw.Size(IconSize, IconSize)
            .Margin = New osForms.Padding(2)
            .SizeMode = osForms.PictureBoxSizeMode.StretchImage
            .Anchor = CType((osForms.AnchorStyles.Left Or osForms.AnchorStyles.Right), osForms.AnchorStyles)
            .Image = SetMsgIcon(msgType).ToBitmap()
        End With
    End Sub

    Private Sub PrepPopupGui(msgTitle As String)
        Text = msgTitle
        FormBorderStyle = osForms.FormBorderStyle.FixedDialog
        StartPosition = osForms.FormStartPosition.CenterScreen
        ControlBox = False
        ShowInTaskbar = False
        TopMost = True
        AutoScaleMode = osForms.AutoScaleMode.Font
        AutoScaleDimensions = New osDraw.SizeF(6.0!, 13.0!)
        DoubleBuffered = True
        MinimizeBox = False
        MaximizeBox = False
        Size = New osDraw.Size(375, 164)
        Controls.Add(objContentContainer)
    End Sub

    Private Sub PrepBtnContainer(msgType As MsgBoxType)
        objContentContainer.SetColumnSpan(objBtnPanel, 2)

        AddButtons(msgType)
        With objBtnPanel
            .Dock = osForms.DockStyle.Fill
            .FlowDirection = osForms.FlowDirection.RightToLeft
            .MaximumSize = New osDraw.Size(0, 44)
            .MinimumSize = New osDraw.Size(0, 44)
            .Name = "btnContainer"
            .TabIndex = 2
        End With
    End Sub

    Private Sub PrepMsg(msgText As String)
        With lblMsg
            .AutoSize = True
            .Dock = osForms.DockStyle.Fill
            .Font = New osDraw.Font("Segoe UI", 9.75!, osDraw.FontStyle.Regular, osDraw.GraphicsUnit.Point, CType(0, Byte))
            .Margin = New osForms.Padding(6)
            .Name = "txtMsg"
            .TabIndex = 1
            .Text = msgText
        End With
    End Sub

    Private Sub AddButtons(msgType As MsgBoxType)
        Select Case msgType
            Case MsgBoxType.isAlert
                AddButton("No", osForms.DialogResult.No, False)
                AddButton("Yes", osForms.DialogResult.Yes, True)
            Case MsgBoxType.isQuestion
                AddButton("Cancel", osForms.DialogResult.Cancel, False)
                AddButton("No", osForms.DialogResult.No, False)
                AddButton("Yes", osForms.DialogResult.Yes, True)
        End Select
    End Sub

    Private Sub AddButton(btnText As String, btnResult As osForms.DialogResult, Optional isDefault As Boolean = False)
        Dim objBtn = New osForms.Button() With {
            .Text = btnText,
            .DialogResult = btnResult,
            .Size = New osDraw.Size(75, 38),
            .FlatStyle = osForms.FlatStyle.Popup,
            .UseVisualStyleBackColor = True
        }

        objBtnPanel.Controls.Add(objBtn)

        If isDefault Then AcceptButton = objBtn
        If btnResult = osForms.DialogResult.Cancel Then CancelButton = objBtn
    End Sub

    Private Function SetMsgIcon(mType As MsgBoxType) As osDraw.Icon
        Return If(mType = MsgBoxType.isAlert,
            osIcons.Warning, osIcons.Question)
    End Function

    Private Function GenContainerCol(Optional isIconCol As Boolean = False) As osForms.ColumnStyle
        If isIconCol Then
            Return New osForms.ColumnStyle(osForms.SizeType.Absolute, 52.0!)
        Else
            Return New osForms.ColumnStyle(osForms.SizeType.Percent, 100.0!)
        End If
    End Function

    Private Function GenContainerRow(Optional isBtnRow As Boolean = False) As osForms.RowStyle
        Return New osForms.RowStyle(osForms.SizeType.Absolute,
                                    If(isBtnRow, 75.0!, 44.0!))
    End Function

    Protected Overrides ReadOnly Property CreateParams() As osForms.CreateParams
        Get
            Dim cp As osForms.CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE Or WS_EX_TOOLWINDOW
            Return cp
        End Get
    End Property

    Protected Overrides Sub WndProc(ByRef m As osForms.Message)
        If m.Msg = WM_MOUSEACTIVATE Then
            m.Result = CType(MA_NOACTIVATE, IntPtr)
            Return
        End If
        MyBase.WndProc(m)
    End Sub

    Public Shared Function DisplayPopup(msg As String, title As String, mtype As MsgBoxType) As PromptResponse
        Using dlg As New ResponseBox(msg, title, mtype)
            Dim objPromptResp = dlg.ShowDialog()

            Select Case objPromptResp
                Case osForms.DialogResult.Yes
                    Return PromptResponse.isYes
                Case osForms.DialogResult.No
                    Return PromptResponse.isNo
                Case osForms.DialogResult.Cancel
                    Return PromptResponse.isCancel
            End Select
        End Using
    End Function

End Class

Public NotInheritable Class PromptResponseState
    Private Sub New()
    End Sub

    Private Shared _depth As Integer = 0

    Public Shared ReadOnly Property isPromptResponseOpen As Boolean
        Get
            Return Threading.Volatile.Read(_depth) > 0
        End Get
    End Property

    Public Shared Sub PreventSecondaryClose()
        Threading.Interlocked.Increment(_depth)
    End Sub

    Public Shared Sub EnterPromptResponse()
        Threading.Interlocked.Increment(_depth)
    End Sub

    Public Shared Sub ExitPromptResponse()
        Threading.Interlocked.Decrement(_depth)
    End Sub
End Class

Public Class PromptData

    Public Property Title As String
    Public Property Msg As String
    Public Property MsgType As MsgBoxType

    Public Sub New()

    End Sub

    Public Sub New(pType As PromptType)
        Select Case pType
            Case PromptType.Prefs_Save
                Msg = "Confirm Saving To Preferences?"
                Title = "Save Preferences"
                MsgType = MsgBoxType.isAlert
            Case PromptType.Prefs_Close
                Msg = "Settings have been changed... Save Changes?"
                Title = "Save Changes"
                MsgType = MsgBoxType.isQuestion
            Case PromptType.GameMenu_Leave
                Msg = "Are you sure you want to close MTG Arena?"
                Title = "Exit Game"
                MsgType = MsgBoxType.isQuestion
            Case PromptType.GameMenu_Restart
                Msg = "This Option Force Closes and Restarts MTGA... Continue?"
                Title = "Restart Game"
                MsgType = MsgBoxType.isQuestion
            Case PromptType.CloseApp
                Msg = "Are you sure you want to exit osAutoCast?"
                Title = "Close osAutoCast"
                MsgType = MsgBoxType.isAlert
            Case PromptType.DisableService
                Msg = "This will Disable osAutoCast... Continue?"
                Title = "Disable"
                MsgType = MsgBoxType.isQuestion
        End Select
    End Sub

End Class

Public Class ProgressValData
    Implements IDisposable

    Private disposedValue As Boolean

    Private ProgressDuration As TimeSpan
    Private ProgressEase As Func(Of Double, Double)

    Public Property ProgressComplete As Boolean

    Public Sub New()

    End Sub

    Public Sub New(pDur As TimeSpan, Optional pEasing As Func(Of Double, Double) = Nothing)
        ProgressDuration = pDur
        ProgressEase = pEasing

        ProgressComplete = False
    End Sub

    Public Sub CalcProgress(pElapsed As Stopwatch, ByRef objProgVal As Double)
        Dim progElapsed = pElapsed.Elapsed
        Dim progVal = Math.Min(1.0, Math.Round(progElapsed.TotalMilliseconds / ProgressDuration.TotalMilliseconds, 2))

        objProgVal = If(ProgressEase Is Nothing, progVal, ProgressEase(progVal))
        If progVal >= 1.0 Then ProgressComplete = True
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ProgressDuration = Nothing
                ProgressEase = Nothing
            End If

            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

End Class
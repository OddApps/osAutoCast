Imports System.Data
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Reflection
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Threading
Imports System.Windows.Media.Animation
Imports osDraw = System.Drawing
Imports osRect = SharpDX.Mathematics.Interop
Imports osText = SharpDX.DirectWrite
Imports osForms = System.Windows.Forms
Imports osIcons = System.Drawing.SystemIcons
Imports osTarget = SharpDX.Direct2D1
Imports osProgColor = SharpDX.Mathematics.Interop.RawColor4
Imports osProgBlendState = SharpDX.Direct3D11.BlendState
Imports osAutoCast.DataTypeLib.AnimationObject
Imports osAutoCast.DataTypeLib.AnimationType
Imports osAutoCast.DataTypeLib.AnimationVisual
Imports SharpDX
Imports SharpDX.Direct3D11
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports pxShader_Text = System.Windows.Media.Effects.PixelShader
Imports pxShader_Pixel = SharpDX.Direct3D11.PixelShader
Imports pxShader_Vertex = SharpDX.Direct3D11.VertexShader
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.osShaderDataLib

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
    End Enum

    Public Enum UpdateStatus
        ToEnabled
        ToDisabled
        CancelUpdate
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

    Public Enum ProgMode
        AutoPass
        AutoCast
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
        AutoCast
        AutoPass
        ShowOpts
        ShowMenu
        ShowMenuOverlay
        InputShift
        InputClick
        None
    End Enum

    Public Enum TriggerType
        AutoCast
        AutoPass
        ShowPrefs
        ShowMenu
        ShowMenuOverlay
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

    Public Enum LoadContentData
        isLoading
        isInit
        isPrefPrep
        isLoadingUI
        isStartingSvc
        isStarting
        isApplyConfig
    End Enum

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
        propGlowColor
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

    Public Enum osShaderType
        sTypePixel
        sTypeVertex
        sTypeText
    End Enum

#End Region

End Module

Namespace GameMenuOpts

    Public Enum GameMenuItem
        ShowStart
        ShowClose
    End Enum

End Namespace

Public Module ProgBarLib

    Public Const objShader_Vertex As String =
"struct VSOut {
    float4 pos : SV_Position;
};

VSOut VSMain(uint vid : SV_VertexID) {
	float2 p = (vid == 0) ? float2(-1,-1)
               : (vid == 1) ? float2(-1, 3)
                             : float2( 3,-1);

    VSOut o;
	
    o.pos = float4(p, 0.0, 1.0);
    return o;
}"


    Public Const objShader_Pixel As String =
"cbuffer Bar : register(b0) {
    float prevValue;
    float currValue;
    float invSize;
    float flags;
    float4 pcoloractive;
    float4 pcolorbg;
    float barOffset;
    float pad0;
    float pad1;
    float pad2;
};

struct PSIn {
    float4 pos:SV_Position;
};

float snap_to_pixel(float t, float invSize) {
    float px = 1.0 / invSize;
    float p  = round(t * px);
	
    return p * invSize;
}

float aa_cover_edge(float edge, float x, float invSize) {
	float d = x - edge;
	float w = max(0.5 * invSize, 0.5 * fwidth(x));
	
	return saturate(0.5 - d / (2.0 * w));
}

float aa_band_vel(float a, float b, float x, float invSize, float dv) {
    float lo = min(a, b);
    float hi = max(a, b);

	hi = max(hi, lo + invSize);

	const float FEATHER_PER_UNIT_DV_PX = 0.75;
    const float FEATHER_EXTRA_MAX_PX   = 0.5;

    float addHalfPx = min(dv * FEATHER_PER_UNIT_DV_PX, FEATHER_EXTRA_MAX_PX);
    float halfAA    = max(0.5 * invSize, 0.5 * fwidth(x)) + addHalfPx * invSize;

	float left  = smoothstep(lo - halfAA, lo + halfAA, x);
    float right = 1.0 - smoothstep(hi - halfAA, hi + halfAA, x);
	
    return saturate(left * right);
}

float4 PSMain(PSIn pin) : SV_Target {
	float u = (pin.pos.x - barOffset) * invSize;

	if (u < 0.0 || u > 1.0) discard;

	float a = snap_to_pixel(prevValue, invSize);
    float b = snap_to_pixel(currValue, invSize);

    bool fullCompose = (((uint)flags & 4u) != 0u);

    if (fullCompose) {
		float filled = aa_cover_edge(b, u, invSize);
		float4 col = lerp(pcolorbg, pcoloractive, filled);
		
        col.a = 1.0;
		
        return col;
    } else {
		if (a == b) discard;

        float dv = abs(b - a);
        float m  = aa_band_vel(a, b, u, invSize, dv);
		
        if (m <= 0.0) discard;

		float4 baseCol = (b >= a) ? pcoloractive : pcolorbg;

		return baseCol;
    }
}"

End Module

Public Class osShaderDataLib

    Public Interface iPxShader
        ReadOnly Property iShaderType As osShaderType
        Function sText() As pxShader_Text
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

        Public Function isShaderText() As pxShader_Text Implements iPxShader.sText
            Return Nothing
        End Function

        Public Function isShaderPixel() As pxShader_Pixel Implements iPxShader.sPixel
            Return pxShaderObj
        End Function

    End Class

    Public Class pxShaderText
        Implements iPxShader

        Public Property pxShaderEff As pxShader_Text

        Public Sub New(ShaderEff As pxShader_Text)
            Me.pxShaderEff = ShaderEff
        End Sub

        Public ReadOnly Property iShaderType As osShaderType Implements iPxShader.iShaderType
            Get
                Return sTypeText
            End Get
        End Property

        Public Function isShaderObject() As pxShader_Pixel Implements iPxShader.sPixel
            Return Nothing
        End Function

        Public Function isShaderVertex() As pxShader_Vertex Implements iPxShader.sVertex
            Return Nothing
        End Function

        Public Function isShaderText() As pxShader_Text Implements iPxShader.sText
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

        Public Function isShaderText() As pxShader_Text Implements iPxShader.sText
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
        _osPrefVQ_DT = New DataTable()

        PopulateVQ_Cols()
        PopulateDataVQ()
    End Sub

    Public Sub PopulateVQ_Cols()
        _osPrefVQ_DT.Columns.Add("vqIdx", GetType(Integer))
        _osPrefVQ_DT.Columns.Add("vqName", GetType(String))
    End Sub

    Private Sub PopulateDataVQ()
        _osPrefVQ_DT.Rows.Add(0, "Performance")
        _osPrefVQ_DT.Rows.Add(1, "Quality")
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
                ' TODO: dispose managed state (managed objects)
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
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
        ' Default constructor
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

    Public Sub New()
        txtComposed = GenFormattedText()

        With CoreDataLib.FetchProgSizeReport(TriggerType.AutoCast)
            txtLocation = New Point((.Item("pW") - txtComposed.Width) \ 2,
                                    (.Item("pH") - txtComposed.Height) \ 2)
        End With
    End Sub

    Public Sub New(txtMsg As String, pType As TriggerType, Optional isAP As Boolean = False)
        txtComposed = GenFormattedText(txtMsg, isAP)

        With CoreDataLib.FetchProgSizeReport(pType)
            txtLocation = New Point((.Item("pW") - txtComposed.Width) \ 2,
                                    (.Item("pH") - txtComposed.Height) \ 2)
        End With
    End Sub

    Private Function GenFormattedText() As FormattedText
        Return New FormattedText(osFuncLib_Progress.progDispMsg,
                                 Globalization.CultureInfo.CurrentCulture,
                                 FlowDirection.LeftToRight,
                                 New Typeface(New FontFamily("Segoe UI"),
                                              FontStyles.Normal,
                                              FontWeights.Bold,
                                              FontStretches.Normal), 14, Brushes.Black, 1.0)
    End Function

    Private Function GenFormattedText(txtMsg As String, Optional isAP As Boolean = False) As FormattedText
        Return New FormattedText(txtMsg, Globalization.CultureInfo.CurrentCulture,
                                 FlowDirection.LeftToRight, ComposeTypeFace(),
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
            Case < 30
                Return 15
            Case > 40
                Return 12
        End Select
    End Function

    Private Function ComposeTypeFace() As Typeface
        Return New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal,
                            FontWeights.Bold, FontStretches.Normal)
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

    Public Sub New(eX As Integer, eW As Integer, eB As System.Windows.Media.Brush)

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

    Public Property LoadProcess As Task
    Public Property LoadDuration As Integer

    Public Sub New(objLoadProcess As Task,
                   objLoadDuration As Integer)

        Me.LoadProcess = objLoadProcess
        Me.LoadDuration = objLoadDuration

    End Sub

End Class

Public Class osPopupAnimation

    Public Property aniScale As DoubleAnimation
    Public Property aniFade As DoubleAnimation

    Private aniDuration As Duration
    Private aniDuration_Fade As Duration

    Private aniDuration_AutoStart As TimeSpan = TimeSpan.FromMilliseconds(0)
    Private aniDuration_StartDelay As TimeSpan = TimeSpan.FromMilliseconds(200)
    Private aniDuration_QuickCloseDelay As TimeSpan = TimeSpan.FromMilliseconds(125)

    Private aniDuration_Open As TimeSpan = TimeSpan.FromMilliseconds(380)
    Private aniDuration_Close As TimeSpan = TimeSpan.FromMilliseconds(380)

    Private aniDuration_ExecFade As TimeSpan = TimeSpan.FromMilliseconds(380)
    Private aniDuration_OverlayFade As TimeSpan = TimeSpan.FromMilliseconds(300)

    Private aniDuration_QuickCloseOverlay As TimeSpan = TimeSpan.FromMilliseconds(1)
    Private aniDuration_QuickClosePopup As TimeSpan = TimeSpan.FromMilliseconds(420)
    Private aniDuration_QuickCloseFade As TimeSpan = TimeSpan.FromMilliseconds(275)

    Public Sub New()
    End Sub

    Public Sub New(aniType As AnimationType, aniObject As AnimationObject, Optional isQuickClose As Boolean = False)
        Select Case aniType
            Case aniOpen
                If aniObject = aniPopup Then
                    SetAniDuration(aniDuration, aniDuration_Open)
                    SetAniDuration(aniDuration_Fade, aniDuration_ExecFade)

                    SetAnimation(aniOpen, aniPopup, aniDuration_Fade, True, Me.aniFade)
                    SetAnimation(aniOpen, aniPopup, aniDuration, False, Me.aniScale)
                Else
                    SetAniDuration(aniDuration_Fade, aniDuration_OverlayFade)
                    SetAnimation(aniOpen, aniOverlay, aniDuration_Fade, True, Me.aniFade)
                End If
            Case aniClose
                If aniObject = aniPopup Then
                    SetAniDuration(aniDuration, CloseDuration_Scale(isQuickClose, aniObject))
                    SetAniDuration(aniDuration_Fade, CloseDuration_Fade(isQuickClose, aniObject))

                    SetAnimation(aniClose, aniPopup, aniDuration_Fade, True, Me.aniFade, isQuickClose)
                    SetAnimation(aniClose, aniPopup, aniDuration, False, Me.aniScale, isQuickClose)
                Else
                    SetAniDuration(aniDuration_Fade, CloseDuration_Fade(isQuickClose, aniObject))
                    SetAnimation(aniClose, aniOverlay, aniDuration_Fade, True, Me.aniFade)
                End If
        End Select
    End Sub

    Private Function CloseDuration_Fade(isQuickClose As Boolean, aniObject As AnimationObject)
        Select Case aniObject
            Case aniPopup
                Return If(isQuickClose, aniDuration_QuickCloseFade,
                    aniDuration_Close)
            Case aniOverlay
                Return If(isQuickClose, aniDuration_QuickCloseOverlay,
                    aniDuration_OverlayFade)
        End Select
    End Function

    Private Function CloseDuration_Scale(isQuickClose As Boolean, aniObject As AnimationObject)
        Select Case aniObject
            Case aniPopup
                Return If(isQuickClose, aniDuration_QuickClosePopup,
                    aniDuration_Close)
            Case aniOverlay
                Return If(isQuickClose, aniDuration_QuickCloseOverlay,
                    aniDuration_OverlayFade)
        End Select
    End Function

    Private Sub SetAnimation(aniType As AnimationType, aniObject As AnimationObject, aniDur As Duration,
                             isFadeAni As Boolean, ByRef aniObj As DoubleAnimation,
                             Optional isQuickClose As Boolean = False)

        Dim objAniVal = SetAniValues(aniType, aniObject, isFadeAni, isQuickClose)

        Dim objNewAni As New DoubleAnimation() With {
            .From = objAniVal.vStart, .To = objAniVal.vStop,
            .FillBehavior = FillBehavior.HoldEnd,
            .Duration = aniDur
        }

        SetAniOptions(aniType, aniObject, objAniVal.vOpen,
                    objNewAni, isFadeAni, isQuickClose)

        'If objAniVal.vOpen Then
        '    If aniObject = aniPopup Then
        '        objNewAni.BeginTime = aniDelay_OverlayOpen
        '    Else
        '        objNewAni.BeginTime = aniDuration_AutoStart
        '    End If
        'End If

        aniObj = objNewAni
    End Sub

    Private Sub SetAniOptions(aniType As AnimationType, aniObject As AnimationObject, isOpen As Boolean,
                            ByRef objAni As DoubleAnimation, isFadeAni As Boolean, Optional isQuickClose As Boolean = False)

        SetAniDelay(aniType, aniObject, isOpen,
                    objAni, isQuickClose)

        If isPopupFadeClose(aniType, aniObject, isFadeAni) Then
            If Not isQuickClose Then
                objAni.EasingFunction = ApplyEase()
            End If
        Else
            objAni.EasingFunction = If(isQuickClose,
                ApplyEase(True), ApplyEase())
        End If

    End Sub

    Private Function isPopupFadeClose(aniType As AnimationType, aniObject As AnimationObject, isFadeAni As Boolean) As Boolean
        If aniType = aniClose AndAlso aniObject = aniPopup Then
            Return isFadeAni
        End If
    End Function

    Private Sub SetAniDelay(aniType As AnimationType, aniObject As AnimationObject, isOpen As Boolean,
                            ByRef objAni As DoubleAnimation, Optional isQuickClose As Boolean = False)
        Select Case isOpen
            Case True
                If aniObject = aniPopup Then
                    objAni.BeginTime = aniDuration_StartDelay
                Else
                    objAni.BeginTime = aniDuration_AutoStart
                End If
            Case False
                If aniObject = aniPopup Then
                    objAni.BeginTime = If(isQuickClose,
                        aniDuration_QuickCloseDelay, aniDuration_AutoStart)
                End If
        End Select
    End Sub

    Private Function SetAniValues(aniType As AnimationType, aniObject As AnimationObject,
                                  Optional isFadeAni As Boolean = False,
                                  Optional isQuickClose As Boolean = False) As (vStart As Double, vStop As Double, vOpen As Boolean)
        Select Case aniType
            Case aniOpen
                Return (vStart:=CalcStartVal(isFadeAni, aniObject),
                    vStop:=CalcStopVal(isFadeAni, aniObject),
                    vOpen:=True)
            Case aniClose
                Return (vStart:=CalcStartVal(isFadeAni, aniObject, True),
                    vStop:=CalcStopVal(isFadeAni, aniObject, True, isQuickClose),
                    vOpen:=False)
        End Select
    End Function

    Private Function CalcStartVal(isFade As Boolean, aniObject As AnimationObject,
                                  Optional isClose As Boolean = False, Optional isQuickClose As Boolean = False) As Double
        Select Case aniObject
            Case aniPopup
                Return If(isClose, 1.0,
                    If(isFade, 0.0, 0.01))
            Case aniOverlay
                Return If(isClose, 0.7, 0.0)
        End Select
    End Function

    Private Function CalcStopVal(isFade As Boolean, aniObject As AnimationObject,
                                 Optional isClose As Boolean = False, Optional isQuickClose As Boolean = False) As Double
        Select Case aniObject
            Case aniPopup
                Return If(isClose,
                    If(isFade, 0.0, If(isQuickClose, 20.0, 0.01)), 1.0)
            Case aniOverlay
                Return If(isClose, 0.0, 0.7)
        End Select
    End Function

    Public Sub DisposeAni()
        aniScale = Nothing
        aniFade = Nothing

        aniDuration = Nothing
        aniDuration_Open = Nothing
        aniDuration_Close = Nothing
    End Sub

    Private Sub SetAniDuration(ByRef objDur As Duration, valDur As TimeSpan)
        objDur = New Duration(valDur)
    End Sub

    Private Function ApplyEase() As QuinticEase
        Return New QuinticEase With {
            .EasingMode = EasingMode.EaseIn
        }
    End Function

    Private Function ApplyEase(isQuickClose As Boolean) As QuadraticEase
        Return New QuadraticEase With {
            .EasingMode = EasingMode.EaseOut
        }
    End Function

    'Private Function ApplyEase(isOpen As Boolean) As EasingFunctionBase
    '    If isOpen Then
    '        Return New QuinticEase With {
    '            .EasingMode = EasingMode.EaseIn
    '        }
    '    Else
    '        Return New QuinticEase With {
    '            .EasingMode = EasingMode.EaseIn
    '        }
    '    End If
    'End Function

End Class

Public Module osPopupMenuLib

    Public DisposeUI_PopupMenu As Action(Of osPopupMenu_GUI) =
        Sub(objGui_PopupMenu As osPopupMenu_GUI)
            If objGui_PopupMenu IsNot Nothing Then
                With objGui_PopupMenu

                    Try
                        RemoveHandler .Closed, AddressOf osHandler_UI.PrepDispatch

                        If .IsLoaded Then
                            .IsHitTestVisible = False
                            .Opacity = 0
                            .DataContext = Nothing
                            .Close()
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

    Private Sub ExecPrepUI_PopupMenu(objGui_PopupMenu As osPopupMenu_GUI, objGui_PopupMenuOverlay As MenuOverlayWindow)
        Dim objWin_PopupMenu = objGui_PopupMenu

        With objWin_PopupMenu
            .Owner = objGui_PopupMenuOverlay
            .Owner.ShowInTaskbar = False

            .ShowInTaskbar = False
            .Topmost = True
            .ShowActivated = False

            .InitPopupMenu()
            .Show()
            .InitPopupOpen()
        End With
    End Sub

    Private Sub ExecPrepUI_PopupMenuOverlay(objGui_PopupMenuOverlay As MenuOverlayWindow)
        Dim objWin_PopupMenuOverlay = objGui_PopupMenuOverlay

        With objWin_PopupMenuOverlay
            AddHandler .objStacker.MouseUp,
                Sub(sender As Object, e As MouseButtonEventArgs)
                    If DetermineMouseClick(e) Then
                        osHandler_UI.pmFunc_TerminatePopupMenu(sender, e)
                    End If
                End Sub

            .InitPopupMenuOverlay()
            .InitTransitionVisuals(aniOpen)
        End With
    End Sub

    Private Sub ExecPrepUI_TrayMenuOverlay(objGui_PopupMenuOverlay As MenuOverlayWindow)
        Dim objWin_PopupMenuOverlay = objGui_PopupMenuOverlay

        With objWin_PopupMenuOverlay
            .PrepTrayMenuOverlay()
            .Show()
        End With
    End Sub

    Public Function GeneratePopupMenuGUI() As Func(Of osPopupMenu_GUI)
        Return Function()
                   Return Application.Current.Dispatcher.
                    Invoke(Function()
                               Dim objWin_PopupMenu = New osPopupMenu_GUI()
                               AddHandler objWin_PopupMenu.Closed, AddressOf osHandler_UI.PrepDispatch

                               Return objWin_PopupMenu
                           End Function)
               End Function
    End Function

    Public Function GeneratePopupMenuOverlayGUI(Optional isFromTray As Boolean = False) As Func(Of MenuOverlayWindow)
        Return Function()
                   Return Application.Current.Dispatcher.
                    Invoke(Function()
                               Return New MenuOverlayWindow(isFromTray)
                           End Function)
               End Function
    End Function

    Public DisplayUI_PopupMenu As Action(Of osPopupMenu_GUI, MenuOverlayWindow) = AddressOf ExecPrepUI_PopupMenu
    Public DisplayUI_PopupMenuOverlay As Action(Of MenuOverlayWindow) = AddressOf ExecPrepUI_PopupMenuOverlay
    Public DisplayUI_TrayOverlay As Action(Of MenuOverlayWindow) = AddressOf ExecPrepUI_TrayMenuOverlay

End Module

Public Class GUI_PrepData
    Implements IDisposable

    Private disposedValue As Boolean

    Public Property guiAction As Action(Of Window)
    Public Property guiDispatch As Dispatcher
    Public Property guiIsLoaded As Boolean

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

    Public Sub New()

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
            Case TriggerAction.AutoCast
                guiDispatch = objWin.Dispatcher
            Case TriggerAction.AutoPass
                guiAction = AddressOf guiAction_AutoPass
                guiDispatch = objWin.Dispatcher
            Case TriggerAction.ShowMenu
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

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
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
            End Select
        End Using
    End Function

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
            Case PromptType.CloseApp
                Msg = "Are you sure you want to exit osAutoCast?"
                Title = "Close osAutoCast"
                MsgType = MsgBoxType.isAlert
            Case PromptType.DisableService
                Msg = "This will Disable osAutoCast... Continue?"
                Title = "Disable"
                MsgType = MsgBoxType.isAlert
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

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class
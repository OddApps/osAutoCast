
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Windows.Forms

Public Class ProgressModel
    Inherits PictureBox

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint And ControlStyles.OptimizedDoubleBuffer And ControlStyles.UserPaint, True)
        Me.DoubleBuffered = True

        Me.UpdateStyles()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        'Using offscreenBmp As New Bitmap(Me.Width, Me.Height)
        '    Using progGraphics As Graphics = Graphics.FromImage(offscreenBmp)
        '        progGraphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        '        progGraphics.FillRectangle(ProgBrush_BG, ProgContainer)

        '        Dim progressRect As New RectangleF(0, 0, DetermineProgress(), Me.Height)

        '        If progressRect.Width > 0 Then
        '            progGraphics.FillRectangle(ProgBrush_Active, progressRect)
        '        End If

        '        If progShowMsg Then
        '            With New ProgTextPos(progGraphics, pObj:=objAutoPassProg)
        '                progGraphics.DrawString(progDispMsg, progFont_AP, Brushes.White, .txtX, .txtY)
        '            End With
        '        End If
        '    End Using

        '    e.Graphics.DrawImageUnscaled(offscreenBmp, 0, 0)
        'End Using

        ''  ApplyProgBorder(progGraphics)
        'MyBase.OnPaint(e)
    End Sub

    Private Sub ApplyProgBorder(pGraphics As Graphics)
        Using borderPath As New GraphicsPath()
            With GenerateBorderLayout(20)
                borderPath.AddLine(.BorderOutline_1.OutlinePoint_1, .BorderOutline_1.OutlinePoint_2)
                borderPath.AddArc(.OutlineShape_1.ShapeRect, .OutlineShape_1.StartAngle, .OutlineShape_1.ArcAngle)

                borderPath.AddLine(.BorderOutline_2.OutlinePoint_1, .BorderOutline_2.OutlinePoint_2)
                borderPath.AddArc(.OutlineShape_2.ShapeRect, .OutlineShape_2.StartAngle, .OutlineShape_2.ArcAngle)

                borderPath.AddLine(.BorderOutline_3.OutlinePoint_1, .BorderOutline_3.OutlinePoint_2)

                Using ProgBorderStylus As New Pen(Color.Black, 4)
                    pGraphics.SmoothingMode = SmoothingMode.AntiAlias
                    pGraphics.DrawPath(ProgBorderStylus, borderPath)
                End Using
            End With
        End Using
    End Sub

    Private Function DetermineProgress() As Single
        Dim valProgSize = GetProgSize(TriggerType.AutoPass)

        Return If(ShowProgFull(), valProgSize,
            valProgSize - (progValue * valProgSize))
    End Function

    Private Function GenerateBorderLayout(bRadius As Integer) As ProgressBorderLayout
        With Me
            Return New ProgressBorderLayout(New BorderOutlineData(.Width - 1, 0, .Width - 1, .Height - bRadius - 1),
                                            New BorderShapeData(.Width - bRadius - 1, .Height - bRadius - 1, bRadius, bRadius, 0, 90),
                                            New BorderOutlineData(.Width - bRadius - 1, .Height - 1, bRadius, .Height - 1),
                                            New BorderShapeData(0, .Height - bRadius - 1, bRadius, bRadius, 90, 90),
                                            New BorderOutlineData(0, .Height - bRadius - 1, 0, 0))
        End With
    End Function

End Class

Public Class CustomProgressBar
    Inherits ProgressBar

    Public Sub New()
        ' Enables double buffering to reduce flicker
        Me.SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        ' Fill background
        g.Clear(Color.FromArgb(22, 22, 22)) ' << Set your background color here

        ' Fill progress
        Dim rect As New Rectangle(0, 0, CInt((Me.Value / Me.Maximum) * Me.Width), Me.Height)
        Using b As New SolidBrush(Color.DodgerBlue) ' << Set progress bar color
            g.FillRectangle(b, rect)
        End Using

        ' Optional draw border
        Using pen As New Pen(Color.Black, 1)
            g.DrawRectangle(pen, 0, 0, Me.Width - 1, Me.Height - 1)
        End Using
    End Sub
End Class


Public Class SmoothProgressBarr
    Inherits Control

    Private currentValue As Double = 0
    Private targetValue As Double = 0
    Private startValue As Double = 0
    Private animationDuration As Integer
    Private elapsedTime As Integer = 0
    Private animationRunning As Boolean = False
    Private WithEvents animTimer As New Timer()

    Public Sub New(Optional durationMs As Integer = 1000)
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        animationDuration = durationMs
        animTimer.Interval = 15 ' ~66 FPS
        Me.BackColor = Color.FromArgb(22, 22, 22)
        Me.ForeColor = Color.LimeGreen
    End Sub

    Public Sub SetProgress(value As Integer, Optional durationMs As Integer = 1000)
        value = Math.Max(0, Math.Min(100, value))

        If value = targetValue Then Exit Sub

        startValue = currentValue
        targetValue = value

        elapsedTime = 0
        animationRunning = True
        animTimer.Start()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        ' Background
        Using bgBrush As New SolidBrush(Me.BackColor)
            e.Graphics.FillRectangle(bgBrush, Me.ClientRectangle)
        End Using

        ' Foreground (Progress)
        Dim fillWidth As Single = CSng(Me.Width * (currentValue / 100.0))
        If fillWidth > 0 Then
            Dim barRect As New RectangleF(0, 0, fillWidth, Me.Height)
            Using fgBrush As New SolidBrush(Me.ForeColor)
                e.Graphics.FillRectangle(fgBrush, barRect)
            End Using
        End If

        ' Optional: Text
        'Dim text As String = $"{CInt(currentValue)}%"
        'Dim size As SizeF = e.Graphics.MeasureString(text, Me.Font)
        'Dim pos As New PointF((Me.Width - size.Width) / 2, (Me.Height - size.Height) / 2)
        'e.Graphics.DrawString(text, Me.Font, Brushes.White, pos)
    End Sub

    Private Sub animTimer_Tick(sender As Object, e As EventArgs) Handles animTimer.Tick
        If Not animationRunning Then Exit Sub

        elapsedTime += animTimer.Interval

        Dim progressRatio As Double = Math.Min(1.0, elapsedTime / animationDuration)
        currentValue = Lerp(startValue, targetValue, progressRatio)

        Me.Invalidate()

        If progressRatio >= 1.0 Then
            animationRunning = False
            animTimer.Stop()
        End If
    End Sub

    Private Function Lerp(startVal As Double, endVal As Double, t As Double) As Double
        Return startVal + (endVal - startVal) * t
    End Function

    Public Sub StartProgress()
        animTimer.Start()
    End Sub

    Public Sub StopProgress()
        animTimer.Stop()
    End Sub

End Class



Public Class ProgressBorderLayout
    Public Property BorderOutline_1 As BorderOutlineData
    Public Property OutlineShape_1 As BorderShapeData

    Public Property BorderOutline_2 As BorderOutlineData
    Public Property OutlineShape_2 As BorderShapeData

    Public Property BorderOutline_3 As BorderOutlineData

    Public Sub New()

    End Sub

    Public Sub New(bOutline1 As BorderOutlineData, outShape1 As BorderShapeData,
                   bOutline2 As BorderOutlineData, outShape2 As BorderShapeData,
                   bOutline3 As BorderOutlineData)

        Me.BorderOutline_1 = bOutline1
        Me.OutlineShape_1 = outShape1

        Me.BorderOutline_2 = bOutline2
        Me.OutlineShape_2 = outShape2

        Me.BorderOutline_3 = bOutline3

    End Sub
End Class

Public Class BorderOutlineData
    Public Property OutlinePoint_1 As System.Drawing.Point
    Public Property OutlinePoint_2 As System.Drawing.Point

    Public Sub New()

    End Sub

    Public Sub New(x1 As Integer, y1 As Integer,
                   x2 As Integer, y2 As Integer)

        Me.OutlinePoint_1 = New Point(x1, y1)
        Me.OutlinePoint_2 = New Point(x2, y2)
    End Sub
End Class

Public Class BorderShapeData
    Public Property ShapeRect As System.Drawing.Rectangle
    Public Property StartAngle As Integer
    Public Property ArcAngle As Integer

    Public Sub New()

    End Sub

    Public Sub New(rectX As Integer, rectY As Integer,
                   rectW As Integer, rectH As Integer,
                   strtAngle As Integer, sweepAngle As Integer)

        Me.ShapeRect = New Rectangle(rectX, rectY, rectW, rectH)

        Me.StartAngle = strtAngle
        Me.ArcAngle = sweepAngle

    End Sub
End Class

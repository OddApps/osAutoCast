Imports System.Windows.Markup
Imports System.Globalization
Imports System.Windows.Media.Effects
Imports osAutoCast.DataTypeLib.MenuProperty
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.DataTypeLib.VisualEasing
Imports osAutoCast.DataTypeLib.PrefUI_State
Imports System.Windows.Media.Animation
Imports System.Windows.Controls.Primitives
Imports System
Imports System.Windows
Imports System.Windows.Data
Imports System.Windows.Media
Imports System.ComponentModel
Imports System.Collections.Generic

Namespace osStyle

    Public Class osStyles
        Inherits DependencyObject

        Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("CornerRadius", GetType(CornerRadius), GetType(osStyles),
                             New PropertyMetadata(New CornerRadius(0)))

        Public Shared ReadOnly BorderThicknessProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("BorderThickness", GetType(Thickness), GetType(osStyles),
                             New PropertyMetadata(New Thickness(1)))

        Public Shared ReadOnly BorderBrushProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("BorderBrush", GetType(Brush), GetType(osStyles),
                             New PropertyMetadata(Brushes.Transparent))

        Public Shared ReadOnly BackgroundProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("Background", GetType(Brush), GetType(osStyles),
                             New PropertyMetadata(Brushes.Transparent))

        Public Shared ReadOnly DefaultBackgroundProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("DefaultBackground", GetType(Brush), GetType(osStyles),
                             New FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender))

        Public Shared ReadOnly TemplateProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("Template", GetType(ControlTemplate), GetType(osStyles),
                             New PropertyMetadata(Nothing, AddressOf UpdateView))

        Public Shared Sub SetCornerRadius(ByVal objBorder As UIElement, ByVal valRadius As CornerRadius)
            objBorder.SetValue(CornerRadiusProperty, valRadius)
        End Sub

        Public Shared Function GetCornerRadius(ByVal objBorder As UIElement) As CornerRadius
            Return CType(objBorder.GetValue(CornerRadiusProperty), CornerRadius)
        End Function

        Public Shared Sub SetBorderThickness(ByVal objBorder As UIElement, ByVal valThickness As Thickness)
            objBorder.SetValue(BorderThicknessProperty, valThickness)
        End Sub

        Public Shared Function GetBorderThickness(ByVal objBorder As UIElement) As Thickness
            Return CType(objBorder.GetValue(BorderThicknessProperty), Thickness)
        End Function

        Public Shared Sub SetBorderBrush(ByVal objBorder As UIElement, ByVal valBorderColor As Brush)
            objBorder.SetValue(BorderBrushProperty, valBorderColor)
        End Sub

        Public Shared Function GetBorderBrush(ByVal objBorder As UIElement) As Brush
            Return CType(objBorder.GetValue(BorderBrushProperty), Brush)
        End Function

        Public Shared Sub SetBackground(ByVal objButton As UIElement, ByVal valBackground As Brush)
            objButton.SetValue(BackgroundProperty, valBackground)
        End Sub

        Public Shared Function GetBackground(ByVal objButton As UIElement) As Brush
            Return CType(objButton.GetValue(BackgroundProperty), Brush)
        End Function

        Public Shared Sub SetDefaultBackground(ByVal objButton As UIElement, ByVal valDefaultBackground As Brush)
            objButton.SetValue(DefaultBackgroundProperty, valDefaultBackground)
        End Sub

        Public Shared Function GetDefaultBackground(ByVal objButton As UIElement) As Brush
            Return CType(objButton.GetValue(DefaultBackgroundProperty), Brush)
        End Function

        Public Shared Sub SetTemplate(ByVal objTemplate As DependencyObject, ByVal valTemplate As ControlTemplate)
            objTemplate.SetValue(TemplateProperty, valTemplate)
        End Sub

        Public Shared Function GetTemplate(ByVal objTemplate As DependencyObject) As ControlTemplate
            Return CType(objTemplate.GetValue(TemplateProperty), ControlTemplate)
        End Function

        Private Shared Sub UpdateView(depObj As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim objTemplate = FindTemplate(depObj)
            If objTemplate Is Nothing Then Return

            Dim objView = InitView(e.NewValue)
            objTemplate.Template = objView
        End Sub

        Private Shared Function FindTemplate(depObj As DependencyObject) As Control
            Return TryCast(depObj, Control)
        End Function

        Private Shared Function InitView(objV As Object) As ControlTemplate
            Return TryCast(objV, ControlTemplate)
        End Function

    End Class

    Public Class osContainerLayout

        Private Const SeparatorTag As String = "Content_Separator"

        Public Shared ReadOnly RowsProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("Rows", GetType(String), GetType(osContainerLayout),
                             New PropertyMetadata("", AddressOf UpdateContainerRows))

        Public Shared ReadOnly SeparatorBrushProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("SeparatorBrush", GetType(Brush), GetType(osContainerLayout),
                             New PropertyMetadata(Brushes.LightGray, AddressOf UpdateContentSeperator))

        Public Shared Sub SetSeparatorBrush(objSeparator As DependencyObject, value As Brush)
            objSeparator.SetValue(SeparatorBrushProperty, value)
        End Sub

        Public Shared Function GetSeparatorBrush(objSeparator As DependencyObject) As Brush
            Return CType(objSeparator.GetValue(SeparatorBrushProperty), Brush)
        End Function

        Public Shared ReadOnly SeparatorThicknessProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("SeparatorThickness", GetType(Double), GetType(osContainerLayout),
                             New PropertyMetadata(1.0, AddressOf UpdateContentSeperator))

        Public Shared Sub SetSeparatorThickness(objSeparator As DependencyObject, value As Double)
            objSeparator.SetValue(SeparatorThicknessProperty, value)
        End Sub

        Public Shared Function GetSeparatorThickness(objSeparator As DependencyObject) As Double
            Return CType(objSeparator.GetValue(SeparatorThicknessProperty), Double)
        End Function

        Private Shared Sub UpdateContentSeperator(objCont As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim objContainer = FindContainer(objCont)
            If objContainer Is Nothing Then Return

            If objContainer.IsLoaded Then
                GenerateContainer(objContainer)
            Else
                RemoveHandler objContainer.Loaded, AddressOf Grid_Loaded
                AddHandler objContainer.Loaded, AddressOf Grid_Loaded
            End If
        End Sub

        Private Shared Sub UpdateContainerRows(objCont As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim objContainer = FindContainer(objCont)
            If objContainer Is Nothing Then Return

            If objContainer.IsLoaded Then
                GenerateContainer(objContainer)
            Else
                RemoveHandler objContainer.Loaded, AddressOf Grid_Loaded
                AddHandler objContainer.Loaded, AddressOf Grid_Loaded
            End If
        End Sub

        Private Shared Sub Grid_Loaded(sender As Object, e As RoutedEventArgs)
            Dim objContainer = FindContainer(sender)
            If objContainer Is Nothing Then Return

            RemoveHandler objContainer.Loaded, AddressOf Grid_Loaded
            GenerateContainer(objContainer)
        End Sub

        Private Shared Function FindContainer(depObj As DependencyObject) As Grid
            Return TryCast(depObj, Grid)
        End Function

        Private Shared Function FindContainer(sObj As Object) As Grid
            Return TryCast(sObj, Grid)
        End Function

        Public Shared Sub SetRows(objRows As DependencyObject, value As String)
            objRows.SetValue(RowsProperty, value)
        End Sub

        Public Shared Function GetRows(objRows As DependencyObject) As String
            Return CType(objRows.GetValue(RowsProperty), String)
        End Function

        Private Shared Sub GenerateContainer(grid As Grid)
            Dim rowsSpec = GetRows(grid)
            If rowsSpec Is Nothing Then rowsSpec = ""

            Dim parts = rowsSpec.Split(New Char() {","c},
                                       StringSplitOptions.RemoveEmptyEntries)

            Dim contentCount = parts.Length
            If contentCount = 0 Then
                Return
            End If

            For i As Integer = grid.Children.Count - 1 To 0 Step -1
                Dim fe = TryCast(grid.Children(i), FrameworkElement)

                If fe IsNot Nothing AndAlso fe.Tag IsNot Nothing AndAlso fe.Tag.ToString() = SeparatorTag Then
                    grid.Children.RemoveAt(i)
                End If
            Next

            Dim childMeta As New List(Of Tuple(Of UIElement, Integer, Integer))()

            For Each chObj As UIElement In grid.Children
                Dim fe = TryCast(chObj, FrameworkElement)

                If fe Is Nothing Then Continue For
                If fe.Tag IsNot Nothing AndAlso fe.Tag.ToString() = SeparatorTag Then Continue For

                Dim origRow As Integer = Grid.GetRow(chObj)
                Dim origRowSpan As Integer = Grid.GetRowSpan(chObj)

                If origRowSpan < 1 Then origRowSpan = 1

                If origRow < 0 Then origRow = 0
                If origRow >= contentCount Then origRow = contentCount - 1

                If origRow + origRowSpan > contentCount Then
                    origRowSpan = Math.Max(1, contentCount - origRow)
                End If

                childMeta.Add(Tuple.Create(chObj, origRow, origRowSpan))
            Next

            childMeta = childMeta.OrderBy(Function(t) t.Item2).ThenBy(Function(t) t.Item1.GetHashCode()).ToList()

            grid.RowDefinitions.Clear()

            For i As Integer = 0 To contentCount - 1
                Dim token = parts(i).Trim()
                Dim defContent As New RowDefinition()

                If String.Equals(token, "auto", StringComparison.OrdinalIgnoreCase) Then
                    defContent.Height = GridLength.Auto
                ElseIf token.EndsWith("*"c) Then
                    Dim starPart = token.TrimEnd("*"c)
                    Dim value As Double = 1.0

                    If Not String.IsNullOrEmpty(starPart) Then
                        Double.TryParse(starPart, value)
                    End If

                    defContent.Height = New GridLength(value, GridUnitType.Star)
                Else
                    Dim px As Double = 0

                    If Double.TryParse(token, px) Then
                        defContent.Height = New GridLength(px, GridUnitType.Pixel)
                    Else
                        defContent.Height = GridLength.Auto
                    End If
                End If

                grid.RowDefinitions.Add(defContent)

                If i < contentCount - 1 Then
                    Dim sepThickness = GetSeparatorThickness(grid)
                    Dim defSep As New RowDefinition()

                    If sepThickness <= 0 Then
                        defSep.Height = New GridLength(0, GridUnitType.Pixel)
                    Else
                        defSep.Height = New GridLength(sepThickness, GridUnitType.Pixel)
                    End If

                    grid.RowDefinitions.Add(defSep)
                End If
            Next

            Dim totalRows = grid.RowDefinitions.Count

            For Each tup In childMeta
                Dim ch = tup.Item1
                Dim origRow = tup.Item2
                Dim origSpan = tup.Item3

                Dim newRow = Math.Max(0, origRow * 2)
                Dim newSpan = Math.Max(1, origSpan * 2 - 1)

                If newRow > totalRows - 1 Then
                    newRow = Math.Max(0, totalRows - 1)
                End If

                If newRow + newSpan > totalRows Then
                    newSpan = Math.Max(1, totalRows - newRow)
                End If

                Grid.SetRow(ch, newRow)
                Grid.SetRowSpan(ch, newSpan)
            Next

            Dim sepBrush = GetSeparatorBrush(grid)
            Dim sepThicknessFinal = GetSeparatorThickness(grid)

            If sepThicknessFinal > 0 AndAlso contentCount > 1 Then
                Dim colSpan As Integer = If(grid.ColumnDefinitions.Count > 0, grid.ColumnDefinitions.Count, 1)

                For i As Integer = 0 To contentCount - 2
                    Dim sepRowIndex = i * 2 + 1

                    Dim rect As New Rectangle() With {
                        .HorizontalAlignment = HorizontalAlignment.Stretch,
                        .VerticalAlignment = VerticalAlignment.Stretch,
                        .Fill = If(sepBrush, Brushes.LightGray),
                        .IsHitTestVisible = False,
                        .Tag = SeparatorTag,
                        .SnapsToDevicePixels = True
                    }

                    Grid.SetRow(rect, sepRowIndex)
                    Grid.SetColumn(rect, 0)
                    Grid.SetColumnSpan(rect, colSpan)
                    Grid.SetZIndex(rect, 10000)

                    grid.Children.Add(rect)
                Next
            End If

            Dim wnd = Window.GetWindow(grid)

            If wnd IsNot Nothing Then
                If wnd.SizeToContent = SizeToContent.Manual Then
                    wnd.UpdateLayout()
                    wnd.Height = Double.NaN
                    wnd.UpdateLayout()
                Else
                    wnd.UpdateLayout()
                End If
            Else
                grid.UpdateLayout()
            End If
        End Sub

    End Class

    Public Class osPopupScale

        Public Shared ReadOnly PopupScaleProperty As DependencyProperty = DependencyProperty.
            RegisterAttached("PopupScale", GetType(Double), GetType(osPopupScale),
                             New PropertyMetadata(0.00, AddressOf OnPopupScaleChanged))

        Public Shared Sub SetPopupScale(objPopupScale As DependencyObject, valScale As Double)
            objPopupScale.SetValue(PopupScaleProperty, valScale)
        End Sub

        Public Shared Function GetPopupScale(objPopupScale As DependencyObject) As Double
            Return CDbl(objPopupScale.GetValue(PopupScaleProperty))
        End Function

        Private Shared Sub OnPopupScaleChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim objContainerUI = TryCast(d, UIElement)
            If objContainerUI Is Nothing Then Return

            Dim valScale As Double = CDbl(e.NewValue)

            With EnsureScaleTransform(objContainerUI)
                .ScaleX = valScale
                .ScaleY = valScale
            End With
        End Sub

        Private Shared Function EnsureScaleTransform(objContainer As UIElement) As ScaleTransform
            Dim objVisualTransform = objContainer.RenderTransform

            If objVisualTransform Is Nothing Then
                Dim objVisualScale = New ScaleTransform(0.00, 0.00)

                objContainer.RenderTransform = objVisualScale
                Return objVisualScale
            End If

            Dim objVerifyVisual = TryCast(objVisualTransform, ScaleTransform)
            If objVerifyVisual IsNot Nothing Then Return objVerifyVisual

            Dim chkTransformGroup = TryCast(objVisualTransform, TransformGroup)

            If chkTransformGroup IsNot Nothing Then
                Dim chkVisualGroup = chkTransformGroup.Children.
                    OfType(Of ScaleTransform)().FirstOrDefault()

                If chkVisualGroup IsNot Nothing Then Return chkVisualGroup

                chkVisualGroup = New ScaleTransform(0.00, 0.00)
                chkTransformGroup.Children.Insert(0, chkVisualGroup)

                Return chkVisualGroup
            End If

            Dim objVisualGroup As New TransformGroup()
            Dim objVisualScaler As New ScaleTransform(0.00, 0.00)

            objVisualGroup.Children.Add(objVisualScaler)
            objVisualGroup.Children.Add(objVisualTransform)

            objContainer.RenderTransform = objVisualGroup

            Return objVisualScaler
        End Function

    End Class

    Public Class osVisualSwitch
        ' Attached property to assign the Storyboard to watch
        Public Shared ReadOnly StoryboardProperty As DependencyProperty =
            DependencyProperty.RegisterAttached("Storyboard", GetType(Storyboard), GetType(osVisualSwitch),
                                                New PropertyMetadata(Nothing, AddressOf OnStoryboardChanged))

        Public Shared Sub SetStoryboard(o As DependencyObject, sb As Storyboard)
            o.SetValue(StoryboardProperty, sb)
        End Sub
        Public Shared Function GetStoryboard(o As DependencyObject) As Storyboard
            Return CType(o.GetValue(StoryboardProperty), Storyboard)
        End Function

        ' Optional RenderAtScale to control bitmap cache scale while animating
        Public Shared ReadOnly RenderAtScaleProperty As DependencyProperty =
            DependencyProperty.RegisterAttached("RenderAtScale", GetType(Double), GetType(osVisualSwitch),
                                                New PropertyMetadata(1.0))

        Public Shared Sub SetRenderAtScale(o As DependencyObject, value As Double)
            o.SetValue(RenderAtScaleProperty, value)
        End Sub

        Public Shared Function GetRenderAtScale(o As DependencyObject) As Double
            Return CDbl(o.GetValue(RenderAtScaleProperty))
        End Function

        ' Internal map to keep handlers and target reference so we can RemoveHandler later
        Private Class HandlerInfo
            Public Property TargetRef As WeakReference(Of UIElement)
            Public Property CurrentStateHandler As EventHandler
            Public Property CompletedHandler As EventHandler
            Public Property RenderAtScale As Double
        End Class

        Private Shared ReadOnly s_handlers As New Dictionary(Of Storyboard, HandlerInfo)()

        ' Attached property to store saved quality state per element
        Private Shared ReadOnly SavedStateProperty As DependencyProperty =
            DependencyProperty.RegisterAttached("SavedQualityState", GetType(QualityState), GetType(osVisualSwitch), New PropertyMetadata(Nothing))

        Private Shared Sub SetSavedState(e As DependencyObject, st As QualityState)
            e.SetValue(SavedStateProperty, st)
        End Sub
        Private Shared Function GetSavedState(e As DependencyObject) As QualityState
            Return CType(e.GetValue(SavedStateProperty), QualityState)
        End Function

        ' Called when Storyboard attached/changed
        Private Shared Sub OnStoryboardChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim target = TryCast(d, UIElement)
            Dim oldSb = TryCast(e.OldValue, Storyboard)
            Dim newSb = TryCast(e.NewValue, Storyboard)

            If oldSb IsNot Nothing Then
                ' remove handlers
                SyncLock s_handlers
                    Dim info As HandlerInfo = Nothing
                    If s_handlers.TryGetValue(oldSb, info) Then
                        If info.CurrentStateHandler IsNot Nothing Then RemoveHandler oldSb.CurrentStateInvalidated, info.CurrentStateHandler
                        If info.CompletedHandler IsNot Nothing Then RemoveHandler oldSb.Completed, info.CompletedHandler
                        s_handlers.Remove(oldSb)
                    End If
                End SyncLock
            End If

            If newSb IsNot Nothing AndAlso target IsNot Nothing Then
                Dim renderScale = GetRenderAtScale(d)
                ' create handlers that capture the target and storyboard
                Dim stateHandler As EventHandler = Nothing
                Dim completedHandler As EventHandler = Nothing

                stateHandler = Sub(s, args)
                                   Try
                                       ' Works best when storyboard was begun controllable (Begin(target, True))
                                       Dim clockState = newSb.GetCurrentState(target)
                                       If clockState = ClockState.Active Then
                                           EnableAnimationQualityMode(target, renderScale)
                                       Else
                                           RestoreQualityMode(target)
                                       End If
                                   Catch ex As Exception
                                       ' ignore (GetCurrentState may throw if not controllable)
                                   End Try
                               End Sub

                completedHandler = Sub(s, args)
                                       RestoreQualityMode(target)
                                   End Sub

                AddHandler newSb.CurrentStateInvalidated, stateHandler
                AddHandler newSb.Completed, completedHandler

                Dim hi As New HandlerInfo With {
                    .TargetRef = New WeakReference(Of UIElement)(target),
                    .CurrentStateHandler = stateHandler,
                    .CompletedHandler = completedHandler,
                    .RenderAtScale = renderScale
                }

                SyncLock s_handlers
                    s_handlers(newSb) = hi
                End SyncLock
            End If
        End Sub

        ' ---- Quality toggling (preserves previous values) ----

        Private Class QualityState
            Public Property BitmapScaling As BitmapScalingMode?
            Public Property EdgeMode As EdgeMode?
            Public Property TextRendering As TextRenderingMode?
            Public Property TextFormatting As TextFormattingMode?
            Public Property CacheMode As CacheMode
            Public Property UseLayoutRounding As Boolean?
            Public Property SnapsToDevicePixels As Boolean?
        End Class

        Private Shared Sub EnableAnimationQualityMode(target As UIElement, Optional renderAtScale As Double = 1.0)
            If target Is Nothing Then Return

            ' If already saved, do nothing
            If GetSavedState(target) IsNot Nothing Then Return

            Dim prev As New QualityState
            prev.BitmapScaling = RenderOptions.GetBitmapScalingMode(target)
            prev.EdgeMode = RenderOptions.GetEdgeMode(target)
            prev.TextRendering = TextOptions.GetTextRenderingMode(target)
            prev.TextFormatting = TextOptions.GetTextFormattingMode(target)
            prev.CacheMode = target.CacheMode
            Dim fe = TryCast(target, FrameworkElement)
            If fe IsNot Nothing Then
                prev.UseLayoutRounding = fe.UseLayoutRounding
                prev.SnapsToDevicePixels = fe.SnapsToDevicePixels
            End If

            SetSavedState(target, prev)

            ' Apply lower-quality / fast settings
            RenderOptions.SetBitmapScalingMode(target, BitmapScalingMode.LowQuality)
            RenderOptions.SetEdgeMode(target, EdgeMode.Aliased)
            TextOptions.SetTextRenderingMode(target, TextRenderingMode.Aliased)
            TextOptions.SetTextFormattingMode(target, TextFormattingMode.Display)

            Try
                target.CacheMode = New BitmapCache(renderAtScale)
            Catch ex As Exception
                ' ignore if setting CacheMode fails
            End Try

            If fe IsNot Nothing Then
                fe.UseLayoutRounding = True
                fe.SnapsToDevicePixels = True
            End If
        End Sub

        Private Shared Sub RestoreQualityMode(target As UIElement)
            If target Is Nothing Then Return

            Dim saved = GetSavedState(target)
            If saved Is Nothing Then
                ' Nothing saved — as a fallback restore to reasonable defaults
                RenderOptions.SetBitmapScalingMode(target, BitmapScalingMode.HighQuality)
                RenderOptions.SetEdgeMode(target, EdgeMode.Unspecified)
                TextOptions.SetTextRenderingMode(target, TextRenderingMode.Auto)
                TextOptions.SetTextFormattingMode(target, TextFormattingMode.Ideal)
                target.CacheMode = Nothing

                Dim fe_NoSave = TryCast(target, FrameworkElement)

                If fe_NoSave IsNot Nothing Then
                    fe_NoSave.UseLayoutRounding = False
                    fe_NoSave.SnapsToDevicePixels = False
                End If
                Return
            End If

            If saved.BitmapScaling.HasValue Then RenderOptions.SetBitmapScalingMode(target, saved.BitmapScaling.Value) Else RenderOptions.SetBitmapScalingMode(target, BitmapScalingMode.HighQuality)
            If saved.EdgeMode.HasValue Then RenderOptions.SetEdgeMode(target, saved.EdgeMode.Value) Else RenderOptions.SetEdgeMode(target, EdgeMode.Unspecified)
            If saved.TextRendering.HasValue Then TextOptions.SetTextRenderingMode(target, saved.TextRendering.Value) Else TextOptions.SetTextRenderingMode(target, TextRenderingMode.Auto)
            If saved.TextFormatting.HasValue Then TextOptions.SetTextFormattingMode(target, saved.TextFormatting.Value) Else TextOptions.SetTextFormattingMode(target, TextFormattingMode.Ideal)

            target.CacheMode = saved.CacheMode

            Dim fe = TryCast(target, FrameworkElement)
            If fe IsNot Nothing Then
                If saved.UseLayoutRounding.HasValue Then fe.UseLayoutRounding = saved.UseLayoutRounding.Value
                If saved.SnapsToDevicePixels.HasValue Then fe.SnapsToDevicePixels = saved.SnapsToDevicePixels.Value
            End If

            ' Clear saved state
            SetSavedState(target, Nothing)
        End Sub
    End Class

End Namespace
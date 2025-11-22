Imports System
Imports System.IO
Imports System.Collections.Concurrent
Imports System.Resources
Imports System.Globalization
Imports SharpDX.D3DCompiler
Imports SharpDX.Direct3D11
Imports SharpDX
Imports osAutoCast.osShaderDataLib
Imports osAsm = System.Reflection.Assembly
Imports pxShader_Text = System.Windows.Media.Effects.PixelShader
Imports pxShader_Pixel = SharpDX.Direct3D11.PixelShader
Imports pxShader_Vertex = SharpDX.Direct3D11.VertexShader
Imports osAutoCast.CoreDataLib
Imports osAutoCast.DataTypeLib.osShaderType
Imports osProgDevice = SharpDX.Direct3D11.Device

Public Module osHandler_Shader

    Public ReadOnly Property ShaderDevice() As osProgDevice
        Get
            Return GetShaderDevice()
        End Get
    End Property

    Public ReadOnly osShaderIdx As New Concurrent.
        ConcurrentBag(Of idxShaderRecord)

    Public ShaderIdxData As New List(Of osShaderDetails)

    Private Function BuildIdxKey(idxName As String) As String
        Return $"{ShaderDevice.NativePointer.ToInt64():X16}|{idxName}"
    End Function

    Private Function FetchAsm() As Reflection.Assembly
        Return osAsm.GetExecutingAssembly()
    End Function

    Public Sub AddShaderToIdx(objShaderRecord As osShaderDetails)
        With objShaderRecord
            Dim idxID = BuildIdxKey(.ShaderName)

            Select Case .ShaderType
                Case sTypePixel : osShaderIdx.Add(New idxShaderRecord(
                                     idxID, InitShader_Pixel(objShaderRecord)))
                Case sTypeText : osShaderIdx.Add(New idxShaderRecord(
                                     idxID, InitShader_Text(objShaderRecord)))
                Case sTypeVertex : osShaderIdx.Add(New idxShaderRecord(
                                     idxID, InitShader_Vertex(objShaderRecord)))
            End Select
        End With
    End Sub

    Public Function FetchShader(objShaderType As osShaderType) As iPxShader
        Return osShaderIdx.First(
            Function(idxObj)
                Return idxObj.ID = FetchShaderIdx(objShaderType)
            End Function).ShaderData
    End Function

    Public Function InitShader_Pixel(objShaderRecord As osShaderDetails) As pxShaderPixel
        Dim pxShaderObj As pxShader_Pixel

        Using objShaderStream = FetchAsm().GetManifestResourceStream(objShaderRecord.ShaderName)
            Using objShaderByte = ShaderBytecode.FromStream(objShaderStream)
                pxShaderObj = New PixelShader(ShaderDevice, objShaderByte)
            End Using
        End Using

        Return New pxShaderPixel(pxShaderObj)
    End Function

    Public Function InitShader_Text(objShaderRecord As osShaderDetails) As pxShaderText
        Dim pxShaderObj As New pxShader_Text

        Using objShaderStream = FetchAsm().GetManifestResourceStream(objShaderRecord.ShaderName)
            pxShaderObj.SetStreamSource(objShaderStream)
        End Using

        Return New pxShaderText(pxShaderObj)
    End Function

    Public Function InitShader_Vertex(objShaderRecord As osShaderDetails) As pxShaderVertex
        Dim pxShaderObj As pxShader_Vertex

        Using objShaderStream = FetchAsm().GetManifestResourceStream(objShaderRecord.ShaderName)
            Using objShaderByte = ShaderBytecode.FromStream(objShaderStream)
                pxShaderObj = New VertexShader(ShaderDevice, objShaderByte)
            End Using
        End Using

        Return New pxShaderVertex(pxShaderObj)
    End Function

    Public Function FetchShaderIdx(objShaderType As osShaderType) As String
        Return BuildIdxKey(ShaderIdxData.FirstOrDefault(
                           Function(objShader) objShader.ShaderType = objShaderType).ShaderName)
    End Function

    'Public Function RemoveCachedPixelShader(objDevice As Device, idxName As String) As Boolean
    '    If objDevice Is Nothing Then Throw New ArgumentNullException(NameOf(objDevice))
    '    If String.IsNullOrWhiteSpace(idxName) Then Throw New ArgumentNullException(NameOf(idxName))

    '    Dim objIdxKey = BuildIdxKey(idxName)
    '    Dim objPxShader As PixelShader = Nothing

    '    If osShaderIdx.TryRemove(objIdxKey, objPxShader) Then
    '        Utilities.Dispose(objPxShader)
    '        Return True
    '    End If

    '    Return False
    'End Function

    'Public Sub ClearShaderCache()
    '    For Each kvp In osShaderIdx.ToArray()
    '        Dim ps As PixelShader = Nothing
    '        If osShaderIdx.TryRemove(kvp.Key, ps) Then
    '            Utilities.Dispose(ps)
    '        End If
    '    Next
    'End Sub

    'Public Sub DisposeCachedShadersForDevice(device As Device)
    '    If device Is Nothing Then Throw New ArgumentNullException(NameOf(device))

    '    Dim devPtrHex = $"{device.NativePointer.ToInt64():X16}|"

    '    For Each objShaderKey In osShaderIdx.Keys.Where(Function(objKey) objKey.StartsWith(devPtrHex, StringComparison.OrdinalIgnoreCase)).ToArray()
    '        Dim objPxShader As PixelShader = Nothing
    '        If osShaderIdx.TryRemove(objShaderKey, objPxShader) Then
    '            Utilities.Dispose(objPxShader)
    '        End If
    '    Next
    'End Sub

End Module

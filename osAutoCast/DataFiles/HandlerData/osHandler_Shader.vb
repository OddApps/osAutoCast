Imports System
Imports System.IO
Imports System.Collections.Concurrent
Imports System.Resources
Imports System.Globalization
Imports SharpDX.D3DCompiler
Imports SharpDX.Direct3D11
Imports SharpDX
Imports osAutoCast.osShaderDataLib
Imports pxShader_Effect = System.Windows.Media.Effects.PixelShader
Imports pxShader_Object = SharpDX.Direct3D11.PixelShader
Imports osAutoCast.CoreDataLib

Public Module osHandler_Shader

    Private osResMan As ResourceManager = My.Resources.ResourceManager

    Public ReadOnly osShaderIdx As New Concurrent.
        ConcurrentDictionary(Of String, iPxShader)()

    Public ShaderIdxData As New List(Of osShaderDetails)


    Private Function BuildIdxKey(device As Device, idxName As String) As String
        Dim devPtr As IntPtr = If(device IsNot Nothing, device.NativePointer, IntPtr.Zero)
        Return $"{devPtr.ToInt64():X16}|{idxName}"

    End Function

    Private Function FetchResource(idxName As String) As Byte()
        Dim objResObj = osResMan.GetObject(idxName, CultureInfo.CurrentUICulture)
        Return DirectCast(objResObj, Byte())
    End Function

    Public Sub AddShaderToIdx(objDevice As Device, objShaderRecord As osShaderDetails)
        With objShaderRecord
            '  Dim objShader = FetchResource(.ShaderName)
            Dim idxKey = BuildIdxKey(objDevice, .ShaderName)

            Select Case .ShaderType
                Case osShaderType.ShaderObject
                    osShaderIdx.TryAdd(idxKey,
                                        GenerateShader(objDevice, objShaderRecord))
                Case osShaderType.ShaderEffect
                    osShaderIdx.TryAdd(idxKey,
                                        GenerateShader(objDevice, objShaderRecord, True))
            End Select
        End With
    End Sub

    Public Function GenerateShader(objDevice As Device, objShaderRecord As osShaderDetails) As pxShaderObject
        Dim pxShaderObj As pxShader_Object
        Dim asm = System.Reflection.Assembly.GetExecutingAssembly()

        Using s = asm.GetManifestResourceStream(objShaderRecord.ShaderName)
            Using bc = ShaderBytecode.FromStream(s)
                pxShaderObj = New PixelShader(objDevice, bc)
            End Using
        End Using

        Return New pxShaderObject(pxShaderObj)
    End Function

    Public Function GenerateShader(objShaderType As osShaderType, objShaderBytes As Byte()) As pxShaderObject
        Dim pxShaderObj As pxShader_Object

        Using pxShaderByteCode As New ShaderBytecode(objShaderBytes)
            pxShaderObj = New PixelShader(GetShaderDevice, pxShaderByteCode)
        End Using

        Return New pxShaderObject(pxShaderObj)
    End Function

    Public Function GenerateShader(objDevice As Device, objShaderRecord As osShaderDetails, isEffect As Boolean) As pxShaderEffect
        Dim pxShaderObj As New pxShader_Effect

        Dim asm = System.Reflection.Assembly.GetExecutingAssembly()

        Using objMemStream = asm.GetManifestResourceStream(objShaderRecord.ShaderName)
            pxShaderObj.SetStreamSource(objMemStream)
        End Using

        Return New pxShaderEffect(pxShaderObj)
    End Function

    Public Function GenerateShader(objShaderType As osShaderType, objShaderBytes As Byte(), isEffect As Boolean) As pxShaderEffect
        Dim pxShaderObj As New pxShader_Effect

        Using objMemStream As New MemoryStream(objShaderBytes)
            pxShaderObj.SetStreamSource(objMemStream)
        End Using

        Return New pxShaderEffect(pxShaderObj)
    End Function

    Public Function LoadPxShader(objDevice As Device, idxName As String) As pxShader_Object
        Dim idxKey = BuildIdxKey(objDevice, idxName)
        Dim objShaderI As iPxShader = Nothing

        osShaderIdx.TryGetValue(idxKey, objShaderI)
        Return objShaderI.ToShaderObj()
    End Function

    Public Function LoadPxShader(idxName As String) As pxShader_Effect
        Dim idxKey = BuildIdxKey(GetShaderDevice(), idxName)
        Dim objShaderI As iPxShader = Nothing

        osShaderIdx.TryGetValue(idxKey, objShaderI)
        Return objShaderI.ToShaderEff()
    End Function

    Public Function RemoveCachedPixelShader(objDevice As Device, idxName As String) As Boolean
        If objDevice Is Nothing Then Throw New ArgumentNullException(NameOf(objDevice))
        If String.IsNullOrWhiteSpace(idxName) Then Throw New ArgumentNullException(NameOf(idxName))

        Dim objIdxKey = BuildIdxKey(objDevice, idxName)
        Dim objPxShader As PixelShader = Nothing

        If osShaderIdx.TryRemove(objIdxKey, objPxShader) Then
            Utilities.Dispose(objPxShader)
            Return True
        End If

        Return False
    End Function

    Public Sub ClearShaderCache()
        For Each kvp In osShaderIdx.ToArray()
            Dim ps As PixelShader = Nothing
            If osShaderIdx.TryRemove(kvp.Key, ps) Then
                Utilities.Dispose(ps)
            End If
        Next
    End Sub

    Public Sub DisposeCachedShadersForDevice(device As Device)
        If device Is Nothing Then Throw New ArgumentNullException(NameOf(device))

        Dim devPtrHex = $"{device.NativePointer.ToInt64():X16}|"

        For Each objShaderKey In osShaderIdx.Keys.Where(Function(objKey) objKey.StartsWith(devPtrHex, StringComparison.OrdinalIgnoreCase)).ToArray()
            Dim objPxShader As PixelShader = Nothing
            If osShaderIdx.TryRemove(objShaderKey, objPxShader) Then
                Utilities.Dispose(objPxShader)
            End If
        Next
    End Sub

End Module

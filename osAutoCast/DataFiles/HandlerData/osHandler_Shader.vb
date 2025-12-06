Imports System.Collections.Concurrent
Imports System.IO
Imports System.Text
Imports System.Windows.Threading
Imports osAutoCast.CoreDataLib
Imports osAutoCast.DataTypeLib.osShaderType
Imports osAutoCast.osShaderDataLib
Imports SharpDX.D3DCompiler
Imports SharpDX.Direct3D11
Imports osAsm = System.Reflection.Assembly
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports pxShader_Pixel = SharpDX.Direct3D11.PixelShader
Imports pxShader_Text = System.Windows.Media.Effects.PixelShader
Imports pxShader_Vertex = SharpDX.Direct3D11.VertexShader

Public Module osHandler_Shader

    Private ShaderCompressedData As String = "osShader_Data.csd"

    Private pxShaderData_Pixel As pxShaderPixel
    Private pxShaderData_Text As pxShaderText
    Private pxShaderData_Vertex As pxShaderVertex

    Private shaderBytes_Pixel As Byte()
    Private shaderBytes_Text As Byte()
    Private shaderBytes_Vertex As Byte()

    Public osShaderNameList As New List(Of String) From {
        {"osShader_ProgPixel.ps"},
        {"osShader_ProgVertex.ps"},
        {"osShader_Text.ps"}
    }

    Public ShaderDataIdx As New Dictionary(Of String, Byte())

    Private ShaderDataNames() As String = {"Px", "Vx", "Tx"}

    Public ReadOnly osShaderIdx As New Concurrent.
        ConcurrentBag(Of idxShaderRecord)

    Public ShaderDetailsIdx As New List(Of osShaderDetails)

    Public ReadOnly Property ShaderDevice() As osProgDevice
        Get
            Return GetShaderDevice()
        End Get
    End Property

    Private Function BuildIdxKey(idxName As String) As String
        Return $"{ShaderDevice.NativePointer.ToInt64():X16}|{idxName}"
    End Function

    Private Function FetchAsm() As Reflection.Assembly
        Return osAsm.GetExecutingAssembly()
    End Function

    Public Async Function StreamToBytesAsync(stream As Stream) As Task(Of Byte())
        If stream Is Nothing Then Return Nothing

        If stream.CanSeek Then
            stream.Position = 0
        End If

        Using ms As New MemoryStream()
            Await stream.CopyToAsync(ms)
            Return ms.ToArray()
        End Using
    End Function

    Public Async Function BuildShaderCatalog() As Task(Of Dictionary(Of String, Byte()))
        Return Await Task.Run(
            Async Function()
                Dim objShaderPrep = Await InitShaderPrep()

                Return objShaderPrep.Select(
                    Function(shaderB, idx) ShaderDataRecord(ShaderDataNames(idx), shaderB.Data)).
                        ToDictionary(Function(shaderRecord) shaderRecord.Key,
                                     Function(shaderRecord) shaderRecord.Value)
            End Function)
    End Function

    Private Function ShaderDataRecord(shaderN As String, shaderB As Byte()) As KeyValuePair(Of String, Byte())
        Return New KeyValuePair(Of String, Byte())(shaderN, shaderB)
    End Function

    Public Async Function InitShaderPrep() As Task(Of ShaderBytecode())
        Return Await Task.Run(
            Async Function()
                Dim objShaderBytes = Await DecompressFromBytes()
                Return Await PrepShaderData(objShaderBytes)
            End Function)
    End Function

    Public Async Function PrepShaderData(objShaderBytes As Byte()) As Task(Of ShaderBytecode())
        Return Await Task.Run(
            Function()
                Using objShaderMemory = New MemoryStream(objShaderBytes)
                    Using objShaderCompressed = ShaderBytecode.FromStream(objShaderMemory)
                        Return objShaderCompressed.Decompress()
                    End Using
                End Using
            End Function)
    End Function

    Private Function GetShaderDataStream() As Stream
        Return FetchAsm().GetManifestResourceStream($"osAutoCast.{ShaderCompressedData}")
    End Function

    Public Async Function DecompressFromBytes() As Task(Of Byte())
        Return Await Task.Run(
            Async Function()
                Using objShaderStream = GetShaderDataStream()
                    Return Await StreamToBytesAsync(objShaderStream)
                End Using
            End Function)
    End Function

    Public Async Function PreloadShaderCatalog(objShaderTask As Task(Of Dictionary(Of String, Byte()))) As Task
        Await Task.Run(
            Async Function()

                ShaderDataIdx = Await objShaderTask

                Dim objShaderTask_Px = Task.Run(
                    Sub()
                        PrepDispatcher().Invoke(
                            Sub()
                                Dim pxShaderObj As pxShader_Pixel

                                Using objShaderStream As New MemoryStream(ShaderDataIdx("Px"))
                                    Using objShaderByte = ShaderBytecode.FromStream(objShaderStream)
                                        pxShaderObj = New PixelShader(ShaderDevice, objShaderByte)
                                    End Using
                                End Using

                                pxShaderData_Pixel = New pxShaderPixel(pxShaderObj)
                            End Sub)
                    End Sub)

                Dim objShaderTask_Tx = Task.Run(
                    Sub()
                        PrepDispatcher().Invoke(
                            Sub()
                                Dim pxShaderObj As New pxShader_Text

                                Using objShaderStream As New MemoryStream(ShaderDataIdx("Tx"))
                                    pxShaderObj.SetStreamSource(objShaderStream)
                                End Using

                                pxShaderData_Text = New pxShaderText(pxShaderObj)
                            End Sub)
                    End Sub)

                Dim objShaderTask_Vx = Task.Run(
                    Sub()
                        PrepDispatcher().Invoke(
                            Sub()
                                Dim pxShaderObj As pxShader_Vertex

                                Using objShaderStream As New MemoryStream(ShaderDataIdx("Vx"))
                                    Using objShaderByte = ShaderBytecode.FromStream(objShaderStream)
                                        pxShaderObj = New VertexShader(ShaderDevice, objShaderByte)
                                    End Using
                                End Using

                                pxShaderData_Vertex = New pxShaderVertex(pxShaderObj)
                            End Sub)
                    End Sub)

                Await Task.WhenAll(objShaderTask_Px,
                                   objShaderTask_Tx,
                                   objShaderTask_Vx)

            End Function)
    End Function

    Public Async Function AddShaderToIdx(objShaderDetails As osShaderDetails) As Task
        Await PrepDispatcher.InvokeAsync(
            Sub()
                Dim idxID = BuildIdxKey(objShaderDetails.ShaderName)

                Select Case objShaderDetails.ShaderType
                    Case sTypePixel
                        osShaderIdx.Add(New idxShaderRecord(
                                        idxID, pxShaderData_Pixel))
                    Case sTypeText
                        osShaderIdx.Add(New idxShaderRecord(
                                        idxID, pxShaderData_Text))
                    Case sTypeVertex
                        osShaderIdx.Add(New idxShaderRecord(
                                        idxID, pxShaderData_Vertex))
                End Select
            End Sub, DispatcherPriority.Normal)
    End Function

    Public Function FetchShader(objShaderType As osShaderType) As iPxShader
        Return osShaderIdx.First(
            Function(idxObj)
                Return idxObj.ID = FetchShaderIdx(objShaderType)
            End Function).ShaderData
    End Function

    Public Function FetchShaderIdx(objShaderType As osShaderType) As String
        Return BuildIdxKey(ShaderDetailsIdx.FirstOrDefault(
                           Function(objShader) objShader.ShaderType = objShaderType).ShaderName)
    End Function

End Module

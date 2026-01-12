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

    Private ShaderCompressedData As String = "osEffect_Shaders.csd"

    Private pxShaderData_Pixel As pxShaderPixel
    Private pxShaderData_Vertex As pxShaderVertex
    Private pxShaderData_Text_G As pxShaderText_G
    Private pxShaderData_Text_S As pxShaderText_S

    Public osShaderNameList As New List(Of String) From {
        {"osShader_ProgPixel.ps"}, {"osShader_ProgVertex.ps"},
        {"osShader_TextGlow.ps"}, {"osShader_TextStroke.ps"}
    }



    Public ShaderDataIdx As New Dictionary(Of String, Byte())

    Private ShaderDataNames() As String = {"Px", "Vx", "TxG", "TxS"}

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

    Public Function GetTestShader(isStroke As Boolean) As pxShader_Text
        Return New pxShader_Text() With {
            .UriSource = New Uri("/osAutoCast;component/DataFiles/VisualData/EffectsLib/" &
                $"EffectResources/osTestShader_{If(isStroke, "TextStroke",
                "TextGlow")}.ps", UriKind.Relative)}
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

    'Public Function BuildShaderCatalog() As Task(Of Dictionary(Of String, Byte()))
    '    Return Task.Run(
    '        Async Function()
    '            Dim objShaderPrep = Await InitShaderPrep()

    '            Return objShaderPrep.Select(
    '                Function(shaderB, idx) ShaderDataRecord(ShaderDataNames(idx), shaderB.Data)).
    '                    ToDictionary(Function(shaderRecord) shaderRecord.Key,
    '                                 Function(shaderRecord) shaderRecord.Value)
    '        End Function)
    'End Function

    Public Async Function BuildShaderCatalog(isNew As Boolean) As Task
        Dim shaderBytes = Await InitShaderPrep()
        '  Await Task.Run(
        '     Sub()
        ShaderDataIdx = shaderBytes.
                    Select(Function(shaderB, idx)
                               Return ShaderDataRecord(
                                    ShaderDataNames(idx), shaderB.Data)
                           End Function).ToDictionary(
                                Function(r) r.Key,
                                Function(r) r.Value)
        '    End Sub)
    End Function

    Public Function BuildShaderCatalog() As Task(Of Dictionary(Of String, Byte()))
        Return Task.Run(
        Function()
            Dim shaderBytes = InitShaderPrep_Sync()

            Return shaderBytes.
                Select(Function(shaderB, idx)
                           Return ShaderDataRecord(
                               ShaderDataNames(idx),
                               shaderB.Data)
                       End Function).
                ToDictionary(Function(r) r.Key,
                             Function(r) r.Value)
        End Function)
    End Function

    Private Function InitShaderPrep_Sync() As ShaderBytecode()
        Dim objShaderBytes = DecompressFromBytes_Sync()
        Return PrepShaderData_Sync(objShaderBytes)
    End Function

    Private Function PrepShaderData_Sync(objShaderBytes As Byte()) As ShaderBytecode()
        Using objShaderMemory = New MemoryStream(objShaderBytes)
            Using objShaderCompressed = ShaderBytecode.FromStream(objShaderMemory)
                Return objShaderCompressed.Decompress()
            End Using
        End Using
    End Function

    Private Function DecompressFromBytes_Sync() As Byte()
        Using objShaderStream = GetShaderDataStream()
            Using ms As New MemoryStream()
                objShaderStream.CopyTo(ms)
                Return ms.ToArray()
            End Using
        End Using
    End Function


    Private Function ShaderDataRecord(shaderN As String, shaderB As Byte()) As KeyValuePair(Of String, Byte())
        Return New KeyValuePair(Of String, Byte())(shaderN, shaderB)
    End Function

    Public Async Function InitShaderPrep() As Task(Of ShaderBytecode())
        'Return Await Task.Run(
        '    Async Function()
        Dim objShaderBytes = Await DecompressFromBytes()
                Return Await PrepShaderData(objShaderBytes)
        '   End Function)
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
        Using objShaderStream = GetShaderDataStream()
            Return Await StreamToBytesAsync(objShaderStream)
        End Using
    End Function

    'Public Async Function DecompressFromBytes() As Task(Of Byte())
    '    Return Await Task.Run(
    '        Async Function()
    '            Using objShaderStream = GetShaderDataStream()
    '                Return Await StreamToBytesAsync(objShaderStream)
    '            End Using
    '        End Function)
    'End Function

    Public Async Function PreloadShaderCatalog() As Task
        Dim objTaskDone As Boolean

        objTaskDone = Await Task.Run(
            Async Function()
                Dim pxTask = Task.Run(Function()
                                          Using ms As New MemoryStream(ShaderDataIdx("Px"))
                                              Return ShaderBytecode.FromStream(ms)
                                          End Using
                                      End Function)

                Dim vxTask = Task.Run(Function()
                                          Using ms As New MemoryStream(ShaderDataIdx("Vx"))
                                              Return ShaderBytecode.FromStream(ms)
                                          End Using
                                      End Function)

                Dim aba = Await Task.WhenAll(pxTask, vxTask)

                Await PrepDispatcher().InvokeAsync(
                                         Sub()
                                             ' Pixel
                                             Using pxByte = aba(0)
                                                 Dim pxShader = New PixelShader(ShaderDevice, pxByte)
                                                 pxShaderData_Pixel = New pxShaderPixel(pxShader)
                                             End Using

                                             ' Vertex
                                             Using vxByte = aba(1)
                                                 Dim vxShader = New VertexShader(ShaderDevice, vxByte)
                                                 pxShaderData_Vertex = New pxShaderVertex(vxShader)
                                             End Using

                                             ' Text shaders
                                             Dim txG As New pxShader_Text
                                             txG.SetStreamSource(New MemoryStream(ShaderDataIdx("TxG")))
                                             pxShaderData_Text_G = New pxShaderText_G(txG)

                                             Dim txS As New pxShader_Text
                                             txS.SetStreamSource(New MemoryStream(ShaderDataIdx("TxS")))
                                             pxShaderData_Text_S = New pxShaderText_S(txS)
                                         End Sub, DispatcherPriority.Background)
                Return True
            End Function)
        ' ShaderDataIdx = Await BuildShaderCatalog()

        ' Load bytecode in background

    End Function


    'Public Function PreloadShaderCatalog(objShaderLst As Dictionary(Of String, Byte())) As Task
    '    Return Task.Run(
    '        Async Function()

    '            ShaderDataIdx = objShaderLst

    '            Dim objShaderTask_Px = Task.Run(
    '                Sub()
    '                    PrepDispatcher().Invoke(
    '                        Sub()
    '                            Dim pxShaderObj As pxShader_Pixel

    '                            Using objShaderStream As New MemoryStream(ShaderDataIdx("Px"))
    '                                Using objShaderByte = ShaderBytecode.FromStream(objShaderStream)
    '                                    pxShaderObj = New PixelShader(ShaderDevice, objShaderByte)
    '                                End Using
    '                            End Using

    '                            pxShaderData_Pixel = New pxShaderPixel(pxShaderObj)
    '                        End Sub)
    '                End Sub)

    '            Dim objShaderTask_TxG = Task.Run(
    '                Sub()
    '                    PrepDispatcher().Invoke(
    '                        Sub()
    '                            Dim pxShaderObj As New pxShader_Text

    '                            Using objShaderStream As New MemoryStream(ShaderDataIdx("TxG"))
    '                                pxShaderObj.SetStreamSource(objShaderStream)
    '                            End Using

    '                            pxShaderData_Text_G = New pxShaderText_G(pxShaderObj)
    '                        End Sub)
    '                End Sub)

    '            Dim objShaderTask_TxS = Task.Run(
    '                Sub()
    '                    PrepDispatcher().Invoke(
    '                        Sub()
    '                            Dim pxShaderObj As New pxShader_Text

    '                            Using objShaderStream As New MemoryStream(ShaderDataIdx("TxS"))
    '                                pxShaderObj.SetStreamSource(objShaderStream)
    '                            End Using

    '                            pxShaderData_Text_S = New pxShaderText_S(pxShaderObj)
    '                        End Sub)
    '                End Sub)

    '            Dim objShaderTask_Vx = Task.Run(
    '                Sub()
    '                    PrepDispatcher().Invoke(
    '                        Sub()
    '                            Dim pxShaderObj As pxShader_Vertex

    '                            Using objShaderStream As New MemoryStream(ShaderDataIdx("Vx"))
    '                                Using objShaderByte = ShaderBytecode.FromStream(objShaderStream)

    '                                    pxShaderObj = New VertexShader(ShaderDevice, objShaderByte)
    '                                End Using
    '                            End Using

    '                            pxShaderData_Vertex = New pxShaderVertex(pxShaderObj)
    '                        End Sub)
    '                End Sub)

    '            Await Task.WhenAll(objShaderTask_Px, objShaderTask_Vx,
    '                               objShaderTask_TxG, objShaderTask_TxS)

    '        End Function)
    'End Function

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

                Dim objShaderTask_TxG = Task.Run(
                    Sub()
                        PrepDispatcher().Invoke(
                            Sub()
                                Dim pxShaderObj As New pxShader_Text

                                Using objShaderStream As New MemoryStream(ShaderDataIdx("TxG"))
                                    pxShaderObj.SetStreamSource(objShaderStream)
                                End Using

                                pxShaderData_Text_G = New pxShaderText_G(pxShaderObj)
                            End Sub)
                    End Sub)

                Dim objShaderTask_TxS = Task.Run(
                    Sub()
                        PrepDispatcher().Invoke(
                            Sub()
                                Dim pxShaderObj As New pxShader_Text

                                Using objShaderStream As New MemoryStream(ShaderDataIdx("TxS"))
                                    pxShaderObj.SetStreamSource(objShaderStream)
                                End Using

                                pxShaderData_Text_S = New pxShaderText_S(pxShaderObj)
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

                Await Task.WhenAll(objShaderTask_Px, objShaderTask_Vx,
                                   objShaderTask_TxG, objShaderTask_TxS)

            End Function)
    End Function

    Public Async Function AddShaderToIdx(objShaderDetails As osShaderDetails) As Task

        Await PrepDispatcher().InvokeAsync(
                    Sub()
                        Dim idxID = BuildIdxKey(objShaderDetails.ShaderName)

                        Select Case objShaderDetails.ShaderType
                            Case sTypePixel
                                osShaderIdx.Add(New idxShaderRecord(
                                        idxID, pxShaderData_Pixel))
                            Case sTypeText_G
                                osShaderIdx.Add(New idxShaderRecord(
                                        idxID, pxShaderData_Text_G))
                            Case sTypeText_S
                                osShaderIdx.Add(New idxShaderRecord(
                                        idxID, pxShaderData_Text_S))
                            Case sTypeVertex
                                osShaderIdx.Add(New idxShaderRecord(
                                        idxID, pxShaderData_Vertex))
                        End Select
                    End Sub, DispatcherPriority.Background)
    End Function

    Public Async Function AddShaderToIdxAsync(
    objShaderDetails As osShaderDetails
) As Task

        Dim dispatcher = PrepDispatcher()

        Await dispatcher.InvokeAsync(
        Sub()
            Dim idxID = BuildIdxKey(objShaderDetails.ShaderName)

            Select Case objShaderDetails.ShaderType
                Case sTypePixel
                    osShaderIdx.Add(New idxShaderRecord(idxID, pxShaderData_Pixel))

                Case sTypeText_G
                    osShaderIdx.Add(New idxShaderRecord(idxID, pxShaderData_Text_G))

                Case sTypeText_S
                    osShaderIdx.Add(New idxShaderRecord(idxID, pxShaderData_Text_S))

                Case sTypeVertex
                    osShaderIdx.Add(New idxShaderRecord(idxID, pxShaderData_Vertex))
            End Select
        End Sub,
        DispatcherPriority.Background
    ).Task.ConfigureAwait(False)

    End Function


    Public Function FetchShader(objShaderType As osShaderType) As iPxShader
        Return osShaderIdx.First(
            Function(idxObj)
                Return idxObj.ID = FetchShaderIdx(objShaderType)
            End Function).ShaderData
    End Function

    Public Function FetchShader(objShaderType As osShaderType, isTask As Boolean) As Task(Of iPxShader)
        Return Task.Run(Function()
                            Return osShaderIdx.First(
                                        Function(idxObj)
                                            Return idxObj.ID = FetchShaderIdx(objShaderType)
                                        End Function).ShaderData
                        End Function)
    End Function

    Public Function FetchShaderIdx(objShaderType As osShaderType) As String
        Return BuildIdxKey(ShaderDetailsIdx.FirstOrDefault(
                           Function(objShader) objShader.ShaderType = objShaderType).ShaderName)
    End Function

End Module

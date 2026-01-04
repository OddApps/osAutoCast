Imports System.Windows.Media.Animation
Imports osAutoCast.DataTypeLib.LoadContentData
Imports osAutoCast.DataTypeLib.LoadEventType
Imports osAutoCast.osLoader_UI
Imports osAutoCast.osShaderDataLib
Imports SharpDX.D3DCompiler
Imports SharpDX.Direct3D11
Imports osAsm = System.Reflection.Assembly
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports pxShader_Pixel = SharpDX.Direct3D11.PixelShader
Imports pxShader_Text = System.Windows.Media.Effects.PixelShader
Imports pxShader_Vertex = SharpDX.Direct3D11.VertexShader
Imports System.Collections.Concurrent
Imports System.IO

Dim shaderList As New List(Of ShaderBytecode)
Dim fafa As IEnumerable(Of String) = New String() {"C:\shaders\osShader_ProgPixel.ps", "C:\shaders\osShader_ProgVertex.ps", 
											"C:\shaders\osShader_TextGlow.ps", "C:\shaders\osShader_TextStroke.ps"}

For Each f In fafa
	Using fs As FileStream = File.OpenRead(f)
		Dim sb As ShaderBytecode = ShaderBytecode.FromStream(fs)
		shaderList.Add(sb)
	End Using
Next

Dim compressed As ShaderBytecode = ShaderBytecode.Compress(shaderList.ToArray())

File.WriteAllBytes("C:\shaders\fa.fa", compressed.Data)

compressed.Dispose()
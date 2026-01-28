Imports System.ComponentModel
Imports System.Windows.Threading
Imports Microsoft.Win32.SafeHandles
Imports SharpDX.Direct3D
Imports SharpDX.Direct3D11
Imports SharpDX.DXGI
Imports FactoryDW = SharpDX.DirectWrite.Factory
Imports osDwFactoryType = SharpDX.DirectWrite.FactoryType
Imports osFactoryType = SharpDX.Direct2D1.FactoryType
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports osProgDeviceContext = SharpDX.Direct3D11.DeviceContext
Imports osProgDxgiDevice = SharpDX.DXGI.Device1
Imports osProgDxgiFactory = SharpDX.DXGI.Factory
Imports osProgDxgiFactory2 = SharpDX.DXGI.Factory2
Imports osProgFactoryD2D = SharpDX.Direct2D1.Factory

Public NotInheritable Class osHandler_Graphics

    Private Shared osGraphicFlags As DeviceCreationFlags = DeviceCreationFlags.BgraSupport

    Private Shared _progDevice As osProgDevice
    Public Shared ReadOnly Property pDevice As osProgDevice
        Get
            Return _progDevice
        End Get
    End Property

    Private Shared _progContext As osProgDeviceContext
    Public Shared ReadOnly Property pContext As osProgDeviceContext
        Get
            Return _progContext
        End Get
    End Property

    Private Shared _progDxgiFactory As osProgDxgiFactory
    Public Shared ReadOnly Property pDxgiFactory As osProgDxgiFactory
        Get
            Return _progDxgiFactory
        End Get
    End Property

    Private Shared _progDxgiFactory2 As osProgDxgiFactory2
    Public Shared ReadOnly Property pDxgiFactory2 As osProgDxgiFactory2
        Get
            Return _progDxgiFactory2
        End Get
    End Property

    Private Shared _progD2dFactory As osProgFactoryD2D
    Public Shared ReadOnly Property pD2DFactory As osProgFactoryD2D
        Get
            Return _progD2dFactory
        End Get
    End Property

    Private Shared _progDwFactory As FactoryDW
    Public Shared ReadOnly Property pDWFactory As FactoryDW
        Get
            Return _progDwFactory
        End Get
    End Property

    Private Sub New()
    End Sub

    Public Shared Async Function EnsureCreated() As Task
        Dim objOsGraphics = Await InitGraphicCreation()

        With objOsGraphics
            _progD2dFactory = .osGraphics_ProgFactoryD2D
            _progDwFactory = .osGraphics_ProgDwFactory

            _progDxgiFactory2 = .osGraphics_ProgDxgiFactory2

            _progDevice = .osGraphics_ProgDevice
            _progContext = .osGraphics_ProgDeviceContext
            _progDxgiFactory = .osGraphics_ProgDxgiFactory
        End With
    End Function

    Private Shared Function InitGraphicCreation() As Task(Of osGraphicsData)
        Dim objTask_GraphicsData As New TaskCompletionSource(Of osGraphicsData)()
        Dim objTask_InitGraphics As New BackgroundWorker()

        AddHandler objTask_InitGraphics.DoWork,
            Sub(sender As Object, e As DoWorkEventArgs)
                Dim osG_ProgDevice = New osProgDevice(DriverType.Hardware, osGraphicFlags)
                Dim osG_ProgContext = osG_ProgDevice.ImmediateContext

                Dim osG_ProgDxgiFactory As osProgDxgiFactory = Nothing
                Using dxgiDev = osG_ProgDevice.QueryInterface(Of osProgDxgiDevice)()
                    Using objAdapter = dxgiDev.Adapter
                        osG_ProgDxgiFactory = objAdapter.GetParent(Of osProgDxgiFactory)()
                    End Using
                End Using

                Dim resProgD2dFactory = New osProgFactoryD2D(osFactoryType.MultiThreaded)
                Dim resProgDwFactory = New FactoryDW(osDwFactoryType.Shared)
                Dim resProgDxgiFactory2 = osG_ProgDxgiFactory.QueryInterface(Of osProgDxgiFactory2)()

                e.Result = New osGraphicsData(resProgD2dFactory, resProgDwFactory,
                                      osG_ProgDevice, resProgDxgiFactory2, osG_ProgContext, osG_ProgDxgiFactory)
            End Sub

        AddHandler objTask_InitGraphics.RunWorkerCompleted,
            Sub(sender As Object, e As RunWorkerCompletedEventArgs)
                objTask_GraphicsData.SetResult(DirectCast(e.Result, osGraphicsData))
                objTask_InitGraphics.Dispose()
            End Sub

        objTask_InitGraphics.RunWorkerAsync()

        Return objTask_GraphicsData.Task
    End Function

    Public Shared Sub FlushDevice()
        If _progContext Is Nothing Then Return

        _progContext.ClearState()
        _progContext.Flush()
    End Sub

    Public Shared Sub DisposeAll()
        _progDwFactory.SafeDispose()
        _progD2dFactory.SafeDispose()
        _progDxgiFactory.SafeDispose()
        _progContext.SafeDispose()
        _progDevice.SafeDispose()
    End Sub

End Class
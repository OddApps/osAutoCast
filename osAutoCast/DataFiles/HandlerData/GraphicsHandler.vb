Imports SharpDX.Direct3D
Imports SharpDX.Direct3D11
Imports SharpDX.DXGI
Imports osProgDevice = SharpDX.Direct3D11.Device
Imports osFactoryType = SharpDX.Direct2D1.FactoryType
Imports osProgDeviceContext = SharpDX.Direct3D11.DeviceContext
Imports osProgFactoryD2D = SharpDX.Direct2D1.Factory
Imports FactoryDW = SharpDX.DirectWrite.Factory
Imports osProgDXGIFactory = SharpDX.DXGI.Factory

Public NotInheritable Class GraphicsHandler

    Private Shared _initLock As New Object()

    Private Shared _progDevice As osProgDevice
    Public Shared ReadOnly Property pDevice As osProgDevice
        Get
            ' EnsureCreated()
            Return _progDevice
        End Get
    End Property

    Private Shared _progContext As osProgDeviceContext
    Public Shared ReadOnly Property pContext As osProgDeviceContext
        Get
            EnsureCreated()
            Return _progContext
        End Get
    End Property

    Private Shared _progDxgiFactory As osProgDXGIFactory
    Public Shared ReadOnly Property pDxgiFactory As osProgDXGIFactory
        Get
            EnsureCreated()
            Return _progDxgiFactory
        End Get
    End Property

    Private Shared _progD2dFactory As osProgFactoryD2D
    Public Shared ReadOnly Property pD2DFactory As osProgFactoryD2D
        Get
            EnsureCreated()
            Return _progD2dFactory
        End Get
    End Property

    Private Shared _progDwFactory As FactoryDW
    Public Shared ReadOnly Property pDWFactory As FactoryDW
        Get
            EnsureCreated()
            Return _progDwFactory
        End Get
    End Property

    Private Sub New()  ' Prevent instantiation
    End Sub

    Public Shared Sub EnsureCreated()
        If _progDevice IsNot Nothing Then Return
        SyncLock _initLock
            If _progDevice IsNot Nothing Then Return

            Dim flags = DeviceCreationFlags.BgraSupport
            _progDevice = New osProgDevice(DriverType.Hardware, flags)
            _progContext = _progDevice.ImmediateContext

            Using dxgiDev = _progDevice.QueryInterface(Of SharpDX.DXGI.Device)()
                Using adapter = dxgiDev.Adapter
                    _progDxgiFactory = adapter.GetParent(Of SharpDX.DXGI.Factory)()
                End Using
            End Using

            _progD2dFactory = New osProgFactoryD2D(osFactoryType.MultiThreaded)
            _progDwFactory = New FactoryDW(SharpDX.DirectWrite.FactoryType.Shared)
        End SyncLock
    End Sub

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

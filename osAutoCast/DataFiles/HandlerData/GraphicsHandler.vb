Imports System.Threading
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
Imports osProgFactoryD2D = SharpDX.Direct2D1.Factory

Public NotInheritable Class GraphicsHandler

    Private Shared _initLock As New Object()

    Private Shared _progDevice As osProgDevice
    Public Shared ReadOnly Property pDevice As osProgDevice
        Get
            EnsureCreated()
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

    Private Shared _progDxgiFactory As osProgDxgiFactory
    Public Shared ReadOnly Property pDxgiFactory As osProgDxgiFactory
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

    Private Sub New()
    End Sub

    Public Shared Sub EnsureCreated()

        If _progDevice IsNot Nothing Then Return

        SyncLock _initLock
            If _progDevice IsNot Nothing Then Return

            Dim deviceFlags = DeviceCreationFlags.BgraSupport

            _progDevice = New osProgDevice(DriverType.Hardware, deviceFlags)
            _progContext = _progDevice.ImmediateContext

            Using dxgiDev = _progDevice.QueryInterface(Of osProgDxgiDevice)()
                Using adapter = dxgiDev.Adapter
                    _progDxgiFactory = adapter.GetParent(Of osProgDxgiFactory)()
                End Using
            End Using

            _progD2dFactory = New osProgFactoryD2D(osFactoryType.MultiThreaded)
            _progDwFactory = New FactoryDW(osDwFactoryType.Shared)
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

Public Module WaitHandleAsync

    <Runtime.CompilerServices.Extension>
    Public Function AwaitSignalAsync(evHandle As IntPtr, Optional ct As CancellationToken = Nothing) As Task

        If evHandle = IntPtr.Zero Then
            Return Task.CompletedTask
        End If

        Dim wh As New EventWaitHandle(False, EventResetMode.AutoReset)
        wh.SafeWaitHandle = New SafeWaitHandle(evHandle, ownsHandle:=False)

        Dim tcs = New TaskCompletionSource(Of Object)(TaskCreationOptions.RunContinuationsAsynchronously)
        Dim reg As RegisteredWaitHandle = Nothing

        reg = ThreadPool.RegisterWaitForSingleObject(wh,
           Sub(state, timedOut)
               reg.Unregister(Nothing)
               If ct.IsCancellationRequested Then
                   tcs.TrySetCanceled(ct)
               Else
                   tcs.TrySetResult(Nothing)
               End If
           End Sub, state:=Nothing,
           millisecondsTimeOutInterval:=-1,
           executeOnlyOnce:=True)

        If ct.CanBeCanceled Then
            ct.Register(Sub()
                            reg.Unregister(Nothing)
                            tcs.TrySetCanceled(ct)
                        End Sub)
        End If

        Return tcs.Task
    End Function

End Module

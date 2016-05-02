using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Services.Audio.Implementation.Interop
{
    [Guid(Constants.IID_IMMDeviceEnumerator)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(
            [In] EDataFlow dataFlow, 
            [In] DEVICE_STATE dwStateMask, 
            [MarshalAs(UnmanagedType.Interface)] out IMMDeviceCollection ppDevices);
        void GetDefaultAudioEndpoint(
            [In] EDataFlow dataFlow, 
            [In] ERole role, 
            [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppEndpoint);
        void GetDevice(
            [MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrId,
            [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppDevice);
        void RegisterEndpointNotificationCallback(
            [MarshalAs(UnmanagedType.Interface)] [In] IMMNotificationClient pClient);
        void UnregisterEndpointNotificationCallback(
            [MarshalAs(UnmanagedType.Interface)] [In] IMMNotificationClient pClient);
    }
}

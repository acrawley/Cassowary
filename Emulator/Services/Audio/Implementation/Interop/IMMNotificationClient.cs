using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Services.Audio.Implementation.Interop
{
    [Guid(Constants.IID_IMMNotificationClient)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IMMNotificationClient
    {
        void OnDeviceStateChanged(
            [MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDeviceId,
            [In] uint dwNewState);
        void OnDeviceAdded(
            [MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDeviceId);
        void OnDeviceRemoved(
            [MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDeviceId);
        void OnDefaultDeviceChanged(
            [In] EDataFlow flow, 
            [In] ERole role, 
            [MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDefaultDeviceId);
        void OnPropertyValueChanged(
            [MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDeviceId,
            [In] PropertyKey key);
    }
}

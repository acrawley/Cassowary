using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Audio.Implementation.Interop
{
    [Guid(Constants.IID_IMMDeviceCollection)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IMMDeviceCollection
    {
        void GetCount(out uint pcDevices);
        void Item(
            [In] uint nDevice, 
            [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppDevice);
    }
}

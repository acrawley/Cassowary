using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Audio.Implementation.Interop
{
    [Guid(Constants.IID_IMMDevice)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IMMDevice
    {
        void Activate(
            [In] ref Guid iid, 
            [In] CLSCTX dwClsCtx, 
            [In] ref IntPtr pActivationParams, 
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        void OpenPropertyStore(
            [In] STGM stgmAccess, 
            [MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppProperties);
        void GetId(
            [MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        void GetState(out DEVICE_STATE pdwState);
    }
}

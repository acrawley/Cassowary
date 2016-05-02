using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Services.Audio.Implementation.Interop
{
    [Guid(Constants.IID_IPropertyStore)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(
            [In] uint iProp, 
            out PropertyKey pkey);
        void GetValue(
            [In] ref PropertyKey key,
            out PROPVARIANT pv);
        void SetValue(
            [In] ref PropertyKey key,
            [In] ref PROPVARIANT propvar);
        void Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;
    }
}

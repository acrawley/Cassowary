using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Services.Audio.Implementation.Interop
{
    [Guid(Constants.IID_IAudioRenderClient)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IAudioRenderClient
    {
        void GetBuffer(
            [In] UInt32 NumFramesRequested,
            out IntPtr ppData);

        void ReleaseBuffer(
            [In] UInt32 NumFramesWritten,
            [In] UInt32 dwFLags);
    }
}

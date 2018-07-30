using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Audio.Implementation.Interop
{
    [Guid(Constants.IID_IAudioClient)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IAudioClient
    {
        void Initialize(
            [In] AUDCLNT_SHAREMODE ShareMode,
            [In] UInt32 StreamFlags,
            [In] Int64 hnsBufferDuration,
            [In] Int64 hnsPeriodicity,
            [In] IntPtr pFormat,
            [In] ref Guid AudioSessionGuid);

        void GetBufferSize(out UInt32 pNumBufferFrames);

        void GetStreamLatency(out Int64 phnsLatency);

        void GetCurrentPadding(out UInt32 pNumPaddingFrames);

        void IsFormatSupported(
            [In] AUDCLNT_SHAREMODE ShareMode,
            [In] ref WAVEFORMATEX pFormat,
            out IntPtr ppClosestMatch);

        void GetMixFormat(out IntPtr ppDeviceFormat);

        void GetDevicePeriod(
            out Int64 phnsDefaultDevicePeriod,
            out Int64 phnsMinimumDevicePeriod);

        void Start();

        void Stop();

        void Reset();

        void SetEventHandle(
            [In] IntPtr eventHandle);

        void GetService(
            [In] ref Guid riid,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    }

    internal enum AUDCLNT_SHAREMODE
    {
        AUDCLNT_SHAREMODE_SHARED,
        AUDCLNT_SHAREMODE_EXCLUSIVE,
    }

    internal enum AUDCLNT_STREAMFLAGS : UInt32
    {
        AUDCLNT_STREAMFLAGS_CROSSPROCESS = 0x00010000,
        AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000,
        AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00040000,
        AUDCLNT_STREAMFLAGS_NOPERSIST = 0x00080000,
        AUDCLNT_STREAMFLAGS_RATEADJUST = 0x00100000,
        AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM = 0x80000000,
        AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY=0x08000000,
    }

    internal enum AUDCLNT_SESSIONFLAGS : UInt32
    {
        AUDCLNT_SESSIONFLAGS_EXPIREWHENUNOWNED = 0x10000000,
        AUDCLNT_SESSIONFLAGS_DISPLAY_HIDE = 0x20000000,
        AUDCLNT_SESSIONFLAGS_DISPLAY_HIDEWHENEXPIRED = 0x40000000
    }
}

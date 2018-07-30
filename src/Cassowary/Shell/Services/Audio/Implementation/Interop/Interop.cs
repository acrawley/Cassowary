using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Audio.Implementation.Interop
{
    internal static class Constants
    {
        internal const string CLSID_MMDeviceEnumerator = "BCDE0395-E52F-467C-8E3D-C4579291692E";

        internal const string IID_IMMDevice = "D666063F-1587-4E43-81F1-B948E807363F";
        internal const string IID_IMMDeviceCollection = "0BD7A1BE-7A1A-44DB-8397-CC5392387B5E";
        internal const string IID_IMMDeviceEnumerator = "A95664D2-9614-4F35-A746-DE8DB63617E6";
        internal const string IID_IMMNotificationClient = "7991EEC9-7E89-4D85-8390-6C703CEC60C0";
        internal const string IID_IPropertyStore = "886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99";
        internal const string IID_IAudioClient = "1CB9AD4C-DBFA-4c32-B178-C2F568A703B2";
        internal const string IID_IAudioRenderClient = "F294ACFC-3146-4483-A7BF-ADDCA7C260E2";

        internal const UInt16 WAVE_FORMAT_PCM = 1;
        internal const UInt16 WAVE_FORMAT_EXTENSIBLE = 0xFFFE;

        internal static readonly Guid KSDATAFORMAT_SUBTYPE_PCM = new Guid("00000001-0000-0010-8000-00aa00389b71");
        internal static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71");
    }

    internal static class PKEY
    {
        internal static readonly PropertyKey PKEY_DeviceInterface_FriendlyName = new PropertyKey()
        {
            fmtid = new Guid(0x026e516e, 0xb814, 0x414b, 0x83, 0xcd, 0x85, 0x6d, 0x6f, 0xef, 0x48, 0x22),
            pid = 2
        };

        internal static readonly PropertyKey PKEY_Device_DeviceDesc = new PropertyKey()
        {
            fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
            pid = 2
        };

        internal static readonly PropertyKey PKEY_Device_FriendlyName = new PropertyKey()
        {
            fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
            pid = 14
        };
    }

    public enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    public enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Flags]
    internal enum DEVICE_STATE : UInt32
    {
        DEVICE_STATE_ACTIVE = 0x01,
        DEVICE_STATE_DISABLED = 0x02,
        DEVICE_STATE_NOTPRESENT = 0x04,
        DEVICE_STATE_UNPLUGGED = 0x08,
        DEVICE_STATEMASK_ALL = 0x0F
    }

    public enum STGM : UInt32
    {
        STGM_READ = 0x00,
        STGM_WRITE = 0x01,
        STGM_READWRITE = 0x02
    }

    public enum CLSCTX : UInt32
    {
        CLSCTX_INPROC_SERVER = 0x01,
        CLSCTX_INPROC_HANDLER = 0x02,
        CLSCTX_LOCAL_SERVER = 0x04,
        CLSCTX_REMOTE_SERVER = 0x10,
        CLSCTX_SERVER = CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER,
        CLSCTX_ALL = CLSCTX_SERVER | CLSCTX_INPROC_HANDLER
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WAVEFORMATEX
    {
        public UInt16 wFormatTag;
        public UInt16 nChannels;
        public UInt32 nSamplesPerSec;
        public UInt32 nAvgBytesPerSec;
        public UInt16 nBlockAlign;
        public UInt16 wBitsPerSample;
        public UInt16 cbSize;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct WAVEFORMATEXTENSIBLE
    {
        [FieldOffset(0)]
        public WAVEFORMATEX Format;

        [FieldOffset(18)]
        public UInt16 wValidBitsPerSample;

        [FieldOffset(18)]
        public UInt16 wSamplesPerBlock;

        [FieldOffset(18)]
        public UInt16 wReserved;

        [FieldOffset(20)]
        public UInt32 dwChannelMask;

        [FieldOffset(24)]
        public Guid SubFormat;
    }
}

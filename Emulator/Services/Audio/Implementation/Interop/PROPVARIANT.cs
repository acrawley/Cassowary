using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Emulator.Services.Audio.Implementation.Interop
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PROPVARIANT
    {
        [DllImport("ole32.dll", PreserveSig = false)]
        private static extern void PropVariantClear(ref PROPVARIANT pvar);

        #region Constructor

        public PROPVARIANT(object value)
            :this()
        {
            this.Value = value;
        }

        #endregion

        #region Private Fields

        [FieldOffset(0)]
        private ushort vt;

        [FieldOffset(8)]
        private IntPtr ptrValue;

        [FieldOffset(8)]
        private short boolValue;

        [FieldOffset(8)]
        private FILETIME fileTimeValue;

        [FieldOffset(8)]
        private byte byteValue;

        [FieldOffset(8)]
        private ushort ushortValue;

        [FieldOffset(8)]
        private uint uintValue;

        [FieldOffset(8)]
        private ulong ulongValue;

        [FieldOffset(8)]
        private sbyte sbyteValue;

        [FieldOffset(8)]
        private short shortValue;

        [FieldOffset(8)]
        private int intValue;

        [FieldOffset(8)]
        private long longValue;

        #endregion

        #region Private Properties

        private VarEnum VarType
        {
            get { return (VarEnum)this.vt; }
            set { this.vt = (ushort)value; }
        }

        #endregion

        #region Public Properties

        public object Value
        {
            get
            {
                switch (this.VarType)
                {
                    case VarEnum.VT_EMPTY:
                        return null;

                    case VarEnum.VT_BOOL:
                        return this.boolValue == (short)-1;

                    case VarEnum.VT_LPWSTR:
                        return Marshal.PtrToStringUni(this.ptrValue);

                    case VarEnum.VT_NULL:
                        return null;

                    case VarEnum.VT_BSTR:
                        return Marshal.PtrToStringBSTR(this.ptrValue);

                    case VarEnum.VT_UI1:
                        return this.byteValue;

                    case VarEnum.VT_UI2:
                        return this.ushortValue;

                    case VarEnum.VT_UI4:
                        return this.uintValue;

                    case VarEnum.VT_UI8:
                        return this.ulongValue;

                    case VarEnum.VT_I1:
                        return this.sbyteValue;

                    case VarEnum.VT_I2:
                        return this.shortValue;

                    case VarEnum.VT_I4:
                        return this.intValue;

                    case VarEnum.VT_I8:
                        return this.longValue;

                    case VarEnum.VT_FILETIME:
                        return this.fileTimeValue;
                }

                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Invalid type '{0}'!", this.VarType));
            }
            set
            {
                this.Clear();
                TypeCode typeCode = Type.GetTypeCode(value.GetType());

                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        this.VarType = VarEnum.VT_BOOL;
                        this.boolValue = (bool)value ? (short)-1 : (short)0;
                        break;

                    case TypeCode.String:
                        this.VarType = VarEnum.VT_LPWSTR;
                        this.ptrValue = Marshal.StringToCoTaskMemUni((string)value);
                        break;

                    case TypeCode.Byte:
                        this.VarType = VarEnum.VT_UI1;
                        this.byteValue = (byte)value;
                        break;

                    case TypeCode.UInt16:
                        this.VarType = VarEnum.VT_UI2;
                        this.ushortValue = (ushort)value;
                        break;

                    case TypeCode.UInt32:
                        this.VarType = VarEnum.VT_UI4;
                        this.uintValue = (uint)value;
                        break;

                    case TypeCode.UInt64:
                        this.VarType = VarEnum.VT_UI8;
                        this.ulongValue = (ulong)value;
                        break;

                    case TypeCode.SByte:
                        this.VarType = VarEnum.VT_I1;
                        this.sbyteValue = (sbyte)value;
                        break;

                    case TypeCode.Int16:
                        this.VarType = VarEnum.VT_I2;
                        this.shortValue = (short)value;
                        break;

                    case TypeCode.Int32:
                        this.VarType = VarEnum.VT_I4;
                        this.intValue = (int)value;
                        break;

                    case TypeCode.Int64:
                        this.VarType = VarEnum.VT_I8;
                        this.longValue = (long)value;
                        break;

                    default:
                        throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Invalid type '{0}'!", typeCode));
                }
            }
        }

        #endregion

        #region Public Methods

        public void Clear()
        {
            // Also frees any allocated unmanaged memory
            PROPVARIANT pv = this;
            PROPVARIANT.PropVariantClear(ref pv);
        }

        #endregion
    }
}

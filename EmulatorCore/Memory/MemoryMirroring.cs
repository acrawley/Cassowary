using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Memory;

namespace EmulatorCore.Memory
{
    internal class MemoryMirroring : IMemoryMirroring
    {
        #region Constructor

        internal MemoryMirroring(int sourceStartAddress, int sourceSize, int mirrorStartAddress, int mirrorEndAddress)
        {
            this.SourceStartAddress = sourceStartAddress;
            this.SourceSize = sourceSize;
            this.MirrorStartAddress = mirrorStartAddress;
            this.MirrorEndAddress = mirrorEndAddress;
        }

        #endregion

        #region Properties

        internal int SourceStartAddress;
        internal int SourceSize;
        internal int MirrorStartAddress;
        internal int MirrorEndAddress;

        #endregion

        public override string ToString()
        {
            return String.Format("0x{0:X8} - 0x{1:X8} => 0x{2:X8} - 0x{3:X8}",
                this.SourceStartAddress,
                this.SourceStartAddress + this.SourceSize,
                this.MirrorStartAddress,
                this.MirrorEndAddress);
        }

        #region IMemoryMirroring Implementation

        int IMemoryMirroring.SourceStartAddress
        {
            get { return this.SourceStartAddress; }
        }

        int IMemoryMirroring.SourceSize
        {
            get { return this.SourceSize; }
        }

        int IMemoryMirroring.MirrorStartAddress
        {
            get { return this.MirrorStartAddress; }
        }

        int IMemoryMirroring.MirrorEndAddress
        {
            get { return this.MirrorEndAddress; }
        }

        #endregion
    }
}

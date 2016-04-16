using System;
using EmulatorCore.Components.Memory;

namespace EmulatorCore.Memory
{
    internal class MemoryMapping : IMemoryMapping
    {
        #region Constructor

        internal MemoryMapping(IMemoryMappedDevice device, int startAddress, int endAddress)
        {
            this.Device = device;
            this.StartAddress = startAddress;
            this.EndAddress = endAddress;
        }

        #endregion

        #region Internal Fields

        internal IMemoryMappedDevice Device;
        internal int StartAddress;
        internal int EndAddress;

        #endregion

        #region IMemoryMapping Implementation

        IMemoryMappedDevice IMemoryMapping.Device
        {
            get { return this.Device; }
        }

        int IMemoryMapping.StartAddress
        {
            get { return this.StartAddress; }
        }

        int IMemoryMapping.EndAddress
        {
            get { return this.EndAddress; }
        }

        #endregion
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using Cassowary.Shared.Components.Core;
using Cassowary.Shared.Components.Memory;

namespace Cassowary.Shared.Memory
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

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "0x{0:X8} - 0x{1:X8} => {2}",
                this.StartAddress,
                this.EndAddress,
                (this.Device is IEmulatorComponent) ? ((IEmulatorComponent)this.Device).Name : this.Device.GetType().Name);
        }

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

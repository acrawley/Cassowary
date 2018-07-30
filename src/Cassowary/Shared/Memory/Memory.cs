using System;
using Cassowary.Shared.Components;
using Cassowary.Shared.Components.Core;
using Cassowary.Shared.Components.Memory;

namespace Cassowary.Shared.Memory
{
    public class Memory : IEmulatorComponent, IMemoryMappedDevice
    {
        #region Private Fields

        private byte[] contents;
        private int startAddress;
        private int size;
        private IMemoryMapping mapping;
        private IMemoryBus bus;

        #endregion

        #region Constructor

        public Memory(string name, IMemoryBus bus, int startAddress, int size)
        {
            this.Name = name;
            this.contents = new byte[size];
            this.bus = bus;

            this.startAddress = startAddress;
            this.size = size;

            this.mapping = bus.RegisterMappedDevice(this, startAddress, startAddress + (size - 1));
        }

        public void Remap(int newStartAddress)
        {
            if (this.startAddress != newStartAddress)
            {
                this.startAddress = newStartAddress;

                this.bus.RemoveMapping(this.mapping);

                if (newStartAddress != -1)
                {
                    this.mapping = this.bus.RegisterMappedDevice(this, this.startAddress, this.startAddress + (this.size - 1));
                }
            }
        }

        #endregion

        #region IEmulatorComponent Implementation

        public string Name { get; private set; }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            return this.contents[address - this.startAddress];
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            this.contents[address - this.startAddress] = value;
        }

        #endregion
    }
}

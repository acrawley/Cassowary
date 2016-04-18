using System;
using EmulatorCore.Components;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.Memory;

namespace EmulatorCore.Memory
{
    public class Memory : IEmulatorComponent, IMemoryMappedDevice
    {
        #region Private Fields

        private byte[] contents;
        int startAddress;

        #endregion

        #region Constructor

        public Memory(string name, IMemoryBus bus, int startAddress, int size)
        {
            this.Name = name;
            this.contents = new byte[size];
            this.startAddress = startAddress;

            bus.RegisterMappedDevice(this, startAddress, startAddress + (size - 1));
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

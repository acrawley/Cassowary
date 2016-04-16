using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components;
using EmulatorCore.Components.Memory;

namespace NesEmulator.PPU
{
    internal class PaletteMemory : IEmulatorComponent, IMemoryMappedDevice
    {
        #region Private Fields

        private byte[] contents;

        #endregion

        #region Constructor

        public PaletteMemory(IMemoryBus ppuBus)
        {
            ppuBus.RegisterMappedDevice(this, 0x3F00, 0x3F1F);
            this.contents = new byte[0x20];
        }

        #endregion

        #region IEmulatorComponent Implementation

        public string Name
        {
            get { return "Palette Memory"; }
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            address -= 0x3F00;

            // Background color (lowest byte) is the same for all palettes
            if ((address & 0x03) == 0)
            {
                return this.contents[0];
            }

            return this.contents[address];
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            address -= 0x3F00;

            if ((address & 0x03) == 0)
            {
                this.contents[0] = value;
            }

            this.contents[address] = value;
        }

        #endregion
    }
}

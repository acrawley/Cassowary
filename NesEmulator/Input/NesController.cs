using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components;
using EmulatorCore.Components.Memory;

namespace NesEmulator.Input
{
    internal class NesControlPad : IEmulatorComponent, IMemoryMappedDevice
    {
        private bool reading;
        private bool[] controllerOneButtons;
        private bool[] controllerTwoButtons;

        private int controllerOneIndex;
        private int controllerTwoIndex;

        public NesControlPad(IMemoryBus cpuBus)
        {
            this.reading = false;
            this.controllerOneButtons = new bool[8];
            this.controllerTwoButtons = new bool[8];
            this.controllerOneIndex = 0;
            this.controllerTwoIndex = 0;

            cpuBus.RegisterMappedDevice(this, 0x4016, 0x4017);
        }

        private byte ReadController(bool[] controllerData, ref int dataIndex)
        {
            // Top three bits return whatever's on the bus, usually the MSB of the port address (0x40)
            if (dataIndex < 8)
            {
                byte value = (byte)(controllerData[dataIndex] ? 0x41 : 0x40);
                dataIndex++;
                return value;
            }

            return 0x41;
        }

        #region IEmulatorComponent Implementation

        public string Name
        {
            get { return "NES Controller"; }
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            if (address == 0x4016)
            {
                return this.ReadController(controllerOneButtons, ref controllerOneIndex);
            }

            return this.ReadController(controllerTwoButtons, ref controllerTwoIndex);
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            bool strobe = ((value & 0x01) == 0x01);

            if (this.reading && !strobe)
            {
                // TODO: Update stored state

                // Order: A, B, Select, Start, Up, Down, Left, Right

                this.controllerOneIndex = 0;
                this.controllerTwoIndex = 0;
            }

            this.reading = strobe;
        }

        #endregion
    }
}

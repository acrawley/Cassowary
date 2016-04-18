using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.Input;
using EmulatorCore.Components.Memory;
using NesEmulator.Input.Devices;

namespace NesEmulator.Input
{
    internal class InputManager : IEmulatorComponent, IMemoryMappedDevice
    {
        #region Constants

        private const int JOY1_ADDR = 0x4016;
        private const int JOY2_ADDR = 0x4017;

        #endregion

        #region MEF Imports

        [ImportMany(typeof(INesInputDeviceFactory))]
        private IEnumerable<Lazy<INesInputDeviceFactory, INesInputDeviceFactoryMetadata>> DeviceFactories { get; set; }

        [Import(typeof(IInputReader))]
        private IInputReader InputReader { get; set; }

        #endregion

        #region Private Fields

        private INesInputDevice[] controllers;

        #endregion

        #region Constructor

        public InputManager(IMemoryBus cpuBus)
        {
            cpuBus.RegisterMappedDevice(this, InputManager.JOY1_ADDR, InputManager.JOY2_ADDR);
            this.controllers = new INesInputDevice[4] {
                new NesControlPad(),
                new NesControlPad(),
                null,
                null
            };
        }

        #endregion

        #region IEmulatorComponent Implementation

        public string Name
        {
            get { return "NES Input Manager"; }
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            bool done;
            if (address == InputManager.JOY1_ADDR)
            {
                return (this.controllers[0] != null) ? this.controllers[0].Read(out done) : (byte)0x00;
            }

            return (this.controllers[1] != null) ? this.controllers[1].Read(out done) : (byte)0x00;

            // TODO: Four Score / Satellite
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            if (address == InputManager.JOY1_ADDR && ((value & 0x01) == 0x01))
            {
                // Poll and strobe all attached controllers
                for (int i = 0; i < 4; i++)
                {
                    if (this.controllers[i] != null)
                    {
                        this.InputReader.Poll(i, this.controllers[i].InputElements);
                        this.controllers[i].Strobe();
                    }
                }
            }
        }

        #endregion
    }
}

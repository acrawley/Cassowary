using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components;
using Cassowary.Shared.Components.Core;
using Cassowary.Shared.Components.Input;

namespace Cassowary.Core.Nes.Input.Devices.Implementation
{
    [Export(typeof(INesInputDeviceFactory))]
    internal class NesControlPadFactory : INesInputDeviceFactory
    {
        #region INesInputDeviceFactory Implementation

        INesInputDevice INesInputDeviceFactory.CreateInstance()
        {
            return new NesControlPad();
        }

        #endregion
    }

    internal class NesControlPad : INesInputDevice
    {
        #region Constants

        private const int BUTTON_A = 0;
        private const int BUTTON_B = 1;
        private const int BUTTON_SELECT = 2;
        private const int BUTTON_START = 3;
        private const int BUTTON_UP = 4;
        private const int BUTTON_DOWN = 5;
        private const int BUTTON_LEFT = 6;
        private const int BUTTON_RIGHT = 7;

        #endregion

        #region Private Fields

        private bool[] buttons;
        private int currentButton;
        private IEnumerable<IInputElement> inputElements;

        #endregion

        #region Constructor

        internal NesControlPad()
        {
            this.buttons = new bool[8];
            this.currentButton = 0;

            this.inputElements = new ReadOnlyCollection<IInputElement>(
                new IInputElement[] {
                  new ButtonInputElement("A", "BTN_A", () => this.buttons[BUTTON_A], (s) => this.buttons[BUTTON_A] = s),
                  new ButtonInputElement("B", "BTN_B", () => this.buttons[BUTTON_B], (s) => this.buttons[BUTTON_B] = s),
                  new ButtonInputElement("Select", "BTN_SELECT", () => this.buttons[BUTTON_SELECT], (s) => this.buttons[BUTTON_SELECT] = s),
                  new ButtonInputElement("Start", "BTN_START", () => this.buttons[BUTTON_START], (s) => this.buttons[BUTTON_START] = s),
                  new ButtonInputElement("D-Pad Up", "BTN_UP", () => this.buttons[BUTTON_UP], (s) => this.buttons[BUTTON_UP] = s),
                  new ButtonInputElement("D-Pad Down", "BTN_DOWN", () => this.buttons[BUTTON_DOWN], (s) => this.buttons[BUTTON_DOWN] = s),
                  new ButtonInputElement("D-Pad Left", "BTN_LEFT", () => this.buttons[BUTTON_LEFT], (s) => this.buttons[BUTTON_LEFT] = s),
                  new ButtonInputElement("D-Pad Right", "BTN_RIGHT", () => this.buttons[BUTTON_RIGHT], (s) => this.buttons[BUTTON_RIGHT] = s)
            });
        }

        #endregion

        #region INesInputDevice Implementation

        byte INesInputDevice.Read(out bool done)
        {
            done = true;

            // Top three bits return whatever's on the bus, usually the MSB of the port address (0x40)
            if (currentButton < 8)
            {
                byte value = (byte)(buttons[currentButton] ? 0x41 : 0x40);

                currentButton++;
                if (currentButton != 8)
                {
                    done = false;
                }

                return value;
            }

            return 0x41;
        }

        void INesInputDevice.Strobe()
        {
            this.currentButton = 0;
        }

        #endregion

        #region IInputDevice Implementation

        IEnumerable<IInputElement> IInputDevice.InputElements
        {
            get
            {
                return this.inputElements;
            }
        }

        #endregion

        #region IEmulatorComponent Implementation

        string IEmulatorComponent.Name
        {
            get
            {
                return "NES Control Pad";
            }
        }

        #endregion
    }
}

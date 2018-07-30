using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Cassowary.Services.Configuration;
using Cassowary.Services.Input.Implementation.Configuration;
using Cassowary.Shared.Components.Input;

namespace Cassowary.Services.Input.Implementation
{
    [Export(typeof(IInputReader))]
    [Export(typeof(IConfigurationSection))]
    internal class InputService : IInputReader, IConfigurationSection
    {
        #region MEF Imports

        [Import(typeof(IKeyboardReader))]
        private IKeyboardReader KeyboardReader { get; set; }

        [Import(typeof(IXInputReader))]
        private IXInputReader XInputReader { get; set; }

        #endregion

        #region Private Fields

        InputConfiguration inputConfig;

        #endregion

        #region IConfigurationSection Implementation

        Type IConfigurationSection.SectionType
        {
            get { return typeof(InputConfiguration); }
        }

        object IConfigurationSection.Section
        {
            get { return this.inputConfig; }
            set { this.inputConfig = (InputConfiguration)value; }
        }

        #endregion

        private void UpdateButtonState(IButtonInputElement deviceElement, IEnumerable<ElementMapping> mappings)
        {
            bool pressed = false;

            foreach (ElementMapping mapping in mappings)
            {
                if (mapping is KeyboardMapping)
                {
                    pressed = this.KeyboardReader.GetKeyState(((KeyboardMapping)mapping).Key);
                }
                else if (mapping is XInputMapping)
                {
                    XInputMapping xInputMapping = (XInputMapping)mapping;
                    pressed = this.XInputReader.GetButtonState(xInputMapping.Controller, xInputMapping.Element);
                }

                if (pressed)
                {
                    break;
                }
            }

            deviceElement.State = pressed;
        }

        #region IInputReader Implementation

        void IInputReader.Poll(int controllerIndex, IInputDevice device)
        {
            Controller controller = this.inputConfig.Controllers
                .FirstOrDefault(c => c.Index == controllerIndex 
                                     && c.Connected 
                                     && String.Equals(c.Type, device.Name, StringComparison.Ordinal));

            if (controller != null)
            {
                foreach (IInputElement deviceElement in device.InputElements)
                {
                    ControllerElement configElement = controller.Elements.FirstOrDefault(e => String.Equals(e.Id, deviceElement.Id, StringComparison.Ordinal));
                    if (configElement != null)
                    {
                        if (deviceElement is IButtonInputElement)
                        {
                            this.UpdateButtonState((IButtonInputElement)deviceElement, configElement.Mappings);
                        }
                    }
                }
            }
        }

        #endregion
    }
}

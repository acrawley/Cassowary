using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using EmulatorCore.Components.Input;

namespace Emulator.Services.Input.Implementation
{
    [Export(typeof(IInputService))]
    [Export(typeof(IInputReader))]
    internal class InputService : IInputService, IInputReader
    {
        #region Private Fields

        private List<Key> activeKeys;
        private Dictionary<string, Key> mappings;

        #endregion

        #region Constructor

        internal InputService()
        {
            this.activeKeys = new List<Key>();

            // TODO: Configurable mappings
            this.mappings = new Dictionary<string, Key>()
            {
                { "BTN_A", Key.Z },
                { "BTN_B", Key.X },
                { "BTN_SELECT", Key.A },
                { "BTN_START", Key.S },
                { "BTN_UP", Key.Up },
                { "BTN_DOWN", Key.Down },
                { "BTN_LEFT", Key.Left },
                { "BTN_RIGHT", Key.Right }
            };
        }

        #endregion

        #region IInputReader Implementation

        void IInputReader.Poll(int controllerIndex, IEnumerable<IInputElement> elements)
        {
            if (controllerIndex == 0)
            {
                lock(this.activeKeys)
                {
                    foreach (IButtonInputElement element in elements.OfType<IButtonInputElement>())
                    {
                        element.State = this.activeKeys.Contains(this.mappings[element.Id]);
                    }
                }
            }
        }

        #endregion

        #region IInputService Implementation

        void IInputService.SetKeyDown(Key key)
        {
            lock (this.activeKeys)
            {
                if (!this.activeKeys.Contains(key))
                {
                    this.activeKeys.Add(key);
                }
            }
        }

        void IInputService.SetKeyUp(Key key)
        {
            lock(this.activeKeys)
            {
                this.activeKeys.Remove(key);
            }
        }

        #endregion
    }
}

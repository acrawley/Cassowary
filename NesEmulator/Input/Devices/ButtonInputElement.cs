using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components;
using EmulatorCore.Components.Input;

namespace NesEmulator.Input.Devices
{
    [DebuggerDisplay("Button '{Name,nq}', Pressed = {State}")]
    internal class ButtonInputElement : IButtonInputElement
    {
        #region Private Fields

        Func<bool> getStateFunc;
        Action<bool> setStateFunc;

        #endregion

        #region Constructor

        internal ButtonInputElement(string name, string id, Func<bool> getStateFunc, Action<bool> setStateFunc)
        {
            this.Name = name;
            this.Id = id;
            this.getStateFunc = getStateFunc;
            this.setStateFunc = setStateFunc;
        }

        #endregion

        #region IInputElement Implementation

        public string Name { get; private set; }
        public string Id { get; private set; }

        #endregion

        #region IButtonInputElement Implementation

        public bool State
        {
            get { return this.getStateFunc(); }
            set { this.setStateFunc(value); }
        }

        #endregion
    }
}

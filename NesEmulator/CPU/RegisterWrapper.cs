using System;
using System.Diagnostics;
using System.Globalization;
using EmulatorCore.Components.CPU;

namespace NesEmulator.CPU
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class RegisterWrapper : IProcessorRegister
    {
        #region Private Fields

        private Func<int> getValueFunc;
        private Action<int> setValueFunc;
        private Func<string> getFormattedValueFunc;

        #endregion

        #region Constructor

        internal RegisterWrapper(string name, string description, int width, Func<int> getValue, Action<int> setValue)
            :this(name, description, width, getValue, setValue, null)
        {
        }

        internal RegisterWrapper(string name, string description, int width, Func<int> getValue, Action<int> setValue, Func<string> getFormattedValueFunc)
        {
            this.Name = name;
            this.Description = description;
            this.Width = width;
            this.getValueFunc = getValue;
            this.setValueFunc = setValue;
            this.getFormattedValueFunc = getFormattedValueFunc;
        }

        #endregion

        #region Private Properties

        private string DebuggerDisplay
        {
            get
            {
                // Generate a format string that will use the correct width for the register
                string formatFormat = String.Format(CultureInfo.InvariantCulture, "{{0}} = 0x{{1:X{0}}}", Math.Ceiling(this.Width / 4f));
                return String.Format(CultureInfo.CurrentCulture, formatFormat, this.Name, this.Value);
            }
        }

        #endregion

        #region IProcessorRegister Implementation

        public string Description { get; private set; }

        public string Name { get; private set; }

        public int Value
        {
            get { return this.getValueFunc(); }
            set { this.setValueFunc(value); }
        }

        public string FormattedValue
        {
            get
            {
                if (this.getFormattedValueFunc != null)
                {
                    return this.getFormattedValueFunc();
                }

                return this.Value.ToString();
            }
        }

        public int Width { get; private set; }

        #endregion
    }
}

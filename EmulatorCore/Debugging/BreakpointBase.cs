using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Debugging;

namespace EmulatorCore.Debugging
{
    public class BreakpointBase : IBreakpoint, INotifyPropertyChanged
    {
        #region Constructor

        protected BreakpointBase()
        {
        }

        #endregion

        #region IBreakpoint Implementation

        private bool _enabled;
        public bool Enabled
        {
            get { return this._enabled; }
            set
            {
                if (this._enabled != value)
                {
                    this._enabled = value;
                    this.OnPropertyChanged(nameof(Enabled));
                }
            }
        }

        public event EventHandler BreakpointHit;

        internal void OnBreakpointHit()
        {
            this.BreakpointHit?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}

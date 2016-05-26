using System;
using System.ComponentModel;

namespace EmulatorCore.Components.Debugging
{
    public interface IBreakpoint : INotifyPropertyChanged
    {
        bool Enabled { get; set; }

        event EventHandler BreakpointHit;
    }
}

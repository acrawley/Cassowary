using System;
using System.ComponentModel;

namespace Cassowary.Shared.Components.Debugging
{
    public interface IBreakpoint : INotifyPropertyChanged
    {
        bool Enabled { get; set; }

        event EventHandler BreakpointHit;
    }
}

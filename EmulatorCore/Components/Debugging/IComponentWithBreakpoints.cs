using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Core;

namespace EmulatorCore.Components.Debugging
{
    public interface IComponentWithBreakpoints : IEmulatorComponent
    {
        IEnumerable<string> SupportedBreakpointTypes { get; }
        IBreakpoint CreateBreakpoint(string type);
        void DeleteBreakpoint(IBreakpoint breakpoint);
    }
}

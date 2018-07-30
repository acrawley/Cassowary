using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Core;

namespace Cassowary.Shared.Components.Debugging
{
    public interface IComponentWithBreakpoints : IEmulatorComponent
    {
        IEnumerable<string> SupportedBreakpointTypes { get; }
        IBreakpoint CreateBreakpoint(string type);
        void DeleteBreakpoint(IBreakpoint breakpoint);
    }
}

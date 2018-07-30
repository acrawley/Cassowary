using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Memory;

namespace Cassowary.Shared.Components.Debugging
{
    public interface IMemoryBreakpoint : IBreakpoint
    {
        IMemoryBus Bus { get; }
        int TargetAddress { get; set; }
        AccessType AccessType { get; set; }
    }

    [Flags]
    public enum AccessType
    {
        Unknown = 0,
        Read = 1,
        Write = 2
    }
}

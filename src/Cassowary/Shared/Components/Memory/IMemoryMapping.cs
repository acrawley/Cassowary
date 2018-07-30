using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Components.Memory
{
    public interface IMemoryMapping
    {
        IMemoryMappedDevice Device { get; }
        int StartAddress { get; }
        int EndAddress { get; }
    }
}

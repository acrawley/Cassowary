using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Components.Memory
{
    public interface IMemoryMirroring
    {
        int SourceStartAddress { get; }
        int SourceSize { get; }
        int MirrorStartAddress { get; }
        int MirrorEndAddress { get; }
    }
}

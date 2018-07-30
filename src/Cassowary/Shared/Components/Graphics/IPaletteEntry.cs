using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Components.Graphics
{
    public interface IPaletteEntry
    {
        int Index { get; }

        byte R { get; }
        byte G { get; }
        byte B { get; }
        byte A { get; }

        int BGR { get; }
    }
}

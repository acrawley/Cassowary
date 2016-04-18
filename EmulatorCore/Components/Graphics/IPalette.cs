using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.Graphics
{
    public interface IPalette
    {
        IEnumerable<IPaletteEntry> PaletteEntries { get; }

        string Name { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.Graphics
{
    public interface IPaletteFramebuffer
    {
        void SetPixel(int x, int y, int color);
        void Present();
    }
}

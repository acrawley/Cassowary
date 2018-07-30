using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Components.Graphics
{
    public interface IPaletteFramebuffer
    {
        void Initialize(int width, int height, int paletteSize);
        void SetPixel(int x, int y, int color);
        void Present();
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Graphics;

namespace NesTests.Fakes
{
    [Export(typeof(IPaletteFramebuffer))]
    internal class FakePaletteFramebuffer : IPaletteFramebuffer
    {
        void IPaletteFramebuffer.Initialize(int width, int height, int paletteSize)
        {
            
        }

        void IPaletteFramebuffer.Present()
        {
            
        }

        void IPaletteFramebuffer.SetPixel(int x, int y, int color)
        {
           
        }
    }
}

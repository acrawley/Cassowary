using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.Graphics
{
    public class PaletteEntry : IPaletteEntry
    {
        #region Constructors

        private PaletteEntry(int index, byte r, byte g, byte b, byte a)
        {
            this.Index = index;
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }

        public static PaletteEntry FromRGB(int index, byte r, byte g, byte b)
        {
            return new PaletteEntry(index, r, g, b, 0xFF);
        }

        public static PaletteEntry FromRGBA(int index, byte r, byte g, byte b, byte a)
        {
            return new PaletteEntry(index, r, g, b, a);
        }

        #endregion

        #region Public Properties

        public int Index { get; private set; }

        public byte R { get; private set; }

        public byte G { get; private set; }

        public byte B { get; private set; }

        public byte A { get; private set; }

        public int BGR
        {
            get
            {
                return (this.R << 16) |
                       (this.G << 8) |
                       (this.B);
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Graphics;

namespace NesEmulator.PPU
{
    [Export(typeof(IPalette))]
    internal class DefaultPalette : IPalette
    {
        #region Private Fields

        private IPaletteEntry[] palette;

        #endregion

        #region IPalette Implementation

        public string Name
        {
            get { return "Default NES Palette"; }
        }

        public IEnumerable<IPaletteEntry> PaletteEntries
        {
            get
            {
                if (this.palette == null)
                {
                    // RGB Palette
                    this.palette = new IPaletteEntry[] {
                        PaletteEntry.FromRGB(0, 0x75, 0x75, 0x75),
                        PaletteEntry.FromRGB(1, 0x27, 0x1B, 0x8F),
                        PaletteEntry.FromRGB(2, 0x00, 0x00, 0xAB),
                        PaletteEntry.FromRGB(3, 0x47, 0x00, 0x9F),
                        PaletteEntry.FromRGB(4, 0x8F, 0x00, 0x77),
                        PaletteEntry.FromRGB(5, 0xAB, 0x00, 0x13),
                        PaletteEntry.FromRGB(6, 0xA7, 0x00, 0x00),
                        PaletteEntry.FromRGB(7, 0x7F, 0x0B, 0x00),
                        PaletteEntry.FromRGB(8, 0x43, 0x2F, 0x00),
                        PaletteEntry.FromRGB(9, 0x00, 0x47, 0x00),
                        PaletteEntry.FromRGB(10, 0x00, 0x51, 0x00),
                        PaletteEntry.FromRGB(11, 0x00, 0x3F, 0x17),
                        PaletteEntry.FromRGB(12, 0x1B, 0x3F, 0x5F),
                        PaletteEntry.FromRGB(13, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(14, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(15, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(16, 0xBC, 0xBC, 0xBC),
                        PaletteEntry.FromRGB(17, 0x00, 0x73, 0xEF),
                        PaletteEntry.FromRGB(18, 0x23, 0x3B, 0xEF),
                        PaletteEntry.FromRGB(19, 0x83, 0x00, 0xF3),
                        PaletteEntry.FromRGB(20, 0xBF, 0x00, 0xBF),
                        PaletteEntry.FromRGB(21, 0xE7, 0x00, 0x5B),
                        PaletteEntry.FromRGB(22, 0xDB, 0x2B, 0x00),
                        PaletteEntry.FromRGB(23, 0xCB, 0x4F, 0x0F),
                        PaletteEntry.FromRGB(24, 0x8B, 0x73, 0x00),
                        PaletteEntry.FromRGB(25, 0x00, 0x97, 0x00),
                        PaletteEntry.FromRGB(26, 0x00, 0xAB, 0x00),
                        PaletteEntry.FromRGB(27, 0x00, 0x93, 0x3B),
                        PaletteEntry.FromRGB(28, 0x00, 0x83, 0x8B),
                        PaletteEntry.FromRGB(29, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(30, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(31, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(32, 0xFF, 0xFF, 0xFF),
                        PaletteEntry.FromRGB(33, 0x3F, 0xBF, 0xFF),
                        PaletteEntry.FromRGB(34, 0x5F, 0x97, 0xFF),
                        PaletteEntry.FromRGB(35, 0xA7, 0x8B, 0xFD),
                        PaletteEntry.FromRGB(36, 0xF7, 0x7B, 0xFF),
                        PaletteEntry.FromRGB(37, 0xFF, 0x77, 0xB7),
                        PaletteEntry.FromRGB(38, 0xFF, 0x77, 0x63),
                        PaletteEntry.FromRGB(39, 0xFF, 0x9B, 0x3B),
                        PaletteEntry.FromRGB(40, 0xF3, 0xBF, 0x3F),
                        PaletteEntry.FromRGB(41, 0x83, 0xD3, 0x13),
                        PaletteEntry.FromRGB(42, 0x4F, 0xDF, 0x4B),
                        PaletteEntry.FromRGB(43, 0x58, 0xF8, 0x98),
                        PaletteEntry.FromRGB(44, 0x00, 0xEB, 0xDB),
                        PaletteEntry.FromRGB(45, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(46, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(47, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(48, 0xFF, 0xFF, 0xFF),
                        PaletteEntry.FromRGB(49, 0xAB, 0xE7, 0xFF),
                        PaletteEntry.FromRGB(50, 0xC7, 0xD7, 0xFF),
                        PaletteEntry.FromRGB(51, 0xD7, 0xCB, 0xFF),
                        PaletteEntry.FromRGB(52, 0xFF, 0xC7, 0xFF),
                        PaletteEntry.FromRGB(53, 0xFF, 0xC7, 0xDB),
                        PaletteEntry.FromRGB(54, 0xFF, 0xBF, 0xB3),
                        PaletteEntry.FromRGB(55, 0xFF, 0xDB, 0xAB),
                        PaletteEntry.FromRGB(56, 0xFF, 0xE7, 0xA3),
                        PaletteEntry.FromRGB(57, 0xE3, 0xFF, 0xA3),
                        PaletteEntry.FromRGB(58, 0xAB, 0xF3, 0xBF),
                        PaletteEntry.FromRGB(59, 0xB3, 0xFF, 0xCF),
                        PaletteEntry.FromRGB(60, 0x9F, 0xFF, 0xF3),
                        PaletteEntry.FromRGB(61, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(62, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(63, 0x00, 0x00, 0x00)
                    };
                }

                return this.palette;
            }
        }

        #endregion
    }
}

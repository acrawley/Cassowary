using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Graphics;

namespace Cassowary.Core.Nes.PPU
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
                    // NTSC-ish Palette (generated from http://bisqwit.iki.fi/utils/nespalette.php)
                    this.palette = new IPaletteEntry[] {
                        PaletteEntry.FromRGB(0x00, 0x52, 0x52, 0x52),
                        PaletteEntry.FromRGB(0x01, 0x01, 0x1A, 0x51),
                        PaletteEntry.FromRGB(0x02, 0x0F, 0x0F, 0x65),
                        PaletteEntry.FromRGB(0x03, 0x23, 0x06, 0x63),
                        PaletteEntry.FromRGB(0x04, 0x36, 0x03, 0x4B),
                        PaletteEntry.FromRGB(0x05, 0x40, 0x04, 0x26),
                        PaletteEntry.FromRGB(0x06, 0x3F, 0x09, 0x04),
                        PaletteEntry.FromRGB(0x07, 0x32, 0x13, 0x00),
                        PaletteEntry.FromRGB(0x08, 0x1F, 0x20, 0x00),
                        PaletteEntry.FromRGB(0x09, 0x0B, 0x2A, 0x00),
                        PaletteEntry.FromRGB(0x0A, 0x00, 0x2F, 0x00),
                        PaletteEntry.FromRGB(0x0B, 0x00, 0x2E, 0x0A),
                        PaletteEntry.FromRGB(0x0C, 0x00, 0x26, 0x2D),
                        PaletteEntry.FromRGB(0x0D, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(0x0E, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(0x0F, 0x00, 0x00, 0x00),

                        PaletteEntry.FromRGB(0x10, 0xA0, 0xA0, 0xA0),
                        PaletteEntry.FromRGB(0x11, 0x1E, 0x4A, 0x9D),
                        PaletteEntry.FromRGB(0x12, 0x38, 0x37, 0xBC),
                        PaletteEntry.FromRGB(0x13, 0x58, 0x28, 0xB8),
                        PaletteEntry.FromRGB(0x14, 0x75, 0x21, 0x94),
                        PaletteEntry.FromRGB(0x15, 0x84, 0x25, 0x5C),
                        PaletteEntry.FromRGB(0x16, 0x82, 0x2E, 0x24),
                        PaletteEntry.FromRGB(0x17, 0x6F, 0x3F, 0x00),
                        PaletteEntry.FromRGB(0x18, 0x51, 0x52, 0x00),
                        PaletteEntry.FromRGB(0x19, 0x31, 0x63, 0x00),
                        PaletteEntry.FromRGB(0x1A, 0x1A, 0x6B, 0x05),
                        PaletteEntry.FromRGB(0x1B, 0x0E, 0x69, 0x2E),
                        PaletteEntry.FromRGB(0x1C, 0x10, 0x5C, 0x68),
                        PaletteEntry.FromRGB(0x1D, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(0x1E, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(0x1F, 0x00, 0x00, 0x00),

                        PaletteEntry.FromRGB(0x20, 0xFE, 0xFF, 0xFF),
                        PaletteEntry.FromRGB(0x21, 0x69, 0x9E, 0xFC),
                        PaletteEntry.FromRGB(0x22, 0x89, 0x87, 0xFF),
                        PaletteEntry.FromRGB(0x23, 0xAE, 0x76, 0xFF),
                        PaletteEntry.FromRGB(0x24, 0xCE, 0x6D, 0xF1),
                        PaletteEntry.FromRGB(0x25, 0xE0, 0x70, 0xB2),
                        PaletteEntry.FromRGB(0x26, 0xDE, 0x7C, 0x70),
                        PaletteEntry.FromRGB(0x27, 0xC8, 0x91, 0x3E),
                        PaletteEntry.FromRGB(0x28, 0xA6, 0xA7, 0x25),
                        PaletteEntry.FromRGB(0x29, 0x81, 0xBA, 0x28),
                        PaletteEntry.FromRGB(0x2A, 0x63, 0xC4, 0x46),
                        PaletteEntry.FromRGB(0x2B, 0x54, 0xC1, 0x7D),
                        PaletteEntry.FromRGB(0x2C, 0x56, 0xB3, 0xC0),
                        PaletteEntry.FromRGB(0x2D, 0x3C, 0x3C, 0x3C),
                        PaletteEntry.FromRGB(0x2E, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(0x2F, 0x00, 0x00, 0x00),

                        PaletteEntry.FromRGB(0x30, 0xFE, 0xFF, 0xFF),
                        PaletteEntry.FromRGB(0x31, 0xBE, 0xD6, 0xFD),
                        PaletteEntry.FromRGB(0x32, 0xCC, 0xCC, 0xFF),
                        PaletteEntry.FromRGB(0x33, 0xDD, 0xC4, 0xFF),
                        PaletteEntry.FromRGB(0x34, 0xEA, 0xC0, 0xF9),
                        PaletteEntry.FromRGB(0x35, 0xF2, 0xC9, 0xDF),
                        PaletteEntry.FromRGB(0x36, 0xF1, 0xC7, 0xC2),
                        PaletteEntry.FromRGB(0x37, 0xE8, 0xD0, 0xAA),
                        PaletteEntry.FromRGB(0x38, 0xD9, 0xDA, 0x9D),
                        PaletteEntry.FromRGB(0x39, 0xC9, 0xE2, 0x9B),
                        PaletteEntry.FromRGB(0x3A, 0xBC, 0xE6, 0xAE),
                        PaletteEntry.FromRGB(0x3B, 0xB4, 0xE5, 0xC7),
                        PaletteEntry.FromRGB(0x3C, 0xB5, 0xDF, 0xE4),
                        PaletteEntry.FromRGB(0x3D, 0xA9, 0xA9, 0xA9),
                        PaletteEntry.FromRGB(0x3E, 0x00, 0x00, 0x00),
                        PaletteEntry.FromRGB(0x3F, 0x00, 0x00, 0x00)
                    };
                }

                return this.palette;
            }
        }

        #endregion
    }
}

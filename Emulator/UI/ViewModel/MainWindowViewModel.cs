using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EmulatorCore.Components.Graphics;

namespace Emulator.UI.ViewModel
{
    internal class MainWindowViewModel : IPaletteFramebuffer
    {
        private byte[] pixelData;
        private Color[] palette;

        internal MainWindowViewModel()
        {
            this.Framebuffer = new WriteableBitmap(256, 240, 72, 72, PixelFormats.Bgr32, null);
            this.pixelData = new byte[256 * 240 * 4];

            this.palette = new Color[] {
                Color.FromRgb(0x75, 0x75, 0x75),
                Color.FromRgb(0x27, 0x1B, 0x8F),
                Color.FromRgb(0x00, 0x00, 0xAB),
                Color.FromRgb(0x47, 0x00, 0x9F),
                Color.FromRgb(0x8F, 0x00, 0x77),
                Color.FromRgb(0xAB, 0x00, 0x13),
                Color.FromRgb(0xA7, 0x00, 0x00),
                Color.FromRgb(0x7F, 0x0B, 0x00),
                Color.FromRgb(0x43, 0x2F, 0x00),
                Color.FromRgb(0x00, 0x47, 0x00),
                Color.FromRgb(0x00, 0x51, 0x00),
                Color.FromRgb(0x00, 0x3F, 0x17),
                Color.FromRgb(0x1B, 0x3F, 0x5F),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0xBC, 0xBC, 0xBC),
                Color.FromRgb(0x00, 0x73, 0xEF),
                Color.FromRgb(0x23, 0x3B, 0xEF),
                Color.FromRgb(0x83, 0x00, 0xF3),
                Color.FromRgb(0xBF, 0x00, 0xBF),
                Color.FromRgb(0xE7, 0x00, 0x5B),
                Color.FromRgb(0xDB, 0x2B, 0x00),
                Color.FromRgb(0xCB, 0x4F, 0x0F),
                Color.FromRgb(0x8B, 0x73, 0x00),
                Color.FromRgb(0x00, 0x97, 0x00),
                Color.FromRgb(0x00, 0xAB, 0x00),
                Color.FromRgb(0x00, 0x93, 0x3B),
                Color.FromRgb(0x00, 0x83, 0x8B),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0xFF, 0xFF, 0xFF),
                Color.FromRgb(0x3F, 0xBF, 0xFF),
                Color.FromRgb(0x5F, 0x97, 0xFF),
                Color.FromRgb(0xA7, 0x8B, 0xFD),
                Color.FromRgb(0xF7, 0x7B, 0xFF),
                Color.FromRgb(0xFF, 0x77, 0xB7),
                Color.FromRgb(0xFF, 0x77, 0x63),
                Color.FromRgb(0xFF, 0x9B, 0x3B),
                Color.FromRgb(0xF3, 0xBF, 0x3F),
                Color.FromRgb(0x83, 0xD3, 0x13),
                Color.FromRgb(0x4F, 0xDF, 0x4B),
                Color.FromRgb(0x58, 0xF8, 0x98),
                Color.FromRgb(0x00, 0xEB, 0xDB),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0xFF, 0xFF, 0xFF),
                Color.FromRgb(0xAB, 0xE7, 0xFF),
                Color.FromRgb(0xC7, 0xD7, 0xFF),
                Color.FromRgb(0xD7, 0xCB, 0xFF),
                Color.FromRgb(0xFF, 0xC7, 0xFF),
                Color.FromRgb(0xFF, 0xC7, 0xDB),
                Color.FromRgb(0xFF, 0xBF, 0xB3),
                Color.FromRgb(0xFF, 0xDB, 0xAB),
                Color.FromRgb(0xFF, 0xE7, 0xA3),
                Color.FromRgb(0xE3, 0xFF, 0xA3),
                Color.FromRgb(0xAB, 0xF3, 0xBF),
                Color.FromRgb(0xB3, 0xFF, 0xCF),
                Color.FromRgb(0x9F, 0xFF, 0xF3),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0x00)
            };
        }

        public WriteableBitmap Framebuffer { get; private set; }

        void IPaletteFramebuffer.SetPixel(int x, int y, int color)
        {
            Color value = this.palette[color];
            this.pixelData[(x * 4) + (y * 1024)] = value.B;
            this.pixelData[(x * 4) + (y * 1024) + 1] = value.G;
            this.pixelData[(x * 4) + (y * 1024) + 2] = value.R;
            this.pixelData[(x * 4) + (y * 1024) + 3] = 0xff;
        }

        void IPaletteFramebuffer.Present()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.Framebuffer.WritePixels(new Int32Rect(0, 0, 256, 240), this.pixelData, 1024, 0);
            });
        }
    }
}

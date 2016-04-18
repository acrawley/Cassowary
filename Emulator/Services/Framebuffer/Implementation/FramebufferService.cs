using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EmulatorCore.Components.Graphics;

namespace Emulator.Services.Framebuffer.Implementation
{
    [Export(typeof(IFramebufferService))]
    [Export(typeof(IPaletteFramebuffer))]
    internal class FramebufferService : IFramebufferService, IPaletteFramebuffer
    {
        #region Private Fields

        private WriteableBitmap bitmap;
        private int[] framebuffer;
        private int[] palette;
        private int width;
        private int height;

        #endregion

        #region MEF Imports

        [ImportMany(typeof(IPalette))]
        IEnumerable<IPalette> Palettes { get; set; }

        #endregion

        #region IFramebufferService Implementation

        public event EventHandler RenderTargetChanged;

        WriteableBitmap IFramebufferService.RenderTarget
        {
            get { return this.bitmap; }
        }

        #endregion

        #region IPaletteFramebuffer Implementation

        void IPaletteFramebuffer.Initialize(int width, int height, int paletteSize)
        {
            this.width = width;
            this.height = height;

            this.bitmap = new WriteableBitmap(width, height, 72, 72, PixelFormats.Bgr32, null);
            this.framebuffer = new int[width * height];

            this.palette = this.Palettes.First().PaletteEntries.Select(p => p.BGR).ToArray();

            if (this.RenderTargetChanged != null)
            {
                this.RenderTargetChanged(this, EventArgs.Empty);
            }
        }

        void IPaletteFramebuffer.SetPixel(int x, int y, int color)
        {
            this.framebuffer[x + (y * 256)] = this.palette[color];
        }

        void IPaletteFramebuffer.Present()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.bitmap.WritePixels(new Int32Rect(0, 0, this.width, this.height), this.framebuffer, this.width * 4, 0);
            });
        }

        #endregion
    }
}

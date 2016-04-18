using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Emulator.Services.Framebuffer
{
    internal interface IFramebufferService
    {
        WriteableBitmap RenderTarget { get; }

        event EventHandler RenderTargetChanged;
    }
}

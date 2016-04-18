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
using Emulator.Services.Framebuffer;
using EmulatorCore.Components.Graphics;
using EmulatorCore.Services.UI;

namespace Emulator.UI.ViewModel
{
    internal class MainWindowViewModel : ViewModelBase
    {
        [Import(typeof(IFramebufferService))]
        private IFramebufferService FramebufferService { get; set; }

        internal MainWindowViewModel()
        { 
        }

        public override void OnImportsSatisfied()
        {
            this.FramebufferService.RenderTargetChanged += this.OnFramebufferRenderTargetChanged;
        }

        private void OnFramebufferRenderTargetChanged(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(Framebuffer));
        }

        public WriteableBitmap Framebuffer
        {
            get { return this.FramebufferService.RenderTarget; }
        }
    }
}

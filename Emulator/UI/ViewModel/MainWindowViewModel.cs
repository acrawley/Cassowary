using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emulator.Services.Framebuffer;
using Emulator.Services.Input;
using Emulator.UI.Helpers;
using EmulatorCore.Components.Graphics;
using EmulatorCore.Services.UI;

namespace Emulator.UI.ViewModel
{
    internal class MainWindowViewModel : ViewModelBase
    {
        #region MEF Imports

        [Import(typeof(IFramebufferService))]
        private IFramebufferService FramebufferService { get; set; }

        [Import(typeof(IKeyboardReader))]
        private IKeyboardReader KeyboardReader { get; set; }

        #endregion

        #region Constructor / Initialization

        internal MainWindowViewModel()
        {
            this.KeyDownCommand = new UICommand<KeyEventArgs>(this.OnKeyDown);
            this.KeyUpCommand = new UICommand<KeyEventArgs>(this.OnKeyUp);
        }

        public override void OnImportsSatisfied()
        {
            this.FramebufferService.RenderTargetChanged += (sender, e) => this.OnPropertyChanged(nameof(Framebuffer));
        }

        #endregion

        #region Public Properties for Binding

        public WriteableBitmap Framebuffer
        {
            get { return this.FramebufferService.RenderTarget; }
        }

        public ICommand KeyDownCommand { get; private set; }
        public ICommand KeyUpCommand { get; private set; }

        #endregion

        #region Input

        private void OnKeyDown(KeyEventArgs args)
        {
            if (!args.IsRepeat)
            {
                this.KeyboardReader.NotifyKeyDown(args.Key);
            }
        }

        private void OnKeyUp(KeyEventArgs args)
        {
            this.KeyboardReader.NotifyKeyUp(args.Key);
        }

        #endregion
    }
}

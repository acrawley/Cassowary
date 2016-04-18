using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Emulator.UI.ViewModel;
using EmulatorCore.Components;
using EmulatorCore.Services.UI;

namespace Emulator
{
    internal class EmulatorApp
    {
        private Application application;

        #region MEF Imports

        [Import(typeof(IViewModelService))]
        private IViewModelService ViewModelService { get; set; }

        [ImportMany(typeof(IEmulatorFactory))]
        private IEnumerable<Lazy<IEmulatorFactory, IEmulatorFactoryMetadata>> EmulatorFactories { get; set; }

        #endregion

        internal int Run()
        {
            this.application = new Application();
            this.application.Startup += OnApplicationStartup;
            this.application.Exit += OnApplicationExit;
            return this.application.Run();
        }

        IEmulator emulator;

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            this.emulator.Stop();
        }

        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
            // Load and show the main window
            this.application.MainWindow = this.ViewModelService.CreateViewForViewModel<Window>(new MainWindowViewModel());
            this.application.MainWindow.Show();

            IEmulatorFactory factory = this.EmulatorFactories.SingleOrDefault(f => f.Metadata.EmulatorName == "NES").Value;
            this.emulator = factory.CreateInstance();

            // Start the emulator thread
            Task.Run(() => this.EmulatorThreadProc());
        }

        private void EmulatorThreadProc()
        {
            //emulator.LoadFile(@"Z:\public\ROMs\NES\World\Super Mario Bros. (W) [!].nes");
            emulator.LoadFile(@"Z:\public\ROMs\NES\World\Mario Bros. (W) [!].nes");
            //emulator.LoadFile(@"Z:\public\ROMs\NES\USA\Arkanoid (U) [!].nes");
            //emulator.LoadFile(@"Z:\public\ROMs\NES\World\Donkey Kong (W) (PRG1) [!].nes");
            //emulator.LoadFile(@"Z:\public\ROMs\NES\World\Duck Hunt (W) [!].nes");
            //emulator.LoadFile(@"Z:\public\ROMs\NES\tests\spritecans-2011\spritecans.nes");
            //emulator.LoadFile(@"Z:\public\ROMs\NES\USA\Balloon Fight (U) [!].nes.!ut");

            //emulator.LoadFile(@"z:\public\ROMs\NES\tests\nestest.nes");
            emulator.Run();
        }
    }
}

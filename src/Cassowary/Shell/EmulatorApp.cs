﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Cassowary.Services.Audio;
using Cassowary.Services.Configuration;
using Cassowary.UI.ViewModel;
using Cassowary.Shared.Components;
using Cassowary.Shared.Services.UI;

namespace Cassowary
{
    internal class EmulatorApp
    {
        private Application application;

        #region MEF Imports

        [Import(typeof(IViewModelService))]
        private IViewModelService ViewModelService { get; set; }

        [ImportMany(typeof(IEmulatorFactory))]
        private IEnumerable<Lazy<IEmulatorFactory, IEmulatorFactoryMetadata>> EmulatorFactories { get; set; }

        [Import(typeof(IConfigurationService))]
        private IConfigurationService ConfigurationService { get; set; }

        [Import(typeof(IAudioService))]
        private IAudioService AudioService { get; set; }

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
            this.ConfigurationService.Save();
            this.AudioService.Stop();
        }

        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
            // Load configuration
            this.ConfigurationService.ConfigurationFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.xml");
            this.ConfigurationService.Load();

            this.AudioService.Start();

            // Load and show the main window
            this.application.MainWindow = this.ViewModelService.CreateViewForViewModel<Window>(new MainWindowViewModel());
            this.application.MainWindow.Show();

            IEmulatorFactory factory = this.EmulatorFactories.SingleOrDefault(f => f.Metadata.EmulatorName == "NES").Value;
            this.emulator = factory.CreateInstance();

            string rom = null;
            if (e.Args.Length >= 1)
            {
                rom = e.Args[0];
            }

            // Start the emulator thread
            Task.Run(() => this.EmulatorThreadProc(rom));
        }

        private void EmulatorThreadProc(string rom)
        {
            if (!String.IsNullOrEmpty(rom))
            {
                emulator.LoadFile(rom);
            }
            else
            {
                //emulator.LoadFile(@"Z:\public\ROMs\NES\World\Super Mario Bros. (W) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\World\Mario Bros. (W) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\USA\Arkanoid (U) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\World\Donkey Kong (W) (PRG1) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\World\Duck Hunt (W) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\tests\spritecans-2011\spritecans.nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\USA\Balloon Fight (U) [!].nes");

                //emulator.LoadFile(@"Z:\public\ROMs\NES\USA\Legend of Zelda, The (U) (PRG1) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\Tests\holy_diver\testroms\M4_P256K_C256K.nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\USA\Super Mario Bros. 2 (U) (PRG1) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\USA\Super Mario Bros. 3 (U) (PRG1) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\Tests\mmc3_test\1-clocking.nes");
                emulator.LoadFile(@"Z:\public\ROMs\NES\USA\Kirby's Adventure (U) (PRG1) [!].nes");
                //emulator.LoadFile(@"Z:\public\ROMs\NES\Unlicensed\Hot Dance 2000 (Unl).nes");

                //emulator.LoadFile(@"Z:\public\ROMs\NES\Tests\apu_mixer\square.nes");
            }

            emulator.Run();
        }
    }
}

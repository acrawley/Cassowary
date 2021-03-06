﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Threading;
using Cassowary.Core.Nes.CPU;
using Cassowary.Core.Nes.PPU;
using Cassowary.Core.Nes.ROM;
using Cassowary.Shared;
using Cassowary.Shared.Components;
using Cassowary.Shared.Components.Core;
using Cassowary.Shared.Components.CPU;
using Cassowary.Shared.Components.Memory;
using Cassowary.Shared.Extensions;
using Cassowary.Shared.Memory;

namespace Cassowary.Core.Nes
{
    [Export(typeof(IEmulatorFactory))]
    [EmulatorName("NES")]
    internal class NesEmulatorFactory : IEmulatorFactory
    {
        [Import(typeof(CompositionContainer))]
        internal CompositionContainer container { get; private set; }

        IEmulator IEmulatorFactory.CreateInstance()
        {
            return new NesEmulator(container);
        }
    }

    internal class NesEmulator : EmulatorBase, IEmulator
    {
        private NesRomLoader loader;
        private Ricoh2A03 cpu;
        private IMemoryBus cpuBus;
        private IMemoryBus ppuBus;
        private Memory cpuRam;
        private Ricoh2C02 ppu;

        private bool run;

        internal NesEmulator(CompositionContainer container)
            : base(container)
        {
            // Create and connect parts
            this.cpuBus = new MemoryBus(16, "CPU Bus");
            this.ppuBus = new MemoryBus(16, "PPU Bus");

            this.cpuRam = new Memory("CPU RAM", this.cpuBus, 0x0000, 0x800);
            this.cpu = new Ricoh2A03(this.cpuBus);

            this.loader = new NesRomLoader(this.cpuBus, this.ppuBus, this.cpu.GetInterruptByName("IRQ"));

            this.ppu = new Ricoh2C02(this.cpu, this.cpuBus, this.ppuBus);

            // CPU RAM is mirrored between 0x0800 and 0x1FFF
            this.cpuBus.SetMirroringRange(0x0000, 0x07FF, 0x0800, 0x1FFF);

            // PPU registers on the CPU bus are mirrored between 0x2008 and 0x3FFFF
            this.cpuBus.SetMirroringRange(0x2000, 0x2007, 0x2008, 0x3FFF);

            // Nametable mirrors
            this.ppuBus.SetMirroringRange(0x2000, 0x2EFF, 0x3000, 0x3EFF);

            // Palette mirrors
            this.ppuBus.SetMirroringRange(0x3F00, 0x3F1F, 0x3F20, 0x3FFF);

            this.Compose();
        }

        void IEmulator.LoadFile(string fileName)
        {
            loader.LoadRom(fileName);
            ((IProcessorCore)this.cpu).Reset();
        }

        AutoResetEvent stopEvent = new AutoResetEvent(false);

        void IEmulator.Run()
        {
            int cycles;
            int cycleCount = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            this.run = true;

            IProcessorCore core = (IProcessorCore)this.cpu;
            IComponentWithClock ppuClock = (IComponentWithClock)this.ppu;
            IComponentWithClock apuClock = (IComponentWithClock)this.cpu.APU;

            while (this.run)
            {
                cycles = core.Step();
                if (cycles < 0)
                {
                    break;
                }

                for (int i = 0; i < cycles; i++)
                {
                    // PPU ticks 3 times during each CPU cycle
                    ppuClock.Tick();
                    ppuClock.Tick();
                    ppuClock.Tick();
                }

                for (int i = 0; i < cycles; i++)
                {
                    apuClock.Tick();
                }

                cycleCount += cycles;
                if (sw.ElapsedMilliseconds >= 1000)
                {
                    Trace.WriteLine(String.Format("Emulated clock speed: {0} MHz", cycleCount / (1000000f)));
                    cycleCount = 0;
                    sw.Restart();
                }
            }

            stopEvent.Set();
        }

        void IEmulator.Stop()
        {
            this.run = false;
            stopEvent.WaitOne();
        }

        public override IEnumerable<IEmulatorComponent> Components
        {
            get
            {
                return new IEmulatorComponent[]
                {
                    this.cpuBus,
                    this.ppuBus,
                    this.loader,
                    this.cpu,
                    this.ppu,
                    this.cpuRam,
                    this.cpu.InputManager,
                    this.cpu.APU,
                };
            }
        }
    }
}

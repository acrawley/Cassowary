using System;
using System.ComponentModel.Composition;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Memory;
using NesEmulator.ROM.Readers;

namespace NesEmulator.ROM.Mappers.Implementation
{
    [Export(typeof(IMapperFactory))]
    [MapperId(0)]
    internal class NROMFactory : IMapperFactory
    {
        IMapper IMapperFactory.CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
        {
            return new NROMMapper(reader, cpuBus, ppuBus);
        }
    }

    internal class NROMMapper : MapperBase, IMemoryMappedDevice
    {
        #region Private Fields

        private byte[] prgRom = new byte[0x8000];
        private byte[] chrRom = new byte[0x2000];

        #endregion

        #region Constructor

        internal NROMMapper(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus)
            : base(reader, cpuBus, ppuBus)
        {
            if (this.Reader.ChrRomSize != 0x2000 && this.Reader.ChrRomSize != 0x0000)
            {
                throw new InvalidOperationException("NROM expects 0 or 1 bank of CHR ROM!");
            }

            if (this.Reader.PrgRomSize != 0x4000 && this.Reader.PrgRomSize != 0x8000)
            {
                throw new InvalidOperationException("NROM expects 1 or 2 banks of PRG ROM!");
            }

            // 2 16KB banks of PRG ROM at CPU 0x8000 and 0xC000
            base.MapCpuRange(this, 0x8000, 0xFFFF);

            // 8K bank of CHR ROM at PPU 0x0000
            base.MapPpuRange(this, 0x0000, 0x1FFF);

            // Load data from ROM
            this.Reader.GetPrgRom(0, this.prgRom, 0, 0x4000);
            if (this.Reader.PrgRomSize == 0x4000)
            {
                this.Reader.GetPrgRom(0, this.prgRom, 0x4000, 0x4000);
            }
            else
            {
                this.Reader.GetPrgRom(0x4000, this.prgRom, 0x4000, 0x4000);
            }

            if (this.Reader.ChrRomSize > 0)
            {
                this.Reader.GetChrRom(0, this.chrRom, 0, 0x2000);
            }

            base.SetNametableMirroring(this.Reader.Mirroring);
        }

        #endregion

        #region MapperBase Implementation

        public override string Name
        {
            get { return "NROM Mapper"; }
        }

        protected override void Dispose()
        {
            base.DisposeMappings();
            base.DisposeMirrorings();
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                return this.prgRom[address - 0x8000];
            }
            else if (address >= 0x0000 && address <= 0x1FFF)
            {
                return this.chrRom[address];
            }

            throw new InvalidOperationException();
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            if (address >= 0x0000 && address <= 0x01FFF && this.Reader.ChrRomSize == 0)
            {
                // Cartridge has CHR RAM
                this.chrRom[address] = value;
            }
        }

        #endregion
    }
}

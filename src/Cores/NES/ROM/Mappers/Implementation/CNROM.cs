using System;
using System.ComponentModel.Composition;
using Cassowary.Shared.Components.CPU;
using Cassowary.Shared.Components.Memory;
using Cassowary.Core.Nes.ROM.Readers;

namespace Cassowary.Core.Nes.ROM.Mappers.Implementation
{
    [Export(typeof(IMapperFactory))]
    [MapperId(3)]
    internal class CNROMMapperFactory : IMapperFactory
    {
        IMapper IMapperFactory.CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
        {
            return new CNROMMapper(reader, cpuBus, ppuBus);
        }
    }

    internal class CNROMMapper : MapperBase, IMemoryMappedDevice
    {
        #region Constants

        private const int MaxChrSize = 2 * 1024 * 1024;
        private const int ChrRomBankSize = 8 * 1024;

        #endregion

        #region Private Fields

        private byte[] prgRom = new byte[0x8000];
        private byte[] chrRom;

        private UInt32 chrRomBankMask;

        private int chrRomBanks;

        #endregion

        #region Registers

        private void SetChrBankRegister(byte value)
        {
            this.chrRomBankMask = (UInt32)(value % this.chrRomBanks) << 13;
        }

        #endregion

        #region Constructor

        internal CNROMMapper(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus)
            : base(reader, cpuBus, ppuBus)
        {
            if (this.Reader.PrgRomSize != 16 * 1024 && this.Reader.PrgRomSize != 32 * 1024)
            {
                throw new ArgumentOutOfRangeException("CNROM expects 16 or 32KB of PRG ROM!");
            }

            if (this.Reader.ChrRomSize > CNROMMapper.MaxChrSize)
            {
                throw new ArgumentOutOfRangeException("CNROM supports a maximum of 2MB of CHR ROM!");
            }

            // 2 16KB banks of PRG ROM at CPU 0x8000 and 0xC000
            base.MapCpuRange(this, 0x8000, 0xFFFF);

            // 8K bank of CHR ROM at PPU 0x0000
            base.MapPpuRange(this, 0x0000, 0x1FFF);

            // Load data from ROM
            this.chrRom = new byte[this.Reader.ChrRomSize];
            this.chrRomBanks = this.Reader.ChrRomSize / CNROMMapper.ChrRomBankSize;

            this.Reader.GetChrRom(0, this.chrRom, 0, this.Reader.ChrRomSize);

            this.Reader.GetPrgRom(0, this.prgRom, 0, 0x4000);
            if (this.Reader.PrgRomSize == 0x4000)
            {
                this.Reader.GetPrgRom(0, this.prgRom, 0x4000, 0x4000);
            }
            else
            {
                this.Reader.GetPrgRom(0x4000, this.prgRom, 0x4000, 0x4000);
            }

            base.SetNametableMirroring(this.Reader.Mirroring);
        }

        #endregion

        #region MapperBase Implementation

        public override string Name
        {
            get { return "CNROM Mapper"; }
        }

        protected override void Dispose()
        {
            this.DisposeMappings();
            this.DisposeMirrorings();
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            if (address <= 0x1FFF)
            {
                return this.chrRom[this.chrRomBankMask | ((UInt32)address & 0x1FFF)];
            }
            else if (address >= 0x8000 && address <= 0xFFFF)
            {
                return this.prgRom[address - 0x8000];
            }

            throw new NotImplementedException();
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            if (address >= 8000 && address <= 0xFFFF)
            {
                this.SetChrBankRegister(value);
            }
        }

        #endregion
    }
}

using System;
using System.ComponentModel.Composition;
using Cassowary.Shared.Components.CPU;
using Cassowary.Shared.Components.Memory;
using Cassowary.Core.Nes.ROM.Readers;

namespace Cassowary.Core.Nes.ROM.Mappers.Implementation
{
    [Export(typeof(IMapperFactory))]
    [MapperId(2)]
    internal class UxROMMapperFactory : IMapperFactory
    {
        IMapper IMapperFactory.CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
        {
            return new UxROMMapper(reader, cpuBus, ppuBus);
        }
    }

    [Export(typeof(IMapperFactory))]
    [MapperId(71)]
    internal class Camerica71MapperFactory : IMapperFactory
    {
        IMapper IMapperFactory.CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
        {
            return new UxROMMapper(reader, cpuBus, ppuBus, UxRomVariant.Camerica71);
        }
    }

    internal enum UxRomVariant
    {
        UxROM,
        Camerica71
    }


    internal class UxROMMapper : MapperBase, IMemoryMappedDevice
    {
        #region Constants

        private const int MaxPrgSize = 4 * 1024 * 1024;
        private const int PrgRomBankSize = 16 * 1024;

        #endregion

        #region Private Fields

        private byte[] prgRom;
        private byte[] chrRom = new byte[0x2000];

        private UInt32 prgRomLoBankMask;
        private UInt32 prgRomHiBankMask;

        private int prgRomBanks;

        private UInt16 bankSwitchRegisterAddrRangeEnd;
        private UInt16 bankSwitchRegisterAddrRangeStart;

        #endregion

        #region Registers

        private void SetPrgBankRegister(byte value)
        {
            this.prgRomLoBankMask = (UInt32)((value % this.prgRomBanks) << 14);
        }

        #endregion

        #region Constructor

        internal UxROMMapper(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, UxRomVariant variant = UxRomVariant.UxROM) 
            : base(reader, cpuBus, ppuBus)
        {
            if (this.Reader.ChrRomSize != 0 && this.Reader.ChrRomSize != 8 * 1024)
            {
                throw new ArgumentOutOfRangeException("UxROM expects 0 or 8KB of CHR ROM!");
            }

            if (this.Reader.PrgRomSize > UxROMMapper.MaxPrgSize)
            {
                throw new ArgumentOutOfRangeException("UxROM supports a maximum of 4MB of PRG ROM!");
            }

            // 2 16KB banks of PRG ROM at CPU 0x8000 and 0xC000
            base.MapCpuRange(this, 0x8000, 0xFFFF);

            // 8K bank of CHR ROM at PPU 0x0000
            base.MapPpuRange(this, 0x0000, 0x1FFF);

            // Load data from ROM
            this.prgRom = new byte[this.Reader.PrgRomSize];
            this.prgRomBanks = this.Reader.PrgRomSize / UxROMMapper.PrgRomBankSize;

            if (this.Reader.ChrRomSize > 0)
            {
                this.Reader.GetChrRom(0, this.chrRom, 0, this.Reader.ChrRomSize);
            }

            this.Reader.GetPrgRom(0, this.prgRom, 0, this.Reader.PrgRomSize);

            base.SetNametableMirroring(this.Reader.Mirroring);

            // High bank of PRG ROM is always mapped at 0xC000
            this.prgRomHiBankMask = (UInt32)(this.prgRomBanks - 1) << 14;

            switch (variant)
            {
                case UxRomVariant.UxROM:
                    this.bankSwitchRegisterAddrRangeStart = 0x8000;
                    this.bankSwitchRegisterAddrRangeEnd = 0xFFFF;
                    break;

                case UxRomVariant.Camerica71:
                    this.bankSwitchRegisterAddrRangeStart = 0xC000;
                    this.bankSwitchRegisterAddrRangeEnd = 0xFFFF;
                    break;
            }
        }

        #endregion

        #region MapperBase Implementation

        public override string Name
        {
            get { return "UxROM Mapper"; }
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
            if (address >= 0 && address <= 0x1FFF)
            {
                return this.chrRom[address];
            }
            else if (address >= 0x8000 && address <= 0xBFFF)
            {
                return this.prgRom[this.prgRomLoBankMask | ((UInt32)address & 0x3FFF)];
            }
            else if (address >= 0xC000 && address <= 0xFFFF)
            {
                return this.prgRom[this.prgRomHiBankMask | ((UInt32)address & 0x3FFF)];
            }

            throw new InvalidOperationException();
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            if (address >= 0 && address <= 0x1FFF && this.Reader.ChrRomSize == 0)
            {
                // Write to CHR RAM
                this.chrRom[address] = value;
            }
            else if (address >= this.bankSwitchRegisterAddrRangeStart && address <= this.bankSwitchRegisterAddrRangeEnd)
            {
                this.SetPrgBankRegister(value);
            }
        }

        #endregion
    }
}

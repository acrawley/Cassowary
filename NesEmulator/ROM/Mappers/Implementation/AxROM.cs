using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Memory;
using NesEmulator.ROM.Readers;

namespace NesEmulator.ROM.Mappers.Implementation
{
    [Export(typeof(IMapperFactory))]
    [MapperId(7)]
    internal class AxROMMapperFactory : IMapperFactory
    {
        IMapper IMapperFactory.CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
        {
            return new AxROMMapper(reader, cpuBus, ppuBus);
        }
    }


    internal class AxROMMapper : MapperBase, IMemoryMappedDevice
    {
        #region Constants

        private const int MaxPrgSize = 256 * 1024;
        private const int PrgRomBankSize = 32 * 1024;

        #endregion

        #region Private Fields

        private byte[] prgRom;
        private byte[] chrRam = new byte[0x2000];

        private int prgRomBanks;

        private UInt32 prgRomBankMask;

        #endregion

        #region Registers

        private void SetBankSelectRegister(byte value)
        {
            this.prgRomBankMask = (UInt32)(((value & 0x07) % this.prgRomBanks) << 15);

            base.DisposeMirrorings();
            base.SetNametableMirroring(((value & 0x10) == 0x10) ? MirroringMode.SingleScreenB : MirroringMode.SingleScreenA);
        }

        #endregion

        #region Constructor

        public AxROMMapper(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus)
            : base(reader, cpuBus, ppuBus)
        {
            if (this.Reader.ChrRomSize != 0)
            {
                throw new ArgumentOutOfRangeException("AxROM expects CHR RAM!");
            }

            if (this.Reader.PrgRomSize > AxROMMapper.MaxPrgSize)
            {
                throw new ArgumentOutOfRangeException("AxROM supports a maximum of 256KB of PRG ROM!");
            }

            // 1 32KB bank of PRG ROM at CPU 0x8000
            base.MapCpuRange(this, 0x8000, 0xFFFF);

            // 8K bank of CHR ROM at PPU 0x0000
            base.MapPpuRange(this, 0x0000, 0x1FFF);

            // Load data from ROM
            this.prgRom = new byte[this.Reader.PrgRomSize];
            this.prgRomBanks = this.Reader.PrgRomSize / AxROMMapper.PrgRomBankSize;
            this.Reader.GetPrgRom(0, this.prgRom, 0, this.Reader.PrgRomSize);

            // First bank of PRG ROM is initially mapped at 0x8000
            this.prgRomBankMask = (UInt32)((((this.prgRomBanks - 1) & 0x07) % this.prgRomBanks) << 15); ;
        }

        #endregion

        #region MapperBase Implementation

        public override string Name
        {
            get { return "AxROM Mapper"; }
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
            if (address >= 0x0000 && address <= 0x1FFF)
            {
                return this.chrRam[address];
            }
            else if (address >= 0x8000 && address <= 0xFFFF)
            {
                return this.prgRom[this.prgRomBankMask | ((UInt32)address & 0x7FFF)];
            }

            throw new NotImplementedException();
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            if (address >= 0x0000 && address <= 0x1FFF)
            {
                this.chrRam[address] = value;
            }
            else if (address >= 0x8000 && address <= 0xFFFF)
            {
                this.SetBankSelectRegister(value);
            }
        }

        #endregion
    }
}

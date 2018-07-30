using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.CPU;
using Cassowary.Shared.Components.Memory;
using Cassowary.Core.Nes.ROM.Readers;

namespace Cassowary.Core.Nes.ROM.Mappers.Implementation
{
    [Export(typeof(IMapperFactory))]
    [MapperId(4)]
    internal class MMC3MapperFactory : IMapperFactory
    {
        IMapper IMapperFactory.CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
        {
            return new MMC3Mapper(reader, cpuBus, ppuBus, irq);
        }
    }

    internal class MMC3Mapper : MapperBase, IMemoryMappedDevice
    {
        #region Constants

        private const int PrgRomMaxSize = 512 * 1024;
        private const int PrgRamMaxSize = 8 * 1024;
        private const int ChrRomMaxSize = 256 * 1024;

        private const int PrgRomBankSize = 8 * 1024;
        private const int ChrRomBankSize = 1 * 1024;

        #endregion

        #region Private Fields

        private byte[] prgRom;
        private byte[] prgRam;
        private byte[] chrRom;

        private int prgRomBanks;
        private int chrRomBanks;

        private UInt32 prgRomBank0Mask;
        private UInt32 prgRomBank1Mask;
        private UInt32 prgRomBank2Mask;
        private UInt32 prgRomBank3Mask;

        private UInt32 chrRomBank0Mask;
        private UInt32 chrRomBank1Mask;
        private UInt32 chrRomBank2Mask;
        private UInt32 chrRomBank3Mask;
        private UInt32 chrRomBank4Mask;
        private UInt32 chrRomBank5Mask;
        private UInt32 chrRomBank6Mask;
        private UInt32 chrRomBank7Mask;

        private byte[] bankRegisters = new byte[8];

        private int scanlineCounter;
        private bool lastPpuA12;
        private IProcessorInterrupt irq;
        private IDisposable irqAssertion;

        #endregion

        #region Registers

        private enum BankRegister
        {
            ChrRom2KBank0 = 0,
            ChrRom2KBank1 = 1,
            ChrRom1KBank0 = 2,
            ChrRom1KBank1 = 3,
            ChrRom1KBank2 = 4,
            ChrRom1KBank3 = 5,
            PrgRomBank0 = 6,
            PrgRomBank1 = 7,
        }

        private enum PrgRomBankMode
        {
            HiBankFixed,
            LoBankFixed
        }

        private enum ChrRomBankMode
        {
            LoBanks2K,
            HiBanks2K
        }

        private BankRegister selectedBank;
        private PrgRomBankMode prgRomBankMode;
        private ChrRomBankMode chrRomBankMode;

        private void SetBankSelectRegister(byte value)
        {
            this.selectedBank = (BankRegister)(value & 0x07);

            // When set:   0x8000 - 0x9000 is swappable and 0xC000 - 0xDFFF is fixed to the second-to-last bank
            //  otherwise: 0x8000 - 0x9000 is fixed to the second to last bank and 0xC000 - 0xDFFF is swappable
            this.prgRomBankMode = ((value & 0x40) == 0x40) ? PrgRomBankMode.LoBankFixed : PrgRomBankMode.HiBankFixed;

            // When set  : 2 2K banks of CHR ROM at 0x1000 - 0x1FFF and 4 1K banks at 0x0000 - 0x1000
            //  otherwise: 4 1K banks of CHR ROM at 0x1000 - 0x1FFF and 2 2K banks at 0x0000 - 0x1000;
            this.chrRomBankMode = ((value & 0x80) == 0x80) ? ChrRomBankMode.HiBanks2K : ChrRomBankMode.LoBanks2K;

            this.RecalcuateBankMasks();
        }

        private bool chrRomBankInitialized;

        private void SetBankDataRegister(byte value)
        {
            if (!this.chrRomBankInitialized && 
                this.selectedBank != BankRegister.PrgRomBank0 && 
                this.selectedBank != BankRegister.PrgRomBank1)
            {
                this.chrRomBankInitialized = true;
            }

            this.bankRegisters[(int)this.selectedBank] = value;

            this.RecalcuateBankMasks();
        }

        private void RecalcuateBankMasks()
        {
            // PRG ROM banks
            UInt32 swappablePrgBankMask = (UInt32)(((this.bankRegisters[6] & 0x3F) % this.prgRomBanks) << 13);
            UInt32 fixedPrgBankMask = (UInt32)(this.prgRomBanks - 2) << 13;

            if (this.prgRomBankMode == PrgRomBankMode.HiBankFixed)
            {
                this.prgRomBank0Mask = swappablePrgBankMask;
                this.prgRomBank2Mask = fixedPrgBankMask;
            }
            else
            {
                this.prgRomBank0Mask = fixedPrgBankMask;
                this.prgRomBank2Mask = swappablePrgBankMask;
            }

            // PRG bank 1 is always swappable
            this.prgRomBank1Mask = (UInt32)(((this.bankRegisters[7] & 0x3F) % this.prgRomBanks) << 13);

            // CHR ROM banks
            // 1K banks
            UInt32 chrRom1KBank0Mask = (UInt32)(this.bankRegisters[2] % this.chrRomBanks) << 10;
            UInt32 chrRom1KBank1Mask = (UInt32)(this.bankRegisters[3] % this.chrRomBanks) << 10;
            UInt32 chrRom1KBank2Mask = (UInt32)(this.bankRegisters[4] % this.chrRomBanks) << 10;
            UInt32 chrRom1KBank3Mask = (UInt32)(this.bankRegisters[5] % this.chrRomBanks) << 10;

            // 2K banks
            UInt32 chrRom2KBank0LoMask = (UInt32)((this.bankRegisters[0] & 0xFE) % this.chrRomBanks) << 10;
            UInt32 chrRom2KBank0HiMask = (UInt32)((this.bankRegisters[0] | 0x01) % this.chrRomBanks) << 10;
            UInt32 chrRom2KBank1LoMask = (UInt32)((this.bankRegisters[1] & 0xFE) % this.chrRomBanks) << 10;
            UInt32 chrRom2KBank1HiMask = (UInt32)((this.bankRegisters[1] | 0x01) % this.chrRomBanks) << 10;

            if (this.chrRomBankMode == ChrRomBankMode.HiBanks2K)
            {
                this.chrRomBank0Mask = chrRom1KBank0Mask;
                this.chrRomBank1Mask = chrRom1KBank1Mask;
                this.chrRomBank2Mask = chrRom1KBank2Mask;
                this.chrRomBank3Mask = chrRom1KBank3Mask;

                this.chrRomBank4Mask = chrRom2KBank0LoMask;
                this.chrRomBank5Mask = chrRom2KBank0HiMask;
                this.chrRomBank6Mask = chrRom2KBank1LoMask;
                this.chrRomBank7Mask = chrRom2KBank1HiMask;
            }
            else
            {
                this.chrRomBank0Mask = chrRom2KBank0LoMask;
                this.chrRomBank1Mask = chrRom2KBank0HiMask;
                this.chrRomBank2Mask = chrRom2KBank1LoMask;
                this.chrRomBank3Mask = chrRom2KBank1HiMask;

                this.chrRomBank4Mask = chrRom1KBank0Mask;
                this.chrRomBank5Mask = chrRom1KBank1Mask;
                this.chrRomBank6Mask = chrRom1KBank2Mask;
                this.chrRomBank7Mask = chrRom1KBank3Mask;
            }
        }

        private void SetMirroringRegister(byte value)
        {
            base.DisposeMirrorings();
            if ((value & 0x01) == 0x01)
            {
                base.SetNametableMirroring(MirroringMode.Horizontal);
            }
            else
            {
                base.SetNametableMirroring(MirroringMode.Vertical);
            }
        }

        private void SetPrgRamRegister(byte value)
        {
            // Not implemented
        }

        private byte scanlineCounterReloadValue;

        private void SetIrqLatchRegister(byte value)
        {
            this.scanlineCounterReloadValue = value;
        }

        private bool reloadScanlineCounter;

        private void SetIrqReloadRegister(byte value)
        {
            this.reloadScanlineCounter = true;
        }

        bool irqEnabled;

        private void SetIrqDisableRegister(byte value)
        {
            this.irqEnabled = false;

            if (this.irqAssertion != null)
            {
                this.irqAssertion.Dispose();
                this.irqAssertion = null;
            }
        }

        private void SetIrqEnableRegister(byte value)
        {
            this.irqEnabled = true;
        }

        #endregion

        #region Constructor

        internal MMC3Mapper(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
            : base(reader, cpuBus, ppuBus)
        {
            if (this.Reader.PrgRamSize > MMC3Mapper.PrgRamMaxSize)
            {
                throw new ArgumentOutOfRangeException("MMC3 supports a maximum of 8KB of PRG RAM");
            }

            if (this.Reader.PrgRomSize > MMC3Mapper.PrgRomMaxSize)
            {
                throw new ArgumentOutOfRangeException("MMC3 supports a maximum of 512KB of PRG ROM");
            }

            if (this.Reader.ChrRomSize > MMC3Mapper.ChrRomMaxSize)
            {
                throw new ArgumentOutOfRangeException("MMC3 supports a maximum of 256KB of CHR ROM");
            }

            this.irq = irq;

            // Initialize ROM
            int prgRomSize = this.Reader.PrgRomSize;
            int prgRamSize = (this.Reader.PrgRamSize != 0) ? this.Reader.PrgRamSize : 0x2000;
            int chrRomSize = (this.Reader.ChrRomSize != 0) ? this.Reader.ChrRomSize : 0x2000;

            this.prgRomBanks = this.Reader.PrgRomSize / MMC3Mapper.PrgRomBankSize;
            this.chrRomBanks = (this.Reader.ChrRomSize != 0) ? (this.Reader.ChrRomSize / MMC3Mapper.ChrRomBankSize) : 8;

            this.prgRam = new byte[prgRamSize];
            this.prgRom = new byte[prgRomSize];
            this.chrRom = new byte[chrRomSize];

            // Read PRG ROM
            this.Reader.GetPrgRom(0, this.prgRom, 0, prgRomSize);

            if (this.Reader.ChrRomSize != 0)
            {
                // Read CHR ROM
                this.Reader.GetChrRom(0, this.chrRom, 0, chrRomSize);
            }

            // 8K bank of PRG RAM at CPU 0x6000
            this.MapCpuRange(this, 0x6000, 0x7FFF);

            // 2 16K banks of PRG ROM at CPU 0x8000 and 0xC000
            this.MapCpuRange(this, 0x8000, 0xFFFF);

            // 2 2K banks and 4 1K banks of CHR ROM between PPU 0x0000 and 0x1000
            this.MapPpuRange(this, 0x0000, 0x1FFF);

            // Initial state is undefined, except that the last bank of PRG ROM will be mapped from 0xE000 - 0xFFFF
            this.prgRomBank3Mask = (UInt32)(this.prgRomBanks - 1) << 13;

            // Set initial nametable mirroring from ROM data as a hack to fix some homebrew that doesn't properly
            //  initialize the mapper
            base.SetNametableMirroring(this.Reader.Mirroring);
        }

        #endregion

        #region Scanline Counter / IRQ

        private void HandlePpuMemoryAccess(int address)
        {
            if (!this.chrRomBankInitialized)
            {
                this.chrRomBankInitialized = true;
                Debug.WriteLine("MMC3: Warning - accessing PPU memory before CHR banks are initialized!  Assuming NROM defaults.");

                // Map first 8K of CHR ROM to fix some homebrew that assumes MMC3's default mapping looks like NROM.
                this.chrRomBank0Mask = 0x0000;
                this.chrRomBank1Mask = 0x0400;
                this.chrRomBank2Mask = 0x0800;
                this.chrRomBank3Mask = 0x0C00;
                this.chrRomBank4Mask = 0x1000;
                this.chrRomBank5Mask = 0x1400;
                this.chrRomBank6Mask = 0x1800;
                this.chrRomBank7Mask = 0x1C00;
            }

            bool currentPpuA12 = ((address & 0x1000) == 0x1000);

            if (!this.lastPpuA12 && currentPpuA12)
            {
                // A12 line of PPU address bus has transitioned 0 -> 1 - clock the scanline counter
                // TODO: The transition should only count if A12 was 0 for 2 ticks of the CPU M2 line
                this.TickScanlineCounter();
            }

            this.lastPpuA12 = currentPpuA12;
        }

        private void TickScanlineCounter()
        {
            if (this.scanlineCounter == 0)
            {
                if (this.irqEnabled && this.irqAssertion == null)
                {
                    this.irqAssertion = this.irq.Assert();
                }

                this.scanlineCounter = this.scanlineCounterReloadValue;
            }
            else if (this.reloadScanlineCounter)
            {
                this.scanlineCounter = this.scanlineCounterReloadValue;
                this.reloadScanlineCounter = false;
            }
            else
            {
                this.scanlineCounter--;
            }
        }

        #endregion

        #region MapperBase Implementation

        public override string Name
        {
            get { return "MMC3 Mapper"; }
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
                this.HandlePpuMemoryAccess(address);

                UInt32 chrRomBankMask = GetChrRomBankMaskForAddress(address);
                return this.chrRom[chrRomBankMask | (UInt32)(address & 0x03FF)];
            }
            else if (address >= 0x6000 && address <= 0x7FFF)
            {
                return this.prgRam[address - 0x6000];
            }
            else if (address >= 0x8000 && address <= 0xFFFF)
            {
                UInt32 prgRomBankMask = 0;
                switch (address & 0xE000)
                {
                    case 0x8000: prgRomBankMask = this.prgRomBank0Mask; break;
                    case 0xA000: prgRomBankMask = this.prgRomBank1Mask; break;
                    case 0xC000: prgRomBankMask = this.prgRomBank2Mask; break;
                    case 0xE000: prgRomBankMask = this.prgRomBank3Mask; break;
                }

                return this.prgRom[prgRomBankMask | (UInt32)(address & 0x1FFF)];
            }

            throw new InvalidOperationException();
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            if (address >= 0x0000 && address <= 0x1FFF)
            {
                this.HandlePpuMemoryAccess(address);

                if (this.Reader.ChrRomSize == 0)
                {
                    // Handle CHR RAM
                    UInt32 chrRomBankMask = this.GetChrRomBankMaskForAddress(address);
                    this.chrRom[chrRomBankMask | (UInt32)(address & 0x03FF)] = value;
                }
            }
            else if (address >= 0x6000 && address <= 0x7FFF)
            {
                this.prgRam[address - 0x6000] = value;
            }
            else if (address >= 0x8000 && address <= 0xFFFF)
            {
                switch (address & 0xE001)
                {
                    case 0x8000: this.SetBankSelectRegister(value); break;
                    case 0x8001: this.SetBankDataRegister(value); break;
                    case 0xA000: this.SetMirroringRegister(value); break;
                    case 0xA001: this.SetPrgRamRegister(value); break;
                    case 0xC000: this.SetIrqLatchRegister(value); break;
                    case 0xC001: this.SetIrqReloadRegister(value); break;
                    case 0xE000: this.SetIrqDisableRegister(value); break;
                    case 0xE001: this.SetIrqEnableRegister(value); break;
                }
            }
        }

        private UInt32 GetChrRomBankMaskForAddress(int address)
        {
            switch (address & 0x1C00)
            {
                case 0x0000: return this.chrRomBank0Mask;
                case 0x0400: return this.chrRomBank1Mask;
                case 0x0800: return this.chrRomBank2Mask;
                case 0x0C00: return this.chrRomBank3Mask;
                case 0x1000: return this.chrRomBank4Mask;
                case 0x1400: return this.chrRomBank5Mask;
                case 0x1800: return this.chrRomBank6Mask;
                case 0x1C00: return this.chrRomBank7Mask;
            }

            throw new InvalidOperationException();
        }

        #endregion
    }
}

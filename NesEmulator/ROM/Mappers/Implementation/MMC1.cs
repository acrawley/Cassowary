using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Memory;
using NesEmulator.ROM.Readers;

namespace NesEmulator.ROM.Mappers.Implementation
{
    [Export(typeof(IMapperFactory))]
    [MapperId(1)]
    internal class MMC1MapperFactory : IMapperFactory
    {
        IMapper IMapperFactory.CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
        {
            return new MMC1Mapper(reader, cpuBus, ppuBus);
        }
    }

    internal class MMC1Mapper : MapperBase, IMemoryMappedDevice
    {
        #region Constants

        private const int PrgRomMaxSize = 512 * 1024;
        private const int ChrRomMaxSize = 128 * 1024;
        private const int PrgRamMaxSize = 32 * 1024;

        private const int PrgRomBankSize = 16 * 1024;
        private const int ChrRomBankSize = 4 * 1024;

        #endregion

        #region Private Fields

        private byte[] prgRam;
        private byte[] prgRom;
        private byte[] chrRom;

        private byte shiftRegister;

        private UInt32 prgRomLoBankMask;
        private UInt32 prgRomHiBankMask;
        private UInt32 chrRomLoBankMask;
        private UInt32 chrRomHiBankMask;

        private int chrRomBanks;
        private int prgRomBanks;

        #endregion

        #region Registers

        private byte controlRegisterValue;
        private bool prgSwitchBoth;
        private bool prgSwitchHi;
        private bool prgSwitchLo;
        private bool chrSwitch8K;

        private void SetControlRegister(byte value)
        {
            this.controlRegisterValue = value;

            // Set mirroring
            this.DisposeMirrorings();
            switch (value & 0x03)
            {
                case 0x00:
                    // One screen, mapped to lower nametable: A A
                    //                                        A A
                    base.MapNametableA(0x2000);
                    base.MapNametableB(-1);
                    this.MirrorPpuRange(0x2000, 0x23FF, 0x2400, 0x2FFF);
                    break;

                case 0x01:
                    // One screen, mapped to upper nametable: B B
                    //                                        B B
                    base.MapNametableA(-1);
                    base.MapNametableB(0x2400);
                    this.MirrorPpuRange(0x2400, 0x27FF, 0x2000, 0x23FF);
                    this.MirrorPpuRange(0x2400, 0x27FF, 0x2800, 0x2FFF);
                    break;

                case 0x02:
                    // Vertical mirroring: A B
                    //                     A B
                    base.SetNametableMirroring(MirroringMode.Vertical);
                    break;

                case 0x03:
                    // Horizontal mirroring: A A
                    //                       B B
                    base.SetNametableMirroring(MirroringMode.Horizontal);
                    break;
            }

            this.prgSwitchBoth = false;
            this.prgSwitchHi = false;
            this.prgSwitchLo = false;

            switch ((value & 0x0C) >> 2)
            {
                case 0x00:
                case 0x01:
                    this.prgSwitchBoth = true;
                    break;

                case 0x02:
                    this.prgSwitchHi = true;
                    // Fix first bank at 0x8000
                    this.prgRomLoBankMask = 0;
                    break;

                case 0x03:
                    this.prgSwitchLo = true;
                    // Fix last bank at 0xC000
                    this.prgRomHiBankMask = (UInt32)(this.prgRomBanks - 1) << 14;
                    break;
            }

            this.chrSwitch8K = ((value & 0x10) == 0x00);
        }

        private void SetChrLoBankRegister(byte value)
        {
            if (this.Reader.ChrRomSize == 0)
            {
                return;
            }

            if (this.chrSwitch8K)
            {
                int bank = (value & 0x1E) % this.chrRomBanks;
                this.chrRomLoBankMask = (UInt32)bank << 12;
                this.chrRomHiBankMask = ((UInt32)bank + 1) << 12;
            }
            else
            {
                this.chrRomLoBankMask = (UInt32)(value % this.chrRomBanks) << 12;
            }
        }

        private void SetChrHiBankRegister(byte value)
        {
            if (this.Reader.ChrRomSize == 0)
            {
                return;
            }

            if (!this.chrSwitch8K)
            {
                this.chrRomHiBankMask = (UInt32)(value % this.chrRomBanks) << 12;
            }
        }

        bool prgRamEnabled;

        private void SetPrgBankRegister(byte value)
        {
            this.prgRamEnabled = ((value & 0x10) == 0x00);

            if (this.prgSwitchBoth)
            {
                int bank = (value & 0x0E) % this.prgRomBanks;
                this.prgRomLoBankMask = (UInt32)bank << 14;
                this.prgRomHiBankMask = (UInt32)(bank + 1) << 14;
            }
            else if (this.prgSwitchHi)
            {
                this.prgRomHiBankMask = (UInt32)((value & 0x0F) % this.prgRomBanks) << 14;
            }
            else if (this.prgSwitchLo)
            {
                this.prgRomLoBankMask = (UInt32)((value & 0x0F) % this.prgRomBanks) << 14;
            }
        }

        #endregion

        #region Constructor

        internal MMC1Mapper(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus)
            : base(reader, cpuBus, ppuBus)
        {
            if (this.Reader.PrgRamSize > MMC1Mapper.PrgRamMaxSize)
            {
                throw new ArgumentOutOfRangeException("MMC1 supports a maximum of 32KB of PRG RAM");
            }

            if (this.Reader.PrgRomSize > MMC1Mapper.PrgRomMaxSize)
            {
                throw new ArgumentOutOfRangeException("MMC1 supports a maximum of 512KB of PRG ROM");
            }

            if (this.Reader.ChrRomSize > MMC1Mapper.ChrRomMaxSize)
            {
                throw new ArgumentOutOfRangeException("MMC1 supports a maximum of 128KB of CHR ROM");
            }

            // Initialize ROM
            int prgRomSize = this.Reader.PrgRomSize;
            int prgRamSize = (this.Reader.PrgRamSize != 0) ? this.Reader.PrgRamSize : 0x2000;
            int chrRomSize = (this.Reader.ChrRomSize != 0) ? this.Reader.ChrRomSize : 0x2000;

            this.prgRomBanks = prgRomSize / MMC1Mapper.PrgRomBankSize;
            this.chrRomBanks = chrRomSize / MMC1Mapper.ChrRomBankSize;

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
            else
            {
                // We're using CHR RAM - initialize the masks for the fixed banks
                this.chrRomLoBankMask = 0x0000;
                this.chrRomHiBankMask = 0x1000;
            }

            this.shiftRegister = 0x10;

            // 8K bank of PRG RAM at CPU 0x6000
            this.MapCpuRange(this, 0x6000, 0x7FFF);

            // 2 16K banks of PRG ROM at CPU 0x8000 and 0xC000
            this.MapCpuRange(this, 0x8000, 0xFFFF);

            // 2 4K banks of CHR ROM at PPU 0x0000 and 0x1000
            this.MapPpuRange(this, 0x0000, 0x1FFF);

            // Initial state varies, but the last bank of PRG ROM seems to always be mapped
            //  at 0xC000 - see http://forums.nesdev.com/viewtopic.php?t=6766
            this.prgRomHiBankMask = (UInt32)(this.prgRomBanks - 1) << 14;
        }

        #endregion

        #region BaseMapper Implementation

        public override string Name
        {
            get { return "MMC1 Mapper"; }
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
            if (address >= 0x0000 && address <= 0x0FFF)
            {
                return this.chrRom[this.chrRomLoBankMask | ((UInt32)address & 0x0FFF)];
            }
            else if (address >= 0x1000 && address <= 0x1FFF)
            {
                return this.chrRom[this.chrRomHiBankMask | ((UInt32)address & 0x0FFF)];
            }
            else if (address >= 0x6000 && address <= 0x7FFF)
            {
                return this.prgRamEnabled ? this.prgRam[address - 0x6000] : (byte)0x00;
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
            if (address >= 0x0000 && address <= 0x1FFF)
            {
                if (this.Reader.ChrRomSize == 0)
                {
                    // Cartridge has CHR RAM
                    this.chrRom[address] = value;
                }
            }
            if (address >= 0x6000 && address <= 0x7FFF)
            {
                if (this.prgRamEnabled)
                {
                    this.prgRam[address - 0x6000] = value;
                }
            }
            else if (address >= 0x8000 && address <= 0xFFFF)
            {
                if ((value & 0x80) == 0x80)
                {
                    // Write with high bit set resets the internal shift register
                    this.shiftRegister = 0x10;

                    // Reset also locks PRG ROM at 0xC000 to the last bank
                    this.SetControlRegister((byte)(this.controlRegisterValue | 0x0C));
                }
                else
                {
                    bool registerFull = ((this.shiftRegister & 0x01) == 0x01);

                    this.shiftRegister >>= 1;
                    this.shiftRegister |= (byte)((value & 0x01) << 4);

                    if (registerFull)
                    {
                        Debug.WriteLine("MMC1: Register full: Address = 0x{0:X4}, Value = 0x{1:X2}", address, this.shiftRegister);

                        // Copy shift register value to selected internal register and reset
                        if (address >= 0x8000 && address <= 0x9FFF)
                        {
                            this.SetControlRegister(this.shiftRegister);
                        }
                        else if (address >= 0xA000 && address <= 0xBFFF)
                        {
                            this.SetChrLoBankRegister(this.shiftRegister);
                        }
                        else if (address >= 0xC000 && address <= 0xDFFF)
                        {
                            this.SetChrHiBankRegister(this.shiftRegister);
                        }
                        else if (address >= 0xE000 && address <= 0xFFFF)
                        {
                            this.SetPrgBankRegister(this.shiftRegister);
                        }

                        this.shiftRegister = 0x10;
                    }
                }
            }
        }

        #endregion
    }
}

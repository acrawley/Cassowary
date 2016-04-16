using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Graphics;
using EmulatorCore.Components.Memory;
using EmulatorCore.Extensions;

namespace NesEmulator.PPU
{
    internal class Ricoh2C02 : IEmulatorComponent, IMemoryMappedDevice
    {
        #region Constants

        private const int PPUCTRL_REGISTER = 0x2000;
        private const int PPUMASK_REGISTER = 0x2001;
        private const int PPUSTATUS_REGISTER = 0x2002;
        private const int OAMADDR_REGISTER = 0x2003;
        private const int OAMDATA_REGISTER = 0x2004;
        private const int PPUSCROLL_REGISTER = 0x2005;
        private const int PPUADDR_REGISTER = 0x2006;
        private const int PPUDATA_REGISTER = 0x2007;

        private static long StopwatchTicksPerFrame = Stopwatch.Frequency / 60;

        #endregion

        #region MEF Imports

        [Import(typeof(IPaletteFramebuffer))]
        internal IPaletteFramebuffer Framebuffer { get; private set; }

        #endregion

        private IProcessorInterrupt nmi;
        private IDisposable nmiAssertion;

        private IMemoryBus cpuBus;
        private IMemoryBus ppuBus;
        private IMemoryBus oamBus;

        private bool oddFrame;
        private int scanline;
        private int cycle;

        #region Registers and Flags

        private byte PPULATCH;

        private int baseNametableAddr;
        private int ppuDataIncrement;
        private int spritePatternTableAddr;
        private int backgroundPatternTableAddr;
        private bool tallSprites;
        private bool isMaster;
        private bool generateNMI;

        private void SetPpuCtrl(byte value)
        {
            switch (value & 0x03)
            {
                case 0x00: this.baseNametableAddr = 0x2000; break;
                case 0x01: this.baseNametableAddr = 0x2400; break;
                case 0x02: this.baseNametableAddr = 0x2800; break;
                case 0x03: this.baseNametableAddr = 0x2C00; break;
            }

            this.ppuDataIncrement = ((value & 0x04) == 0x04) ? 32 : 1;
            this.spritePatternTableAddr = ((value & 0x08) == 0x08) ? 0x1000 : 0x0000;
            this.backgroundPatternTableAddr = ((value & 0x10) == 0x10) ? 0x1000 : 0x0000;
            this.tallSprites = ((value & 0x20) == 0x20);
            this.isMaster = ((value & 0x40) == 0x40);
            this.generateNMI = ((value & 0x80) == 0x80);
        }

        private bool greyscale;
        private bool showLeftBackground;
        private bool showLeftSprites;
        private bool showBackground;
        private bool showSprites;
        private bool redEmphasis;
        private bool greenEmphasis;
        private bool blueEmphasis;

        private void SetPpuMask(byte value)
        {
            this.greyscale = ((value & 0x01) == 0x01);
            this.showLeftBackground = ((value & 0x02) == 0x02);
            this.showLeftSprites = ((value & 0x04) == 0x04);
            this.showBackground = ((value & 0x08) == 0x08);
            this.showSprites = ((value & 0x10) == 0x10);
            this.redEmphasis = ((value & 0x20) == 0x20);
            this.greenEmphasis = ((value & 0x40) == 0x40);
            this.blueEmphasis = ((value & 0x80) == 0x80);
        }

        private bool spriteOverflow;
        private bool spriteZeroHit;
        private bool isVBlank;

        private byte PPUSTATUS
        {
            get
            {
                // Reset latches
                this.ppuScrollState = PPUSCROLL_State.HorizontalOffset;
                this.ppuAddrState = PPUADDR_State.HiByte;

                byte value = (byte)((this.PPULATCH & 0x1F) |
                                    (this.spriteOverflow ? 0x20 : 0x00) |
                                    (this.spriteZeroHit ? 0x40 : 0x00) |
                                    (this.isVBlank ? 0x80 : 0x00));

                // The VBlank flag is cleared after reading
                this.isVBlank = false;

                this.PPULATCH = value;
                return value;
            }
        }

        private byte oamAddr;

        private void SetOamAddr(byte value)
        {
            this.oamAddr = value;
        }

        private byte OAMDATA
        {
            get
            {
                // TODO - this is weird
                return 0x00;
            }
            set
            {
                this.oamBus.Write(this.oamAddr, value);
                this.oamAddr++;
            }
        }

        private enum PPUSCROLL_State
        {
            HorizontalOffset,
            VerticalOffset,
        }

        private byte horizontalScrollOffset;
        private byte verticalScrollOffset;
        private PPUSCROLL_State ppuScrollState;

        private void SetPpuScroll(byte value)
        {
            switch (this.ppuScrollState)
            {
                case PPUSCROLL_State.HorizontalOffset:
                    this.horizontalScrollOffset = value;
                    this.ppuScrollState = PPUSCROLL_State.VerticalOffset;
                    break;

                case PPUSCROLL_State.VerticalOffset:
                    this.verticalScrollOffset = value;
                    this.ppuScrollState = PPUSCROLL_State.HorizontalOffset;
                    break;
            }
        }

        private enum PPUADDR_State
        {
            HiByte,
            LoByte,
        }

        private UInt16 ppuAddr;
        private PPUADDR_State ppuAddrState;

        private void SetPpuAddr(byte value)
        {
            switch (this.ppuAddrState)
            {
                case PPUADDR_State.HiByte:
                    this.ppuAddr = (UInt16)(value << 8);
                    this.ppuAddrState = PPUADDR_State.LoByte;
                    break;

                case PPUADDR_State.LoByte:
                    this.ppuAddr |= value;
                    this.ppuAddrState = PPUADDR_State.HiByte;
                    break;
            }
        }

        private byte ppuDataBuffer = 0;

        private byte PPUDATA
        {
            get
            {
                byte readVal = this.ppuBus.Read(this.ppuAddr);

                // Reading data from 0x0000 - 0x3EFF returns the value of an internal buffer, then loads the requested
                //  address into the buffer, where it will be returned on the next read.  Reads between 0x3F00 and 0x3FFF
                //  return the value immediately.
                byte value = (((this.ppuAddr % 0x4000) & 0xFF00) == 0x3F00) ? readVal : this.ppuDataBuffer;
                this.ppuDataBuffer = readVal;
                this.ppuAddr += (UInt16)this.ppuDataIncrement;
                return value;
            }
            set
            {
                this.ppuBus.Write(this.ppuAddr, value);
                this.ppuAddr += (UInt16)this.ppuDataIncrement;
            }
        }

        #endregion

        internal Ricoh2C02(IProcessorCore cpu, IMemoryBus cpuBus, IMemoryBus ppuBus, IMemoryBus oamBus)
        {
            this.nmi = cpu.GetInterruptByName("NMI");
            this.cpuBus = cpuBus;
            this.ppuBus = ppuBus;
            this.oamBus = oamBus;

            cpuBus.RegisterMappedDevice(this, 0x2000, 0x2007);

            this.SetPpuCtrl(0x00);
            this.SetPpuMask(0x00);
            this.SetOamAddr(0x00);

            this.spriteZeroHit = false;
            this.spriteOverflow = false;
            this.isVBlank = false;
            this.oddFrame = false;
            this.scanline = -1;
            this.cycle = 0;

            this.vbiTimer = new Stopwatch();
            this.vbiTimer.Start();
        }

        public void Step()
        {
            // PPU ticks 3 times during each CPU cycle
            Tick();
            Tick();
            Tick();
        }

        Stopwatch vbiTimer;

        private byte nametableFetch;
        private byte attributeTableFetch;
        private byte tileBitmapLoFetch;
        private byte tileBitmapHiFetch;

        private UInt16 tileBitmapLoShiftRegister;
        private UInt16 tileBitmapHiShiftRegister;
        private UInt16 attributeShiftRegister;

        private void FetchTileData(int row, int col, int tileRow)
        {
            this.tileBitmapLoShiftRegister <<= 1;
            this.tileBitmapHiShiftRegister <<= 1;

            // 8 cycles required to read a complete set of tile data
            int step = (cycle - 1) & 0x07;

            switch (step)
            {
                case 0:
                    // Cycle 0: Put address of nametable data on the bus
                    this.ppuAddr = (UInt16)(this.baseNametableAddr + (row * 32) + col);
                    break;
                case 1:
                    // Cycle 1: Read nametable data
                    this.nametableFetch = this.ppuBus.Read(this.ppuAddr);
                    break;
                case 2:
                    // Cycle 2: Put address of attribute table data on the bus
                    this.ppuAddr = (UInt16)(this.baseNametableAddr + 0x3C0 + ((row >> 2) * 8) + (col >> 2));
                    break;
                case 3:
                    // Cycle 3: Read attribute data
                    this.attributeTableFetch = this.ppuBus.Read(this.ppuAddr);
                    break;
                case 4:
                    // Cycle 4: Put address of low byte of tile bitmap on the bus
                    this.ppuAddr = (UInt16)(this.backgroundPatternTableAddr + (this.nametableFetch * 16 + tileRow));
                    break;
                case 5:
                    // Cycle 5: Read low byte of tile bitmap
                    this.tileBitmapLoFetch = this.ppuBus.Read(this.ppuAddr);
                    break;
                case 6:
                    // Cycle 6: Put address of high byte of tile bitmap on the bus
                    this.ppuAddr = (UInt16)(this.backgroundPatternTableAddr + (this.nametableFetch * 16) + 8 + tileRow);
                    break;
                case 7:
                    // Cycle 7: Read high byte of tile bitmap and shift into the registers
                    this.tileBitmapHiFetch = this.ppuBus.Read(this.ppuAddr);

                    this.tileBitmapHiShiftRegister |= this.tileBitmapHiFetch;
                    this.tileBitmapLoShiftRegister |= this.tileBitmapLoFetch;

                    this.attributeShiftRegister >>= 8;
                    this.attributeShiftRegister |= (UInt16)(this.attributeTableFetch << 8);
                    break;
            }
        }

        private void Tick()
        {
            if (this.cycle > 340)
            {
                this.cycle = 0;
                this.scanline++;
            }

            if (this.scanline > 260)
            {
                this.scanline = -1;
                this.oddFrame = !this.oddFrame;
            }

            if (this.scanline == -1)
            {
                // Pre-render scanline
                if (cycle == 1)
                {
                    // Reset per-frame flags on scanline -1, cycle 1
                    this.isVBlank = false;
                    this.spriteZeroHit = false;
                    this.spriteOverflow = false;
                    this.oddFrame = !this.oddFrame;

                    if (this.nmiAssertion != null)
                    {
                        this.nmiAssertion.Dispose();
                        this.nmiAssertion = null;
                    }
                }
                else if (cycle >= 321 && cycle <= 336 && this.showBackground)
                {
                    if (cycle <= 328)
                    {
                        this.FetchTileData(0, 0, 0);
                    }
                    else
                    {
                        this.FetchTileData(0, 1, 0);
                    }
                }
                else if (cycle == 338 && this.oddFrame)
                {
                    // Pre-render scanline is one cycle shorter on odd frames
                    cycle++;
                }
            }
            else if (this.scanline <= 239)
            {
                // Visible scanlines
                if (cycle >= 1 && cycle <= 256 && this.showBackground)
                {
                    int paletteOffset = (((this.tileBitmapLoShiftRegister & 0x8000) == 0x8000) ? 0x01 : 0x00) +
                                        (((this.tileBitmapHiShiftRegister & 0x8000) == 0x8000) ? 0x02 : 0x00);

                    byte tileRow = (byte)(this.scanline >> 3);
                    byte tileCol = (byte)((this.cycle - 1) >> 3);

                    byte quadrant = (byte)((((tileRow & 0x02) == 0x02) ? 0x04 : 0x00) +
                                           (((tileCol & 0x02) == 0x02) ? 0x02 : 0x00));
                    byte paletteIndex = (byte)(((this.attributeShiftRegister & (0x03 << quadrant)) >> quadrant) & 0x03);

                    int color = this.ppuBus.Read(0x3f00 + (paletteIndex * 4) + paletteOffset);

                    this.Framebuffer.SetPixel(this.cycle - 1, this.scanline, color);
                    this.FetchTileData(tileRow, tileCol + 2, this.scanline & 0x07);
                }
                else if (cycle >= 321 && cycle <= 336 && this.showBackground)
                {
                    // Pre-fetch first two tiles for next scanline
                    if (cycle <= 328)
                    {
                        this.FetchTileData((this.scanline + 1) >> 3, 0, (this.scanline + 1) & 0x07);
                    }
                    else
                    {
                        this.FetchTileData((this.scanline + 1) >> 3, 1, (this.scanline + 1) & 0x07);
                    }
                }
            }
            else if (this.scanline == 240)
            {
                // Post-render scanline - PPU is idle
            }
            else //if (this.scanline <= 260)
            {
                // VBlank scanlines
                if (this.scanline == 241 && this.cycle == 1)
                {
                    //if (this.vbiTimer.ElapsedTicks > Ricoh2C02.StopwatchTicksPerFrame)
                    //{
                    //    Debug.WriteLine("Frame too slow!");
                    //}

                    while (this.vbiTimer.ElapsedTicks < Ricoh2C02.StopwatchTicksPerFrame) { }

                    this.vbiTimer.Restart();

                    this.Framebuffer.Present();

                    // VBlank flag is set on the second tick of line 241
                    this.isVBlank = true;

                    // Assert NMI on CPU, if enabled
                    if (this.generateNMI)
                    {
                        this.nmiAssertion = this.nmi.Assert();
                    }
                }
            }

            this.cycle++;
        }

        #region IEmulatorComponent Implementation

        public string Name
        {
            get { return "Ricoh 2C02 PPU"; }
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            switch (address)
            {
                case Ricoh2C02.PPUCTRL_REGISTER:
                case Ricoh2C02.PPUMASK_REGISTER:
                case Ricoh2C02.OAMADDR_REGISTER:
                case Ricoh2C02.PPUSCROLL_REGISTER:
                case Ricoh2C02.PPUADDR_REGISTER:
                    // Write-only registers return whatever happens to be on the bus
                    return this.PPULATCH;

                case Ricoh2C02.PPUSTATUS_REGISTER:
                    return this.PPUSTATUS;

                case Ricoh2C02.OAMDATA_REGISTER:
                    return this.OAMDATA;

                case Ricoh2C02.PPUDATA_REGISTER:
                    return this.PPUDATA;
            }

            return 0;
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            switch (address)
            {
                case Ricoh2C02.PPUCTRL_REGISTER:
                    this.SetPpuCtrl(value);
                    break;

                case Ricoh2C02.PPUMASK_REGISTER:
                    this.SetPpuMask(value);
                    break;

                case Ricoh2C02.OAMADDR_REGISTER:
                    this.SetOamAddr(value);
                    break;

                case Ricoh2C02.OAMDATA_REGISTER:
                    this.OAMDATA = value;
                    break;

                case Ricoh2C02.PPUSCROLL_REGISTER:
                    this.SetPpuScroll(value);
                    break;

                case Ricoh2C02.PPUADDR_REGISTER:
                    this.SetPpuAddr(value);
                    break;

                case Ricoh2C02.PPUDATA_REGISTER:
                    this.PPUDATA = value;
                    break;
            }

            this.PPULATCH = value;
        }

        #endregion
    }
}

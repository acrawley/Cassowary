using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using EmulatorCore.Components;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Graphics;
using EmulatorCore.Components.Memory;
using EmulatorCore.Extensions;

namespace NesEmulator.PPU
{
    internal class Ricoh2C02 : IEmulatorComponent, IMemoryMappedDevice, IPartImportsSatisfiedNotification
    {
        #region Constants

        public const int PPUCTRL_REGISTER = 0x2000;
        public const int PPUMASK_REGISTER = 0x2001;
        public const int PPUSTATUS_REGISTER = 0x2002;
        public const int OAMADDR_REGISTER = 0x2003;
        public const int OAMDATA_REGISTER = 0x2004;
        public const int PPUSCROLL_REGISTER = 0x2005;
        public const int PPUADDR_REGISTER = 0x2006;
        public const int PPUDATA_REGISTER = 0x2007;

        private static long StopwatchTicksPerFrame = Stopwatch.Frequency / 60;

        #endregion

        #region MEF Imports

        [Import(typeof(IPaletteFramebuffer))]
        internal IPaletteFramebuffer Framebuffer { get; private set; }

        #endregion

        #region Private Fields

        private IProcessorInterrupt vbi;
        private IDisposable vbiAssertion;
        Stopwatch vbiTimer;

        private IMemoryBus ppuBus;

        private bool oddFrame;
        private int scanline;
        private int cycle;

        private byte[] paletteMemory;
        private byte[] primaryOam;
        private byte[] secondaryOam;

        #endregion

        #region Constructor

        internal Ricoh2C02(IProcessorCore cpu, IMemoryBus cpuBus, IMemoryBus ppuBus)
        {
            this.ppuBus = ppuBus;

            this.vbi = cpu.GetInterruptByName("NMI");
            this.vbiTimer = new Stopwatch();

            this.paletteMemory = new byte[0x20];
            this.primaryOam = new byte[0x100];
            this.secondaryOam = new byte[0x20];

            // PPU registers on CPU bus
            cpuBus.RegisterMappedDevice(this, 0x2000, 0x2007);

            // Palette data
            ppuBus.RegisterMappedDevice(this, 0x3F00, 0x3F1F);

            // Set up power-on state
            this.SetPpuCtrl(0x00);
            this.SetPpuMask(0x00);
            this.SetOamAddr(0x00);

            this.spriteZeroHit = false;
            this.spriteOverflow = false;
            this.isVBlank = false;
            this.oddFrame = false;
            this.scanline = -1;
            this.cycle = 0;

            this.vbiTimer.Start();
        }

        #endregion

        #region IPartImportsSatisfiedNotification Implementation

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            this.Framebuffer.Initialize(256, 240, 64);
        }

        #endregion

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
                if (this.overrideOamRead)
                {
                    // OAMDATA always returns 0xFF when the override flag is set during cycles 1-64
                    //  of a visible scanline
                    return 0xFF;
                }

                return this.primaryOam[this.oamAddr];
            }
            set
            {
                if (this.scanline <= 239 && (this.showSprites || this.showBackground))
                {
                    // When rendering visible scanlines, writes to OAMDATA don't actually update OAM,
                    //  but they do increment the top 6 bits of OAMADDR
                    this.oamAddr += 4;
                }
                else
                {
                    this.primaryOam[this.oamAddr] = value;
                    this.oamAddr++;
                }
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

        public void Step()
        {
            // PPU ticks 3 times during each CPU cycle
            Tick();
            Tick();
            Tick();
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
                TickPreRenderScanline();
            }
            else if (this.scanline <= 239)
            {
                // Visible scanlines
                TickVisibleScanline();
            }
            else if (this.scanline == 240)
            {
                // Post-render scanline - PPU is idle
            }
            else //if (this.scanline <= 260)
            {
                // VBlank scanlines
                TickVBlankScanline();
            }

            this.cycle++;
        }

        private void TickPreRenderScanline()
        {
            if (this.cycle == 0)
            {
                // 2C02 BUG: If OAMADDR is set to a value of 0x08 or greater at the start of a frame, the first 8
                //  bytes of OAM are replaced with the 8 bytes starting at OAMADDR & 0xF8. 
                if (this.oamAddr >= 0x08)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        this.primaryOam[i] = this.primaryOam[(this.oamAddr & 0xF8) + i];
                    }
                }
            }
            else if (this.cycle == 1)
            {
                // Reset per-frame flags on scanline -1, cycle 1
                this.isVBlank = false;
                this.spriteZeroHit = false;
                this.spriteOverflow = false;

                this.oddFrame = !this.oddFrame;

                if (this.vbiAssertion != null)
                {
                    this.vbiAssertion.Dispose();
                    this.vbiAssertion = null;
                }
            }
            else if (this.cycle >= 321 && this.cycle <= 336 && this.showBackground)
            {
                if (this.cycle <= 328)
                {
                    this.FetchTileData(0, 0, 0);
                }
                else
                {
                    this.FetchTileData(0, 1, 0);
                }
            }
            else if (this.cycle == 339 && this.oddFrame)
            {
                // Pre-render scanline is one cycle shorter on odd frames
                this.cycle++;
            }
        }

        private void TickVisibleScanline()
        {
            if (cycle == 0)
            {
                // Idle
            }
            else if (cycle >= 1 && cycle <= 256)
            {
                int pixelColor = 0;
                int bgPalette = 0;

                if (this.showBackground)
                {
                    // Use data from shift registers to calculate pixel color
                    bgPalette = (((this.tileBitmapLoShiftRegister & 0x8000) == 0x8000) ? 0x01 : 0x00) +
                                (((this.tileBitmapHiShiftRegister & 0x8000) == 0x8000) ? 0x02 : 0x00);

                    byte tileRow = (byte)(this.scanline >> 3);
                    byte tileCol = (byte)((this.cycle - 1) >> 3);

                    byte paletteIndex = 0;
                    if (bgPalette != 0)
                    {
                        // Palette entry 0 always comes from the first palette, so only do this for non-zero entries
                        byte quadrant = (byte)((((tileRow & 0x02) == 0x02) ? 0x04 : 0x00) +
                                               (((tileCol & 0x02) == 0x02) ? 0x02 : 0x00));
                        paletteIndex = (byte)(((this.attributeShiftRegister & (0x03 << quadrant)) >> quadrant) & 0x03);
                    }

                    pixelColor = this.paletteMemory[(paletteIndex * 4) + bgPalette];

                    this.FetchTileData(tileRow, tileCol + 2, this.scanline & 0x07);
                }

                if (this.showSprites)
                {
                    bool foundSprite = false;
                    // Process sprites in priority order
                    for (int i = 0; i < 8; i++)
                    {
                        if (this.spriteXPositionCounter[i] == 0)
                        {
                            if (!foundSprite)
                            {
                                int spritePalette = (((this.spriteBitmapLoShiftRegister[i] & 0x80) == 0x80) ? 0x01 : 0x00) +
                                                    (((this.spriteBitmapHiShiftRegister[i] & 0x80) == 0x80) ? 0x02 : 0x00);

                                if (spritePalette != 0)
                                {
                                    // Found a sprite with a non-transparent pixel at this location
                                    foundSprite = true;

                                    // Use the sprite's color if the background is transparent or the sprite has foreground priority
                                    if (bgPalette == 0 || ((this.spriteAttributeLatch[i] & 0x20) == 0x00))
                                    {
                                        pixelColor = this.paletteMemory[((this.spriteAttributeLatch[i] & 0x03) * 4) + 16 + spritePalette];

                                        if (i == 0 && this.spriteZeroPresent)
                                        {
                                            this.spriteZeroHit = true;
                                        }
                                    }
                                }
                            }

                            this.spriteBitmapLoShiftRegister[i] <<= 1;
                            this.spriteBitmapHiShiftRegister[i] <<= 1;
                        }
                        else
                        {
                            this.spriteXPositionCounter[i]--;
                        }
                    }
                }

                this.Framebuffer.SetPixel(this.cycle - 1, this.scanline, pixelColor);
            }
            else if (cycle <= 320 && this.showSprites)
            {
                // Pre-fetch sprite tiles for next scanline
                this.FetchSpriteData();

            }
            else if (cycle <= 336 && this.showBackground)
            {
                // Pre-fetch first two background tiles for next scanline
                if (cycle <= 328)
                {
                    this.FetchTileData((this.scanline + 1) >> 3, 0, (this.scanline + 1) & 0x07);
                }
                else
                {
                    this.FetchTileData((this.scanline + 1) >> 3, 1, (this.scanline + 1) & 0x07);
                }
            }

            // Sprite evaluation for next scanline
            DoSpriteEvaluation();
        }

        private void TickVBlankScanline()
        {
            if (this.scanline == 241 && this.cycle == 1)
            {
                // If speed throttling is enabled, wait until it's been at least 1/60th of a second since the last VBlank
                if (this.vbiTimer.IsRunning)
                {
                    //if (this.vbiTimer.ElapsedTicks > Ricoh2C02.StopwatchTicksPerFrame)
                    //{
                    //    Debug.WriteLine("Frame too slow - {0} > {1}!", this.vbiTimer.ElapsedTicks, Ricoh2C02.StopwatchTicksPerFrame);
                    //}

                    while (this.vbiTimer.ElapsedTicks < Ricoh2C02.StopwatchTicksPerFrame)
                    {
                    }

                    this.vbiTimer.Restart();
                }

                this.Framebuffer.Present();

                // VBlank flag is set on the second tick of line 241
                this.isVBlank = true;

                // Assert NMI on CPU, if enabled
                if (this.generateNMI)
                {
                    this.vbiAssertion = this.vbi.Assert();
                }
            }
        }

        #region Sprite Evaluation Logic

        private SpriteEvaluationState currentState;
        private int spritesFound;
        bool spriteZeroPresent;
        private byte spriteEvalTemp;
        bool overrideOamRead;        

        private enum SpriteEvaluationState
        {
            EvaluateYCoord,
            CopyTileIndex,
            CopyAttributes,
            CopyXCoord,
            OverflowCheck,
            EvaluationComplete
        }

        private void DoSpriteEvaluation()
        {
            if (this.cycle == 0)
            {
                this.currentState = SpriteEvaluationState.EvaluateYCoord;
                this.spritesFound = 0;
                this.spriteZeroPresent = false;
            }
            else if (this.cycle <= 64)
            {
                this.overrideOamRead = true;

                if ((this.cycle & 0x01) == 0x01)
                {
                    // Odd cycle - write 0xFF to secondary OAM
                    this.secondaryOam[this.cycle >> 1] = 0xFF;
                }
            }
            else if (this.cycle <= 256)
            {
                this.overrideOamRead = false;

                if ((this.cycle & 0x01) == 0x01)
                {
                    // Odd cycle - read data for the current sprite from primary OAM
                    // NOTE: This assumes OAMADDR was reset to 0 as expected during ticks 257-320 of the previous
                    //  scanline.  If game code modifies OAMADDR afterwards, lots of weird stuff can happen.
                    this.spriteEvalTemp = this.primaryOam[this.oamAddr];
                }
                else
                {
                    // Even cycle - write to secondary OAM
                    switch (this.currentState)
                    {
                        case SpriteEvaluationState.EvaluateYCoord:
                            this.secondaryOam[this.spritesFound * 4] = spriteEvalTemp;

                            // If the Y coordinate of the sprite is in range, we need to copy the rest of its data
                            //  from primary OAM
                            if (this.scanline >= this.spriteEvalTemp && this.scanline < (this.spriteEvalTemp + 8))
                            {
                                this.spriteZeroPresent = this.cycle == 66;

                                currentState = SpriteEvaluationState.CopyTileIndex;
                                this.oamAddr++;
                            }
                            else
                            {
                                // Sprite was not in range, check the next one
                                this.IncrementOamAddrAndCheckOverflow(4);
                            }
                            break;

                        case SpriteEvaluationState.CopyTileIndex:
                            this.secondaryOam[(this.spritesFound * 4) + 1] = spriteEvalTemp;
                            this.currentState = SpriteEvaluationState.CopyAttributes;
                            this.IncrementOamAddrAndCheckOverflow(1);
                            break;

                        case SpriteEvaluationState.CopyAttributes:
                            this.secondaryOam[(this.spritesFound * 4) + 2] = spriteEvalTemp;
                            this.currentState = SpriteEvaluationState.CopyXCoord;
                            this.IncrementOamAddrAndCheckOverflow(1);
                            break;

                        case SpriteEvaluationState.CopyXCoord:
                            this.secondaryOam[(this.spritesFound * 4) + 3] = spriteEvalTemp;

                            this.spritesFound++;
                            if (this.spritesFound == 8)
                            {
                                // Found 8 sprites for this line - continue processing to check for overflow
                                this.currentState = SpriteEvaluationState.OverflowCheck;
                            }
                            else
                            {
                                // Look for another sprite on this line
                                this.currentState = SpriteEvaluationState.EvaluateYCoord;
                            }

                            this.IncrementOamAddrAndCheckOverflow(1);
                            break;

                        case SpriteEvaluationState.OverflowCheck:
                            if (this.scanline >= this.spriteEvalTemp && this.scanline < (this.spriteEvalTemp + 8))
                            {
                                // Found another sprite on a full scanline
                                this.spriteOverflow = true;
                                this.IncrementOamAddrAndCheckOverflow(4);
                            }
                            else
                            {
                                // Sprite isn't on this scanline, check the next one
                                // 2C02 BUG: The OAM address is incremented by 5 in this case, so the Y coordinate
                                //  evaluated for further sprites won't actually be the Y coordinate
                                this.IncrementOamAddrAndCheckOverflow(5);
                            }

                            break;

                        case SpriteEvaluationState.EvaluationComplete:
                            // Do nothing
                            break;
                    }
                }
            }
            else if (this.cycle <= 320)
            {
                this.oamAddr = 0;
            }
        }

        private void IncrementOamAddrAndCheckOverflow(int increment)
        {
            byte temp = (byte)(this.oamAddr + increment);
            if (temp < this.oamAddr)
            {
                this.currentState = SpriteEvaluationState.EvaluationComplete;
            }
            else
            {
                this.oamAddr = temp;
            }
        }

        #endregion

        #region Background Tile Fetch Logic

        private byte nametableLatch;
        private byte attributeTableLatch;
        private byte tileBitmapLoLatch;
        private byte tileBitmapHiLatch;

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
                    this.nametableLatch = this.ppuBus.Read(this.ppuAddr);
                    break;
                case 2:
                    // Cycle 2: Put address of attribute table data on the bus
                    this.ppuAddr = (UInt16)(this.baseNametableAddr + 0x3C0 + ((row >> 2) * 8) + (col >> 2));
                    break;
                case 3:
                    // Cycle 3: Read attribute data
                    this.attributeTableLatch = this.ppuBus.Read(this.ppuAddr);
                    break;
                case 4:
                    // Cycle 4: Put address of low byte of tile bitmap on the bus
                    this.ppuAddr = (UInt16)(this.backgroundPatternTableAddr + (this.nametableLatch * 16 + tileRow));
                    break;
                case 5:
                    // Cycle 5: Read low byte of tile bitmap
                    this.tileBitmapLoLatch = this.ppuBus.Read(this.ppuAddr);
                    break;
                case 6:
                    // Cycle 6: Put address of high byte of tile bitmap on the bus
                    this.ppuAddr = (UInt16)(this.backgroundPatternTableAddr + (this.nametableLatch * 16) + 8 + tileRow);
                    break;
                case 7:
                    // Cycle 7: Read high byte of tile bitmap and shift into the registers
                    this.tileBitmapHiLatch = this.ppuBus.Read(this.ppuAddr);

                    this.tileBitmapHiShiftRegister |= this.tileBitmapHiLatch;
                    this.tileBitmapLoShiftRegister |= this.tileBitmapLoLatch;

                    this.attributeShiftRegister >>= 8;
                    this.attributeShiftRegister |= (UInt16)(this.attributeTableLatch << 8);
                    break;
            }
        }

        #endregion

        #region Sprite Tile Fetch Logic

        private byte[] spriteBitmapLoShiftRegister = new byte[8];
        private byte[] spriteBitmapHiShiftRegister = new byte[8];
        private byte[] spriteAttributeLatch = new byte[8];
        private byte[] spriteXPositionCounter = new byte[8];

        private void FetchSpriteData()
        {
            int step = (cycle - 1) & 0x07;
            int sprite = (cycle - 257) >> 3;

            switch (step)
            {
                case 0:
                    // Cycle 0: Put address of nametable garbage on bus
                    this.ppuAddr = 0x00; // TODO: Where does the real thing read from?
                    break;

                case 1:
                    // Cycle 1: Read and discard nametable garbage
                    this.ppuBus.Read(this.ppuAddr);
                    break;

                case 2:
                    // Cycle 2: Put address of nametable garbage on bus, load attribute byte
                    this.ppuAddr = 0x00;
                    this.spriteAttributeLatch[sprite] = this.secondaryOam[(sprite * 4) + 2];
                    break;

                case 3:
                    // Cycle 3: Read and discard nametable garbage, load X coordinate
                    this.ppuBus.Read(this.ppuAddr);
                    this.spriteXPositionCounter[sprite] = this.secondaryOam[(sprite * 4) + 3];
                    break;

                case 4:
                    // Cycle 4: Put address of low byte of sprite bitmap on the bus
                    this.ppuAddr = (UInt16)(this.spritePatternTableAddr + (this.secondaryOam[(sprite * 4) + 1] * 16 + (this.scanline - this.secondaryOam[(sprite * 4)])));
                    break;

                case 5:
                    {
                        // Cycle 5: Read low byte of sprite bitmap
                        byte pattern = this.ppuBus.Read(this.ppuAddr);

                        if ((this.spriteAttributeLatch[sprite] & 0x40) == 0x40)
                        {
                            // Flip pattern horizontally
                            pattern = (byte)(((pattern * 0x0802u & 0x22110u) | (pattern * 0x8020u & 0x88440u)) * 0x10101u >> 16);
                        }

                        if (sprite >= this.spritesFound)
                        {
                            // Sprite not present - replace with transparent tile
                            pattern = 0x00;
                        }

                        this.spriteBitmapLoShiftRegister[sprite] = pattern;
                    }
                    break;

                case 6:
                    // Cycle 6: Put address of high byte of sprite bitmap on the bus
                    this.ppuAddr = (UInt16)(this.spritePatternTableAddr + (this.secondaryOam[(sprite * 4) + 1] * 16 + 8 + (this.scanline - this.secondaryOam[(sprite * 4)])));
                    break;

                case 7:
                    {
                        // Cycle 7: Read high byte of sprite bitmap
                        byte pattern = this.ppuBus.Read(this.ppuAddr);

                        if ((this.spriteAttributeLatch[sprite] & 0x40) == 0x40)
                        {
                            pattern = (byte)(((pattern * 0x0802u & 0x22110u) | (pattern * 0x8020u & 0x88440u)) * 0x10101u >> 16);
                        }

                        if (sprite >= this.spritesFound)
                        {
                            pattern = 0x00;
                        }

                        this.spriteBitmapHiShiftRegister[sprite] = pattern;
                    }
                    break;
            }
        }

        #endregion

        #region IEmulatorComponent Implementation

        public string Name
        {
            get { return "Ricoh 2C02 PPU"; }
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            if (address >= 0x3F00 && address <= 0x3F1F)
            {
                return this.paletteMemory[address - 0x3F00];
            }

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
            if (address >= 0x3F00 && address <= 0x3F1F)
            {
                address -= 0x3F00;

                // The first entry of the first palette is the "universal" background color used
                //  for any pixel with a value of 0.  The first entries of the other palettes can
                //  still be set, but they're normally never seen.  The first entries of the sprite
                //  palettes mirror the first entry of the corresponding background palette
                switch (address)
                {
                    case 0x10:
                        this.paletteMemory[0x00] = value;
                        break;

                    case 0x14:
                        this.paletteMemory[0x04] = value;
                        break;

                    case 0x18:
                        this.paletteMemory[0x08] = value;
                        break;

                    case 0x1C:
                        this.paletteMemory[0x0C] = value;
                        break;

                    default:
                        this.paletteMemory[address] = value;
                        break;
                }

                return;
            }

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

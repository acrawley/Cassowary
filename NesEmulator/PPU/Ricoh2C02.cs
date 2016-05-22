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

        private const UInt16 COARSE_X_MASK = 0x001F;
        private const UInt16 COARSE_Y_MASK = 0x03E0;
        private const UInt16 NAMETABLE_MASK = 0x0C00;
        private const UInt16 FINE_Y_MASK = 0x7000;
        private const UInt16 HORIZONTAL_MASK = 0x041F;
        private const UInt16 VERTICAL_MASK = 0x7BE0;

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

            //this.vbiTimer.Start();
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

        private int ppuDataIncrement;
        private int spritePatternTableAddr;
        private int backgroundPatternTableAddr;
        private bool tallSprites;
        private bool isMaster;
        private bool generateNMI;

        private void SetPpuCtrl(byte value)
        {
            // t: ... BA ..... ..... = d: ......BA
            this.vramAddrTemp = (UInt16)((this.vramAddrTemp & 0x73FF) | ((value & 0x03) << 10));

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
                // Reset latch
                this.firstWrite = true;

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

        private bool firstWrite;
        private UInt16 vramAddr;
        private UInt16 vramAddrTemp;
        private byte fineXScroll;

        private void SetPpuScroll(byte value)
        {
            if (firstWrite)
            {
                // Update Fine X and Coarse X scroll data
                // x:                CBA = d: .....CBA
                // t: ... .. ..... HGFED = d: HGFED...
                this.fineXScroll = (byte)(value & 0x07);
                this.vramAddrTemp = (UInt16)((this.vramAddrTemp & 0x7FE0) | ((value & 0xF8) >> 3));
            }
            else
            {
                // Update Fine Y and Coarse Y scroll data
                // t: CBA .. HGFED ..... = d: HGFEDCBA
                this.vramAddrTemp = (UInt16)(((value & 0x07) << 12) | ((value & 0xF8) << 2) | (this.vramAddrTemp & 0x0C1F));
            }

            this.firstWrite = !this.firstWrite;
        }

        private void SetPpuAddr(byte value)
        {
            if (firstWrite)
            {
                // Load upper 8 bits of VRAM addr
                // t: .FE DC BA... ..... = d: ..FEDCBA
                // t: X.. .. ..... ..... = 0
                this.vramAddrTemp = (UInt16)(((value & 0x3F) << 8) | (this.vramAddrTemp & 0x00FF));
            }
            else
            {
                // Load lower 8 bits of VRAM addr
                // t: ... .. ..HGF EDCBA = d: HGFEDCBA
                // v                     = t
                this.vramAddrTemp = (UInt16)((this.vramAddrTemp & 0x7F00) | value);
                this.vramAddr = this.vramAddrTemp;
            }

            this.firstWrite = !this.firstWrite;
        }

        private byte ppuDataBuffer = 0;

        private byte PPUDATA
        {
            get
            {
                byte readVal = this.ppuBus.Read(this.vramAddr);

                // Reading data from 0x0000 - 0x3EFF returns the value of an internal buffer, then loads the requested
                //  address into the buffer, where it will be returned on the next read.  Reads between 0x3F00 and 0x3FFF
                //  return the value immediately.
                byte value = (((this.vramAddr % 0x4000) & 0xFF00) == 0x3F00) ? readVal : this.ppuDataBuffer;
                this.ppuDataBuffer = readVal;
                this.vramAddr += (UInt16)this.ppuDataIncrement;
                return value;
            }
            set
            {
                this.ppuBus.Write(this.vramAddr, value);
                this.vramAddr += (UInt16)this.ppuDataIncrement;
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
            else if (cycle <= 256)
            {
                if (this.cycle == 1)
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

                if (this.showBackground)
                {
                    this.FetchTileData();
                }
            }
            else if (cycle == 257 && this.showBackground)
            {
                // Copy horizontal scroll data from temp register to VRAM addr
                // v: ... .F ..... EDCBA = t: ... .F ..... EDCBA
                this.vramAddr = (UInt16)((this.vramAddrTemp & HORIZONTAL_MASK) | (this.vramAddr & VERTICAL_MASK));
            }
            else if (this.cycle >= 280 && this.cycle <= 304 && this.showBackground)
            {
                // Copy vertical scroll data from temp register to VRAM addr
                // v: IHG F. EDCBA ..... = t: IHG F. EDCBA .....
                this.vramAddr = (UInt16)((this.vramAddrTemp & VERTICAL_MASK) | (this.vramAddr & HORIZONTAL_MASK));

            }
            else if (this.cycle >= 321 && this.cycle <= 336 && this.showBackground)
            {
                this.FetchTileData();
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
            else if (cycle <= 256)
            {
                int pixelColor = 0;
                int bgPalette = 0;

                if (this.showBackground)
                {
                    // Use data from shift registers to calculate pixel color
                    bgPalette = (((this.tileBitmapLoShiftRegister & (0x8000 >> this.fineXScroll)) != 0) ? 0x01 : 0x00) +
                                (((this.tileBitmapHiShiftRegister & (0x8000 >> this.fineXScroll)) != 0) ? 0x02 : 0x00);

                    int paletteIndex = 0;
                    if (bgPalette != 0)
                    {
                        // Palette entry 0 always comes from the first palette, so only calculate the target palette
                        //  for non-zero pixels.
                        paletteIndex = (((this.attributeLoShiftRegister & (0x80 >> this.fineXScroll)) != 0) ? 0x01 : 0x00) +
                                       (((this.attributeHiShiftRegister & (0x80 >> this.fineXScroll)) != 0) ? 0x02 : 0x00);
                    }

                    pixelColor = this.paletteMemory[(paletteIndex * 4) + bgPalette];

                    this.FetchTileData();
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

                                        if (i == 0 && this.spriteZeroOnCurrentScanline)
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
            else if (cycle <= 320)
            {
                if (cycle == 257 && this.showBackground)
                {
                    // Copy horizontal scroll data from temp register to VRAM addr
                    // v: ... .F ..... EDCBA = t: ... .F ..... EDCBA
                    this.vramAddr = (UInt16)((this.vramAddrTemp & HORIZONTAL_MASK) | (this.vramAddr & VERTICAL_MASK));
                }

                if (this.showSprites)
                {
                    // Pre-fetch sprite tiles for next scanline
                    this.FetchSpriteData();
                }
            }
            else if (cycle <= 336 && this.showBackground)
            {
                // Pre-fetch first two background tiles for next scanline
                this.FetchTileData();
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
        bool spriteZeroOnNextScanline;
        bool spriteZeroOnCurrentScanline;
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
                this.spriteZeroOnCurrentScanline = this.spriteZeroOnNextScanline;
                this.spriteZeroOnNextScanline = false;
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
                            if (this.scanline >= this.spriteEvalTemp && this.scanline < (this.spriteEvalTemp + (this.tallSprites ? 16 : 8)))
                            {
                                if (!this.spriteZeroOnNextScanline)
                                {
                                    this.spriteZeroOnNextScanline = this.cycle == 66;
                                }

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

        private byte attributeLoShiftRegister;
        private byte attributeHiShiftRegister;
        private byte nextAttributeLoValue;
        private byte nextAttributeHiValue;

        private void FetchTileData()
        {
            // Shift bitmap data registeres
            this.tileBitmapLoShiftRegister <<= 1;
            this.tileBitmapHiShiftRegister <<= 1;

            // Shift attribute data registers - bits are the same for all pixels in a tile, so
            //  two single-bit values serve as virtual bits 9-16 of the register.
            this.attributeLoShiftRegister <<= 1;
            this.attributeHiShiftRegister <<= 1;
            this.attributeLoShiftRegister |= this.nextAttributeLoValue;
            this.attributeHiShiftRegister |= this.nextAttributeHiValue;

            // 8 cycles required to read a complete set of tile data, since PPU reads take 2 cycles
            int step = (this.cycle - 1) & 0x07;

            switch (step)
            {
                case 1:
                    // Cycle 0 / 1: Read nametable data
                    this.nametableLatch = this.ppuBus.Read(0x2000 | (this.vramAddr & (NAMETABLE_MASK | COARSE_Y_MASK | COARSE_X_MASK)));
                    break;
                case 3:
                    // Cycle 2 / 3: Read attribute data
                    this.attributeTableLatch = this.ppuBus.Read(0x23C0 | (this.vramAddr & NAMETABLE_MASK) | ((this.vramAddr >> 4) & 0x38) | ((this.vramAddr >> 2) & 0x07));
                    break;
                case 5:
                    // Cycle 4 / 5: Read low byte of tile bitmap
                    this.tileBitmapLoLatch = this.ppuBus.Read(this.backgroundPatternTableAddr + (this.nametableLatch * 16 + ((this.vramAddr & FINE_Y_MASK) >> 12)));
                    break;
                case 7:
                    // Cycle 6 / 7: Read high byte of tile bitmap and shift into the registers
                    this.tileBitmapHiLatch = this.ppuBus.Read(this.backgroundPatternTableAddr + (this.nametableLatch * 16) + 8 + ((this.vramAddr & FINE_Y_MASK) >> 12));

                    // Shift bitmap data for next tile into registers
                    this.tileBitmapHiShiftRegister |= this.tileBitmapHiLatch;
                    this.tileBitmapLoShiftRegister |= this.tileBitmapLoLatch;

                    // Set attribute bits for next tile
                    // Attribute table bits are selected via a 4-to-1 mux driven by bits 2 and 7 of the VRAM address
                    //   v & 0x40    v & 0x02    Mask
                    //   0           0           00000011
                    //   0           1           00001100
                    //   1           0           00110000
                    //   1           1           11000000
                    byte quadrant = (byte)((((this.vramAddr & 0x40) == 0x40) ? 0x04 : 0x00) +
                                           (((this.vramAddr & 0x02) == 0x02) ? 0x02 : 0x00));
                    byte paletteIndex = (byte)((this.attributeTableLatch >> quadrant) & 0x03);

                    this.nextAttributeLoValue = (byte)(paletteIndex & 0x01);
                    this.nextAttributeHiValue = (byte)((paletteIndex & 0x02) >> 1);

                    // Increment coarse X for next tile
                    if ((this.vramAddr & COARSE_X_MASK) != COARSE_X_MASK)
                    {
                        this.vramAddr++;
                    }
                    else
                    {
                        // Overflow - reset coarse X to 0 and flip to next nametable
                        this.vramAddr &= (FINE_Y_MASK | NAMETABLE_MASK | COARSE_Y_MASK);
                        this.vramAddr ^= 0x0400;
                    }

                    // Increment fine Y after the last tile on the line
                    if (cycle == 256)
                    {
                        if ((this.vramAddr & FINE_Y_MASK) != FINE_Y_MASK)
                        {
                            this.vramAddr += 0x1000;
                        }
                        else
                        {
                            // Overflow - reset fine Y to 0 and increment coarse Y
                            this.vramAddr &= 0x8FFF;

                            int coarseY = (vramAddr & COARSE_Y_MASK) >> 5;
                            if (coarseY == 29)
                            {
                                // Normal overflow - reset coarse Y to 0 and flip to next nametable
                                coarseY = 0;
                                this.vramAddr ^= 0x0800;
                            }
                            else if (coarseY == 31)
                            {
                                // Super overflow - Coarse Y will never naturally exceed 29, but it's possible to manually
                                //  set it to a larger value.  In that case, the overflow doesn't cause a nametable switch.
                                coarseY = 0;
                            }
                            else
                            {
                                coarseY++;
                            }

                            this.vramAddr = (UInt16)((this.vramAddr & (FINE_Y_MASK | NAMETABLE_MASK | COARSE_X_MASK)) | (coarseY << 5));
                        }
                    }

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
                case 1:
                    // Cycle 0 / 1: Read and discard nametable garbage
                    this.ppuBus.Read(0x00);
                    break;

                case 2:
                    // Cycle 2: load attribute byte
                    this.spriteAttributeLatch[sprite] = this.secondaryOam[(sprite * 4) + 2];
                    break;

                case 3:
                    // Cycle 3: Read and discard nametable garbage, load X coordinate
                    this.ppuBus.Read(0x00);
                    this.spriteXPositionCounter[sprite] = this.secondaryOam[(sprite * 4) + 3];
                    break;

                case 5:
                case 7:
                    {
                        // Cycle 4 / 5: Read low byte of sprite bitmap
                        // Cycle 6 / 7: Read high byte of sprite bitmap
                        int tileIndex = this.secondaryOam[(sprite * 4) + 1];
                        int patternTableAddr = this.spritePatternTableAddr;
                        int tileRow = this.scanline - this.secondaryOam[sprite * 4];
                        bool vFlip = ((this.spriteAttributeLatch[sprite] & 0x80) == 0x80);

                        if (this.tallSprites)
                        {
                            patternTableAddr = ((tileIndex & 0x01) == 0x01) ? 0x1000 : 0x0000;
                            tileIndex &= 0xFE;

                            if (tileRow >= 8)
                            {
                                tileRow -= 8;

                                if (!vFlip)
                                {
                                    // 8x16 sprites also flip their tile order when flipped vertically - the
                                    //  higher-numbered tile goes on top and the lower-numbered tile underneath.
                                    tileIndex++;
                                }
                            }
                            else if (vFlip)
                            {
                                tileIndex++;
                            }
                        }

                        int tileAddress = patternTableAddr +
                            // Select tile
                            (tileIndex * 16) +

                            // Select plane
                            ((step == 5) ? 0 : 8) +

                            // Select row
                            (vFlip ? (7 - tileRow) : tileRow);

                        byte pattern = this.ppuBus.Read(tileAddress);

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

                        // Store in appropriate shift register
                        if (step == 5)
                        {
                            this.spriteBitmapLoShiftRegister[sprite] = pattern;
                        }
                        else
                        {
                            this.spriteBitmapHiShiftRegister[sprite] = pattern;
                        }
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
                value &= 0x3F;

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

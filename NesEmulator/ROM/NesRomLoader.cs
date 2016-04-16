using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using EmulatorCore.Components;
using EmulatorCore.Components.Memory;
using NesEmulator.ROM.Readers;

namespace NesEmulator.ROM
{
    internal class NesRomLoader : IEmulatorComponent, IMemoryMappedDevice
    {
        #region MEF Imports

        [ImportMany(typeof(IImageReaderFactory))]
        private IEnumerable<IImageReaderFactory> ImageReaderFactories { get; set; }

        #endregion

        #region Private Fields

        private IMemoryBus cpuBus;
        private IMemoryBus ppuBus;

        private Stream imageStream;
        private IImageReader reader;
        private byte[] prgRom = new byte[0x8000];
        private byte[] chrRom = new byte[0x2000];

        #endregion

        #region Constructor

        internal NesRomLoader(IMemoryBus cpuBus, IMemoryBus ppuBus)
        {
            this.cpuBus = cpuBus;
            this.ppuBus = ppuBus;

            // 2 16KB banks of PRG ROM at 0x8000 and 0xC000
            cpuBus.RegisterMappedDevice(this, 0x8000, 0xFFFF);

            // 8K bank of CHR ROM at 0x0000
            ppuBus.RegisterMappedDevice(this, 0x0000, 0x1FFF);
        }

        #endregion

        #region Public API

        public void LoadRom(string fileName)
        {
            if (this.imageStream != null)
            {
                this.imageStream.Close();
                this.imageStream.Dispose();
            }

            this.imageStream = File.OpenRead(fileName);
            this.reader = this.GetReader();

            if (reader == null)
            {
                throw new InvalidOperationException("Not a valid ROM!");
            }

            // Load initial ROM data
            this.reader.GetPrgRomBank(this.prgRom, 0, 0);
            this.reader.GetPrgRomBank(this.prgRom, this.reader.PrgRomBanks > 1 ? 1 : 0, 0x4000);
            
            this.reader.GetChrRomBank(this.chrRom, 0, 0);

            if (this.reader.Mirroring == MirroringMode.Horizontal)
            {
                // Nametable 1 = Nametable 3
                this.ppuBus.SetMirroringRange(0x2000, 0x23FF, 0x2800, 0x2BFF);

                // Nametable 2 = Nametable 4
                this.ppuBus.SetMirroringRange(0x2400, 0x27FF, 0x2C00, 0x2FFF);
            }
            else if (this.reader.Mirroring == MirroringMode.Vertical)
            {
                // Nametable 1 = Nametable 2
                this.ppuBus.SetMirroringRange(0x2000, 0x23FF, 0x2400, 0x27FF);

                // Nametable 3 = Nametable 4
                this.ppuBus.SetMirroringRange(0x2800, 0x2BFF, 0x2C00, 0x2FFF);
            }

            // 16 bit add test
            //byte[] test = new byte[] {
            //    0x18,             // CLC
            //    0xad, 0x20, 0x80, // LDA $8020
            //    0x6d, 0x23, 0x80, // ADC $8023
            //    0x8d, 0x26, 0x80, // STA $8026
            //    0xad, 0x21, 0x80, // LDA $8021
            //    0x6d, 0x24, 0x80, // ADC $8024
            //    0x8d, 0x27, 0x80, // STA $8027
            //    0x4c, 0x13, 0x80, // JMP $8013
            //    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // padding
            //    0xd2, 0x04, 0x00, // .dw 04d2
            //    0x2e, 0x16, 0x00  // .dw 162e 
            //};

            // BRK / IRQ test
            //prgRom[0x7FFE] = 0x06;
            //prgRom[0x7FFF] = 0x80;
            //byte[] test = new byte[] {
            //    0x58,               // SEI
            //    0x00,               // BRK
            //    0xEA,               // NOP
            //    0x4c, 0x03, 0x80,   // JMP $8003
            //    0xa9, 0x42,         // LDA #42
            //    0x40,               // RTI
            //};

            //Array.Copy(test, prgRom, test.Length);
        }

        #endregion

        private IImageReader GetReader()
        {
            IImageReader reader;

            foreach (IImageReaderFactory factory in this.ImageReaderFactories)
            {
                reader = factory.CreateImageReader(this.imageStream);
                if (reader.IsValidImage)
                {
                    return reader;
                }
            }

            return null;
        }

        #region IEmulatorComponent Implementation

        public string Name
        {
            get { return "NES ROM Loader"; }
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                return prgRom[address - 0x8000];
            }
            else if (address >= 0x0000 && address <= 0x1FFF)
            {
                return chrRom[address];
            }

            throw new InvalidOperationException();
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            // TODO: Mappers
        }

        #endregion
    }
}

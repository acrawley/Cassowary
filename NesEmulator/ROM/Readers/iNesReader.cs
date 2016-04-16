using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesEmulator.ROM.Readers
{
    [Export(typeof(IImageReaderFactory))]
    internal class iNesReaderFactory : IImageReaderFactory
    {
        #region IImageReaderFactory Implementation

        IImageReader IImageReaderFactory.CreateImageReader(Stream imageStream)
        {
            return new iNesReader(imageStream);
        }

        #endregion
    }

    internal class iNesReader : IImageReader
    {
        private const int PrgRomBankSize = 1024 * 16;
        private const int ChrRomBankSize = 1024 * 8;

        private Stream imageStream;
        byte[] header;

        internal iNesReader(Stream imageStream)
        {
            this.imageStream = imageStream;
            this.header = new byte[16];

            imageStream.Read(this.header, 0, 16);
            imageStream.Seek(-16, SeekOrigin.Current);
        }

        private void EnsureValidImage()
        {
            if (!this.IsValidImage)
            {
                throw new InvalidDataException("Not a valid iNES image!");
            }
        }

        public void GetPrgRomBank(byte[] buffer, int bank, int offset)
        {
            this.EnsureValidImage();

            if (bank >= this.PrgRomBanks)
            {
                throw new ArgumentOutOfRangeException("bank");
            }

            int romOffset = 16 + 
                            (this.HasTrainer ? 512 : 0) + 
                            PrgRomBankSize * bank;
            this.imageStream.Seek(romOffset, SeekOrigin.Begin);
            this.imageStream.Read(buffer, offset, PrgRomBankSize);
        }

        public void GetChrRomBank(byte[] buffer, int bank, int offset)
        {
            this.EnsureValidImage();

            if (bank >= this.ChrRomBanks)
            {
                throw new ArgumentOutOfRangeException("bank");
            }

            byte[] prgRomBank = new byte[ChrRomBankSize];

            int romOffset = 16 + 
                            (this.HasTrainer ? 512 : 0) + 
                            (PrgRomBankSize * this.PrgRomBanks) + 
                            ChrRomBankSize * bank;
            this.imageStream.Seek(romOffset, SeekOrigin.Begin);
            this.imageStream.Read(buffer, offset, ChrRomBankSize);
        }

        #region IImageReader Implementation

        public bool IsValidImage
        {
            get
            {
                // Header should start with string "NES\x1a" and not have the bits set
                //  in Flags7 indicating that it's in iNES 2.0 format
                return header[0] == 0x4e &&
                       header[1] == 0x45 &&
                       header[2] == 0x53 &&
                       header[3] == 0x1a &&
                       (header[7] & 0x0C) != 0x08;
            }
        }

        private int _prgRomBanks = -1;
        public int PrgRomBanks
        {
            get
            {
                this.EnsureValidImage();

                if (this._prgRomBanks == -1)
                {
                    this._prgRomBanks = header[4];
                }

                return this._prgRomBanks;
            }
        }

        private int _prgRamBanks = -1;
        public int PrgRamBanks
        {
            get
            {
                this.EnsureValidImage();

                if (this._prgRamBanks == -1)
                {
                    this._prgRamBanks = this.header[8];
                }

                return this._prgRamBanks;
            }
        }

        private int _chrRomBanks = -1;
        public int ChrRomBanks
        {
            get
            {
                this.EnsureValidImage();

                if (this._chrRomBanks == -1)
                {
                    this._chrRomBanks = this.header[5];
                }

                return this._chrRomBanks;
            }
        }

        private MirroringMode _mirroring = MirroringMode.Unknown;
        public MirroringMode Mirroring
        {
            get
            {
                this.EnsureValidImage();

                if (this._mirroring == MirroringMode.Unknown)
                {
                    switch (this.header[6] & 0x09)
                    {
                        case 0x00:
                            this._mirroring = MirroringMode.Horizontal;
                            break;

                        case 0x01:
                            this._mirroring = MirroringMode.Vertical;
                            break;

                        case 0x08:
                            this._mirroring = MirroringMode.FourScreen;
                            break;

                        default:
                            Debug.Fail(String.Format(CultureInfo.CurrentCulture, "Unexpected mirroring mode, header.Flags6 = 0x{0:x}", this.header[6]));
                            break;
                    }
                }

                return this._mirroring;
            }
        }

        public bool HasSaveRam
        {
            get
            {
                this.EnsureValidImage();

                return (this.header[6] & 0x02) == 0x02;
            }
        }

        public bool HasTrainer
        {
            get
            {
                this.EnsureValidImage();

                return (this.header[6] & 0x04) == 0x04;
            }
        }

        private int _mapper = -1;
        public int Mapper
        {
            get
            {
                this.EnsureValidImage();

                if (this._mapper == -1)
                {
                    int mapperLowNibble = (this.header[6] & 0xF0) >> 4;

                    // Early iNES dumps often had garbage in the header reserved bits - if they're not 0,
                    //  assume only the low nibble of the mapper number is valid.
                    if (this.header[12] == 0 &&
                        this.header[13] == 0 &&
                        this.header[14] == 0 &&
                        this.header[15] == 0)
                    {
                        this._mapper = mapperLowNibble + (this.header[7] & 0xF0);
                    }

                    this._mapper = mapperLowNibble;
                }

                return this._mapper;
            }
        }

        public bool IsVsUnisystem
        {
            get
            {
                this.EnsureValidImage();

                return (this.header[7] & 0x01) == 0x01;
            }
        }

        public bool IsPlayChoice10
        {
            get
            {
                this.EnsureValidImage();

                return (this.header[7] & 0x02) == 0x02;
            }
        }

        #endregion
    }
}

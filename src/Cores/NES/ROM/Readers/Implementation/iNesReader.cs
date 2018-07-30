using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Core.Nes.ROM.Readers.Implementation
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
        private const int PrgRamBankSize = 1024 * 8;
        private const int ChrRomBankSize = 1024 * 8;

        private Stream imageStream;
        private byte[] header;
        private bool hasBrokenHeader;

        internal iNesReader(Stream imageStream)
        {
            this.imageStream = imageStream;
            this.header = new byte[16];

            imageStream.Read(this.header, 0, 16);
            imageStream.Seek(-16, SeekOrigin.Current);

            // Early iNES dumps often contained garbage starting at header byte 7.  If reserved bytes
            //  12-15 are not all 0's, assume nothing in the header past byte 6 is valid.
            this.hasBrokenHeader = !(this.header[12] == 0 &&
                                     this.header[13] == 0 &&
                                     this.header[14] == 0 &&
                                     this.header[15] == 0);
        }

        private void EnsureValidImage()
        {
            if (!this.IsValidImage)
            {
                throw new InvalidDataException("Not a valid iNES image!");
            }
        }

        public void GetPrgRom(int sourceIndex, byte[] destination, int destinationIndex, int length)
        {
            this.EnsureValidImage();

            if (sourceIndex >= this.PrgRomSize)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            if (sourceIndex + length > this.PrgRomSize)
            {
                throw new ArgumentOutOfRangeException(nameof(length)); 
            }

            int romOffset = 16 +
                            (this.HasTrainer ? 512 : 0) +
                            sourceIndex;
            this.imageStream.Seek(romOffset, SeekOrigin.Begin);
            if (this.imageStream.Read(destination, destinationIndex, length) != length)
            {
                throw new InvalidOperationException();
            }
        }

        public void GetChrRom(int sourceIndex, byte[] destination, int destinationIndex, int length)
        {
            this.EnsureValidImage();

            if (sourceIndex >= this.ChrRomSize)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            if (sourceIndex + length > this.ChrRomSize)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            int romOffset = 16 +
                            (this.HasTrainer ? 512 : 0) +
                            this.PrgRomSize +
                            sourceIndex;
            this.imageStream.Seek(romOffset, SeekOrigin.Begin);
            if (this.imageStream.Read(destination, destinationIndex, length) != length)
            {
                throw new InvalidOperationException();
            }
        }

        #region IImageReader Implementation

        public bool IsValidImage
        {
            get
            {
                // Header should start with string "NES\x1a" and not have the bits set
                //  in Flags7 indicating that it's in NES 2.0 format
                return header[0] == 0x4e &&
                       header[1] == 0x45 &&
                       header[2] == 0x53 &&
                       header[3] == 0x1a &&
                       (header[7] & 0x0C) != 0x08;
            }
        }

        private int _prgRomSize = -1;
        public int PrgRomSize
        {
            get
            {
                this.EnsureValidImage();

                if (this._prgRomSize == -1)
                {
                    this._prgRomSize = PrgRomBankSize * header[4];
                }

                return this._prgRomSize;
            }
        }

        private int _prgRamSize = -1;
        public int PrgRamSize
        {
            get
            {
                this.EnsureValidImage();

                if (this._prgRamSize == -1)
                {
                    if (this.hasBrokenHeader)
                    {
                        this._prgRamSize = 0;
                    }
                    else
                    {
                        this._prgRamSize = PrgRamBankSize * this.header[8];
                    }
                }

                return this._prgRamSize;
            }
        }

        private int _chrRomSize = -1;
        public int ChrRomSize
        {
            get
            {
                this.EnsureValidImage();

                if (this._chrRomSize == -1)
                {
                    this._chrRomSize = ChrRomBankSize * this.header[5];
                }

                return this._chrRomSize;
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
                    if ((this.header[6] & 0x08) == 0x08)
                    {
                        this._mirroring = MirroringMode.FourScreen;
                    }
                    else if ((this.header[6] & 0x01) == 0x01)
                    {
                        this._mirroring = MirroringMode.Vertical;
                    }
                    else if ((this.header[6] & 0x01) == 0x00)
                    {
                        this._mirroring = MirroringMode.Horizontal;
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
                    this._mapper = (this.header[6] & 0xF0) >> 4;

                    // Early iNES dumps often had garbage in the header reserved bits - if so,
                    //  assume only the low nibble of the mapper number is valid.
                    if (!this.hasBrokenHeader)
                    {
                        this._mapper = this._mapper + (this.header[7] & 0xF0);
                    }
                }

                return this._mapper;
            }
        }

        public int SubMapper
        {
            get { return 0; }
        }

        public bool IsVsUnisystem
        {
            get
            {
                this.EnsureValidImage();

                if (this.hasBrokenHeader)
                {
                    return false;
                }

                return (this.header[7] & 0x01) == 0x01;
            }
        }

        public bool IsPlayChoice10
        {
            get
            {
                this.EnsureValidImage();

                if (this.hasBrokenHeader)
                {
                    return false;
                }

                return (this.header[7] & 0x02) == 0x02;
            }
        }

        #endregion
    }
}

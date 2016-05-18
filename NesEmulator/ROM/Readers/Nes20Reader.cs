using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesEmulator.ROM.Readers
{
    [Export(typeof(IImageReaderFactory))]
    internal class Nes20ReaderFactory : IImageReaderFactory
    {
        IImageReader IImageReaderFactory.CreateImageReader(Stream imageStream)
        {
            return new Nes20Reader(imageStream);
        }
    }

    class Nes20Reader : IImageReader
    {
        private const int PrgRomBankSize = 1024 * 16;
        private const int ChrRomBankSize = 1024 * 8;
        private static readonly int[] PrgRamSizes = new int[] {
            0, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576 };

        private Stream imageStream;
        byte[] header;

        internal Nes20Reader(Stream imageStream)
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
                throw new InvalidDataException("Not a valid NES 2.0 image!");
            }
        }

        #region IImageReader Implementation

        public bool IsValidImage
        {
            get
            {
                // Header should start with string "NES\x1a" and have the bits set
                //  in Flags7 indicating that it's in NES 2.0 format
                return header[0] == 0x4e &&
                       header[1] == 0x45 &&
                       header[2] == 0x53 &&
                       header[3] == 0x1a &&
                       (header[7] & 0x0C) == 0x08;
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
                    this._prgRomSize = PrgRomBankSize * (this.header[4] | ((this.header[9] & 0x0F) << 8));
                }

                return this._prgRomSize;
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
                    this._chrRomSize = ChrRomBankSize * (this.header[5] | ((this.header[9] & 0xF0) << 4));
                }

                return this._chrRomSize;
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
                    this._prgRamSize =
                        PrgRamSizes[this.header[10] & 0x0F] +
                        PrgRamSizes[(this.header[10] & 0xF0) >> 4];
                }

                return this._prgRamSize;
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

                return ((this.header[6] & 0x02) == 0x02);
            }
        }

        public bool HasTrainer
        {
            get
            {
                this.EnsureValidImage();

                return ((this.header[6] & 0x04) == 0x04);
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
                    this._mapper =
                        ((this.header[6] & 0xF0) >> 4) |
                         (this.header[7] & 0xF0) |
                        ((this.header[8] & 0x0F) << 8);
                }

                return this._mapper;
            }
        }

        private int _subMapper = -1;
        public int SubMapper
        {
            get
            {
                this.EnsureValidImage();

                if (this._subMapper == -1)
                {
                    this._subMapper = (this.header[8] & 0xF0) >> 4;
                }

                return this._subMapper;
            }
        }

        public bool IsVsUnisystem
        {
            get
            {
                this.EnsureValidImage();

                return ((this.header[7] & 0x01) == 0x01);
            }
        }

        public bool IsPlayChoice10
        {
            get
            {
                this.EnsureValidImage();

                return ((this.header[7] & 0x02) == 0x02);
            }
        }

        #endregion
    }
}

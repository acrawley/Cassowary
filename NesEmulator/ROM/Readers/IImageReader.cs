using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesEmulator.ROM.Readers
{
    internal enum MirroringMode
    {
        Unknown,
        Horizontal,
        Vertical,
        FourScreen
    }

    internal interface IImageReader
    {
        bool IsValidImage { get; }

        int PrgRomSize { get; }
        int PrgRamSize { get; }
        int ChrRomSize { get; }

        void GetPrgRom(int sourceIndex, byte[] destination, int destinationIndex, int length);
        void GetChrRom(int sourceIndex, byte[] destination, int destinationIndex, int length);

        MirroringMode Mirroring { get; }
        bool HasSaveRam { get; }
        bool HasTrainer { get; }
        int Mapper { get; }
        int SubMapper { get; }

        bool IsVsUnisystem { get; }
        bool IsPlayChoice10 { get; }

    }
}

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

        int PrgRomBanks { get; }
        int PrgRamBanks { get; }
        int ChrRomBanks { get; }

        void GetPrgRomBank(byte[] data, int bank, int offset);
        void GetChrRomBank(byte[] data, int bank, int offset);

        MirroringMode Mirroring { get; }
        bool HasSaveRam { get; }
        bool HasTrainer { get; }
        int Mapper { get; }

        bool IsVsUnisystem { get; }
        bool IsPlayChoice10 { get; }

    }
}

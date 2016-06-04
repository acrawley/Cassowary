using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.Memory;
using EmulatorCore.Memory;
using NesEmulator.ROM.Readers;

namespace NesEmulator.ROM.Mappers.Implementation
{
    internal abstract class MapperBase : IMapper
    {
        #region Private Fields

        private bool isDisposed = false;

        private List<IMemoryMapping> cpuMappings;
        private List<IMemoryMirroring> cpuMirrorings;

        private List<IMemoryMapping> ppuMappings;
        private List<IMemoryMirroring> ppuMirrorings;

        private Memory nametableA;
        private Memory nametableB;

        #endregion

        #region Constructor

        protected MapperBase(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus)
        {
            this.Reader = reader;
            this.CpuBus = cpuBus;
            this.PpuBus = ppuBus;

            this.cpuMappings = new List<IMemoryMapping>();
            this.cpuMirrorings = new List<IMemoryMirroring>();

            this.ppuMappings = new List<IMemoryMapping>();
            this.ppuMirrorings = new List<IMemoryMirroring>();
        }

        #endregion

        #region Properties

        protected IImageReader Reader { get; private set; }
        protected IMemoryBus CpuBus { get; private set; }
        protected IMemoryBus PpuBus { get; private set; }

        #endregion

        #region Memory Helpers

        protected void SetNametableMirroring(MirroringMode mode)
        {
            switch (mode)
            {
                case MirroringMode.Horizontal:
                    // Horizontal mirroring: A A
                    //                       B B
                    this.MapNametableA(0x2000);
                    this.MirrorPpuRange(0x2000, 0x23FF, 0x2400, 0x27FF);

                    this.MapNametableB(0x2800);
                    this.MirrorPpuRange(0x2800, 0x2BFF, 0x2C00, 0x2FFF);

                    break;

                case MirroringMode.Vertical:
                    // Vertical mirroring: A B
                    //                     A B
                    this.MapNametableA(0x2000);
                    this.MirrorPpuRange(0x2000, 0x23FF, 0x2800, 0x2BFF);

                    this.MapNametableB(0x2400);
                    this.MirrorPpuRange(0x2400, 0x27FF, 0x2C00, 0x2FFF);

                    break;

                case MirroringMode.SingleScreenA:
                    // One screen, mapped to lower nametable: A A
                    //                                        A A
                    this.MapNametableA(0x2000);
                    this.MapNametableB(-1);
                    this.MirrorPpuRange(0x2000, 0x23FF, 0x2400, 0x2FFF);
                    break;

                case MirroringMode.SingleScreenB:
                    // One screen, mapped to upper nametable: B B
                    //                                        B B
                    this.MapNametableA(-1);
                    this.MapNametableB(0x2400);
                    this.MirrorPpuRange(0x2400, 0x27FF, 0x2000, 0x23FF);
                    this.MirrorPpuRange(0x2400, 0x27FF, 0x2800, 0x2FFF);
                    break;

                default:
                    throw new NotImplementedException(String.Format(CultureInfo.CurrentCulture, "Nametable mirroring mode '{0}' is not implemented!", mode));
            }
        }

        protected void MapNametableA(int address)
        {
            if (this.nametableA == null)
            {
                this.nametableA = new Memory("Nametable A", this.PpuBus, address, 0x400);
            }
            else
            {
                this.nametableA.Remap(address);
            }
        }

        protected void MapNametableB(int address)
        {
            if (this.nametableB == null)
            {
                this.nametableB = new Memory("Nametable B", this.PpuBus, address, 0x400);
            }
            else
            {
                this.nametableB.Remap(address);
            }
        }

        protected IMemoryMapping MapCpuRange(IMemoryMappedDevice device, int startAddress, int endAddress)
        {
            IMemoryMapping mapping = this.CpuBus.RegisterMappedDevice(device, startAddress, endAddress);

            this.cpuMappings.Add(mapping);

            return mapping;
        }

        protected IMemoryMapping MapPpuRange(IMemoryMappedDevice device, int startAddress, int endAddress)
        {
            IMemoryMapping mapping = this.PpuBus.RegisterMappedDevice(device, startAddress, endAddress);

            this.ppuMappings.Add(mapping);

            return mapping;
        }

        protected IMemoryMirroring MirrorCpuRange(int sourceStartAddress, int sourceEndAddress, int mirrorStartAddress, int mirrorEndAddress)
        {
            IMemoryMirroring mirroring = this.CpuBus.SetMirroringRange(sourceStartAddress, sourceEndAddress, mirrorStartAddress, mirrorEndAddress);

            this.cpuMirrorings.Add(mirroring);

            return mirroring;
        }

        protected IMemoryMirroring MirrorPpuRange(int sourceStartAddress, int sourceEndAddress, int mirrorStartAddress, int mirrorEndAddress)
        {
            IMemoryMirroring mirroring = this.PpuBus.SetMirroringRange(sourceStartAddress, sourceEndAddress, mirrorStartAddress, mirrorEndAddress);

            this.ppuMirrorings.Add(mirroring);

            return mirroring;
        }

        protected void DisposeMappings()
        {
            foreach (IMemoryMapping cpuMapping in this.cpuMappings)
            {
                this.CpuBus.RemoveMapping(cpuMapping);
            }

            foreach (IMemoryMapping ppuMapping in this.ppuMappings)
            {
                this.PpuBus.RemoveMapping(ppuMapping);
            }

            this.cpuMappings.Clear();
            this.ppuMappings.Clear();

            if (this.nametableA != null)
            {
                this.nametableA.Remap(-1);
            }

            if (this.nametableB != null)
            {
                this.nametableB.Remap(-1);
            }
        }

        protected void DisposeMirrorings()
        {
            foreach (IMemoryMirroring cpuMirroring in this.cpuMirrorings)
            {
                this.CpuBus.RemoveMirroring(cpuMirroring);
            }

            foreach (IMemoryMirroring ppuMirroring in this.ppuMirrorings)
            {
                this.PpuBus.RemoveMirroring(ppuMirroring);
            }

            this.cpuMirrorings.Clear();
            this.ppuMirrorings.Clear();
        }

        #endregion

        #region IEmulatorComponent Implementation

        public abstract string Name { get; }

        #endregion

        #region IDisposable Implementation

        protected abstract void Dispose();

        void IDisposable.Dispose()
        {
            if (!isDisposed)
            {
                this.Dispose();
                isDisposed = true;
            }
        }

        #endregion
    }
}

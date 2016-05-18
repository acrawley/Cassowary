using System.Collections.Generic;
using EmulatorCore.Components.Core;

namespace EmulatorCore.Components.Memory
{
    public interface IMemoryBus : IEmulatorComponent
    {
        byte Read(int address);
        void Write(int address, byte value);


        IEnumerable<IMemoryMirroring> Mirrorings { get; }

        IMemoryMirroring SetMirroringRange(int sourceStartAddress, int sourceEndAddress, int mirrorStartAddress, int mirrorEndAddress);

        void RemoveMirroring(IMemoryMirroring mirroring);


        IEnumerable<IMemoryMapping> Mappings { get; }

        IMemoryMapping RegisterMappedDevice(IMemoryMappedDevice device, int startAddress, int endAddress);

        void RemoveMapping(IMemoryMapping mapping);
    }
}

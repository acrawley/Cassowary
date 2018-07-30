using System.Collections.Generic;
using Cassowary.Shared.Components.Core;
using Cassowary.Shared.Components.Debugging;

namespace Cassowary.Shared.Components.Memory
{
    public interface IMemoryBus : IComponentWithBreakpoints
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

using System.Collections.Generic;

namespace EmulatorCore.Components.Memory
{
    public interface IMemoryBus : IEmulatorComponent
    {
        byte Read(int address);
        void Write(int address, byte value);

        void SetMirroringRange(int sourceStartAddress, int sourceEndAddress, int mirrorStartAddress, int mirrorEndAddress);

        IMemoryMapping RegisterMappedDevice(IMemoryMappedDevice device, int address);
        IMemoryMapping RegisterMappedDevice(IMemoryMappedDevice device, int startAddress, int endAddress);

        void RemoveMapping(IMemoryMapping mapping);

        IEnumerable<IMemoryMapping> Mappings { get; }
    }
}

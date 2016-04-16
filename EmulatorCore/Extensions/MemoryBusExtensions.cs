using System;
using EmulatorCore.Components.Memory;

namespace EmulatorCore.Extensions
{
    public static class MemoryBusExtensions
    {
        public static UInt16 ReadUInt16LE(this IMemoryBus bus, int address)
        {
            return (UInt16)(bus.Read(address) | (bus.Read(address + 1) << 8));
        }
    }
}

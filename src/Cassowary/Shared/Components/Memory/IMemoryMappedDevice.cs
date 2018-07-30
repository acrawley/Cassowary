using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Components.Memory
{
    public interface IMemoryMappedDevice
    {
        byte Read(int address);
        void Write(int address, byte value);
    }
}

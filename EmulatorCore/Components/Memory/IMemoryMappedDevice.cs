using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.Memory
{
    public interface IMemoryMappedDevice
    {
        byte Read(int address);
        void Write(int address, byte value);
    }
}

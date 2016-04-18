using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Input;

namespace NesEmulator.Input.Devices
{
    internal interface INesInputDevice : IInputDevice
    {
        byte Read(out bool done);

        void Strobe();
    }
}

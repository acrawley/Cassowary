using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Input;

namespace Cassowary.Core.Nes.Input.Devices
{
    internal interface INesInputDevice : IInputDevice
    {
        byte Read(out bool done);

        void Strobe();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Components.Core
{
    public interface IComponentWithClock : IEmulatorComponent
    {
        void Tick();
    }
}

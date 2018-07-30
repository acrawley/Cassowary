using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Core;

namespace Cassowary.Shared.Components.Input
{
    public interface IInputDevice : IEmulatorComponent
    {
        IEnumerable<IInputElement> InputElements { get; }
    }
}

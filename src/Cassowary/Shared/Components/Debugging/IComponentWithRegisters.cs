using System.Collections.Generic;
using Cassowary.Shared.Components.Core;

namespace Cassowary.Shared.Components.Debugging
{
    public interface IComponentWithRegisters : IEmulatorComponent
    {
        IEnumerable<IRegister> Registers { get; }
    }
}

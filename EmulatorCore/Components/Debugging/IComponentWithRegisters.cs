using System.Collections.Generic;
using EmulatorCore.Components.Core;

namespace EmulatorCore.Components.Debugging
{
    public interface IComponentWithRegisters : IEmulatorComponent
    {
        IEnumerable<IRegister> Registers { get; }
    }
}

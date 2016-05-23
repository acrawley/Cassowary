using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.Core
{
    public interface IComponentWithRegisters : IEmulatorComponent
    {
        IEnumerable<IRegister> Registers { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.CPU
{
    public interface IProcessorInterrupt
    {
        string Name { get; }
        int Priority { get; }
        IDisposable Assert();
    }
}

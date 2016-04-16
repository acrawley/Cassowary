using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.CPU
{
    public interface IProcessorRegister
    {
        string Name { get; }
        string Description { get; }
        string FormattedValue { get; }
        int Width { get; }
        int Value { get; set; }
    }
}

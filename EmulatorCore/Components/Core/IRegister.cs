using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.Core
{
    public interface IRegister
    {
        string Name { get; }
        string Description { get; }
        string FormattedValue { get; }
        int Width { get; }
        int Value { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.CPU
{
    public interface IInstruction
    {
        string Mnemonic { get; }
        string Operands { get; }
        string OperandsDetail { get; }
    }
}

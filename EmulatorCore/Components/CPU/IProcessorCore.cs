using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.CPU
{
    public interface IProcessorCore : IEmulatorComponent
    {
        /// <summary>
        /// Executes the next instruction
        /// </summary>
        /// <returns>Number of machine cycles elapsed</returns>
        int Step();

        void Reset();

        IEnumerable<byte> GetInstructionBytes(int address);

        IInstruction DecodeInstruction(IEnumerable<byte> instructionBytes);

        IEnumerable<IProcessorInterrupt> Interrupts { get; }

        IEnumerable<IProcessorRegister> Registers { get; }
    }
}

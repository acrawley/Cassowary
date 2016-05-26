using System.Collections.Generic;
using EmulatorCore.Components.Debugging;

namespace EmulatorCore.Components.CPU
{
    public interface IProcessorCore : IComponentWithRegisters
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
    }
}

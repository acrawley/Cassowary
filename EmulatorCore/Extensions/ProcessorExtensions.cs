using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.CPU;

namespace EmulatorCore.Extensions
{
    public static class ProcessorExtensions
    {
        public static IProcessorRegister GetRegisterByName(this IProcessorCore cpu, string registerName)
        {
            return cpu.Registers.FirstOrDefault(r => String.Equals(r.Name, registerName, StringComparison.Ordinal));
        }

        public static IProcessorInterrupt GetInterruptByName(this IProcessorCore cpu, string interruptName)
        {
            return cpu.Interrupts.FirstOrDefault(i => String.Equals(i.Name, interruptName, StringComparison.Ordinal));
        }
    }
}

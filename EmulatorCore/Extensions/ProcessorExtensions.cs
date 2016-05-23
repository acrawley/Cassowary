using System;
using System.Linq;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.CPU;

namespace EmulatorCore.Extensions
{
    public static class ProcessorExtensions
    {
        public static IRegister GetRegisterByName(this IComponentWithRegisters component, string registerName)
        {
            return component.Registers.FirstOrDefault(r => String.Equals(r.Name, registerName, StringComparison.Ordinal));
        }

        public static IProcessorInterrupt GetInterruptByName(this IProcessorCore cpu, string interruptName)
        {
            return cpu.Interrupts.FirstOrDefault(i => String.Equals(i.Name, interruptName, StringComparison.Ordinal));
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using EmulatorCore.Components;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Memory;
using EmulatorCore.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NesTests
{
    [TestClass]
    public class CpuTests : EmulatorTestBase
    {
        [TestMethod]
        public void RunCpuTest()
        {
            IEmulator nesEmulator = this.GetInstance();
            nesEmulator.LoadFile(@"TestResources\CpuTests\nestest.nes");

            List<string> expectedOutput = new List<string>(File.ReadAllLines(@"TestResources\CpuTests\nestest.expected"));

            IProcessorCore cpu = (IProcessorCore)nesEmulator.Components.First(c => c.Name == "Ricoh 2A03 CPU");
            IComponentWithRegisters ppu = (IComponentWithRegisters)nesEmulator.Components.First(c => c.Name == "Ricoh 2C02 PPU");
            IComponentWithClock ppuTick = (IComponentWithClock)ppu;

            IMemoryBus cpuBus = (IMemoryBus)nesEmulator.Components.First(c => c.Name == "CPU Bus");

            // Jump to automated test entry point
            cpu.GetRegisterByName("PC").Value = 0xC000;

            // Set initial state
            cpu.GetRegisterByName("S").Value = 0xFD;
            ppu.GetRegisterByName("cycle").Value = 0;
            ppu.GetRegisterByName("scanline").Value = 241;

            foreach (string outputLine in expectedOutput)
            {
                string state = this.DumpEmulatorState(cpu, ppu, cpuBus);

                if (!outputLine.StartsWith("#") && 
                    !String.Equals(state, outputLine, StringComparison.Ordinal))
                {
                    Debug.WriteLine("**** {0}", (object)state);
                    Debug.WriteLine("Exp: {0}", (object)outputLine);
                    Assert.Fail("Output mismatch!");
                }

                Debug.WriteLine("     {0}", (object)state);

                int cycles = cpu.Step();
                if (cycles < 0)
                {
                    Assert.Fail("CPU step failed!");
                }

                for (int i = 0; i < cycles; i++)
                {
                    // PPU ticks 3 times during each CPU cycle
                    ppuTick.Tick();
                    ppuTick.Tick();
                    ppuTick.Tick();
                }
            }
        }

        // Dump current CPU/PPU state in Nintendulator-ish format
        private string DumpEmulatorState(IProcessorCore cpu, IComponentWithRegisters ppu, IMemoryBus cpuBus)
        {
            UInt16 PC = (UInt16)cpu.GetRegisterByName("PC").Value;
            byte A = (byte)cpu.GetRegisterByName("A").Value;
            byte X = (byte)cpu.GetRegisterByName("X").Value;
            byte Y = (byte)cpu.GetRegisterByName("Y").Value;
            string P = cpu.GetRegisterByName("P").FormattedValue;
            byte S = (byte)cpu.GetRegisterByName("S").Value;

            int cycle = ppu.GetRegisterByName("cycle").Value;
            int scanline = ppu.GetRegisterByName("scanline").Value;

            StringBuilder state = new StringBuilder();
            state.AppendFormat("{0:X4}  ", PC);

            IEnumerable<byte> instructionBytes = cpu.GetInstructionBytes(PC);

            if (!instructionBytes.Any())
            {
                state.AppendFormat("UNKNOWN: {0:X2}", cpuBus.Read(PC));
                return state.ToString();
            }

            IInstruction instruction = cpu.DecodeInstruction(instructionBytes);

            state.AppendFormat("{0,-8} {1,4} {2} {3}",
                String.Join(" ", instructionBytes.Select(b => String.Format("{0:X2}", b))),
                instruction.Mnemonic,
                instruction.Operands,
                instruction.OperandsDetail);

            state.Append(' ', 48 - state.Length);

            state.AppendFormat("A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} SP:{4:X2} CYC:{5,3} SL:{6}",
                A, X, Y, P, S, cycle, scanline);

            return state.ToString();
        }
    }
}

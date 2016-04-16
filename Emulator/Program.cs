using EmulatorCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Memory;
using EmulatorCore.Extensions;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace Emulator
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            EmulatorApp emulator = new EmulatorApp();

            Container.SatisfyImportsOnce(emulator);

            return emulator.Run();
        }

        private static CompositionContainer _container;
        private static CompositionContainer Container
        {
            get
            {
                if (_container == null)
                {
                    _container = InitializeComponentModel();
                }

                return _container;
            }
        }

        private static CompositionContainer InitializeComponentModel()
        {
            AggregateCatalog catalog = new AggregateCatalog();

            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Program).Assembly));
            catalog.Catalogs.Add(new DirectoryCatalog(Path.GetDirectoryName(typeof(Program).Assembly.Location)));

            CompositionContainer container = new CompositionContainer(catalog);

            // Add the container to itself so other components can do additional composition
            container.ComposeExportedValue(container);

            return container;
        }
    }

    //internal class Emulator
    //{
    //    [ImportMany(typeof(IEmulatorFactory))]
    //    private IEnumerable<Lazy<IEmulatorFactory, IEmulatorFactoryMetadata>> EmulatorFactories { get; set; }

    //    private IEmulator GetInstance()
    //    {
    //        IEmulatorFactory factory = this.EmulatorFactories.SingleOrDefault(e => e.Metadata.EmulatorName == "NES").Value;
    //        return factory.CreateInstance();
    //    }

    //    public void Run()
    //    {
    //        //Task.Run(() => this.RunThread());
    //        this.RunThread();
    //    }

    //    private void RunThread()
    //    {
    //        IEmulator nesEmulator = this.GetInstance();

    //        //nesEmulator.LoadFile(@"d:\emulators\nes\roms\smb.nes");
    //        //nesEmulator.LoadFile(@"D:\Emulators\NES\roms\tests\instr_test-v4\rom_singles\01-basics.nes");
    //        nesEmulator.LoadFile(@"D:\Emulators\NES\roms\tests\nestest.nes");
    //        nesEmulator.Run();
    //    }

    //    public void RunCpuTest()
    //    {
    //        IEmulator nesEmulator = this.GetInstance();
    //        nesEmulator.LoadFile(@"D:\Emulators\NES\roms\tests\nestest.nes");

    //        List<string> expectedOutput = new List<string>(File.ReadAllLines(@"D:\Emulators\NES\roms\tests\nestest.log"));

    //        IProcessorCore cpu = (IProcessorCore)nesEmulator.Components.First(c => c.Name == "Ricoh 2A03 CPU");
    //        IMemoryBus cpuBus = (IMemoryBus)nesEmulator.Components.First(c => c.Name == "CPU Bus");

    //        cpu.Registers.First(r => r.Name == "S").Value = 0xFD;

    //        // Jump to automated test entry point
    //        IProcessorRegister PC = cpu.GetRegisterByName("PC");
    //        PC.Value = 0xC000;

    //        foreach (string outputLine in expectedOutput)
    //        {
    //            string state = this.DumpCPUState(cpu, cpuBus);
    //            string expected = outputLine.Substring(0, 79);

    //            if (!String.Equals(state, expected, StringComparison.Ordinal))
    //            {
    //                Debug.WriteLine("**** {0}", (object)state);
    //                Debug.WriteLine("Exp: {0}", (object)expected);
    //                break;
    //            }

    //            Debug.WriteLine("     {0}", (object)state);

    //            if (cpu.Step() < 0)
    //            {
    //                break;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Dump current CPU state in Nintendulator format
    //    /// </summary>
    //    /// <returns>CPU state</returns>
    //    private string DumpCPUState(IProcessorCore cpu, IMemoryBus cpuBus)
    //    {
    //        UInt16 PC = (UInt16)cpu.GetRegisterByName("PC").Value;
    //        byte A = (byte)cpu.GetRegisterByName("A").Value;
    //        byte X = (byte)cpu.GetRegisterByName("X").Value;
    //        byte Y = (byte)cpu.GetRegisterByName("Y").Value;
    //        string P = cpu.GetRegisterByName("P").FormattedValue;
    //        byte S = (byte)cpu.GetRegisterByName("S").Value;

    //        StringBuilder state = new StringBuilder();
    //        state.AppendFormat("{0:X4}  ", PC);

    //        IEnumerable<byte> instructionBytes = cpu.GetInstructionBytes(PC);

    //        if (!instructionBytes.Any())
    //        {
    //            state.AppendFormat("UNKNOWN: {0:X2}", cpuBus.Read(PC));
    //            return state.ToString();
    //        }

    //        IInstruction instruction = cpu.DecodeInstruction(instructionBytes);

    //        state.AppendFormat("{0,-8} {1,4} {2} {3}",
    //            String.Join(" ", instructionBytes.Select(b => String.Format("{0:X2}", b))),
    //            instruction.Mnemonic,
    //            instruction.Operands,
    //            instruction.OperandsDetail);

    //        state.Append(' ', 48 - state.Length);

    //        state.AppendFormat("A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} SP:{4:X2}", A, X, Y, P, S);

    //        return state.ToString();
    //    }
    //}
}

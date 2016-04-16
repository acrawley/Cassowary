using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesEmulator.CPU
{
    internal enum AddressingMode
    {
        Implicit,
        Accumulator,
        Immediate,
        ZeroPage,
        ZeroPageIndexedX,
        ZeroPageIndexedY,
        Relative,
        Absolute,
        AbsoluteIndexedX,
        AbsoluteIndexedY,
        Indirect,

        /// <summary>
        /// (Indirect,X)
        /// </summary>
        IndexedIndirect,

        /// <summary>
        /// (Indirect),Y
        /// </summary>
        IndirectIndexed
    }

    [Flags]
    internal enum InstructionType
    {
        Read = 0x01,
        Write = 0x02,
        Unofficial = 0x04,
    }

    internal delegate int InstructionFunc(ref byte arg);

    [DebuggerDisplay("{Mnemonic,nq}, Mode = {AddressingMode}")]
    internal class InstructionData
    {
        private static IDictionary<AddressingMode, byte> SizeMap;

        static InstructionData()
        {
            SizeMap = new Dictionary<AddressingMode, byte>() {
                { AddressingMode.Accumulator ,1 },
                { AddressingMode.Implicit, 1 },

                { AddressingMode.Immediate, 2 },
                { AddressingMode.IndexedIndirect, 2 },
                { AddressingMode.IndirectIndexed, 2 },
                { AddressingMode.Relative, 2 },
                { AddressingMode.ZeroPage, 2 },
                { AddressingMode.ZeroPageIndexedX, 2 },
                { AddressingMode.ZeroPageIndexedY, 2 },

                { AddressingMode.Absolute, 3 },
                { AddressingMode.AbsoluteIndexedX, 3 },
                { AddressingMode.AbsoluteIndexedY, 3 },
                { AddressingMode.Indirect, 3 },
            };
        }

        public InstructionData(byte opcode, string mnemonic, AddressingMode addressingMode, int cycles, InstructionType type, InstructionFunc implementation)
        {
            this.Opcode = opcode;
            this.Mnemonic = mnemonic;
            this.AddressingMode = addressingMode;
            this.Cycles = cycles;
            this.IsRead = (type & InstructionType.Read) == InstructionType.Read;
            this.IsWrite = (type & InstructionType.Write) == InstructionType.Write;
            this.IsUnofficial = (type & InstructionType.Unofficial) == InstructionType.Unofficial;
            this.Implementation = implementation;
            this.Size = InstructionData.SizeMap[addressingMode];
        }

        public byte Opcode { get; set; }
        public string Mnemonic { get; set; }
        public AddressingMode AddressingMode { get; private set; }
        public InstructionFunc Implementation { get; private set; }
        public byte Size { get; private set; }

        public int Cycles { get; private set; }
        public bool IsRead { get; private set; }
        public bool IsWrite { get; private set; }
        public bool IsUnofficial { get; private set; }
    }
}

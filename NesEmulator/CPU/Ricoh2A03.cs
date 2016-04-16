using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Memory;
using EmulatorCore.Extensions;

namespace NesEmulator.CPU
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class Ricoh2A03 : IProcessorCore, IMemoryMappedDevice
    {
        #region Constants

        private const UInt16 NMI_VECTOR = 0xFFFA;
        private const UInt16 RESET_VECTOR = 0xFFFC;
        private const UInt16 IRQ_VECTOR = 0xFFFE;
        private const UInt16 OAMDMA_ADDR = 0x4014;

        #endregion

        #region Private Fields

        private IMemoryBus cpuBus;
        private InstructionData[] instructions = new InstructionData[0x100];
        private ReadOnlyCollection<IProcessorRegister> registers;
        private ReadOnlyCollection<IProcessorInterrupt> interrupts;

        private bool resetAsserted = false;
        private bool nmiAsserted = false;
        private int irqAsserted = 0;
        private EventType pendingEvent = EventType.None;

        private bool oamDmaActive;
        private int oamDmaCycle;
        private UInt16 oamDmaBaseAddress;
        private byte oamDmaTempRegister;

        #endregion

        #region Registers and Flags

        private UInt16 PC;
        private byte S;
        private byte A;
        private byte X;
        private byte Y;

        private bool CFlag;
        private bool ZFlag;
        private bool IFlag;
        private bool DFlag;
        private bool VFlag;
        private bool NFlag;

        private byte P
        {
            get
            {
                return (byte)((this.CFlag ? 0x01 : 0x00) |
                              (this.ZFlag ? 0x02 : 0x00) |
                              (this.IFlag ? 0x04 : 0x00) |
                              (this.DFlag ? 0x08 : 0x00) |
                              (this.VFlag ? 0x40 : 0x00) |
                              (this.NFlag ? 0x80 : 0x00));
            }

            set
            {
                this.CFlag = (value & 0x01) == 0x01;
                this.ZFlag = (value & 0x02) == 0x02;
                this.IFlag = (value & 0x04) == 0x04;
                this.DFlag = (value & 0x08) == 0x08;
                this.VFlag = (value & 0x40) == 0x40;
                this.NFlag = (value & 0x80) == 0x80;
            }
        }

        #endregion

        #region Constructor

        internal Ricoh2A03(IMemoryBus cpuBus)
        {
            this.cpuBus = cpuBus;

            this.cpuBus.RegisterMappedDevice(this, Ricoh2A03.OAMDMA_ADDR);

            // Initial register values
            this.P = 0x34;
            this.A = 0;
            this.X = 0;
            this.Y = 0;
            this.S = 0xFD;

            this.registers = new ReadOnlyCollection<IProcessorRegister>(
                new IProcessorRegister[] {
                    new RegisterWrapper("PC", "Program Counter", 16, () => this.PC, (value) => this.PC = (UInt16)value),
                    new RegisterWrapper("A", "Accumulator", 8, () => this.A, (value) => this.A = (byte)value),
                    new RegisterWrapper("X", "X", 8, () => this.X, (value) => this.X = (byte)value),
                    new RegisterWrapper("Y", "Y", 8, () => this.Y, (value) => this.Y = (byte)value),
                    new RegisterWrapper("S", "Stack Pointer", 8, () => this.S, (value) => this.S = (byte)value),
                    new RegisterWrapper("P", "Processor Flags", 8, () => this.P, (value) => this.P = (byte)value, () => this.GetFlagsString())
                }
            );

            this.interrupts = new ReadOnlyCollection<IProcessorInterrupt>(
                new IProcessorInterrupt[] {
                    new InterruptWrapper("Reset", 0, isAsserted => this.resetAsserted = isAsserted),
                    new InterruptWrapper("NMI", 1, isAsserted => this.nmiAsserted = isAsserted),
                    new InterruptWrapper("IRQ", 2, isAsserted => this.irqAsserted += isAsserted ? 1 : -1)
                }
            );

            this.InitializeOpTable();
        }

        #endregion

        #region Opcode Table

        private void InitializeOpTable()
        {
            // Official instructions

            this.AddOpTableEntry("ADC", this.ADC, InstructionType.Read,
                new InstructionVariant(0x69, AddressingMode.Immediate, 2),
                new InstructionVariant(0x65, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x75, AddressingMode.ZeroPageIndexedX, 3),
                new InstructionVariant(0x6d, AddressingMode.Absolute, 4),
                new InstructionVariant(0x7D, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0x79, AddressingMode.AbsoluteIndexedY, 4),
                new InstructionVariant(0x61, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0x71, AddressingMode.IndirectIndexed, 5));
            this.AddOpTableEntry("AND", this.AND, InstructionType.Read,
                new InstructionVariant(0x29, AddressingMode.Immediate, 2),
                new InstructionVariant(0x25, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x35, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x2D, AddressingMode.Absolute, 4),
                new InstructionVariant(0x3D, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0x39, AddressingMode.AbsoluteIndexedY, 4),
                new InstructionVariant(0x21, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0x31, AddressingMode.IndirectIndexed, 5));
            this.AddOpTableEntry("ASL", this.ASL, InstructionType.Read | InstructionType.Write,
                new InstructionVariant(0x0A, AddressingMode.Accumulator, 2),
                new InstructionVariant(0x06, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0x16, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0x0E, AddressingMode.Absolute, 6),
                new InstructionVariant(0x1E, AddressingMode.AbsoluteIndexedX, 7));
            this.AddOpTableEntry("BCC", this.BCC, InstructionType.Read, 0x90, AddressingMode.Relative, 2);
            this.AddOpTableEntry("BCS", this.BCS, InstructionType.Read, 0xB0, AddressingMode.Relative, 2);
            this.AddOpTableEntry("BEQ", this.BEQ, InstructionType.Read, 0xF0, AddressingMode.Relative, 2);
            this.AddOpTableEntry("BIT", this.BIT, InstructionType.Read,
                new InstructionVariant(0x24, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x2C, AddressingMode.Absolute, 4));
            this.AddOpTableEntry("BMI", this.BMI, InstructionType.Read, 0x30, AddressingMode.Relative, 2);
            this.AddOpTableEntry("BNE", this.BNE, InstructionType.Read, 0xD0, AddressingMode.Relative, 2);
            this.AddOpTableEntry("BPL", this.BPL, InstructionType.Read, 0x10, AddressingMode.Relative, 2);
            this.AddOpTableEntry("BRK", this.BRK, InstructionType.Read, 0x00, AddressingMode.Implicit, 7);
            this.AddOpTableEntry("BVC", this.BVC, InstructionType.Read, 0x50, AddressingMode.Relative, 2);
            this.AddOpTableEntry("BVS", this.BVS, InstructionType.Read, 0x70, AddressingMode.Relative, 2);
            this.AddOpTableEntry("CLC", this.CLC, InstructionType.Read, 0x18, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("CLD", this.CLD, InstructionType.Read, 0xD8, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("CLI", this.CLI, InstructionType.Read, 0x58, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("CLV", this.CLV, InstructionType.Read, 0xB8, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("CMP", this.CMP, InstructionType.Read,
                new InstructionVariant(0xC9, AddressingMode.Immediate, 2),
                new InstructionVariant(0xC5, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0xD5, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0xCD, AddressingMode.Absolute, 4),
                new InstructionVariant(0xDD, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0xD9, AddressingMode.AbsoluteIndexedY, 4),
                new InstructionVariant(0xC1, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0xD1, AddressingMode.IndirectIndexed, 5));
            this.AddOpTableEntry("CPX", this.CPX, InstructionType.Read,
                new InstructionVariant(0xE0, AddressingMode.Immediate, 2),
                new InstructionVariant(0xE4, AddressingMode.ZeroPage, 2),
                new InstructionVariant(0xEC, AddressingMode.Absolute, 4));
            this.AddOpTableEntry("CPY", this.CPY, InstructionType.Read,
                new InstructionVariant(0xC0, AddressingMode.Immediate, 2),
                new InstructionVariant(0xC4, AddressingMode.ZeroPage, 2),
                new InstructionVariant(0xCC, AddressingMode.Absolute, 4));
            this.AddOpTableEntry("DEC", this.DEC, InstructionType.Read | InstructionType.Write,
                new InstructionVariant(0xC6, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0xD6, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0xCE, AddressingMode.Absolute, 6),
                new InstructionVariant(0xDE, AddressingMode.AbsoluteIndexedX, 7));
            this.AddOpTableEntry("DEX", this.DEX, InstructionType.Read, 0xCA, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("DEY", this.DEY, InstructionType.Read, 0x88, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("EOR", this.EOR, InstructionType.Read,
                new InstructionVariant(0x49, AddressingMode.Immediate, 2),
                new InstructionVariant(0x45, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x55, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x4D, AddressingMode.Absolute, 4),
                new InstructionVariant(0x5D, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0x59, AddressingMode.AbsoluteIndexedY, 4),
                new InstructionVariant(0x41, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0x51, AddressingMode.IndirectIndexed, 5));
            this.AddOpTableEntry("INC", this.INC, InstructionType.Read | InstructionType.Write,
                new InstructionVariant(0xE6, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0xF6, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0xEE, AddressingMode.Absolute, 6),
                new InstructionVariant(0xFE, AddressingMode.AbsoluteIndexedX, 7));
            this.AddOpTableEntry("INX", this.INX, InstructionType.Read, 0xE8, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("INY", this.INY, InstructionType.Read, 0xC8, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("JMP", null, InstructionType.Read,
                new InstructionVariant(0x4C, AddressingMode.Absolute, 3),
                new InstructionVariant(0x6C, AddressingMode.Indirect, 5));
            this.AddOpTableEntry("JSR", null, InstructionType.Read, 0x20, AddressingMode.Absolute, 6);
            this.AddOpTableEntry("LDA", this.LDA, InstructionType.Read,
                new InstructionVariant(0xA9, AddressingMode.Immediate, 2),
                new InstructionVariant(0xA5, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0xB5, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0xAD, AddressingMode.Absolute, 4),
                new InstructionVariant(0xBD, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0xB9, AddressingMode.AbsoluteIndexedY, 4),
                new InstructionVariant(0xA1, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0xB1, AddressingMode.IndirectIndexed, 5));
            this.AddOpTableEntry("LDX", this.LDX, InstructionType.Read,
                new InstructionVariant(0xA2, AddressingMode.Immediate, 2),
                new InstructionVariant(0xA6, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0xB6, AddressingMode.ZeroPageIndexedY, 4),
                new InstructionVariant(0xAE, AddressingMode.Absolute, 4),
                new InstructionVariant(0xBE, AddressingMode.AbsoluteIndexedY, 4));
            this.AddOpTableEntry("LDY", this.LDY, InstructionType.Read,
                new InstructionVariant(0xA0, AddressingMode.Immediate, 2),
                new InstructionVariant(0xA4, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0xB4, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0xAC, AddressingMode.Absolute, 4),
                new InstructionVariant(0xBC, AddressingMode.AbsoluteIndexedX, 4));
            this.AddOpTableEntry("LSR", this.LSR, InstructionType.Read | InstructionType.Write,
                new InstructionVariant(0x4A, AddressingMode.Accumulator, 2),
                new InstructionVariant(0x46, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0x56, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0x4E, AddressingMode.Absolute, 6),
                new InstructionVariant(0x5E, AddressingMode.AbsoluteIndexedX, 7));
            this.AddOpTableEntry("NOP", this.NOP, InstructionType.Read, 0xEA, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("ORA", this.ORA, InstructionType.Read,
                new InstructionVariant(0x09, AddressingMode.Immediate, 2),
                new InstructionVariant(0x05, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x15, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x0D, AddressingMode.Absolute, 4),
                new InstructionVariant(0x1D, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0x19, AddressingMode.AbsoluteIndexedY, 4),
                new InstructionVariant(0x01, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0x11, AddressingMode.IndirectIndexed, 5));
            this.AddOpTableEntry("PHA", this.PHA, InstructionType.Write, 0x48, AddressingMode.Implicit, 3);
            this.AddOpTableEntry("PHP", this.PHP, InstructionType.Write, 0x08, AddressingMode.Implicit, 3);
            this.AddOpTableEntry("PLA", this.PLA, InstructionType.Read, 0x68, AddressingMode.Implicit, 4);
            this.AddOpTableEntry("PLP", this.PLP, InstructionType.Read, 0x28, AddressingMode.Implicit, 4);
            this.AddOpTableEntry("ROL", this.ROL, InstructionType.Read | InstructionType.Write,
                new InstructionVariant(0x2A, AddressingMode.Accumulator, 2),
                new InstructionVariant(0x26, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0x36, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0x2E, AddressingMode.Absolute, 6),
                new InstructionVariant(0x3E, AddressingMode.AbsoluteIndexedX, 7));
            this.AddOpTableEntry("ROR", this.ROR, InstructionType.Read | InstructionType.Write,
                new InstructionVariant(0x6A, AddressingMode.Accumulator, 2),
                new InstructionVariant(0x66, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0x76, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0x6E, AddressingMode.Absolute, 6),
                new InstructionVariant(0x7E, AddressingMode.AbsoluteIndexedX, 7));
            this.AddOpTableEntry("RTI", this.RTI, InstructionType.Read, 0x40, AddressingMode.Implicit, 6);
            this.AddOpTableEntry("RTS", this.RTS, InstructionType.Read, 0x60, AddressingMode.Implicit, 6);
            this.AddOpTableEntry("SBC", this.SBC, InstructionType.Read,
                new InstructionVariant(0xE9, AddressingMode.Immediate, 2),
                new InstructionVariant(0xE5, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0xF5, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0xED, AddressingMode.Absolute, 4),
                new InstructionVariant(0xFD, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0xF9, AddressingMode.AbsoluteIndexedY, 4),
                new InstructionVariant(0xE1, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0xF1, AddressingMode.IndirectIndexed, 5));
            this.AddOpTableEntry("SEC", this.SEC, InstructionType.Read, 0x38, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("SED", this.SED, InstructionType.Read, 0xF8, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("SEI", this.SEI, InstructionType.Read, 0x78, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("STA", this.STA, InstructionType.Write,
                new InstructionVariant(0x85, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x95, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x8D, AddressingMode.Absolute, 4),
                new InstructionVariant(0x9D, AddressingMode.AbsoluteIndexedX, 5),
                new InstructionVariant(0x99, AddressingMode.AbsoluteIndexedY, 5),
                new InstructionVariant(0x81, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0x91, AddressingMode.IndirectIndexed, 6));
            this.AddOpTableEntry("STX", this.STX, InstructionType.Write,
                new InstructionVariant(0x86, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x96, AddressingMode.ZeroPageIndexedY, 4),
                new InstructionVariant(0x8E, AddressingMode.Absolute, 4));
            this.AddOpTableEntry("STY", this.STY, InstructionType.Write,
                new InstructionVariant(0x84, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x94, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x8C, AddressingMode.Absolute, 4));
            this.AddOpTableEntry("TAX", this.TAX, InstructionType.Read, 0xAA, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("TAY", this.TAY, InstructionType.Read, 0xA8, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("TSX", this.TSX, InstructionType.Read, 0xBA, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("TXA", this.TXA, InstructionType.Read, 0x8A, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("TXS", this.TXS, InstructionType.Read, 0x9A, AddressingMode.Implicit, 2);
            this.AddOpTableEntry("TYA", this.TYA, InstructionType.Read, 0x98, AddressingMode.Implicit, 2);

            // Unofficial / undocumented instructions

            this.AddOpTableEntry("DCP", this.DCP, InstructionType.Read | InstructionType.Write | InstructionType.Unofficial,
                new InstructionVariant(0xC7, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0xD7, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0xCF, AddressingMode.Absolute, 6),
                new InstructionVariant(0xDF, AddressingMode.AbsoluteIndexedX, 7),
                new InstructionVariant(0xDB, AddressingMode.AbsoluteIndexedY, 7),
                new InstructionVariant(0xC3, AddressingMode.IndexedIndirect, 8),
                new InstructionVariant(0xD3, AddressingMode.IndirectIndexed, 8));
            this.AddOpTableEntry("ISB", this.ISB, InstructionType.Read | InstructionType.Write | InstructionType.Unofficial,
                new InstructionVariant(0xE7, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0xF7, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0xEF, AddressingMode.Absolute, 6),
                new InstructionVariant(0xFF, AddressingMode.AbsoluteIndexedX, 7),
                new InstructionVariant(0xFB, AddressingMode.AbsoluteIndexedY, 7),
                new InstructionVariant(0xE3, AddressingMode.IndexedIndirect, 8),
                new InstructionVariant(0xF3, AddressingMode.IndirectIndexed, 8));
            this.AddOpTableEntry("LAX", this.LAX, InstructionType.Read | InstructionType.Unofficial,
                new InstructionVariant(0xA7, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0xB7, AddressingMode.ZeroPageIndexedY, 4),
                new InstructionVariant(0xAF, AddressingMode.Absolute, 4),
                new InstructionVariant(0xBF, AddressingMode.AbsoluteIndexedY, 4),
                new InstructionVariant(0xA3, AddressingMode.IndexedIndirect, 6),
                new InstructionVariant(0xB3, AddressingMode.IndirectIndexed, 5));
            this.AddOpTableEntry("NOP", this.NOP, InstructionType.Read | InstructionType.Unofficial,
                new InstructionVariant(0x1A, AddressingMode.Implicit, 2),
                new InstructionVariant(0x3A, AddressingMode.Implicit, 2),
                new InstructionVariant(0x5A, AddressingMode.Implicit, 2),
                new InstructionVariant(0x7A, AddressingMode.Implicit, 2),
                new InstructionVariant(0xDA, AddressingMode.Implicit, 2),
                new InstructionVariant(0xFA, AddressingMode.Implicit, 2),
                new InstructionVariant(0x80, AddressingMode.Immediate, 2),
                new InstructionVariant(0x82, AddressingMode.Immediate, 2),
                new InstructionVariant(0x89, AddressingMode.Immediate, 2),
                new InstructionVariant(0xC2, AddressingMode.Immediate, 2),
                new InstructionVariant(0xE2, AddressingMode.Immediate, 2),
                new InstructionVariant(0x04, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x44, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x64, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x14, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x34, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x54, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x74, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0xD4, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0xF4, AddressingMode.ZeroPageIndexedX, 4),
                new InstructionVariant(0x0C, AddressingMode.Absolute, 4),
                new InstructionVariant(0x1C, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0x3C, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0x5C, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0x7C, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0xDC, AddressingMode.AbsoluteIndexedX, 4),
                new InstructionVariant(0xFC, AddressingMode.AbsoluteIndexedX, 4));
            this.AddOpTableEntry("SAX", this.SAX, InstructionType.Write | InstructionType.Unofficial,
                new InstructionVariant(0x87, AddressingMode.ZeroPage, 3),
                new InstructionVariant(0x97, AddressingMode.ZeroPageIndexedY, 4),
                new InstructionVariant(0x8F, AddressingMode.Absolute, 4),
                new InstructionVariant(0x83, AddressingMode.IndexedIndirect, 6));
            this.AddOpTableEntry("SBC", this.SBC, InstructionType.Read | InstructionType.Unofficial, 0xEB, AddressingMode.Immediate, 2);
            this.AddOpTableEntry("SLO", this.SLO, InstructionType.Read | InstructionType.Write | InstructionType.Unofficial,
                new InstructionVariant(0x07, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0x17, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0x0F, AddressingMode.Absolute, 6),
                new InstructionVariant(0x1F, AddressingMode.AbsoluteIndexedX, 7),
                new InstructionVariant(0x1B, AddressingMode.AbsoluteIndexedY, 7),
                new InstructionVariant(0x03, AddressingMode.IndexedIndirect, 8),
                new InstructionVariant(0x13, AddressingMode.IndirectIndexed, 8));
            this.AddOpTableEntry("SRE", this.SRE, InstructionType.Read | InstructionType.Write | InstructionType.Unofficial,
                new InstructionVariant(0x47, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0x57, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0x4F, AddressingMode.Absolute, 6),
                new InstructionVariant(0x5F, AddressingMode.AbsoluteIndexedX, 7),
                new InstructionVariant(0x5B, AddressingMode.AbsoluteIndexedY, 7),
                new InstructionVariant(0x43, AddressingMode.IndexedIndirect, 8),
                new InstructionVariant(0x53, AddressingMode.IndirectIndexed, 8));
            this.AddOpTableEntry("RLA", this.RLA, InstructionType.Read | InstructionType.Write | InstructionType.Unofficial,
                new InstructionVariant(0x27, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0x37, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0x2F, AddressingMode.Absolute, 6),
                new InstructionVariant(0x3F, AddressingMode.AbsoluteIndexedX, 7),
                new InstructionVariant(0x3B, AddressingMode.AbsoluteIndexedY, 7),
                new InstructionVariant(0x23, AddressingMode.IndexedIndirect, 8),
                new InstructionVariant(0x33, AddressingMode.IndirectIndexed, 8));
            this.AddOpTableEntry("RRA", this.RRA, InstructionType.Read | InstructionType.Write | InstructionType.Unofficial,
                new InstructionVariant(0x67, AddressingMode.ZeroPage, 5),
                new InstructionVariant(0x77, AddressingMode.ZeroPageIndexedX, 6),
                new InstructionVariant(0x6F, AddressingMode.Absolute, 6),
                new InstructionVariant(0x7F, AddressingMode.AbsoluteIndexedX, 7),
                new InstructionVariant(0x7B, AddressingMode.AbsoluteIndexedY, 7),
                new InstructionVariant(0x63, AddressingMode.IndexedIndirect, 8),
                new InstructionVariant(0x73, AddressingMode.IndirectIndexed, 8));
        }

        private struct InstructionVariant
        {
            public InstructionVariant(byte opcode, AddressingMode addressingMode, int cycles)
            {
                this.Opcode = opcode;
                this.AddressingMode = addressingMode;
                this.Cycles = cycles;
            }

            public byte Opcode { get; private set; }
            public AddressingMode AddressingMode { get; private set; }

            public int Cycles { get; private set; }
        }

        private void AddOpTableEntry(string mnemonic, InstructionFunc implementation, InstructionType type, params InstructionVariant[] variants)
        {
            foreach (InstructionVariant variant in variants)
            {
                this.AddOpTableEntry(mnemonic, implementation, type, variant.Opcode, variant.AddressingMode, variant.Cycles);
            }
        }

        private void AddOpTableEntry(string mnemonic, InstructionFunc implementation, InstructionType type, byte opcode, AddressingMode addressingMode, int cycles)
        {
            Debug.Assert(this.instructions[opcode] == null, "Duplicate opcode?");
            this.instructions[opcode] = new InstructionData(opcode, mnemonic, addressingMode, cycles, type, implementation);
        }

        #endregion

        #region Operations

        private int ADC(ref byte arg)
        {
            // Add with carry
            int result = this.A + arg + (this.CFlag ? 1 : 0);

            this.CFlag = result > 0xFF;

            this.VFlag = (~(this.A ^ arg) &    // Bit 7 set if A and arg have same sign
                           (this.A ^ result) & // Bit 7 set if A and result have different sign
                           0x80) == 0x80;      // Overflow if bit 7 is set

            this.A = (byte)result;

            this.ZFlag = (this.A == 0);
            this.NFlag = (this.A & 0x80) == 0x80;
            return 0;
        }

        private int AND(ref byte arg)
        {
            // Logical AND
            this.A &= arg;
            this.ZFlag = (this.A == 0);
            this.NFlag = (this.A & 0x80) == 0x80;
            return 0;
        }

        private int ASL(ref byte arg)
        {
            // Arithmetic shift left
            this.CFlag = (arg & 0x80) == 0x80;
            arg <<= 1;
            this.ZFlag = (arg == 0);
            this.NFlag = (arg & 0x80) == 0x80;
            return 0;
        }

        private int BCC(ref byte arg)
        {
            // Branch if carry clear
            return this.BranchCore(arg, !this.CFlag);
        }

        private int BCS(ref byte arg)
        {
            // Branch if carry set
            return this.BranchCore(arg, this.CFlag);
        }

        private int BEQ(ref byte arg)
        {
            // Branch if equal
            return this.BranchCore(arg, this.ZFlag);
        }

        private int BIT(ref byte arg)
        {
            // Bit test
            byte tmp = (byte)(this.A & arg);

            this.NFlag = (arg & 0x80) == 0x80;
            this.VFlag = (arg & 0x40) == 0x40;
            this.ZFlag = (tmp == 0x00);

            return 0;
        }

        private int BMI(ref byte arg)
        {
            // Branch if minus
            return this.BranchCore(arg, this.NFlag);
        }

        private int BNE(ref byte arg)
        {
            // Branch if not equal
            return this.BranchCore(arg, !this.ZFlag);
        }

        private int BPL(ref byte arg)
        {
            // Branch if positive
            return this.BranchCore(arg, !this.NFlag);
        }

        private int BRK(ref byte arg)
        {
            // Force interrupt

            // Increment PC before dispatching the interrupt - BRK pushes PC+1, so the instruction
            //  after the BRK is skipped when an RTI is executed
            this.PC++;
            this.DispatchInterrupt(EventType.BRK);
            return 0;
        }

        private int BVC(ref byte arg)
        {
            // Branch if overflow clear
            return this.BranchCore(arg, !this.VFlag);
        }

        private int BVS(ref byte arg)
        {
            // Branch if overflow set
            return this.BranchCore(arg, this.VFlag);
        }

        private int BranchCore(byte arg, bool shouldBranch)
        {
            if (shouldBranch)
            {
                UInt16 newPC = (UInt16)(this.PC + (sbyte)arg);
                // 1 extra cycle if branch is taken, 2 extra cycles if the branch is to another page
                int cycles = ((newPC & 0x100) == (this.PC & 0100)) ? 1 : 2;
                this.PC = (UInt16)newPC;
                return cycles;
            }

            return 0;
        }

        private int CLC(ref byte arg)
        {
            // Clear carry flag
            this.CFlag = false;
            return 0;
        }

        private int CLD(ref byte arg)
        {
            // Disable decimal mode
            this.DFlag = false;
            return 0;
        }

        private int CLI(ref byte arg)
        {
            // Clear interrupt disable flag
            this.IFlag = false;
            return 0;
        }

        private int CLV(ref byte arg)
        {
            // Clear overflow flag
            this.VFlag = false;
            return 0;
        }

        private int CMP(ref byte arg)
        {
            // Compare accumulator
            return this.CompareCore(this.A, arg);
        }

        private int CPX(ref byte arg)
        {
            // Compare X register
            return this.CompareCore(this.X, arg);
        }

        private int CPY(ref byte arg)
        {
            // Compare Y register
            return this.CompareCore(this.Y, arg);
        }

        private int CompareCore(byte reg, byte arg)
        {
            byte tmp = (byte)(reg - arg);
            this.NFlag = (tmp & 0x80) == 0x80;
            this.CFlag = (reg >= tmp);
            this.ZFlag = (tmp == 0);
            return 0;
        }

        private int DEC(ref byte arg)
        {
            // Decrement memory
            return this.DecrementCore(ref arg);
        }

        private int DEX(ref byte arg)
        {
            // Decrement X register
            return this.DecrementCore(ref this.X);
        }

        private int DEY(ref byte arg)
        {
            // Decrement Y register
            return this.DecrementCore(ref this.Y);
        }

        private int DecrementCore(ref byte arg)
        {
            arg--;
            this.NFlag = (arg & 0x80) == 0x80;
            this.ZFlag = (arg == 0);
            return 0;
        }

        private int EOR(ref byte arg)
        {
            // Exclusive OR
            this.A ^= arg;
            this.NFlag = (this.A & 0x80) == 0x80;
            this.ZFlag = (this.A == 0);
            return 0;
        }

        private int INC(ref byte arg)
        {
            // Increment memory
            return this.IncrementCore(ref arg);
        }

        private int INX(ref byte arg)
        {
            // Increment X register
            return this.IncrementCore(ref this.X);
        }

        private int INY(ref byte arg)
        {
            // Increment Y register
            return this.IncrementCore(ref this.Y);
        }

        private int IncrementCore(ref byte arg)
        {
            arg++;
            this.NFlag = (arg & 0x80) == 0x80;
            this.ZFlag = (arg == 0);
            return 0;
        }

        private int JMP(int addr)
        {
            // Unconditional jump
            this.PC = (UInt16)addr;
            return 0;
        }

        private int JSR(int addr)
        {
            // Jump to subroutine
            int retAddr = this.PC - 1;
            this.PushStack((byte)(retAddr >> 8));
            this.PushStack((byte)(retAddr & 0xFF));
            this.PC = (UInt16)addr;
            return 0;
        }

        private int LDA(ref byte arg)
        {
            // Load accumulator
            this.A = arg;
            this.ZFlag = (arg == 0);
            this.NFlag = (arg & 0x80) == 0x80;
            return 0;
        }

        private int LDX(ref byte arg)
        {
            // Load X register
            this.X = arg;
            this.ZFlag = (arg == 0);
            this.NFlag = (arg & 0x80) == 0x80;
            return 0;
        }

        private int LDY(ref byte arg)
        {
            // Load Y register
            this.Y = arg;
            this.ZFlag = (arg == 0);
            this.NFlag = (arg & 0x80) == 0x80;
            return 0;
        }

        private int LSR(ref byte arg)
        {
            // Logical Shift Right
            this.NFlag = false;
            this.CFlag = (arg & 0x01) == 0x01;
            arg = (byte)(arg >> 1);
            this.ZFlag = (arg == 0);
            return 0;
        }

        private int NOP(ref byte arg)
        {
            // No operation
            return 0;
        }

        private int ORA(ref byte arg)
        {
            // Logical inclusive OR
            this.A |= arg;
            this.NFlag = (this.A & 0x80) == 0x80;
            this.ZFlag = (this.A == 0);
            return 0;
        }

        private int PHA(ref byte arg)
        {
            // Push accumulator
            this.PushStack(this.A);
            return 0;
        }

        private int PHP(ref byte arg)
        {
            // Push processor status
            // NOTE: The flags value pushed seems to always have bits 5 and 6 set, so the following
            //  testcase will set A = 0b01100000 (0x30).  Verified in Visual6502:
            //  http://visual6502.org/JSSim/expert.html?a=0&d=a90048a9ff2808684c0800
            //    0000: A9 00    LDA #00    ; A = 0x00
            //    0002: 48       PHA        ; Push A
            //    0003: A9 FF    LDA #FF    ; A = 0xFF
            //    0005: 28       PLP        ; Pop 0x00 into status register
            //    0006: 08       PHP        ; Push status register
            //    0007: 68       PLA        ; Pop status register into A
            //    0008: 4C 08 00 JMP $0008  ; Infinite loop
            this.PushStack((byte)(this.P | 0x30));
            return 0;
        }

        private int PLA(ref byte arg)
        {
            // Pull accumulator
            this.A = this.PopStack();
            this.NFlag = (this.A & 0x80) == 0x80;
            this.ZFlag = (this.A == 0);
            return 0;
        }

        private int PLP(ref byte arg)
        {
            // Pull processor status
            this.P = this.PopStack();
            return 0;
        }

        private int ROL(ref byte arg)
        {
            // Rotate left
            byte tmp = (byte)(arg & 0x80);
            arg = (byte)((arg << 1) | (this.CFlag ? 0x01 : 0x00));
            this.CFlag = (tmp != 0);
            this.NFlag = (arg & 0x80) == 0x80;
            this.ZFlag = (arg == 0);
            return 0;
        }

        private int ROR(ref byte arg)
        {
            // Rotate right
            byte tmp = (byte)(arg & 0x01);
            arg = (byte)((arg >> 1) | (this.CFlag ? 0x80 : 0x00));
            this.CFlag = (tmp != 0);
            this.NFlag = (arg & 0x80) == 0x80;
            this.ZFlag = (arg == 0);
            return 0;
        }

        private int RTI(ref byte arg)
        {
            // Return from interrupt

            // Pop flags and PC, but ignore the B flag
            this.P = this.PopStack();
            this.PC = (UInt16)(this.PopStack() | (this.PopStack() << 8));

            return 0;
        }

        private int RTS(ref byte arg)
        {
            // Return from subroutine
            this.PC = (UInt16)((this.PopStack() | (this.PopStack() << 8)) + 1);
            return 0;
        }

        private int SBC(ref byte arg)
        {
            // Subtract with carry
            byte tmp = (byte)~arg;
            this.ADC(ref tmp);
            return 0;
        }

        private int SEC(ref byte arg)
        {
            // Set carry flag
            this.CFlag = true;
            return 0;
        }

        private int SED(ref byte arg)
        {
            // Set decimal flag
            this.DFlag = true;
            return 0;
        }

        private int SEI(ref byte arg)
        {
            // Set interrupt disable flag
            this.IFlag = true;
            return 0;
        }

        private int STA(ref byte arg)
        {
            // Store accumulator
            arg = this.A;
            return 0;
        }

        private int STX(ref byte arg)
        {
            // Store X register
            arg = this.X;
            return 0;
        }

        private int STY(ref byte arg)
        {
            // Store Y register
            arg = this.Y;
            return 0;
        }

        private int TAX(ref byte arg)
        {
            // Transfer accumulator to X register
            this.X = this.A;
            this.NFlag = (this.X & 0x80) == 0x80;
            this.ZFlag = (this.X == 0);
            return 0;
        }

        private int TAY(ref byte arg)
        {
            // Transfer accumulator to Y register
            this.Y = this.A;
            this.NFlag = (this.Y & 0x80) == 0x80;
            this.ZFlag = (this.Y == 0);
            return 0;
        }

        private int TSX(ref byte arg)
        {
            // Transfer S register to X register
            this.X = this.S;
            this.NFlag = (this.X & 0x80) == 0x80;
            this.ZFlag = (this.X == 0);
            return 0;
        }

        private int TXA(ref byte arg)
        {
            // Transfer X register to accumulator
            this.A = this.X;
            this.NFlag = (this.A & 0x80) == 0x80;
            this.ZFlag = (this.A == 0);
            return 0;
        }

        private int TXS(ref byte arg)
        {
            // Transfer X register to S register
            this.S = this.X;
            return 0;
        }

        private int TYA(ref byte arg)
        {
            // Transfer Y register to accumulator
            this.A = this.Y;
            this.NFlag = (this.A & 0x80) == 0x80;
            this.ZFlag = (this.A == 0);
            return 0;
        }

        #endregion

        #region Unofficial Operations

        private int DCP(ref byte arg)
        {
            // Decrement memory and compare to accumulator
            this.DecrementCore(ref arg);
            this.CompareCore(this.A, arg);
            return 0;
        }

        private int ISB(ref byte arg)
        {
            // Increment memory and subtract from accumulator
            this.IncrementCore(ref arg);
            this.SBC(ref arg);
            return 0;
        }

        private int LAX(ref byte arg)
        {
            // Load accumulator and X register
            this.LDA(ref arg);
            this.LDX(ref arg);
            return 0;
        }

        private int SAX(ref byte arg)
        {
            // Store logical AND of accumulator and X register
            arg = (byte)(this.X & this.A);
            return 0;
        }

        private int SLO(ref byte arg)
        {
            // Shift left, then OR with accumulator
            this.ASL(ref arg);
            this.ORA(ref arg);
            return 0;
        }

        private int SRE(ref byte arg)
        {
            // Shift right, then XOR with accumulator
            this.LSR(ref arg);
            this.EOR(ref arg);
            return 0;
        }

        private int RLA(ref byte arg)
        {
            // Rotate left, then AND with accumulator
            this.ROL(ref arg);
            this.AND(ref arg);
            return 0;
        }

        private int RRA(ref byte arg)
        {
            // Rotate right, then add to accumulator
            this.ROR(ref arg);
            this.ADC(ref arg);
            return 0;
        }

        #endregion

        #region Instruction Decoding

        int breakpoint = -1;

        private int DispatchNextInstruction()
        {
            InstructionData instruction;
            byte arg1;
            byte arg2;

            if (!this.FetchInstruction(this.PC, out instruction, out arg1, out arg2))
            {
                Debug.WriteLine("No implementation for opcode 0x{0,2:X2} - CPU halted!", this.cpuBus.Read(this.PC));
                return -1;
            }

            if (this.PC == this.breakpoint)
            {
                Debugger.Break();
            }

            //this.DumpState(instruction, arg1, arg2);

            this.PC += instruction.Size;

            bool pageFault;
            UInt16 addr = this.DecodeTargetAddr(instruction.AddressingMode, arg1, arg2, out pageFault);

            int cycles = instruction.Cycles;

            if (instruction.Implementation == null)
            {
                // JMP and JSR operate directly on a 16-bit address rather than an 8-bit arg, so handle
                //  them specially
                switch (instruction.Opcode)
                {
                    case 0x20: // JSR
                        cycles += this.JSR(addr);
                        break;

                    case 0x4C: // JMP Absolute
                        cycles += this.JMP(addr);
                        break;

                    case 0x6C: // JMP Indirect
                        // NMOS 6502 bug: If the first byte of the indirect address is the last byte of a
                        //  page of memory, the second byte will come from the first byte of that page, not
                        //  the first byte of the next page (e.g. JMP ($02FF) will read the target address
                        //  from $02FF and $0200, not $02FF and $0300).
                        int lo = arg1 | (arg2 << 8);
                        int hi = (lo & 0xFF00) | ((arg1 + 1) & 0xFF);
                        addr = (UInt16)(this.cpuBus.Read(lo) | (this.cpuBus.Read(hi) << 8));
                        cycles += this.JMP(addr);
                        break;
                }
            }
            else
            {
                byte arg = 0;

                if (instruction.IsRead)
                {
                    arg = this.ReadArg(instruction.AddressingMode, arg1, addr);

                    if (instruction.IsWrite)
                    {
                        // Read-Modify-Write instructions write the original value back before modifying
                        this.WriteArg(instruction.AddressingMode, addr, arg);
                    }
                    else if (pageFault)
                    {
                        // If we crossed a page boundary while calculating the target address of a read-only
                        //  instruction, there's a 1-cycle penalty
                        cycles++;
                    }
                }

                cycles += instruction.Implementation(ref arg);

                if (instruction.IsWrite)
                {
                    // Store the result
                    this.WriteArg(instruction.AddressingMode, addr, arg);
                }
            }

            return cycles;
        }

        private bool FetchInstruction(UInt16 address, out InstructionData instruction, out byte arg1, out byte arg2)
        {
            byte opcode = this.cpuBus.Read(address);
            instruction = this.instructions[opcode];
            if (instruction == null)
            {
                arg1 = 0;
                arg2 = 0;
                return false;
            }

            // CPU always fetches the byte after the current instruction
            arg1 = this.cpuBus.Read(address + 1);

            switch (instruction.AddressingMode)
            {
                case AddressingMode.Indirect:
                case AddressingMode.Absolute:
                case AddressingMode.AbsoluteIndexedX:
                case AddressingMode.AbsoluteIndexedY:
                    arg2 = this.cpuBus.Read(address + 2);
                    break;

                default:
                    arg2 = 0;
                    break;
            }

            return true;
        }

        private UInt16 DecodeTargetAddr(AddressingMode mode, byte arg1, byte arg2, out bool pageFault)
        {
            pageFault = false;

            switch (mode)
            {
                case AddressingMode.Implicit:
                case AddressingMode.Accumulator:
                    // No address or data associated with this instruction
                    return 0;

                case AddressingMode.Immediate:
                case AddressingMode.Relative:
                    // Data associated with this instruction, but it's not an address
                    return 0;

                case AddressingMode.Absolute:
                case AddressingMode.Indirect:
                    return (UInt16)(arg1 | (arg2 << 8));

                case AddressingMode.ZeroPage:
                    return arg1;

                case AddressingMode.ZeroPageIndexedX:
                    return (UInt16)((arg1 + this.X) & 0xFF);

                case AddressingMode.ZeroPageIndexedY:
                    return (UInt16)((arg1 + this.Y) & 0xFF);

                case AddressingMode.AbsoluteIndexedX:
                    {
                        UInt16 baseAddr = (UInt16)(arg1 | (arg2 << 8));
                        UInt16 addr = (UInt16)(baseAddr + this.X);
                        pageFault = (baseAddr & 0x100) != (addr & 0x100);
                        return addr;
                    }

                case AddressingMode.AbsoluteIndexedY:
                    {
                        UInt16 baseAddr = (UInt16)(arg1 | (arg2 << 8));
                        UInt16 addr = (UInt16)(baseAddr + this.Y);
                        pageFault = (baseAddr & 0x100) != (addr & 0x100);
                        return addr;
                    }

                case AddressingMode.IndexedIndirect:
                    {
                        byte lo = this.cpuBus.Read((arg1 + this.X) & 0xFF);
                        byte hi = this.cpuBus.Read((arg1 + this.X + 1) & 0xFF);
                        return (UInt16)(lo | (hi << 8));
                    }

                case AddressingMode.IndirectIndexed:
                    {
                        byte lo = this.cpuBus.Read(arg1);
                        byte hi = this.cpuBus.Read((arg1 + 1) & 0xFF);
                        UInt16 baseAddr = (UInt16)(lo | (hi << 8));
                        UInt16 addr = (UInt16)(baseAddr + this.Y);
                        pageFault = (baseAddr & 0x100) != (addr & 0x100);
                        return addr;
                    }
            }

            Debug.WriteLine("Unhandled addressing mode '{0}'!", mode);
            return 0;
        }

        private byte ReadArg(AddressingMode mode, byte next, UInt16 address)
        {
            switch (mode)
            {
                case AddressingMode.Implicit:
                    return 0;

                case AddressingMode.Accumulator:
                    return this.A;

                case AddressingMode.Immediate:
                case AddressingMode.Relative:
                    return next;

                case AddressingMode.ZeroPage:
                case AddressingMode.ZeroPageIndexedX:
                case AddressingMode.ZeroPageIndexedY:
                case AddressingMode.Absolute:
                case AddressingMode.AbsoluteIndexedX:
                case AddressingMode.AbsoluteIndexedY:
                case AddressingMode.IndexedIndirect:
                case AddressingMode.IndirectIndexed:
                    return this.cpuBus.Read(address);
            }

            Debug.WriteLine("Unhandled addressing mode '{0}' for read!", mode);
            return 0;
        }

        private void WriteArg(AddressingMode mode, UInt16 address, byte value)
        {
            switch (mode)
            {
                case AddressingMode.Accumulator:
                    this.A = value;
                    return;

                case AddressingMode.Implicit:
                    return;

                case AddressingMode.ZeroPage:
                case AddressingMode.ZeroPageIndexedX:
                case AddressingMode.ZeroPageIndexedY:
                case AddressingMode.Absolute:
                case AddressingMode.AbsoluteIndexedX:
                case AddressingMode.AbsoluteIndexedY:
                case AddressingMode.IndexedIndirect:
                case AddressingMode.IndirectIndexed:
                    this.cpuBus.Write(address, value);
                    return;
            }

            Debug.WriteLine("Unhandled addressing mode '{0}' for write!", mode);
        }

        #endregion

        #region Interrupt Handling

        private enum EventType
        {
            None,
            Reset,
            NMI,
            IRQ,
            BRK
        }

        private bool CheckInterrupts()
        {
            if (this.resetAsserted)
            {
                this.resetAsserted = false;
                this.pendingEvent = EventType.Reset;
                return true;
            }

            if (this.nmiAsserted)
            {
                // NMI is edge-triggered, so clear the assert now
                this.nmiAsserted = false;
                this.pendingEvent = EventType.NMI;
                return true;
            }

            if (this.irqAsserted > 0 && !this.IFlag)
            {
                this.pendingEvent = EventType.IRQ;
                return true;
            }

            return false;
        }

        private void DispatchInterrupt(EventType type)
        {
            UInt16 vector;

            if (type == EventType.Reset)
            {
                // Reset moves the stack pointer, but doesn't actually push anything
                vector = Ricoh2A03.RESET_VECTOR;

                this.S -= 3;
            }
            else
            {
                // Push PC
                this.PushStack((byte)(this.PC >> 8));
                this.PushStack((byte)(this.PC & 0xFF));

                // Push flags - bit 6 is always set, bit 5 is set if this was triggered by a BRK
                this.PushStack((byte)(this.P | (type == EventType.BRK ? 0x30 : 0x20)));

                vector = type == EventType.NMI ? Ricoh2A03.NMI_VECTOR : Ricoh2A03.IRQ_VECTOR;
            }

            this.IFlag = true;
            this.PC = this.cpuBus.ReadUInt16LE(vector);
        }

        #endregion

        #region OAM DMA

        private bool ProcessOamDma()
        {
            if (this.oamDmaActive)
            {
                if (this.oamDmaCycle == 512)
                {
                    this.oamDmaActive = false;
                }
                else if ((this.oamDmaCycle & 0x01) == 0x00)
                {
                    // Even cycle - read from main memory
                    this.oamDmaTempRegister = this.cpuBus.Read(this.oamDmaBaseAddress + (this.oamDmaCycle >> 2));
                }
                else
                {
                    // Odd cycle - write to OAM port
                    this.cpuBus.Write(PPU.Ricoh2C02.OAMDATA_REGISTER, this.oamDmaTempRegister);
                }

                this.oamDmaCycle++;
                return true;
            }

            return false;
        }

        #endregion

        #region Helpers

        private string DebuggerDisplay
        {
            get
            {
                return String.Format("CPU: PC = 0x{0:X4}, A = 0x{1:X2}, X = 0x{2:X2}, Y = 0x{3:X2}, S = 0x{4:X2}, Flags = {5}",
                    this.PC, this.A, this.X, this.Y, this.S, this.GetFlagsString());
            }
        }

        private string GetFlagsString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}{1}__{2}{3}{4}{5}",
                this.NFlag ? "N" : "n",
                this.VFlag ? "V" : "v",
                this.DFlag ? "D" : "d",
                this.IFlag ? "I" : "i",
                this.ZFlag ? "Z" : "z",
                this.CFlag ? "C" : "c");
        }

        private void DumpState(InstructionData op, byte arg1, byte arg2)
        {
            StringBuilder state = new StringBuilder();
            IInstruction instruction = this.DecodeInstruction(op, arg1, arg2);

            state.AppendFormat("{0:X4}  {1:X2} {2} {3}  {4,4} {5} {6}",
                this.PC,
                op.Opcode,
                (op.Size > 1) ? String.Format(CultureInfo.InvariantCulture, "{0:X2}", arg1) : "  ",
                (op.Size > 2) ? String.Format(CultureInfo.InvariantCulture, "{0:X2}", arg1) : "  ",
                instruction.Mnemonic,
                instruction.Operands,
                instruction.OperandsDetail);

            state.Append(' ', 48 - state.Length);

            state.AppendFormat("A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} SP:{4:X2}",
                this.A, this.X, this.Y, this.GetFlagsString(), this.S);

            Debug.WriteLine(state.ToString());
        }

        private void PushStack(byte data)
        {
            this.cpuBus.Write(0x100 + this.S, data);
            this.S--;
        }

        private byte PopStack()
        {
            this.S++;
            return this.cpuBus.Read(0x100 + this.S);
        }

        internal class Instruction : IInstruction
        {
            internal Instruction(string mnemonic, string operands, string operandsDetail)
            {
                this.Mnemonic = mnemonic;
                this.Operands = operands;
                this.OperandsDetail = operandsDetail;
            }

            public string Mnemonic { get; private set; }
            public string Operands { get; private set; }
            public string OperandsDetail { get; private set; }
        }

        private IInstruction DecodeInstruction(InstructionData op, byte arg1, byte arg2)
        {
            bool pageFault;
            UInt16 addr = this.DecodeTargetAddr(op.AddressingMode, arg1, arg2, out pageFault);

            string mnemonic = String.Format(CultureInfo.CurrentCulture, "{0}{1}", op.IsUnofficial ? "*" : String.Empty, op.Mnemonic);
            string operands = String.Empty;
            string detail = String.Empty;

            switch (op.AddressingMode)
            {
                case AddressingMode.Implicit:
                    break;

                case AddressingMode.Accumulator:
                    operands = "A";
                    break;

                case AddressingMode.Immediate:
                    operands = String.Format(CultureInfo.InvariantCulture, "#${0:X2}", arg1);
                    break;

                case AddressingMode.ZeroPage:
                    operands = String.Format(CultureInfo.InvariantCulture, "${0:X2}", addr);
                    detail = String.Format(CultureInfo.InvariantCulture, "= {0:X2}", this.cpuBus.Read(addr));
                    break;

                case AddressingMode.ZeroPageIndexedX:
                    operands = String.Format(CultureInfo.InvariantCulture, "${0:X2},X", (byte)(addr - this.X));
                    detail = String.Format(CultureInfo.InvariantCulture, "@ {0:X2} = {1:X2}", addr, this.cpuBus.Read(addr));
                    break;

                case AddressingMode.ZeroPageIndexedY:
                    operands = String.Format(CultureInfo.InvariantCulture, "${0:X2},Y", (byte)(addr - this.Y));
                    detail = String.Format(CultureInfo.InvariantCulture, "@ {0:X2} = {1:X2}", addr, cpuBus.Read(addr));
                    break;

                case AddressingMode.Absolute:
                    operands = String.Format(CultureInfo.InvariantCulture, "${0:X4}", addr);
                    if (op.Implementation != null)
                    {
                        // Dereference pointer for non-jump instructions
                        detail = String.Format(CultureInfo.InvariantCulture, "= {0:X2}", this.cpuBus.Read(addr));
                    }
                    break;

                case AddressingMode.AbsoluteIndexedX:
                    operands = String.Format(CultureInfo.InvariantCulture, "${0:X4},X", (UInt16)(addr - this.X));
                    detail = String.Format(CultureInfo.InvariantCulture, "@ {0:X4} = {1:X2}", addr, cpuBus.Read(addr));
                    break;

                case AddressingMode.AbsoluteIndexedY:
                    operands = String.Format(CultureInfo.InvariantCulture, "${0:X4},Y", (UInt16)(addr - this.Y));
                    detail = String.Format(CultureInfo.InvariantCulture, "@ {0:X4} = {1:X2}", addr, cpuBus.Read(addr));
                    break;

                case AddressingMode.Relative:
                    operands = String.Format(CultureInfo.InvariantCulture, "${0:X4}", this.PC + (sbyte)arg1 + op.Size);
                    break;

                case AddressingMode.Indirect:
                    operands = String.Format(CultureInfo.InvariantCulture, "(${0:X4})", addr);
                    detail = String.Format(CultureInfo.InvariantCulture, "= {0:X4}", this.cpuBus.ReadUInt16LE(addr));
                    break;

                case AddressingMode.IndexedIndirect:
                    operands = String.Format(CultureInfo.InvariantCulture, "(${0:X2},X)", arg1);
                    detail = String.Format(CultureInfo.InvariantCulture, "@ {0:X2} = {1:X4} = {2:X2}", (arg1 + this.X) % 0x100, addr, this.cpuBus.Read(addr));
                    break;

                case AddressingMode.IndirectIndexed:
                    operands = String.Format(CultureInfo.InvariantCulture, "(${0:X2}),Y", arg1);
                    detail = String.Format(CultureInfo.InvariantCulture, "= {0:X4} @ {1:X4} = {2:X2}", (UInt16)(addr - this.Y), addr, this.cpuBus.Read(addr));
                    break;

            }

            return new Instruction(mnemonic, operands, detail);
        }

        #endregion

        #region IEmulatorComponent Implementation

        public string Name
        {
            get { return "Ricoh 2A03 CPU"; }
        }

        #endregion

        #region IProcessorCore Implementation

        int IProcessorCore.Step()
        {
            if (this.ProcessOamDma())
            {
                return 1;
            }

            if (this.CheckInterrupts())
            {
                this.DispatchInterrupt(this.pendingEvent);
            }

            return this.DispatchNextInstruction();
        }

        void IProcessorCore.Reset()
        {
            this.DispatchInterrupt(EventType.Reset);
        }

        IEnumerable<byte> IProcessorCore.GetInstructionBytes(int address)
        {
            InstructionData op;
            byte arg1;
            byte arg2;

            if (!this.FetchInstruction((UInt16)address, out op, out arg1, out arg2))
            {
                return Enumerable.Empty<byte>();
            }

            if (op.Size == 1)
            {
                return new byte[] { op.Opcode };
            }
            else if (op.Size == 2)
            {
                return new byte[] { op.Opcode, arg1 };
            }
            else
            {
                return new byte[] { op.Opcode, arg1, arg2 };
            }
        }

        IInstruction IProcessorCore.DecodeInstruction(IEnumerable<byte> instructionBytes)
        {
            byte[] bytes = instructionBytes.ToArray();
            if (bytes.Length == 0 || bytes.Length > 3)
            {
                throw new InvalidOperationException("Must decode 1-3 bytes!");
            }

            InstructionData op = this.instructions[bytes[0]];
            if (op == null)
            {
                throw new InvalidOperationException("Not a valid instruction!");
            }

            if (bytes.Length != op.Size)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Expected {0} bytes while decoding '{1}'!", op.Size, op.Mnemonic));
            }

            byte arg1 = (bytes.Length > 1) ? bytes[1] : (byte)0;
            byte arg2 = (bytes.Length > 2) ? bytes[2] : (byte)0;

            return this.DecodeInstruction(op, arg1, arg2);
        }

        IEnumerable<IProcessorInterrupt> IProcessorCore.Interrupts
        {
            get { return this.interrupts; }
        }

        IEnumerable<IProcessorRegister> IProcessorCore.Registers
        {
            get { return this.registers; }
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            return 0;
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            if (address == Ricoh2A03.OAMDMA_ADDR)
            {
                // Begin DMA transfer to OAM
                this.oamDmaActive = true;
                this.oamDmaCycle = 0;
                this.oamDmaBaseAddress = (UInt16)(value * 0x100);
            }
        }

        #endregion
    }
}
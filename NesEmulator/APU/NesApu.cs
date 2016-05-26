using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Audio;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Memory;
using EmulatorCore.Extensions;

namespace NesEmulator.APU
{
    internal class NesApu : IComponentWithClock, IMemoryMappedDevice, IPartImportsSatisfiedNotification
    {
        #region Constants

        private const UInt16 PULSE1_REGISTER1_ADDR = 0x4000;
        private const UInt16 PULSE1_REGISTER2_ADDR = 0x4001;
        private const UInt16 PULSE1_REGISTER3_ADDR = 0x4002;
        private const UInt16 PULSE1_REGISTER4_ADDR = 0x4003;

        private const UInt16 PULSE2_REGISTER1_ADDR = 0x4004;
        private const UInt16 PULSE2_REGISTER2_ADDR = 0x4005;
        private const UInt16 PULSE2_REGISTER3_ADDR = 0x4006;
        private const UInt16 PULSE2_REGISTER4_ADDR = 0x4007;

        private const UInt16 TRIANGLE_REGISTER1_ADDR = 0x4008;
        private const UInt16 TRIANGLE_REGISTER3_ADDR = 0x400A;
        private const UInt16 TRIANGLE_REGISTER4_ADDR = 0x400B;

        private const UInt16 NOISE_REGISTER1_ADDR = 0x400C;
        private const UInt16 NOISE_REGISTER3_ADDR = 0x400E;
        private const UInt16 NOISE_REGISTER4_ADDR = 0x400F;

        private const UInt16 DMC_REGISTER1_ADDR = 0x4010;
        private const UInt16 DMC_REGISTER2_ADDR = 0x4011;
        private const UInt16 DMC_REGISTER3_ADDR = 0x4012;
        private const UInt16 DMC_REGISTER4_ADDR = 0x4013;

        private const UInt16 APU_STATUS_ADDR = 0x4015;
        private const UInt16 FRAME_COUNTER_ADDR = 0x4017;

        private const int BUFFER_SIZE = 4096;

        internal static readonly byte[] LengthValues = new byte[] {
            0x0A, 0xFE, 0x14, 0x02, 0x28, 0x04, 0x50, 0x06, 0xA0, 0x08, 0x3C, 0x0A, 0x0E, 0x0C, 0x1A, 0x0E,
            0x0C, 0x10, 0x10, 0x12, 0x30, 0x14, 0x60, 0x16, 0xC0, 0x18, 0x48, 0x1A, 0x10, 0x1C, 0x20, 0x1E };

        #endregion

        #region MEF Imports

        [Import(typeof(IAudioRenderer))]
        private IAudioRenderer AudioRenderer { get; set; }

        #endregion

        #region Registers

        private bool frameCounter5Step;
        private bool irqInhibit;
        private int frameCounterUpdate;

        private void SetFrameCounterRegister(byte value)
        {
            this.frameCounter5Step = ((value & 0x80) == 0x80);
            this.irqInhibit = ((value & 0x40) == 0x40);

            // Side effects of updating this register are delayed by several cycles
            if ((value & 0xC0) == 0xC0)
            {
                this.frameCounterUpdate = this.isApuCycle ? 3 : 4;
            }

            // Clear any asserted IRQ when the inhibit bit is set
            if (this.irqInhibit && this.frameIrqAssertion != null)
            {
                this.frameIrqAssertion.Dispose();
                this.frameIrqAssertion = null;
            }
        }

        private byte APU_STATUS
        {
            get
            {
                byte value = (byte)((this.dmc.InterruptAsserted ? 0x80 : 0x00) |
                                    (this.frameIrqAssertion != null ? 0x40 : 0x00) |
                                    (this.dmc.Enabled ? 0x10 : 0x00) |
                                    (this.noise.Enabled ? 0x08 : 0x00) |
                                    (this.triangle.Enabled ? 0x04 : 0x00) |
                                    (this.pulse2.Enabled ? 0x02 : 0x00) |
                                    (this.pulse1.Enabled ? 0x01 : 0x00));

                // Reading clears the frame counter IRQ, but not the DMC IRQ
                if (this.frameIrqAssertion != null)
                {
                    this.frameIrqAssertion.Dispose();
                    this.frameIrqAssertion = null;
                }

                return value;
            }
            set
            {
                this.pulse1.Enabled = ((value & 0x01) == 0x01);
                this.pulse2.Enabled = ((value & 0x02) == 0x02);
                this.triangle.Enabled = ((value & 0x04) == 0x04);
                this.noise.Enabled = ((value & 0x08) == 0x08);
                this.dmc.Enabled = ((value & 0x10) == 0x10);

                // Writing clears the DMC register, but not the frame counter IRQ
                this.dmc.ClearInterrupt();
            }
        }

        #endregion

        #region Private Fields

        private IProcessorInterrupt cpuIrq;
        private IDisposable frameIrqAssertion;

        private PulseChannel pulse1;
        private PulseChannel pulse2;
        private TriangleChannel triangle;
        private NoiseChannel noise;
        private DeltaChannel dmc;

        private bool isApuCycle;
        private int cyclesPerSample;
        private int cycle;
        private byte[] buffer;
        private int bufferPos;
        private IntPtr ptr;
        private int frameCounterCycle;

        #endregion

        #region Constructor

        internal NesApu(IMemoryBus cpuBus, IProcessorCore cpu)
        {
            this.cpuIrq = cpu.GetInterruptByName("IRQ");

            this.pulse1 = new PulseChannel(true);
            this.pulse2 = new PulseChannel(false);
            this.triangle = new TriangleChannel();
            this.noise = new NoiseChannel();
            this.dmc = new DeltaChannel(cpuBus, cpuIrq);

            this.isApuCycle = false;
            this.buffer = new byte[NesApu.BUFFER_SIZE];
            this.ptr = Marshal.AllocHGlobal(4);
        }

        #endregion

        #region IComponentWithClock Implementation

        void IComponentWithClock.Tick()
        {
            if (this.isApuCycle)
            {
                // Most channels are ticked once per "APU cycle", or every other CPU cycle
                this.pulse1.Tick();
                this.pulse2.Tick();
                this.noise.Tick();
                this.dmc.Tick();
            }

            // Triangle ticks every CPU cycle
            this.triangle.Tick();
            this.TickFrameCounter();

            // Generate audio sample
            this.TickSampleGenerator();

            this.isApuCycle = !this.isApuCycle;
        }

        #endregion

        #region Frame Counter

        private void TickFrameCounter()
        {
            if (this.frameCounterUpdate >= 0)
            {
                this.frameCounterUpdate--;

                if (this.frameCounterUpdate == 0)
                {
                    // Correct number of cycles have elapsed since the frame counter register was updated - reset
                    //  the frame count and tick all clocks.
                    if (this.frameCounter5Step)
                    {
                        this.TickQuarterFrameClocks();
                        this.TickHalfFrameClocks();
                    }

                    this.frameCounterCycle = 0;
                }
            }

            switch (this.frameCounterCycle)
            {
                case 0:
                    this.AssertFrameIrq();
                    break;

                case 7457:
                    this.TickQuarterFrameClocks();
                    break;

                case 14913:
                    this.TickQuarterFrameClocks();
                    this.TickHalfFrameClocks();
                    break;

                case 22371:
                    this.TickQuarterFrameClocks();
                    break;

                case 29828:
                    this.AssertFrameIrq();
                    break;

                case 29829:
                    if (!this.frameCounter5Step)
                    {
                        // This is the last cycle of the 4-step cycle, but is skipped in the 5-step cycle
                        this.AssertFrameIrq();

                        this.TickQuarterFrameClocks();
                        this.TickHalfFrameClocks();

                        this.frameCounterCycle = 0;
                    }
                    break;

                case 37281:
                    this.TickQuarterFrameClocks();
                    this.TickHalfFrameClocks();

                    this.frameCounterCycle = 0;
                    break;
            }

            this.frameCounterCycle++;
        }

        private void AssertFrameIrq()
        {
            if (!this.irqInhibit && !this.frameCounter5Step && this.frameIrqAssertion == null)
            {
                this.frameIrqAssertion = this.cpuIrq.Assert();
            }
        }

        private void TickQuarterFrameClocks()
        {
            this.pulse1.TickQuarterFrame();
            this.pulse2.TickQuarterFrame();
            this.triangle.TickQuarterFrame();
            this.noise.TickQuarterFrame();
        }

        private void TickHalfFrameClocks()
        {
            this.pulse1.TickHalfFrame();
            this.pulse2.TickHalfFrame();
            this.triangle.TickHalfFrame();
            this.noise.TickHalfFrame();
        }

        #endregion

        #region Sample Generation

        private void TickSampleGenerator()
        {
            this.cycle--;
            if (this.cycle < 0)
            {
                this.cycle = this.cyclesPerSample;

                float pulseOut = 0;
                if (this.pulse1.OutputValue != 0 || this.pulse2.OutputValue != 0)
                {
                    pulseOut = 95.88f / ((8128f / (this.pulse1.OutputValue + this.pulse2.OutputValue)) + 100);
                }

                float tndOut = 0;
                if (this.triangle.OutputValue != 0 || this.noise.OutputValue != 0 || this.dmc.OutputValue != 0)
                {
                    tndOut = 159.79f / ((1f / ((this.triangle.OutputValue / 8227f) + (this.noise.OutputValue / 12241f) + (this.dmc.OutputValue / 22638f))) + 100);
                }

                if (this.AudioRenderer.IsFloatFormat)
                {
                    // Formula from NesDev produces a value between 0.0 and 1.0, we want -0.5 - 0.5
                    float apuOut = 0.5f - (pulseOut + tndOut);

                    Marshal.StructureToPtr<float>(apuOut, ptr, false);
                    Marshal.Copy(ptr, this.buffer, this.bufferPos, 4);
                    Marshal.Copy(ptr, this.buffer, this.bufferPos + 4, 4);
                }

                this.bufferPos += 8;
            }

            if (this.bufferPos == NesApu.BUFFER_SIZE)
            {
                this.AudioRenderer.EnqueueBuffer(this.buffer);
                this.buffer = new byte[NesApu.BUFFER_SIZE];
                this.bufferPos = 0;
            }
        }

        private void OnAudioFormatChanged(object sender, EventArgs e)
        {
            // Reset sample counters when format changes
            this.cyclesPerSample = (1789773 / this.AudioRenderer.SampleRate) - 1;
            this.cycle = this.cyclesPerSample;
            this.bufferPos = 0;
        }

        #endregion

        #region IPartImportsSatisfiedNotification Implementation

        public void OnImportsSatisfied()
        {
            this.AudioRenderer.FormatChanged += this.OnAudioFormatChanged;
            this.AudioRenderer.Initialize(4);
        }

        #endregion

        #region IEmulatorComponent Implementation

        string IEmulatorComponent.Name
        {
            get { return "NES APU"; }
        }

        #endregion

        #region IMemoryMappedDevice Implementation

        byte IMemoryMappedDevice.Read(int address)
        {
            if (address == APU_STATUS_ADDR)
            {
                return this.APU_STATUS;
            }

            return 0;
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            switch (address)
            {
                case PULSE1_REGISTER1_ADDR: this.pulse1.SetRegister1(value); break;
                case PULSE1_REGISTER2_ADDR: this.pulse1.SetRegister2(value); break;
                case PULSE1_REGISTER3_ADDR: this.pulse1.SetRegister3(value); break;
                case PULSE1_REGISTER4_ADDR: this.pulse1.SetRegister4(value); break;

                case PULSE2_REGISTER1_ADDR: this.pulse2.SetRegister1(value); break;
                case PULSE2_REGISTER2_ADDR: this.pulse2.SetRegister2(value); break;
                case PULSE2_REGISTER3_ADDR: this.pulse2.SetRegister3(value); break;
                case PULSE2_REGISTER4_ADDR: this.pulse2.SetRegister4(value); break;

                case TRIANGLE_REGISTER1_ADDR: this.triangle.SetRegister1(value); break;
                case TRIANGLE_REGISTER3_ADDR: this.triangle.SetRegister3(value); break;
                case TRIANGLE_REGISTER4_ADDR: this.triangle.SetRegister4(value); break;

                case NOISE_REGISTER1_ADDR: this.noise.SetRegister1(value); break;
                case NOISE_REGISTER3_ADDR: this.noise.SetRegister3(value); break;
                case NOISE_REGISTER4_ADDR: this.noise.SetRegister4(value); break;

                case DMC_REGISTER1_ADDR: this.dmc.SetRegister1(value); break;
                case DMC_REGISTER2_ADDR: this.dmc.SetRegister2(value); break;
                case DMC_REGISTER3_ADDR: this.dmc.SetRegister3(value); break;
                case DMC_REGISTER4_ADDR: this.dmc.SetRegister4(value); break;

                case FRAME_COUNTER_ADDR: this.SetFrameCounterRegister(value); break;

                case APU_STATUS_ADDR: this.APU_STATUS = value; break;
            }
        }

        #endregion
    }
}

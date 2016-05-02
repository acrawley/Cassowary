using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Audio;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.Memory;

namespace NesEmulator.APU
{
    internal class NesApu : IEmulatorComponent, IMemoryMappedDevice, IPartImportsSatisfiedNotification
    {
        #region Constants

        private const UInt16 PULSE1_REGISTER1 = 0x4000;
        private const UInt16 PULSE1_REGISTER2 = 0x4001;
        private const UInt16 PULSE1_REGISTER3 = 0x4002;
        private const UInt16 PULSE1_REGISTER4 = 0x4003;

        private const UInt16 PULSE2_REGISTER1 = 0x4004;
        private const UInt16 PULSE2_REGISTER2 = 0x4005;
        private const UInt16 PULSE2_REGISTER3 = 0x4006;
        private const UInt16 PULSE2_REGISTER4 = 0x4007;

        private const UInt16 TRIANGLE_REGISTER1 = 0x4008;
        private const UInt16 TRIANGLE_REGISTER2 = 0x400A;
        private const UInt16 TRIANGLE_REGISTER3 = 0x400B;

        private const UInt16 NOISE_REGISTER1 = 0x400C;
        private const UInt16 NOISE_REGISTER2 = 0x400E;
        private const UInt16 NOISE_REGISTER3 = 0x400F;

        private const UInt16 DMC_REGISTER1 = 0x4010;
        private const UInt16 DMC_REGISTER2 = 0x4011;
        private const UInt16 DMC_REGISTER3 = 0x4012;
        private const UInt16 DMC_REGISTER4 = 0x4013;

        private const UInt16 APU_STATUS = 0x4015;
        private const UInt16 FRAME_COUNTER = 0x4017;

        private const int BUFFER_SIZE = 4096;

        #endregion

        #region MEF Imports

        [Import(typeof(IAudioRenderer))]
        private IAudioRenderer Renderer { get; set; }

        #endregion

        PulseChannel pulse1;
        PulseChannel pulse2;
        TriangleChannel triangle;

        bool isApuCycle;
        int cyclesPerSample;
        int cycle;
        byte[] buffer;
        int bufferPos;
        IntPtr ptr;

        #region Registers

        #endregion

        internal NesApu()
        {
            this.pulse1 = new PulseChannel();
            this.pulse2 = new PulseChannel();
            this.triangle = new TriangleChannel();

            this.isApuCycle = false;
            this.buffer = new byte[NesApu.BUFFER_SIZE];
            this.bufferPos = 0;
            this.ptr = Marshal.AllocHGlobal(4);
        }

        internal void Tick()
        {
            if (this.isApuCycle)
            {
                // Pulse channels are ticked once per "APU cycle", or every other CPU cycle
                this.pulse1.Tick();
                this.pulse2.Tick();
            }

            this.triangle.Tick();

            this.cycle--;
            if (this.cycle < 0)
            {
                this.cycle = this.cyclesPerSample;

                float pulseOut = 0;
                if (this.pulse1.OutputValue != 0 || this.pulse2.OutputValue != 0)
                {
                    pulseOut = 95.88f / ((8128 / (this.pulse1.OutputValue + this.pulse2.OutputValue)) + 100);
                }

                byte[] sample = new byte[4];

                Marshal.StructureToPtr<float>(pulseOut, ptr, false);
                Marshal.Copy(ptr, this.buffer, this.bufferPos, 4);
                Marshal.Copy(ptr, this.buffer, this.bufferPos + 4, 4);
                this.bufferPos += 8;
            }

            if (this.bufferPos == this.buffer.Length)
            {
                this.Renderer.EnqueueBuffer(this.buffer);
                this.buffer = new byte[NesApu.BUFFER_SIZE];
                this.bufferPos = 0;
            }

            this.isApuCycle = !this.isApuCycle;
        }

        #region IPartImportsSatisfiedNotification Implementation

        public void OnImportsSatisfied()
        {
            this.Renderer.Initialize(4);

            this.cyclesPerSample = 1789773 / this.Renderer.SampleRate;
            this.cycle = this.cyclesPerSample;
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
            return 0;
        }

        void IMemoryMappedDevice.Write(int address, byte value)
        {
            switch (address)
            {
                case PULSE1_REGISTER1: this.pulse1.SetRegister1(value); break;
                case PULSE1_REGISTER2: this.pulse1.SetRegister2(value); break;
                case PULSE1_REGISTER3: this.pulse1.SetRegister3(value); break;
                case PULSE1_REGISTER4: this.pulse1.SetRegister4(value); break;

                case PULSE2_REGISTER1: this.pulse2.SetRegister1(value); break;
                case PULSE2_REGISTER2: this.pulse2.SetRegister2(value); break;
                case PULSE2_REGISTER3: this.pulse2.SetRegister3(value); break;
                case PULSE2_REGISTER4: this.pulse2.SetRegister4(value); break;
            }
        }

        #endregion
    }
}

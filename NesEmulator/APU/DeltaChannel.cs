using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.CPU;
using EmulatorCore.Components.Memory;

namespace NesEmulator.APU
{
    internal class DeltaChannel
    {
        #region Constants

        private static readonly UInt16[] Rates = new UInt16[] {
            0xD6, 0xBE, 0xAA, 0xA0, 0x8F, 0x7F, 0x71, 0x6B, 0x5F, 0x50, 0x47, 0x40, 0x35, 0x2A, 0x24, 0x1B };

        #endregion

        #region Registers

        private bool irqEnabled;
        private bool loop;
        private UInt16 timer;

        internal void SetRegister1(byte value)
        {
            this.irqEnabled = ((value & 0x80) == 0x80);
            this.loop = ((value & 0x40) == 0x40);
            this.timer = DeltaChannel.Rates[value & 0x0F];

            if (!this.irqEnabled)
            {
                this.ClearInterrupt();
            }
        }

        internal void SetRegister2(byte value)
        {
            this.OutputValue = (value & 0x7F);
        }

        private UInt16 sampleAddress;

        internal void SetRegister3(byte value)
        {
            this.sampleAddress = (UInt16)(0xC000 + (value * 64));
        }

        private UInt16 sampleLength;

        internal void SetRegister4(byte value)
        {
            this.sampleLength = (UInt16)((value * 16) + 1);
        }

        #endregion

        #region Private Fields

        private IMemoryBus cpuBus;
        private IProcessorInterrupt dmcInterrupt;
        private IDisposable dmcInterruptAssertion;

        private byte sampleBuffer;
        private bool sampleBufferEmpty;

        private byte sampleShiftRegister;
        private int bitsRemaining;
        private bool silence;

        private UInt16 currentAddress;
        private UInt16 bytesRemaining;
        private int timerValue;
        private bool enabled;

        #endregion

        #region Delta Modulation Channel Implementation

        internal DeltaChannel(IMemoryBus cpuBus, IProcessorInterrupt irq)
        {
            this.cpuBus = cpuBus;
            this.dmcInterrupt = irq;
        }

        internal void Tick()
        {
            if (this.timerValue == 0)
            {
                this.timerValue = this.timer;

                if (this.bitsRemaining == 0)
                {
                    this.bitsRemaining = 8;
                    this.sampleShiftRegister = this.sampleBuffer;
                    this.silence = this.sampleBufferEmpty;

                    this.LoadNextSampleByte();
                }
                else
                {
                    if (!this.silence)
                    {
                        int newValue = this.OutputValue + (((this.sampleShiftRegister & 0x01) == 0x01) ? 2 : -2);

                        if (newValue >= 0 && newValue <= 127)
                        {
                            this.OutputValue = newValue;
                        }

                        this.sampleShiftRegister >>= 1;
                    }

                    this.bitsRemaining--;
                }
            }
            else
            {
                this.timerValue--;
            }
        }

        private void LoadNextSampleByte()
        {
            if (this.bytesRemaining == 0)
            {
                this.sampleBufferEmpty = true;
                this.sampleBuffer = 0;
                return;
            }

            this.sampleBuffer = this.cpuBus.Read(this.currentAddress);
            if (this.currentAddress == 0xFFFF)
            {
                this.currentAddress = 0x8000;
            }
            else
            {
                this.currentAddress++;
            }

            this.bytesRemaining--;
            if (this.bytesRemaining == 0)
            {
                if (this.loop)
                {
                    this.currentAddress = this.sampleAddress;
                    this.bytesRemaining = this.sampleLength;
                }
                else if (this.irqEnabled && this.dmcInterruptAssertion == null)
                {
                    this.dmcInterruptAssertion = this.dmcInterrupt.Assert();
                }
            }

            this.sampleBufferEmpty = false;
        }

        internal int OutputValue { get; private set; }

        internal bool Enabled
        {
            get { return this.bytesRemaining > 0; }
            set
            {
                this.enabled = value;

                if (!this.enabled)
                {
                    this.bytesRemaining = 0;
                }
                else if (this.bytesRemaining == 0)
                {
                    this.currentAddress = this.sampleAddress;
                    this.bytesRemaining = this.sampleLength;

                    this.LoadNextSampleByte();
                }
            }
        }

        internal bool InterruptAsserted
        {
            get { return this.dmcInterruptAssertion != null; }
        }

        internal void ClearInterrupt()
        {
            if (this.dmcInterruptAssertion != null)
            {
                this.dmcInterruptAssertion.Dispose();
                this.dmcInterruptAssertion = null;
            }
        }

        #endregion
    }
}

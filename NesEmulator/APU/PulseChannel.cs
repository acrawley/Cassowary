using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesEmulator.APU
{
    internal class PulseChannel
    {
        #region Constants

        private static readonly byte[,] Waveform = {
            // 12.5% duty cycle
            {0, 1, 0, 0, 0, 0, 0, 0 },

            // 25% duty cycle
            {0, 1, 1, 0, 0, 0, 0, 0 },

            // 50% duty cycle
            {0, 1, 1, 1, 1, 0, 0, 0 },
            
            // 25% duty cycle, inverted
            {1, 0, 0, 1, 1, 1, 1, 1 } };

        #endregion

        #region Registers

        private byte duty;
        private bool haltLengthCounter;
        private bool constantVolume;
        private byte volume;

        internal void SetRegister1(byte value)
        {
            this.duty = (byte)((value & 0xC0) >> 6);
            this.haltLengthCounter = ((value & 0x20) == 0x20);
            this.constantVolume = ((value & 0x10) == 0x10);
            this.volume = (byte)(value & 0x0F);
        }

        private bool sweepEnabled;
        private byte sweepPeriod;
        private bool sweepNegative;
        private byte sweepCount;

        internal void SetRegister2(byte value)
        {
            this.sweepEnabled = ((value & 0x80) == 0x80);
            this.sweepPeriod = (byte)((value & 0x70) >> 4);
            this.sweepNegative = ((value & 0x08) == 0x08);
            this.sweepCount = (byte)(value & 0x07);
        }

        private UInt16 timer;
        private byte lengthCounter;

        internal void SetRegister3(byte value)
        {
            this.timer = (UInt16)((this.timer & 0x300) | value);
        }

        internal void SetRegister4(byte value)
        {
            this.timer = (UInt16)(((value & 0x07) << 8) | (this.timer & 0xFF));
            this.lengthCounter = (byte)((value & 0xF8) >> 3);

            // TODO: Envelope
            this.sequenceIndex = 0;
            this.timerValue = this.timer;

            // Output is silenced if timer is less than 8 - max freq of pulse channel is ~12.4KHz
            if (this.timer < 8)
            {
                this.OutputValue = 0;
            }
        }

        #endregion

        #region Private Fields

        private int sequenceIndex;
        private int timerValue;

        #endregion

        internal void Tick()
        {
            if (this.timer >= 8)
            {
                this.timerValue--;
                if (this.timerValue < 0)
                {
                    this.timerValue = this.timer;

                    this.OutputValue = (byte)(15 * PulseChannel.Waveform[this.duty, this.sequenceIndex]);

                    this.sequenceIndex--;
                    if (this.sequenceIndex < 0)
                    {
                        this.sequenceIndex = 7;                        
                    }
                }
            }
        }

        internal byte OutputValue { get; private set; }
    }
}

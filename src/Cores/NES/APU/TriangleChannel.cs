using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Core.Nes.APU
{
    internal class TriangleChannel
    {
        #region Constants

        private static readonly byte[] Waveform = new byte[] {
            0x0F, 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

        #endregion

        #region Registers

        bool haltLengthCounter;
        byte linearCounter;

        internal void SetRegister1(byte value)
        {
            this.haltLengthCounter = ((value & 0x80) == 0x80);
            this.linearCounter = (byte)(value & 0x7F);
        }

        private UInt16 timer;
        private byte lengthCounter;
        private bool reloadLinearCounter;

        internal void SetRegister3(byte value)
        {
            this.timer = (UInt16)((this.timer & 0x300) | value);
        }

        internal void SetRegister4(byte value)
        {
            this.timer = (UInt16)(((value & 0x07) << 8) | (this.timer & 0xFF));

            if (this.enabled)
            {
                this.lengthCounter = NesApu.LengthValues[((value & 0xF8) >> 3)];
            }

            this.reloadLinearCounter = true;
        }

        #endregion

        #region Private Fields

        private bool enabled;
        private int sequenceIndex;
        private int timerValue;
        private int linearCounterValue;

        #endregion

        #region Triangle Channel Implementation

        internal void Tick()
        {
            if (this.timerValue == 0)
            {
                this.timerValue = this.timer;

                if (this.lengthCounter > 0 && this.linearCounterValue > 0)
                {
                    // Only clock the sequencer if the counters allow
                    this.sequenceIndex++;
                    if (this.sequenceIndex > 31)
                    {
                        this.sequenceIndex = 0;
                    }
                }
            }
            else
            {
                this.timerValue--;
            }
        }

        internal void TickQuarterFrame()
        {
            if (this.reloadLinearCounter)
            {
                this.linearCounterValue = this.linearCounter;
            }
            else if (this.linearCounterValue != 0)
            {
                this.linearCounterValue--;
            }

            if (!this.haltLengthCounter)
            {
                this.reloadLinearCounter = false;
            }
        }

        internal void TickHalfFrame()
        {
            if (!this.haltLengthCounter && this.lengthCounter != 0)
            {
                this.lengthCounter--;
            }
        }

        internal int OutputValue
        {
            get { return TriangleChannel.Waveform[this.sequenceIndex]; }
        }

        internal bool Enabled
        {
            get { return this.lengthCounter > 0; }
            set
            {
                this.enabled = value;
                if (!this.enabled)
                {
                    this.lengthCounter = 0;
                }
            }
        }

        #endregion
    }
}

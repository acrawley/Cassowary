using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesEmulator.APU
{
    internal class NoiseChannel
    {
        #region Constants

        private static readonly UInt16[] Period = new UInt16[] {
            0x002, 0x004, 0x008, 0x010, 0x020, 0x030, 0x040, 0x050, 0x065, 0x07F, 0x0BE, 0x0FE, 0x17D, 0x1FC, 0x3F9, 0x7F2 };

        #endregion

        #region Registers

        private bool haltLengthCounter;
        private bool constantVolume;
        private byte volume;

        internal void SetRegister1(byte value)
        {
            this.haltLengthCounter = ((value & 0x20) == 0x20);
            this.constantVolume = ((value & 0x10) == 0x10);
            this.volume = (byte)(value & 0x0F);
        }

        bool modeFlag;
        UInt16 timer;

        internal void SetRegister3(byte value)
        {
            this.modeFlag = ((value & 0x80) == 0x80);
            this.timer = NoiseChannel.Period[value & 0x0F];
        }

        private byte lengthCounter;
        private bool reloadEnvelope;

        internal void SetRegister4(byte value)
        {
            if (this.enabled)
            {
                this.lengthCounter = NesApu.LengthValues[((value & 0xF8) >> 3)];
            }

            this.reloadEnvelope = true;
        }

        #endregion

        #region Private Fields

        private bool enabled;
        private UInt16 shiftRegister;
        private int timerValue;
        private int envelopeCounter;
        private int envelopeDividerValue;

        #endregion

        #region Noise Channel Implementation

        internal NoiseChannel()
        {
            this.shiftRegister = 1;
        }

        internal void Tick()
        {
            if (this.timerValue == 0)
            {
                this.timerValue = this.timer;

                bool feedback = ((this.shiftRegister & 0x01) == 0x01) ^
                                 (this.modeFlag ?
                                    ((this.shiftRegister & 0x40) == 0x40) :
                                    ((this.shiftRegister & 0x02) == 0x02));

                this.shiftRegister >>= 1;

                if (feedback)
                {
                    this.shiftRegister |= 0x4000;
                }
            }
            else
            {
                this.timerValue--;
            }
        }

        internal void TickQuarterFrame()
        {
            if (this.reloadEnvelope)
            {
                this.reloadEnvelope = false;
                this.envelopeCounter = 15;
                this.envelopeDividerValue = this.volume;
            }
            else
            {
                if (this.envelopeDividerValue == 0)
                {
                    this.envelopeDividerValue = this.volume;

                    if (this.envelopeCounter > 0)
                    {
                        this.envelopeCounter--;
                    }
                    else if (this.haltLengthCounter)
                    {
                        this.envelopeCounter = 15;
                    }
                }
                else
                {
                    this.envelopeDividerValue--;
                }
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
            get
            {
                if (this.lengthCounter > 0 && ((this.shiftRegister & 0x01) == 0x00))
                {
                    return this.constantVolume ? this.volume : this.envelopeCounter;
                }

                return 0;
            }
        }

        internal bool Enabled
        {
            get { return this.enabled && this.lengthCounter > 0; }
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

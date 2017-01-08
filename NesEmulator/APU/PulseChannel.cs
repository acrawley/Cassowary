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
        private byte sweepShiftCount;
        private bool reloadSweep;

        internal void SetRegister2(byte value)
        {
            this.sweepEnabled = ((value & 0x80) == 0x80);
            this.sweepPeriod = (byte)((value & 0x70) >> 4);
            this.sweepNegative = ((value & 0x08) == 0x08);
            this.sweepShiftCount = (byte)(value & 0x07);

            this.reloadSweep = true;
        }

        private UInt16 timer;
        private byte lengthCounter;
        private bool reloadEnvelope;

        internal void SetRegister3(byte value)
        {
            this.timer = (UInt16)((this.timer & 0x700) | value);
        }

        internal void SetRegister4(byte value)
        {
            this.timer = (UInt16)(((value & 0x07) << 8) | (this.timer & 0xFF));

            if (this.enabled)
            {
                // Length counter can only be set when the channel is enabled
                this.lengthCounter = NesApu.LengthValues[((value & 0xF8) >> 3)];
            }

            this.reloadEnvelope = true;
            this.sequenceIndex = 0;

            this.UpdatePeriod(false);
        }

        #endregion

        #region Private Fields

        private bool isChannelOne;
        private bool enabled;

        private int sequenceIndex;
        private int timerValue;

        private int envelopeCounter;
        private int envelopeDividerValue;

        private int sweepDividerValue;
        private bool sweepOverflow;

        #endregion

        #region Pulse Channel Implementation

        internal PulseChannel(bool isChannelOne)
        {
            this.isChannelOne = isChannelOne;
        }

        internal void Tick()
        {
            if (this.timerValue == 0)
            {
                this.timerValue = this.timer;

                if (this.sequenceIndex == 0)
                {
                    this.sequenceIndex = 7;
                }
                else
                {
                    this.sequenceIndex--;
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
            // Length counter
            if (!this.haltLengthCounter && this.lengthCounter != 0)
            {
                this.lengthCounter--;
            }

            // Sweep
            if (this.reloadSweep)
            {
                if (this.sweepDividerValue == 0)
                {
                    this.UpdatePeriod(this.sweepEnabled);
                }

                this.sweepDividerValue = this.sweepPeriod;
                this.reloadSweep = false;
            }
            else if (this.sweepDividerValue != 0)
            {
                this.sweepDividerValue--;
            }
            else if (this.sweepDividerValue == 0)
            {
                this.sweepDividerValue = sweepPeriod;
                this.UpdatePeriod(this.sweepEnabled);
            }
        }

        private void UpdatePeriod(bool update)
        {
            int timerDelta = this.timer >> this.sweepShiftCount;

            // Channel one's sweep unit negates the one's complement of the delta, channel 2 negates normally
            timerDelta = this.sweepNegative ?
                            (this.isChannelOne ? (~timerDelta) : (-timerDelta)) :
                            timerDelta;

            int targetPeriod = this.timer + timerDelta;

            if (targetPeriod > 0x7FF || this.timer < 8)
            {
                this.sweepOverflow = true;
            }
            else
            {
                this.sweepOverflow = false;

                if (update && this.sweepShiftCount != 0)
                {
                    this.timer = (UInt16)targetPeriod;
                }
            }
        }

        internal int OutputValue
        {
            get
            {
                if (this.timer >= 8 && this.lengthCounter > 0 && !this.sweepOverflow)
                {
                    return (this.constantVolume ? this.volume : this.envelopeCounter) * PulseChannel.Waveform[this.duty, this.sequenceIndex];
                }

                return 0;
            }
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

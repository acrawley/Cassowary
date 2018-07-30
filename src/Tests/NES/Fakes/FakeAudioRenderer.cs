using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Audio;

namespace Cassowary.Tests.Nes.Fakes
{
    [Export(typeof(IAudioRenderer))]
    class FakeAudioRenderer : IAudioRenderer
    {
        public int BitsPerSample
        {
            get { return 32; }
        }

        public int Channels
        {
            get { return 2; }
        }

        public bool IsFloatFormat
        {
            get { return true; }
        }

        public int SampleRate
        {
            get { return 48000; }
        }

        public event EventHandler FormatChanged;

        public void EnqueueBuffer(byte[] sampleData)
        {
            
        }

        public void Initialize(int bufferCount)
        {
            this.FormatChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

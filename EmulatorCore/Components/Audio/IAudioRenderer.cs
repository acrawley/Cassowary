using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.Audio
{
    public interface IAudioRenderer
    {
        void Initialize(int bufferCount);

        int SampleRate { get; }
        int BitsPerSample { get; }
        int Channels { get; }

        bool IsFloatFormat { get; }

        void EnqueueBuffer(byte[] sampleData);
    }
}

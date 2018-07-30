using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Components.Audio
{
    public interface IAudioRenderer
    {
        void Initialize(int bufferCount);

        int SampleRate { get; }
        int BitsPerSample { get; }
        int Channels { get; }
        bool IsFloatFormat { get; }
        event EventHandler FormatChanged;

        void EnqueueBuffer(byte[] sampleData);
    }
}

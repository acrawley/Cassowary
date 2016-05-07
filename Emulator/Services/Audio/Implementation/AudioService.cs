using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emulator.Services.Audio.Implementation.Interop;
using EmulatorCore.Components.Audio;

namespace Emulator.Services.Audio.Implementation
{
    [Export(typeof(IAudioRenderer))]
    [Export(typeof(IAudioService))]
    internal class AudioService : IAudioRenderer, IAudioService
    {
        #region Private Fields

        private AutoResetEvent bufferReadyEvent;
        private ManualResetEvent shutdownEvent;

        private IAudioRenderClient audioRenderClient;
        private IAudioClient audioClient;

        private UInt32 wasapiBufferSize;

        private BufferQueue bufferQueue;
        private int bufferOffset = 0;

        private Thread renderThread;

        #endregion

        #region Constructor

        internal AudioService()
        {
            this.bufferReadyEvent = new AutoResetEvent(false);
            this.shutdownEvent = new ManualResetEvent(false);
        }

        #endregion

        #region IAudioRenderer Implementation

        public int SampleRate { get; private set; }

        public int BitsPerSample { get; private set; }

        public int Channels { get; private set; }

        public bool IsFloatFormat { get; private set; }

        public event EventHandler FormatChanged;

        public void EnqueueBuffer(byte[] sampleData)
        {
            this.bufferQueue.Enqueue(sampleData);
        }

        public void Initialize(int bufferCount)
        {
            this.bufferQueue = new BufferQueue(bufferCount);

            this.InitializeAudioDevice();
        }

        private void InitializeAudioDevice()
        {
            // Get default audio device
            MMDeviceEnumerator deviceEnumeratorClass = new MMDeviceEnumerator();
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)deviceEnumeratorClass;

            IMMDevice defaultDevice;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out defaultDevice);

            // Log device name
            IPropertyStore store;
            defaultDevice.OpenPropertyStore(STGM.STGM_READ, out store);

            PROPVARIANT pv = new PROPVARIANT();
            PropertyKey pkey = PKEY.PKEY_DeviceInterface_FriendlyName;
            store.GetValue(ref pkey, out pv);

            Debug.WriteLine("Using audio device '{0}'", pv.Value);

            // Retrieve IAudioClient
            Guid iid = new Guid(Constants.IID_IAudioClient);
            IntPtr propVar = IntPtr.Zero;
            object obj;
            defaultDevice.Activate(ref iid, CLSCTX.CLSCTX_ALL, ref propVar, out obj);

            this.audioClient = (IAudioClient)obj;

            // Get default format
            IntPtr defaultFormat;
            this.audioClient.GetMixFormat(out defaultFormat);
            this.UpdateWaveFormatInfo(defaultFormat);

            // Initialize IAudioClient
            Guid g = Guid.Empty;
            this.audioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, (uint)AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_EVENTCALLBACK, 0, 0, defaultFormat, ref g);
            this.audioClient.GetBufferSize(out this.wasapiBufferSize);
            this.audioClient.SetEventHandle(this.bufferReadyEvent.SafeWaitHandle.DangerousGetHandle());

            // Retrieve IAudioRenderClient
            iid = new Guid(Constants.IID_IAudioRenderClient);
            object ppv;
            this.audioClient.GetService(ref iid, out ppv);

            this.audioRenderClient = (IAudioRenderClient)ppv;

            // Start processing samples
            this.audioClient.Start();
        }

        private void UpdateWaveFormatInfo(IntPtr defaultFormat)
        {
            WAVEFORMATEX format = Marshal.PtrToStructure<WAVEFORMATEX>(defaultFormat);
            this.SampleRate = (int)format.nSamplesPerSec;
            this.BitsPerSample = format.wBitsPerSample;
            this.Channels = format.nChannels;

            switch (format.wFormatTag)
            {
                case Constants.WAVE_FORMAT_PCM:
                    this.IsFloatFormat = false;
                    break;

                case Constants.WAVE_FORMAT_EXTENSIBLE:
                    {
                        WAVEFORMATEXTENSIBLE formatEx = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(defaultFormat);
                        if (formatEx.SubFormat == Constants.KSDATAFORMAT_SUBTYPE_PCM)
                        {
                            this.IsFloatFormat = false;
                        }
                        else if (formatEx.SubFormat == Constants.KSDATAFORMAT_SUBTYPE_IEEE_FLOAT)
                        {
                            this.IsFloatFormat = true;
                        }
                        else
                        {
                            Debug.Fail("Unknown value for WAVEFORMATEXTENSIBLE.SubFormat!");
                        }
                    }
                    break;

                default:
                    Debug.Fail("Unknown value for WAVEFORMATEX.wFormatTag!");
                    break;
            }

            if (this.FormatChanged != null)
            {
                this.FormatChanged(this, EventArgs.Empty);
            }
        }

        private void RenderThreadProc()
        {
            IntPtr buffer;
            WaitHandle[] handles = new WaitHandle[] {
                this.bufferReadyEvent,
                this.shutdownEvent,
            };

            while (true)
            {
                // Wait for WASAPI to signal that it needs data
                if (WaitHandle.WaitAny(handles) != 0)
                {
                    break;
                }

                // Retrieve next queued sample - this will block if there is no sample available
                byte[] sampleData = this.bufferQueue.Peek();
                if (sampleData == null)
                {
                    // Wait for sample was cancelled
                    break;
                }

                // Determine amount of data necessary to fill WASAPI buffer
                uint queuedFrames = 0;
                this.audioClient.GetCurrentPadding(out queuedFrames);
                uint availableBufferFrames = this.wasapiBufferSize - queuedFrames;
                uint availableSampleFrames = (uint)(sampleData.Length - this.bufferOffset) / 8;

                int framesToWrite = (int)Math.Min(availableBufferFrames, availableSampleFrames);
                if (framesToWrite == 0)
                {
                    // WASAPI seems to signal sometimes even if its buffers are full - ignore it
                    continue;
                }

                // Copy sample data to WASAPI buffer
                this.audioRenderClient.GetBuffer((uint)framesToWrite, out buffer);
                Marshal.Copy(sampleData, this.bufferOffset, buffer, framesToWrite * 8);
                this.audioRenderClient.ReleaseBuffer((uint)framesToWrite, 0);

                this.bufferOffset += framesToWrite * 8;

                // If we've consumed all the data from a queued sample, remove it from the queue
                if (this.bufferOffset == sampleData.Length)
                {
                    this.bufferOffset = 0;
                    this.bufferQueue.Dequeue();
                }
            }
        }

        #endregion

        #region IAudioService Implementation

        void IAudioService.Start()
        {
            this.renderThread = new Thread(this.RenderThreadProc);
            this.renderThread.Name = "Audio Render Thread";
            this.renderThread.Priority = ThreadPriority.Highest;
            this.renderThread.Start();
        }

        void IAudioService.Stop()
        {
            this.audioClient.Stop();
            this.shutdownEvent.Set();
            this.bufferQueue.CancelWait();
        }

        #endregion
    }
}

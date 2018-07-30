using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cassowary.Services.Audio.Implementation
{
    internal class BufferQueue
    {
        private ManualResetEvent dequeueEvent;
        private ManualResetEvent enqueueEvent;
        private AutoResetEvent cancelEvent;

        private WaitHandle[] dequeueEvents;
        private WaitHandle[] enqueueEvents;

        private object syncObject;
        private byte[][] buffers;
        private int head;
        private int tail;

        internal BufferQueue(int size)
        {
            // Initial state - dequeue blocks, enqueue does not
            this.dequeueEvent = new ManualResetEvent(false);
            this.enqueueEvent = new ManualResetEvent(true);
            this.cancelEvent = new AutoResetEvent(false);

            this.dequeueEvents = new WaitHandle[] {
                this.dequeueEvent,
                this.cancelEvent
            };

            this.enqueueEvents = new WaitHandle[] {
                this.enqueueEvent,
                this.cancelEvent
            };

            this.syncObject = new Object();
            this.buffers = new byte[size][];
            this.head = 0;
            this.tail = 0;
        }

        internal byte[] Dequeue()
        {
            if (WaitHandle.WaitAny(this.dequeueEvents) != 0)
            {
                return null;
            }

            lock (this.syncObject)
            {
                byte[] buffer = this.buffers[this.tail];
                this.buffers[this.tail] = null;
                this.tail++;
                if (this.tail == this.buffers.Length)
                {
                    this.tail = 0;
                }

                // Queue has space - pending enqueue can proceed
                this.enqueueEvent.Set();

                if (this.tail == this.head)
                {
                    // Queue is empty - further attempts to dequeue a buffer should block
                    this.dequeueEvent.Reset();
                }

                return buffer;
            }
        }

        internal byte[] Peek()
        {
            if (WaitHandle.WaitAny(this.dequeueEvents) != 0)
            {
                return null;
            }

            lock (this.syncObject)
            {
                return this.buffers[this.tail];
            }
        }

        internal void Enqueue(byte[] buffer)
        {
            if (WaitHandle.WaitAny(this.enqueueEvents) != 0)
            {
                return;
            }

            lock (this.syncObject)
            {
                this.buffers[this.head] = buffer;
                this.head++;

                if (this.head == this.buffers.Length)
                {
                    this.head = 0;
                }

                // Queue has data - pending dequeue can proceed
                this.dequeueEvent.Set();

                if (this.head == this.tail)
                {
                    // Queue is full - further attempt to enqueue a buffer should block
                    this.enqueueEvent.Reset();
                }
            }
        }

        internal void CancelWait()
        {
            this.cancelEvent.Set();
        }
    }
}

using System;
using System.Buffers;
using System.Diagnostics;
using MangleSocks.Core.Util.Threading;

namespace MangleSocks.Core.Socks
{
    sealed class DatagramReassembler : IDisposable
    {
        static readonly ArrayPool<ArraySegment<byte>> s_DefaultFragmentsPool = ArrayPool<ArraySegment<byte>>.Create();

        readonly ITimer _timer;
        readonly ArrayPool<byte> _bufferPool;
        readonly ArrayPool<ArraySegment<byte>> _fragmentsPool;
        readonly ArraySegment<byte>[] _fragments;
        int _count;
        bool _disposed;
        bool _lastSetWasDisposed;

        public DatagramReassembler(ITimerFactory timerFactory, ArrayPool<byte> bufferPool)
        {
            if (timerFactory == null) throw new ArgumentNullException(nameof(timerFactory));
            this._timer = timerFactory.Create(TimeSpan.FromSeconds(10), this.TimerCallback);
            this._bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            this._fragmentsPool = s_DefaultFragmentsPool;
            this._fragments = this._fragmentsPool.Rent(127);
            this._lastSetWasDisposed = true;
        }

        void TimerCallback()
        {
            lock (this._fragments)
            {
                this._timer.Stop();
                this.DiscardFragments();
            }
        }

        public ReassembledFragments GetCompletedSetOrAdd(byte[] fragment, int position, bool isFinalInSet)
        {
            return this.GetCompletedSetOrAdd(new ArraySegment<byte>(fragment), position, isFinalInSet);
        }

        public ReassembledFragments GetCompletedSetOrAdd(ArraySegment<byte> fragment, int position, bool isFinalInSet)
        {
            if (fragment.Array == null)
            {
                throw new ArgumentNullException(nameof(fragment), "Value has null array");
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (!this._lastSetWasDisposed)
            {
                throw new InvalidOperationException("BUG: Must dispose last set before invoking this method.");
            }

            lock (this._fragments)
            {
                var fragmentBuffer = fragment;
                var isSolitary = position == 0 && !isFinalInSet;

                if (isSolitary)
                {
                    this._timer.Stop();
                    this.DiscardFragments();
                    isFinalInSet = true;
                }
                else if (position != this._count + 1)
                {
                    this._timer.Stop();
                    this.DiscardFragments();
                    return default;
                }
                else
                {
                    var fragmentCopy = this._bufferPool.Rent(fragment.Count);
                    Buffer.BlockCopy(fragment.Array, fragment.Offset, fragmentCopy, 0, fragment.Count);
                    fragmentBuffer = new ArraySegment<byte>(fragmentCopy, 0, fragment.Count);
                }

                this._fragments[this._count++] = fragmentBuffer;

                if (isFinalInSet)
                {
                    this._timer.Stop();
                    return new ReassembledFragments(this);
                }

                this._timer.Reset();
                return default;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DatagramReassembler()
        {
            // Prevent leak of pooled buffers
            this.Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            lock (this._fragments)
            {
                if (disposing)
                {
                    this._timer?.Dispose();
                }

                this.DiscardFragments();
                this._fragmentsPool.Return(this._fragments, true);
            }

            this._disposed = true;
        }

        void DiscardFragments()
        {
            for (var i = 0; i < this._count; i++)
            {
                this._bufferPool.Return(this._fragments[i].Array);
            }
            Array.Clear(this._fragments, 0, this._count);
            this._count = 0;
        }

        [DebuggerDisplay("Count = {Count}")]
        internal struct ReassembledFragments : IDisposable
        {
            readonly DatagramReassembler _reassembler;

            public ReassembledFragments(DatagramReassembler reassembler)
            {
                this._reassembler = reassembler ?? throw new ArgumentNullException(nameof(reassembler));
                this._reassembler._lastSetWasDisposed = false;
            }

            public ArraySegment<byte> this[int index]
            {
                get
                {
                    if (this._reassembler == null)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    if (this._reassembler._disposed)
                    {
                        throw new ObjectDisposedException(this.GetType().FullName);
                    }

                    return this._reassembler._fragments[index];
                }
            }

            public int Count => this._reassembler?._count ?? 0;

            public void Dispose()
            {
                if (this._reassembler == null || this._reassembler._disposed)
                {
                    return;
                }

                if (this._reassembler._count == 1)
                {
                    this._reassembler._fragments[0] = default;
                    this._reassembler._count = 0;
                }
                else
                {
                    this._reassembler.DiscardFragments();
                }

                this._reassembler._lastSetWasDisposed = true;
            }
        }
    }
}
using System.Buffers;
using System.Threading;

namespace MangleSocks.Tests.Helpers
{
    class DebugArrayPool<T> : ArrayPool<T>
    {
        readonly ArrayPool<T> _pool;
        int _outstanding;

        public int Outstanding => Volatile.Read(ref this._outstanding);

        public DebugArrayPool()
        {
            this._pool = Create();
        }

        public override T[] Rent(int minimumLength)
        {
            Interlocked.Increment(ref this._outstanding);
            return this._pool.Rent(minimumLength);
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            Interlocked.Decrement(ref this._outstanding);
            this._pool.Return(array, clearArray);
        }
    }
}
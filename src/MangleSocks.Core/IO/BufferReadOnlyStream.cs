using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MangleSocks.Core.IO
{
    struct BufferReadOnlyStream : IReadOnlyStream
    {
        readonly MemoryStream _stream;

        public BufferReadOnlyStream(params byte[] buffer) : this(buffer, 0, buffer.Length) { }

        public BufferReadOnlyStream(byte[] buffer, int offset, int count)
        {
            this._stream = new MemoryStream(buffer, offset, count, false, true);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return this._stream.Read(buffer, offset, count);
        }

        public Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default)
        {
            return this._stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public void Dispose() { }
    }
}
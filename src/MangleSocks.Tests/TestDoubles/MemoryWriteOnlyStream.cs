using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MangleSocks.Core.IO;

namespace MangleSocks.Tests.TestDoubles
{
    sealed class MemoryWriteOnlyStream : IWriteOnlyStream
    {
        readonly MemoryStream _output;

        public MemoryWriteOnlyStream()
        {
            this._output = new MemoryStream();
        }

        public Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default)
        {
            return this._output.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public byte[] ToArray()
        {
            return this._output.ToArray();
        }

        public void Dispose() { }
    }
}
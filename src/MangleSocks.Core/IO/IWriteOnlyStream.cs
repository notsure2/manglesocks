using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangleSocks.Core.IO
{
    public interface IWriteOnlyStream : IDisposable
    {
        Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default);
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangleSocks.Core.IO
{
    public interface IReadOnlyStream : IDisposable
    {
        int Read(byte[] buffer, int offset, int count);

        Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default);
    }
}
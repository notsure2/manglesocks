using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MangleSocks.Core.IO
{
    static class ReadOnlyStreamExtensions
    {
        public static void ReadExactly<TReadOnlyStream>(
            this TReadOnlyStream stream,
            byte[] buffer,
            int offset,
            int count) where TReadOnlyStream : IReadOnlyStream
        {
            stream.ReadExactlyInternal(buffer, offset, count, false).GetAwaiter().GetResult();
        }

        public static Task ReadExactlyAsync<TReadOnlyStream>(
            this TReadOnlyStream stream,
            byte[] buffer,
            int offset,
            int count) where TReadOnlyStream : IReadOnlyStream
        {
            var task = stream.ReadExactlyInternal(buffer, offset, count, true);
            return task.IsCompletedSuccessfully ? Task.CompletedTask : task.AsTask();
        }

        static async ValueTask<object> ReadExactlyInternal<TReadOnlyStream>(
            this TReadOnlyStream stream,
            byte[] buffer,
            int offset,
            int count,
            bool async) where TReadOnlyStream : IReadOnlyStream
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            int bytesRead;
            int totalBytesRead = 0;
            while (totalBytesRead < count && (bytesRead = async
                       ? await stream
                           .ReadAsync(buffer, offset + totalBytesRead, count - totalBytesRead)
                           .ConfigureAwait(false)
                       : stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead)) != 0)
            {
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead < count)
            {
                throw new EndOfStreamException();
            }

            return null;
        }

        public static async Task CopyTo<TReadOnlyStream>(
            this TReadOnlyStream source,
            IWriteOnlyStream destination,
            byte[] buffer,
            CancellationToken cancellationToken = default) where TReadOnlyStream : IReadOnlyStream
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            int bytesRead;
            while ((bytesRead = await source
                       .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                       .ConfigureAwait(false)) != 0)
            {
                await destination
                    .WriteAsync(buffer, 0, bytesRead, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
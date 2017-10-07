using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MangleSocks.Core.IO
{
    sealed class TcpStream : ITcpStream
    {
        readonly Stream _stream;

        public EndPoint LocalEndPoint { get; }
        public EndPoint RemoteEndPoint { get; }

        public TcpStream(Socket socket) : this(
            new NetworkStream(socket, true),
            socket.LocalEndPoint,
            socket.RemoteEndPoint) { }

        internal TcpStream(Stream stream, EndPoint localEndPoint, EndPoint remoteEndPoint)
        {
            this.LocalEndPoint = localEndPoint;
            this._stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return this.ReadInternal(buffer, offset, count, false, default).GetAwaiter().GetResult();
        }

        public Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default)
        {
            return this.ReadInternal(buffer, offset, count, true, cancellationToken).AsTask();
        }

        ValueTask<int> ReadInternal(
            byte[] buffer,
            int offset,
            int count,
            bool async,
            CancellationToken cancellationToken)
        {
            if (async)
            {
                return new ValueTask<int>(this._stream.ReadAsync(buffer, offset, count, cancellationToken));
            }

            var bytesRead = this._stream.Read(buffer, offset, count);
            return new ValueTask<int>(bytesRead);
        }

        public Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return this._stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public void Dispose()
        {
            this._stream.Dispose();
        }
    }
}
using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MangleSocks.Core.IO;

namespace MangleSocks.Core.Server
{
    class TcpProxy : IProxy
    {
        static readonly IPEndPoint s_EmptyEndPoint = new IPEndPoint(IPAddress.Any, 0);

        readonly ITcpStream _clientStream;
        readonly ITcpConnector _tcpConnector;
        readonly EndPoint _destinationEndPoint;
        readonly ArrayPool<byte> _bufferPool;

        public EndPoint BindEndPoint => s_EmptyEndPoint;

        public TcpProxy(
            ITcpStream clientStream,
            EndPoint destinationEndPoint,
            ITcpConnector tcpConnector,
            ArrayPool<byte> bufferPool)
        {
            this._clientStream = clientStream ?? throw new ArgumentNullException(nameof(clientStream));
            this._destinationEndPoint = destinationEndPoint
                                        ?? throw new ArgumentNullException(nameof(destinationEndPoint));
            this._tcpConnector = tcpConnector ?? throw new ArgumentNullException(nameof(tcpConnector));
            this._bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));

            if (!(destinationEndPoint is IPEndPoint) && !(destinationEndPoint is DnsEndPoint))
            {
                throw new ArgumentException(
                    "EndPoint must be either IPEndPoint or DnsEndPoint",
                    nameof(destinationEndPoint));
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using (var remoteStream = await this._tcpConnector
                .ConnectTcpAsync(this._destinationEndPoint)
                .ConfigureAwait(false))
            {
                await this.RunLoopAsync(cancellationToken, remoteStream).ConfigureAwait(false);
            }
        }

        Task RunLoopAsync(CancellationToken cancellationToken, ITcpStream remoteStream)
        {
            var sendLoop = Task.Run(
                () => this.CopyAsync(this._clientStream, remoteStream, cancellationToken),
                cancellationToken);

            var receiveLoop = Task.Run(
                () => this.CopyAsync(remoteStream, this._clientStream, cancellationToken),
                cancellationToken);

            return Task.WhenAll(sendLoop, receiveLoop);
        }

        async Task CopyAsync(
            IReadOnlyStream source,
            IWriteOnlyStream destination,
            CancellationToken cancellationToken = default)
        {
            byte[] buffer = this._bufferPool.Rent(4096);
            try
            {
                int bytesRead;
                while ((bytesRead = await source
                           .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                           .ConfigureAwait(false)) != 0)
                {
                    // Transform

                    await destination
                        .WriteAsync(buffer, 0, bytesRead, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                this._bufferPool.Return(buffer);
            }
        }

        public void Dispose()
        {
        }
    }
}

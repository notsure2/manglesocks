using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;
using MangleSocks.Core.Util.Threading;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Core.Server
{
    class UdpProxy : IProxy
    {
        readonly IReadOnlyTcpStream _clientStream;
        readonly IDatagramInterceptor _interceptor;
        readonly ArrayPool<byte> _bufferPool;
        readonly IBoundUdpClient _boundClient;
        readonly IUdpClient _relayClient;
        readonly DatagramReassembler _reassembler;
        readonly TaskCompletionSource<EndPoint> _clientEndPointTask;
        readonly CancellationTokenSource _clientSessionTerminationSource;
        readonly ILogger _log;

        bool _disposed;

        public EndPoint BindEndPoint => this._boundClient.BindEndPoint;

        public UdpProxy(
            IReadOnlyTcpStream clientStream,
            IUdpClientFactory clientFactory,
            IDatagramInterceptor interceptor,
            ArrayPool<byte> bufferPool,
            ITimerFactory timerFactory,
            ILoggerFactory loggerFactory)
        {
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            this._clientStream = clientStream ?? throw new ArgumentNullException(nameof(clientStream));
            this._interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
            this._bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));

            this._boundClient = clientFactory.CreateBoundUdpClient(
                new IPEndPoint(((IPEndPoint)clientStream.LocalEndPoint).Address, 0));
            this._relayClient = clientFactory.CreateUdpClient();
            this._reassembler = new DatagramReassembler(timerFactory, this._bufferPool);
            this._clientEndPointTask =
                new TaskCompletionSource<EndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);

            this._clientSessionTerminationSource = new CancellationTokenSource();
            this._log = loggerFactory.CreateLogger(this.GetType().Name);
            this._log.LogInformation("Started UDP listener on {0}", this.BindEndPoint);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            var clientStreamTerminationTask = this.WaitForClientStreamTerminationAsync(cancellationToken);

            using (var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                this._clientSessionTerminationSource.Token,
                cancellationToken))
            {
                var combinedCancellationToken = combinedCancellationTokenSource.Token;
                using (combinedCancellationToken.Register(this.DisposeProxyingObjects))
                {
                    var sendLoop = Task.Run(
                        () => this.DoSendLoopAsync(combinedCancellationToken),
                        combinedCancellationToken);

                    var receiveLoop = Task.Run(
                        () => this.DoReceiveLoopAsync(combinedCancellationToken),
                        combinedCancellationToken);

                    await Task.WhenAll(clientStreamTerminationTask, sendLoop, receiveLoop).ConfigureAwait(false);
                }
            }
        }

        async Task WaitForClientStreamTerminationAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(o => ((IDisposable)o).Dispose(), this._clientStream))
            {
                var buffer = this._bufferPool.Rent(1);
                try
                {
                    var bytesRead = await this._clientStream
                        .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false);

                    if (bytesRead > 0)
                    {
                        this._log.LogWarning("Protocol violation: Unexpected data sent in TCP wait stream.");
                    }

                    this._log.LogDebug("Control TCP stream terminated.");
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
                finally
                {
                    try
                    {
                        this._clientSessionTerminationSource.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // ignore
                    }
                    this._bufferPool.Return(buffer);
                }
            }
        }

        async Task DoSendLoopAsync(CancellationToken cancellationToken)
        {
            using (this._log.BeginScope("SendLoop"))
            {
                var buffer = this._bufferPool.Rent(DatagramHeader.MaxUdpSize);
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var result = await this._boundClient
                            .ReceiveAsync(
                                buffer,
                                0,
                                this._boundClient.BindEndPoint)
                            .ConfigureAwait(false);

                        if (!Equals(((IPEndPoint)result.RemoteEndPoint).Address, ((IPEndPoint)this._clientStream.RemoteEndPoint).Address))
                        {
                            this._log.LogWarning(
                                "Dropped {0} bytes from unknown source: {1}",
                                result.ReceivedBytes,
                                result.RemoteEndPoint);
                        }

                        this._log.LogDebug("Received {0} bytes from {1}", result.ReceivedBytes, result.RemoteEndPoint);

                        var datagram = Datagram.ReadFrom(
                            new ArraySegment<byte>(buffer, 0, result.ReceivedBytes),
                            this._bufferPool);

                        var fragments = this._reassembler.GetCompletedSetOrAdd(
                            datagram.Payload,
                            datagram.Header.FragmentPosition,
                            datagram.Header.IsFinalFragment);

                        if (fragments.Count == 0)
                        {
                            this._log.LogDebug("Buffering datagram segment #{0}", datagram.Header.FragmentPosition);
                            continue;
                        }

                        using (fragments)
                        {
                            int totalCount = 0;
                            for (int i = 0; i < fragments.Count; i++)
                            {
                                var fragment = fragments[i];
                                Buffer.BlockCopy(
                                    fragment.Array,
                                    fragment.Offset,
                                    buffer,
                                    totalCount,
                                    fragment.Count);
                                totalCount += fragment.Count;
                            }

                            if (!await this._interceptor
                                .TryInterceptOutgoingAsync(
                                    new ArraySegment<byte>(buffer, 0, totalCount),
                                    datagram.Header.RemoteEndPoint,
                                    this._relayClient)
                                .ConfigureAwait(false))
                            {
                                this._log.LogDebug(
                                    "Sending {0} bytes to {1}",
                                    totalCount,
                                    datagram.Header.RemoteEndPoint);
                                await this._relayClient
                                    .SendAsync(
                                        buffer,
                                        0,
                                        totalCount,
                                        datagram.Header.RemoteEndPoint)
                                    .ConfigureAwait(false);
                            }

                            this._clientEndPointTask.TrySetResult(result.RemoteEndPoint);
                        }
                    }
                }
                finally
                {
                    this._bufferPool.Return(buffer);
                }
            }
        }

        async Task DoReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var clientEndPoint = await this._clientEndPointTask.Task.ConfigureAwait(false);

            using (this._log.BeginScope("ReceiveLoop"))
            {
                var buffer = this._bufferPool.Rent(
                    DatagramHeader.InternetProtocolV6HeaderLength + DatagramHeader.MaxUdpSize);
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var result = await this._relayClient
                            .ReceiveAsync(
                                buffer,
                                DatagramHeader.InternetProtocolV6HeaderLength,
                                new IPEndPoint(IPAddress.Any, 0))
                            .ConfigureAwait(false);

                        if (result.RemoteEndPoint is DnsEndPoint)
                        {
                            throw new NotSupportedException("DNS endpoints are not supported in the receive loop.");
                        }

                        this._log.LogDebug("Received {0} bytes from {1}", result.ReceivedBytes, result.RemoteEndPoint);

                        var datagramHeader = new DatagramHeader(result.RemoteEndPoint);
                        var datagramHeaderByteCount = datagramHeader.ByteCount;
                        var bufferSendOffset = DatagramHeader.InternetProtocolV6HeaderLength - datagramHeaderByteCount;
                        datagramHeader.WriteTo(
                            new ArraySegment<byte>(buffer, bufferSendOffset, datagramHeaderByteCount));

                        var datagramTotalByteCount = datagramHeaderByteCount + result.ReceivedBytes;
                        if (datagramTotalByteCount > DatagramHeader.MaxUdpSize)
                        {
                            this._log.LogWarning(
                                $"Dropping oversize packet from {result.RemoteEndPoint} ({result.ReceivedBytes} bytes)");
                            continue;
                        }

                        if (await this._interceptor
                            .TryInterceptIncomingAsync(
                                new Datagram(
                                    datagramHeader,
                                    new ArraySegment<byte>(
                                        buffer,
                                        DatagramHeader.InternetProtocolV6HeaderLength,
                                        result.ReceivedBytes)),
                                this._boundClient)
                            .ConfigureAwait(false))
                        {
                            continue;
                        }

                        this._log.LogDebug("Sending {0} bytes to {1}", datagramTotalByteCount, clientEndPoint);
                        await this._boundClient
                            .SendAsync(
                                buffer,
                                bufferSendOffset,
                                datagramTotalByteCount,
                                clientEndPoint)
                            .ConfigureAwait(false);
                    }
                }
                finally
                {
                    this._bufferPool.Return(buffer);
                }
            }
        }

        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._clientSessionTerminationSource.Cancel();
            this._clientSessionTerminationSource.Dispose();
            this._clientStream.Dispose(); // Aborts wait
            this._interceptor.Dispose();
            this._reassembler.Dispose();
            this.DisposeProxyingObjects();

            this._disposed = true;
        }

        void DisposeProxyingObjects()
        {
            this._boundClient.Dispose();
            this._relayClient.Dispose();
            this._clientEndPointTask.TrySetCanceled();
        }
    }
}
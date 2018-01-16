using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MangleSocks.Core.Socks;
using MangleSocks.Core.Util;

namespace MangleSocks.Core.IO
{
    class UdpClient : IBoundUdpClient
    {
        static readonly IPEndPoint s_AnyIPv4EndPoint = new IPEndPoint(IPAddress.Any, 0);
        static readonly IPEndPoint s_AnyIPv6EndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);

        static readonly Task<SocketReceiveFromResult> s_NeverendingReceive =
            new TaskCompletionSource<SocketReceiveFromResult>().Task;

        readonly ArrayPool<byte> _bufferPool;
        readonly Lazy<Socket> _lazyIpv4Socket;
        readonly Lazy<byte[]> _lazyIpv4ReceiveBuffer;
        readonly Lazy<Socket> _lazyIpv6Socket;
        readonly Lazy<byte[]> _lazyIpv6ReceiveBuffer;
        
        Task<SocketReceiveFromResult> _ipv4ReceiveTask;
        Task<SocketReceiveFromResult> _ipv6ReceiveTask;
        bool _disposed;

        // One of them will always be null in case of explicit bind.
        public EndPoint BindEndPoint => this.Ipv4LocalEndPoint ?? this.Ipv6LocalEndPoint;

        public EndPoint Ipv4LocalEndPoint => this._lazyIpv4Socket.IsValueCreated
            ? this._lazyIpv4Socket.Value.LocalEndPoint
            : null;

        public EndPoint Ipv6LocalEndPoint => this._lazyIpv6Socket.IsValueCreated
            ? this._lazyIpv6Socket.Value.LocalEndPoint
            : null;

        public UdpClient(EndPoint bindEndPoint, ArrayPool<byte> bufferPool) : this(bufferPool)
        {
            Socket server;
            switch (bindEndPoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    server = this._lazyIpv4Socket.Value;
                    break;

                case AddressFamily.InterNetworkV6:
                    server = this._lazyIpv6Socket.Value;
                    break;

                default:
                    throw new NotSupportedException("Only Ipv4 and Ipv6 endpoints are supported.");
            }

            server.Bind(bindEndPoint);
        }

        public UdpClient(ArrayPool<byte> bufferPool)
        {
            this._bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));

            this._lazyIpv4Socket = new Lazy<Socket>(
                () => new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp),
                LazyThreadSafetyMode.None);
            this._lazyIpv4ReceiveBuffer = new Lazy<byte[]>(
                () => this._bufferPool.Rent(DatagramHeader.MaxUdpSize),
                LazyThreadSafetyMode.None);

            this._lazyIpv6Socket = new Lazy<Socket>(
                () => new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp),
                LazyThreadSafetyMode.None);
            this._lazyIpv6ReceiveBuffer = new Lazy<byte[]>(
                () => this._bufferPool.Rent(DatagramHeader.MaxUdpSize),
                LazyThreadSafetyMode.None);
        }

        public Task<int> SendAsync(byte[] buffer, int offset, int count, EndPoint destinationEndPoint)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            var segment = new ArraySegment<byte>(buffer, offset, count);

            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            return this
                .GetClientSocket(destinationEndPoint)
                .SendToAsync(
                    segment,
                    SocketFlags.None,
                    destinationEndPoint.ToEndPointWithUnmappedAddress());
        }

        public async Task<SocketReceiveFromResult> ReceiveAsync(
            byte[] buffer,
            int offset,
            EndPoint sourceEndPoint)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (sourceEndPoint == null) throw new ArgumentNullException(nameof(sourceEndPoint));

            if (buffer.Length < offset + DatagramHeader.MaxUdpSize)
            {
                throw new ArgumentException("Not enough space in buffer to receive UDP packet.", nameof(buffer));
            }

            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            SocketReceiveFromResult result;
            if (sourceEndPoint is IPEndPoint sourceIpEndPoint && Equals(sourceIpEndPoint.Address, IPAddress.Any))
            {
                result = await this
                    .DualModeReceiveFromAnyAsync(buffer, offset)
                    .ConfigureAwait(false);
            }
            else
            {
                result = await this
                    .GetClientSocket(sourceEndPoint)
                    .ReceiveFromAsync(
                        new ArraySegment<byte>(buffer, offset, DatagramHeader.MaxUdpSize),
                        SocketFlags.None,
                        sourceEndPoint)
                    .ConfigureAwait(false);
            }

            result.RemoteEndPoint = result.RemoteEndPoint.ToEndPointWithUnmappedAddress();
            return result;
        }

        async Task<SocketReceiveFromResult> DualModeReceiveFromAnyAsync(
            byte[] buffer,
            int offset)
        {
            if (this._ipv4ReceiveTask == null
                && this._lazyIpv4Socket.IsValueCreated
                && this._lazyIpv4Socket.Value.LocalEndPoint != null)
            {
                this._ipv4ReceiveTask = this._ipv4ReceiveTask
                                        ?? this._lazyIpv4Socket.Value.ReceiveFromAsync(
                                            new ArraySegment<byte>(
                                                this._lazyIpv4ReceiveBuffer.Value,
                                                0,
                                                DatagramHeader.MaxUdpSize),
                                            SocketFlags.None,
                                            s_AnyIPv4EndPoint);
            }

            if (this._ipv6ReceiveTask == null
                && this._lazyIpv6Socket.IsValueCreated
                && this._lazyIpv6Socket.Value.LocalEndPoint != null)
            {
                this._ipv6ReceiveTask = this._ipv6ReceiveTask
                                        ?? this._lazyIpv6Socket.Value.ReceiveFromAsync(
                                            new ArraySegment<byte>(
                                                this._lazyIpv6ReceiveBuffer.Value,
                                                0,
                                                DatagramHeader.MaxUdpSize),
                                            SocketFlags.None,
                                            s_AnyIPv6EndPoint);
            }

            if (this._ipv4ReceiveTask == null && this._ipv6ReceiveTask == null)
            {
                throw new InvalidOperationException("A bind or send operation is required before receiving.");
            }

            var receiveTask = await Task
                .WhenAny(this._ipv4ReceiveTask ?? s_NeverendingReceive, this._ipv6ReceiveTask ?? s_NeverendingReceive)
                .ConfigureAwait(false);

            var result = receiveTask.GetAwaiter().GetResult();
            byte[] receiveBuffer = null;
            if (receiveTask == this._ipv4ReceiveTask)
            {
                this._ipv4ReceiveTask = null;
                receiveBuffer = this._lazyIpv4ReceiveBuffer.Value;
            }
            else if (receiveTask == this._ipv6ReceiveTask)
            {
                this._ipv6ReceiveTask = null;
                receiveBuffer = this._lazyIpv6ReceiveBuffer.Value;
            }

            Buffer.BlockCopy(receiveBuffer, 0, buffer, offset, result.ReceivedBytes);
            return result;
        }

        Socket GetClientSocket(EndPoint endPoint)
        {
            Socket clientSocket;
            switch (endPoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    clientSocket = this._lazyIpv4Socket.Value;
                    break;

                case AddressFamily.InterNetworkV6:
                    clientSocket = this._lazyIpv6Socket.Value;
                    break;

                default:
                    throw new NotSupportedException("Only Ipv4 and Ipv6 endpoints are supported.");
            }

            if (clientSocket == null)
            {
                throw new ArgumentException(
                    $"Value has an {nameof(endPoint.AddressFamily)} different from that of the associated socket.",
                    nameof(endPoint));
            }

            var socket = clientSocket;
            return socket;
        }

        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            if (this._lazyIpv4Socket.IsValueCreated)
            {
                this._lazyIpv4Socket.Value.Dispose();
            }

            if (this._lazyIpv4ReceiveBuffer.IsValueCreated)
            {
                this._bufferPool.Return(this._lazyIpv4ReceiveBuffer.Value);
            }

            if (this._lazyIpv6Socket.IsValueCreated)
            {
                this._lazyIpv6Socket.Value.Dispose();
            }

            if (this._lazyIpv6ReceiveBuffer.IsValueCreated)
            {
                this._bufferPool.Return(this._lazyIpv6ReceiveBuffer.Value);
            }

            this._disposed = true;
        }
    }
}
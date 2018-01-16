using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MangleSocks.Core.Socks;
using MangleSocks.Core.Util;

namespace MangleSocks.Core.IO
{
    class UdpClient : IBoundUdpClient
    {
        readonly Socket _socket;

        public EndPoint BindEndPoint { get; private set; }

        public UdpClient()
        {
            this._socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        }

        public void Bind(EndPoint localEndPoint)
        {
            this._socket.Bind(localEndPoint);
            this.BindEndPoint = this._socket.LocalEndPoint.ToEndPointWithUnmappedAddress();
        }

        public Task<int> SendAsync(byte[] buffer, int offset, int count, EndPoint destinationEndPoint)
        {
            return this._socket.SendToAsync(
                new ArraySegment<byte>(buffer, offset, count),
                    SocketFlags.None,
                    destinationEndPoint.ToEndPointWithUnmappedAddress());
        }

        public async Task<SocketReceiveFromResult> ReceiveAsync(
            byte[] buffer,
            int offset,
            EndPoint sourceEndPoint)
        {
            var result = await this._socket.ReceiveFromAsync(
                new ArraySegment<byte>(buffer, offset, DatagramHeader.MaxUdpSize),
                SocketFlags.None,
                sourceEndPoint).ConfigureAwait(false);
            result.RemoteEndPoint = result.RemoteEndPoint.ToEndPointWithUnmappedAddress();
            return result;
        }

        public void Dispose()
        {
            this._socket.Dispose();
        }
    }
}
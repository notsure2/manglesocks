﻿using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using UdpClient = MangleSocks.Core.IO.UdpClient;

namespace MangleSocks.Core.Server
{
    public class DefaultConnector : IConnector
    {
        readonly ArrayPool<byte> _bufferPool;

        public DefaultConnector(ArrayPool<byte> bufferPool)
        {
            this._bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
        }

        public async Task<ITcpStream> ConnectTcpAsync(EndPoint destinationEndPoint)
        {
            if (destinationEndPoint == null) throw new ArgumentNullException(nameof(destinationEndPoint));

            var client = new TcpClient();
            switch (destinationEndPoint)
            {
                case IPEndPoint ipEndPoint:
                    await client.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port).ConfigureAwait(false);
                    break;

                case DnsEndPoint dnsEndPoint:
                    await client.ConnectAsync(dnsEndPoint.Host, dnsEndPoint.Port).ConfigureAwait(false);
                    break;

                default:
                    throw new ArgumentException($"Invalid EndPoint type: {destinationEndPoint.GetType().FullName}");
            }

            return new TcpStream(client.Client);
        }

        public IBoundUdpClient CreateBoundUdpClient(EndPoint bindEndPoint)
        {
            return new UdpClient(bindEndPoint, this._bufferPool);
        }

        public IUdpClient CreateUdpClient()
        {
            return new UdpClient(this._bufferPool);
        }
    }
}
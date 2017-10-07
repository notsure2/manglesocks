using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using UdpClient = MangleSocks.Core.IO.UdpClient;

namespace MangleSocks.Core.Server
{
    class DefaultConnector : IConnector
    {
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
            var client = new UdpClient();
            client.Bind(bindEndPoint);
            return client;
        }

        public IUdpClient CreateUdpClient()
        {
            return new UdpClient();
        }
    }
}
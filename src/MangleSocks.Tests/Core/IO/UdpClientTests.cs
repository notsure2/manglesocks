using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.Helpers;
using MangleSocks.Tests.TestDoubles;
using Xunit;
using UdpClient = MangleSocks.Core.IO.UdpClient;

namespace MangleSocks.Tests.Core.IO
{
    public class UdpClientTests : IDisposable
    {
        readonly DebugArrayPool<byte> _bufferPool;

        public UdpClientTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public void Trying_to_receive_without_send_or_bind_should_throw()
        {
            using (var udpClient = new UdpClient(this._bufferPool))
            {
                udpClient
                    .Awaiting(x => x.ReceiveAsync(new byte[DatagramHeader.MaxUdpSize], 0, FakeEndPoints.CreateRemote()))
                    .ShouldThrow<InvalidOperationException>();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task Trying_to_receive_different_IP_type_before_corresponding_bind(bool ipv4OrIpv6, bool bindOrSend)
        {
            UdpClient udpClient;

            IPEndPoint bindEndPoint;
            IPEndPoint mismatchingReceiveEndPoint;

            if (ipv4OrIpv6)
            {
                bindEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
                mismatchingReceiveEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 12345);
            }
            else
            {
                bindEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 12345);
                mismatchingReceiveEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
            }

            var remoteEndPoint = new IPEndPoint(bindEndPoint.Address, bindEndPoint.Port + 1);

            if (bindOrSend)
            {
                udpClient = new UdpClient(bindEndPoint, this._bufferPool);
            }
            else
            {
                udpClient = new UdpClient(this._bufferPool);
                await udpClient
                    .SendAsync(new byte[1], 0, 1, remoteEndPoint)
                    .ConfigureAwait(true);
            }

            var buffer = new byte[DatagramHeader.MaxUdpSize];

            using (udpClient)
            {
                udpClient
                    .Awaiting(x => x.ReceiveAsync(buffer, 0, mismatchingReceiveEndPoint))
                    .ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task Receiving_from_ipv6_and_ipv4_should_work_when_receiving_from_any_source()
        {
            using (var udpClient = new UdpClient(this._bufferPool))
            {
                var ipv4ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                ipv4ServerSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                var bufferIpv4 = new byte[] { 1, 2, 3, 4 };
                await udpClient.SendAsync(bufferIpv4, 0, bufferIpv4.Length, ipv4ServerSocket.LocalEndPoint);
                udpClient.Ipv4LocalEndPoint.Should().NotBeNull();
                var sendToIpv4 = new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)udpClient.Ipv4LocalEndPoint).Port);
                ipv4ServerSocket.SendTo(new byte[] { 10, 9, 8, 7 }, sendToIpv4);
                var receiveBufferIpv4 = new byte[DatagramHeader.MaxUdpSize];
                var receiveTaskIpv4 = udpClient.ReceiveAsync(receiveBufferIpv4, 0, new IPEndPoint(IPAddress.Any, 0));
                receiveTaskIpv4.ExecutionTimeOf(x => x.Wait()).ShouldNotExceed(TimeSpan.FromSeconds(5));
                receiveBufferIpv4.Take(4).Should().Equal(10, 9, 8, 7);

                var ipv6ServerSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                ipv6ServerSocket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                var buffer2 = new byte[] { 10, 20, 30, 40 };
                await udpClient.SendAsync(buffer2, 0, buffer2.Length, ipv6ServerSocket.LocalEndPoint);
                udpClient.Ipv6LocalEndPoint.Should().NotBeNull();
                var sendToIpv6 = new IPEndPoint(IPAddress.IPv6Loopback, ((IPEndPoint)udpClient.Ipv6LocalEndPoint).Port);
                ipv6ServerSocket.SendTo(new byte[] { 100, 90, 80, 70 }, sendToIpv6);
                var receiveBufferIpv6 = new byte[DatagramHeader.MaxUdpSize];
                var receiveTaskIpv6 = udpClient.ReceiveAsync(receiveBufferIpv6, 0, new IPEndPoint(IPAddress.Any, 0));
                receiveTaskIpv6.ExecutionTimeOf(x => x.Wait()).ShouldNotExceed(TimeSpan.FromSeconds(5));
                receiveBufferIpv6.Take(4).Should().Equal(100, 90, 80, 70);
            }
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0);
        }
    }
}
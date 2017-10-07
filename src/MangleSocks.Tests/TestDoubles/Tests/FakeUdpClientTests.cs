using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MangleSocks.Tests.TestDoubles.Tests
{
    public class FakeUdpClientTests
    {
        [Fact]
        public async Task Staging_packet_makes_it_available_in_receive()
        {
            using (var client = new FakeUdpClient())
            {
                var destination = FakeEndPoints.CreateLocal();
                client.StageReceivedPacket(destination, 1, 2, 3);
                var buffer = new byte[10];
                var result = await client.ReceiveAsync(buffer, 0, 3, destination).ConfigureAwait(true);
                result.ReceivedBytes.Should().Be(3);
                result.RemoteEndPoint.Should().Be(destination);
                buffer.Take(3).Should().Equal(1, 2, 3);
            }
        }

        [Fact]
        public async Task Written_packet_can_be_retrieved()
        {
            using (var client = new FakeUdpClient())
            {
                var destination = FakeEndPoints.CreateRemote();
                var packet = new byte[] { 1, 2, 3, 4, 5 };
                await client.SendAsync(packet, 0, packet.Length, destination).ConfigureAwait(true);
                var received = client.WaitForSentPackets(1).First();
                received.Destination.Should().Be(destination);
                received.Packet.Should().Equal(1, 2, 3, 4, 5);
            }
        }

        [Fact]
        public void Dispose_aborts_pending_receive()
        {
            var client = new FakeUdpClient();
            var readTask = client.ReceiveAsync(new byte[1], 0, 1, FakeEndPoints.CreateLocal());
            client.Dispose();
            readTask.Awaiting(t => t).ShouldThrow<Exception>();
        }
    }
}
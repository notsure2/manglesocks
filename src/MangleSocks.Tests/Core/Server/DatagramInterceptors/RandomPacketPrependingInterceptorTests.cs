using System;
using System.Threading.Tasks;
using FluentAssertions;
using MangleSocks.Core.Server.DatagramInterceptors;
using MangleSocks.Tests.Helpers;
using MangleSocks.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MangleSocks.Tests.Core.Server.DatagramInterceptors
{
    public class RandomPacketPrependingInterceptorTests : IDisposable
    {
        readonly DebugArrayPool<byte> _bufferPool;

        public RandomPacketPrependingInterceptorTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public async Task Random_packets_sent_only_before_first_packet_to_each_destination()
        {
            var settings = new RandomSessionPrefixInterceptor.Settings { CountMin = 2, CountMax = 2 };
            var interceptor = new RandomSessionPrefixInterceptor(this._bufferPool, new NullLoggerFactory());
            interceptor.ConfigureWith(settings);
            var client = new FakeUdpClient();

            var payload = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 });
            await interceptor.TryInterceptOutgoingAsync(payload, FakeEndPoints.CreateRemote(), client)
                .ConfigureAwait(true);

            payload = new ArraySegment<byte>(new byte[] { 5, 6, 7, 8 });
            await interceptor.TryInterceptOutgoingAsync(payload, FakeEndPoints.CreateRemote(), client)
                .ConfigureAwait(true);

            var packets = client.WaitForSentPackets(4);
            packets[0].Packet.Length.Should().BeInRange(settings.BytesMin, settings.BytesMax);
            packets[1].Packet.Length.Should().BeInRange(settings.BytesMin, settings.BytesMax);
            packets[2].Packet.Should().Equal(1, 2, 3, 4);
            packets[3].Packet.Should().Equal(5, 6, 7, 8);
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0);
        }
    }
}
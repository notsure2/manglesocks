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
    public class RandomFirstSessionPrefixInterceptorTests : IDisposable
    {
        readonly DebugArrayPool<byte> _bufferPool;

        public RandomFirstSessionPrefixInterceptorTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public async Task Random_packets_sent_only_before_first_packet_to_first_destination()
        {
            var settings = new RandomFirstSessionPrefixInterceptor.Settings { CountMin = 2, CountMax = 2 };
            var interceptor = new RandomFirstSessionPrefixInterceptor(this._bufferPool, new NullLoggerFactory());
            interceptor.ConfigureWith(settings);
            var client = new FakeUdpClient();

            var destinationEndPoint = FakeEndPoints.CreateRemote();

            var payload = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 });
            var isSendingHandledByInterceptor = await interceptor
                .TryInterceptOutgoingAsync(payload, destinationEndPoint, client)
                .ConfigureAwait(true);
            isSendingHandledByInterceptor.Should().BeFalse();

            payload = new ArraySegment<byte>(new byte[] { 5, 6, 7, 8 });
            isSendingHandledByInterceptor = await interceptor
                .TryInterceptOutgoingAsync(payload, destinationEndPoint, client)
                .ConfigureAwait(true);
            isSendingHandledByInterceptor.Should().BeFalse();

            var packets = client.WaitForSentPackets(2);
            packets[0].Packet.Length.Should().BeInRange(settings.BytesMin, settings.BytesMax);
            packets[1].Packet.Length.Should().BeInRange(settings.BytesMin, settings.BytesMax);
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0);
        }
    }
}
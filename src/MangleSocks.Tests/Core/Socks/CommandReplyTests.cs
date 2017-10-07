using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.Helpers;
using MangleSocks.Tests.TestDoubles;
using Xunit;

namespace MangleSocks.Tests.Core.Socks
{
    public class CommandReplyTests : IDisposable
    {
        readonly DebugArrayPool<byte> _bufferPool;

        public CommandReplyTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public void Unsupported_endpoint_type_should_throw()
        {
            var commandReply = new CommandReply(CommandReplyType.Succeeded, new DummyEndPoint());
            var stream = new MemoryWriteOnlyStream();

            commandReply
                .Awaiting(x => x.WriteToAsync(stream, this._bufferPool))
                .ShouldThrow<InvalidDataException>()
                .And.Message.Should().ContainEquivalentOf(nameof(EndPoint));
        }

        [Fact]
        public async Task Ipv4()
        {
            var commandReply = new CommandReply(
                CommandReplyType.Succeeded,
                new IPEndPoint(IPAddress.Parse("1.2.3.4"), 65534));

            var stream = new MemoryWriteOnlyStream();
            await commandReply.WriteToAsync(stream, this._bufferPool).ConfigureAwait(true);

            stream.ToArray().Should().Equal(
                5,
                (byte)CommandReplyType.Succeeded,
                0,
                (byte)AddressType.Ipv4,
                1,
                2,
                3,
                4,
                255,
                254);
        }

        [Fact]
        public async Task Ipv6()
        {
            var commandReply = new CommandReply(
                CommandReplyType.Succeeded,
                new IPEndPoint(IPAddress.Parse("2001:db8:85a3:8d3:1319:8a2e:370:7348"), 65534));

            var stream = new MemoryWriteOnlyStream();
            await commandReply.WriteToAsync(stream, this._bufferPool).ConfigureAwait(true);

            stream.ToArray().Should().Equal(
                5,
                (byte)CommandReplyType.Succeeded,
                0,
                (byte)AddressType.Ipv6,
                32,
                1,
                13,
                184,
                133,
                163,
                8,
                211,
                19,
                25,
                138,
                46,
                3,
                112,
                115,
                72,
                255,
                254);
        }

        [Fact]
        public async Task Domain()
        {
            var commandReply = new CommandReply(CommandReplyType.Succeeded, new DnsEndPoint("google.com", 65534));
            var stream = new MemoryWriteOnlyStream();
            await commandReply.WriteToAsync(stream, this._bufferPool).ConfigureAwait(true);

            stream.ToArray().Should().Equal(
                5,
                (byte)CommandReplyType.Succeeded,
                0,
                (byte)AddressType.DomainName,
                10,
                103,
                111,
                111,
                103,
                108,
                101,
                46,
                99,
                111,
                109,
                255,
                254);
        }

        class DummyEndPoint : EndPoint { }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0, "All outstanding buffers should be returned.");
        }
    }
}
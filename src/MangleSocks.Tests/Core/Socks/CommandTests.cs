using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.Helpers;
using Xunit;

namespace MangleSocks.Tests.Core.Socks
{
    public class CommandTests : IDisposable
    {
        static readonly IReadOnlyList<byte> s_ValidIpv4BindRequest = new byte[]
        {
            5,
            (byte)CommandType.Bind,
            0,
            (byte)AddressType.Ipv4,
            1, 2, 3, 4,
            255, 254
        };

        static readonly IReadOnlyList<byte> s_ValidIpv6BindRequest = new byte[]
        {
            5,
            (byte)CommandType.Bind,
            0,
            (byte)AddressType.Ipv6,
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
            255, 254
        };

        static readonly IReadOnlyList<byte> s_ValidDomainBindRequest = new byte[]
        {
            5,
            (byte)CommandType.Bind,
            0,
            (byte)AddressType.DomainName,
            10, 103, 111, 111, 103, 108, 101, 46, 99, 111, 109,
            255, 254
        };

        readonly DebugArrayPool<byte> _bufferPool;

        public CommandTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public void Wrong_protocol_version_should_throw()
        {
            var bytes = s_ValidIpv4BindRequest.ToArray();
            bytes[0] = 4;
            Func<Task> act = () => Command.ReadFromAsync(new BufferReadOnlyStream(bytes), this._bufferPool);
            act.Should()
                .Throw<InvalidDataException>()
                .And.Message.Should()
                .ContainEquivalentOf("version");
        }

        [Fact]
        public void Invalid_request_type_should_throw()
        {
            var bytes = s_ValidIpv4BindRequest.ToArray();
            bytes[1] = 255;
            Func<Task> act = () => Command.ReadFromAsync(new BufferReadOnlyStream(bytes), this._bufferPool);
            var ex = act.Should().Throw<ProtocolException>().Which;
            ex.ErrorCode.Should().Be(CommandReplyType.CommandNotSupported);
            ex.Message.Should().ContainEquivalentOf("command type");
        }

        [Fact]
        public void Non_zero_reserved_field_should_throw()
        {
            var bytes = s_ValidIpv4BindRequest.ToArray();
            bytes[2] = 255;
            Func<Task> act = () => Command.ReadFromAsync(new BufferReadOnlyStream(bytes), this._bufferPool);
            act.Should().Throw<InvalidDataException>()
                .And.Message.Should()
                .ContainEquivalentOf("invalid command");
        }

        [Fact]
        public void Invalid_address_type_should_throw()
        {
            var bytes = s_ValidIpv4BindRequest.ToArray();
            bytes[3] = 255;
            Func<Task> act = () => Command.ReadFromAsync(new BufferReadOnlyStream(bytes), this._bufferPool);
            var ex = act.Should().Throw<ProtocolException>().Which;
            ex.ErrorCode.Should().Be(CommandReplyType.AddressTypeNotSupported);
            ex.Message.Should().ContainEquivalentOf("address type");
        }

        [Fact]
        public async Task Ipv4_address()
        {
            var bytes = s_ValidIpv4BindRequest.ToArray();
            var stream = new BufferReadOnlyStream(bytes);
            var request = await Command.ReadFromAsync(stream, this._bufferPool).ConfigureAwait(true);
            request.CommandType.Should().Be(CommandType.Bind);
            request.EndPoint.Should().Be(new IPEndPoint(IPAddress.Parse("1.2.3.4"), 65534));
        }

        [Fact]
        public async Task Ipv6_address()
        {
            var bytes = s_ValidIpv6BindRequest.ToArray();
            var stream = new BufferReadOnlyStream(bytes);
            var request = await Command.ReadFromAsync(stream, this._bufferPool).ConfigureAwait(true);
            request.CommandType.Should().Be(CommandType.Bind);
            request.EndPoint.Should().Be(new IPEndPoint(IPAddress.Parse("102:304:506:708:90a:b0c:d0e:f10"), 65534));
        }

        [Fact]
        public async Task Domain_address()
        {
            var bytes = s_ValidDomainBindRequest.ToArray();
            var stream = new BufferReadOnlyStream(bytes);
            var request = await Command.ReadFromAsync(stream, this._bufferPool).ConfigureAwait(true);
            request.CommandType.Should().Be(CommandType.Bind);
            request.EndPoint.Should().Be(new DnsEndPoint("google.com", 65534));
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0, "All outstanding buffers should be returned.");
        }
    }
}
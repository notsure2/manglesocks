using System.Buffers;
using FluentAssertions;
using MangleSocks.Core.Server;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MangleSocks.Tests.Core.Server
{
    public class SocksConnectionTests
    {
        [Fact]
        public void Responds_to_unsupported_authentication_method_with_failure()
        {
            using (var stream = new LoopbackTcpStream(5, 1, 254))
            using (var connection = new SocksConnection(
                stream,
                new FakeProxyFactory(),
                ArrayPool<byte>.Shared,
                new NullLoggerFactory()))
            {
                connection.StartHandlingClient();
                stream.WaitForWrittenBytes(2).Should().Equal(5, 255);
            }
        }

        [Theory]
        [InlineData(CommandType.Bind)]
        [InlineData((CommandType)255)]
        public void Responds_to_invalid_command_with_failure(CommandType invalidCommandType)
        {
            using (var stream = new LoopbackTcpStream(5, 1, 0, 5, (byte)invalidCommandType, 0, 1, 1, 2, 3, 4, 0, 255))
            using (var connection = new SocksConnection(
                stream,
                new FakeProxyFactory(),
                ArrayPool<byte>.Shared,
                new NullLoggerFactory()))
            {
                connection.StartHandlingClient();
                stream.WaitForWrittenBytes(2).Should().Equal(5, 0);
                stream.WaitForWrittenBytes(10).Should().Equal(
                    5,
                    (byte)CommandReplyType.CommandNotSupported,
                    0,
                    1,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0);
            }
        }

        [Fact]
        public void Responds_to_invalid_address_type_with_failure()
        {
            using (var stream = new LoopbackTcpStream(
                5,
                1,
                0,
                5,
                (byte)CommandType.Connect,
                0,
                254,
                1,
                2,
                3,
                4,
                0,
                255))
            using (var connection = new SocksConnection(
                stream,
                new FakeProxyFactory(),
                ArrayPool<byte>.Shared,
                new NullLoggerFactory()))
            {
                connection.StartHandlingClient();
                stream.WaitForWrittenBytes(2).Should().Equal(5, 0);
                stream.WaitForWrittenBytes(10).Should().Equal(
                    5,
                    (byte)CommandReplyType.AddressTypeNotSupported,
                    0,
                    1,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0);
            }
        }

        [Fact]
        public void Assumes_ownership_of_stream_and_disposes_it()
        {
            var stream = new LoopbackTcpStream(5, 1, 254);
            using (var connection = new SocksConnection(
                stream,
                new FakeProxyFactory(),
                ArrayPool<byte>.Shared,
                new NullLoggerFactory()))
            {
                connection.StartHandlingClient();
                stream.WaitForWrittenBytes(2).Should().Equal(5, 255);
            }

            stream.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Dispose_disposes_created_proxies()
        {
            var stream = new LoopbackTcpStream(
                5,
                1,
                0,
                5,
                (byte)CommandType.Connect,
                0,
                (byte)AddressType.Ipv4,
                1,
                2,
                3,
                4,
                0,
                255);

            var proxyFactory = new FakeProxyFactory();
            var proxy = new FakeProxy();
            proxyFactory.NextTcpProxyToReturn = proxy;

            using (var connection = new SocksConnection(
                stream,
                proxyFactory,
                ArrayPool<byte>.Shared,
                new NullLoggerFactory()))
            {
                connection.StartHandlingClient();
                proxy.WaitForRunAsyncCall();
            }

            proxy.IsDisposed.Should().BeTrue();
        }
    }
}
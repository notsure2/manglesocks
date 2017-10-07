using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MangleSocks.Core.Server;
using MangleSocks.Tests.Helpers;
using MangleSocks.Tests.TestDoubles;
using Xunit;

namespace MangleSocks.Tests.Core.Server
{
    public class TcpProxyTests : IDisposable
    {
        readonly DebugArrayPool<byte> _bufferPool;

        public TcpProxyTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public async Task Proxy_streams_in_both_directions()
        {
            using (var clientStream = new LoopbackTcpStream(1, 2, 3, 4, 5))
            using (var remoteStream = new LoopbackTcpStream(10, 9, 8, 7, 6))
            {
                var connector = remoteStream.GetConnector();
                clientStream.Write(200, 199, 198, 197, 196);
                remoteStream.Write(100, 99, 98, 97, 96);
                clientStream.CloseStagedBytesSender();
                remoteStream.CloseStagedBytesSender();

                using (var proxy = new TcpProxy(
                    clientStream,
                    FakeEndPoints.CreateRemote(),
                    connector,
                    this._bufferPool))
                {
                    await proxy.RunAsync(CancellationToken.None).ConfigureAwait(true);
                }

                clientStream.GetAllWrittenBytes().Should().Equal(200, 199, 198, 197, 196, 10, 9, 8, 7, 6);
                remoteStream.GetAllWrittenBytes().Should().Equal(100, 99, 98, 97, 96, 1, 2, 3, 4, 5);
            }
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0, "All outstanding buffers should be returned.");
        }
    }
}
using System;
using System.Threading.Tasks;
using FluentAssertions;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.Helpers;
using MangleSocks.Tests.TestDoubles;
using Xunit;

namespace MangleSocks.Tests.Core.Socks
{
    public class GreetingReplyTests : IDisposable
    {
        readonly DebugArrayPool<byte> _bufferPool;

        public GreetingReplyTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public async Task Reply_should_be_written()
        {
            var greetingReply = new GreetingReply { SelectedAuthenticationMethod = AuthenticationMethod.UsernamePassword };
            var stream = new MemoryWriteOnlyStream();
            await greetingReply.WriteToAsync(stream, this._bufferPool).ConfigureAwait(true);
            stream.ToArray().Should().Equal(5, (byte)greetingReply.SelectedAuthenticationMethod);
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0, "All outstanding buffers should be returned.");
        }
    }
}
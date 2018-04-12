using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.Helpers;
using Xunit;

namespace MangleSocks.Tests.Core.Socks
{
    public class GreetingTests : IDisposable
    {
        readonly DebugArrayPool<byte> _bufferPool;

        public GreetingTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public void Wrong_protocol_version_should_throw()
        {
            var stream = new BufferReadOnlyStream(4, 1, 0);
            Func<Task> act = () => Greeting.ReadFromAsync(stream, this._bufferPool);
            act.Should().Throw<InvalidDataException>().And.Message.Should().ContainEquivalentOf("version");
        }

        [Fact]
        public void No_supported_authentication_methods_should_throw()
        {
            var stream = new BufferReadOnlyStream(5, 0, 0);
            Func<Task> act = () => Greeting.ReadFromAsync(stream, this._bufferPool);
            act.Should().Throw<InvalidDataException>().And.Message.Should().ContainEquivalentOf("authentication methods");
        }

        [Fact]
        public async Task No_authentication_should_work()
        {
            var stream = new BufferReadOnlyStream(5, 1, 0);
            var greeting = await Greeting.ReadFromAsync(stream, this._bufferPool).ConfigureAwait(true);
            greeting.SupportedAuthenticationMethods.Should().Equal(AuthenticationMethod.None);
        }

        [Fact]
        public async Task All_authentication_methods_supported()
        {
            var stream = new BufferReadOnlyStream(
                new[] { 5, 255 }
                    .Concat(Enumerable.Range(0, 255))
                    .Select(x => (byte)x)
                    .ToArray());

            var greeting = await Greeting
                .ReadFromAsync(stream, this._bufferPool)
                .ConfigureAwait(true);

            greeting.SupportedAuthenticationMethods
                .Should()
                .Equal(Enumerable.Range(0, 255).Select(x => (AuthenticationMethod)x));
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0, "All outstanding buffers should be returned.");
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MangleSocks.Tests.TestDoubles.Tests
{
    public class LoopbackTcpStreamTests
    {
        [Fact]
        public void Staging_bytes_makes_them_available_in_sync_read()
        {
            using (var stream = new LoopbackTcpStream())
            {
                stream.StageReadBytes(1, 2, 3);
                var buffer = new byte[10];
                stream.Read(buffer, 0, 3).Should().Be(3);
                buffer.Take(3).Should().Equal(1, 2, 3);
            }
        }

        [Fact]
        public async Task Staging_bytes_makes_them_available_in_async_read()
        {
            using (var stream = new LoopbackTcpStream())
            {
                stream.StageReadBytes(1, 2, 3);
                var buffer = new byte[10];
                var readBytes = await stream.ReadAsync(buffer, 0, 3).ConfigureAwait(true);
                readBytes.Should().Be(3);
                buffer.Take(3).Should().Equal(1, 2, 3);
            }
        }

        [Fact]
        public async Task Written_bytes_can_be_retrieved()
        {
            using (var stream = new LoopbackTcpStream())
            {
                var bytes = new byte[] { 1, 2, 3, 4, 5 };
                await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(true);
                stream.GetAllWrittenBytes().Should().Equal(1, 2, 3, 4, 5);
            }
        }

        [Fact]
        public void Dispose_aborts_pending_read()
        {
            var stream = new LoopbackTcpStream();
            var readTask = stream.ReadAsync(new byte[1], 0, 1);
            stream.Dispose();
            readTask.Awaiting(t => t).ShouldThrow<Exception>();
        }
    }
}
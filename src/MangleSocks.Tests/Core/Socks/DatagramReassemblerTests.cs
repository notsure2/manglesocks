using System;
using FluentAssertions;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.Helpers;
using MangleSocks.Tests.TestDoubles;
using Xunit;

namespace MangleSocks.Tests.Core.Socks
{
    public class DatagramReassemblerTests : IDisposable
    {
        readonly ManuallyInvokedTimerFactory _timerFactory;
        readonly DebugArrayPool<byte> _bufferPool;
        readonly DatagramReassembler _reassembler;

        public DatagramReassemblerTests()
        {
            this._timerFactory = new ManuallyInvokedTimerFactory();
            this._bufferPool = new DebugArrayPool<byte>();
            this._reassembler = new DatagramReassembler(this._timerFactory, this._bufferPool);
        }

        [Fact]
        public void Reassembling_with_null_segment_throws()
        {
            this._reassembler
                .Invoking(r => r.GetCompletedSetOrAdd(default(ArraySegment<byte>), 0, false))
                .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Reassembling_with_negative_position_throws()
        {
            this._reassembler
                .Invoking(r => r.GetCompletedSetOrAdd(new byte[1], -1, true))
                .ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Reassembling_solitary_fragments_should_return_them()
        {
            using (var fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 1, 2, 3, 4, 5 }, 0, false))
            {
                fragments.Count.Should().Be(1);
                fragments[0].Should().Equal(1, 2, 3, 4, 5);
            }
        }

        [Fact]
        public void Reassembling_without_disposing_each_set_before_the_next_should_throw()
        {
            var fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 1, 2, 3 }, 1, false);
            fragments.Count.Should().Be(0);

            try
            {
                fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 4, 5, 6 }, 2, true);
                fragments.Count.Should().Be(2);
                fragments[0].Should().Equal(1, 2, 3);
                fragments[1].Should().Equal(4, 5, 6);

                this._reassembler
                    .Invoking(r => r.GetCompletedSetOrAdd(new byte[] { 10, 11, 12 }, 0, false))
                    .ShouldThrow<InvalidOperationException>()
                    .And.Message.Should().ContainEquivalentOf("last set").And.ContainEquivalentOf("dispose");

                this._reassembler
                    .Invoking(r => r.GetCompletedSetOrAdd(new byte[] { 7, 8, 9 }, 1, false))
                    .ShouldThrow<InvalidOperationException>()
                    .And.Message.Should().ContainEquivalentOf("last set").And.ContainEquivalentOf("dispose");
            }
            finally
            {
                fragments.Dispose();
            }
        }

        [Fact]
        public void Reassembling_fragments_in_the_correct_order_should_return_the_completed_set()
        {
            var fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 1, 2, 3 }, 1, false);
            fragments.Count.Should().Be(0);

            using (fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 4, 5, 6 }, 2, true))
            {
                fragments.Count.Should().Be(2);
                fragments[0].Should().Equal(1, 2, 3);
                fragments[1].Should().Equal(4, 5, 6);
            }

            // Once more to ensure no leftover state from previous set
            fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 7, 8, 9 }, 1, false);
            fragments.Count.Should().Be(0);

            using (fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 10, 11, 12 }, 2, true))
            {
                fragments.Count.Should().Be(2);
                fragments[0].Should().Equal(7, 8, 9);
                fragments[1].Should().Equal(10, 11, 12);
            }
        }

        [Fact]
        public void Reassembling_fragments_in_the_wrong_order_should_discard_all_fragments()
        {
            var fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 1, 2, 3 }, 1, false);
            fragments.Count.Should().Be(0);

            fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 7, 8, 9 }, 3, true);
            fragments.Count.Should().Be(0);

            fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 4, 5, 6 }, 2, false);
            fragments.Count.Should().Be(0);
        }

        [Fact]
        public void Reassembling_fragments_interrupted_by_a_solitary_fragment_should_discards_them()
        {
            var fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 1, 2, 3 }, 1, false);
            fragments.Count.Should().Be(0);

            using (fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 1, 2, 3, 4, 5 }, 0, false))
            {
                fragments.Count.Should().Be(1);
                fragments[0].Should().Equal(1, 2, 3, 4, 5);
            }

            fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 4, 5, 6 }, 2, true);
            fragments.Count.Should().Be(0);
        }

        [Fact]
        public void Timeout_when_receiving_fragments_should_discard_all_fragments()
        {
            var timer = this._timerFactory.LastCreated;

            var fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 1, 2, 3 }, 1, false);
            fragments.Count.Should().Be(0);

            fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 4, 5, 6 }, 2, false);
            fragments.Count.Should().Be(0);

            timer.InvokeCallback();

            fragments = this._reassembler.GetCompletedSetOrAdd(new byte[] { 7, 8, 9 }, 3, true);
            fragments.Count.Should().Be(0);
        }

        [Fact]
        public void Reassembling_after_dispose_throws()
        {
            this._reassembler.Dispose();
            this._reassembler
                .Invoking(x => x.GetCompletedSetOrAdd(new byte[] { 1, 2, 3 }, 1, false))
                .ShouldThrow<ObjectDisposedException>();
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0, "All outstanding buffers should be returned.");
        }
    }
}
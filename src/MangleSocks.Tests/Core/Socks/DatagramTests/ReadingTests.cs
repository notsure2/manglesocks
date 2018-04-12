using System;
using System.IO;
using FluentAssertions;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.Helpers;
using Xunit;

namespace MangleSocks.Tests.Core.Socks.DatagramTests
{
    public class ReadingTests : IDisposable
    {
        readonly DebugArrayPool<byte> _bufferPool;

        public ReadingTests()
        {
            this._bufferPool = new DebugArrayPool<byte>();
        }

        [Fact]
        public void Truncated_bytes_throws()
        {
            var stream = new byte[] { 0, 0, 0, (byte)AddressType.Ipv4, 1, 2, 3, 4, 255 };
            Action act = () => Datagram.ReadFrom(stream, this._bufferPool);
            act.Should().Throw<InvalidDataException>().And.Message.Should().ContainEquivalentOf("truncated");
        }

        [Fact]
        public void Incorrect_reserved_bytes_throws()
        {
            var stream = new byte[] { 0, 1, 0, (byte)AddressType.Ipv4, 1, 2, 3, 4, 255, 254 };
            Action act = () => Datagram.ReadFrom(stream, this._bufferPool);
            act.Should().Throw<InvalidDataException>().And.Message.Should().ContainEquivalentOf("corrupt");
        }

        [Fact]
        public void Fragment_in_middle_of_set()
        {
            var stream = new byte[] { 0, 0, 127, (byte)AddressType.Ipv4, 1, 2, 3, 4, 255, 254, 1 };
            var packet = Datagram.ReadFrom(stream, this._bufferPool);
            packet.Header.FragmentPosition.Should().Be(127);
            packet.Header.IsFragment.Should().BeTrue();
            packet.Header.IsFinalFragment.Should().BeFalse();
        }

        [Fact]
        public void Invalid_final_fragment_bit_but_zero_position_throws()
        {
            var stream = new byte[] { 0, 0, 128, (byte)AddressType.Ipv4, 1, 2, 3, 4, 255, 254 };
            Action act = () => Datagram.ReadFrom(stream, this._bufferPool);
            act.Should().Throw<InvalidDataException>().And.Message.Should().ContainEquivalentOf("fragment");
        }

        [Fact]
        public void Final_fragment_of_set()
        {
            var stream = new byte[] { 0, 0, 255, (byte)AddressType.Ipv4, 1, 2, 3, 4, 255, 254, 1 };
            var packet = Datagram.ReadFrom(stream, this._bufferPool);
            packet.Header.FragmentPosition.Should().Be(127);
            packet.Header.IsFragment.Should().BeTrue();
            packet.Header.IsFinalFragment.Should().BeTrue();
        }

        [Fact]
        public void Empty_payload()
        {
            var stream = new byte[] { 0, 0, 0, (byte)AddressType.Ipv4, 1, 2, 3, 4, 255, 254 };
            var packet = Datagram.ReadFrom(stream, this._bufferPool);
            packet.Header.IsFragment.Should().BeFalse();
            packet.Payload.Count.Should().Be(0);
        }

        [Fact]
        public void Non_empty_payload()
        {
            var stream = new byte[] { 0, 0, 0, (byte)AddressType.Ipv4, 1, 2, 3, 4, 255, 254, 1, 2, 3, 4, 5 };
            var packet = Datagram.ReadFrom(stream, this._bufferPool);
            packet.Header.IsFragment.Should().BeFalse();
            packet.Payload.Count.Should().Be(5);
            packet.Payload.ToArray().Should().Equal(1, 2, 3, 4, 5);
        }

        public void Dispose()
        {
            this._bufferPool.Outstanding.Should().Be(0, "All outstanding buffers should be returned.");
        }
    }
}

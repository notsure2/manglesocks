using System;   
using System.Linq;
using System.Net;
using System.Text;
using FluentAssertions;
using FluentAssertions.Execution;
using MangleSocks.Core.Socks;
using Xunit;

namespace MangleSocks.Tests.Core.Socks.DatagramTests
{
    public class WritingTests
    {
        [Theory]
        [CombinatorialData]
        public void Endpoint_payload_fragment_combination_tests(
            [CombinatorialValues(AddressType.Ipv4, AddressType.Ipv6, AddressType.DomainName)] AddressType addressType,
            bool isFragment,
            bool hasPayload,
            bool testInsufficientBuffer)
        {
            EndPoint endPoint;
            byte[] expectedEndPointBytes;

            switch (addressType)
            {
                case AddressType.Ipv4:
                    endPoint = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 127);
                    expectedEndPointBytes = new byte[] { 1, 2, 3, 4, 0, 127 };
                    break;

                case AddressType.Ipv6:
                    endPoint = new IPEndPoint(IPAddress.Parse("::1"), 127);
                    expectedEndPointBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 127 };
                    break;

                case AddressType.DomainName:
                    endPoint = new DnsEndPoint("google.com", 127);
                    var addressByteCount = Encoding.ASCII.GetByteCount("google.com");
                    expectedEndPointBytes = new[] { (byte)addressByteCount }
                        .Concat(Encoding.ASCII.GetBytes("google.com"))
                        .Concat(new byte[] { 0, 127 })
                        .ToArray();
                    break;

                default:
                    throw new AssertionFailedException($"Unknown address type: {addressType}");
            }

            var datagram = new Datagram(
                new DatagramHeader(isFragment ? (byte)2 : (byte)0, isFragment, endPoint),
                hasPayload ? new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 }) : new ArraySegment<byte>());

            // Intentional bytes of empty space before the actual space
            const int emptySpacePrefix = 1;
            var buffer = new byte[emptySpacePrefix + 2 + 1 + 1 + expectedEndPointBytes.Length + datagram.Payload.Count];
            int bufferLengthToCompare = buffer.Length - emptySpacePrefix;

            if (testInsufficientBuffer)
            {
                bufferLengthToCompare--;
                datagram
                    .Invoking(d => d.WriteTo(new ArraySegment<byte>(buffer, emptySpacePrefix, bufferLengthToCompare)))
                    .Should().Throw<ArgumentException>().And.Message.Should().ContainEquivalentOf("insufficient");
                return;
            }

            datagram.WriteTo(new ArraySegment<byte>(buffer, emptySpacePrefix, bufferLengthToCompare));
            var expectedBytes = Enumerable.Repeat((byte)0, emptySpacePrefix)
                .Concat(new byte[] { 0, 0, isFragment ? (byte)130 : (byte)0, (byte)addressType })
                .Concat(expectedEndPointBytes)
                .Concat(datagram.Payload)
                .ToArray();

                buffer.Should().Equal(expectedBytes);
        }
    }
}
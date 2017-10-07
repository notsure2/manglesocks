using System;
using MangleSocks.Core.Socks;

namespace MangleSocks.Tests.Helpers
{
    static class DatagramExtensions
    {
        public static byte[] ToBytes(this Datagram datagram)
        {
            var bytesNeeded = datagram.Header.ByteCount + datagram.Payload.Count;
            var bytes = new byte[bytesNeeded];
            datagram.Header.WriteTo(new ArraySegment<byte>(bytes, 0, datagram.Header.ByteCount));
            Array.Copy(
                datagram.Payload.Array,
                datagram.Payload.Offset,
                bytes,
                datagram.Header.ByteCount,
                datagram.Payload.Count);
            return bytes;
        }
    }
}
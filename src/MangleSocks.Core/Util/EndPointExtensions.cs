using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;

namespace MangleSocks.Core.Util
{
    static class EndPointExtensions
    {
        public static EndPoint ToEndPointWithUnmappedAddress(this EndPoint endPoint)
        {
            if (endPoint is IPEndPoint ipEndPoint && ipEndPoint.Address.IsIPv4MappedToIPv6)
            {
                return new IPEndPoint(ipEndPoint.Address.MapToIPv4(), ipEndPoint.Port);
            }

            return endPoint;
        }

        public static EndPoint ReadEndPoint<TReadOnlyStream>(
            this TReadOnlyStream stream,
            AddressType addressType,
            ArrayPool<byte> bufferPool) where TReadOnlyStream : IReadOnlyStream
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferPool == null) throw new ArgumentNullException(nameof(bufferPool));

            int bufferLengthNeeded;
            byte[] buffer = null;
            try
            {
                IPAddress address;
                ushort port;
                EndPoint endPoint;
                switch (addressType)
                {
                    case AddressType.Ipv4:
                        bufferLengthNeeded = sizeof(int) + sizeof(ushort);
                        buffer = bufferPool.Rent(bufferLengthNeeded);
                        stream.ReadExactly(buffer, 0, bufferLengthNeeded);
                        var integralAddress = BitConverter.ToUInt32(buffer, 0);
                        address = new IPAddress(integralAddress);
                        port = (ushort)(buffer[4] << 8 | buffer[5]);
                        endPoint = new IPEndPoint(address, port);
                        break;

                    case AddressType.Ipv6:
                        bufferLengthNeeded = 16;
                        buffer = bufferPool.Rent(bufferLengthNeeded);
                        stream.ReadExactly(buffer, 0, bufferLengthNeeded);
                        address = new IPAddress(buffer);
                        stream.ReadExactly(buffer, 0, sizeof(ushort));
                        port = (ushort)(buffer[0] << 8 | buffer[1]);
                        endPoint = new IPEndPoint(address, port);
                        break;

                    case AddressType.DomainName:
                        bufferLengthNeeded = byte.MaxValue + sizeof(ushort);
                        buffer = bufferPool.Rent(bufferLengthNeeded);
                        stream.ReadExactly(buffer, 0, 1);
                        var domainLength = buffer[0];
                        stream.ReadExactly(buffer, 0, domainLength + sizeof(ushort));
                        port = (ushort)(buffer[domainLength] << 8 | buffer[domainLength + 1]);
                        string domain = Encoding.ASCII.GetString(buffer, 0, domainLength);
                        endPoint = new DnsEndPoint(domain, port);
                        break;

                    default:
                        throw new ProtocolException(
                            $"Invalid address type: {addressType}",
                            CommandReplyType.AddressTypeNotSupported);
                }
                return endPoint;
            }
            finally
            {
                if (buffer != null)
                {
                    bufferPool.Return(buffer);
                }
            }
        }

        public static int ToBytes(this EndPoint bindEndPoint, byte[] buffer, int offset)
        {
            int bytesWritten;

            switch (bindEndPoint)
            {
                case IPEndPoint ipv4EndPoint when ipv4EndPoint.AddressFamily == AddressFamily.InterNetwork:
                    buffer[offset] = (byte)AddressType.Ipv4;
                    Buffer.BlockCopy(ipv4EndPoint.Address.GetAddressBytes(), 0, buffer, offset + 1, 4);
                    buffer[offset + 5] = (byte)(ipv4EndPoint.Port >> 8);
                    buffer[offset + 6] = (byte)ipv4EndPoint.Port;
                    bytesWritten = 7;
                    break;

                case IPEndPoint ipv6EndPoint when ipv6EndPoint.AddressFamily == AddressFamily.InterNetworkV6:
                    buffer[offset] = (byte)AddressType.Ipv6;
                    Buffer.BlockCopy(ipv6EndPoint.Address.GetAddressBytes(), 0, buffer, offset + 1, 16);
                    buffer[offset + 17] = (byte)(ipv6EndPoint.Port >> 8);
                    buffer[offset + 18] = (byte)ipv6EndPoint.Port;
                    bytesWritten = 19;
                    break;

                case DnsEndPoint dnsEndPoint:
                    buffer[offset] = (byte)AddressType.DomainName;
                    var bytesCopied = Encoding.ASCII.GetBytes(
                        dnsEndPoint.Host,
                        0,
                        dnsEndPoint.Host.Length,
                        buffer,
                        offset + 2);
                    buffer[offset + 1] = (byte)bytesCopied;
                    buffer[offset + 2 + bytesCopied] = (byte)(dnsEndPoint.Port >> 8);
                    buffer[offset + 2 + bytesCopied + 1] = (byte)dnsEndPoint.Port;
                    bytesWritten = 1 + 1 + bytesCopied + 2;
                    break;

                default:
                    throw new InvalidDataException($"Invalid EndPoint type: {bindEndPoint.GetType().FullName}");
            }

            return bytesWritten;
        }
    }
}
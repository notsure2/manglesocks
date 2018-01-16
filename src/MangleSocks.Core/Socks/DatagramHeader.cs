using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MangleSocks.Core.IO;
using MangleSocks.Core.Util;

namespace MangleSocks.Core.Socks
{
    public struct DatagramHeader : IEquatable<DatagramHeader>
    {
        const int c_FixedHeaderPartLength = 2 + 1 + 1 + sizeof(ushort);

        public const int MaxUdpSize = ushort.MaxValue + 1;
        public const int InternetProtocolV4HeaderLength = c_FixedHeaderPartLength + sizeof(int);
        public const int InternetProtocolV6HeaderLength = c_FixedHeaderPartLength + 16;

        static readonly IPEndPoint s_EmptyEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public byte FragmentPosition { get; }
        public bool IsFinalFragment { get; }

        /// <summary>
        /// This is the requested destination when relaying packets received from
        /// the SOCKS client, and the reply source when relaying back to the client.
        /// </summary>
        public EndPoint RemoteEndPoint { get; }

        public bool IsFragment => this.FragmentPosition != 0;

        public int ByteCount
        {
            get
            {
                int lengthRequired;

                var remoteEndPoint = this.RemoteEndPoint ?? s_EmptyEndPoint;
                switch (remoteEndPoint)
                {
                    case IPEndPoint ipv4EndPoint when ipv4EndPoint.Address.AddressFamily == AddressFamily.InterNetwork:
                        lengthRequired = InternetProtocolV4HeaderLength;
                        break;

                    case IPEndPoint ipv6EndPoint when ipv6EndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6:
                        lengthRequired = InternetProtocolV6HeaderLength;
                        break;

                    case DnsEndPoint dnsEndPoint:
                        lengthRequired = c_FixedHeaderPartLength + dnsEndPoint.Host.Length + 1;
                        break;

                    default:
                        throw new InvalidDataException($"Invalid EndPoint type: {remoteEndPoint.GetType().FullName}");
                }
                return lengthRequired;
            }
        }

        public DatagramHeader(EndPoint remoteEndPoint) : this(0, false, remoteEndPoint) { }

        public DatagramHeader(byte fragmentPosition, bool isFinalFragment, EndPoint remoteEndPoint)
        {
            this.FragmentPosition = fragmentPosition;
            this.IsFinalFragment = fragmentPosition != 0 && isFinalFragment;
            this.RemoteEndPoint = remoteEndPoint ?? s_EmptyEndPoint;
        }

        public int WriteTo(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null)
            {
                throw new ArgumentNullException(nameof(buffer.Array));
            }

            var lengthRequired = this.ByteCount;

            if (buffer.Count < lengthRequired)
            {
                throw new ArgumentException("Insufficient buffer space.", nameof(buffer));
            }

            int index = buffer.Offset;
            buffer.Array[index++] = 0;
            buffer.Array[index++] = 0;

            var fragment = this.FragmentPosition;
            if (fragment > 0 && this.IsFinalFragment)
            {
                fragment |= 128;
            }
            buffer.Array[index++] = fragment;

            var remoteEndPoint = this.RemoteEndPoint ?? s_EmptyEndPoint;
            remoteEndPoint.ToBytes(buffer.Array, index);

            return lengthRequired;
        }

        public static DatagramHeader ReadFrom(ArraySegment<byte> buffer, ArrayPool<byte> bufferPool)
        {
            if (buffer.Array == null) throw new ArgumentNullException(nameof(buffer.Array));
            if (bufferPool == null) throw new ArgumentNullException(nameof(bufferPool));

            // | RSV | FRAG | ATYP | DST.ADDR | DST.PORT | DATA     |
            // |   2 |    1 |    1 | Variable |        2 | Variable |
            if (buffer.Count < 10)
            {
                throw new InvalidDataException("UDP SOCKS datagram is truncated.");
            }

            if (buffer.Array[buffer.Offset] != 0
                || buffer.Array[buffer.Offset + 1] != 0)
            {
                throw new InvalidDataException("UDP SOCKS datagram is corrupt.");
            }

            var fragmentPosition = (byte)(buffer.Array[buffer.Offset + 2] & 127);
            var isFinalFragment = (buffer.Array[buffer.Offset + 2] & 128) != 0;

            if (isFinalFragment && fragmentPosition == 0)
            {
                throw new InvalidDataException("Zero fragment position but last fragment bit set");
            }

            var addressType = (AddressType)buffer.Array[buffer.Offset + 3];

            var packetStream = new BufferReadOnlyStream(buffer.Array, buffer.Offset + 4, buffer.Count - 4);
            var remoteEndPoint = packetStream.ReadEndPoint(addressType, bufferPool);

            return new DatagramHeader(fragmentPosition, isFinalFragment, remoteEndPoint);
        }

        public override bool Equals(object obj)
        {
            return obj is DatagramHeader header && this.Equals(header);
        }

        public bool Equals(DatagramHeader other)
        {
            return this.FragmentPosition == other.FragmentPosition &&
                   this.IsFinalFragment == other.IsFinalFragment &&
                   EqualityComparer<EndPoint>.Default.Equals(this.RemoteEndPoint, other.RemoteEndPoint);
        }

        public override int GetHashCode()
        {
            var hashCode = 40103158;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + this.FragmentPosition.GetHashCode();
            hashCode = hashCode * -1521134295 + this.IsFinalFragment.GetHashCode();
            hashCode = hashCode * -1521134295
                       + EqualityComparer<EndPoint>.Default.GetHashCode(this.RemoteEndPoint);
            return hashCode;
        }

        public static bool operator ==(DatagramHeader header1, DatagramHeader header2)
        {
            return header1.Equals(header2);
        }

        public static bool operator !=(DatagramHeader header1, DatagramHeader header2)
        {
            return !(header1 == header2);
        }

        public override string ToString()
        {
            return this.IsFragment
                ? $"#{this.FragmentPosition}; Final: {this.IsFinalFragment}; {this.RemoteEndPoint}"
                : this.RemoteEndPoint.ToString();
        }
    }
}
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;

namespace MangleSocks.Core.Socks
{
    public struct Datagram : IEquatable<Datagram>
    {
        public DatagramHeader Header { get; }
        public ArraySegment<byte> Payload { get; }

        public Datagram(DatagramHeader header, ArraySegment<byte> payload)
        {
            this.Header = header;
            this.Payload = payload;
        }

        public void WriteTo(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null)
            {
                throw new ArgumentNullException(nameof(buffer.Array));
            }

            int headerBytesWritten = this.Header.WriteTo(buffer);
            if (headerBytesWritten + this.Payload.Count > buffer.Count)
            {
                throw new ArgumentException("Insufficient buffer space.");
            }

            if (this.Payload.Array != null)
            {
                Buffer.BlockCopy(
                    this.Payload.Array,
                    this.Payload.Offset,
                    buffer.Array,
                    buffer.Offset + headerBytesWritten,
                    this.Payload.Count);
            }
        }

        public static Datagram Create(EndPoint remoteEndPoint, params byte[] payload)
        {
            return new Datagram(new DatagramHeader(remoteEndPoint), new ArraySegment<byte>(payload));
        }

        public static Datagram ReadFrom(byte[] buffer, ArrayPool<byte> bufferPool)
        {
            return ReadFrom(new ArraySegment<byte>(buffer), bufferPool);
        }

        public static Datagram ReadFrom(ArraySegment<byte> buffer, ArrayPool<byte> bufferPool)
        {
            if (buffer.Array == null) throw new ArgumentNullException(nameof(buffer.Array));
            if (bufferPool == null) throw new ArgumentNullException(nameof(bufferPool));

            var header = DatagramHeader.ReadFrom(buffer, bufferPool);
            var headerLength = header.ByteCount;
            var payload = new ArraySegment<byte>(
                buffer.Array,
                buffer.Offset + headerLength,
                buffer.Count - headerLength);

            return new Datagram(header, payload);
        }

        public override bool Equals(object obj)
        {
            return obj is Datagram datagram && this.Equals(datagram);
        }

        public bool Equals(Datagram other)
        {
            return EqualityComparer<DatagramHeader>.Default.Equals(this.Header, other.Header) &&
                   EqualityComparer<ArraySegment<byte>>.Default.Equals(this.Payload, other.Payload);
        }

        public override int GetHashCode()
        {
            var hashCode = 1268427973;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<DatagramHeader>.Default.GetHashCode(this.Header);
            hashCode = hashCode * -1521134295 + EqualityComparer<ArraySegment<byte>>.Default.GetHashCode(this.Payload);
            return hashCode;
        }

        public static bool operator ==(Datagram datagram1, Datagram datagram2)
        {
            return datagram1.Equals(datagram2);
        }

        public static bool operator !=(Datagram datagram1, Datagram datagram2)
        {
            return !(datagram1 == datagram2);
        }

        public override string ToString()
        {
            return $"{this.Header}; {this.Payload.Count} bytes";
        }
    }
}
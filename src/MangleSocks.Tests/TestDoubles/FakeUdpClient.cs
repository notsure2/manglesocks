using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;
using MangleSocks.Tests.Helpers;

namespace MangleSocks.Tests.TestDoubles
{
    class FakeUdpClient : IBoundUdpClient
    {
        readonly CancellationTokenSource _disposeTokenSource;
        readonly BufferBlock<QueuedReceivedPacket> _received;
        readonly BufferBlock<QueuedSentPacket> _sent;

        bool _disposed;

        public EndPoint BindEndPoint { get; }

        public FakeUdpClient()
        {
            this._disposeTokenSource = new CancellationTokenSource();
            this._received = new BufferBlock<QueuedReceivedPacket>();
            this._sent = new BufferBlock<QueuedSentPacket>();
            this.BindEndPoint = FakeEndPoints.CreateLocal();
        }

        public Datagram StageReceivedDatagram(EndPoint source, EndPoint relayToDestination, params byte[] packet)
        {
            var datagram = Datagram.Create(relayToDestination, packet);
            this.StageReceivedPacket(source, datagram.ToBytes());
            return datagram;
        }

        public void StageReceivedPacket(EndPoint source, params byte[] packet)
        {
            this._received.Post(
                new QueuedReceivedPacket
                {
                    Source = source,
                    Packet = packet
                });
        }

        public async Task<SocketReceiveFromResult> ReceiveAsync(
            byte[] buffer,
            int offset,
            EndPoint remoteEndPoint)
        {
            var queuedPacket = await this._received
                .ReceiveAsync(this._disposeTokenSource.Token)
                .ConfigureAwait(false);
            Array.Copy(queuedPacket.Packet, 0, buffer, offset, queuedPacket.Packet.Length);

            return new SocketReceiveFromResult
            {
                RemoteEndPoint = queuedPacket.Source,
                ReceivedBytes = queuedPacket.Packet.Length
            };
        }

        public Datagram SendDatagram(EndPoint destinationEndPoint, params byte[] buffer)
        {
            var datagram = Datagram.Create(destinationEndPoint, buffer);
            this.Send(destinationEndPoint, datagram.ToBytes());
            return datagram;
        }

        public void Send(EndPoint destinationEndPoint, params byte[] buffer)
        {
            this._sent.Post(
                new QueuedSentPacket
                {
                    Destination = destinationEndPoint,
                    Packet = buffer
                });
        }

        public async Task<int> SendAsync(byte[] buffer, int offset, int count, EndPoint destinationEndPoint)
        {
            var bytes = new byte[count];
            Array.Copy(buffer, offset, bytes, 0, count);
            await this._sent.SendAsync(
                new QueuedSentPacket
                {
                    Destination = destinationEndPoint,
                    Packet = bytes
                },
                this._disposeTokenSource.Token).ConfigureAwait(false);
            return count;
        }

        public IList<QueuedSentPacket> WaitForSentPackets(int count, int timeoutInSeconds = 5)
        {
            var batchBlock = new BatchBlock<QueuedSentPacket>(count);
            this._sent.LinkTo(batchBlock, new DataflowLinkOptions { MaxMessages = count });
            return batchBlock.Receive(TimeSpan.FromSeconds(timeoutInSeconds));
        }

        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._disposeTokenSource.Cancel();
            this._disposeTokenSource.Dispose();
            this._disposed = true;
        }

        internal struct QueuedSentPacket
        {
            public EndPoint Destination;
            public byte[] Packet;
        }

        internal struct QueuedReceivedPacket
        {
            public EndPoint Source;
            public byte[] Packet;
        }
    }
}
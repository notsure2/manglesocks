using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MangleSocks.Core.IO;

namespace MangleSocks.Tests.TestDoubles
{
    class FakeUdpClient : IBoundUdpClient
    {
        readonly CancellationTokenSource _disposeTokenSource;
        readonly BufferBlock<QueuedPacket> _queuedPackets;
        readonly BufferBlock<QueuedPacket> _sentPackets;

        bool _disposed;

        public EndPoint BindEndPoint { get; }

        public FakeUdpClient()
        {
            this._disposeTokenSource = new CancellationTokenSource();
            this._queuedPackets = new BufferBlock<QueuedPacket>();
            this._sentPackets = new BufferBlock<QueuedPacket>();
            this.BindEndPoint = FakeEndPoints.CreateLocal();
        }

        public void StageReceivedPacket(EndPoint destination, params byte[] packet)
        {
            this._queuedPackets.Post(
                new QueuedPacket
                {
                    Destination = destination,
                    Packet = packet
                });
        }

        public async Task<SocketReceiveFromResult> ReceiveAsync(
            byte[] buffer,
            int offset,
            int count,
            EndPoint remoteEndPoint)
        {
            var queuedPacket = await this._queuedPackets
                .ReceiveAsync(this._disposeTokenSource.Token)
                .ConfigureAwait(false);
            Array.Copy(queuedPacket.Packet, 0, buffer, offset, queuedPacket.Packet.Length);

            return new SocketReceiveFromResult
            {
                RemoteEndPoint = queuedPacket.Destination,
                ReceivedBytes = queuedPacket.Packet.Length
            };
        }

        public void Send(EndPoint destinationEndPoint, params byte[] buffer)
        {
            this._sentPackets.Post(
                new QueuedPacket
                {
                    Destination = destinationEndPoint,
                    Packet = buffer
                });
        }

        public async Task<int> SendAsync(byte[] buffer, int offset, int count, EndPoint destinationEndPoint)
        {
            var bytes = new byte[count];
            Array.Copy(buffer, offset, bytes, 0, count);
            await this._sentPackets.SendAsync(
                new QueuedPacket
                {
                    Destination = destinationEndPoint,
                    Packet = bytes
                },
                this._disposeTokenSource.Token).ConfigureAwait(false);
            return count;
        }

        public IList<QueuedPacket> WaitForSentPackets(int count)
        {
            var batchBlock = new BatchBlock<QueuedPacket>(count);
            this._sentPackets.LinkTo(batchBlock, new DataflowLinkOptions { MaxMessages = count });
            return batchBlock.Receive();
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

        internal struct QueuedPacket
        {
            public EndPoint Destination;
            public byte[] Packet;
        }
    }
}
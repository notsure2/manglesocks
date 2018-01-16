using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MangleSocks.Core.IO;
using SystemTcpListener = System.Net.Sockets.TcpListener;

namespace MangleSocks.Tests.TestDoubles
{
    sealed class LoopbackTcpStream : ITcpStream
    {
        readonly Stream _senderStream;
        readonly Stream _receiverStream;
        readonly BufferBlock<byte> _writtenBytes;

        public EndPoint LocalEndPoint { get; }
        public EndPoint RemoteEndPoint { get; }
        public bool IsDisposed { get; private set; }

        public LoopbackTcpStream(params byte[] initialStagedReadBytes)
        {
            var tcpListener = new SystemTcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();
                var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                clientSocket.Connect(tcpListener.LocalEndpoint);
                this._senderStream = new NetworkStream(clientSocket, true);
                var receiverSocket = tcpListener.AcceptSocket();
                this._receiverStream = new NetworkStream(receiverSocket, true);
                this.LocalEndPoint = receiverSocket.LocalEndPoint;
                this.RemoteEndPoint = receiverSocket.RemoteEndPoint;

                this._writtenBytes = new BufferBlock<byte>();
            }
            finally
            {
                tcpListener.Stop();
            }

            if (initialStagedReadBytes != null)
            {
                this.StageReadBytes(initialStagedReadBytes);
            }
        }

        public void StageReadBytes(params byte[] stagedBytesToRead)
        {
            this._senderStream.Write(stagedBytesToRead, 0, stagedBytesToRead.Length);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return this._receiverStream.Read(buffer, offset, count);
        }

        public Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default)
        {
            return this._receiverStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public void Write(params byte[] bytes)
        {
            this.WriteAsync(bytes, 0, bytes.Length);
        }

        public Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int i = offset; i < count; i++)
            {
                this._writtenBytes.Post(buffer[i]);
            }
            return Task.CompletedTask;
        }

        public void CloseStagedBytesSender()
        {
            this._senderStream.Dispose();
        }

        public IList<byte> GetAllWrittenBytes()
        {
            this._writtenBytes.TryReceiveAll(out var bytes);
            return bytes;
        }

        public byte[] WaitForWrittenBytes(int count, int timeoutSeconds = 5)
        {
            var batchBlock = new BatchBlock<byte>(count);
            this._writtenBytes.LinkTo(batchBlock, new DataflowLinkOptions { MaxMessages = count });
            return batchBlock.Receive(TimeSpan.FromSeconds(timeoutSeconds));
        }

        public ITcpConnector GetConnector()
        {
            return new FakeTcpConnector(this);
        }

        public ITcpListener GetListener()
        {
            return new FakeTcpListener(this);
        }

        public void Dispose()
        {
            this._receiverStream.Dispose();
            this._senderStream.Dispose();
            this.IsDisposed = true;
        }

        class FakeTcpConnector : ITcpConnector
        {
            readonly ITcpStream _tcpStream;

            public FakeTcpConnector(ITcpStream stagedTcpStream)
            {
                this._tcpStream = stagedTcpStream ?? throw new ArgumentNullException(nameof(stagedTcpStream));
            }

            public Task<ITcpStream> ConnectTcpAsync(EndPoint destinationEndPoint)
            {
                return Task.FromResult(this._tcpStream);
            }
        }

        class FakeTcpListener : ITcpListener
        {
            readonly ITcpStream _tcpStream;

            public FakeTcpListener(ITcpStream stagedTcpStream)
            {
                this._tcpStream = stagedTcpStream ?? throw new ArgumentNullException(nameof(stagedTcpStream));
            }

            public Task<ITcpStream> GetNextClientStreamAsync()
            {
                return Task.FromResult(this._tcpStream);
            }

            public void Dispose()
            {
            }
        }
    }
}
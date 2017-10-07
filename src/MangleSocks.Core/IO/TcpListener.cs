using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemTcpListener = System.Net.Sockets.TcpListener;

namespace MangleSocks.Core.IO
{
    public sealed class TcpListener : ITcpListener
    {
        public const ushort DefaultPort = 1081;

        readonly SystemTcpListener _listener;
        readonly ILogger _log;
        bool _disposed;

        public EndPoint ListenEndPoint => this._listener.LocalEndpoint;

        public TcpListener(IPEndPoint listenEndPoint, ILoggerFactory loggerFactory)
        {
            if (listenEndPoint == null) throw new ArgumentNullException(nameof(listenEndPoint));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            this._listener = new SystemTcpListener(listenEndPoint);
            this._log = loggerFactory.CreateLogger(this.GetType().Name);

            this._log.LogInformation("Starting TCP listener on {0}", this._listener.LocalEndpoint);
            this._listener.Start();
        }

        public async Task<ITcpStream> GetNextClientStreamAsync()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            var socket = await this._listener.AcceptSocketAsync().ConfigureAwait(false);
            socket.NoDelay = true;
            return new TcpStream(socket);
        }

        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._log.LogInformation("Stopping TCP listener on {0}", this._listener.LocalEndpoint);
            this._listener.Stop();
            this._disposed = true;
        }
    }
}
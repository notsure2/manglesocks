using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Core.Server
{
    public sealed class SocksServer : IDisposable
    {
        readonly ITcpListener _listener;
        readonly ISocksConnectionFactory _connectionFactory;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly List<IDisposable> _connections;
        readonly ILogger _log;
        readonly object _startStopLock = new object();

        bool _disposed;
        Task _serverLoop;

        public SocksServer(
            ITcpListener listener,
            ISocksConnectionFactory connectionFactory,
            ILoggerFactory loggerFactory)
        {
            this._listener = listener ?? throw new ArgumentNullException(nameof(listener));
            this._connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            this._cancellationTokenSource = new CancellationTokenSource();
            this._connections = new List<IDisposable>(20);
            this._log = loggerFactory.CreateLogger(this.GetType().Name);
        }

        public void Start()
        {
            lock (this._startStopLock)
            {
                if (this._disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                if (this._serverLoop != null)
                {
                    throw new InvalidOperationException("Server already running.");
                }

                this._log.LogInformation("Starting up...");

                this._serverLoop = Task.Run(
                    this.DoServerLoop,
                    this._cancellationTokenSource.Token);
            }
        }

        async Task DoServerLoop()
        {
            var cancellationToken = this._cancellationTokenSource.Token;

            using (cancellationToken.Register(o => ((IDisposable)o).Dispose(), this._listener))
            {
                ITcpStream stream = null;
                ISocksConnection connection = null;
                try
                {
                    while (true)
                    {
                        stream = await this._listener.GetNextClientStreamAsync().ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();

                        this._log.LogDebug("Accepted connection from {0}", stream.RemoteEndPoint);
                        connection = this._connectionFactory.Create(stream);
                        this._connections.Add(connection);
                        connection.StartHandlingClient();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
                {
                    try
                    {
                        connection?.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }

                    try
                    {
                        stream?.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }
                catch (Exception ex)
                {
                    this._log.LogError(ex, "Unhandled exception in background listener loop");
                }
            }
        }

        public void Dispose()
        {
            lock (this._startStopLock)
            {
                if (this._disposed)
                {
                    return;
                }

                this._log.LogInformation("Shutting down...");

                // Sequence is important
                this._cancellationTokenSource.Cancel();

                try
                {
                    this._serverLoop?.GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    // ignored
                }

                this._serverLoop?.Dispose();
                this._cancellationTokenSource.Dispose();

                foreach (var connection in this._connections)
                {
                    try
                    {
                        connection.Dispose();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                this._log.LogDebug("Shutdown complete.");
                this._disposed = true;
            }
        }
    }
}
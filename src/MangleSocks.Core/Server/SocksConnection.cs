using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Core.Server
{
    public sealed class SocksConnection : ISocksConnection
    {
        static readonly IPEndPoint s_EmptyEndPoint = new IPEndPoint(0, 0);

        readonly ITcpStream _clientStream;
        readonly IProxyFactory _proxyFactory;
        readonly ArrayPool<byte> _bufferPool;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly ILogger _log;
        readonly object _startStopLocker = new object();

        Task _handleClientTask;
        bool _disposed;

        public SocksConnection(
            ITcpStream stream,
            IProxyFactory proxyFactory,
            ArrayPool<byte> bufferPool,
            ILoggerFactory loggerFactory)
        {
            this._clientStream = stream ?? throw new ArgumentNullException(nameof(stream));
            this._proxyFactory = proxyFactory ?? throw new ArgumentNullException(nameof(proxyFactory));
            this._bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));

            this._cancellationTokenSource = new CancellationTokenSource();
            this._log = loggerFactory.CreateLogger(this.GetType().Name);
        }

        public void StartHandlingClient()
        {
            lock (this._startStopLocker)
            {
                if (this._disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                if (this._handleClientTask != null)
                {
                    throw new InvalidOperationException("Client already being handled.");
                }

                this._handleClientTask = Task.Run(
                    this.HandleClientAsync,
                    this._cancellationTokenSource.Token);
            }
        }

        async Task HandleClientAsync()
        {
            var cancellationToken = this._cancellationTokenSource.Token;

            using (this._log.BeginScope(Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=')))
            using (cancellationToken.Register(o => ((IDisposable)o).Dispose(), this._clientStream))
            {
                try
                {
                    this._log.LogInformation("Incoming connection from {0}", this._clientStream.RemoteEndPoint);
                    if (!await this.TryNegotiateSupportedAuthenticationMethodAsync().ConfigureAwait(false))
                    {
                        return;
                    }
                    await this.RunProxyAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    this._log.LogError(ex, "Error.");
                }
                finally
                {
                    try
                    {
                        this._clientStream.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                    finally
                    {
                        this._log.LogInformation("Connection terminated.");
                    }
                }
            }
        }

        async Task<bool> TryNegotiateSupportedAuthenticationMethodAsync()
        {
            var greeting = await Greeting
                .ReadFromAsync(this._clientStream, this._bufferPool)
                .ConfigureAwait(false);

            if (greeting.SupportedAuthenticationMethods.All(x => x != AuthenticationMethod.None))
            {
                this._log.LogError("Client does not support no authentication.");
                await new GreetingReply(AuthenticationMethod.NoAcceptableMethods)
                    .WriteToAsync(this._clientStream, this._bufferPool)
                    .ConfigureAwait(false);
                return false;
            }

            await new GreetingReply(AuthenticationMethod.None)
                .WriteToAsync(this._clientStream, this._bufferPool)
                .ConfigureAwait(false);
            return true;
        }

        async Task RunProxyAsync(CancellationToken cancellationToken)
        {
            IProxy proxy;
            try
            {
                var command = await Command.ReadFromAsync(this._clientStream, this._bufferPool).ConfigureAwait(false);
                switch (command.CommandType)
                {
                    case CommandType.Connect:
                        proxy = this._proxyFactory.CreateTcpProxy(
                            this._clientStream,
                            command.EndPoint,
                            this._bufferPool);
                        break;

                    case CommandType.UdpAssociate:
                        proxy = this._proxyFactory.CreateUdpProxy(this._clientStream, this._bufferPool);
                        break;

                    default:
                        throw new ProtocolException(CommandReplyType.CommandNotSupported);
                }
            }
            catch (Exception ex)
            {
                var commandReplyType = CommandReplyType.GeneralFailure;
                switch (ex)
                {
                    case ProtocolException socksEx:
                        this._log.LogError(socksEx, "Protocol error");
                        commandReplyType = socksEx.ErrorCode;
                        break;

                    case IOException _:
                    case SocketException _:
                        this._log.LogError("Network error: {0}", ex.Message);
                        break;

                    default:
                        this._log.LogError("Connection error.", ex);
                        break;
                }

                await new CommandReply(commandReplyType, s_EmptyEndPoint)
                    .WriteToAsync(this._clientStream, this._bufferPool)
                    .ConfigureAwait(false);
                return;
            }

            var reply = new CommandReply(CommandReplyType.Succeeded, proxy.BindEndPoint);
            await reply.WriteToAsync(this._clientStream, this._bufferPool).ConfigureAwait(false);

            using (proxy)
            {
                await proxy.RunAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            lock (this._startStopLocker)
            {
                if (this._disposed)
                {
                    return;
                }

                this._cancellationTokenSource.Cancel();

                try
                {
                    this._handleClientTask.GetAwaiter().GetResult();
                }
                catch
                {
                    // ignored
                }

                this._handleClientTask.Dispose();
                this._cancellationTokenSource.Dispose();

                this._disposed = true;
            }
        }
    }
}
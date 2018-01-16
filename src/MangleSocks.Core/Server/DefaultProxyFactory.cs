using System;
using System.Buffers;
using System.Net;
using MangleSocks.Core.IO;
using MangleSocks.Core.Util.Threading;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Core.Server
{
    public class DefaultProxyFactory : IProxyFactory
    {
        readonly IDatagramInterceptor _datagramInterceptor;
        readonly ITcpConnector _tcpConnector;
        readonly IUdpClientFactory _udpClientFactory;
        readonly ILoggerFactory _loggerFactory;
        readonly ITimerFactory _timerFactory;

        public DefaultProxyFactory(
            IDatagramInterceptor datagramInterceptor,
            ITcpConnector tcpConnector,
            IUdpClientFactory udpClientFactory,
            ILoggerFactory loggerFactory)
        {
            this._datagramInterceptor = datagramInterceptor
                                        ?? throw new ArgumentNullException(nameof(datagramInterceptor));
            this._tcpConnector = tcpConnector ?? throw new ArgumentNullException(nameof(tcpConnector));
            this._udpClientFactory = udpClientFactory ?? throw new ArgumentNullException(nameof(udpClientFactory));
            this._loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this._timerFactory = ThreadingTimerFactory.Instance;
        }

        public IProxy CreateTcpProxy(ITcpStream clientStream, EndPoint destinationEndPoint, ArrayPool<byte> bufferPool)
        {
            return new TcpProxy(clientStream, destinationEndPoint, this._tcpConnector, bufferPool);
        }

        public IProxy CreateUdpProxy(IReadOnlyTcpStream clientStream, ArrayPool<byte> bufferPool)
        {
            return new UdpProxy(
                clientStream,
                this._udpClientFactory,
                this._datagramInterceptor,
                bufferPool,
                this._timerFactory,
                this._loggerFactory);
        }
    }
}
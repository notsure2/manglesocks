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
        readonly ILoggerFactory _loggerFactory;
        readonly ITimerFactory _timerFactory;
        readonly IConnector _connector;

        public DefaultProxyFactory(IDatagramInterceptor datagramInterceptor, ILoggerFactory loggerFactory)
        {
            this._datagramInterceptor = datagramInterceptor
                                        ?? throw new ArgumentNullException(nameof(datagramInterceptor));
            this._loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this._timerFactory = ThreadingTimerFactory.Instance;
            this._connector = new DefaultConnector();
        }

        public IProxy CreateTcpProxy(ITcpStream clientStream, EndPoint destinationEndPoint, ArrayPool<byte> bufferPool)
        {
            return new TcpProxy(clientStream, destinationEndPoint, this._connector, bufferPool);
        }

        public IProxy CreateUdpProxy(IReadOnlyTcpStream clientStream, ArrayPool<byte> bufferPool)
        {
            return new UdpProxy(
                clientStream,
                this._connector,
                this._datagramInterceptor,
                bufferPool,
                this._timerFactory,
                this._loggerFactory);
        }
    }
}
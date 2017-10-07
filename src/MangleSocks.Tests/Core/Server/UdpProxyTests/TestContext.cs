using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using MangleSocks.Core.Server;
using MangleSocks.Tests.Helpers;
using MangleSocks.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace MangleSocks.Tests.Core.Server.UdpProxyTests
{
    class TestContext : IDisposable
    {
        readonly ManuallyInvokedTimerFactory _timerFactory;
        readonly DebugArrayPool<byte> _bufferPool;

        public LoopbackTcpStream ClientStream { get; }
        public FakeUdpClient BoundUdpClient { get; }
        public FakeUdpClient RelayingUdpClient { get; }
        public ManuallyInvokedTimer Timer => this._timerFactory.LastCreated;
        public IDatagramInterceptor Interceptor { get; }
        public UdpProxy Proxy { get; }

        public TestContext()
        {
            this.ClientStream = new LoopbackTcpStream();
            this.BoundUdpClient = new FakeUdpClient();
            this.RelayingUdpClient = new FakeUdpClient();
            this._timerFactory = new ManuallyInvokedTimerFactory();
            this._bufferPool = new DebugArrayPool<byte>();
            this.Interceptor = A.Fake<IDatagramInterceptor>();

            this.Proxy = new UdpProxy(
                this.ClientStream,
                new FakeUdpClientFactory(this.BoundUdpClient, this.RelayingUdpClient),
                this.Interceptor,
                this._bufferPool,
                this._timerFactory,
                new NullLoggerFactory());
        }

        public void RunProxy(Action concurrentAction)
        {
            Exception actionError = null;
            try
            {
                Task.Run(
                    () =>
                    {
                        var proxyTask = this.Proxy.RunAsync(CancellationToken.None);
                        try
                        {
                            concurrentAction();
                        }
                        catch (Exception ex)
                        {
                            actionError = ex;
                        }
                        this.Proxy.Dispose();
                        return proxyTask;
                    }).GetAwaiter().GetResult();
            }
            catch (IOException)
            {
                // ignore
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                if (actionError == null)
                {
                    throw;
                }

                var combined = new AggregateException(ex, actionError);
                combined.Flatten();
                throw combined;
            }

            if (actionError != null)
            {
                ExceptionDispatchInfo.Capture(actionError).Throw();
            }
        }

        public void Dispose()
        {
            this.Proxy.Dispose();
            this._bufferPool.Outstanding.Should().Be(0, "All outstanding buffers should be returned.");
            this.ClientStream.Dispose();
        }
    }
}
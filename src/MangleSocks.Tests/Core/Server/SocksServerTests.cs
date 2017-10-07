using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;
using FakeItEasy;
using MangleSocks.Core.Server;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TcpListener = MangleSocks.Core.IO.TcpListener;

namespace MangleSocks.Tests.Core.Server
{
    public class SocksServerTests
    {
        [Fact]
        public void Disposing_should_dispose_all_open_connections()
        {
            var listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, 0), new NullLoggerFactory());

            var connectionsBlock = new BufferBlock<ISocksConnection>();
            var connectionBatchBlock = new BatchBlock<ISocksConnection>(2);
            connectionsBlock.LinkTo(connectionBatchBlock);

            var connectionFactory = A.Fake<ISocksConnectionFactory>();
            A.CallTo(() => connectionFactory.Create(null))
                .WithAnyArguments()
                .ReturnsLazily(
                    () =>
                    {
                        var connection = A.Fake<ISocksConnection>();
                        connectionsBlock.Post(connection);
                        return connection;
                    });

            ISocksConnection[] connections;
            using (var server = new SocksServer(listener, connectionFactory, new NullLoggerFactory()))
            {
                server.Start();
                var client1 = new TcpClient();
                client1.Connect(
                    ((IPEndPoint)listener.ListenEndPoint).Address,
                    ((IPEndPoint)listener.ListenEndPoint).Port);
                var client2 = new TcpClient();
                client2.Connect(
                    ((IPEndPoint)listener.ListenEndPoint).Address,
                    ((IPEndPoint)listener.ListenEndPoint).Port);
                connections = connectionBatchBlock.Receive();
            }

            foreach (var connection in connections)
            {
                A.CallTo(() => connection.Dispose()).MustHaveHappened();
            }
        }
    }
}
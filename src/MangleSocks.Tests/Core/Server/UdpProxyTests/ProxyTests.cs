using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;
using MangleSocks.Core.Server;
using MangleSocks.Core.Server.DatagramInterceptors;
using MangleSocks.Tests.Helpers;
using MangleSocks.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MangleSocks.Tests.Core.Server.UdpProxyTests
{
    public class ProxyTests : IDisposable
    {
        readonly TestContext _context;

        public ProxyTests()
        {
            this._context = new TestContext();
        }

        [Fact]
        public void Proxy_sends_non_fragmented_packets_in_both_directions()
        {
            var source = FakeEndPoints.CreateLocal();
            var destination = FakeEndPoints.CreateRemote();

            var boundUdpClientStagedReceivedDatagram = Datagram.From(destination, 1, 2, 3, 4);
            this._context.BoundUdpClient.StageReceivedPacket(
                destination,
                boundUdpClientStagedReceivedDatagram.ToBytes());

            var boundUdpClientSentFirstDatagram = Datagram.From(source, 200, 199, 198, 197, 196);
            this._context.BoundUdpClient.Send(destination, boundUdpClientSentFirstDatagram.ToBytes());

            this._context.RelayingUdpClient.StageReceivedPacket(source, 10, 9, 8, 7);
            var boundClientExpectedSentSecondDatagram = Datagram.From(source, 10, 9, 8, 7);

            this._context.RelayingUdpClient.Send(source, 100, 99, 98, 97, 96);

            this._context.RunProxy(
                () =>
                {
                    var boundClientSentPackets = this._context.BoundUdpClient.WaitForSentPackets(2)
                        .Select(x => x.Packet).ToList();
                    boundClientSentPackets.Should().HaveCount(2);
                    boundClientSentPackets[0].Should().Equal(boundUdpClientSentFirstDatagram.ToBytes());
                    boundClientSentPackets[1].Should().Equal(boundClientExpectedSentSecondDatagram.ToBytes());
                    A.CallTo(
                            () => this._context.Interceptor.TryInterceptIncomingAsync(
                                A<Datagram>.That.Matches(
                                    x => boundClientExpectedSentSecondDatagram.Header == x.Header
                                         && boundClientExpectedSentSecondDatagram.Payload.SequenceEqual(x.Payload)),
                                this._context.BoundUdpClient))
                        .MustHaveHappened(Repeated.Exactly.Once);

                    var relayClientSentPackets = this._context.RelayingUdpClient.WaitForSentPackets(2)
                        .Select(x => x.Packet).ToList();
                    relayClientSentPackets.Should().HaveCount(2);
                    relayClientSentPackets[0].Should().Equal(100, 99, 98, 97, 96);
                    relayClientSentPackets[1].Should().Equal(1, 2, 3, 4);
                    A.CallTo(
                            () => this._context.Interceptor.TryInterceptOutgoingAsync(
                                A<ArraySegment<byte>>.That.Matches(
                                    x => new byte[] { 1, 2, 3, 4 }.SequenceEqual(x)),
                                destination,
                                this._context.RelayingUdpClient))
                        .MustHaveHappened(Repeated.Exactly.Once);
                });
        }

        [Fact]
        public void Proxy_reassembles_incoming_datagrams()
        {
            var destination = FakeEndPoints.CreateRemote();

            var boundUdpClientStagedReceivedDatagram1 = new Datagram(
                new DatagramHeader(1, false, destination),
                new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 }));
            var boundUdpClientStagedReceivedDatagram2 = new Datagram(
                new DatagramHeader(2, true, destination),
                new ArraySegment<byte>(new byte[] { 5, 6, 7, 8 }));
            this._context.BoundUdpClient.StageReceivedPacket(
                destination,
                boundUdpClientStagedReceivedDatagram1.ToBytes());
            this._context.BoundUdpClient.StageReceivedPacket(
                destination,
                boundUdpClientStagedReceivedDatagram2.ToBytes());

            this._context.RunProxy(
                () =>
                {
                    var relayClientSentPackets = this._context.RelayingUdpClient.WaitForSentPackets(1)
                        .Select(x => x.Packet).ToList();
                    relayClientSentPackets.Should().HaveCount(1);
                    relayClientSentPackets[0].Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);

                    A.CallTo(
                            () => this._context.Interceptor.TryInterceptOutgoingAsync(
                                A<ArraySegment<byte>>.That.Matches(
                                    x => new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }.SequenceEqual(x)),
                                destination,
                                this._context.RelayingUdpClient))
                        .MustHaveHappened(Repeated.Exactly.Once);
                });
        }

        [Fact]
        public void Proxy_terminates_with_client_stream_termination()
        {
            using (var clientStream = new LoopbackTcpStream())
            {
                var boundUdpClient = new FakeUdpClient();
                var relayingUdpClient = new FakeUdpClient();
                var timerFactory = new ManuallyInvokedTimerFactory();
                var bufferPool = new DebugArrayPool<byte>();
                using (var proxy = new UdpProxy(
                    clientStream,
                    new FakeUdpClientFactory(boundUdpClient, relayingUdpClient),
                    PassthroughInterceptor.Instance,
                    bufferPool,
                    timerFactory,
                    new NullLoggerFactory()))
                {
                    var task = proxy.RunAsync(CancellationToken.None);
                    clientStream.Dispose();

                    task.Awaiting(x => x).ShouldThrow<Exception>();
                }
            }
        }

        [Fact]
        public void Proxy_terminates_with_dispose()
        {
            using (var clientStream = new LoopbackTcpStream())
            {
                var boundUdpClient = new FakeUdpClient();
                var relayingUdpClient = new FakeUdpClient();
                var timerFactory = new ManuallyInvokedTimerFactory();
                var bufferPool = new DebugArrayPool<byte>();
                var interceptor = A.Fake<IDatagramInterceptor>();

                using (var proxy = new UdpProxy(
                    clientStream,
                    new FakeUdpClientFactory(boundUdpClient, relayingUdpClient),
                    interceptor,
                    bufferPool,
                    timerFactory,
                    new NullLoggerFactory()))
                {
                    var task = proxy.RunAsync(CancellationToken.None);
                    proxy.Dispose();

                    task.Awaiting(x => x).ShouldThrow<Exception>();
                }

                A.CallTo(() => interceptor.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [Fact]
        public void Proxy_does_NOT_relay_max_UDP_size_incoming_packets()
        {
            var source = FakeEndPoints.CreateLocal();

            // Must send at least 1 packet to learn the destination and unblock the proxy's receive loop.
            this._context.BoundUdpClient.StageReceivedPacket(FakeEndPoints.CreateRemote(), 0);

            this._context.RelayingUdpClient.StageReceivedPacket(
                source,
                SecureRandom.GetBytes(DatagramHeader.MaxUdpSize));
            this._context.RelayingUdpClient.StageReceivedPacket(source, 1, 2, 3, 4);

            this._context.RunProxy(
                () =>
                {
                    var boundClientSentPackets = this._context.BoundUdpClient
                        .WaitForSentPackets(1).Select(x => x.Packet).ToList();
                    boundClientSentPackets.Should().HaveCount(1);
                    boundClientSentPackets[0].Should().Equal(Datagram.From(source, 1, 2, 3, 4).ToBytes());

                    A.CallTo(
                            () => this._context.Interceptor.TryInterceptIncomingAsync(
                                A<Datagram>.That.Matches(x => new byte[] { 1, 2, 3, 4 }.SequenceEqual(x.Payload)),
                                this._context.BoundUdpClient))
                        .MustHaveHappened(Repeated.Exactly.Once);

                    A.CallTo(
                            () => this._context.Interceptor.TryInterceptIncomingAsync(
                                A<Datagram>._,
                                this._context.BoundUdpClient))
                        .MustHaveHappened(Repeated.Exactly.Once);
                });
        }

        [Fact]
        public void Proxy_does_not_relay_if_interceptor_returns_true()
        {
            var source = FakeEndPoints.CreateLocal();
            var destination = FakeEndPoints.CreateRemote();

            int incomingCounter = 0;
            int outgoingCounter = 0;
            A.CallTo(() => this._context.Interceptor.TryInterceptIncomingAsync(A<Datagram>._, A<IUdpClient>._))
                .ReturnsLazily(() => Task.FromResult(Interlocked.Increment(ref incomingCounter) % 2 == 1));
            A.CallTo(
                    () => this._context.Interceptor.TryInterceptOutgoingAsync(
                        A<ArraySegment<byte>>._,
                        A<EndPoint>._,
                        A<IUdpClient>._))
                .ReturnsLazily(() => Task.FromResult(Interlocked.Increment(ref outgoingCounter) % 2 == 1));

            this._context.BoundUdpClient.StageReceivedPacket(
                destination,
                Datagram.From(destination, 10, 9, 8, 7).ToBytes());
            this._context.BoundUdpClient.StageReceivedPacket(
                destination,
                Datagram.From(destination, 110, 99, 88, 77).ToBytes());
            this._context.RelayingUdpClient.StageReceivedPacket(source, 1, 2, 3, 4);
            this._context.RelayingUdpClient.StageReceivedPacket(source, 11, 22, 33, 44);

            this._context.RunProxy(
                () =>
                {
                    var boundClientSentPackets = this._context.BoundUdpClient.WaitForSentPackets(1);
                    boundClientSentPackets.Should().HaveCount(1);
                    boundClientSentPackets[0].Packet.Should().Equal(Datagram.From(source, 11, 22, 33, 44).ToBytes());
                    var relayingClientSentPackets = this._context.RelayingUdpClient.WaitForSentPackets(1);
                    relayingClientSentPackets[0].Packet.Should().Equal(110, 99, 88, 77);
                });
        }

        public void Dispose()
        {
            this._context.Dispose();
        }
    }
}
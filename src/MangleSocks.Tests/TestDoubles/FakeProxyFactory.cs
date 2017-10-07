using System.Buffers;
using System.Net;
using MangleSocks.Core.IO;
using MangleSocks.Core.Server;

namespace MangleSocks.Tests.TestDoubles
{
    class FakeProxyFactory : IProxyFactory
    {
        public bool TcpProxyCreated { get; private set; }
        public bool UdpProxyCreated { get; private set; }

        public IProxy NextTcpProxyToReturn { get; set; }
        public IProxy NextUdpProxyToReturn { get; set; }

        public IProxy CreateTcpProxy(ITcpStream clientStream, EndPoint destinationEndPoint, ArrayPool<byte> bufferPool)
        {
            this.TcpProxyCreated = true;
            return this.NextTcpProxyToReturn ?? new FakeProxy();
        }

        public IProxy CreateUdpProxy(IReadOnlyTcpStream clientStream, ArrayPool<byte> bufferPool)
        {
            this.UdpProxyCreated = true;
            return this.NextUdpProxyToReturn ?? new FakeProxy();
        }
    }
}
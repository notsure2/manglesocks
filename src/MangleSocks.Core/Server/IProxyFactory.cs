using System.Buffers;
using System.Net;
using MangleSocks.Core.IO;

namespace MangleSocks.Core.Server
{
    public interface IProxyFactory
    {
        IProxy CreateTcpProxy(ITcpStream clientStream, EndPoint destinationEndPoint, ArrayPool<byte> bufferPool);
        IProxy CreateUdpProxy(IReadOnlyTcpStream clientStream, ArrayPool<byte> bufferPool);
    }
}
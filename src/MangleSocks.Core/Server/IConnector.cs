using MangleSocks.Core.IO;

namespace MangleSocks.Core.Server
{
    public interface IConnector : ITcpConnector, IUdpClientFactory
    {
    }
}
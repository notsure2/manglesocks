using System.Net;

namespace MangleSocks.Core.IO
{
    public interface IUdpClientFactory
    {
        IBoundUdpClient CreateBoundUdpClient(EndPoint bindEndPoint);
        IUdpClient CreateUdpClient();
    }
}
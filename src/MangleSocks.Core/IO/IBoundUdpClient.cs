using System.Net;

namespace MangleSocks.Core.IO
{
    public interface IBoundUdpClient : IUdpClient
    {
        EndPoint BindEndPoint { get; }
    }
}
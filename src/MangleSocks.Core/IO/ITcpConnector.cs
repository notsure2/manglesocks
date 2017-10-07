using System.Net;
using System.Threading.Tasks;

namespace MangleSocks.Core.IO
{
    public interface ITcpConnector
    {
        Task<ITcpStream> ConnectTcpAsync(EndPoint destinationEndPoint);
    }
}
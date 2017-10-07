using System.Net;

namespace MangleSocks.Core.IO
{
    public interface IReadOnlyTcpStream : IReadOnlyStream
    {
        EndPoint LocalEndPoint { get; }
        EndPoint RemoteEndPoint { get; }
    }
}

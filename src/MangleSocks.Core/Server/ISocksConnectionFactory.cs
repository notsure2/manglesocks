using MangleSocks.Core.IO;

namespace MangleSocks.Core.Server
{
    public interface ISocksConnectionFactory
    {
        ISocksConnection Create(ITcpStream stream);
    }
}
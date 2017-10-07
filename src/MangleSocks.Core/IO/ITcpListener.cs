using System;
using System.Threading.Tasks;

namespace MangleSocks.Core.IO
{
    public interface ITcpListener : IDisposable
    {
        Task<ITcpStream> GetNextClientStreamAsync();
    }
}
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MangleSocks.Core.IO
{
    public interface IUdpClient : IDisposable
    {
        Task<SocketReceiveFromResult> ReceiveAsync(byte[] buffer, int offset, int count, EndPoint remoteEndPoint);
        Task<int> SendAsync(byte[] buffer, int offset, int count, EndPoint destinationEndPoint);
    }
}
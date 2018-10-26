using System;
using System.Net;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;

namespace MangleSocks.Core.Server
{
    public interface IDatagramInterceptor : IDisposable
    { 
        Task<bool> TryInterceptOutgoingAsync(
            ArraySegment<byte> payload,
            EndPoint destinationEndPoint,
            IUdpClient relayClient);

        Task<bool> TryInterceptIncomingAsync(Datagram datagram, IUdpClient boundClient);
    }
}
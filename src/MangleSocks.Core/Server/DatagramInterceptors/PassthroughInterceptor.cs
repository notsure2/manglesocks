using System;
using System.Net;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;
using MangleSocks.Core.Util.Threading.Tasks;

namespace MangleSocks.Core.Server.DatagramInterceptors
{
    public sealed class PassthroughInterceptor : IDatagramInterceptor
    {
        public Task<bool> TryInterceptOutgoingAsync(ArraySegment<byte> payload, EndPoint destinationEndPoint, IUdpClient relayClient)
        {
            return CachedTasks.FalseTask;
        }

        public Task<bool> TryInterceptIncomingAsync(Datagram datagram, IUdpClient boundClient)
        {
            return CachedTasks.FalseTask;
        }

        public void Dispose()
        {
        }
    }
}
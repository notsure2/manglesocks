using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MangleSocks.Core.Server
{
    public interface IProxy : IDisposable
    {
        EndPoint BindEndPoint { get; }
        Task RunAsync(CancellationToken cancellationToken);
    }
}
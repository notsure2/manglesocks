using System.Net;
using MangleSocks.Core.Util.Directory;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Core.Settings
{
    public interface IAppSettings
    {
        IPEndPoint ListenEndPoint { get; }
        ImplDescriptor DatagramInterceptorDescriptor { get; }
        object DatagramInterceptorSettings { get; }
        LogLevel LogLevel { get; }
    }
}
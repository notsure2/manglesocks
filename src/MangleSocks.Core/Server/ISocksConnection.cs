using System;

namespace MangleSocks.Core.Server
{
    public interface ISocksConnection : IDisposable
    {
        void StartHandlingClient();
    }
}
using System;

namespace MangleSocks.Core.Util.Threading
{
    public interface ITimer : IDisposable
    {
        void Start();
        void Stop();
    }
}
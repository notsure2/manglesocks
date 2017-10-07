using System;

namespace MangleSocks.Core.Util.Threading
{
    public interface ITimerFactory
    {
        ITimer Create(TimeSpan interval, Action callback);
    }
}
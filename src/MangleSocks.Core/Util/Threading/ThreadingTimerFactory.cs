using System;

namespace MangleSocks.Core.Util.Threading
{
    class ThreadingTimerFactory : ITimerFactory
    {
        public static readonly ITimerFactory Instance = new ThreadingTimerFactory();

        ThreadingTimerFactory() { }

        public ITimer Create(TimeSpan interval, Action callback)
        {
            return new ThreadingTimer(interval, callback);
        }
    }
}
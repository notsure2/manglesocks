using System;
using MangleSocks.Core.Util.Threading;

namespace MangleSocks.Tests.TestDoubles
{
    class ManuallyInvokedTimerFactory : ITimerFactory
    {
        public ManuallyInvokedTimer LastCreated { get; private set; }

        public ITimer Create(TimeSpan interval, Action callback)
        {
            return this.LastCreated = new ManuallyInvokedTimer(callback);
        }
    }
}
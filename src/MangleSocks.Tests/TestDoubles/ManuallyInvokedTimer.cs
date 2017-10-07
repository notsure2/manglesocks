using System;
using MangleSocks.Core.Util.Threading;

namespace MangleSocks.Tests.TestDoubles
{
    class ManuallyInvokedTimer : ITimer
    {
        readonly Action _callback;

        public ManuallyInvokedTimer(Action callback)
        {
            this._callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void InvokeCallback()
        {
            this._callback();
        }

        public void Dispose()
        {
        }
    }
}
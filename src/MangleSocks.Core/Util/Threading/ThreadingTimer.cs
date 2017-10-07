using System;
using System.Threading;

namespace MangleSocks.Core.Util.Threading
{
    class ThreadingTimer : ITimer
    {
        readonly TimeSpan _interval;
        readonly Action _callback;
        readonly Timer _timer;

        public ThreadingTimer(TimeSpan interval, Action callback)
        {
            this._interval = interval;
            this._callback = callback ?? throw new ArgumentNullException(nameof(callback));
            this._timer = new Timer(this.TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        void TimerCallback(object state)
        {
            this._callback();
        }

        public void Start()
        {
            this._timer.Change(this._interval, this._interval);
        }

        public void Stop()
        {
            this._timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            this._timer.Dispose();
        }
    }
}
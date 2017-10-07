using System;

namespace MangleSocks.Core.Util.Threading
{
    static class TimerExtensions
    {
        public static void Reset(this ITimer timer)
        {
            if (timer == null) throw new ArgumentNullException(nameof(timer));
            timer.Stop();
            timer.Start();
        }
    }
}
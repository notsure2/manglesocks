using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MangleSocks.Core.Server;

namespace MangleSocks.Tests.TestDoubles
{
    class FakeProxy : IProxy
    {
        readonly ManualResetEventSlim _runAsyncCalledWaitHandle;

        public EndPoint BindEndPoint { get; } = FakeEndPoints.CreateLocal();
        public bool IsDisposed { get; private set; }

        public FakeProxy()
        {
            this._runAsyncCalledWaitHandle = new ManualResetEventSlim(false);
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            this._runAsyncCalledWaitHandle.Set();
            return Task.CompletedTask;
        }

        public void WaitForRunAsyncCall(int timeoutSeconds = 5)
        {
            this._runAsyncCalledWaitHandle.Wait(TimeSpan.FromSeconds(timeoutSeconds));
        }

        public void Dispose()
        {
            this.IsDisposed = true;
        }
    }
}
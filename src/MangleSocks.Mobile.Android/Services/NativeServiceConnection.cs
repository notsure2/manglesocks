using System;
using Android.Content;
using Android.OS;
using JavaObject = Java.Lang.Object;

namespace MangleSocks.Mobile.Droid.Services
{
    class NativeServiceConnection : JavaObject, IServiceConnection
    {
        public event EventHandler<INativeService> ServiceConnected;

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            var binder = (NativeServiceBinder)service;
            this.ServiceConnected?.Invoke(this, binder.Service);
        }

        public void OnServiceDisconnected(ComponentName name)
        {
        }
    }
}
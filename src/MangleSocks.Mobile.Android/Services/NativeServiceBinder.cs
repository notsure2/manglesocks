using System;
using Android.OS;

namespace MangleSocks.Mobile.Droid.Services
{
    class NativeServiceBinder : Binder
    {
        public INativeService Service { get; }

        public NativeServiceBinder(INativeService service)
        {
            this.Service = service ?? throw new ArgumentNullException(nameof(service));
        }
    }
}
using System;
using MangleSocks.Core.Settings;
using MangleSocks.Mobile.Bootstrap;
using Serilog;

namespace MangleSocks.Mobile.Droid
{
    static class ServiceConfiguration
    {
        public static IServiceProvider CreateServiceProvider(IAppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return MobileServiceConfiguration.CreateServiceProvider(
                settings,
                config => config.WriteTo.AndroidLog(outputTemplate: "{Scope} {Message}{NewLine}{Exception}"));
        }
    }
}
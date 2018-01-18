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
                loggerConfiguration =>
                {
                    loggerConfiguration
                        .WriteTo.AndroidLog(
                            outputTemplate: "[{Level:u3} [{SourceContext}]{Scope} {Message}{NewLine}{Exception}");
                });
        }
    }
}
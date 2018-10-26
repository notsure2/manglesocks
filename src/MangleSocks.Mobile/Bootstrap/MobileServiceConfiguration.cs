using System;
using System.Net;
using MangleSocks.Core.Bootstrap;
using MangleSocks.Core.Server;
using MangleSocks.Core.Server.DatagramInterceptors;
using MangleSocks.Mobile.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MangleSocks.Mobile.Bootstrap
{
    public static class MobileServiceConfiguration
    {
        public static IServiceProvider CreateServiceProvider(
            AppSettingsModel settings,
            Action<global::Serilog.LoggerConfiguration> loggerConfigureAction)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var loggerFactory = LoggerConfiguration.CreateLoggerFactory(settings.LogLevel, loggerConfigureAction);
            return ClientServiceConfiguration.CreateServiceProvider(
                new IPEndPoint(IPAddress.Loopback, settings.ListenPort),
                loggerFactory,
                serviceCollection =>
                {
                    switch (settings.Mode)
                    {
                        case ClientMode.Simple:
                            serviceCollection.AddTransient<IDatagramInterceptor, PassthroughInterceptor>();
                            return;

                        case ClientMode.UdpRandomFirstSessionPrefix:
                            if (!(settings.DatagramInterceptorSettings is RandomFirstSessionPrefixInterceptor.Settings
                                interceptorSettings))
                            {
                                interceptorSettings = new RandomFirstSessionPrefixInterceptor.Settings();
                                settings.DatagramInterceptorSettings = interceptorSettings;
                            }

                            serviceCollection.AddSingleton(interceptorSettings);
                            serviceCollection.AddTransient<IDatagramInterceptor, RandomFirstSessionPrefixInterceptor>();
                            return;

                        default:
                            throw new NotSupportedException("Unsupported mode: " + settings.Mode);
                    }
                });
        }
    }
}
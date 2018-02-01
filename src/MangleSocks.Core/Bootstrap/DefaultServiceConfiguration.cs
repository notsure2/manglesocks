using System;
using System.Buffers;
using MangleSocks.Core.IO;
using MangleSocks.Core.Server;
using MangleSocks.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Core.Bootstrap
{
    public static class DefaultServiceConfiguration
    {
        public static IServiceProvider CreateServiceProvider(
            IAppSettings settings,
            ILoggerFactory loggerFactory,
            Action<IServiceCollection> configureAction = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            var collection = new ServiceCollection();
            collection.AddSingleton(ArrayPool<byte>.Shared);
            collection.AddSingleton(loggerFactory);
            collection.AddScoped<ITcpListener>(
                s => new TcpListener(settings.ListenEndPoint, s.GetRequiredService<ILoggerFactory>()));
            collection.AddSingleton<ISocksConnectionFactory, DefaultSocksConnectionFactory>();
            collection.AddSingleton<IConnector, DefaultConnector>();
            collection.AddSingleton<ITcpConnector>(s => s.GetRequiredService<IConnector>());
            collection.AddSingleton<IUdpClientFactory>(s => s.GetRequiredService<IConnector>());
            collection.AddTransient(
                s =>
                {
                    var interceptor = settings.DatagramInterceptorDescriptor.CreateInstance<IDatagramInterceptor>(s);
                    interceptor.ConfigureWith(settings.DatagramInterceptorSettings);
                    return interceptor;
                });
            collection.AddTransient<IProxyFactory, DefaultProxyFactory>();
            collection.AddScoped<SocksServer>();

            configureAction?.Invoke(collection);
            return collection.BuildServiceProvider();
        }
    }
}
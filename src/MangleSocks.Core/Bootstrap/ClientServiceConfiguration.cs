using System;
using System.Buffers;
using System.Net;
using MangleSocks.Core.IO;
using MangleSocks.Core.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Core.Bootstrap
{
    public static class ClientServiceConfiguration
    {
        public static IServiceProvider CreateServiceProvider(
            IPEndPoint listenEndPoint,
            ILoggerFactory loggerFactory,
            Action<IServiceCollection> configureAction = null)
        {
            if (listenEndPoint == null) throw new ArgumentNullException(nameof(listenEndPoint));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            var collection = new ServiceCollection();
            collection.AddSingleton(ArrayPool<byte>.Shared);
            collection.AddSingleton(loggerFactory);
            collection.AddScoped<ITcpListener>(
                s => new TcpListener(listenEndPoint, s.GetRequiredService<ILoggerFactory>()));
            collection.AddSingleton<ISocksConnectionFactory, DefaultSocksConnectionFactory>();
            collection.AddSingleton<IConnector, DefaultConnector>();
            collection.AddSingleton<ITcpConnector>(s => s.GetRequiredService<IConnector>());
            collection.AddSingleton<IUdpClientFactory>(s => s.GetRequiredService<IConnector>());
            collection.AddTransient<IProxyFactory, DefaultProxyFactory>();
            collection.AddScoped<SocksServer>();

            configureAction?.Invoke(collection);
            return collection.BuildServiceProvider();
        }
    }
}
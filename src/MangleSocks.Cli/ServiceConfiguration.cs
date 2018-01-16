using System;
using System.Buffers;
using System.Net;
using MangleSocks.Core.IO;
using MangleSocks.Core.Server;
using MangleSocks.Core.Util.Directory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace MangleSocks.Cli
{
    static class ServiceConfiguration
    {
        public static IServiceProvider CreateServiceProvider(
            IPEndPoint listenEndPoint,
            DirectoryDescriptor datagramInterceptorDescriptor,
            object datagramInterceptorSettings,
            LogLevel logLevel)
        {
            if (listenEndPoint == null)
            {
                throw new ArgumentNullException(nameof(listenEndPoint));
            }

            if (datagramInterceptorDescriptor == null)
            {
                throw new ArgumentNullException(nameof(datagramInterceptorDescriptor));
            }

            var collection = new ServiceCollection();
            collection.AddSingleton(ArrayPool<byte>.Shared);
            collection.AddSingleton(CreateLoggerFactory(logLevel));
            collection.AddSingleton<ITcpListener>(
                s => new TcpListener(listenEndPoint, s.GetRequiredService<ILoggerFactory>()));
            collection.AddSingleton<ISocksConnectionFactory, DefaultSocksConnectionFactory>();
            collection.AddSingleton<IConnector, DefaultConnector>();
            collection.AddSingleton<ITcpConnector>(s => s.GetRequiredService<IConnector>());
            collection.AddSingleton<IUdpClientFactory>(s => s.GetRequiredService<IConnector>());
            collection.AddTransient(
                s =>
                {
                    var interceptor = datagramInterceptorDescriptor.CreateInstance<IDatagramInterceptor>(s);
                    interceptor.ConfigureWith(datagramInterceptorSettings);
                    return interceptor;
                });
            collection.AddTransient<IProxyFactory, DefaultProxyFactory>();
            collection.AddSingleton<SocksServer>();

            return collection.BuildServiceProvider();
        }

        static ILoggerFactory CreateLoggerFactory(LogLevel logLevel)
        {
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(ConvertLevel(logLevel))
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:u} {Level:u3}] [{SourceContext}]{Scope} {Message}{NewLine}{Exception}");

            var logger = configuration.CreateLogger();
            var loggerFactory = new LoggerFactory(new[] { new SerilogLoggerProvider(logger, true) });
            return loggerFactory;
        }

        static LogEventLevel ConvertLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                default:
                    return LogEventLevel.Verbose;
            }
        }
    }
}
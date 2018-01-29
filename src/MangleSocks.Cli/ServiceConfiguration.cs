using System;
using MangleSocks.Core.Bootstrap;
using MangleSocks.Core.Settings;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace MangleSocks.Cli
{
    static class ServiceConfiguration
    {
        public static IServiceProvider CreateServiceProvider(IAppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var loggerFactory = CreateLoggerFactory(settings.LogLevel);
            return DefaultServiceConfiguration.CreateServiceProvider(settings, loggerFactory);
        }

        static ILoggerFactory CreateLoggerFactory(LogLevel logLevel)
        {
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(ConvertLevel(logLevel))
                .Enrich.FromLogContext()
                .WriteTo
                .Console(
                    outputTemplate:
                    "[{Timestamp:u} {Level:u3}] [{SourceContext}]{Scope} {Message}{NewLine}{Exception}");

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
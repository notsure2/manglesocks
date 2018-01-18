using System;
using MangleSocks.Core.Bootstrap;
using MangleSocks.Core.Settings;
using MangleSocks.Mobile.Serilog;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Xamarin.Forms;

namespace MangleSocks.Mobile.Bootstrap
{
    public static class MobileServiceConfiguration
    {
        public static IServiceProvider CreateServiceProvider(
            IAppSettings settings,
            Action<LoggerConfiguration> loggerConfigurationAction)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var loggerFactory = CreateLoggerFactory(settings.LogLevel, loggerConfigurationAction);
            return DefaultServiceConfiguration.CreateServiceProvider(settings, loggerFactory);
        }

        static ILoggerFactory CreateLoggerFactory(LogLevel logLevel, Action<LoggerConfiguration> configureAction)
        {
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(ConvertLevel(logLevel))
                .Enrich.FromLogContext()
                .Enrich.WithProperty(Constants.SourceContextPropertyName, "MangleSocks")
                .WriteTo.Sink(
                    new MessageSink(
                        "[{SourceContext}]{Scope} {Message}{NewLine}{Exception}",
                        MessagingCenter.Instance));

            configureAction?.Invoke(configuration);

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
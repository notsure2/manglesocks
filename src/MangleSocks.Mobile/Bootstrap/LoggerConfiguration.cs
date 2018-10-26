using System;
using MangleSocks.Mobile.Serilog;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Xamarin.Forms;

namespace MangleSocks.Mobile.Bootstrap
{
    static class LoggerConfiguration
    {
        public static ILoggerFactory CreateLoggerFactory(
            LogLevel logLevel,
            Action<global::Serilog.LoggerConfiguration> configureAction)
        {
            var configuration = new global::Serilog.LoggerConfiguration()
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
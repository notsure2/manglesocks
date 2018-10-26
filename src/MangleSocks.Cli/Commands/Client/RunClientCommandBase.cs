using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using MangleSocks.Cli.Bootstrap;
using MangleSocks.Core.Bootstrap;
using MangleSocks.Core.IO;
using MangleSocks.Core.Server;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Cli.Commands.Client
{
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    abstract class RunClientCommandBase
    {
        [Option(ValueName = "ENDPOINT", LongName = "listen")]
        public IPEndPoint ListenEndPoint { get; set; } = new IPEndPoint(IPAddress.Loopback, TcpListener.DefaultPort);

        [Option(ValueName = "LEVEL")]
        public LogLevel Verbosity { get; set; } = LogLevel.Information;

        protected IServiceProvider CreateServiceProvider()
        {
            var loggerFactory = LoggerConfiguration.CreateLoggerFactory(this.Verbosity);
            return ClientServiceConfiguration.CreateServiceProvider(
                this.ListenEndPoint,
                loggerFactory,
                this.ConfigureServices);
        }

        protected abstract void ConfigureServices(IServiceCollection serviceCollection);

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void OnExecute(IConsole console)
        {
            var serviceProvider = this.CreateServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(this.GetType().Name);

                logger.LogInformation("Starting version " + Program.Version);
                logger.LogInformation("** Press CTRL+C to shutdown.");

                using (var server = scope.ServiceProvider.GetRequiredService<SocksServer>())
                {
                    server.Start();

                    var waitHandle = new ManualResetEventSlim(false);
                    console.CancelKeyPress += delegate
                    {
                        console.WriteLine("- CTRL+C pressed. Shutting down...");
                        waitHandle.Set();
                    };
                    waitHandle.Wait();
                }
            }
        }
    }
}
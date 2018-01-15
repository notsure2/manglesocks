using System;
using System.Reflection;
using System.Threading;
using MangleSocks.Core.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace MangleSocks.Cli
{
    static class Program
    {
        static readonly string s_Version = typeof(Program).Assembly
                                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                               .InformationalVersion ?? "<unknown>";

        static int Main(string[] cliArgs)
        {
            try
            {
                Console.WriteLine(
                    "** MangleSocks - A TCP/UDP SOCKS5 proxy with stream transformation. Version {0}",
                    s_Version);
                Console.WriteLine();

                var args = new Arguments();
                args.PopulateFrom(cliArgs);

                if (args.ShowHelp)
                {
                    args.WriteUsageOptions(Console.Out);
                    return -2;
                }

                var serviceProvider = ServiceConfiguration.CreateServiceProvider(
                    args.ListenEndPoint,
                    args.DatagramInterceptorDescriptor,
                    args.DatagramInterceptorSettings,
                    args.LogLevel);

                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(typeof(Program).Name);

                logger.LogInformation("Starting version " + s_Version);
                logger.LogInformation("** Press CTRL+C to shutdown.");
                args.LogProperties(logger);

                using (var server = serviceProvider.GetRequiredService<SocksServer>())
                {
                    server.Start();

                    var waitHandle = new ManualResetEventSlim(false);
                    Console.TreatControlCAsInput = false;
                    Console.CancelKeyPress += delegate
                    {
                        Console.WriteLine("- CTRL+C pressed. Shutting down...");
                        waitHandle.Set();
                    };
                    waitHandle.Wait();
                }

                return 0;
            }
            catch (OptionException ex)
            {
                Console.Error.WriteLine("Invalid arguments: " + ex.Message);
                Console.Error.WriteLine("Run with -h for help.");
                return -2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unhandled exception: {0}", ex);
                return -1;
            }
        }
    }
}

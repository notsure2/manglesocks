using System;
using System.Reflection;
using MangleSocks.Cli.CommandLine.Conventions;
using MangleSocks.Cli.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace MangleSocks.Cli
{
    static class Program
    {
        public static readonly string Version = typeof(Program).Assembly
                                                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                    .InformationalVersion ?? "<unknown>";

        static int Main(string[] cliArgs)
        {
            try
            {
                Console.WriteLine(
                    "** MangleSocks - A TCP/UDP proxy with stream transformation. Version {0}",
                    Version);
                Console.WriteLine("** https://github.com/notsure2/manglesocks");
                Console.WriteLine("** License: https://opensource.org/licenses/MIT");
                Console.WriteLine();

                var app = new CommandLineApplication<RootCommand>();
                app.Conventions.UseDefaultConventions();
                app.Conventions.AddConvention(new EnrichOptionsConvention());
                return app.Execute(cliArgs);
            }
            catch (CommandParsingException ex)
            {
                Console.Error.WriteLine(ex.Message);
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

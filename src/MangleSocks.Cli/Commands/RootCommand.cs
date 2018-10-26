using System.Diagnostics.CodeAnalysis;
using MangleSocks.Cli.CommandLine.Conventions;
using McMaster.Extensions.CommandLineUtils;

namespace MangleSocks.Cli.Commands
{
    [Command]
    [HelpOption(Inherited = true)]
    [Subcommand("client", typeof(ClientCommand))]
    [IPEndPointValueParserConvention]
    class RootCommand
    {
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
        }
    }
}
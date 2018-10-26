using System.Diagnostics.CodeAnalysis;
using MangleSocks.Cli.Commands.Client;
using McMaster.Extensions.CommandLineUtils;

namespace MangleSocks.Cli.Commands
{
    [Command]
    [Subcommand("simple", typeof(SimpleCommand))]
    [Subcommand("udp-random-first-session-prefix", typeof(UdpRandomFirstSessionPrefixCommand))]
    class ClientCommand
    {
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
        }
    }
}
using MangleSocks.Core.Server;
using MangleSocks.Core.Server.DatagramInterceptors;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace MangleSocks.Cli.Commands.Client
{
    [Command]
    class SimpleCommand : RunClientCommandBase
    {
        protected override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IDatagramInterceptor, PassthroughInterceptor>();
        }
    }
}
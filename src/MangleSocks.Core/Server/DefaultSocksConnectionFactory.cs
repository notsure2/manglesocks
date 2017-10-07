using System;
using MangleSocks.Core.IO;
using Microsoft.Extensions.DependencyInjection;

namespace MangleSocks.Core.Server
{
    public class DefaultSocksConnectionFactory : ISocksConnectionFactory
    {
        readonly IServiceProvider _serviceProvider;

        public DefaultSocksConnectionFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public ISocksConnection Create(ITcpStream stream)
        {
            return ActivatorUtilities.CreateInstance<SocksConnection>(this._serviceProvider, stream);
        }
    }
}
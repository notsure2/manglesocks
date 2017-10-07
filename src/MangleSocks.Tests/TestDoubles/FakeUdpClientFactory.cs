using System;
using System.Net;
using MangleSocks.Core.IO;

namespace MangleSocks.Tests.TestDoubles
{
    class FakeUdpClientFactory : IUdpClientFactory
    {
        readonly IBoundUdpClient _stagedBoundUdpClient;
        readonly IUdpClient _stagedUdpClient;

        public FakeUdpClientFactory(IBoundUdpClient stagedBoundClient, IUdpClient stagedClient)
        {
            this._stagedBoundUdpClient = stagedBoundClient
                                         ?? throw new ArgumentNullException(nameof(stagedBoundClient));
            this._stagedUdpClient = stagedClient ?? throw new ArgumentNullException(nameof(stagedClient));
        }

        public IBoundUdpClient CreateBoundUdpClient(EndPoint bindEndPoint)
        {
            return this._stagedBoundUdpClient;
        }

        public IUdpClient CreateUdpClient()
        {
            return this._stagedUdpClient;
        }
    }
}
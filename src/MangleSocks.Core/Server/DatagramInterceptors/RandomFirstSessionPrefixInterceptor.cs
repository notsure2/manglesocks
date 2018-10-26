using System;
using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using MangleSocks.Core.DataAnnotations;
using MangleSocks.Core.IO;
using MangleSocks.Core.Socks;
using MangleSocks.Core.Util;
using MangleSocks.Core.Util.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MangleSocks.Core.Server.DatagramInterceptors
{
    public sealed class RandomFirstSessionPrefixInterceptor : IDatagramInterceptor
    {
        readonly ArrayPool<byte> _bufferPool;
        readonly Settings _settings;
        readonly ILogger _log;
        readonly Random _random;

        bool _randomPacketsSent;

        public RandomFirstSessionPrefixInterceptor(ArrayPool<byte> bufferPool, Settings settings, ILoggerFactory loggerFactory)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            this._bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            this._log = loggerFactory.CreateLogger(this.GetType().Name);
            this._random = CryptoRandom.Instance;

            Validator.ValidateObject(settings, new ValidationContext(settings));
            this._settings = settings;
        }

        public async Task<bool> TryInterceptOutgoingAsync(
            ArraySegment<byte> payload,
            EndPoint destinationEndPoint,
            IUdpClient relayClient)
        {
            if (payload.Count == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(payload));
            if (destinationEndPoint == null) throw new ArgumentNullException(nameof(destinationEndPoint));
            if (relayClient == null) throw new ArgumentNullException(nameof(relayClient));

            if (!this._randomPacketsSent)
            {
                await this.SendRandomPacketsAsync(destinationEndPoint, relayClient).ConfigureAwait(false);
                this._randomPacketsSent = true;
            }

            return false;
        }

        async Task SendRandomPacketsAsync(EndPoint destinationEndPoint, IUdpClient relayClient)
        {
            var settings = this._settings;

            var randomPacketCount = this._random.Next(
                settings.CountMin,
                settings.CountMax);

            var randomPacketBuffer = this._bufferPool.Rent(settings.BytesMax);
            try
            {
                for (var i = 0; i < randomPacketCount; i++)
                {
                    this._random.NextBytes(randomPacketBuffer);

                    var randomSize = this._random.Next(
                        settings.BytesMin,
                        settings.BytesMax);

                    var randomDelay = this._random.Next(
                        settings.DelayMsMin,
                        settings.DelayMsMax);

                    await Task.Delay(randomDelay).ConfigureAwait(false);

                    this._log.LogDebug(
                        "Sending random packet {0}/{1}: {2} bytes delayed {3}ms",
                        i + 1,
                        randomPacketCount,
                        randomSize,
                        randomDelay);

                    await relayClient
                        .SendAsync(randomPacketBuffer, 0, randomSize, destinationEndPoint)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                this._bufferPool.Return(randomPacketBuffer);
            }
        }

        public Task<bool> TryInterceptIncomingAsync(Datagram datagram, IUdpClient boundClient)
        {
            return CachedTasks.FalseTask;
        }

        public void Dispose()
        {
        }

        [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
        public class Settings
        {
            [Range(0, 100)]
            [GreaterThanOrEqualTo(nameof(CountMin))]
            public int CountMax { get; set; } = 23;

            [Range(0, 100)]
            public int CountMin { get; set; } = 17;

            [Range(0, 10000)]
            [GreaterThanOrEqualTo(nameof(DelayMsMin))]
            public int DelayMsMax { get; set; } = 250;

            [Range(0, 10000)]
            public int DelayMsMin { get; set; } = 50;

            [Range(0, 65536)]
            [GreaterThanOrEqualTo(nameof(BytesMin))]
            public int BytesMax { get; set; } = 1017;

            [Range(0, 65536)]
            public int BytesMin { get; set; } = 481;
        }
    }
}
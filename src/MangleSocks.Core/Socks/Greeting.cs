using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using MangleSocks.Core.IO;

namespace MangleSocks.Core.Socks
{
    public struct Greeting
    {
        public AuthenticationMethod[] SupportedAuthenticationMethods { get; set; }

        public static async Task<Greeting> ReadFromAsync(IReadOnlyStream stream, ArrayPool<byte> bufferPool)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferPool == null) throw new ArgumentNullException(nameof(bufferPool));

            // |VER | NMETHODS | METHODS  |
            // | 1  |    1     | 1 to 255 |
            var buffer = bufferPool.Rent(256);
            try
            {
                await stream.ReadExactlyAsync(buffer, 0, 3).ConfigureAwait(false);

                if (buffer[0] != 5)
                {
                    throw new InvalidDataException($"Invalid SOCKS version in greeting: {buffer[0]}");
                }

                var supportedAuthenticationMethodCount = buffer[1];
                if (supportedAuthenticationMethodCount < 1)
                {
                    throw new InvalidDataException("No authentication methods supported.");
                }

                var greeting = new Greeting
                {
                    SupportedAuthenticationMethods = new AuthenticationMethod[supportedAuthenticationMethodCount]
                };

                greeting.SupportedAuthenticationMethods[0] = (AuthenticationMethod)buffer[2];
                if (supportedAuthenticationMethodCount > 1)
                {
                    stream.ReadExactly(buffer, 0, supportedAuthenticationMethodCount - 1);

                    for (int i = 0; i < supportedAuthenticationMethodCount - 1; i++)
                    {
                        greeting.SupportedAuthenticationMethods[i + 1] = (AuthenticationMethod)buffer[i];
                    }
                }

                return greeting;
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }
    }
}
 
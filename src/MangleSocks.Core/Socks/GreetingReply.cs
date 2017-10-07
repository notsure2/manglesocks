using System;
using System.Buffers;
using System.Threading.Tasks;
using MangleSocks.Core.IO;

namespace MangleSocks.Core.Socks
{
    public struct GreetingReply
    {
        public AuthenticationMethod SelectedAuthenticationMethod { get; set; }

        public GreetingReply(AuthenticationMethod selectedAuthenticationMethod)
        {
            this.SelectedAuthenticationMethod = selectedAuthenticationMethod;
        }

        public async Task WriteToAsync(IWriteOnlyStream stream, ArrayPool<byte> bufferPool)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferPool == null) throw new ArgumentNullException(nameof(bufferPool));

            // | VER | METHOD |
            // |  1  |    1   |
            var buffer = bufferPool.Rent(2);
            try
            {
                buffer[0] = 5;
                buffer[1] = (byte)this.SelectedAuthenticationMethod;
                await stream.WriteAsync(buffer, 0, 2).ConfigureAwait(false);
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }
    }
}
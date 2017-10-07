using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using MangleSocks.Core.Util;

namespace MangleSocks.Core.Socks
{
    public struct CommandReply
    {
        public CommandReplyType ReplyType { get; }
        public EndPoint BindEndPoint { get; }

        public CommandReply(CommandReplyType replyType, EndPoint bindEndPoint)
        {
            this.ReplyType = replyType;
            this.BindEndPoint = bindEndPoint ?? throw new ArgumentNullException(nameof(bindEndPoint));
        }

        public async Task WriteToAsync(IWriteOnlyStream stream, ArrayPool<byte> bufferPool)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferPool == null) throw new ArgumentNullException(nameof(bufferPool));

            // | VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
            // |  1  | 1   | X'00' | 1    | Variable | 2        |
            var buffer = bufferPool.Rent(4 + 1 + byte.MaxValue + sizeof(ushort));
            try
            {
                buffer[0] = 5;
                buffer[1] = (byte)this.ReplyType;
                buffer[2] = 0;

                var bindEndPoint = this.BindEndPoint ?? new IPEndPoint(new IPAddress(0L), 0);
                int endPointBytesWritten = bindEndPoint.ToBytes(buffer, 3);

                await stream.WriteAsync(buffer, 0, 3 + endPointBytesWritten).ConfigureAwait(false);
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }
    }
}
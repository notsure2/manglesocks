using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MangleSocks.Core.IO;
using MangleSocks.Core.Util;

namespace MangleSocks.Core.Socks
{
    public struct Command
    {
        public CommandType CommandType { get; }
        public EndPoint EndPoint { get; }

        public Command(CommandType commandType, EndPoint endPoint)
        {
            this.CommandType = commandType;
            this.EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        public static async Task<Command> ReadFromAsync(IReadOnlyStream stream, ArrayPool<byte> bufferPool)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferPool == null) throw new ArgumentNullException(nameof(bufferPool));

            // | VER | CMD | RSV   | ATYP | DST.ADDR | DST.PORT |
            // | 1   | 1   | X'00' | 1    | Variable | 2        |
            var buffer = bufferPool.Rent(byte.MaxValue + sizeof(ushort));
            try
            {
                await stream.ReadExactlyAsync(buffer, 0, 4).ConfigureAwait(false);

                if (buffer[0] != 5)
                {
                    throw new InvalidDataException($"Invalid SOCKS version in greeting: {buffer[0]}");
                }

                var commandType = (CommandType)buffer[1];
                if (commandType != CommandType.Bind 
                    && commandType != CommandType.Connect
                    && commandType != CommandType.UdpAssociate)
                {
                    throw new ProtocolException(
                        $"Invalid command type: {commandType}",
                        CommandReplyType.CommandNotSupported);
                }

                if (buffer[2] != 0)
                {
                    throw new InvalidDataException("Invalid command.");
                }

                var addressType = (AddressType)buffer[3];
                var endPoint = stream.ReadEndPoint(addressType, bufferPool);
                return new Command(commandType, endPoint);
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }
    }
}
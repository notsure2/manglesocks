using System;
using System.Runtime.Serialization;

namespace MangleSocks.Core.Socks
{
    [Serializable]
    public class ProtocolException : Exception
    {
        public CommandReplyType ErrorCode { get; }

        public ProtocolException(CommandReplyType errorCode = CommandReplyType.GeneralFailure) : this(
            $"SOCKS error: {errorCode}",
            errorCode) { }

        public ProtocolException(string message, CommandReplyType errorCode, Exception inner = null) : base(message, inner)
        {
            this.ErrorCode = errorCode;
        }

        protected ProtocolException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}
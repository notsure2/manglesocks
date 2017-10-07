namespace MangleSocks.Core.Socks
{
    public enum CommandReplyType : byte
    {
        Succeeded = 0,
        GeneralFailure = 1,
        ConnectionNotAllowedByRuleset = 2,
        NetworkUnreachable = 3,
        HostUnreachable = 4,
        ConnectionRefused = 5,
        TtlExpired = 6,
        CommandNotSupported = 7,
        AddressTypeNotSupported = 8
    }
}
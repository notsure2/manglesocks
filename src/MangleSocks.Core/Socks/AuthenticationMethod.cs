namespace MangleSocks.Core.Socks
{
    public enum AuthenticationMethod : byte
    {
        None = 0,
        GssApi = 1,
        UsernamePassword = 2,
        NoAcceptableMethods = 255
    }
}
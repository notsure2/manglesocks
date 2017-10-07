using MangleSocks.Core.Util;

namespace MangleSocks.Tests.Helpers
{
    static class SecureRandom
    {
        public static byte[] GetBytes(int count)
        {
            var bytes = new byte[count];
            CryptoRandom.Instance.NextBytes(bytes);
            return bytes;
        }
    }
}
using System;
using System.Net;
using System.Security.Cryptography;

namespace MangleSocks.Tests.TestDoubles
{
    static class FakeEndPoints
    {
        static readonly RandomNumberGenerator s_Random = RandomNumberGenerator.Create();

        public static IPEndPoint CreateLocal()
        {
            return new IPEndPoint(IPAddress.Loopback, GetRandomUInt16());
        }

        public static EndPoint CreateRemote()
        {
            var address = $"{GetRandomByte()}.{GetRandomByte()}.{GetRandomByte()}.{GetRandomByte()}";
            return new IPEndPoint(IPAddress.Parse(address), GetRandomUInt16());
        }

        static byte GetRandomByte()
        {
            var byteArray = new byte[1];
            s_Random.GetNonZeroBytes(byteArray);
            var value = byteArray[0];
            if (value == byte.MaxValue)
            {
                value--;
            }
            return value;
        }

        static ushort GetRandomUInt16()
        {
            var portBytes = new byte[2];
            s_Random.GetNonZeroBytes(portBytes);
            var value = BitConverter.ToUInt16(portBytes, 0);
            if (value == ushort.MaxValue)
            {
                value--;
            }
            return value;
        }
    }
}
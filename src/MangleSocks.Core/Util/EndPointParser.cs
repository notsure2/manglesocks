using System;
using System.Linq;
using System.Net;

namespace MangleSocks.Core.Util
{
    public static class IPEndPointParser
    {
        public static IPEndPoint Parse(string endPointExpression, ushort defaultPort)
        {
            if (string.IsNullOrWhiteSpace(endPointExpression))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(endPointExpression));
            }

            if (defaultPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultPort));
            }

            string addressExpression;
            ushort port;

            int indexOfLastColon = endPointExpression.LastIndexOf(':');
            if (indexOfLastColon == -1)
            {
                addressExpression = endPointExpression;
                port = defaultPort;
            }
            else
            {
                addressExpression = endPointExpression.Substring(0, indexOfLastColon);
                if (!ushort.TryParse(endPointExpression.Substring(indexOfLastColon + 1), out port)
                    || port > IPEndPoint.MaxPort)
                {
                    throw new ArgumentOutOfRangeException(nameof(endPointExpression), "Invalid port number");
                }
            }

            if (IPAddress.TryParse(addressExpression, out var ipAddress))
            {
                return new IPEndPoint(ipAddress, port);
            }

            var address = Dns.GetHostEntry(addressExpression).AddressList.FirstOrDefault()
                          ?? throw new ArgumentException($"Failed to resolve IP address for '{addressExpression}'");
            return new IPEndPoint(address, port);
        }
    }
}
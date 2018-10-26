using System;
using System.Globalization;
using System.Net;
using MangleSocks.Core.Util;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace MangleSocks.Cli.CommandLine.ValueParsers
{
    class IPEndPointValueParser : IValueParser<IPEndPoint>
    {
        public static readonly IValueParser<IPEndPoint> Instance = new IPEndPointValueParser();

        IPEndPointValueParser() { }

        public Type TargetType { get; } = typeof(IPEndPoint);

        object IValueParser.Parse(string argName, string value, CultureInfo culture)
        {
            return this.Parse(argName, value, culture);
        }

        public IPEndPoint Parse(string argName, string value, CultureInfo culture)
        {
            try
            {
                return IPEndPointParser.Parse(value, 0);
            }
            catch (Exception ex)
            {
                throw new FormatException(ex.Message);
            }
        }
    }
}
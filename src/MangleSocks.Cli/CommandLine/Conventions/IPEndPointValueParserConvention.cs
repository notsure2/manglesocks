using System;
using MangleSocks.Cli.CommandLine.ValueParsers;
using McMaster.Extensions.CommandLineUtils.Conventions;

namespace MangleSocks.Cli.CommandLine.Conventions
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class IPEndPointValueParserConventionAttribute : Attribute, IConvention
    {
        public void Apply(ConventionContext context)
        {
            context.Application.ValueParsers.Add(IPEndPointValueParser.Instance);
        }
    }
}
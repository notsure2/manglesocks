using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using MangleSocks.Core.IO;
using MangleSocks.Core.Server;
using MangleSocks.Core.Util;
using MangleSocks.Core.Util.Directory;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace MangleSocks.Cli
{
    class Arguments
    {
        readonly OptionSet _optionSet;
        readonly IDictionary<string, string> _datagramInterceptorSettingsByName;

        public IPEndPoint ListenEndPoint { get; private set; }
        public DirectoryDescriptor DatagramInterceptorDescriptor { get; private set; }
        public object DatagramInterceptorSettings { get; set; }
        public LogLevel LogLevel { get; private set; }
        public bool ShowHelp { get; private set; }

        public Arguments()
        {
            this.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, TcpListener.DefaultPort);
            this.DatagramInterceptorDescriptor = DirectoryDescriptor.GetByNameOrNull<IDatagramInterceptor>("default");
            this.DatagramInterceptorSettings = new Dictionary<string, string>();
            this.LogLevel = LogLevel.Information;
            this._datagramInterceptorSettingsByName = new Dictionary<string, string>();

            this._optionSet = new OptionSet
            {
                {
                    "l|listen=", $"Listen endpoint (default: {this.ListenEndPoint})",
                    x => { this.ListenEndPoint = IPEndPointParser.Parse(x, TcpListener.DefaultPort); }
                },
                {
                    "u|udp=",
                    "UDP proxy mode.\r\nOne of: " + string.Join(
                                                      ", ",
                                                      DirectoryDescriptor.GetAll<IDatagramInterceptor>()
                                                          .Select(x => x.Identifier))
                                                  + "\r\n(use with -h for list of applicable options)",
                    x =>
                    {
                        this.DatagramInterceptorDescriptor =
                            DirectoryDescriptor.GetByNameOrNull<IDatagramInterceptor>(x)
                            ?? throw new OptionException("Invalid UDP proxy mode", "u");
                        this.DatagramInterceptorSettings = this.DatagramInterceptorDescriptor
                            .SettingsDescriptor.CreateInstance();
                    }
                },
                {
                    "U|uo:", "UDP proxy mode options.",
                    (p, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(p))
                        {
                            throw new OptionException(
                                "Invalid UDP proxy mode setting value. Run with -h for help.",
                                "U");
                        }

                        this._datagramInterceptorSettingsByName[p] = v;
                    }
                },
                {
                    "v|verbosity=", "Logging level.\r\nOne of: " + string.Join(", ", Enum.GetNames(typeof(LogLevel))) + "\r\n(default: " + this.LogLevel + ")",
                    (LogLevel v) => this.LogLevel = v
                },
                {
                    "h|help", "Show help information.",
                    v => this.ShowHelp = true
                },
                {
                    "<>", v => throw new OptionException("Invalid parameter. Run with -h for help.", "<>")
                }
            };
        }

        public void PopulateFrom(IEnumerable<string> cliArgs)
        {
            this._optionSet.Parse(cliArgs);

            try
            {
                this.DatagramInterceptorDescriptor.SettingsDescriptor?.SetProperties(
                    this.DatagramInterceptorSettings,
                    this._datagramInterceptorSettingsByName);
            }
            catch (ValidationException ex)
            {
                throw new OptionException(ex.Message, "U");
            }
        }

        public void WriteUsageOptions(TextWriter output)
        {
            output.WriteLine("- Usage: MangleSocks.Cli [arguments]");
            output.WriteLine();
            this._optionSet.WriteOptionDescriptions(output);
            if (this.DatagramInterceptorDescriptor != null)
            {
                output.WriteLine();
                if (this.DatagramInterceptorDescriptor.SettingsDescriptor?.PropertyDescriptorsByName.Count > 0)
                {
                    output.WriteLine(
                        "- Available options for UDP proxy mode '{0}':",
                        this.DatagramInterceptorDescriptor.Identifier);
                    output.WriteLine();
                    foreach (var pair in this.DatagramInterceptorDescriptor.SettingsDescriptor
                        .PropertyDescriptorsByName.OrderBy(x => x.Key))
                    {
                        Console.Write(" ");
                        Console.Write(pair.Key);
                        Console.Write(": " + pair.Value.PropertyType.Name);
                        Console.Write(" (default: ");

                        if (pair.Value.DefaultValue is string defaultValueString
                            && string.IsNullOrWhiteSpace(defaultValueString))
                        {
                            Console.Write("<blank>");
                        }
                        else
                        {
                            Console.Write(pair.Value.DefaultValue);
                        }

                        Console.Write(")");
                        if (!string.IsNullOrWhiteSpace(pair.Value.Description))
                        {
                            Console.Write(" - ");
                            Console.Write(pair.Value.Description);
                        }

                        output.WriteLine();
                    }
                }
                else
                {
                    output.WriteLine(
                        "- UDP proxy mode '{0}' does not have any additional options.",
                        this.DatagramInterceptorDescriptor.Identifier);
                }
            }
        }

        public void LogProperties(ILogger logger)
        {
            logger.LogInformation("Listen on: {0}", this.ListenEndPoint.ToString());
            logger.LogInformation("Datagram interceptor: {0}", this.DatagramInterceptorDescriptor.Type.Name);
            logger.LogInformation("Datagram interceptor settings: {@0}", this.DatagramInterceptorSettings);
            logger.LogInformation("Log level: {0}", this.LogLevel);
        }
    }
}
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MangleSocks.Core.Server;
using MangleSocks.Core.Server.DatagramInterceptors;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace MangleSocks.Cli.Commands.Client
{
    [Command]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    class UdpRandomFirstSessionPrefixCommand : RunClientCommandBase
    {
        readonly RandomFirstSessionPrefixInterceptor.Settings _settings =
            new RandomFirstSessionPrefixInterceptor.Settings();

        [Option(ShortName = "", ValueName = "NUMBER")]
        [Description("Minimum number of random packets to send")]
        public int CountMin
        {
            get => this._settings.CountMin;
            set => this._settings.CountMin = value;
        }

        [Option(ShortName = "", ValueName = "NUMBER")]
        [Description("Maximum number of random packets to send")]
        public int CountMax
        {
            get => this._settings.CountMax;
            set => this._settings.CountMax = value;
        }

        [Option(ShortName = "", ValueName = "MS")]
        [Description("Minimum delay between random packets (in milliseconds)")]
        public int DelayMsMin
        {
            get => this._settings.DelayMsMin;
            set => this._settings.DelayMsMin = value;
        }

        [Option(ShortName = "", ValueName = "MS")]
        [Description("Maximum delay between random packets (in milliseconds)")]
        public int DelayMsMax
        {
            get => this._settings.DelayMsMax;
            set => this._settings.DelayMsMax = value;
        }

        [Option(ShortName = "", ValueName = "BYTES")]
        [Description("Minimum random packet size (in bytes)")]
        public int BytesMin
        {
            get => this._settings.BytesMin;
            set => this._settings.BytesMin = value;
        }

        [Option(ShortName = "", ValueName = "BYTES")]
        [Description("Maximum random packet size (in bytes)")]
        public int BytesMax
        {
            get => this._settings.BytesMax;
            set => this._settings.BytesMax = value;
        }

        protected override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(this._settings);
            serviceCollection.AddTransient<IDatagramInterceptor, RandomFirstSessionPrefixInterceptor>();
        }

        public ValidationResult OnValidate()
        {
            var validationResults = new List<ValidationResult>();
            return Validator.TryValidateObject(
                this._settings,
                new ValidationContext(this._settings),
                validationResults,
                true)
                ? ValidationResult.Success
                : validationResults.First();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using MangleSocks.Core.Server;
using MangleSocks.Core.Util.Directory;
using MangleSocks.Mobile.Models;
using Microsoft.Extensions.Logging;
using Plugin.Settings.Abstractions;

namespace MangleSocks.Mobile.ViewModels
{
    public class ConfigureViewModel
    {
        public AppSettingsModel AppSettings { get; }
        public IList<LogLevel> LogLevels { get; }
        public IList<string> DatagramInterceptorNames { get; }

        public ConfigureViewModel(ISettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            this.AppSettings = AppSettingsModel.LoadFrom(settings);
            this.LogLevels = (LogLevel[])Enum.GetValues(typeof(LogLevel));
            this.DatagramInterceptorNames = ImplDescriptor.GetAll<IDatagramInterceptor>()
                .Select(x => x.Identifier)
                .ToList();
        }
    }
}
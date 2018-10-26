using System;
using System.Collections.Generic;
using MangleSocks.Mobile.Models;
using Microsoft.Extensions.Logging;
using Plugin.Settings.Abstractions;

namespace MangleSocks.Mobile.ViewModels
{
    public class ConfigureViewModel
    {
        public AppSettingsModel AppSettings { get; }
        public IList<LogLevel> LogLevels { get; }
        public IList<ClientMode> ClientModes { get; }

        public ConfigureViewModel(ISettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            this.AppSettings = AppSettingsModel.LoadFrom(settings);
            this.LogLevels = (LogLevel[])Enum.GetValues(typeof(LogLevel));
            this.ClientModes = (ClientMode[])Enum.GetValues(typeof(ClientMode));
        }
    }
}
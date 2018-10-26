using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Plugin.Settings.Abstractions;

namespace MangleSocks.Mobile.Models
{
    public class AppSettingsModel
    {
        public ushort ListenPort { get; set; } = 1081;

        public ClientMode Mode { get; set; } = ClientMode.Simple;

        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public object DatagramInterceptorSettings { get; set; }

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public static AppSettingsModel LoadFrom(ISettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var json = settings.GetValueOrDefault(nameof(MangleSocks), "null");
            return JsonConvert.DeserializeObject<AppSettingsModel>(json) ?? new AppSettingsModel();
        }

        public void SaveTo(ISettings settings)
        {
            Validator.ValidateObject(this, new ValidationContext(this), true);
            if (this.DatagramInterceptorSettings != null)
            {
                Validator.ValidateObject(
                    this.DatagramInterceptorSettings,
                    new ValidationContext(this.DatagramInterceptorSettings),
                    true);
            }

            var json = JsonConvert.SerializeObject(this);
            settings.AddOrUpdateValue(nameof(MangleSocks), json);
        }
    }
}
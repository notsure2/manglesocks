using System.Net;
using MangleSocks.Core.Settings;
using MangleSocks.Core.Util.Directory;
using MangleSocks.Mobile.Models;
using Microsoft.Extensions.Logging;
using Plugin.Settings.Abstractions;

namespace MangleSocks.Mobile
{
    public class AppSettings : IAppSettings
    {
        public IPEndPoint ListenEndPoint { get; set; }
        public ImplDescriptor DatagramInterceptorDescriptor { get; set; }
        public object DatagramInterceptorSettings { get; set; }
        public LogLevel LogLevel { get; set; }

        public static IAppSettings Get(ISettings settings)
        {
            return AppSettingsModel.LoadFrom(settings).ToAppSettings();
        }
    }
}
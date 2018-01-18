using System;
using MangleSocks.Mobile.Pages;
using Plugin.Settings;
using Plugin.Settings.Abstractions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MangleSocks.Mobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class App
    {
        public static readonly ISettings Settings = CrossSettings.IsSupported
            ? CrossSettings.Current
            : throw new NotSupportedException("Settings are not supported on this platform");

        public App()
        {
            this.InitializeComponent();
            this.MainPage = new NavigationPage(new Main(MessagingCenter.Instance, Settings));
        }

        protected override void OnStart() { }

        protected override void OnSleep() { }

        protected override void OnResume() { }
    }
}

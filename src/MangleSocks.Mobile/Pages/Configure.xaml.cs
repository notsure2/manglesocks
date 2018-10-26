using System;
using System.ComponentModel.DataAnnotations;
using MangleSocks.Core.Server.DatagramInterceptors;
using MangleSocks.Mobile.Forms;
using MangleSocks.Mobile.Models;
using MangleSocks.Mobile.ViewModels;
using Plugin.Settings.Abstractions;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace MangleSocks.Mobile.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Configure
    {
        readonly ISettings _settings;

        public ConfigureViewModel ViewModel { get; }

        public Configure(ISettings settings)
        {
            this._settings = settings ?? throw new ArgumentNullException(nameof(settings));

            this.ViewModel = new ConfigureViewModel(settings);
            this.InitializeComponent();

            this.LogLevelPicker.SelectedIndexChanged += UpdatePickerMeasurement;
            this.ModePicker.SelectedIndexChanged += UpdatePickerMeasurement;
        }

        static void UpdatePickerMeasurement(object sender, EventArgs args)
        {
            ((Picker)sender).InvalidateMeasureNonVirtual(InvalidationTrigger.MeasureChanged);
        }

        void HandleModePickerSelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            var selectedMode = (ClientMode)picker.SelectedItem;

            this.UdpInterceptorSection.Clear();

            var currentSettings = this.ViewModel.AppSettings.DatagramInterceptorSettings;

            object newSettingsInstance;
            switch (selectedMode)
            {
                case ClientMode.UdpRandomFirstSessionPrefix:
                    newSettingsInstance = new RandomFirstSessionPrefixInterceptor.Settings();
                    break;

                default:
                    newSettingsInstance = null;
                    break;
            }

            if (newSettingsInstance == null)
            {
                currentSettings = this.ViewModel.AppSettings.DatagramInterceptorSettings = null;
            }
            else if (currentSettings == null || currentSettings.GetType() != newSettingsInstance.GetType())
            {
                currentSettings = this.ViewModel.AppSettings.DatagramInterceptorSettings = newSettingsInstance;
            }

            if (currentSettings != null)
            {
                foreach (var section in TableSectionGenerator.GenerateSettingsFormCells(currentSettings))
                {
                    this.UdpInterceptorSection.Add(section);
                }
            }

            if (this.UdpInterceptorSection.Count == 0)
            {
                this.UdpInterceptorSection.Add((Cell)this.Settings.Resources["NoSettingsAvailableCell"]);
            }
        }

        void SaveSettingsAndGoBack(object sender, EventArgs e)
        {
            try
            {
                this.ViewModel.AppSettings.SaveTo(this._settings);
                this.Navigation.PopAsync(true);
            }
            catch (ValidationException ex)
            {
                this.DisplayAlert("Error", ex.Message, "Try again");
            }
        }
    }
}
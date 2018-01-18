using System;
using MangleSocks.Mobile.Messaging;
using Plugin.Settings.Abstractions;
using Serilog.Events;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MangleSocks.Mobile.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Main
    {
        readonly IMessagingCenter _messagingCenter;
        readonly ISettings _settings;

        public static readonly BindableProperty StatusProperty = BindableProperty.Create(
            nameof(Status),
            typeof(ServiceStatus),
            typeof(Main),
            ServiceStatus.Stopped);

        public static readonly BindableProperty ListenEndPointProperty = BindableProperty.Create(
            nameof(ListenEndPoint),
            typeof(string),
            typeof(Main),
            "(unknown)");

        public ServiceStatus Status
        {
            get => (ServiceStatus)this.GetValue(StatusProperty);
            set => this.SetValue(StatusProperty, value);
        }

        public string ListenEndPoint
        {
            get => (string)this.GetValue(ListenEndPointProperty);
            set => this.SetValue(ListenEndPointProperty, value);
        }

		public Main(IMessagingCenter messagingCenter, ISettings settings)
		{
		    this._messagingCenter = messagingCenter ?? throw new ArgumentNullException(nameof(messagingCenter));
		    this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
		    this.InitializeComponent();
		}

	    void NavigateToConfigurePage(object sender, EventArgs e)
	    {
	        this.Navigation.PushAsync(new Configure(this._settings), true);
	    }

        void HandleTriggerButtonClicked(object sender, EventArgs e)
        {
            bool isStartRequest = this.Status != ServiceStatus.Started;

            if (isStartRequest)
            {
                this.LogMessages.Children.Clear();
                this.ConfigureButton.IsEnabled = false;
            }

            this._messagingCenter.Send(
                Application.Current,
                nameof(ServiceActionRequest),
                new ServiceActionRequest { IsStartRequest = isStartRequest });
        }

        protected override void OnAppearing()
        {
            this.ListenEndPoint = AppSettings.Get(this._settings).ListenEndPoint.ToString();

            this._messagingCenter.Subscribe<Application, ServiceStatusUpdate>(
                this,
                nameof(ServiceStatusUpdate),
                (_, update) => Device.BeginInvokeOnMainThread(() =>
                {
                    this.Status = update.Status;
                    this.ConfigureButton.IsEnabled = update.Status == ServiceStatus.Stopped;
                }));

            this._messagingCenter.Subscribe<Application, ServiceLogMessage>(
                this,
                nameof(ServiceLogMessage),
                (_, log) => Device.BeginInvokeOnMainThread(
                    () => this.LogMessages.Children.Add(
                        new Label
                        {
                            Text = log.Message,
                            TextColor = LogLevelToColor(log.Severity)
                        })));

            this._messagingCenter.Send(
                Application.Current,
                nameof(ServiceStatusUpdateRequest),
                new ServiceStatusUpdateRequest());

            base.OnAppearing();
        }

        static Color LogLevelToColor(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    return Color.DarkGray;

                case LogEventLevel.Debug:
                    return Color.Gray;

                case LogEventLevel.Information:
                    return Color.Black;

                case LogEventLevel.Warning:
                    return Color.OrangeRed;

                case LogEventLevel.Error:
                    return Color.Red;

                case LogEventLevel.Fatal:
                    return Color.DarkRed;

                default:
                    return Color.Black;
            }
        }

        protected override void OnDisappearing()
        {
            this._messagingCenter.Unsubscribe<Application, ServiceStatusUpdate>(this, nameof(ServiceStatusUpdate));
            this._messagingCenter.Unsubscribe<Application, ServiceLogMessage>(this, nameof(ServiceLogMessage));
            base.OnDisappearing();
        }
    }
}

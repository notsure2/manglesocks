using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MangleSocks.Mobile.Messaging;
using Plugin.Settings.Abstractions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MangleSocks.Mobile.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Main
    {
        public static readonly BindableProperty LogMessagesProperty = BindableProperty.Create(
            nameof(LogMessages),
            typeof(ObservableCollection<ServiceLogMessage>),
            typeof(Main));

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

        readonly IMessagingCenter _messagingCenter;
        readonly ISettings _settings;
        bool _autoscrollLogMessages;
        ServiceLogMessage _lastLogMessage;

        public ObservableCollection<ServiceLogMessage> LogMessages
        {
            get => (ObservableCollection<ServiceLogMessage>)this.GetValue(LogMessagesProperty);
            set => this.SetValue(LogMessagesProperty, value);
        }

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

        protected override void OnAppearing()
        {
            this.ListenEndPoint = AppSettings.Get(this._settings).ListenEndPoint.ToString();

            this._messagingCenter.Subscribe<Application, ServiceStatusUpdate>(
                this,
                nameof(ServiceStatusUpdate),
                (_, update) => Device.BeginInvokeOnMainThread(
                    () =>
                    {
                        this.Status = update.Status;
                        this.ConfigureButton.IsEnabled = update.Status == ServiceStatus.Stopped;

                        if (this.LogMessages != update.LogMessages)
                        {
                            if (this.LogMessages != null)
                            {
                                this.LogMessages.CollectionChanged -= this.HandleLogMessagesCollectionChanged;
                            }

                            this.LogMessages = update.LogMessages;
                            this._autoscrollLogMessages = true;
                            this.LogMessages.CollectionChanged += this.HandleLogMessagesCollectionChanged;
                        }
                    }));

            this._messagingCenter.Send(
                Application.Current,
                nameof(ServiceStatusUpdateRequest),
                new ServiceStatusUpdateRequest());

            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            this._messagingCenter.Unsubscribe<Application, ServiceStatusUpdate>(this, nameof(ServiceStatusUpdate));
            base.OnDisappearing();
        }

        void NavigateToConfigurePage(object sender, EventArgs e)
	    {
	        this.Navigation.PushAsync(new Configure(this._settings), true);
	    }

        void NavigateToAboutPage(object sender, EventArgs e)
        {
            this.Navigation.PushAsync(new About(), true);
        }

        void HandleTriggerButtonClicked(object sender, EventArgs e)
        {
            bool isStartRequest = this.Status != ServiceStatus.Started;

            if (isStartRequest)
            {
                this.ConfigureButton.IsEnabled = false;
            }

            this._messagingCenter.Send(
                Application.Current,
                nameof(ServiceActionRequest),
                new ServiceActionRequest { IsStartRequest = isStartRequest });
        }

        void HandleListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ((ListView)sender).SelectedItem = null;
        }

        void HandleLogMessagesCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (!this._autoscrollLogMessages
                || notifyCollectionChangedEventArgs.Action != NotifyCollectionChangedAction.Add)
            {
                return;
            }

            var collection = (ObservableCollection<ServiceLogMessage>)sender;
            if (collection.Count == 0)
            {
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                var newItem = (ServiceLogMessage)notifyCollectionChangedEventArgs.NewItems[0];
                this._lastLogMessage = newItem;
                this.LogMessageListView.ScrollTo(newItem, ScrollToPosition.End, true);
            });
        }

        void HandleLogMessagesListViewItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            if (this._lastLogMessage != null && e.Item == this._lastLogMessage)
            {
                this._autoscrollLogMessages = true;
            }
        }

        void HandleLogMessagesListViewItemDisappearing(object sender, ItemVisibilityEventArgs e)
        {
            if (this._lastLogMessage != null && e.Item == this._lastLogMessage)
            {
                this._autoscrollLogMessages = false;
            }
        }
    }
}

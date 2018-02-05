using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using MangleSocks.Mobile.Droid.Services;
using MangleSocks.Mobile.Messaging;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using FormsApplication = Xamarin.Forms.Application;

namespace MangleSocks.Mobile.Droid
{
    [Activity(
        Name = "org.manglesocks.main_activity",
        Label = "MangleSocks",
        Icon = "@drawable/ic_shortcut_icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleInstance,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            Xamarin.Forms.Forms.Init(this, bundle);

            var serviceIntent = new Intent(this, typeof(NativeService));

            MessagingCenter.Instance.Subscribe<FormsApplication, ServiceActionRequest>(
                this,
                nameof(ServiceActionRequest),
                (_, request) =>
                {
                    if (request.IsStartRequest)
                    {
                        this.StartService(serviceIntent);
                    }
                    else
                    {
                        this.StopService(serviceIntent);
                    }
                });

            MessagingCenter.Instance.Subscribe<FormsApplication, ServiceStatusUpdateRequest>(
                this,
                nameof(ServiceStatusUpdateRequest),
                (_, __) =>
                {
                    void OnServiceConnected(object sender, INativeService service)
                    {
                        var localConnection = (NativeServiceConnection)sender;
                        service.NotifyStatusUpdate();
                        localConnection.ServiceConnected -= OnServiceConnected;
                        this.UnbindService(localConnection);
                    }

                    var connection = new NativeServiceConnection();
                    connection.ServiceConnected += OnServiceConnected;
                    this.BindService(serviceIntent, connection, Bind.AutoCreate);
                });

            this.LoadApplication(new App());
        }

        protected override void OnDestroy()
        {
            MessagingCenter.Instance.Unsubscribe<FormsApplication, ServiceActionRequest>(
                this,
                nameof(ServiceActionRequest));
            base.OnDestroy();
        }
    }
}

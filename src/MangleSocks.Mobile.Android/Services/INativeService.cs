using MangleSocks.Mobile.Messaging;

namespace MangleSocks.Mobile.Droid.Services
{
    interface INativeService
    {
        ServiceStatus Status { get; }
    }
}
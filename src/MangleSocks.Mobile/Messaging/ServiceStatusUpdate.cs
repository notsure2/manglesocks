using System.Collections.ObjectModel;

namespace MangleSocks.Mobile.Messaging
{
    public class ServiceStatusUpdate
    {
        public ServiceStatus Status { get; set; }
        public ObservableCollection<ServiceLogMessage> LogMessages { get; set; }
    }
}
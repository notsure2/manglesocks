using Serilog.Events;

namespace MangleSocks.Mobile.Messaging
{
    public class ServiceLogMessage
    {
        public LogEventLevel Severity { get; }
        public string Message { get; }

        public ServiceLogMessage(LogEventLevel severity, string message)
        {
            this.Severity = severity;
            this.Message = message;
        }
    }
}
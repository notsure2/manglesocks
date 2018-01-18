using System;
using System.Globalization;
using MangleSocks.Mobile.Messaging;
using Xamarin.Forms;

namespace MangleSocks.Mobile.Pages.ValueConverters
{
    class ServiceStatusToTriggerButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (ServiceStatus)value;
            switch (status)
            {
                case ServiceStatus.Stopped:
                    return "Start";
                default:
                    return "Stop";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
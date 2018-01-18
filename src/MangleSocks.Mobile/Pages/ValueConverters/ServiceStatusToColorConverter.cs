using System;
using System.Globalization;
using MangleSocks.Mobile.Messaging;
using Xamarin.Forms;

namespace MangleSocks.Mobile.Pages.ValueConverters
{
    class ServiceStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (ServiceStatus)value;
            switch (status)
            {
                case ServiceStatus.Started:
                    return Color.Green;

                default:
                    return Color.Red;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
using System;
using System.Globalization;
using Serilog.Events;
using Xamarin.Forms;

namespace MangleSocks.Mobile.Pages.ValueConverters
{
    public class LogEventLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var level = (LogEventLevel)value;
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
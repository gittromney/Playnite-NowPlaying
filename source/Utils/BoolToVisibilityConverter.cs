using System;
using System.Globalization;
using System.Windows.Data;

namespace NowPlaying.Utils
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string trueVisibility = "Visible";
            string falseVisibility = "Collapsed";

            // Optional parameter is tuple: (trueVisibility, falseVisibility)
            if (parameter is Tuple<string, string>)
            {
                var visibilities = (Tuple<string, string>)parameter;
                trueVisibility = visibilities.Item1;
                falseVisibility = visibilities.Item2;
            }

            // Optional parameter is string: falseVisibility
            else if (parameter is string)
            {
                falseVisibility = (string)parameter;
            }

            return (value is bool && (bool)value) ? trueVisibility : falseVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;

namespace NowPlaying.Utils
{
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double trueOpacity = 1.0;
            double falseOpacity = 0.5;

            // Optional parameter is tuple: (trueOpacity, falseOpacity)
            if (parameter is Tuple<double, double>)
            {
                var opacities = (Tuple<double, double>)parameter;
                trueOpacity = opacities.Item1;
                falseOpacity = opacities.Item2;
            }

            // Optional parameter is double: falseOpacity
            else if (parameter is double)
            {
                falseOpacity = (double)parameter;
            }

            return (value is bool && (bool)value) ? trueOpacity : falseOpacity;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;

namespace NowPlaying.Utils
{
    public class BoolInvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? false : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? false : true;
        }
    }
}

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NowPlaying.Utils
{
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object trueValue, CultureInfo culture)
        {
            if (value != null && value.GetType().IsEnum)
                return (Enum.Equals(value, trueValue));
            else
                return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object trueValue, CultureInfo culture)
        {
            if (value is bool && (bool)value)
                return trueValue;
            else
                return DependencyProperty.UnsetValue;
        }
    }
}

﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace NowPlaying.Utils
{
    /// <summary>
    /// Code taken from https://stackoverflow.com/questions/356194/datatrigger-where-value-is-not-null
    /// </summary>
    public class IsNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }
}

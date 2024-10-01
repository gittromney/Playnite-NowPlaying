using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace NowPlaying.Utils
{
    /// <summary>
    /// Based on code from https://stackoverflow.com/questions/5649875/how-to-make-the-border-trim-the-child-elements
    /// </summary>
    public class BorderClipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 && values[0] is double && values[1] is double && values[2] is CornerRadius)
            {
                var width = (double)values[0];
                var height = (double)values[1];

                if (width < 4 || height < 4)
                {
                    return Geometry.Empty;
                }

                var radius = (CornerRadius)values[2];

                // . inset margin of 2 pixels used to achieve clean corner clipping 
                var clip = new RectangleGeometry(new Rect(2, 2, width-4, height-4), radius.TopLeft, radius.TopLeft);
                clip.Freeze();

                return clip;
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

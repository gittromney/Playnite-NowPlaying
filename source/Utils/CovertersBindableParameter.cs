using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace NowPlaying.Utils
{
    public class IndexedMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && parameter is int)
            {
                var index = (int)parameter;
                return index < values.Length ? values[index] : values[0];
            }
            else
            {
                return values[0];
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// IMultiValueConverter with bindable ConverterParameter.
    /// Based on ConverterBindableParameter by Pascalsz (see below)
    /// </summary>
    [ContentProperty(nameof(MultiBinding))]
    public class MultiConverterBindableParameter : MarkupExtension
    {
        #region Public Properties

        public MultiBinding MultiBinding { get; set; }
        public BindingMode Mode { get; set; }
        public IMultiValueConverter Converter { get; set; }
        public Binding ConverterParameter { get; set; }

        #endregion

        public MultiConverterBindableParameter()
        { }

        public MultiConverterBindableParameter(string path)
        {
            throw new NotImplementedException();
        }

        public MultiConverterBindableParameter(MultiBinding multiBinding)
        {
            MultiBinding = multiBinding;
        }

        #region Overridden Methods

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var multiBinding = MultiBinding;
            if (ConverterParameter != null)
            {
                ConverterParameter.Mode = BindingMode.OneWay;
                multiBinding.Bindings.Add(ConverterParameter);
                multiBinding.Converter = new MultiValueConverterAdapter(Converter);
            }
            return multiBinding.ProvideValue(serviceProvider);
        }

        #endregion

        [ContentProperty(nameof(Converter))]
        private class MultiValueConverterAdapter : IMultiValueConverter
        {
            private readonly IMultiValueConverter Converter;

            public MultiValueConverterAdapter(IMultiValueConverter converter)
            {
                Converter = converter;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (Converter == null || values.Length < 2) return values[0]; // Required for VS design-time
                return Converter.Convert(values.Take(values.Length - 1).ToArray(), targetType, values.Last(), culture);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }


    /// <summary>
    /// Converter with bindable parameter by Author=Pascalsz, 
    /// found here: https://stackoverflow.com/questions/15309008/binding-converterparameter
    /// </summary>
    [ContentProperty(nameof(Binding))]
    public class ConverterBindableParameter : MarkupExtension
    {
        #region Public Properties

        public Binding Binding { get; set; }
        public BindingMode Mode { get; set; }
        public IValueConverter Converter { get; set; }
        public Binding ConverterParameter { get; set; }

        #endregion

        public ConverterBindableParameter()
        { }

        public ConverterBindableParameter(string path)
        {
            Binding = new Binding(path);
        }

        public ConverterBindableParameter(Binding binding)
        {
            Binding = binding;
        }

        #region Overridden Methods

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var multiBinding = new MultiBinding();
            Binding.Mode = Mode;
            multiBinding.Bindings.Add(Binding);
            if (ConverterParameter != null)
            {
                ConverterParameter.Mode = BindingMode.OneWay;
                multiBinding.Bindings.Add(ConverterParameter);
            }
            var adapter = new MultiValueConverterAdapter
            {
                Converter = Converter
            };
            multiBinding.Converter = adapter;
            return multiBinding.ProvideValue(serviceProvider);
        }

        #endregion


        [ContentProperty(nameof(Converter))]
        private class MultiValueConverterAdapter : IMultiValueConverter
        {
            public IValueConverter Converter { get; set; }

            private object lastParameter;

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (Converter == null) return values[0]; // Required for VS design-time
                if (values.Length > 1) lastParameter = values[1];
                return Converter.Convert(values[0], targetType, lastParameter, culture);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                if (Converter == null) return new object[] { value }; // Required for VS design-time

                return new object[] { Converter.ConvertBack(value, targetTypes[0], lastParameter, culture) };
            }
        }
    }
}

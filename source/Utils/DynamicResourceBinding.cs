//
// CODE SOURCE: author=Gor Rustamyan, url=https://stackoverflow.com/questions/20564862/binding-to-resource-key-wpf
// -> modified to add support for up to N bindings per target
//
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NowPlaying.Utils
{

    // This implementation allows each target framework element (e.g. ProgressBar) to use up to a maximum of (N=5)
    // dynamic resource bindings, where N is the number of proxy binder properties defined below ("First", "Second",...).
    // For example, if setting a given target's Foreground and Background with dynamic resource bindings, N must be >= 2;
    // Example XAML using two dynamic resource bindings:
    //
    // <ProgressBar
    //    Value = "{Binding PercentDone, Mode=OneWay}"
    //    Foreground = "{utils:DynamicResourceBinding ProgressBarForeground}"
    //    Background = "{utils:DynamicResourceBinding ProgressBarBackground}"/>
    //
    // The above effectively works as if you could type the following (which isn't allowed):
    //    Foreground = "{DynamicResource {Binding ProgressBarForeground}}"
    //    Background = "{DynamicResource {Binding ProgressBarBackground}}"/>
    //
    public class DynamicResourceBinding : MarkupExtension
    {
        #region Proxy binder properties

        public static object GetFirstProperty(DependencyObject obj) { return (object)obj.GetValue(FirstProperty); }
        public static void SetFirstProperty(DependencyObject obj, object value) { obj.SetValue(FirstProperty, value); }

        public static readonly DependencyProperty FirstProperty = DependencyProperty.RegisterAttached(
            "First", typeof(object), typeof(DynamicResourceBinding), new PropertyMetadata(null, ResourceKeyChanged)
        );

        public static object GetSecondProperty(DependencyObject obj) { return (object)obj.GetValue(SecondProperty); }
        public static void SetSecondProperty(DependencyObject obj, object value) { obj.SetValue(SecondProperty, value); }

        public static readonly DependencyProperty SecondProperty = DependencyProperty.RegisterAttached(
            "Second", typeof(object), typeof(DynamicResourceBinding), new PropertyMetadata(null, ResourceKeyChanged)
        );

        public static object GetThirdProperty(DependencyObject obj) { return (object)obj.GetValue(ThirdProperty); }
        public static void SetThirdProperty(DependencyObject obj, object value) { obj.SetValue(ThirdProperty, value); }

        public static readonly DependencyProperty ThirdProperty = DependencyProperty.RegisterAttached(
            "Third", typeof(object), typeof(DynamicResourceBinding), new PropertyMetadata(null, ResourceKeyChanged)
        );

        public static object GetFourthProperty(DependencyObject obj) { return (object)obj.GetValue(FourthProperty); }
        public static void SetFourthProperty(DependencyObject obj, object value) { obj.SetValue(FourthProperty, value); }

        public static readonly DependencyProperty FourthProperty = DependencyProperty.RegisterAttached(
            "Fourth", typeof(object), typeof(DynamicResourceBinding), new PropertyMetadata(null, ResourceKeyChanged)
        );

        public static object GetFifthProperty(DependencyObject obj) { return (object)obj.GetValue(FifthProperty); }
        public static void SetFifthProperty(DependencyObject obj, object value) { obj.SetValue(FifthProperty, value); }

        public static readonly DependencyProperty FifthProperty = DependencyProperty.RegisterAttached(
            "Fifth", typeof(object), typeof(DynamicResourceBinding), new PropertyMetadata(null, ResourceKeyChanged)
        );


        static void ResourceKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as FrameworkElement;
            var newVal = e.NewValue as Tuple<object, DependencyProperty>;

            if (target == null || newVal == null)
                return;

            var dp = newVal.Item2;

            if (newVal.Item1 == null)
            {
                target.SetValue(dp, dp.GetMetadata(target).DefaultValue);
                return;
            }

            target.SetResourceReference(dp, newVal.Item1.ToString());
        }

        #endregion

        public DynamicResourceBinding() { }

        public DynamicResourceBinding(string path)
        {
            this.Path = new PropertyPath(path);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var provideValueTargetService = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
            if (provideValueTargetService == null)
                return null;

            if (provideValueTargetService.TargetObject != null &&
                provideValueTargetService.TargetObject.GetType().FullName == "System.Windows.SharedDp")
                return this;

            var targetObject = provideValueTargetService.TargetObject as FrameworkElement;
            var targetProperty = provideValueTargetService.TargetProperty as DependencyProperty;
            if (targetObject == null || targetProperty == null)
                return null;

            var binding = new Binding()
            {
                Path = this.Path,
                XPath = this.XPath,
                Mode = this.Mode,
                UpdateSourceTrigger = this.UpdateSourceTrigger,
                Converter = this.Converter,
                ConverterParameter = this.ConverterParameter,
                ConverterCulture = this.ConverterCulture,
                FallbackValue = this.FallbackValue
            };
            if (this.RelativeSource != null) binding.RelativeSource = this.RelativeSource;
            if (this.ElementName != null) binding.ElementName = this.ElementName;
            if (this.Source != null) binding.Source = this.Source;

            var multiBinding = new MultiBinding()
            {
                Converter = HelperConverter.Current,
                ConverterParameter = targetProperty,
                NotifyOnSourceUpdated = true
            };
            multiBinding.Bindings.Add(binding);

            // . choose first unbound proxy property from the list
            if (BindingOperations.GetMultiBinding(targetObject, FirstProperty) == null)
            {
                targetObject.SetBinding(FirstProperty, multiBinding);
            }
            else if (BindingOperations.GetMultiBinding(targetObject, SecondProperty) == null)
            {
                targetObject.SetBinding(SecondProperty, multiBinding);
            }
            else if (BindingOperations.GetMultiBinding(targetObject, ThirdProperty) == null)
            {
                targetObject.SetBinding(ThirdProperty, multiBinding);
            }
            else if (BindingOperations.GetMultiBinding(targetObject, FourthProperty) == null)
            {
                targetObject.SetBinding(FourthProperty, multiBinding);
            }
            else if (BindingOperations.GetMultiBinding(targetObject, FifthProperty) == null)
            {
                targetObject.SetBinding(FifthProperty, multiBinding);
            }
            else
            {
                throw new InvalidOperationException("All available proxy binder properties already in use.");
            }
            return null;
        }


        #region Binding Members

        /// <summary> The source path (for CLR bindings).</summary>
        public object Source
        {
            get;
            set;
        }

        /// <summary> The source path (for CLR bindings).</summary>
        public PropertyPath Path
        {
            get;
            set;
        }

        /// <summary> The XPath path (for XML bindings).</summary>
        [DefaultValue(null)]
        public string XPath
        {
            get;
            set;
        }

        /// <summary> Binding mode </summary>
        [DefaultValue(BindingMode.Default)]
        public BindingMode Mode
        {
            get;
            set;
        }

        /// <summary> Update type </summary>
        [DefaultValue(UpdateSourceTrigger.Default)]
        public UpdateSourceTrigger UpdateSourceTrigger
        {
            get;
            set;
        }

        /// <summary> The Converter to apply </summary>
        [DefaultValue(null)]
        public IValueConverter Converter
        {
            get;
            set;
        }

        /// <summary>
        /// The parameter to pass to converter.
        /// </summary>
        /// <value></value>
        [DefaultValue(null)]
        public object ConverterParameter
        {
            get;
            set;
        }

        /// <summary> Culture in which to evaluate the converter </summary>
        [DefaultValue(null)]
        [TypeConverter(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public CultureInfo ConverterCulture
        {
            get;
            set;
        }

        /// <summary>
        /// Description of the object to use as the source, relative to the target element.
        /// </summary>
        [DefaultValue(null)]
        public RelativeSource RelativeSource
        {
            get;
            set;
        }

        /// <summary> Name of the element to use as the source </summary>
        [DefaultValue(null)]
        public string ElementName
        {
            get;
            set;
        }


        #endregion

        #region BindingBase Members

        /// <summary> Value to use when source cannot provide a value </summary>
        /// <remarks>
        ///     Initialized to DependencyProperty.UnsetValue; if FallbackValue is not set, BindingExpression
        ///     will return target property's default when Binding cannot get a real value.
        /// </remarks>
        public object FallbackValue
        {
            get;
            set;
        }

        #endregion



        #region Nested types

        private class HelperConverter : IMultiValueConverter
        {
            public static readonly HelperConverter Current = new HelperConverter();

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                return Tuple.Create(values[0], (DependencyProperty)parameter);
            }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

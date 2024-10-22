//
// Based on, extended from GridViewSort
// -> See https://thomaslevesque.com/2009/08/04/wpf-automatically-sort-a-gridview-continued/
//    w/ modifications by marcibme, via comments (see url at head of file)
// -> Added support for custom sorting of designated column(s)
// -> Added AutoWidth, MinWidth, FixedWidth column resizing
//
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Data;
using System.Collections.Generic;
using System.Windows.Input;
using ListView = System.Windows.Controls.ListView;
using Binding = System.Windows.Data.Binding;
using Control = System.Windows.Controls.Control;
using System;
using System.Windows.Markup;
using System.Globalization;
using System.Windows.Forms;

namespace NowPlaying.Utils
{
    #region column indexed binding

    /// <summary>
    /// Creates a resolved binding for control property within a GridViewColumn.CellTemplate 
    /// that varies depending on the GridView column it belongs to. 
    ///
    /// Takes a binding to a (ViewModel) source property table, which is a column-indexed 
    /// string[] indexed holding the name of the source property to use for each column of 
    /// the GridView.
    /// </summary>
    /// How it works: 
    /// 
    /// Upon first access when the control property's column can be determined, a binding 
    /// is created directly from the control' property to specified ViewModel source property 
    /// for that column. 
    /// 
    /// Options / special cases
    /// . If a source property name of 'null' is given, then no binding will be created for that column.
    ///    I.e. the default value for that control property will be used for column(s) with a 'null'
    ///    source.
    /// . If a source property name is followed by "=DynamicResource", its resolved binding will be 
    ///    equivalent to using NowPlaying.Utils.DynamicResourceBinding.
    ///
    /// Example use case:
    /// 
    /// ViewModel:
    ///    public string[] ColumnIndexedContent => new string[]
    ///    {
    ///            nameof(Device),
    ///            nameof(Directory),
    ///            nameof(SpaceAvailableForCaches),
    ///            nameof(GamesEnabled)
    ///    };
    ///    public string Device {get; set;}
    ///    public string Directory {get; set;}
    ///    public string SpaceAvailableForCaches {get; set;}
    ///    public string GamesEnabled {get; set;}
    ///    
    ///    public string[] ColumnIndexedForeground => new string[]
    ///    {
    ///        nameof(GlyphBrush) + "=DynamicResource",
    ///        nameof(Blue),
    ///        null,   // null == no binding created - use default Foreground brush for this column
    ///        nameof(Green)
    ///    };
    ///    public string Green => "Green";
    ///    public string Blue => "Blue";
    ///    public string GlyphBrush => "GlyphBrush";
    /// 
    /// XAML:
    /// <GridViewColumn>
    ///   <GridViewColumn.CellTemplate>
    ///      ...  
    ///      <TextBlock 
    ///          Text="{utils:GridViewColumnIndexedBinding ColumnIndexedContent}" 
    ///          Foreground="{utils:GridViewColumnIndexedBinding ColumnIndexedForeground}"/>
    ///      ...
    ///   </GridViewColumn.CellTemplate>
    /// </GridViewColumn>
    /// 
    [ContentProperty(nameof(Path))]
    public class GridViewColumnIndexedBinding : MarkupExtension
    {
        #region column index resolving proxy property

        public static object GetIndexer1(DependencyObject obj)
        {
            return (object)obj.GetValue(Indexer1Property);
        }
        public static void SetIndexer1(DependencyObject obj, object value)
        {
            obj.SetValue(Indexer1Property, value);
        }
        public static readonly DependencyProperty Indexer1Property = DependencyProperty.RegisterAttached(
            "Indexer1", typeof(object), typeof(GridViewColumnIndexedBinding), new PropertyMetadata(null)
        );

        public static object GetIndexer2(DependencyObject obj)
        {
            return (object)obj.GetValue(Indexer2Property);
        }
        public static void SetIndexer2(DependencyObject obj, object value)
        {
            obj.SetValue(Indexer2Property, value);
        }
        public static readonly DependencyProperty Indexer2Property = DependencyProperty.RegisterAttached(
            "Indexer2", typeof(object), typeof(GridViewColumnIndexedBinding), new PropertyMetadata(null)
        );

        public static object GetIndexer3(DependencyObject obj)
        {
            return (object)obj.GetValue(Indexer3Property);
        }
        public static void SetIndexer3(DependencyObject obj, object value)
        {
            obj.SetValue(Indexer3Property, value);
        }
        public static readonly DependencyProperty Indexer3Property = DependencyProperty.RegisterAttached(
            "Indexer3", typeof(object), typeof(GridViewColumnIndexedBinding), new PropertyMetadata(null)
        );

        #endregion

        #region DynamicResource proxy properties

        public static object GetDynamic1Property(DependencyObject obj) { return (object)obj.GetValue(Dynamic1Property); }
        public static void SetDynamic1Property(DependencyObject obj, object value) { obj.SetValue(Dynamic1Property, value); }

        public static readonly DependencyProperty Dynamic1Property = DependencyProperty.RegisterAttached(
            "Dynamic1", typeof(object), typeof(GridViewColumnIndexedBinding), new PropertyMetadata(null, ResourceKeyChanged)
        );

        public static object GetDynamic2Property(DependencyObject obj) { return (object)obj.GetValue(Dynamic2Property); }
        public static void SetDynamic2Property(DependencyObject obj, object value) { obj.SetValue(Dynamic2Property, value); }

        public static readonly DependencyProperty Dynamic2Property = DependencyProperty.RegisterAttached(
            "Dynamic2", typeof(object), typeof(GridViewColumnIndexedBinding), new PropertyMetadata(null, ResourceKeyChanged)
        );

        public static object GetDynamic3Property(DependencyObject obj) { return (object)obj.GetValue(Dynamic3Property); }
        public static void SetDynamic3Property(DependencyObject obj, object value) { obj.SetValue(Dynamic3Property, value); }

        public static readonly DependencyProperty Dynamic3Property = DependencyProperty.RegisterAttached(
            "Dynamic3", typeof(object), typeof(GridViewColumnIndexedBinding), new PropertyMetadata(null, ResourceKeyChanged)
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

        #region Optional Binding Members

        /// <summary> The source path (for CLR bindings).</summary>
        public PropertyPath Path
        {
            get;
            set;
        }

        /// <summary> The source path (for CLR bindings).</summary>
        public object Source
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
 
        public GridViewColumnIndexedBinding() { }
        public GridViewColumnIndexedBinding(string path)
        {
            Path = new PropertyPath(path);
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

            var currentValue = targetObject.GetValue(targetProperty);

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

            // . Create a one-use binding from our indexing proxy property.  The adapter will create a target
            //    binding to the column-indexed source (with respect to the targetObject within a GridView).
            //
            var adapterBinding = new MultiBinding()
            {
                Converter = new FirstAccessColumnIndexBinder(targetObject, targetProperty, binding, currentValue),
                NotifyOnSourceUpdated = true
            };
            adapterBinding.Bindings.Add(binding);
            var indexerProxy = GetFirstUnboundIndexerProxy(targetObject);
            targetObject.SetBinding(indexerProxy, adapterBinding); // Proxy <- Source array binding, not yet indexed by column
            return null;
        }

        private static DependencyProperty GetFirstUnboundIndexerProxy(DependencyObject targetObject)
        {
            // . choose first unbound proxy property from the list
            if (BindingOperations.GetMultiBinding(targetObject, Indexer1Property) == null)
            {
                return Indexer1Property;
            }
            else if (BindingOperations.GetMultiBinding(targetObject, Indexer2Property) == null)
            {
                return Indexer2Property;
            }
            else if (BindingOperations.GetMultiBinding(targetObject, Indexer3Property) == null)
            {
                return Indexer3Property;
            }
            else
            {
                throw new InvalidOperationException("All available column indexer proxy properties already in use.");
            }
        }

        private class FirstAccessColumnIndexBinder : IMultiValueConverter
        {
            private readonly FrameworkElement targetObject;
            private readonly DependencyProperty targetProperty;
            private readonly Binding sourceBinding;
            private readonly object currentValue;

            public FirstAccessColumnIndexBinder(FrameworkElement targetObject, DependencyProperty targetProperty, Binding sourceBinding, object currentValue)
            {
                this.targetObject = targetObject;
                this.targetProperty = targetProperty;
                this.sourceBinding = sourceBinding;
                this.currentValue = currentValue;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                // Note: this converter is only be used once per targetProperty; it
                // 1. determines targetObject's column position in the listview, and
                // 2. creates target property binding to the column-indexed scalar Source (e.g. CacheRootsView.Device)
                // 3. removes temporary binding to our Indexing Proxy/this converter.
                //
                var contentPresenter = WpfUtils.GetAncestor<ContentPresenter>(targetObject);
                var rowPresenter = WpfUtils.GetAncestor<GridViewRowPresenter>(contentPresenter);
                var columnIndex = WpfUtils.FindIndexOfChild(rowPresenter, contentPresenter);
                if (columnIndex >= 0)
                {
                    if (values[0] is string[])
                    {
                        var sourcePath = ((string[])values[0])[columnIndex];

                        // Note: if sourcePath==null -- create no binding for this column
                        if (sourcePath != null)
                        {
                            var dynamicResourcePath = GetDynamicResourcePropertyPath(sourcePath);
                            if (dynamicResourcePath != null)
                            {
                                var binding = new Binding()
                                {
                                    Path = new PropertyPath(dynamicResourcePath),
                                    XPath = sourceBinding.XPath,
                                    Mode = sourceBinding.Mode,
                                    UpdateSourceTrigger = sourceBinding.UpdateSourceTrigger,
                                    Converter = sourceBinding.Converter,
                                    ConverterParameter = sourceBinding.ConverterParameter,
                                    ConverterCulture = sourceBinding.ConverterCulture,
                                    FallbackValue = sourceBinding.FallbackValue
                                };
                                if (sourceBinding.RelativeSource != null) binding.RelativeSource = sourceBinding.RelativeSource;
                                if (sourceBinding.ElementName != null) binding.ElementName = sourceBinding.ElementName;
                                if (sourceBinding.Source != null) binding.Source = sourceBinding.Source;

                                var multiBinding = new MultiBinding()
                                {
                                    Converter = DynamicResourceConverter.Current,
                                    ConverterParameter = targetProperty,
                                    NotifyOnSourceUpdated = true
                                };
                                multiBinding.Bindings.Add(binding);
                                var proxyProperty = GetFirstUnboundDynamicResourceProxy(targetObject);
                                targetObject.SetBinding(proxyProperty, multiBinding);
                            }
                            else
                            {
                                var binding = new Binding()
                                {
                                    Path = new PropertyPath(sourcePath),
                                    NotifyOnSourceUpdated = true
                                };
                                targetObject.SetBinding(targetProperty, binding);
                            }
                        }
                        else
                        {
                            // no binding created, return the property's value when ProvideValue was called
                            targetObject.SetValue(targetProperty, currentValue);
                        }
                    }
                }
                return null;
            }

            private string GetDynamicResourcePropertyPath(string sourcePath)
            {
                var index = sourcePath.IndexOf("=DynamicResource");
                if (index > 0) // match after 1st character
                {
                    return sourcePath.Substring(0, index);
                }
                else
                {
                    return null;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        #region DynamicResource helper methods/classes

        private static DependencyProperty GetFirstUnboundDynamicResourceProxy(DependencyObject targetObject)
        {
            // . choose first unbound proxy property from the list
            if (BindingOperations.GetMultiBinding(targetObject, Dynamic1Property) == null)
            {
                return Dynamic1Property;
            }
            else if (BindingOperations.GetMultiBinding(targetObject, Dynamic2Property) == null)
            {
                return Dynamic2Property;
            }
            else if (BindingOperations.GetMultiBinding(targetObject, Dynamic3Property) == null)
            {
                return Dynamic3Property;
            }
            else
            {
                throw new InvalidOperationException("All available DynamicResource proxy properties already in use.");
            }
        }

        private class DynamicResourceConverter : IMultiValueConverter
        {
            public static readonly DynamicResourceConverter Current = new DynamicResourceConverter();

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

    #endregion

    public abstract class IReversableComparer
    {
        public Comparer<object> forwardComparer;
        public Comparer<object> reverseComparer;

        public IReversableComparer()
        {
            this.forwardComparer = Comparer<object>.Create((x, y) => this.Compare(x, y));
            this.reverseComparer = Comparer<object>.Create((x, y) => this.Compare(x, y, reverse: true));

        }
        public abstract int Compare(object x, object y, bool reverse = false);
    }

    public static class GridViewUtils
    {
        #region Scrolling enhancers

        // . Pass (non-shift-modified) mouse wheel events to parent UIElement, targeting an ancestor ScrollViewer
        public static void MouseWheelToParent(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        public static bool GetHorizontalShiftWheelScrollFlipped(DependencyObject obj)
        {
            return (bool)obj.GetValue(HorizontalShiftWheelScrollFlippedProperty);
        }
        public static void SetHorizontalShiftWheelScrollFlipped(DependencyObject obj, bool value)
        {
            obj.SetValue(HorizontalShiftWheelScrollFlippedProperty, value);
        }
        public static readonly DependencyProperty HorizontalShiftWheelScrollFlippedProperty = DependencyProperty.RegisterAttached
        (
            "HorizontalShiftWheelScrollFlipped", typeof(bool), typeof(GridViewUtils), new PropertyMetadata(false)
        );

        public static bool GetHorizontalShiftWheelScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(HorizontalShiftWheelScrollProperty);
        }
        public static void SetHorizontalShiftWheelScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(HorizontalShiftWheelScrollProperty, value);
        }
        public static readonly DependencyProperty HorizontalShiftWheelScrollProperty = DependencyProperty.RegisterAttached
        (
            "HorizontalShiftWheelScroll",
            typeof(bool),
            typeof(GridViewUtils),
            new UIPropertyMetadata
            (
                false,
                (o, e) =>
                {
                    ListView listView = o as ListView;
                    if (listView != null)
                    {
                        bool oldVal = (bool)e.OldValue;
                        bool newVal = (bool)e.NewValue;
                        if (newVal && !oldVal)
                        {
                            listView.PreviewMouseWheel += HorizontalScrollOnShiftWheel;
                        }
                        if (!newVal && oldVal)
                        {
                            listView.PreviewMouseWheel -= HorizontalScrollOnShiftWheel;
                        }
                    }
                }
            )
        );

        private static void HorizontalScrollOnShiftWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                var scrollViewer = WpfUtils.GetChildOfType<ScrollViewer>(sender as ListView);
                if (scrollViewer != null)
                {
                    var flipped = GetHorizontalShiftWheelScrollFlipped(sender as ListView);
                    if (e.Delta < 0)
                    {
                        if (flipped) { scrollViewer.LineLeft(); } else { scrollViewer.LineRight(); }
                    }
                    else
                    {
                        if (flipped) { scrollViewer.LineRight(); } else { scrollViewer.LineLeft(); }
                    }
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region Column auto sorting (AutoSort)

        public static bool GetAutoSort(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoSortProperty);
        }

        public static void SetAutoSort(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoSortProperty, value);
        }

        public static readonly DependencyProperty AutoSortProperty = DependencyProperty.RegisterAttached
        (
            "AutoSort",
            typeof(bool),
            typeof(GridViewUtils),
            new UIPropertyMetadata(
                false,
                (o, e) =>
                {
                    ListView listView = o as ListView;
                    if (listView != null)
                    {
                        bool oldValue = (bool)e.OldValue;
                        bool newValue = (bool)e.NewValue;
                        if (oldValue && !newValue)
                        {
                            listView.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                        }
                        if (!oldValue && newValue)
                        {
                            listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                        }
                    }
                }
            )
        );

        public static string GetSecondaryAutoSortBy(DependencyObject obj)
        {
            return (string)obj.GetValue(SecondaryAutoSortByProperty);
        }
        public static void SetSecondaryAutoSortBy(DependencyObject obj, string value)
        {
            obj.SetValue(SecondaryAutoSortByProperty, value);
        }

        public static readonly DependencyProperty SecondaryAutoSortByProperty = DependencyProperty.RegisterAttached
        (
           "SecondaryAutoSortBy",
           typeof(string),
           typeof(GridViewUtils),
           new UIPropertyMetadata(null)
        );


        public static string GetPropertyName(DependencyObject obj)
        {
            return (string)obj.GetValue(PropertyNameProperty);
        }

        public static void SetPropertyName(DependencyObject obj, string value)
        {
            obj.SetValue(PropertyNameProperty, value);
        }

        public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.RegisterAttached
        (
            "PropertyName",
            typeof(string),
            typeof(GridViewUtils),
            new UIPropertyMetadata(null)
        );


        public static IReversableComparer GetCustomSort(DependencyObject obj)
        {
            return (IReversableComparer)obj.GetValue(CustomSortProperty);
        }

        public static void SetCustomSort(DependencyObject obj, IReversableComparer value)
        {
            obj.SetValue(CustomSortProperty, value);
        }

        public static readonly DependencyProperty CustomSortProperty = DependencyProperty.RegisterAttached
        (
            "CustomSort",
            typeof(IReversableComparer),
            typeof(GridViewUtils),
            new UIPropertyMetadata(null)
        );


        public static string GetSortedByDefault(DependencyObject obj)
        {
            return (string)obj.GetValue(SortedByDefaultProperty);
        }

        public static void SetSortedByDefault(DependencyObject obj, string value)
        {
            obj.SetValue(SortedByDefaultProperty, value);
        }

        public static readonly DependencyProperty SortedByDefaultProperty = DependencyProperty.RegisterAttached
        (
            "SortedByDefault",
            typeof(string),
            typeof(GridViewUtils),
            new UIPropertyMetadata(null)
        );


        public static bool GetShowSortGlyph(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowSortGlyphProperty);
        }

        public static void SetShowSortGlyph(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowSortGlyphProperty, value);
        }

        public static readonly DependencyProperty ShowSortGlyphProperty = DependencyProperty.RegisterAttached
        (
            "ShowSortGlyph",
            typeof(bool),
            typeof(GridViewUtils),
            new UIPropertyMetadata(true)
        );

        public static ImageSource GetSortGlyphAscending(DependencyObject obj)
        {
            return (ImageSource)obj.GetValue(SortGlyphAscendingProperty);
        }

        public static void SetSortGlyphAscending(DependencyObject obj, ImageSource value)
        {
            obj.SetValue(SortGlyphAscendingProperty, value);
        }

        public static readonly DependencyProperty SortGlyphAscendingProperty = DependencyProperty.RegisterAttached
        (
            "SortGlyphAscending",
            typeof(ImageSource),
            typeof(GridViewUtils),
            new UIPropertyMetadata(null)
        );

        public static ImageSource GetSortGlyphDescending(DependencyObject obj)
        {
            return (ImageSource)obj.GetValue(SortGlyphDescendingProperty);
        }

        public static void SetSortGlyphDescending(DependencyObject obj, ImageSource value)
        {
            obj.SetValue(SortGlyphDescendingProperty, value);
        }

        public static readonly DependencyProperty SortGlyphDescendingProperty = DependencyProperty.RegisterAttached
        (
            "SortGlyphDescending",
            typeof(ImageSource),
            typeof(GridViewUtils),
            new UIPropertyMetadata(null)
        );


        public static string GetSortedColumnName(DependencyObject obj)
        {
            return (string)obj.GetValue(SortedColumnNameProperty);
        }

        public static void SetSortedColumnName(DependencyObject obj, string value)
        {
            obj.SetValue(SortedColumnNameProperty, value);
        }

        public static readonly DependencyProperty SortedColumnNameProperty = DependencyProperty.RegisterAttached
        (
            "SortedColumnName",
            typeof(string),
            typeof(GridViewUtils),
            new UIPropertyMetadata(null)
        );

        public static GridViewColumnHeader GetSortedColumnHeader(DependencyObject obj)
        {
            return (GridViewColumnHeader)obj.GetValue(SortedColumnHeaderProperty);
        }

        public static void SetSortedColumnHeader(DependencyObject obj, GridViewColumnHeader value)
        {
            obj.SetValue(SortedColumnHeaderProperty, value);
        }

        public static readonly DependencyProperty SortedColumnHeaderProperty = DependencyProperty.RegisterAttached
        (
            "SortedColumnHeader",
            typeof(GridViewColumnHeader),
            typeof(GridViewUtils),
            new UIPropertyMetadata(null)
        );

        public static ListSortDirection GetSortDirection(DependencyObject obj)
        {
            return (ListSortDirection)obj.GetValue(SortDirectionProperty);
        }

        public static void SetSortDirection(DependencyObject obj, ListSortDirection value)
        {
            obj.SetValue(SortDirectionProperty, value);
        }

        public static readonly DependencyProperty SortDirectionProperty = DependencyProperty.RegisterAttached
        (
            "SortDirectionProperty",
            typeof(ListSortDirection),
            typeof(GridViewUtils),
            new UIPropertyMetadata(null)
        );

        private static void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;

            if (headerClicked != null && headerClicked.Column != null)
            {
                string columnName = GetPropertyName(headerClicked.Column);

                if (string.IsNullOrEmpty(columnName))
                {
                    columnName = GetDisplayMemberBindingPath(headerClicked);
                }

                if (!string.IsNullOrEmpty(columnName))
                {
                    ListView listView = WpfUtils.GetAncestor<ListView>(headerClicked);
                    if (listView != null)
                    {
                        if (GetAutoSort(listView))
                        {
                            ApplySort(listView, headerClicked, columnName);
                        }
                    }
                }
            }
        }

        private static string GetDisplayMemberBindingPath(GridViewColumnHeader headerClicked)
        {
            if (headerClicked.Column.DisplayMemberBinding != null)
            {
                return ((Binding)headerClicked.Column.DisplayMemberBinding).Path.Path;
            }
            return null;
        }

        public static void ApplySort
        (
            ListView listView,
            GridViewColumnHeader headerClicked,
            string columnName
        )
        {
            ICollectionView view = listView.Items;

            var currentSortedColumnHeader = GetSortedColumnHeader(listView);
            if (currentSortedColumnHeader != null)
            {
                RemoveSortGlyph(currentSortedColumnHeader);
            }

            ListSortDirection direction = ListSortDirection.Ascending;

            var sortedByDefault = GetSortedByDefault(headerClicked.Column);
            if (currentSortedColumnHeader == null && sortedByDefault != null)
            {
                direction = sortedByDefault == "Ascending"
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else if (headerClicked == currentSortedColumnHeader)
            {
                direction = GetSortDirection(listView) == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }

            if (!string.IsNullOrEmpty(columnName))
            {
                SetupAndApplyColumnSort(listView, headerClicked, columnName, direction);
            }
        }

        public static void SetShowColumnSortGlyph(ListView listView, string columnName, bool showGlyph)
        {
            var currentSortedColumnHeader = GridViewUtils.GetSortedColumnHeader(listView);
            if (GetShowSortGlyph(listView) && currentSortedColumnHeader != null)
            {
                if (GetColumnHeaderName(currentSortedColumnHeader) == columnName)
                {
                    if (showGlyph)
                    {
                        var direction = GetSortDirection(listView);
                        AddSortGlyph
                        (
                            currentSortedColumnHeader,
                            direction,
                            direction == ListSortDirection.Ascending
                                ? GetSortGlyphAscending(listView)
                                : GetSortGlyphDescending(listView)
                        );
                    }
                    else
                    {
                        RemoveSortGlyph(currentSortedColumnHeader);
                    }
                }
            }
        }

        public static void SetupAndApplyColumnSort(ListView listView, GridViewColumnHeader columnHeader, string columnName, ListSortDirection direction)
        {
            SortListViewByColumn(listView, columnName, direction, GetCustomSort(columnHeader.Column));

            if (GetShowSortGlyph(listView))
            {
                var columnHidden = GetHideColumn(columnHeader.Column);
                if (columnHidden == false)
                {
                    AddSortGlyph
                    (
                        columnHeader,
                        direction,
                        direction == ListSortDirection.Ascending
                            ? GetSortGlyphAscending(listView)
                            : GetSortGlyphDescending(listView)
                    );
                }
            }
            SetSortedColumnName(listView, columnName);
            SetSortedColumnHeader(listView, columnHeader);
            SetSortDirection(listView, direction);
        }

        public static void RefreshSort(ListView listView)
        {
            var currentSortedColumnHeader = GetSortedColumnHeader(listView);
            if (currentSortedColumnHeader != null)
            {
                var columnName = GetColumnHeaderName(currentSortedColumnHeader);
                SortListViewByColumn(listView, columnName, GetSortDirection(listView), GetCustomSort(currentSortedColumnHeader.Column));
            }
        }

        public static string GetColumnHeaderName(GridViewColumnHeader columnHeader)
        {
            if (columnHeader != null)
            {
                return GetPropertyName(columnHeader.Column) ?? GetDisplayMemberBindingPath(columnHeader);
            }
            return null;
        }

        public static GridViewColumnHeader GetColumnHeaderByName(ListView listView, string columnName)
        {
            var gridView = listView?.View as GridView;
            if (gridView != null && !string.IsNullOrEmpty(columnName))
            {
                for (int i = 0; i < gridView.Columns.Count; i++)
                {
                    var columnHeader = gridView.Columns[i].Header as GridViewColumnHeader;
                    if (GetColumnHeaderName(columnHeader) == columnName)
                    {
                        return columnHeader;
                    }
                }
            }
            return null;
        }

        private static void SortListViewByColumn(ListView listView, string columnName, ListSortDirection direction, IReversableComparer customSort)
        {
            if (customSort != null)
            {
                var source = listView.ItemsSource;

                // . apply property-specified custom sorter to selected column
                var lcView = (ListCollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
                if (direction == ListSortDirection.Descending)
                {
                    lcView.CustomSort = customSort.reverseComparer;
                }
                else
                {
                    lcView.CustomSort = customSort.forwardComparer;
                }
            }
            else
            {
                // . apply default sorter to primary sort target (selected column)
                ICollectionView view = listView.Items;
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(columnName, direction));

                // . also add a secondary sort target, if specified
                var secondaryAutoSortBy = GetSecondaryAutoSortBy(listView);
                if (secondaryAutoSortBy != null)
                {
                    string[] sortBy = secondaryAutoSortBy.Split(',');
                    if (sortBy.Length == 2)
                    {
                        var sortColumn = sortBy[0];
                        var sortDirection = sortBy[1] == "Ascending" ? ListSortDirection.Ascending : ListSortDirection.Descending;
                        if (sortColumn != columnName)
                        {
                            view.SortDescriptions.Add(new SortDescription(sortColumn, sortDirection));
                        }
                    }
                }
            }
        }

        private static void AddSortGlyph(GridViewColumnHeader columnHeader, ListSortDirection direction, ImageSource sortGlyph)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(columnHeader);
            adornerLayer.Add(new SortGlyphAdorner
            (
                columnHeader,
                direction,
                sortGlyph
            ));
        }

        private static void RemoveSortGlyph(GridViewColumnHeader columnHeader)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(columnHeader);
            Adorner[] adorners = adornerLayer.GetAdorners(columnHeader);
            if (adorners != null)
            {
                foreach (Adorner adorner in adorners)
                {
                    if (adorner is SortGlyphAdorner)
                    {
                        adornerLayer.Remove(adorner);
                    }
                }
            }
        }

        private class SortGlyphAdorner : Adorner
        {
            private GridViewColumnHeader columnHeader;
            private ListSortDirection direction;
            private ImageSource sortGlyph;

            public SortGlyphAdorner(GridViewColumnHeader columnHeader, ListSortDirection direction, ImageSource sortGlyph)
                : base(columnHeader)
            {
                this.columnHeader = columnHeader;
                this.direction = direction;
                this.sortGlyph = sortGlyph;
            }

            private Geometry GetDefaultGlyph()
            {
                double x1 = columnHeader.ActualWidth - 15;
                double x2 = x1 + 10;
                double x3 = x1 + 5;
                double y1 = columnHeader.ActualHeight / 2 - 3;
                double y2 = y1 + 5;

                if (direction == ListSortDirection.Ascending)
                {
                    double tmp = y1;
                    y1 = y2;
                    y2 = tmp;
                }

                PathSegmentCollection pathSegmentCollection = new PathSegmentCollection();
                pathSegmentCollection.Add(new LineSegment(new Point(x2, y1), true));
                pathSegmentCollection.Add(new LineSegment(new Point(x3, y2), true));

                PathFigure pathFigure = new PathFigure
                (
                    new Point(x1, y1),
                    pathSegmentCollection,
                    true
                );

                PathFigureCollection pathFigureCollection = new PathFigureCollection();
                pathFigureCollection.Add(pathFigure);

                PathGeometry pathGeometry = new PathGeometry(pathFigureCollection);
                return pathGeometry;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                if (sortGlyph != null)
                {
                    double x = columnHeader.ActualWidth - 13;
                    double y = columnHeader.ActualHeight / 2 - 5;
                    Rect rect = new Rect(x, y, 10, 10);
                    drawingContext.DrawImage(sortGlyph, rect);
                }
                else
                {
                    drawingContext.DrawGeometry(Brushes.WhiteSmoke, new Pen(Brushes.WhiteSmoke, 1.0), GetDefaultGlyph());
                }
            }
        }

        #endregion

        #region Column resizing (AutoWidth, MinWidth, FixedWidth)

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Attatched properties:
        //  AutoWidth   - Fit column width to contents; applied during explicit ColumnResize() method call.
        //  MinWidth    - Enforce minimum column width; applied during any resize activity (including ColumnResize()).
        //  FixedWidth  - Maintain a constant column width; applied during any resize activity
        //  HideColumn  - If "True", hides a column with AutoWidth/MinWidth or FixedWidth by forcing its width to 0.
        //
        // Notes:
        // 1. AutoWidth and MinWidth can be used separately, or in conjunction, where the resulting width is the
        //    Max() of the contents' width and the MinWidth value.
        // 2. FixedWidth should not be combined with AutoWidth or MinWidth properties on a given column.
        // 3. HideColumn can be used to supercede AutoWidth/MinWidth or FixedWidth properties on a given column;
        //    whenever HideColumn is true, the column width will be forced to 0.
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static GridViewColumn FindColumn(ListView listView, string columnName)
        {
            var gridView = listView.View as GridView;
            if (gridView != null)
            {
                foreach (var column in gridView.Columns)
                {
                    if (columnName == GetColumnHeaderName(column.Header as GridViewColumnHeader))
                    {
                        return column;
                    }
                }
            }
            return null;
        }

        public static double GetColumnWidth(GridViewColumn column)
        {
            if (column != null)
            {
                if (double.IsNaN(column.Width))
                {
                    return column.ActualWidth;
                }
                else
                {
                    return column.Width;
                }
            }
            return double.NaN;
        }

        public static void SetColumnWidth(GridViewColumn column, double value)
        {
            if (column != null)
            {
                column.Width = value;
            }
        }

        public static bool GetHideColumn(DependencyObject obj)
        {
            return (bool)obj.GetValue(HideColumnProperty);
        }

        public static void SetHideColumn(DependencyObject obj, bool value)
        {
            obj.SetValue(HideColumnProperty, value);
        }

        public static readonly DependencyProperty HideColumnProperty = DependencyProperty.RegisterAttached
        (
            "HideColumn",
            typeof(bool),
            typeof(GridViewUtils),
            new UIPropertyMetadata
            (
                false,
                (o, e) =>
                {
                    var column = o as GridViewColumn;
                    if (column != null)
                    {
                        bool newVal = (bool)e.NewValue;
                        bool oldVal = (bool)e.OldValue;

                        // . hide column
                        if (newVal && !oldVal)
                        {
                            column.Width = 0;
                        }

                        // . unhide column 
                        else if (!newVal && oldVal)
                        {
                            // . apply FixedWidth, if applicable
                            var fixedWidth = (double)column.GetValue(FixedWidthProperty);
                            if (!double.IsNaN(fixedWidth) && column.ActualWidth != fixedWidth)
                            {
                                column.Width = fixedWidth;
                            }
                            else
                            {
                                // . apply MinWidth, if applicable
                                var minWidth = (double)column.GetValue(MinWidthProperty);
                                if (!double.IsNaN(minWidth) && column.ActualWidth < minWidth)
                                {
                                    column.Width = minWidth;
                                }

                                // . apply AutoWidth, if applicable
                                if ((bool)column.GetValue(AutoWidthProperty))
                                {
                                    if (double.IsNaN(column.Width))
                                    {
                                        column.Width = column.ActualWidth;
                                    }
                                    column.Width = double.NaN;
                                }
                            }
                        }
                    }
                }
            )
        );

        public static bool GetAutoWidth(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoWidthProperty);
        }

        public static void SetAutoWidth(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoWidthProperty, value);
        }

        public static readonly DependencyProperty AutoWidthProperty = DependencyProperty.RegisterAttached
        (
            "AutoWidth",
            typeof(bool),
            typeof(GridViewUtils),
            new UIPropertyMetadata(false)
        );

        private static void AutoWidthResize(GridView gridView)
        {
            if (gridView != null)
            {
                foreach (var column in gridView.Columns)
                {
                    AutoWidthResizeColumn(column);
                }
            }
        }

        public static void AutoWidthResizeColumn(GridViewColumn column)
        {
            if (column != null)
            {
                var hideColumn = (bool)column.GetValue(HideColumnProperty);
                if (hideColumn)
                {
                    column.Width = 0;
                }
                else if ((bool)column.GetValue(AutoWidthProperty))
                {
                    if (double.IsNaN(column.Width))
                    {
                        column.Width = column.ActualWidth;
                    }
                    column.Width = double.NaN;
                }
            }
        }

        private static double GetWidthToRestore(DependencyObject obj)
        {
            return (double)obj.GetValue(WidthToRestoreProperty);
        }
        
        private static void SetWidthToRestore(DependencyObject obj, double value)
        {
            obj.SetValue(WidthToRestoreProperty, value);
        }

        private static readonly DependencyProperty WidthToRestoreProperty = DependencyProperty.RegisterAttached
        ( 
            "WidthToRestore", typeof(double), typeof(GridViewUtils), new PropertyMetadata(double.NaN)
        );

        public static double GetMinWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(MinWidthProperty);
        }

        public static void SetMinWidth(DependencyObject obj, double value)
        {
            obj.SetValue(MinWidthProperty, value);
        }

        public static readonly DependencyProperty MinWidthProperty = DependencyProperty.RegisterAttached
        (
            "MinWidth",
            typeof(double),
            typeof(GridViewUtils),
            new UIPropertyMetadata
            (
                double.NaN,
                (o, e) =>
                {
                    var column = o as GridViewColumn;
                    if (column != null)
                    {
                        double newVal = (double)e.NewValue;
                        double oldVal = (double)e.OldValue;

                        if (!double.IsNaN(newVal) && double.IsNaN(oldVal))
                        {
                            ((INotifyPropertyChanged)column).PropertyChanged += ApplyColumnMinWidth;
                        }
                        else if (double.IsNaN(newVal) && !double.IsNaN(oldVal))
                        {
                            ((INotifyPropertyChanged)column).PropertyChanged -= ApplyColumnMinWidth;
                        }
                        else if (!double.IsNaN(newVal))
                        {
                            if (column.ActualWidth < newVal)
                            {
                                if (column.ActualWidth >= oldVal)
                                {
                                    // condition: old MinWidth <= ActualWidth < new MinWidth 
                                    // . save user's actual width as "WidthToRestore", which can be restored
                                    //    later, should MinWidth be changed to a value < WidthToRestore.
                                    SetWidthToRestore(column, column.ActualWidth);
                                }
                                column.Width = newVal;
                            }
                            else 
                            {
                                var widthToRestore = GetWidthToRestore(column);
                                if (!double.IsNaN(widthToRestore) && newVal < oldVal)
                                {
                                    if (newVal <= widthToRestore && widthToRestore < column.ActualWidth && column.ActualWidth == oldVal)
                                    {
                                        // condition: new MinWidth <= WidthToRestore < ActualWidth == old MinWidth
                                        column.Width = widthToRestore;
                                    }
                                    // condition to (possibly use and) delete saved width: new MinWidth < old MinWidth
                                    SetWidthToRestore(column, double.NaN);
                                }
                            }
                        }
                    }
                }
            )
        );

        private static void ApplyColumnMinWidth(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActualWidth")
            {
                var column = sender as GridViewColumn;
                if (column != null)
                {
                    if ((bool)column.GetValue(HideColumnProperty))
                    {
                        column.Width = 0;
                    }
                    else
                    {
                        var minWidth = (double)column.GetValue(MinWidthProperty);
                        if (!double.IsNaN(minWidth) && column.ActualWidth < minWidth)
                        {
                            column.Width = minWidth;
                        }
                    }
                }
            }
        }

        public static double GetFixedWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(FixedWidthProperty);
        }

        public static void SetFixedWidth(DependencyObject obj, double value)
        {
            obj.SetValue(FixedWidthProperty, value);
        }

        public static readonly DependencyProperty FixedWidthProperty = DependencyProperty.RegisterAttached
        (
            "FixedWidth",
            typeof(double),
            typeof(GridViewUtils),
            new UIPropertyMetadata
            (
                double.NaN,
                (o, e) =>
                {
                    var column = o as GridViewColumn;
                    if (column != null)
                    {
                        double newVal = (double)e.NewValue;
                        double oldVal = (double)e.OldValue;

                        if (!double.IsNaN(newVal) && double.IsNaN(oldVal))
                        {
                            ((INotifyPropertyChanged)column).PropertyChanged += ApplyColumnFixedWidth;
                        }
                        else if (double.IsNaN(newVal) && !double.IsNaN(oldVal))
                        {
                            ((INotifyPropertyChanged)column).PropertyChanged -= ApplyColumnFixedWidth;
                        }
                        else if (!double.IsNaN(newVal) && column.ActualWidth != newVal)
                        {
                            column.Width = newVal;
                        }
                    }
                }
            )
        );

        private static void ApplyColumnFixedWidth(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActualWidth")
            {
                var column = sender as GridViewColumn;
                if (column != null)
                {
                    if ((bool)column.GetValue(HideColumnProperty))
                    {
                        column.Width = 0;
                    }
                    else
                    {
                        var fixedWidth = (double)column.GetValue(FixedWidthProperty);
                        if (!double.IsNaN(fixedWidth) && column.ActualWidth != fixedWidth)
                        {
                            column.Width = fixedWidth;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Performs automatic column resizing on the argument ListView. GridViewColumns that have
        /// an attatched property AutoWidth='True' are sized to fit their contents, and if any resized  
        /// column also has an attached property MinWidth='<value>', the width will be the 
        /// greater of the fit-to-contents width and min width values.  Note, the MinWidth-attached
        /// columns will continually maintain those column's widths to be >= the min value, even
        /// while the user is resizing a column via the UI.
        /// </summary>
        /// <param name="listView"></param>
        public static void ColumnResize(ListView listView)
        {
            AutoWidthResize(listView.View as GridView);
        }

        #endregion
    }
}
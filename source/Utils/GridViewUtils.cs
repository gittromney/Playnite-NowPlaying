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

namespace NowPlaying.Utils
{

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
                var scrollViewer = GetChildOfType<ScrollViewer>(sender as ListView);
                if (scrollViewer != null)
                {
                    if (e.Delta < 0)
                    {
                        scrollViewer.LineRight();
                    }
                    else
                    {
                        scrollViewer.LineLeft();
                    }
                }
            }
        }

        public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
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

        private static GridViewColumnHeader GetSortedColumnHeader(DependencyObject obj)
        {
            return (GridViewColumnHeader)obj.GetValue(SortedColumnHeaderProperty);
        }

        private static void SetSortedColumnHeader(DependencyObject obj, GridViewColumnHeader value)
        {
            obj.SetValue(SortedColumnHeaderProperty, value);
        }

        private static readonly DependencyProperty SortedColumnHeaderProperty = DependencyProperty.RegisterAttached
        (
            "SortedColumnHeader",
            typeof(GridViewColumnHeader), 
            typeof(GridViewUtils), 
            new UIPropertyMetadata(null)
        );

        private static ListSortDirection GetSortDirection(DependencyObject obj)
        {
            return (ListSortDirection)obj.GetValue(SortDirectionProperty);
        }

        private static void SetSortDirection(DependencyObject obj, ListSortDirection value)
        {
            obj.SetValue(SortDirectionProperty, value);
        }

        private static readonly DependencyProperty SortDirectionProperty = DependencyProperty.RegisterAttached
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
                    ListView listView = GetAncestor<ListView>(headerClicked);
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

        public static T GetAncestor<T>(DependencyObject reference) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(reference);
            while (!(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent != null)
                return (T)parent;
            else
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
                SortListViewByColumn(listView, columnName, direction, GetCustomSort(headerClicked.Column));
                
                if (GetShowSortGlyph(listView))
                {
                    AddSortGlyph
                    (
                        headerClicked,
                        direction,
                        direction == ListSortDirection.Ascending
                            ? GetSortGlyphAscending(listView)
                            : GetSortGlyphDescending(listView)
                    );
                }
                SetSortedColumnHeader(listView, headerClicked);
                SetSortDirection(listView, direction);
            }
        }

        public static void RefreshSort(ListView listView)
        {
            var currentSortedColumnHeader = GetSortedColumnHeader(listView);
            if (currentSortedColumnHeader != null)
            {
                string columnName = GetPropertyName(currentSortedColumnHeader.Column);

                if (string.IsNullOrEmpty(columnName))
                {
                    columnName = GetDisplayMemberBindingPath(currentSortedColumnHeader);
                }
                var direction = GetSortDirection(listView);
                SortListViewByColumn(listView, columnName, direction, GetCustomSort(currentSortedColumnHeader.Column));
            }
        }

        private static void SortListViewByColumn(ListView listView, string columnName, ListSortDirection direction, IReversableComparer customSort)
        {
            if (customSort != null)
            {
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
        }

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
                        else if (!double.IsNaN(newVal) && column.ActualWidth < newVal)
                        {
                            column.Width = newVal;
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
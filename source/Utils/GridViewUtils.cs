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
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;

namespace NowPlaying.Utils
{
    public class GridViewUtils
    {
        
        // . Pass ListView/GridView mouse wheel events to parent UIElement for proper scrolling
        public static void MouseWheelToParent(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

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


        public static IComparer GetCustomSort(DependencyObject obj)
        {
            return (IComparer)obj.GetValue(CustomSortProperty);
        }

        public static void SetCustomSort(DependencyObject obj, IComparer value)
        {
            obj.SetValue(CustomSortProperty, value);
        }

        public static readonly DependencyProperty CustomSortProperty = DependencyProperty.RegisterAttached
        (
            "CustomSort",
            typeof(IComparer),
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
                string propertyName = GetPropertyName(headerClicked.Column);

                if (string.IsNullOrEmpty(propertyName))
                {
                    propertyName = GetDisplayMemberBindingPath(headerClicked);
                }

                if (!string.IsNullOrEmpty(propertyName))
                {
                    ListView listView = GetAncestor<ListView>(headerClicked);
                    if (listView != null)
                    {
                        if (GetAutoSort(listView))
                        {                
                            ApplySort(listView.Items, propertyName, listView, headerClicked, GetCustomSort(headerClicked.Column));
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
            ICollectionView view, 
            string propertyName, 
            ListView listView, 
            GridViewColumnHeader sortedColumnHeader,
            IComparer customSort = null 
        )
        {
            var currentSortedColumnHeader = GetSortedColumnHeader(listView);
            if (currentSortedColumnHeader != null)
            {
                RemoveSortGlyph(currentSortedColumnHeader);
            }

            ListSortDirection direction = ListSortDirection.Ascending;
            if (sortedColumnHeader == currentSortedColumnHeader)
            {
                direction = GetSortDirection(listView) == ListSortDirection.Ascending 
                    ? ListSortDirection.Descending 
                    : ListSortDirection.Ascending;
            }

            if (!string.IsNullOrEmpty(propertyName))
            {
                if (customSort != null)
                {
                    // . apply property-specified custom sorter to selected column
                    var lcView = (ListCollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
                    if (direction == ListSortDirection.Descending)
                    {
                        lcView.CustomSort = Comparer<object>.Create((x, y) => customSort.Compare(y, x));
                    }
                    else
                    {
                        lcView.CustomSort = customSort;
                    }
                }
                else
                {
                    // . apply default sorter to selected column
                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new SortDescription(propertyName, direction));
                }
                if (GetShowSortGlyph(listView))
                {
                    AddSortGlyph
                    (
                        sortedColumnHeader,
                        direction,
                        direction == ListSortDirection.Ascending
                            ? GetSortGlyphAscending(listView)
                            : GetSortGlyphDescending(listView)
                    );
                }
                SetSortedColumnHeader(listView, sortedColumnHeader);
                SetSortDirection(listView, direction);
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
        //
        // Note, AutoWidth and MinWidth can be used separately, or in conjunction, where the resulting width is the
        // Max() of the contents' width and the MinWidth value. FixedWidth should not be combined with other properties
        // on a given column.
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
                    var minWidth = (double)column.GetValue(MinWidthProperty);
                    if (!double.IsNaN(minWidth) && column.ActualWidth < minWidth)
                    {
                        column.Width = minWidth;
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
                    var fixedWidth = (double)column.GetValue(FixedWidthProperty);
                    if (!double.IsNaN(fixedWidth) && column.ActualWidth != fixedWidth)
                    {
                        column.Width = fixedWidth;
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
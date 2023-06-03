using NowPlaying.Utils;
using NowPlaying.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NowPlaying.Views
{
    /// <summary>
    /// Interaction logic for CacheRootsView.xaml
    /// </summary>
    public partial class CacheRootsView : UserControl
    {
        public CacheRootsView(CacheRootsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
        
        public void UnselectCacheRoots()
        {
            CacheRoots.UnselectAll();
        }

        private void CacheRoots_ResizeColumns(object sender, RoutedEventArgs e)
        {
            GridViewUtils.ColumnResize(CacheRoots);
        }

        private void PreviewMouseWheelToParent(object sender, MouseWheelEventArgs e)
        {
            GridViewUtils.MouseWheelToParent(sender, e);
        }

    }
}

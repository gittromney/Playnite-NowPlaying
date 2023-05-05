using NowPlaying.ViewModels;
using System.Windows.Controls;

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
    }
}

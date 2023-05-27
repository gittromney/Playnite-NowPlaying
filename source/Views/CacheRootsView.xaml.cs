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

        public void CacheRoots_AutoResizeDirectoryColumn()
        {
            var view = CacheRoots.View as GridView;
            if (view != null && view.Columns.Count > 0)
            {
                foreach (var column in view.Columns)
                {
                    var header = column.Header as GridViewColumnHeader;
                    if (header != null)
                    {
                        var content = header.Content as string;
                        if (content != null && content == "Directory")
                        {
                            if (double.IsNaN(column.Width)) column.Width = 1;
                            column.Width = double.NaN;
                        }
                    }
                }
            }
        }

    }
}

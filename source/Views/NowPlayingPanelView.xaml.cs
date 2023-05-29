using NowPlaying.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using NowPlaying.Utils;

namespace NowPlaying.Views
{
    /// <summary>
    /// Interaction logic for NowPlayingPanelView.xaml
    /// </summary>
    public partial class NowPlayingPanelView : UserControl
    {
        NowPlayingPanelViewModel viewModel;

        public NowPlayingPanelView(NowPlayingPanelViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;
        }

        public void GameCaches_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            viewModel.SelectedGameCaches = GameCaches.SelectedItems.Cast<GameCacheViewModel>().ToList();
        }

        public void GameCaches_ClearSelected()
        {
            GameCaches.SelectedItems.Clear();
            viewModel.SelectedGameCaches = new List<GameCacheViewModel>();
        }

        public void GameCaches_AutoResizeTitleColumn()
        {
            var view = GameCaches.View as GridView;
            if (view != null && view.Columns.Count > 0)
            {
                foreach (var column in view.Columns)
                {
                    var header = column.Header as GridViewColumnHeader;
                    if (header != null)
                    {
                        var content = header.Content as string;
                        if (content != null && content == "Title")
                        {
                            if (double.IsNaN(column.Width)) column.Width = 1;
                            column.Width = double.NaN;
                        }
                    }
                }
            }
        }

        public void Reroot_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                var button = sender as Button;
                ContextMenu contextMenu = button.ContextMenu;
                contextMenu.PlacementTarget = button;
                contextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void RootsButton_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void RootsButton_MouseLeave(object sender, MouseEventArgs e)
        {
        }
    }
}

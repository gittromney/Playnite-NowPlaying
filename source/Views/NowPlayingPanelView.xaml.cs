using NowPlaying.Utils;
using NowPlaying.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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
            DispatcherUtils.Invoke(Dispatcher, () => GameCaches.SelectedItems.Clear());
            viewModel.SelectedGameCaches = new List<GameCacheViewModel>();
        }

        private void GameCaches_ResizeColumns(object sender, RoutedEventArgs e)
        {
            GridViewUtils.ColumnResize(GameCaches);
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

        private void PreviewMouseWheelToParent(object sender, MouseWheelEventArgs e)
        {
            GridViewUtils.MouseWheelToParent(sender, e);
        }

    }
}

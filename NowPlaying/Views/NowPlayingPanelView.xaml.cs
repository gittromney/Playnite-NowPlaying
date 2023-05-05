using NowPlaying.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

    }
}

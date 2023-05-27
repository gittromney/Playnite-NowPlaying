using NowPlaying.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace NowPlaying.Views
{
    /// <summary>
    /// Interaction logic for NowPlayingPanelView.xaml
    /// </summary>
    public partial class NowPlayingPanelView : UserControl
    {
        NowPlayingPanelViewModel viewModel;
        BitmapImage rootsIcon;
        BitmapImage rootsHover;

        public NowPlayingPanelView(NowPlayingPanelViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;

            // . setup Roots icon (default and mouse-over/hover versions)
            this.rootsIcon = BitmapToBitmapImage(Properties.Resources.roots_icon);
            this.rootsHover = BitmapToBitmapImage(Properties.Resources.roots_hover);
            RootsButtonImage.Source = rootsIcon;
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

        BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private void RootsButton_MouseEnter(object sender, MouseEventArgs e)
        {
            var image = RootsButtonImage;
            if (image != null && image.Source != rootsHover)
            {
                image.Source = rootsHover;
            }
        }

        private void RootsButton_MouseLeave(object sender, MouseEventArgs e)
        {
            var image = RootsButtonImage;
            if (image != null && image.Source != rootsIcon)
            {
                image.Source = rootsIcon;
            }
        }
    }
}

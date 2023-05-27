
using NowPlaying.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NowPlaying.Views
{
    public partial class NowPlayingSettingsView : UserControl
    {
        public NowPlayingSettingsView(NowPlayingSettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Scroller_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Scroller.ActualWidth < double.Parse((string)(Scroller.Tag ?? "0")))
            {
                Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            }
            else
            {
                Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                Scroller.ScrollToLeftEnd();
            }
            e.Handled = true;
        }

    }
}
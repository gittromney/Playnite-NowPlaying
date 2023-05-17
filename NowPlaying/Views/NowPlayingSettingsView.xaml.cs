
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
            if (Scroller.ActualWidth < 580)
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

        private void ScrollerShiftOnButtonClick_Workaround(object sender, RoutedEventArgs e)
        {
            Scroller.ScrollToLeftEnd();
            e.Handled = true;
        }
    }
}

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
    }
}
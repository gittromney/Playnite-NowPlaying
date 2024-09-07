using NowPlaying.ViewModels;
using System.Windows.Controls;

namespace NowPlaying.Controls
{
    /// <summary>
    /// Interaction logic for SidebarIcon.xaml
    /// </summary>
    public partial class SidebarIcon : UserControl
    {
        public SidebarIcon(NowPlayingPanelViewModel panelViewModel)
        {
            InitializeComponent();
            DataContext = panelViewModel;
        }
    }
}

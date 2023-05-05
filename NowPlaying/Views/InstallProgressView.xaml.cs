
using NowPlaying.ViewModels;
using System.Windows.Controls;

namespace NowPlaying.Views
{
    /// <summary>
    /// Interaction logic for InstallProgressView.xaml
    /// </summary>
    public partial class InstallProgressView : UserControl
    {
        public InstallProgressView(InstallProgressViewModel progressViewModel)
        {
            InitializeComponent();
            DataContext = progressViewModel;
        }
    }
}

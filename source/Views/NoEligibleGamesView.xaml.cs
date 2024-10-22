using System.Windows.Controls;
using NowPlaying.ViewModels;

namespace NowPlaying.Views
{
    /// <summary>
    /// Interaction logic for NoEligibleGamesView.xaml
    /// </summary>
    public partial class NoEligibleGamesView : UserControl
    {
        private readonly AddGameCachesViewModel viewModel;

        public NoEligibleGamesView(AddGameCachesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.viewModel = viewModel;
        }
    }
}

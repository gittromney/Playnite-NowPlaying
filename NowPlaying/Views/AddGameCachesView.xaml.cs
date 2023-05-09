using NowPlaying.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace NowPlaying.Views
{
    /// <summary>
    /// Interaction logic for AddGameCachesView.xaml
    /// </summary>
    public partial class AddGameCachesView : UserControl
    {
        private readonly AddGameCachesViewModel viewModel;

        public AddGameCachesView(AddGameCachesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.viewModel = viewModel;
        }

        public void EligibleGames_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            viewModel.SelectedGames = EligibleGames.SelectedItems.Cast<GameViewModel>().ToList();
        }

        public void EligibleGames_ClearSelected()
        {
            EligibleGames.SelectedItems.Clear();
            viewModel.SelectedGames = new List<GameViewModel>();
        }

        public void EligibleGames_SelectAll()
        {
            EligibleGames.SelectAll();
            viewModel.SelectedGames = viewModel.EligibleGames;
        }
    }
}

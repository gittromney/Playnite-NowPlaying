using NowPlaying.Models;
using NowPlaying.Utils;
using NowPlaying.Views;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NowPlaying.ViewModels
{
    public class AddGameCachesViewModel : ViewModelBase
    {
        private readonly NowPlaying plugin;
        private readonly Window popup;
        private readonly List<CacheRootViewModel> cacheRoots;
        private readonly List<GameViewModel> allEligibleGames;

        public NowPlayingSettings Settings => plugin.Settings;
        public ThemeResources Theme => plugin.themeResources;

        private string searchText;
        public string SearchText
        {
            get => searchText;
            set
            {
                if (searchText != value)
                {
                    searchText = value;
                    OnPropertyChanged();

                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        EligibleGames = allEligibleGames;
                        OnPropertyChanged(nameof(EligibleGames));
                        SelectNoGames();
                    }
                    else
                    {
                        EligibleGames = allEligibleGames.Where(g => g.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                        OnPropertyChanged(nameof(EligibleGames));
                        SelectNoGames();
                    }

                    var view = popup.Content as AddGameCachesView;
                    GridViewUtils.RefreshSort(view.EligibleGames);
                }
            }
        }

        public class CustomPlatformSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . primary sort: by "Platform", reversable 
                string platformX = ((GameViewModel)(reverse ? y : x)).Platform.ToString();
                string platformY = ((GameViewModel)(reverse ? x : y)).Platform.ToString();

                // . secondary sort: by "Title", always ascending
                string titleX = ((GameViewModel)x).Title;
                string titleY = ((GameViewModel)y).Title;

                return platformX != platformY ? platformX.CompareTo(platformY) : titleX.CompareTo(titleY);
            }
        }
        public CustomPlatformSorter CustomPlatformSort { get; private set; }

        public class CustomInstallSizeSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . primary sort: by Cache/Install size, reversable
                long sizeX = ((GameViewModel)(reverse ? y : x)).InstallSizeBytes;
                long sizeY = ((GameViewModel)(reverse ? x : y)).InstallSizeBytes;

                // . secondary sort: by "Title", always ascending
                string titleX = ((GameViewModel)x).Title;
                string titleY = ((GameViewModel)y).Title;

                return sizeX != sizeY ? sizeX.CompareTo(sizeY) : titleX.CompareTo(titleY);
            }
        }
        public CustomInstallSizeSorter CustomInstallSizeSort { get; private set; }

        public ICommand SelectAllCommand { get; private set; }
        public ICommand SelectNoneCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand EnableSelectedGamesCommand { get; private set; }

        public List<string> CacheRoots => cacheRoots.Select(r => r.Directory).ToList();

        public List<GameViewModel> EligibleGames { get; private set; }

        private List<GameViewModel> selectedGames;
        public List<GameViewModel> SelectedGames 
        { 
            get => selectedGames;
            set
            {
                selectedGames = value;
                OnPropertyChanged();
            }
        }

        public string SelectedCacheRoot { get; set; }

        public bool EligibleGamesExist => allEligibleGames.Count > 0;
        public string EligibleGamesVisibility => allEligibleGames.Count > 0 ? "Visible" : "Collapsed";
        public string NoEligibleGamesVisibility => allEligibleGames.Count > 0 ? "Collapsed" : "Visible";


        public AddGameCachesViewModel(NowPlaying plugin, Window popup)
        {
            this.plugin = plugin;
            this.popup = popup;
            this.CustomPlatformSort = new CustomPlatformSorter();
            this.CustomInstallSizeSort = new CustomInstallSizeSorter();
            this.cacheRoots = plugin.cacheManager.CacheRoots.ToList();
            this.SelectedGames = new List<GameViewModel>();

            var eligibles = plugin.PlayniteApi.Database.Games.Where(g => plugin.IsGameNowPlayingEligible(g) != GameCachePlatform.InEligible);
            this.allEligibleGames = eligibles.Select(g => new GameViewModel(g)).OrderBy(g => g.Title).ToList();

            SelectAllCommand = new RelayCommand(() => SelectAllGames());
            SelectNoneCommand = new RelayCommand(() => SelectNoGames());
            CloseCommand = new RelayCommand(() => CloseWindow());
            EnableSelectedGamesCommand = new RelayCommand(() => EnableSelectedGamesAsync(), () => SelectedGames.Count > 0);

            SelectedCacheRoot = cacheRoots.First()?.Directory;
            OnPropertyChanged(nameof(SelectedCacheRoot));

            EligibleGames = allEligibleGames;
            OnPropertyChanged(nameof(EligibleGames));
            OnPropertyChanged(nameof(EligibleGamesExist));
            OnPropertyChanged(nameof(EligibleGamesVisibility));
            OnPropertyChanged(nameof(NoEligibleGamesVisibility));
        }

        public void SelectNoGames()
        {
            var view = popup.Content as AddGameCachesView;
            view?.EligibleGames_ClearSelected();
        }

        public void SelectAllGames()
        {
            var view = popup.Content as AddGameCachesView;
            view?.EligibleGames_SelectAll();
        }

        private async void EnableSelectedGamesAsync()
        {
            CloseWindow();
            if (SelectedGames != null)
            {
                var cacheRoot = plugin.cacheManager.FindCacheRoot(SelectedCacheRoot);
                if (cacheRoot != null)
                {
                    foreach (var game in SelectedGames)
                    {
                        if (!plugin.IsGameNowPlayingEnabled(game.game))
                        {
                            if (await plugin.CheckIfGameInstallDirIsAccessibleAsync(game.Title, game.InstallDir))
                            {
                                if (plugin.CheckAndConfirmOrAdjustInstallDirDepth(game.game))
                                {
                                    (new NowPlayingGameEnabler(plugin, game.game, cacheRoot.Directory)).Activate();
                                }
                            }
                        }
                    }
                }
            }
        }

        public void CloseWindow()
        {
            plugin.panelViewModel.ModalDimming = false;
            DispatcherUtils.Invoke(popup.Dispatcher, popup.Close);
        }
    }
}

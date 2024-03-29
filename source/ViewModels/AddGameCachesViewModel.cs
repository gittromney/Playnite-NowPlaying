﻿using NowPlaying.Models;
using NowPlaying.Views;
using Playnite.SDK;
using System;
using System.Collections;
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
                }
            }
        }

        public class CustomInstallSizeSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                long sizeX = ((GameViewModel)x).InstallSizeBytes;
                long sizeY = ((GameViewModel)y).InstallSizeBytes;
                return sizeX.CompareTo(sizeY);
            }
        }
        public CustomInstallSizeSorter CustomInstallSizeSort { get; private set; }

        public ICommand ClearSearchTextCommand { get; private set; }
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

        public string EligibleGamesVisibility => allEligibleGames.Count > 0 ? "Visible" : "Collapsed";
        public string NoEligibleGamesVisibility => allEligibleGames.Count > 0 ? "Collapsed" : "Visible";


        public AddGameCachesViewModel(NowPlaying plugin, Window popup)
        {
            this.plugin = plugin;
            this.popup = popup;
            this.CustomInstallSizeSort = new CustomInstallSizeSorter();
            this.cacheRoots = plugin.cacheManager.CacheRoots.ToList();
            this.SelectedGames = new List<GameViewModel>();

            var eligibles = plugin.PlayniteApi.Database.Games.Where(g => plugin.IsGameNowPlayingEligible(g) != GameCachePlatform.InEligible);
            this.allEligibleGames = eligibles.Select(g => new GameViewModel(g)).ToList();

            ClearSearchTextCommand = new RelayCommand(() => SearchText = string.Empty);
            SelectAllCommand = new RelayCommand(() => SelectAllGames());
            SelectNoneCommand = new RelayCommand(() => SelectNoGames());
            CloseCommand = new RelayCommand(() => CloseWindow());
            EnableSelectedGamesCommand = new RelayCommand(() => EnableSelectedGamesAsync(), () => SelectedGames.Count > 0);

            SelectedCacheRoot = cacheRoots.First()?.Directory;
            OnPropertyChanged(nameof(SelectedCacheRoot));

            EligibleGames = allEligibleGames;
            OnPropertyChanged(nameof(EligibleGames));
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
            if (popup.Dispatcher.CheckAccess())
            {
                popup.Close();
            }
            else
            {
                popup.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(popup.Close));
            }
        }
    }
}

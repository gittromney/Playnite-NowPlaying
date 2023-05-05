using NowPlaying.Models;
using NowPlaying.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MenuItem = System.Windows.Controls.MenuItem;
using UserControl = System.Windows.Controls.UserControl;

namespace NowPlaying.ViewModels
{
    public class NowPlayingPanelViewModel : ViewModelBase
    {
        private readonly NowPlaying plugin;

        public NowPlayingPanelViewModel(NowPlaying plugin)
        {
            this.plugin = plugin;
            this.isTopPanelVisible = false;
            this.showSettings = false;
            this.showCacheRoots = false;
            this.SelectedGameCaches = new List<GameCacheViewModel>();
            this.selectionContext = new SelectedCachesContext();
            this.RerootCachesSubMenuItems = new List<MenuItem>();

            this.RefreshCachesCommand = new RelayCommand(() => RefreshGameCaches());
            this.InstallCachesCommand = new RelayCommand(() => InstallSelectedCaches(SelectedGameCaches), () => InstallCachesCanExecute);
            this.UninstallCachesCommand = new RelayCommand(() => UninstallSelectedCaches(SelectedGameCaches), () => UninstallCachesCanExecute);
            this.DisableCachesCommand = new RelayCommand(() => DisableSelectedCaches(SelectedGameCaches), () => DisableCachesCanExecute);
            this.RerootClickCanExecute = new RelayCommand(() => {}, () => RerootCachesCanExecute);

            this.ToggleShowCacheRoots = new RelayCommand(() => ShowCacheRoots = !ShowCacheRoots, () => AreCacheRootsNonEmpty);
            this.ToggleShowSettings = new RelayCommand(() =>
            {
                if (showSettings)
                {
                    plugin.settingsViewModel.CancelEdit();
                    ShowSettings = false;
                }
                else
                {
                    plugin.settingsViewModel.BeginEdit();
                    ShowSettings = true;
                }
            });

            this.SaveSettingsCommand = new RelayCommand(() =>
            {
                List<string> errors;
                if (plugin.settingsViewModel.VerifySettings(out errors))
                {
                    plugin.settingsViewModel.EndEdit();
                    ShowSettings = false;
                }
                else
                {
                    foreach (var error in errors)
                    {
                        NowPlaying.logger.Error(error);
                        plugin.PlayniteApi.Dialogs.ShowErrorMessage(error, "NowPlaying Settings Error");
                    }
                }
            });

            this.CancelSettingsCommand = new RelayCommand(() =>
            {
                plugin.settingsViewModel.CancelEdit();
                ShowSettings = false;
            });

            this.AddGameCachesCommand = new RelayCommand(() =>
            {
                var appWindow = plugin.PlayniteApi.Dialogs.GetCurrentAppWindow();
                var popup = plugin.PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions()
                {
                    ShowCloseButton = false,
                    ShowMaximizeButton = false,
                    ShowMinimizeButton = false
                });
                var viewModel = new AddGameCachesViewModel(plugin, popup);
                var view = new AddGameCachesView(viewModel);
                popup.Content = view;

                // setup popup and center within the current application window
                popup.Width = view.Width;
                popup.MinWidth = view.Width;
                popup.Height = view.Height;
                popup.MinHeight = view.Height;
                popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
                popup.Show();
                viewModel.SelectNoGames();
            });
        }

        public ICommand ToggleShowCacheRoots { get; private set; }
        public ICommand ToggleShowSettings { get; private set; }
        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand CancelSettingsCommand { get; private set; }

        public ICommand RefreshCachesCommand { get; private set; }
        public ICommand AddGameCachesCommand { get; private set; }
        public ICommand InstallCachesCommand { get; private set; }
        public ICommand UninstallCachesCommand { get; private set; }
        public ICommand DisableCachesCommand { get; private set; }
        public ICommand RerootClickCanExecute { get; private set; }

        public ObservableCollection<GameCacheViewModel> GameCaches => plugin.cacheManager.GameCaches;


        public bool AreCacheRootsNonEmpty => plugin.cacheManager.CacheRoots.Count > 0;
        public bool MultipleCacheRoots => plugin.cacheManager.CacheRoots.Count > 1;
        public string GameCachesVisibility => AreCacheRootsNonEmpty ? "Visible" : "Collapsed";
        public string MultipleRootsVisibility => MultipleCacheRoots ? "Visible" : "Collapsed";
        public string GameCachesRootColumnWidth => MultipleCacheRoots ? "NaN" : "0";

        public string InstallCachesMenu { get; private set; }
        public string InstallCachesVisibility { get; private set; }
        public bool InstallCachesCanExecute { get; private set; }
        public string RerootCachesMenu { get; private set; }
        public List<MenuItem> RerootCachesSubMenuItems { get; private set; }
        public string RerootCachesVisibility { get; private set; }
        public bool RerootCachesCanExecute { get; private set; }
        public string UninstallCachesMenu { get; private set; }
        public string UninstallCachesVisibility { get; private set; }
        public bool UninstallCachesCanExecute { get; private set; }
        public string DisableCachesMenu { get; private set; }
        public string DisableCachesVisibility { get; private set; }
        public bool DisableCachesCanExecute { get; private set; }


        private SelectedCachesContext selectionContext;
        public SelectedCachesContext SelectionContext
        {
            get => selectionContext;
            set
            {
                selectionContext = value;
                OnPropertyChanged(nameof(SelectionContext));
            }
        }

        private List<GameCacheViewModel> selectedGameCaches;
        public List<GameCacheViewModel> SelectedGameCaches
        {
            get => selectedGameCaches;
            set
            {
                selectedGameCaches = value;
                SelectionContext = GetSelectedGamesContext(selectedGameCaches);

                InstallCachesMenu = GetInstallCachesMenu(SelectionContext);
                InstallCachesVisibility = InstallCachesMenu != null ? "Visible" : "Collapsed";
                InstallCachesCanExecute = InstallCachesMenu != null;

                OnPropertyChanged(nameof(InstallCachesMenu));
                OnPropertyChanged(nameof(InstallCachesVisibility));
                OnPropertyChanged(nameof(InstallCachesCanExecute));

                RerootCachesMenu = GetRerootCachesMenu(SelectionContext);
                RerootCachesSubMenuItems = RerootCachesMenu != null ? GetRerootCachesSubMenuItems() : null;
                RerootCachesVisibility = RerootCachesMenu != null ? "Visible" : "Collapsed";
                RerootCachesCanExecute = RerootCachesMenu != null;

                OnPropertyChanged(nameof(RerootCachesMenu));
                OnPropertyChanged(nameof(RerootCachesSubMenuItems));
                OnPropertyChanged(nameof(RerootCachesVisibility));
                OnPropertyChanged(nameof(RerootCachesCanExecute));

                UninstallCachesMenu = GetUninstallCachesMenu(SelectionContext);
                UninstallCachesVisibility = UninstallCachesMenu != null ? "Visible" : "Collapsed";
                UninstallCachesCanExecute = UninstallCachesMenu != null;

                OnPropertyChanged(nameof(UninstallCachesMenu));
                OnPropertyChanged(nameof(UninstallCachesVisibility));
                OnPropertyChanged(nameof(UninstallCachesCanExecute));

                DisableCachesMenu = GetDisableCachesMenu(SelectionContext);
                DisableCachesVisibility = DisableCachesMenu != null ? "Visible" : "Collapsed";
                DisableCachesCanExecute = DisableCachesMenu != null;

                OnPropertyChanged(nameof(DisableCachesMenu));
                OnPropertyChanged(nameof(DisableCachesVisibility));
                OnPropertyChanged(nameof(DisableCachesCanExecute));
            }
        }

        private UserControl topPanelView;
        public UserControl TopPanelView
        {
            get => topPanelView;
            set
            {
                topPanelView = value;
                OnPropertyChanged();
            }
        }


        private bool isTopPanelVisible;
        public bool IsTopPanelVisible
        {
            get => isTopPanelVisible;
            set
            {
                if (isTopPanelVisible != value)
                {
                    isTopPanelVisible = value;

                    if (isTopPanelVisible)
                    {
                        TopPanelView = plugin.topPanelView;
                        plugin.topPanelViewModel.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TopPanelView));
                    }
                    else
                    {
                        TopPanelView = null;
                        plugin.topPanelViewModel.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(TopPanelView));
                    }
                }
            }
        }

        private UserControl cacheRootsView;
        public UserControl CacheRootsView
        {
            get => cacheRootsView;
            set
            {
                cacheRootsView = value;
                OnPropertyChanged();
            }
        }


        public string ShowCacheRootsToolTip => showCacheRoots ? "Hide cache roots" : "Show cache roots";
        private bool showCacheRoots;
        public bool ShowCacheRoots
        {
            get => showCacheRoots;
            set
            {
                if (showCacheRoots != value)
                {
                    showCacheRoots = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowCacheRootsToolTip));

                    if (showCacheRoots)
                    {
                        CacheRootsView = plugin.cacheRootsView;
                        plugin.cacheRootsViewModel.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CacheRootsView));
                        plugin.cacheRootsViewModel.RefreshRootsList();
                    }
                    else
                    {
                        plugin.cacheRootsViewModel.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(CacheRootsView));
                        CacheRootsView = null;
                    }
                }
            }
        }

        private UserControl installProgressView;
        public UserControl InstallProgressView
        {
            get => installProgressView;
            set
            {
                installProgressView = value;
                OnPropertyChanged();
            }
        }

        private UserControl settingsView;
        public UserControl SettingsView
        {
            get => settingsView;
            set
            {
                settingsView = value;
                OnPropertyChanged();
            }
        }

        public string ShowSettingsToolTip => showSettings ? "Hide NowPlaying settings" : "Show NowPlaying settings";

        public string SettingsVisibility => ShowSettings ? "Visible" : "Collapsed";

        private bool showSettings;
        public bool ShowSettings
        {
            get => showSettings;
            set
            {
                if (showSettings != value)
                {
                    showSettings = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SettingsVisibility));
                    OnPropertyChanged(nameof(ShowSettingsToolTip));

                    if (showSettings)
                    {
                        SettingsView = plugin.settingsView;
                        plugin.settingsViewModel.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SettingsView));
                    }
                    else
                    {
                        plugin.settingsViewModel.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(SettingsView));
                        SettingsView = null;
                    }
                }
            }
        }

        // . Note: call once all panel sub-views/view models are created
        public void ResetShowState()
        {
            ShowSettings = false;
            ShowCacheRoots = true;
        }

        public void RefreshGameCaches()
        {
            OnPropertyChanged(nameof(GameCaches));
            plugin.panelView.GameCaches_ClearSelected();
        }

        public void UpdateCacheRoots()
        { 
            OnPropertyChanged(nameof(AreCacheRootsNonEmpty));
            OnPropertyChanged(nameof(MultipleCacheRoots));
            OnPropertyChanged(nameof(GameCachesVisibility));
            OnPropertyChanged(nameof(MultipleRootsVisibility));
            OnPropertyChanged(nameof(GameCachesRootColumnWidth));

            // . update game cache menu/can execute/visibility...
            SelectedGameCaches = selectedGameCaches;
        }

        public class SelectedCachesContext
        {
            public bool allEmpty;
            public bool allPaused;
            public bool allInstalled;
            public bool allInstalling;
            public bool allEmptyOrPaused;
            public bool allInstalledOrPaused;
            public bool allInstalledPausedUnknownOrInvalid;
            public bool allWillFit;
            public int count;
            public SelectedCachesContext()
            {
                allEmpty = true;
                allPaused = true;
                allInstalled = true;
                allInstalling = true;
                allEmptyOrPaused = true;
                allInstalledOrPaused = true;
                allInstalledPausedUnknownOrInvalid = true;
                allWillFit = true;
                count = 0;
            }
        }

        private SelectedCachesContext GetSelectedGamesContext(List<GameCacheViewModel> gameCaches)
        {
            var context = new SelectedCachesContext();
            foreach (var gc in gameCaches)
            {
                bool isEmpty = gc.State == GameCacheState.Empty;
                bool isPaused = gc.State == GameCacheState.InProgress && gc.NowInstalling == false;
                bool isUnknown = gc.State == GameCacheState.Unknown;
                bool isInvalid = gc.State == GameCacheState.Invalid;
                bool isInstalled = gc.State == GameCacheState.Populated || gc.State == GameCacheState.Played;
                bool isInstalling = gc.State == GameCacheState.InProgress && gc.NowInstalling == true;
                context.allEmpty &= isEmpty;
                context.allPaused &= isPaused;
                context.allInstalled &= isInstalled;
                context.allInstalling &= isInstalling;
                context.allEmptyOrPaused &= isEmpty || isPaused;
                context.allInstalledOrPaused &= isInstalled || isPaused;
                context.allInstalledPausedUnknownOrInvalid &= isInstalled || isPaused || isUnknown || isInvalid;
                context.allWillFit &= gc.CacheWillFit;
                context.count++;
            }
            return context;
        }

        private string GetInstallCachesMenu(SelectedCachesContext context)
        {
            if (context.count > 0 && context.allWillFit)
            {
                string gameCount = context.count > 1 ? $"s ({context.count} games)" : "";
                return 
                (
                    context.allEmpty            ? "Install selected game cache" + gameCount :
                    context.allPaused           ? "Resume Install selected game cache" + gameCount :
                    context.allEmptyOrPaused    ? "Install / Resume Install selected game cache" + gameCount
                                                : null
                );
            }
            else
            {
                return null;
            }
        }

        private string GetRerootCachesMenu(SelectedCachesContext context)
        {
            if (MultipleCacheRoots && context.count > 0 && context.allEmpty)
            {
                string gameCount = context.count > 1 ? $"s ({context.count} games)" : "";
                return $"Change cache root of selected game{gameCount}";
            }
            else
            {
                return null;
            }
        }

        private List<MenuItem> GetRerootCachesSubMenuItems()
        {
            var subMenuItems = new List<MenuItem>();
            foreach (var cacheRoot in plugin.cacheManager.CacheRoots)
            {
                subMenuItems.Add(new MenuItem()
                {
                    Header = cacheRoot.Directory,
                    Command = new RelayCommand(() => RerootSelectedCaches(SelectedGameCaches, cacheRoot))
                });
            }
            return subMenuItems;
        }

        private string GetUninstallCachesMenu(SelectedCachesContext context)
        {
            if (context.count > 0)
            {
                string gameCount = context.count > 1 ? $"s ({context.count} games)" : "";
                return context.allInstalledPausedUnknownOrInvalid ? "Uninstall selected game cache" + gameCount : null;
            }
            else
            {
                return null;
            }
        }

        private string GetDisableCachesMenu(SelectedCachesContext context)
        {
            if (context.count > 0)
            {
                string gameCount = context.count > 1 ? $"s ({context.count} games)" : "";
                return context.allEmpty ? "Disable caching for selected game" + gameCount : null;
            }
            else
            {
                return null;
            }
        }

        public void InstallSelectedCaches(List<GameCacheViewModel> gameCaches)
        {
            plugin.panelView.GameCaches_ClearSelected();
            foreach (var gameCache in gameCaches)
            {
                InstallGameCache(gameCache);
            }
        }

        public void UninstallSelectedCaches(List<GameCacheViewModel> gameCaches)
        {
            plugin.panelView.GameCaches_ClearSelected();
            foreach (var gameCache in gameCaches)
            {
                UninstallGameCache(gameCache);
            }
        }

        private void RerootSelectedCaches(List<GameCacheViewModel> gameCaches, CacheRootViewModel cacheRoot)
        {
            plugin.panelView.GameCaches_ClearSelected();
            foreach (var gameCache in gameCaches)
            {
                var nowPlayingGame = plugin.FindNowPlayingGame(gameCache.Id);
                if (nowPlayingGame != null)
                {
                    plugin.EnableNowPlayingWithRoot(nowPlayingGame, cacheRoot);
                }
                else
                {
                    plugin.PopupError($"Can't find NowPlaying enabled game '{gameCache.Title}'.");
                }
            }
            plugin.cacheManager.SaveGameCacheEntriesToJson();
        }

        private void DisableSelectedCaches(List<GameCacheViewModel> gameCaches)
        {
            plugin.panelView.GameCaches_ClearSelected();
            foreach (var gameCache in gameCaches)
            {
                DisableGameCache(gameCache);
            }
        }

        public void InstallGameCache(GameCacheViewModel gameCache)
        {
            Game nowPlayingGame = plugin.FindNowPlayingGame(gameCache.entry.Id);
            if (nowPlayingGame != null)
            {
                NowPlayingInstallController controller = new NowPlayingInstallController(plugin, nowPlayingGame, gameCache);
                nowPlayingGame.IsInstalling = true;
                controller.Install(new InstallActionArgs());
            }
            else
            {
                plugin.PopupError($"Can't find NowPlaying Game for {gameCache.entry.Title}");
            }
        }

        public void UninstallGameCache(GameCacheViewModel gameCache)
        {
            Game nowPlayingGame = plugin.FindNowPlayingGame(gameCache.Id);
            if (nowPlayingGame != null)
            {
                NowPlayingUninstallController controller = new NowPlayingUninstallController(plugin, nowPlayingGame, gameCache);
                nowPlayingGame.IsUninstalling = true;
                controller.Uninstall(new UninstallActionArgs());
            }
            else
            {
                plugin.PopupError($"Can't find NowPlaying Game for {gameCache.Title}");
            }
        }

        public void DisableGameCache(GameCacheViewModel gameCache)
        {
            Game nowPlayingGame = plugin.FindNowPlayingGame(gameCache.Id);
            if (nowPlayingGame != null)
            {
                plugin.DisableNowPlayingGameCaching(nowPlayingGame, gameCache.InstallDir, gameCache.ExePath);
                plugin.cacheManager.RemoveGameCache(gameCache.Id);
                plugin.NotifyInfo($"Game caching disabled for '{nowPlayingGame.Name}'");
            }
            else
            {
                plugin.PopupError($"Can't find NowPlaying Game for {gameCache.Title}");
            }

        }

    }
}

using NowPlaying.Utils;
using NowPlaying.Models;
using NowPlaying.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using MenuItem = System.Windows.Controls.MenuItem;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Media.Imaging;
using NowPlaying.Properties;

namespace NowPlaying.ViewModels
{
    public class NowPlayingPanelViewModel : ViewModelBase
    {
        private readonly ILogger logger = NowPlaying.logger;
        private readonly NowPlaying plugin;
        private readonly BitmapImage rootsIcon;

        public BitmapImage RootsIcon => rootsIcon;

        public NowPlayingPanelViewModel(NowPlaying plugin)
        {
            this.plugin = plugin;
            this.CustomEtaSort = new CustomEtaSorter();
            this.CustomSizeSort = new CustomSizeSorter();
            this.CustomSpaceAvailableSort = new CustomSpaceAvailableSorter();
            this.isTopPanelVisible = false;
            this.showSettings = false;
            this.showCacheRoots = false;
            this.SelectedGameCaches = new List<GameCacheViewModel>();
            this.selectionContext = new SelectedCachesContext();
            this.RerootCachesSubMenuItems = new List<MenuItem>();

            this.rootsIcon = ImageUtils.BitmapToBitmapImage(Resources.roots_icon);

            this.RefreshCachesCommand = new RelayCommand(() => RefreshGameCaches());
            this.InstallCachesCommand = new RelayCommand(() => InstallSelectedCaches(SelectedGameCaches), () => InstallCachesCanExecute);
            this.UninstallCachesCommand = new RelayCommand(() => UninstallSelectedCaches(SelectedGameCaches), () => UninstallCachesCanExecute);
            this.DisableCachesCommand = new RelayCommand(() => DisableSelectedCaches(SelectedGameCaches), () => DisableCachesCanExecute);
            this.RerootClickCanExecute = new RelayCommand(() => { }, () => RerootCachesCanExecute);
            this.PauseInstallCommand = new RelayCommand(() =>
            {
                if (InstallProgressView != null)
                {
                    var viewModel = installProgressView.DataContext as InstallProgressViewModel;
                    viewModel.PauseInstallCommand.Execute();
                    plugin.panelView.GameCaches_ClearSelected();
                }
            });
            this.CancelInstallCommand = new RelayCommand(() =>
            {
                if (InstallProgressView != null)
                {
                    var viewModel = installProgressView.DataContext as InstallProgressViewModel;
                    viewModel.CancelInstallCommand.Execute();
                    plugin.panelView.GameCaches_ClearSelected();
                }
            });

            this.CancelQueuedInstallsCommand = new RelayCommand(() =>
            {
                foreach (var gc in SelectedGameCaches)
                {
                    plugin.CancelQueuedInstaller(gc.Id);
                }
            });

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
                        logger.Error(error);
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
                popup.Width = view.MinWidth;
                popup.MinWidth = view.MinWidth;
                popup.Height = view.MinHeight;
                popup.MinHeight = view.MinHeight;
                popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
                popup.ContentRendered += (s, e) => viewModel.SelectNoGames();  // clear auto-selection of 1st item
                popup.ShowDialog();
            });

            // . track game cache list changes, in order to auto-adjust title column width 
            this.GameCaches.CollectionChanged += GameCaches_CollectionChanged;
        }

        private void GameCaches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            plugin.panelView.GameCaches_ClearSelected();
            plugin.panelView.GameCaches_AutoResizeTitleColumn();
        }

        public class CustomEtaSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                double etaX = ((GameCacheViewModel)x).InstallEtaTimeSpan.TotalSeconds;
                double etaY = ((GameCacheViewModel)y).InstallEtaTimeSpan.TotalSeconds;
                return etaX.CompareTo(etaY);
            }
        }
        public CustomEtaSorter CustomEtaSort { get; private set; }

        public class CustomSizeSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                long cacheSizeX   = ((GameCacheViewModel)x).CacheSize;
                long installSizeX = ((GameCacheViewModel)x).InstallSize;
                long cacheSizeY   = ((GameCacheViewModel)y).CacheSize;
                long installSizeY = ((GameCacheViewModel)y).InstallSize;
                long sizeX = cacheSizeX > 0 ? cacheSizeX : -installSizeX;
                long sizeY = cacheSizeY > 0 ? cacheSizeY : -installSizeY;
                return sizeX.CompareTo(sizeY);
            }
        }
        public CustomSizeSorter CustomSizeSort { get; private set; }

        public class CustomSpaceAvailableSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                long spaceX = ((GameCacheViewModel)x).cacheRoot.BytesAvailableForCaches;
                long spaceY = ((GameCacheViewModel)y).cacheRoot.BytesAvailableForCaches;
                return spaceX.CompareTo(spaceY);
            }
        }
        public CustomSpaceAvailableSorter CustomSpaceAvailableSort { get; private set; }


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
        public ICommand CancelQueuedInstallsCommand { get; private set; }
        public ICommand PauseInstallCommand { get; private set; }
        public ICommand CancelInstallCommand { get; private set; }

        public ObservableCollection<GameCacheViewModel> GameCaches => plugin.cacheManager.GameCaches;

        public string TitleWidth { get; private set; } = "NaN";

        public bool AreCacheRootsNonEmpty => plugin.cacheManager.CacheRoots.Count > 0;
        public bool MultipleCacheRoots => plugin.cacheManager.CacheRoots.Count > 1;
        public string GameCachesVisibility => AreCacheRootsNonEmpty ? "Visible" : "Collapsed";
        public string MultipleRootsVisibility => MultipleCacheRoots ? "Visible" : "Collapsed";
        public string GameCachesRootColumnWidth => MultipleCacheRoots ? "55" : "0";

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
        public string CancelQueuedInstallsMenu { get; private set; }
        public string CancelQueuedInstallsVisibility { get; private set; }
        public string PauseInstallMenu { get; private set; }
        public string PauseInstallVisibility { get; private set; }
        public string CancelInstallMenu { get; private set; }
        public string CancelInstallVisibility { get; private set; }


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
                SelectionContext?.UpdateContext(selectedGameCaches);

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

                CancelQueuedInstallsMenu = GetCancelQueuedInstallsMenu(SelectionContext);
                CancelQueuedInstallsVisibility = CancelQueuedInstallsMenu != null ? "Visible" : "Collapsed";
                OnPropertyChanged(nameof(CancelQueuedInstallsMenu));
                OnPropertyChanged(nameof(CancelQueuedInstallsVisibility));

                PauseInstallMenu = GetPauseInstallMenu(SelectionContext);
                PauseInstallVisibility = PauseInstallMenu != null ? "Visible" : "Collapsed";
                OnPropertyChanged(nameof(PauseInstallMenu));
                OnPropertyChanged(nameof(PauseInstallVisibility));

                CancelInstallMenu = GetCancelInstallMenu(SelectionContext);
                CancelInstallVisibility = CancelInstallMenu != null ? "Visible" : "Collapsed";
                OnPropertyChanged(nameof(CancelInstallMenu));
                OnPropertyChanged(nameof(CancelInstallVisibility));
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

        public string ShowCacheRootsToolTip => plugin.GetResourceString(showCacheRoots ? "LOCNowPlayingHideCacheRoots" : "LOCNowPlayingShowCacheRoots");

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
                        plugin.cacheRootsViewModel.RefreshCacheRoots();
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

        public string ShowSettingsToolTip => plugin.GetResourceString(showSettings ? "LOCNowPlayingHideSettings" : "LOCNowPlayingShowSettings");

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
            plugin.cacheManager.UpdateGameCaches();
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
            public bool allQueuedForInstall;
            public bool allInstalledOrPaused;
            public bool allInstalledPausedUnknownOrInvalid;
            public bool allWillFit;
            public int count;

            public SelectedCachesContext()
            {
                ResetContext();
            }

            private void ResetContext()
            {
                allEmpty = true;
                allPaused = true;
                allInstalled = true;
                allInstalling = true;
                allEmptyOrPaused = true;
                allQueuedForInstall = true;
                allInstalledOrPaused = true;
                allInstalledPausedUnknownOrInvalid = true;
                allWillFit = true;
                count = 0;
            }

            public void UpdateContext(List<GameCacheViewModel> gameCaches)
            {
                ResetContext();
                foreach (var gc in gameCaches)
                {
                    bool isInstallingOrQueued = gc.NowInstalling == true || gc.InstallQueueStatus != null;
                    bool isQueuedForInstall = gc.InstallQueueStatus != null;
                    bool isEmpty = gc.State == GameCacheState.Empty && !isInstallingOrQueued;
                    bool isPaused = gc.State == GameCacheState.InProgress && !isInstallingOrQueued;
                    bool isUnknown = gc.State == GameCacheState.Unknown;
                    bool isInvalid = gc.State == GameCacheState.Invalid;
                    bool isInstalled = gc.State == GameCacheState.Populated || gc.State == GameCacheState.Played;
                    bool isInstalling = gc.State == GameCacheState.InProgress && gc.NowInstalling == true;
                    allEmpty &= isEmpty;
                    allPaused &= isPaused;
                    allInstalled &= isInstalled;
                    allInstalling &= isInstalling;
                    allEmptyOrPaused &= isEmpty || isPaused;
                    allQueuedForInstall &= isQueuedForInstall;
                    allInstalledOrPaused &= isInstalled || isPaused;
                    allInstalledPausedUnknownOrInvalid &= isInstalled || isPaused || isUnknown || isInvalid;
                    allWillFit &= gc.CacheWillFit;
                    count++;
                }
            }
        }

        private string GetInstallCachesMenu(SelectedCachesContext context)
        {
            if (context != null && context.count > 0 && context.allWillFit)
            {
                if (context.count > 1)
                {
                    return plugin.FormatResourceString
                    (
                        context.allEmpty ? "LOCNowPlayingInstallGameCachesFmt" :
                        context.allPaused ? "LOCNowPlayingResumeGameCachesFmt" :
                        context.allEmptyOrPaused ? "LOCNowPlayingInstallOrResumeGameCachesFmt" : null,
                        context.count
                    );
                }
                else
                {
                    return plugin.GetResourceString
                    (
                        context.allEmpty ? "LOCNowPlayingInstallGameCache" :
                        context.allPaused ? "LOCNowPlayingResumeGameCache" :
                        context.allEmptyOrPaused ? "LOCNowPlayingInstallOrResumeGameCache" : null
                    );
                }
            }
            else
            {
                return null;
            }
        }

        private string GetRerootCachesMenu(SelectedCachesContext context)
        {
            if (MultipleCacheRoots && context != null && context.count > 0 && context.allEmpty)
            {
                if (context.count > 1)
                {
                    return plugin.FormatResourceString("LOCNowPlayingRerootGameCachesFmt", context.count);
                }
                else
                {
                    return plugin.GetResourceString("LOCNowPlayingRerootGameCache");
                }
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
            if (context != null && context.count > 0 && context.allInstalledPausedUnknownOrInvalid)
            {
                if (context.count > 1)
                {
                    return plugin.FormatResourceString("LOCNowPlayingUninstallGameCachesFmt", context.count);
                }
                else
                {
                    return plugin.GetResourceString("LOCNowPlayingUninstallGameCache");
                }
            }
            else
            {
                return null;
            }
        }

        private string GetDisableCachesMenu(SelectedCachesContext context)
        {
            if (context != null && context.count > 0 && context.allEmpty)
            {
                if (context.count > 1)
                {
                    return plugin.FormatResourceString("LOCNowPlayingDisableGameCachesFmt", context.count);
                }
                else
                {
                    return plugin.GetResourceString("LOCNowPlayingDisableGameCache");
                }
            }
            else
            {
                return null;
            }
        }

        private string GetCancelQueuedInstallsMenu(SelectedCachesContext context)
        {
            if (context != null && context.count > 0 && context.allQueuedForInstall)
            {
                if (context.count > 1)
                {
                    return plugin.FormatResourceString("LOCNowPlayingCancelQueuedInstallsFmt", context.count);
                }
                else
                {
                    return plugin.GetResourceString("LOCNowPlayingCancelQueuedInstall");
                }
            }
            else
            {
                return null;
            }
        }

        private string GetPauseInstallMenu(SelectedCachesContext context)
        {
            if (context != null && context.count > 0 && context.allInstalling)
            {
                if (context.count > 1)
                {
                    return plugin.FormatResourceString("LOCNowPlayingPauseInstallsFmt", context.count);
                }
                else
                {
                    return plugin.GetResourceString("LOCNowPlayingPauseInstall");
                }
            }
            else
            {
                return null;
            }
        }

        private string GetCancelInstallMenu(SelectedCachesContext context)
        {
            if (context != null && context.count > 0 && context.allInstalling)
            {
                if (context.count > 1)
                {
                    return plugin.FormatResourceString("LOCNowPlayingCancelInstallsFmt", context.count);
                }
                else
                {
                    return plugin.GetResourceString("LOCNowPlayingCancelInstall");
                }
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
            bool saveNeeded = false;
            plugin.panelView.GameCaches_ClearSelected();
            foreach (var gameCache in gameCaches)
            {
                var nowPlayingGame = plugin.FindNowPlayingGame(gameCache.Id);
                if (nowPlayingGame != null)
                {
                    plugin.EnableNowPlayingWithRoot(nowPlayingGame, cacheRoot);
                    saveNeeded = true;
                }
                else
                {
                    // NowPlaying game missing (removed from playnite)
                    // . delete cache dir if it exists and disable game caching
                    //
                    plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingMsgGameNotFoundDisablingFmt", gameCache.Title));
                    if (Directory.Exists(gameCache.CacheDir))
                    {
                        Task.Run(() => DirectoryUtils.DeleteDirectory(gameCache.CacheDir, maxRetries: 50));
                    }
                    plugin.cacheManager.RemoveGameCache(gameCache.Id);
                }
            }
            if (saveNeeded)
            {
                plugin.cacheManager.SaveGameCacheEntriesToJson();
            }
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
                NowPlayingInstallController controller = new NowPlayingInstallController(plugin, nowPlayingGame, gameCache, plugin.SpeedLimitIPG);
                nowPlayingGame.IsInstalling = true;
                controller.Install(new InstallActionArgs());
            }
            else
            {
                // NowPlaying game missing (removed from playnite)
                // . delete cache if it exists and disable game caching
                //
                if (Directory.Exists(gameCache.CacheDir))
                {
                    if (gameCache.CacheSize > 0)
                    {
                        plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingMsgGameNotFoundDisableDeleteFmt", gameCache.Title));
                    }
                    else
                    {
                        plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingMsgGameNotFoundDisablingFmt", gameCache.Title));
                    }
                    Task.Run(() => DirectoryUtils.DeleteDirectory(gameCache.CacheDir, maxRetries: 50));
                }
                else
                {
                    plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingMsgGameNotFoundDisablingFmt", gameCache.Title));
                }
                plugin.cacheManager.RemoveGameCache(gameCache.Id);
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
                // NowPlaying game missing (removed from playnite)
                // . delete cache if it exists and disable game caching
                //
                if (Directory.Exists(gameCache.CacheDir))
                {
                    if (gameCache.CacheSize > 0)
                    {
                        plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingMsgGameNotFoundDisableDeleteFmt", gameCache.Title));
                    }
                    else
                    {
                        plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingMsgGameNotFoundDisablingFmt", gameCache.Title));
                    }
                    Task.Run(() => DirectoryUtils.DeleteDirectory(gameCache.CacheDir, maxRetries: 50));
                }
                else
                {
                    plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingMsgGameNotFoundDisablingFmt", gameCache.Title));
                }
                plugin.cacheManager.RemoveGameCache(gameCache.Id);
            }
        }

        public void DisableGameCache(GameCacheViewModel gameCache)
        {
            Game nowPlayingGame = plugin.FindNowPlayingGame(gameCache.Id);
            if (nowPlayingGame != null)
            {
                plugin.DisableNowPlayingGameCaching(nowPlayingGame, gameCache.InstallDir, gameCache.ExePath, gameCache.XtraArgs);
            }
            else
            {
                // . missing NowPlaying game; delete game cache dir on the way out.
                if (Directory.Exists(gameCache.CacheDir))
                {
                    Task.Run(() => DirectoryUtils.DeleteDirectory(gameCache.CacheDir, maxRetries: 50));
                }
            }
            plugin.cacheManager.RemoveGameCache(gameCache.Id);
            plugin.NotifyInfo(plugin.FormatResourceString("LOCNowPlayingMsgGameCachingDisabledFmt", gameCache.Title));
        }

    }
}

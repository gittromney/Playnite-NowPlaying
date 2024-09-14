using NowPlaying.Utils;
using NowPlaying.Models;
using NowPlaying.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
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
using System.Windows;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Controls;

namespace NowPlaying.ViewModels
{
    public class NowPlayingPanelViewModel : ViewModelBase
    {
        private readonly ILogger logger = NowPlaying.logger;
        private readonly NowPlaying plugin;
        private readonly BitmapImage rootsIcon;
        private readonly BitmapImage sidebarIcon;

        private bool modalDimming = false;
        public bool ModalDimming
        { 
            get => modalDimming;
            set
            {
                if (value != modalDimming)
                {
                    modalDimming = value;
                    OnPropertyChanged(nameof(ModalDimmingVisibility));
                }
            }
        }
        public string ModalDimmingVisibility => ModalDimming ? "Visible" : "Hidden";

        public BitmapImage RootsIcon => rootsIcon;
        public BitmapImage SidebarIcon => sidebarIcon;

        public NowPlayingPanelViewModel(NowPlaying plugin)
        {
            this.plugin = plugin;
            this.CustomPlatformSort = new CustomPlatformSorter();
            this.CustomEtaSort = new CustomEtaSorter();
            this.CustomSizeSort = new CustomSizeSorter();
            this.CustomSpaceAvailableSort = new CustomSpaceAvailableSorter();
            this.isTopPanelVisible = false;
            this.showSettings = false;
            this.showCacheRoots = false;
            this.gameCaches = new ObservableCollection<GameCacheViewModel>();
            this.SelectedGameCaches = new List<GameCacheViewModel>();
            this.selectionContext = new SelectedCachesContext();
            this.RerootCachesSubMenuItems = new List<MenuItem>();

            this.rootsIcon = ImageUtils.BitmapToBitmapImage(Resources.roots_icon);
            this.sidebarIcon = ImageUtils.BitmapToBitmapImage(Resources.now_playing_icon);

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

                // tweak window to make it captionless (no area reserved for title, min/max/close buttons)
                view.Loaded += plugin.panelViewModel.MakeWindowCaptionlessOnUserControlLoaded;
                var captionHeight = 0; // window.Heights may be adjusted if window can't be made 'captionless'

                // setup popup and center within the current application window
                if (viewModel.EligibleGamesExist)
                {
                    // popup content: list view + controls to enable eligible games
                    popup.Width = view.MinWidth;
                    popup.MinWidth = view.MinWidth;
                    popup.Height = view.MinHeight + captionHeight;
                    popup.MinHeight = view.MinHeight + captionHeight;
                }
                else
                {
                    // popup content: no further eligible games warning/info 
                    // -> make window smaller to fit content and fixed size
                    var style = (Style)view.TryFindResource("FixedSizeWindow");
                    if (style != null)
                    {
                        popup.Style = style;
                    }
                    view.MinWidth = 800;
                    view.MinHeight = 500;
                    popup.Width = view.MinWidth;
                    popup.MinWidth = view.MinWidth;
                    popup.MaxWidth = view.MinWidth;
                    popup.Height = view.MinHeight + captionHeight;
                    popup.MinHeight = view.MinHeight + captionHeight;
                    popup.MaxHeight = view.MinHeight + captionHeight;
                }
                popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
                popup.ContentRendered += (s, e) =>
                {
                    // . clear auto-selection of 1st item
                    viewModel.SelectNoGames();
                    GridViewUtils.ColumnResize(view.EligibleGames);
                };
                ModalDimming = true;
                popup.ShowDialog();
            });

            // . track game cache list changes, in order to auto-adjust title column width, refresh sorting, etc.
            plugin.cacheManager.GameCaches.CollectionChanged += GameCaches_CollectionChanged;
        }

        public void MakeWindowCaptionlessOnUserControlLoaded(object sender, RoutedEventArgs e)
        {
            var window = GridViewUtils.GetRootAncestor(sender as UserControl) as Window;
            var contentPresenter = GridViewUtils.GetAncestor<ContentPresenter>(sender as UserControl);
            var titleText = GridViewUtils.GetChildByName(window, "PART_TextTitle", skipThisObj: contentPresenter) as TextBlock;

            if (contentPresenter != null && titleText != null)
            {
                // . collapse the window's Title property, if found
                //  (note: this will collapse the caption region since control buttons are already collapsed)
                titleText.Visibility = Visibility.Collapsed;

                // . clear our ContentPresenter's Margin.Top, so the whole window area can be used
                var margin = contentPresenter.Margin;
                margin.Top = 0;
                contentPresenter.Margin = margin;
            }

            else if (window != null)
            {
                // . adjust the window's height assuming a Captioned window 
                window.Height += 2 * SystemParameters.WindowCaptionHeight;
                window.MinHeight += 2 * SystemParameters.WindowCaptionHeight;
                window.MaxHeight += 2 * SystemParameters.WindowCaptionHeight;
            }
        }

        private void GameCaches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(SearchText) == false)
            {
                RefreshSearchableGameCaches(SearchText);
            }
            else
            {
                // . resize columns only if we're viewing all games (empty search text)
                GridViewUtils.ColumnResize(plugin.panelView.GameCaches);
                plugin.panelView.GameCaches_ClearSelected();
            }
        }

        public class CustomPlatformSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . primary sort: by "Platform", reversable 
                string platformX = ((GameCacheViewModel)(reverse ? y : x)).Platform.ToString();
                string platformY = ((GameCacheViewModel)(reverse ? x : y)).Platform.ToString();

                // . secondary sort: by "Title", always ascending
                string titleX = ((GameCacheViewModel)x).Title;
                string titleY = ((GameCacheViewModel)y).Title;

                return platformX != platformY ? platformX.CompareTo(platformY) : titleX.CompareTo(titleY);
            }
        }

        public CustomPlatformSorter CustomPlatformSort { get; private set; }

        public class CustomEtaSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . primary sort: by "ETA", reversable 
                double etaX = ((GameCacheViewModel)(reverse ? y : x)).InstallEtaTimeSpan.TotalSeconds;
                double etaY = ((GameCacheViewModel)(reverse ? x : y)).InstallEtaTimeSpan.TotalSeconds;

                // . secondary sort: by "Title", always ascending
                string titleX = ((GameCacheViewModel)x).Title;
                string titleY = ((GameCacheViewModel)y).Title;

                return etaX != etaY ? etaX.CompareTo(etaY) : titleX.CompareTo(titleY);
            }
        }
        public CustomEtaSorter CustomEtaSort { get; private set; }

        public class CustomSizeSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . primary sort: by Cache/Install size, reversable
                var objX = (GameCacheViewModel)(reverse ? y : x);
                var objY = (GameCacheViewModel)(reverse ? x : y);
                long cacheSizeX   = objX.CacheSize;
                long installSizeX = objX.InstallSize;
                long cacheSizeY   = objY.CacheSize;
                long installSizeY = objY.InstallSize;
                long sizeX = cacheSizeX > 0 ? cacheSizeX : Int64.MinValue + installSizeX;
                long sizeY = cacheSizeY > 0 ? cacheSizeY : Int64.MinValue + installSizeY;

                // . secondary sort: by "Title", always ascending
                string titleX = ((GameCacheViewModel)x).Title;
                string titleY = ((GameCacheViewModel)y).Title;

                return sizeX != sizeY ? sizeX.CompareTo(sizeY) : titleX.CompareTo(titleY);
            }
        }
        public CustomSizeSorter CustomSizeSort { get; private set; }

        public class CustomSpaceAvailableSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . primary sort: by available space for caches, reversable
                long spaceX = ((GameCacheViewModel)(reverse ? y : x)).cacheRoot.BytesAvailableForCaches;
                long spaceY = ((GameCacheViewModel)(reverse ? x : y)).cacheRoot.BytesAvailableForCaches;

                // . secondary sort: by "Title", always ascending
                string titleX = ((GameCacheViewModel)x).Title;
                string titleY = ((GameCacheViewModel)y).Title;

                return spaceX != spaceY ? spaceX.CompareTo(spaceY) : titleX.CompareTo(titleY);
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
        
        public ObservableCollection<GameCacheViewModel> gameCaches;
        public ObservableCollection<GameCacheViewModel> GameCaches => gameCaches;

        private void RefreshSearchableGameCaches(string searchText, bool searchTextUpdated = false)
        {
            var useFilteredGamesList = string.IsNullOrWhiteSpace(searchText) == false;
            if (searchTextUpdated)
            {
                Task.Run(() =>
                {
                    Task.Delay(100);  // time slot for UI search box text update before list view is updated/sorted/etc.

                    plugin.panelView.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(() =>
                    {
                        if (useFilteredGamesList)
                        {
                            gameCaches = plugin.cacheManager.GameCaches.Where(g => g.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToObservable();
                        }
                        else
                        {
                            gameCaches = plugin.cacheManager.GameCaches;
                        }
                        OnPropertyChanged(nameof(GameCaches));
                        plugin.panelView.GameCaches_ClearSelected();
                        GridViewUtils.RefreshSort(plugin.panelView.GameCaches);
                    }));
                });
            }
            else
            {
                if (useFilteredGamesList)
                {
                    gameCaches = plugin.cacheManager.GameCaches.Where(g => g.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToObservable();
                }
                else
                {
                    gameCaches = plugin.cacheManager.GameCaches;
                }
                OnPropertyChanged(nameof(GameCaches));
                plugin.panelView.GameCaches_ClearSelected();
                GridViewUtils.RefreshSort(plugin.panelView.GameCaches);
            }
        }

        public bool AreCacheRootsNonEmpty => plugin.cacheManager.CacheRoots.Count > 0;
        public bool MultipleCacheRoots => plugin.cacheManager.CacheRoots.Count > 1;
        public string GameCachesVisibility => AreCacheRootsNonEmpty ? "Visible" : "Collapsed";
        public string MultipleRootsVisibility => MultipleCacheRoots ? "Visible" : "Collapsed";
        public string GameCachesRootColumnWidth => MultipleCacheRoots ? "55" : "0";


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
                    RefreshSearchableGameCaches(searchText, searchTextUpdated: true);
                }
            }
        }
        public bool ShowTitleColumn
        {
            get => plugin.Settings.ShowGameCacheTitle;
            set
            {
                if (plugin.Settings.ShowGameCacheTitle != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCacheTitle = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowPlatformColumn
        {
            get => plugin.Settings.ShowGameCachePlatform;
            set
            {
                if (plugin.Settings.ShowGameCachePlatform != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCachePlatform = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowSourceDirColumn
        {
            get => plugin.Settings.ShowGameCacheSourceDir;
            set
            {
                if (plugin.Settings.ShowGameCacheSourceDir != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCacheSourceDir = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowStatusColumn
        {
            get => plugin.Settings.ShowGameCacheStatus;
            set
            {
                if (plugin.Settings.ShowGameCacheStatus != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCacheStatus = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowCanInstallColumn
        {
            get => plugin.Settings.ShowGameCacheCanInstall;
            set
            {
                if (plugin.Settings.ShowGameCacheCanInstall != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCacheCanInstall = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowInstallEtaColumn
        {
            get => plugin.Settings.ShowGameCacheInstallEta;
            set
            {
                if (plugin.Settings.ShowGameCacheInstallEta != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCacheInstallEta = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowSizeColumn
        {
            get => plugin.Settings.ShowGameCacheSize;
            set
            {
                if (plugin.Settings.ShowGameCacheSize != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCacheSize = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowRootColumn
        {
            get => plugin.Settings.ShowGameCacheRoot;
            set
            {
                if (plugin.Settings.ShowGameCacheRoot != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCacheRoot = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowSpaceAvailColumn
        {
            get => plugin.Settings.ShowGameCacheSpaceAvail;
            set
            {
                if (plugin.Settings.ShowGameCacheSpaceAvail != value)
                {
                    plugin.settingsViewModel.Settings.ShowGameCacheSpaceAvail = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }

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
                    Command = new RelayCommand(() => RerootSelectedCachesAsync(SelectedGameCaches, cacheRoot))
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

        private async void RerootSelectedCachesAsync(List<GameCacheViewModel> gameCaches, CacheRootViewModel cacheRoot)
        {
            bool saveNeeded = false;
            plugin.panelView.GameCaches_ClearSelected();
            foreach (var gameCache in gameCaches)
            {
                var nowPlayingGame = plugin.FindNowPlayingGame(gameCache.Id);
                if (nowPlayingGame != null)
                {
                    await plugin.EnableNowPlayingWithRootAsync(nowPlayingGame, cacheRoot);
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
                        if(!await Task.Run(() => DirectoryUtils.DeleteDirectory(gameCache.CacheDir, maxRetries: 50)))
                        {
                            plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingDeleteCacheFailedFmt", gameCache.Title));
                        }
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
                NowPlayingInstallController controller = new NowPlayingInstallController(plugin, nowPlayingGame, gameCache, plugin.SpeedLimitIpg);
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

        public void UninstallGameCache(GameCacheViewModel gameCache, bool andThenDisableCaching = false)
        {
            Game nowPlayingGame = plugin.FindNowPlayingGame(gameCache.Id);
            if (nowPlayingGame != null)
            {
                NowPlayingUninstallController controller = new NowPlayingUninstallController(plugin, nowPlayingGame, gameCache, andThenDisableCaching);
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

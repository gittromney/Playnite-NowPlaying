using NowPlaying.Extensions;
using NowPlaying.Utils;
using NowPlaying.Views;
using Playnite.SDK;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace NowPlaying.ViewModels
{
    public class CacheRootsViewModel : ViewModelBase
    {
        private readonly NowPlaying plugin;
        public NowPlayingSettings Settings => plugin.Settings;
        public ThemeResources Theme => plugin.themeResources;

        public ICommand RefreshRootsCommand { get; private set; }
        public ICommand AddCacheRootCommand { get; private set; }
        public ICommand EditMaxFillCommand { get; private set; }
        public ICommand RemoveCacheRootCommand { get; private set; }

        public ColumnSortableCollection<CacheRootViewModel> CacheRoots => plugin.cacheManager.CacheRoots;
        public string SortedColumnName { get; set; }
        public CacheRootViewModel SelectedCacheRoot { get; set; }

        public string EmptyRootsVisible => CacheRoots.Count > 0 ? "Collapsed" : "Visible";
        public string NonEmptyRootsVisible => CacheRoots.Count > 0 ? "Visible" : "Collapsed";

        public bool ShowDeviceColumn
        {
            get => plugin.Settings.ShowRootDevice;
            set
            {
                if (plugin.Settings.ShowRootDevice != value)
                {
                    plugin.settingsViewModel.Settings.ShowRootDevice = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowDirectoryColumn
        {
            get => plugin.Settings.ShowRootDirectory;
            set
            {
                if (plugin.Settings.ShowRootDirectory != value)
                {
                    plugin.settingsViewModel.Settings.ShowRootDirectory = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowSpaceAvailColumn
        {
            get => plugin.Settings.ShowRootSpaceAvail;
            set
            {
                if (plugin.Settings.ShowRootSpaceAvail != value)
                {
                    plugin.settingsViewModel.Settings.ShowRootSpaceAvail = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowGamesColumn
        {
            get => plugin.Settings.ShowRootGames;
            set
            {
                if (plugin.Settings.ShowRootGames != value)
                {
                    plugin.settingsViewModel.Settings.ShowRootGames = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowInstalledColumn
        {
            get => plugin.Settings.ShowRootInstalled;
            set
            {
                if (plugin.Settings.ShowRootInstalled != value)
                {
                    plugin.settingsViewModel.Settings.ShowRootInstalled = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }
        public bool ShowMaxFillColumn
        {
            get => plugin.Settings.ShowRootMaxFill;
            set
            {
                if (plugin.Settings.ShowRootMaxFill != value)
                {
                    plugin.settingsViewModel.Settings.ShowRootMaxFill = value;
                    plugin.settingsViewModel.EndEdit();
                    OnPropertyChanged();
                }
            }
        }

        public CacheRootsViewModel(NowPlaying plugin)
        {
            this.plugin = plugin;
            this.CustomSpaceAvailableSort = new CustomSpaceAvailableSorter();
            this.CustomCachesInstalledSort = new CustomCachesInstalledSorter();
            this.CustomMaxFillReservedSort = new CustomMaxFillReservedSorter();

            AddCacheRootCommand = new RelayCommand(() =>
            {
                var appWindow = plugin.PlayniteApi.Dialogs.GetCurrentAppWindow();
                var popup = plugin.PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions()
                {
                    ShowCloseButton = false,
                    ShowMaximizeButton = false,
                    ShowMinimizeButton = false
                });
                var viewModel = new AddCacheRootViewModel(plugin, popup, isFirstAdded: CacheRoots.Count == 0);
                var view = new AddCacheRootView(viewModel);
                popup.Content = view;

                // tweak window to make it fixed size and headless (no area reserved for title, min/max/close buttons)
                view.Loaded += plugin.panelViewModel.MakeWindowCaptionlessOnUserControlLoaded;
                var style = (Style)view.TryFindResource("FixedSizeWindow");
                if (style != null)
                {
                    popup.Style = style;
                }

                // setup up popup and center within the current application window
                popup.SizeToContent = SizeToContent.WidthAndHeight;
                popup.SizeChanged += (s, e) => plugin.panelViewModel.CenterSizeManagedPopupWindow(appWindow, popup);
                plugin.panelViewModel.ModalDimming = true;
                popup.ShowDialog();
            });

            EditMaxFillCommand = new RelayCommand(
                () => {
                    var appWindow = plugin.PlayniteApi.Dialogs.GetCurrentAppWindow();
                    var popup = plugin.PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions()
                    {
                        ShowCloseButton = false,
                        ShowMaximizeButton = false,
                        ShowMinimizeButton = false
                    });
                    var viewModel = new EditMaxFillViewModel(plugin, popup, SelectedCacheRoot);
                    var view = new EditMaxFillView(viewModel);
                    popup.Content = view;

                    // tweak window to make it fixed size and headless (no area reserved for title, min/max/close buttons)
                    view.Loaded += plugin.panelViewModel.MakeWindowCaptionlessOnUserControlLoaded;
                    var style = (Style)view.TryFindResource("FixedSizeWindow");
                    if (style != null)
                    {
                        popup.Style = style;
                    }

                    // setup up popup and center within the current application window
                    popup.SizeToContent = SizeToContent.WidthAndHeight;
                    popup.SizeChanged += (s, e) => plugin.panelViewModel.CenterSizeManagedPopupWindow(appWindow, popup);
                    plugin.panelViewModel.ModalDimming = true;
                    popup.ShowDialog();
                },
                // CanExecute
                () => SelectedCacheRoot != null
            );

            RemoveCacheRootCommand = new RelayCommand(
                () => {
                    plugin.cacheManager.RemoveCacheRoot(SelectedCacheRoot.Directory);
                    RefreshCacheRoots();
                },
                // canExecute
                () => SelectedCacheRoot?.GameCaches.Count == 0
            );

            RefreshRootsCommand = new RelayCommand(() => RefreshCacheRoots());

            // . forward settings property changes
            plugin.SettingsUpdated += (s, e) => OnPropertyChanged(nameof(Settings));

            // . track cache roots list changes, in order to auto-adjust directory column width 
            this.CacheRoots.CollectionChanged += CacheRoots_CollectionChanged;
            plugin.cacheManager.CacheRoots.SortableColumnsChanged += CacheRoots_SortableColumnsChanged;
        }

        private void CacheRoots_SortableColumnsChanged(object sender, SortableColumnsChangedArgs e)
        {
            if (!string.IsNullOrEmpty(SortedColumnName))
            {
                if (e.changedColumns.Contains(SortedColumnName))
                {
                    DispatcherUtils.Invoke(plugin.cacheRootsView.Dispatcher, () =>
                    {
                        RefreshUpdatedCacheRootSorting(e.itemId);
                    });
                }
            }
        }

        private void RefreshUpdatedCacheRootSorting(string rootDirectory)
        {
            var cacheRoot = plugin.cacheManager.FindCacheRoot(rootDirectory);
            if (cacheRoot != null)
            {
                // . fastest option: 'edit' updated game cache item to force its re-sort
                plugin.cacheManager.cacheRootsCollectionView.EditItem(cacheRoot);
                plugin.cacheManager.cacheRootsCollectionView.CommitEdit();
            }
            else
            {
                // . fallback: refresh sorting of whole collection
                plugin.cacheManager.cacheRootsCollectionView.Refresh();
            }
        }

        private void CacheRoots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            plugin.cacheRootsView.UnselectCacheRoots();
            GridViewUtils.ColumnResize(plugin.cacheRootsView.CacheRoots);
        }

        public void RefreshCacheRoots()
        {
            foreach (var root in CacheRoots)
            {
                root.UpdateGameCaches();
            }
            OnPropertyChanged(nameof(CacheRoots));
            OnPropertyChanged(nameof(EmptyRootsVisible));
            OnPropertyChanged(nameof(NonEmptyRootsVisible));
            plugin.cacheRootsView?.UnselectCacheRoots();
            plugin.panelViewModel?.UpdateCacheRoots();
        }

        public class CustomSpaceAvailableSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . primary sort: by "Space available", reversable 
                long spaceX = ((CacheRootViewModel)(reverse ? y : x)).BytesAvailableForCaches;
                long spaceY = ((CacheRootViewModel)(reverse ? x : y)).BytesAvailableForCaches;

                // . secondary sort: by "Directory", always ascending
                string directoryX = ((CacheRootViewModel)x).Directory;
                string directoryY = ((CacheRootViewModel)y).Directory;

                return spaceX != spaceY ? spaceX.CompareTo(spaceY) : directoryX.CompareTo(directoryY);
            }
        }
        public CustomSpaceAvailableSorter CustomSpaceAvailableSort { get; private set; }

        public class CustomCachesInstalledSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . sort by installed number of caches 1st, and installed cache bytes 2nd
                var objX = (CacheRootViewModel)(reverse ? y : x);
                var objY = (CacheRootViewModel)(reverse ? x : y);

                // . primary sort: by "caches installed", reversable 
                int countX = objX.CachesInstalled;
                int countY = objY.CachesInstalled;

                // . secondary sort: by "installed size on disk", reversable 
                long bytesX = objX.cachesAggregateSizeOnDisk;
                long bytesY = objY.cachesAggregateSizeOnDisk;

                return countX != countY ? countX.CompareTo(countY) : bytesX.CompareTo(bytesY);
            }
        }
        public CustomCachesInstalledSorter CustomCachesInstalledSort { get; private set; }

        public class CustomMaxFillReservedSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                // . sort by max fill level 1st, and reserved bytes (reverse direction) 2nd
                var objX = (CacheRootViewModel)(reverse ? y : x);
                var objY = (CacheRootViewModel)(reverse ? x : y);

                // . primary sort: by "max fill level", reversable 
                double fillX = objX.MaxFillLevel;
                double fillY = objY.MaxFillLevel;

                // . secondary sort: by "reserved space", reversable 
                long bytesX = objX.bytesReservedOnDevice;
                long bytesY = objY.bytesReservedOnDevice;

                return fillX != fillY ? fillX.CompareTo(fillY) : bytesY.CompareTo(bytesX);
            }
        }
        public CustomMaxFillReservedSorter CustomMaxFillReservedSort { get; private set; }

    }
}

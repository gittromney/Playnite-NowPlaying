using NowPlaying.Utils;
using NowPlaying.Views;
using Playnite.SDK;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;

namespace NowPlaying.ViewModels
{
    public class CacheRootsViewModel : ViewModelBase
    {
        public readonly NowPlaying plugin;

        public ICommand RefreshRootsCommand { get; private set; }
        public ICommand AddCacheRootCommand { get; private set; }
        public ICommand EditMaxFillCommand { get; private set; }
        public ICommand RemoveCacheRootCommand { get; private set; }

        public ObservableCollection<CacheRootViewModel> CacheRoots => plugin.cacheManager.CacheRoots;
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
                    plugin.Settings.ShowRootDevice = value;
                    plugin.panelViewModel.SaveSettingsCommand?.Execute(plugin);
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
                    plugin.Settings.ShowRootDirectory = value;
                    plugin.panelViewModel.SaveSettingsCommand?.Execute(plugin);
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
                    plugin.Settings.ShowRootSpaceAvail = value;
                    plugin.panelViewModel.SaveSettingsCommand?.Execute(plugin);
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
                    plugin.Settings.ShowRootGames = value;
                    plugin.panelViewModel.SaveSettingsCommand?.Execute(plugin);
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
                    plugin.Settings.ShowRootInstalled = value;
                    plugin.panelViewModel.SaveSettingsCommand?.Execute(plugin);
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
                    plugin.Settings.ShowRootMaxFill = value;
                    plugin.panelViewModel.SaveSettingsCommand?.Execute(plugin);
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

                // setup up popup and center within the current application window
                popup.Width = view.MinWidth;
                popup.MinWidth = view.MinWidth;
                popup.Height = view.MinHeight + SystemParameters.WindowCaptionHeight;
                popup.MinHeight = view.MinHeight + SystemParameters.WindowCaptionHeight;
                popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
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

                    // setup up popup and center within the current application window
                    popup.Width = view.MinWidth;
                    popup.MinWidth = view.MinWidth;
                    popup.Height = view.MinHeight + SystemParameters.WindowCaptionHeight;
                    popup.MinHeight = view.MinHeight + SystemParameters.WindowCaptionHeight;
                    popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                    popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
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

            // . track cache roots list changes, in order to auto-adjust directory column width 
            this.CacheRoots.CollectionChanged += CacheRoots_CollectionChanged;
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
            plugin.cacheRootsView.UnselectCacheRoots();
            plugin.panelViewModel.UpdateCacheRoots();
        }

        public class CustomSpaceAvailableSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                long spaceX = ((CacheRootViewModel)x).BytesAvailableForCaches;
                long spaceY = ((CacheRootViewModel)y).BytesAvailableForCaches;
                return spaceX.CompareTo(spaceY);
            }
        }
        public CustomSpaceAvailableSorter CustomSpaceAvailableSort { get; private set; }

        public class CustomCachesInstalledSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                // . sort by installed number of caches 1st, and installed cache bytes 2nd
                int countX = ((CacheRootViewModel)x).CachesInstalled;
                int countY = ((CacheRootViewModel)y).CachesInstalled;
                long bytesX = ((CacheRootViewModel)x).cachesAggregateSizeOnDisk;
                long bytesY = ((CacheRootViewModel)y).cachesAggregateSizeOnDisk;
                return countX != countY ? countX.CompareTo(countY) : bytesX.CompareTo(bytesY);
            }
        }
        public CustomCachesInstalledSorter CustomCachesInstalledSort { get; private set; }

        public class CustomMaxFillReservedSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                // . sort by max fill level 1st, and reserved bytes (reverse direction) 2nd
                double fillX = ((CacheRootViewModel)x).MaxFillLevel;
                double fillY = ((CacheRootViewModel)y).MaxFillLevel;
                long bytesX = ((CacheRootViewModel)x).bytesReservedOnDevice;
                long bytesY = ((CacheRootViewModel)y).bytesReservedOnDevice;
                return fillX != fillY ? fillX.CompareTo(fillY) : bytesY.CompareTo(bytesX);
            }
        }
        public CustomMaxFillReservedSorter CustomMaxFillReservedSort { get; private set; }

    }
}

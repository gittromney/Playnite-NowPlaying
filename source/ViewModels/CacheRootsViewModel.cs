﻿using NowPlaying.Utils;
using NowPlaying.Views;
using Playnite.SDK;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = System.Windows.Window;

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

                // tweak window to make it fixed size and headless (no area reserved for title, min/max/close buttons)
                view.Loaded += plugin.panelViewModel.MakeWindowCaptionlessOnUserControlLoaded;
                var style = (Style)view.TryFindResource("FixedSizeWindow");
                if (style != null)
                {
                    popup.Style = style;
                }
                var captionHeight = 0; // window.Heights may be adjusted if window can't be made 'captionless'

                // setup up popup and center within the current application window
                popup.Width = view.MinWidth;
                popup.MinWidth = view.MinWidth;
                popup.MaxWidth = view.MaxWidth;
                popup.Height = view.MinHeight + captionHeight;
                popup.MinHeight = view.MinHeight + captionHeight;
                popup.MaxHeight = view.MaxHeight + captionHeight;
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

                    // tweak window to make it fixed size and headless (no area reserved for title, min/max/close buttons)
                    view.Loaded += plugin.panelViewModel.MakeWindowCaptionlessOnUserControlLoaded;
                    var style = (Style)view.TryFindResource("FixedSizeWindow");
                    if (style != null)
                    {
                        popup.Style = style;
                    }
                    var captionHeight = 0; // window.Heights may be adjusted if window can't be made 'captionless'

                    // setup up popup and center within the current application window
                    popup.Width = view.MinWidth;
                    popup.MinWidth = view.MinWidth;
                    popup.MaxWidth = view.MaxWidth;
                    popup.Height = view.MinHeight + captionHeight;
                    popup.MinHeight = view.MinHeight + captionHeight;
                    popup.MaxHeight = view.MaxHeight + captionHeight;
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
            GridViewUtils.RefreshSort(plugin.cacheRootsView.CacheRoots);
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
            GridViewUtils.RefreshSort(plugin.cacheRootsView.CacheRoots);
        }

        public class CustomSpaceAvailableSorter : IReversableComparer
        {
            public override int Compare(object x, object y, bool reverse = false)
            {
                long spaceX = ((CacheRootViewModel)(reverse ? y : x)).BytesAvailableForCaches;
                long spaceY = ((CacheRootViewModel)(reverse ? x : y)).BytesAvailableForCaches;
                return spaceX.CompareTo(spaceY);
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
                int countX = objX.CachesInstalled;
                int countY = objY.CachesInstalled;
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
                double fillX = objX.MaxFillLevel;
                double fillY = objY.MaxFillLevel;
                long bytesX = objX.bytesReservedOnDevice;
                long bytesY = objY.bytesReservedOnDevice;
                return fillX != fillY ? fillX.CompareTo(fillY) : bytesY.CompareTo(bytesX);
            }
        }
        public CustomMaxFillReservedSorter CustomMaxFillReservedSort { get; private set; }

    }
}

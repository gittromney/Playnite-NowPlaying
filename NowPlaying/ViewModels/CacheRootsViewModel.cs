using NowPlaying.Views;
using Playnite.SDK;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        public CacheRootsViewModel(NowPlaying plugin)
        {
            this.plugin = plugin;

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
                popup.Height = view.MinHeight;
                popup.MinHeight = view.MinHeight;
                popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
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
                    popup.Height = view.MinHeight;
                    popup.MinHeight = view.MinHeight;
                    popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                    popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
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
            plugin.cacheRootsView.CacheRoots_AutoResizeDirectoryColumn();
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
    }
}

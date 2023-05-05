using NowPlaying.Views;
using Playnite.SDK;
using System.Collections.ObjectModel;
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

                // setup up fixed sized popup and center within the current application window
                popup.Width = view.Width;
                popup.MinWidth = view.Width;
                popup.MaxWidth = view.Width;
                popup.Height = view.Height;
                popup.MinHeight = view.Height;
                popup.MaxHeight = view.Height;
                popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
                popup.Show();
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

                    // setup up fixed sized popup and center within the current application window
                    popup.Width = view.Width;
                    popup.MinWidth = view.Width;
                    popup.MaxWidth = view.Width;
                    popup.Height = view.Height;
                    popup.MinHeight = view.Height;
                    popup.MaxHeight = view.Height;
                    popup.Left = appWindow.Left + (appWindow.Width - popup.Width) / 2;
                    popup.Top = appWindow.Top + (appWindow.Height - popup.Height) / 2;
                    popup.Show();
                },
                // CanExecute
                () => SelectedCacheRoot != null
            );

            RemoveCacheRootCommand = new RelayCommand(
                () => {
                    plugin.cacheManager.RemoveCacheRoot(SelectedCacheRoot.Directory);
                    RefreshRootsList();
                },
                // canExecute
                () => SelectedCacheRoot?.GameCaches.Count == 0
            );

            RefreshRootsCommand = new RelayCommand(() => RefreshRootsList());
        }

        public void RefreshRootsList()
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

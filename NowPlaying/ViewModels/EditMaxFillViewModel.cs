using NowPlaying.Utils;
using Playnite.SDK;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NowPlaying.ViewModels
{
    public class EditMaxFillViewModel : ViewModelBase
    {
        private readonly NowPlaying plugin;
        private readonly GameCacheManagerViewModel cacheManager;
        private readonly CacheRootViewModel cacheRoot;
        public Window popup { get; set; }

        public string RootDirectory => cacheRoot.Directory;

        public bool HasSpaceForCaches { get; private set; }
        public string DeviceName => Directory.GetDirectoryRoot(RootDirectory);
        public string DeviceCapacity => SmartUnits.Bytes(DirectoryUtils.GetRootDeviceCapacity(RootDirectory));
        public string SpaceAvailable => SmartUnits.Bytes(DirectoryUtils.GetAvailableFreeSpace(RootDirectory));
        public string SpaceAvailableVisibility => HasSpaceForCaches ? "Visible" : "Hidden";
        public string NoSpaceAvailableVisibility => !HasSpaceForCaches ? "Visible" : "Hidden";

        private double maximumFillLevel;
        public double MaximumFillLevel
        {
            get => maximumFillLevel;
            set
            {
                if (value < 50) value = 50;
                if (value > 100) value = 100;
                if (value >= 50 && value <= 100)
                {
                    maximumFillLevel = value;
                    UpdateSpaceAvailableForCaches();
                    OnPropertyChanged(nameof(MaximumFillLevel));
                    OnPropertyChanged(nameof(SpaceToReserve));
                }
            }
        }
        public string SpaceToReserve => GetSpaceToReserve();
        public string SpaceAvailableForCaches { get; private set; }

        public bool SaveCommandCanExecute { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public EditMaxFillViewModel(NowPlaying plugin, Window popup, CacheRootViewModel cacheRoot)
        {
            this.plugin = plugin;
            this.cacheManager = plugin.cacheManager;
            this.popup = popup;
            this.cacheRoot = cacheRoot;
            this.MaximumFillLevel = cacheRoot.MaxFillLevel;

            this.SaveCommand = new RelayCommand(
                () => {
                    cacheRoot.SetMaxFillLevel(MaximumFillLevel);
                    plugin.cacheManager.SaveCacheRootsToJson();
                    plugin.cacheRootsViewModel.RefreshCacheRoots();
                    CloseWindow();
                },
                // CanExecute
                () => SaveCommandCanExecute
            );
            this.CancelCommand = new RelayCommand(() =>
            {
                plugin.cacheRootsViewModel.RefreshCacheRoots();
                CloseWindow();
            });

            UpdateSpaceAvailableForCaches();
            OnPropertyChanged(null);
        }

        private void UpdateSaveCommandCanExectue()
        {
            bool value = HasSpaceForCaches;
            if (SaveCommandCanExecute != value)
            {
                SaveCommandCanExecute = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string GetSpaceToReserve()
        {
            long deviceSize = DirectoryUtils.GetRootDeviceCapacity(RootDirectory);
            long reservedSize = (long) (deviceSize * (1.0 - MaximumFillLevel / 100.0));
            return "(" + SmartUnits.Bytes(reservedSize) + " reserved for other use)";
        }

        private void UpdateSpaceAvailableForCaches()
        {
            long availableForCaches = CacheRootViewModel.GetAvailableSpaceForCaches(RootDirectory, MaximumFillLevel);
            if (availableForCaches > 0)
            {
                HasSpaceForCaches = true;
                SpaceAvailableForCaches = SmartUnits.Bytes(availableForCaches);
            }
            else
            {
                HasSpaceForCaches = false;
                SpaceAvailableForCaches = "-";
            }

            OnPropertyChanged(nameof(HasSpaceForCaches));
            OnPropertyChanged(nameof(SpaceAvailableForCaches));
            OnPropertyChanged(nameof(SpaceAvailableVisibility));
            OnPropertyChanged(nameof(NoSpaceAvailableVisibility));
            UpdateSaveCommandCanExectue();
        }

        public void CloseWindow()
        {
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

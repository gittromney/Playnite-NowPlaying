﻿using NowPlaying.Utils;
using Playnite.SDK;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NowPlaying.ViewModels
{
    public class AddCacheRootViewModel : ViewModelBase
    {
        private readonly NowPlaying plugin;
        private readonly GameCacheManagerViewModel cacheManager;
        private Dictionary<string, string> rootDevices;
        private List<string> existingRoots;
        public Window popup { get; set; }

        public bool RootIsValid { get; private set; }
        public bool HasSpaceForCaches { get; private set; }

        public string RootStatus { get; private set; }

        private string rootDirectory;
        public string RootDirectory
        {
            get => rootDirectory;
            set
            {
                if (rootDirectory != value)
                {
                    rootDirectory = value;
                    UpdateRootDirectoryStatus();
                    UpdateSpaceAvailableForCaches();
                    OnPropertyChanged(null);
                }
            }
        }

        public string DeviceName => RootIsValid ? Directory.GetDirectoryRoot(RootDirectory) : "-";
        public string DeviceCapacity => RootIsValid ? SmartUnits.Bytes(DirectoryUtils.GetRootDeviceCapacity(RootDirectory)) : "-";
        public string SpaceAvailable => RootIsValid ? SmartUnits.Bytes(DirectoryUtils.GetAvailableFreeSpace(RootDirectory)) : "-";

        public string SpaceAvailableVisibility => !RootIsValid || HasSpaceForCaches ? "Visible" : "Hidden";
        public string NoSpaceAvailableVisibility => RootIsValid && !HasSpaceForCaches ? "Visible" : "Hidden";

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


        public bool AddCommandCanExecute { get; private set; }

        public ICommand SelectFolderCommand { get; private set; }
        public ICommand AddCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }


        public AddCacheRootViewModel(NowPlaying plugin, Window popup, bool isFirstAdded = false)
        {
            this.plugin = plugin;
            this.cacheManager = plugin.cacheManager;
            this.popup = popup;

            // build existing root directory list
            this.existingRoots = cacheManager.CacheRoots.Select(r => r.Directory).ToList();

            // build root device -> root directory mapping
            this.rootDevices = new Dictionary<string, string>();
            foreach (var root in existingRoots)
            {
                string rootDevice = Directory.GetDirectoryRoot(root);

                // only map 1st hit, if multiple cache roots are on a given device (which is not recommended)
                if (!rootDevices.ContainsKey(rootDevice))
                {
                    rootDevices.Add(rootDevice, root);
                }
            }

            this.SelectFolderCommand = new RelayCommand(() => 
            {
                var value = plugin.PlayniteApi.Dialogs.SelectFolder();
                if (!string.IsNullOrEmpty(value))
                {
                    RootDirectory = value;
                }
                UpdateRootDirectoryStatus();
                UpdateSpaceAvailableForCaches();
                OnPropertyChanged(null);
            });

            this.AddCommand = new RelayCommand(() =>
                {
                    cacheManager.AddCacheRoot(RootDirectory, MaximumFillLevel);
                    plugin.cacheRootsViewModel.RefreshCacheRoots();
                    CloseWindow();
                },
                () => RootIsValid && HasSpaceForCaches
            );

            this.CancelCommand = new RelayCommand(() =>
            {
                plugin.cacheRootsViewModel.RefreshCacheRoots();
                CloseWindow();
            });

            // fill with reasonable defaults
            if (isFirstAdded)
            {
                RootDirectory = @"C:\Games\NowPlaying";
                MaximumFillLevel = 80.0;
            }
            else
            {
                RootDirectory = "";
                MaximumFillLevel = 95.0;
            }

            UpdateRootDirectoryStatus();
            UpdateSpaceAvailableForCaches();
            OnPropertyChanged(null);
        }


        private void UpdateRootDirectoryStatus()
        {
            RootIsValid = false;
            if (string.IsNullOrEmpty(RootDirectory))
            {
                RootStatus = plugin.GetResourceString("LOCNowPlayingAddCacheRootStatusSpecify");
            }
            else if (!Directory.Exists(RootDirectory))
            {
                RootStatus = plugin.GetResourceString("LOCNowPlayingAddCacheRootStatusNotFound");
            }
            else
            {
                // . make sure directory root name is always upper case (c:\mydir -> C:\mydir)
                string device = Directory.GetDirectoryRoot(RootDirectory).ToUpper();
                RootDirectory = device + RootDirectory.Substring(device.Length);

                if (!DirectoryUtils.IsEmptyDirectory(RootDirectory))
                {
                    RootStatus = plugin.GetResourceString("LOCNowPlayingAddCacheRootStatusNotEmpty");
                }
                else if (!DirectoryUtils.ExistsAndIsWritable(RootDirectory))
                {
                    RootStatus = plugin.GetResourceString("LOCNowPlayingAddCacheRootStatusNotWritable");
                }
                else if (existingRoots.Contains(RootDirectory))
                {
                    RootStatus = plugin.GetResourceString("LOCNowPlayingAddCacheRootStatusDirIsRoot");
                }
                else if (rootDevices.ContainsKey(device))
                {
                    RootStatus = plugin.FormatResourceString("LOCNowPlayingAddCacheRootStatusDevIsRootFmt", rootDevices[device]);
                }
                else
                {
                    RootIsValid = true;
                    RootStatus = "";
                }
            }
            OnPropertyChanged(nameof(RootIsValid));
            OnPropertyChanged(nameof(RootStatus));
            UpdateAddCommandCanExectue();
        }

        private void UpdateAddCommandCanExectue()
        {
            bool value = RootIsValid && HasSpaceForCaches;
            if (AddCommandCanExecute != value)
            {
                AddCommandCanExecute = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string GetSpaceToReserve()
        {
            if (RootIsValid)
            {
                long deviceSize = DirectoryUtils.GetRootDeviceCapacity(RootDirectory);
                long reservedSize = (long) (deviceSize * (1.0 - MaximumFillLevel / 100.0));
                return SmartUnits.Bytes(reservedSize);
            }
            else
            {
                return "-";
            }
        }

        private void UpdateSpaceAvailableForCaches()
        {
            if (RootIsValid)
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
            UpdateAddCommandCanExectue();
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

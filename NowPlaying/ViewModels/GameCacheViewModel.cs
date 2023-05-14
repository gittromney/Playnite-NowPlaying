using NowPlaying.Core;
using NowPlaying.Models;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace NowPlaying.ViewModels
{
    public class GameCacheViewModel : ObservableObject
    {
        public readonly GameCacheManagerViewModel manager;
        public readonly GameCacheEntry entry;
        public CacheRootViewModel cacheRoot;

        public string Title => entry.Title;
        public string Root => entry.CacheRoot;
        public string Device => Directory.GetDirectoryRoot(Root);
        public string Id => entry.Id;
        public string CacheDir => entry.CacheDir;
        public string InstallDir => entry.InstallDir;
        public string ExePath => entry.ExePath;
        public long InstallSize => entry.InstallSize;
        public long CacheSize => entry.CacheSize;

        public GameCacheState State => entry.State;


        private string installQueueStatus;
        private string uninstallQueueStatus;
        private bool nowInstalling; 
        private bool nowUninstalling;
        public string InstallQueueStatus => installQueueStatus;
        public string UninstallQueueStatus => uninstallQueueStatus;
        public bool NowInstalling => nowInstalling;
        public bool NowUninstalling => nowUninstalling;
        public string Status => GetStatus(entry.State, installQueueStatus, uninstallQueueStatus, nowInstalling, nowUninstalling);
        public string StatusColor => State == GameCacheState.Invalid ? "WarningBrush" : Status == "Uninstalled" ? "TextBrushDarker" : "TextBrush";

        private string cacheInstalledSize;
        public string CacheInstalledSize => cacheInstalledSize;
        public string CacheInstalledSizeColor => CanInstallCache == "No" ? "WarningBrush" : State == GameCacheState.Empty ? "TextBrushDarker" : "TextBrush";

        public bool CacheWillFit { get; private set; }
        public string CanInstallCache => entry.State==GameCacheState.Empty || entry.State==GameCacheState.InProgress ? (CacheWillFit ? "Yes" : "No") : "-";
        public string CanInstallCacheColor => CanInstallCache == "No" ? "WarningBrush" : "TextBrush";

        public TimeSpan InstallEtaTimeSpan { get; private set; }
        public string InstallEta { get; private set; }

        public string CacheRootSpaceAvailable { get; private set; }

        public GameCacheViewModel(GameCacheManagerViewModel manager, GameCacheEntry entry, CacheRootViewModel cacheRoot)
        {
            this.manager = manager;
            this.entry = entry;
            this.cacheRoot = cacheRoot;
            this.nowInstalling = manager.IsPopulateInProgess(entry.Id);
            this.cacheInstalledSize = GetCacheInstalledSize(entry);
            UpdateInstallEta();
        }

        public bool IsUninstalled()
        {
            return State == GameCacheState.Empty;
        }

        public void UpdateCacheRoot()
        {
            OnPropertyChanged(nameof(Root));
            OnPropertyChanged(nameof(Device));
            OnPropertyChanged(nameof(CacheDir));
            UpdateStatus();
            UpdateCacheSize();
            UpdateCacheSpaceWillFit();
            UpdateInstallEta();
        }

        public void UpdateStatus()
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(CanInstallCache));
            OnPropertyChanged(nameof(CanInstallCacheColor));
            OnPropertyChanged(nameof(CacheInstalledSizeColor));
            cacheRoot.UpdateGameCaches();
        }

        public void UpdateInstallQueueStatus(string value = null)
        {
            if (installQueueStatus != value)
            {
                installQueueStatus = value;
                OnPropertyChanged(nameof(InstallQueueStatus));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public void UpdateUninstallQueueStatus(string value = null)
        {
            if (uninstallQueueStatus != value)
            {
                uninstallQueueStatus = value;
                OnPropertyChanged(nameof(UninstallQueueStatus));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public void UpdateNowInstalling(bool value)
        {
            if (nowInstalling != value)
            {
                nowInstalling = value;
                nowUninstalling &= !nowInstalling;

                OnPropertyChanged(nameof(NowInstalling));
                OnPropertyChanged(nameof(NowUninstalling));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(CanInstallCache));
                OnPropertyChanged(nameof(CanInstallCacheColor));
                OnPropertyChanged(nameof(CacheInstalledSizeColor));
            }
        }

        public void UpdateNowUninstalling(bool value)
        {
            if (nowUninstalling != value)
            {
                nowUninstalling = value;
                nowInstalling &= !nowUninstalling;

                OnPropertyChanged(nameof(NowInstalling));
                OnPropertyChanged(nameof(NowUninstalling));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(CanInstallCache));
                OnPropertyChanged(nameof(CanInstallCacheColor));
                OnPropertyChanged(nameof(CacheInstalledSizeColor));
            }
        }

        public void UpdateCacheSize()
        {
            OnPropertyChanged(nameof(CacheSize));

            string value = GetCacheInstalledSize(entry);
            if (cacheInstalledSize != value)
            {
                cacheInstalledSize = value;
                OnPropertyChanged(nameof(CacheInstalledSize));
                OnPropertyChanged(nameof(CacheInstalledSizeColor));
                cacheRoot.UpdateCachesInstalled();
                cacheRoot.UpdateSpaceAvailableForCaches();
            }
        }

        public void UpdateCacheSpaceWillFit()
        {
            bool bval = cacheRoot.BytesAvailableForCaches > (InstallSize - CacheSize);
            if (CacheWillFit != bval)
            {
                CacheWillFit = bval;
                OnPropertyChanged(nameof(CacheWillFit));
                OnPropertyChanged(nameof(CanInstallCache));
                OnPropertyChanged(nameof(CanInstallCacheColor));
                OnPropertyChanged(nameof(CacheInstalledSizeColor));
            }
            string sval = SmartUnits.Bytes(cacheRoot.BytesAvailableForCaches, decimals: 1);
            if (CacheRootSpaceAvailable != sval)
            {
                CacheRootSpaceAvailable = sval;
                OnPropertyChanged(nameof(CacheRootSpaceAvailable));
            }
        }

        public void UpdateInstallEta(TimeSpan? value = null)
        {
            if (value == null)
            {
                value = GetInstallEtaTimeSpan(entry, manager.GetInstallAverageBps(entry.InstallDir));
            }
            if (InstallEtaTimeSpan != value || InstallEta == null)
            {
                InstallEtaTimeSpan = (TimeSpan)value;
                InstallEta = GetInstallEta(InstallEtaTimeSpan);
                OnPropertyChanged(nameof(InstallEtaTimeSpan));
                OnPropertyChanged(nameof(InstallEta));
            }
        }

        private TimeSpan GetInstallEtaTimeSpan(GameCacheEntry entry, long averageBps)
        {
            bool notInstalled = entry.State == GameCacheState.Empty || entry.State == GameCacheState.InProgress;
            if (notInstalled)
            {
                if (averageBps > 0)
                {
                    long bytesLeftToInstall = entry.InstallSize - entry.CacheSize;
                    return TimeSpan.FromSeconds((1.0 * bytesLeftToInstall) / averageBps);
                }
                else
                {
                    return TimeSpan.FromMilliseconds(-1); // Infinite
                }
            }
            else
            {
                return TimeSpan.Zero;
            }
        }

        private string GetInstallEta(TimeSpan etaTimeSpan)
        {
            return etaTimeSpan > TimeSpan.Zero ? SmartUnits.Duration(etaTimeSpan) : etaTimeSpan == TimeSpan.Zero ? "-" : "∞";
        }

        private string GetCacheInstalledSize(GameCacheEntry entry)
        {
            switch (entry.State)
            {
                case GameCacheState.Played:
                case GameCacheState.Populated: return SmartUnits.Bytes(entry.CacheSize, decimals:1);
                case GameCacheState.InProgress: return SmartUnits.BytesOfBytes(entry.CacheSize, entry.InstallSize, decimals:1);
                case GameCacheState.Empty: return "(needs " + SmartUnits.Bytes(entry.InstallSize, decimals:1) + ")";
                default: return "-";
            }
        }

        private string GetStatus(GameCacheState state, string installQueueStatus, string uninstallQueueStatus, bool nowInstalling, bool nowUninstalling)
        {
            if (installQueueStatus != null)
            {
                return $"Queued for install ({installQueueStatus})";
            }
            else if (uninstallQueueStatus != null)
            {
                return $"Queued for uninstall ({uninstallQueueStatus})";
            }
            else if (nowInstalling)
            {
                return "Installing...";
            }
            else if (nowUninstalling)
            {
                return "Uninstalling...";
            }
            else
            {
                switch (state)
                {
                    case GameCacheState.Played:
                    case GameCacheState.Populated: return "Installed";
                    case GameCacheState.InProgress: return "Paused";
                    case GameCacheState.Empty: return "Uninstalled";
                    default: return "** invalid **";
                }
            }
        }

    }

}

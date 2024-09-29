using NowPlaying.Utils;
using NowPlaying.Models;
using System;
using System.IO;
using System.Windows.Media;
using NowPlaying.Extensions;

namespace NowPlaying.ViewModels
{

    public class GameCacheViewModel : ColumnSortableObject
    {
        private readonly NowPlaying plugin;
        private readonly ThemeResources theme;
        public readonly GameCacheManagerViewModel manager;
        public readonly GameCacheEntry entry;
        public CacheRootViewModel cacheRoot;

        public ThemeResources Theme => theme;
        public string Title => entry.Title;
        public string Root => entry.CacheRoot;
        public string Device => Directory.GetDirectoryRoot(Root);
        public string Id => entry.Id;
        public string CacheDir => entry.CacheDir;
        public string InstallDir => entry.InstallDir;
        public string ExePath => entry.ExePath;
        public string XtraArgs => entry.XtraArgs;
        public long InstallSize => entry.InstallSize;
        public long InstallFiles => entry.InstallFiles;
        public long CacheSize => entry.CacheSize;
        public long CacheSizeOnDisk => entry.CacheSizeOnDisk;

        public GameCacheState State => entry.State;
        public GameCachePlatform Platform => entry.Platform;

        private string installQueueStatus;
        private string uninstallQueueStatus;
        private bool nowInstalling; 
        private bool nowUninstalling;
        private string formatStringXofY;
        private int bytesScale;
        private string bytesToCopy;
        private string cacheInstalledSize;

        public string InstallQueueStatus => installQueueStatus;
        public string UninstallQueueStatus => uninstallQueueStatus;
        public bool NowInstalling => nowInstalling;
        public bool NowUninstalling => nowUninstalling;
        public string Status => GetStatus
        (
            entry.State, 
            installQueueStatus, 
            uninstallQueueStatus, 
            nowInstalling,
            plugin.SpeedLimitIpg > 0,
            nowUninstalling
        );

        public Brush InstalledBrush =>
        (
            Status == plugin.GetResourceString("LOCNowPlayingTermsInstalled") ? theme.TextBrush : theme.TextBrushDarker
        );

        public Brush StatusBrush =>
        (
            State == GameCacheState.Unknown || State == GameCacheState.Invalid ? theme.WarningBrush :
            Status == plugin.GetResourceString("LOCNowPlayingTermsUninstalled") ? theme.TextBrushDarker :
            NowInstalling ? (plugin.SpeedLimitIpg > 0 ? theme.SlowInstallBrush : theme.InstallBrush) :
            theme.TextBrush
        );

        public bool CacheWillFit { get; private set; }
        public string CanInstallCache => 
        (
            (entry.State==GameCacheState.Empty || entry.State==GameCacheState.InProgress) 
            ? plugin.GetResourceString(CacheWillFit ? "LOCNowPlayingTermsYes" : "LOCNowPlayingTermsNo") 
            : "-"
        );
        public Brush CanInstallCacheBrush => CanInstallCache == plugin.GetResourceString("LOCNowPlayingTermsNo") ? theme.WarningBrush : theme.TextBrush;

        public TimeSpan InstallEtaTimeSpan { get; private set; }
        public string InstallEta { get; private set; }

        public string CacheInstalledSize => cacheInstalledSize;
        public Brush CacheInstalledSizeBrush => 
        (
            CanInstallCache == plugin.GetResourceString("LOCNowPlayingTermsNo") ? theme.WarningBrush : 
            State == GameCacheState.Empty ? theme.TextBrushDarker : 
            theme.TextBrush
        );

        public string CacheRootSpaceAvailable { get; private set; }
        public Brush CacheRootSpaceAvailableBrush => cacheRoot.BytesAvailableForCaches > 0 ? theme.TextBrush : theme.WarningBrush;

        public bool PartialFileResume { get; set; }

        public GameCacheViewModel(GameCacheManagerViewModel manager, GameCacheEntry entry, CacheRootViewModel cacheRoot)
        {
            this.manager = manager;
            this.plugin = manager.plugin;
            this.theme = plugin.themeResources;
            this.entry = entry;
            this.cacheRoot = cacheRoot;
            this.nowInstalling = manager.IsPopulateInProgess(entry.Id);
            this.PartialFileResume = false;

            this.formatStringXofY = plugin.GetResourceFormatString("LOCNowPlayingProgressXofYFmt2", 2) ?? "{0} of {1}";
            this.cacheInstalledSize = GetCacheInstalledSize(entry);
            this.bytesScale = SmartUnits.GetBytesAutoScale(entry.InstallSize);
            this.bytesToCopy = SmartUnits.Bytes(entry.InstallSize, decimals: 1, userScale: bytesScale);

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
            OnSortableColumnChanged(Id, "Device");
            UpdateStatus();
            UpdateCacheSize();
            UpdateCacheSpaceWillFit();
            UpdateInstallEta();
        }

        public void UpdateStatus()
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusBrush));
            OnPropertyChanged(nameof(InstalledBrush));
            OnPropertyChanged(nameof(CanInstallCache));
            OnPropertyChanged(nameof(CanInstallCacheBrush));
            OnPropertyChanged(nameof(CacheInstalledSizeBrush));
            OnSortableColumnsChanged(Id, new string[] { "Status", "CanInstallCache" });
            cacheRoot.UpdateGameCaches();
        }

        public void UpdateInstallQueueStatus(string value = null)
        {
            if (installQueueStatus != value)
            {
                installQueueStatus = value;
                OnPropertyChanged(nameof(InstallQueueStatus));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(InstalledBrush));
                OnSortableColumnChanged(Id, "Status");
            }
        }

        public void UpdateUninstallQueueStatus(string value = null)
        {
            if (uninstallQueueStatus != value)
            {
                uninstallQueueStatus = value;
                OnPropertyChanged(nameof(UninstallQueueStatus));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(InstalledBrush));
                OnSortableColumnChanged(Id, "Status");
            }
        }

        public void UpdateNowInstalling(bool value)
        {
            if (value)
            {
                // . update auto scale for and bake "OfBytes" to copy string.
                this.bytesScale = SmartUnits.GetBytesAutoScale(entry.InstallSize);
                this.bytesToCopy = SmartUnits.Bytes(entry.InstallSize, decimals: 1, userScale: bytesScale);
            }
            
            nowInstalling = value;
            nowUninstalling &= !nowInstalling;
            UpdateInstallEta();

            OnPropertyChanged(nameof(NowInstalling));
            OnPropertyChanged(nameof(NowUninstalling));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusBrush));
            OnPropertyChanged(nameof(InstalledBrush));
            OnPropertyChanged(nameof(CanInstallCache));
            OnPropertyChanged(nameof(CanInstallCacheBrush));
            OnPropertyChanged(nameof(CacheInstalledSizeBrush));
            OnSortableColumnsChanged(Id, new string[] { "Status", "CanInstallCache", "CacheInstalledSize" });
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
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(InstalledBrush));
                OnPropertyChanged(nameof(CanInstallCache));
                OnPropertyChanged(nameof(CanInstallCacheBrush));
                OnPropertyChanged(nameof(CacheInstalledSizeBrush));
                OnSortableColumnsChanged(Id, new string[] { "Status", "CanInstallCache", "CacheInstalledSize" });
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
                OnPropertyChanged(nameof(CacheInstalledSizeBrush));
                OnSortableColumnChanged(Id, "CacheInstalledSize");
                cacheRoot.UpdateCachesInstalled();
                cacheRoot.UpdateSpaceAvailableForCaches();
            }
        }

        public void UpdateCacheSpaceWillFit()
        {
            bool bval = cacheRoot.BytesAvailableForCaches > (InstallSize - CacheSizeOnDisk);
            if (CacheWillFit != bval)
            {
                CacheWillFit = bval;
                OnPropertyChanged(nameof(CacheWillFit));
                OnPropertyChanged(nameof(CanInstallCache));
                OnPropertyChanged(nameof(CanInstallCacheBrush));
                OnPropertyChanged(nameof(CacheInstalledSizeBrush));
                OnSortableColumnsChanged(Id, new string[] { "CanInstallCache", "CacheInstalledSize" });
            }
            string sval = SmartUnits.Bytes(cacheRoot.BytesAvailableForCaches, decimals: 1);
            if (CacheRootSpaceAvailable != sval || cacheRoot.BytesAvailableForCaches <= 0)
            {
                CacheRootSpaceAvailable = sval;
                OnPropertyChanged(nameof(CacheRootSpaceAvailable));
                OnPropertyChanged(nameof(CacheRootSpaceAvailableBrush));
                OnSortableColumnChanged(Id, "CacheRootSpaceAvailable");
            }
        }

        public void UpdateInstallEta(TimeSpan? value = null)
        {
            if (value == null)
            { 
                var avgBytesPerFile = entry.InstallSize / entry.InstallFiles;
                var avgBps = manager.GetInstallAverageBps(entry.InstallDir, avgBytesPerFile, plugin.SpeedLimitIpg);
                value = GetInstallEtaTimeSpan(entry, avgBps);
            }
            if (InstallEtaTimeSpan != value || InstallEta == null)
            {
                InstallEtaTimeSpan = (TimeSpan)value;
                InstallEta = GetInstallEta(InstallEtaTimeSpan);
                OnPropertyChanged(nameof(InstallEtaTimeSpan));
                OnPropertyChanged(nameof(InstallEta));
                OnSortableColumnChanged(Id, "InstallEta");
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
                case GameCacheState.Populated: return SmartUnits.Bytes(entry.CacheSize, decimals: 1);
                case GameCacheState.InProgress: return string.Format
                (
                    formatStringXofY,
                    SmartUnits.Bytes(entry.CacheSize, userScale: bytesScale, decimals: 1, showUnits: false),
                    bytesToCopy
                );
                case GameCacheState.Empty: return plugin.FormatResourceString
                (
                    "LOCNowPlayingGameCacheSizeNeededFmt", 
                    SmartUnits.Bytes(entry.InstallSize, decimals:1)
                );
                default: return "-";
            }
        }

        private string GetStatus
            (
                GameCacheState state,
                string installQueueStatus,
                string uninstallQueueStatus,
                bool nowInstalling,
                bool isSpeedLimited,
                bool nowUninstalling
            )
        {
            if (installQueueStatus != null)
            {
                return plugin.GetResourceString("LOCNowPlayingTermsQueuedForInstall") + $" ({installQueueStatus})";
            }
            else if (uninstallQueueStatus != null)
            {
                return plugin.GetResourceString("LOCNowPlayingTermsQueuedForUninstall") + $" ({uninstallQueueStatus})";
            }
            else if (nowInstalling)
            {
                return plugin.GetResourceString(isSpeedLimited ? "LOCNowPlayingTermsInstallSpeedLimit" : "LOCNowPlayingTermsInstalling") + "...";
            }
            else if (nowUninstalling)
            {
                return plugin.GetResourceString("LOCNowPlayingTermsUninstalling") + "...";
            }
            else
            {
                switch (state)
                {
                    case GameCacheState.Played:
                    case GameCacheState.Populated: return plugin.GetResourceString("LOCNowPlayingTermsInstalled");
                    case GameCacheState.InProgress: return plugin.GetResourceString("LOCNowPlayingTermsPaused");
                    case GameCacheState.Empty: return plugin.GetResourceString("LOCNowPlayingTermsUninstalled");
                    default: return "** " + plugin.GetResourceString("LOCNowPlayingTermsInvalid") + " **";
                }
            }
        }

    }

}

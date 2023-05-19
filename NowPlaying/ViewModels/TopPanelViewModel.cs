﻿using NowPlaying.Utils;
using Playnite.SDK.Plugins;
using System;
using System.Windows.Media;

namespace NowPlaying.ViewModels
{
 

    public class TopPanelViewModel : ViewModelBase
    {
        public enum Mode { Enable, Uninstall, Install, SlowInstall };

        private readonly NowPlaying plugin;

        private int gamesToEnable;
        private int gamesEnabled;

        private int cachesToUninstall;
        private int cachesUninstalled;

        private GameCacheViewModel nowInstallingCache;
        private bool isSlowInstall;
        private int cachesToInstall;
        private int cachesInstalled;
        private long totalBytesToInstall;
        private long totalBytesInstalled;
        private TimeSpan queuedInstallEta;

        public double PercentDone { get; private set; }
        public string Status { get; private set; }

        public string ProgressIsIndeterminate => TopPanelMode==Mode.Install || TopPanelMode==Mode.SlowInstall ? "False" : "True";
        public string ProgressBarForeground => (TopPanelMode==Mode.Enable ? "TopPanelEnableFgBrush" : 
                                                TopPanelMode==Mode.Uninstall ? "TopPanelUninstallFgBrush" : 
                                                TopPanelMode==Mode.SlowInstall ? "TopPanelSlowInstallFgBrush" :
                                                "TopPanelInstallFgBrush");
        public string ProgressBarBackground => (TopPanelMode==Mode.Enable ? "TopPanelEnableBgBrush" : 
                                                TopPanelMode==Mode.Uninstall ? "TopPanelUninstallBgBrush" :
                                                TopPanelMode == Mode.SlowInstall ? "TopPanelSlowInstallBgBrush" :
                                                "TopPanelInstallBgBrush");

        private Mode topPanelMode;
        public Mode TopPanelMode
        {
            get => topPanelMode;
            set
            {
                if (topPanelMode != value)
                {
                    topPanelMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressBarForeground));
                    OnPropertyChanged(nameof(ProgressBarBackground));
                    OnPropertyChanged(nameof(ProgressIsIndeterminate));
                }
            }
        }

        private bool topPanelVisible;
        public bool TopPanelVisible
        {
            get => topPanelVisible;
            set
            {
                if (topPanelVisible != value)
                {
                    topPanelVisible = value;
                    plugin.topPanelItem.Visible = value;
                    plugin.panelViewModel.IsTopPanelVisible = value;
                }
            }
        }

        public TopPanelViewModel(NowPlaying plugin)
        {
            this.plugin = plugin;
            Reset();
        }

        private void Reset()
        {
            gamesToEnable = 0;
            gamesEnabled = 0;
            cachesToUninstall = 0;
            cachesUninstalled = 0;
            nowInstallingCache = null;
            isSlowInstall = false;
            cachesInstalled = 0;
            cachesToInstall = 0;
            totalBytesToInstall = 0;
            totalBytesInstalled = 0;
            queuedInstallEta = TimeSpan.Zero;
            PercentDone = 0;
            Status = "";
        }

        public void OnJobStatsUpdated(object sender, string cacheId)
        {
            if (cacheId == nowInstallingCache?.Id)
            {
                UpdateStatus();
            }
        }

        public void UpdateStatus()
        {
            TopPanelMode = (gamesEnabled < gamesToEnable ? Mode.Enable : 
                            cachesUninstalled < cachesToUninstall ? Mode.Uninstall : 
                            isSlowInstall ? Mode.SlowInstall : Mode.Install);
            TopPanelVisible = gamesEnabled < gamesToEnable || cachesUninstalled < cachesToUninstall || cachesInstalled < cachesToInstall;

            if (TopPanelVisible)
            {
                switch (TopPanelMode)
                {
                    case Mode.Enable:
                        Status = string.Format("Enabling game {0} of {1}...", gamesEnabled + 1, gamesToEnable);
                        OnPropertyChanged(nameof(Status));
                        break;

                    case Mode.Uninstall:
                        Status = string.Format("Uninstalling {0} of {1}...", cachesUninstalled + 1, cachesToUninstall);
                        OnPropertyChanged(nameof(Status));
                        break;

                    default:
                        PercentDone = (100.0 * (totalBytesInstalled + (nowInstallingCache?.CacheSize ?? 0))) / totalBytesToInstall;
                        OnPropertyChanged(nameof(PercentDone));

                        var cachesInstallEta = SmartUnits.Duration((nowInstallingCache?.InstallEtaTimeSpan ?? TimeSpan.Zero) + queuedInstallEta);
                        Status = string.Format("Installing {0} of {1}, ETA {2}", cachesInstalled + 1, cachesToInstall, cachesInstallEta);
                        OnPropertyChanged(nameof(Status));
                        break;
                }
            }
            else
            {
                Reset();
            }
        }

        public void QueuedInstall(long installSize, TimeSpan eta)
        {
            cachesToInstall++;
            totalBytesToInstall += installSize;
            queuedInstallEta += eta;
            UpdateStatus();
        }

        public void CancelledFromInstallQueue(long installSize, TimeSpan eta)
        {
            cachesToInstall--;
            totalBytesToInstall -= installSize;
            queuedInstallEta -= eta;
            UpdateStatus();
        }

        public void NowInstalling(GameCacheViewModel gameCache, bool isSpeedLimited = false)
        {
            nowInstallingCache = gameCache;
            queuedInstallEta -= gameCache.InstallEtaTimeSpan;
            isSlowInstall = isSpeedLimited;
            plugin.cacheManager.gameCacheManager.eJobStatsUpdated += OnJobStatsUpdated;
            UpdateStatus();
        }

        public void InstallQueuePaused()
        {
            Reset();
            UpdateStatus();
        }

        public void InstallDoneOrCancelled()
        {
            plugin.cacheManager.gameCacheManager.eJobStatsUpdated -= OnJobStatsUpdated;
            if (++cachesInstalled < cachesToInstall)
            {
                // . update state for next cache in the queue
                totalBytesInstalled += nowInstallingCache.InstallSize;
            }
            UpdateStatus();
        }

        public void QueuedUninstall()
        {
            cachesToUninstall++;
            UpdateStatus();
        }

        public void CancelledFromUninstallQueue()
        {
            cachesToUninstall--;
            UpdateStatus();
        }

        public void UninstallDoneOrCancelled()
        {
            cachesUninstalled++;
            UpdateStatus();
        }
        
        public void QueuedEnabler()
        {
            gamesToEnable++;
            UpdateStatus();
        }

        public void EnablerDoneOrCancelled()
        {
            gamesEnabled++;
            UpdateStatus();
        }
    }
}

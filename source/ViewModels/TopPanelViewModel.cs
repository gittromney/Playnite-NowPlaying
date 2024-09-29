using NowPlaying.Utils;
using NowPlaying.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Windows;
using Brush = System.Windows.Media.Brush;

namespace NowPlaying.ViewModels
{
    public class TopPanelViewModel : ViewModelBase
    {
        public enum Mode { Processing, Enable, Uninstall, Install, SlowInstall };

        private readonly NowPlaying plugin;
        private readonly ThemeResources theme;

        private string formatStringXofY;
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

        private LinkedList<string> processingMessage;
        public bool IsProcessing { get; private set; }
        public double PercentDone { get; private set; }
        public string Status { get; private set; }

        public ThemeResources Theme => theme;

        public string ProgressIsIndeterminate => TopPanelMode==Mode.Install || TopPanelMode==Mode.SlowInstall ? "False" : "True";

        public string ProgressBarForeground => (TopPanelMode==Mode.Processing ? "TopPanelProcessingFgBrush" :
                                                TopPanelMode==Mode.Enable ? "TopPanelEnableFgBrush" : 
                                                TopPanelMode==Mode.Uninstall ? "TopPanelUninstallFgBrush" : 
                                                TopPanelMode==Mode.SlowInstall ? "ProgressSlowInstallFgBrush" :
                                                Theme.ProgressInstallFgBrush);
        public string ProgressBarBackground => (TopPanelMode==Mode.Processing ? "TopPanelProcessingBgBrush" :
                                                TopPanelMode==Mode.Enable ? "TopPanelEnableBgBrush" : 
                                                TopPanelMode==Mode.Uninstall ? "TopPanelUninstallBgBrush" :
                                                TopPanelMode == Mode.SlowInstall ? "ProgressSlowInstallBgBrush" :
                                                Theme.ProgressInstallBgBrush);

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

        public Style ProgressBarStyle => plugin.panelViewModel.TopPanelProgressBarStyle;

        public TopPanelViewModel(NowPlaying plugin, ThemeResources theme)
        {
            this.plugin = plugin;
            this.theme = theme;
            this.processingMessage = new LinkedList<string>();
            Reset();
        }

        public void UpdateProgressBarStyle()
        {
            OnPropertyChanged(nameof(ProgressBarStyle));
        }

        private void Reset()
        {
            processingMessage.Clear();
            IsProcessing = false;
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
            this.formatStringXofY = plugin.GetResourceFormatString("LOCNowPlayingProgressXofYFmt2", 2) ?? "{0} of {1}";
        }

        private bool EnableCounterReset()
        {
            gamesEnabled = 0;
            gamesToEnable = 0;
            return true;
        }

        private bool UninstallCounterReset()
        {
            cachesUninstalled = 0;
            cachesToUninstall = 0;
            return true;
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
            TopPanelMode = 
            (
                IsProcessing ? Mode.Processing :
                gamesEnabled < gamesToEnable ? Mode.Enable : 
                cachesUninstalled < cachesToUninstall ? Mode.Uninstall : 
                isSlowInstall ? Mode.SlowInstall : 
                Mode.Install
            );
            
            TopPanelVisible = 
            (
                IsProcessing || 
                gamesEnabled < gamesToEnable || 
                cachesUninstalled < cachesToUninstall || 
                cachesInstalled < cachesToInstall
            );

            if (TopPanelVisible)
            {
                switch (TopPanelMode)
                {
                    case Mode.Processing:
                        Status = processingMessage.First() ?? "Processing...";
                        OnPropertyChanged(nameof(Status));
                        break;

                    case Mode.Enable:
                        Status = plugin.GetResourceString("LOCNowPlayingTermsEnabling") + " ";
                        Status += string.Format(formatStringXofY, gamesEnabled + 1, gamesToEnable) + "...";
                        OnPropertyChanged(nameof(Status));
                        break;

                    case Mode.Uninstall:
                        Status = plugin.GetResourceString("LOCNowPlayingTermsUninstalling") + " ";
                        Status += string.Format(formatStringXofY, cachesUninstalled + 1, cachesToUninstall) + "...";
                        OnPropertyChanged(nameof(Status));
                        break;

                    default:
                        PercentDone = (100.0 * (totalBytesInstalled + (nowInstallingCache?.CacheSize ?? 0))) / totalBytesToInstall;
                        OnPropertyChanged(nameof(PercentDone));

                        var cachesInstallEta = SmartUnits.Duration((nowInstallingCache?.InstallEtaTimeSpan ?? TimeSpan.Zero) + queuedInstallEta);
                        Status = plugin.GetResourceString("LOCNowPlayingTermsInstalling") + " ";
                        Status += string.Format(formatStringXofY, cachesInstalled + 1, cachesToInstall) + ", ";
                        Status += plugin.GetResourceString("LOCNowPlayingTermsEta") + " " + cachesInstallEta;
                        OnPropertyChanged(nameof(Status));
                        break;
                }

                if (gamesEnabled >= gamesToEnable)
                {
                    EnableCounterReset();
                }
                else if (cachesUninstalled >= cachesToUninstall)
                {
                    UninstallCounterReset();
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

        // . Processing messages work as a pseudo-stack.
        // . Latest message is displayed until it is removed or a new message is pushed in.
        //
        public void NowProcessing(bool value, string message = null)
        {
            if (value)
            {
                if (!processingMessage.Contains(message))
                {
                    processingMessage.AddFirst(message);
                }
                IsProcessing = true;
            }
            else
            {
                processingMessage.Remove(message);
                IsProcessing = processingMessage.Count > 0;
            }
            UpdateStatus();
        }
    }
}

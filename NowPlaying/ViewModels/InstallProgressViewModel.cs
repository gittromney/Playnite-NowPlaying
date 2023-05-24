using NowPlaying.Models;
using NowPlaying.Utils;
using Playnite.SDK;
using System;

namespace NowPlaying.ViewModels
{
    public class InstallProgressViewModel : ViewModelBase
    {
        private readonly NowPlaying plugin;
        private readonly NowPlayingInstallController controller;
        private readonly GameCacheManagerViewModel cacheManager;
        private readonly GameCacheViewModel gameCache;
        private readonly RoboStats jobStats;

        private bool preparingToInstall;
        public bool PreparingToInstall
        {
            get => preparingToInstall;
            set
            {
                if (preparingToInstall != value)
                {
                    preparingToInstall = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CopiedFilesAndBytesProgress));
                    OnPropertyChanged(nameof(CurrentFile));
                    OnPropertyChanged(nameof(SpeedDurationEta));
                }
            }
        }

        private int speedLimitIpg;
        public int SpeedLimitIpg
        {
            get => speedLimitIpg;
            set
            {
                if (speedLimitIpg != value)
                {
                    speedLimitIpg = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressPanelTitle));
                    OnPropertyChanged(nameof(ProgressTitleBrush));
                    OnPropertyChanged(nameof(ProgressBarBrush));
                }
            }
        }

        public RelayCommand PauseInstallCommand { get; private set; }
        public RelayCommand CancelInstallCommand { get; private set; }

        public string GameTitle => gameCache.Title;
        public string InstallSize => SmartUnits.Bytes(gameCache.InstallSize);

        //
        // Real-time GameCacheJob Statistics
        //
        private string formatStringCopyingFile;
        private string formatStringXofY;
        private string formatStringFilesAndBytes;
        private string formatStringSpeedDurationEta;
        private double percentDone;
        private long   filesCopied;
        private int    bytesScale;
        private string bytesCopied;
        private string bytesToCopy;
        private string copiedFilesOfFiles;
        private string copiedBytesOfBytes;
        private string currentFile;
        private string duration;
        private string timeRemaining;
        private string averageSpeed;

        // . rolling average of 'average bytes per second'
        private RollingAverage<long> rollAvgAvgBps;
        private int rollAvgDepth;
        private int rollAvgRefreshCnt;
        private int rollAvgRefreshRate;

        public string ProgressPanelTitle =>
        (
            speedLimitIpg > 0
            ? plugin.FormatResourceString("LOCNowPlayingProgressSpeedLimitTitleFmt2", speedLimitIpg, GameTitle)
            : plugin.FormatResourceString("LOCNowPlayingProgressTitleFmt", GameTitle)
        );
        
        public string ProgressTitleBrush => speedLimitIpg > 0 ? "SlowInstallBrush" : "GlyphBrush";

        public double PercentDone => percentDone;
        public string ProgressBarBrush => speedLimitIpg > 0? "TopPanelSlowInstallFgBrush" : "TopPanelInstallFgBrush";

        public string CopiedFilesAndBytesProgress =>
        (
            PreparingToInstall
            ? plugin.GetResourceString("LOCNowPlayingPreparingToInstall")
            : string.Format(formatStringFilesAndBytes, copiedFilesOfFiles, copiedBytesOfBytes)
        );

        public string CurrentFile => PreparingToInstall ? "" : string.Format(formatStringCopyingFile, currentFile);
        public string SpeedDurationEta =>
        (
            PreparingToInstall ? "" : string.Format(formatStringSpeedDurationEta, averageSpeed, duration, timeRemaining)
        );

        public InstallProgressViewModel(NowPlayingInstallController controller, int speedLimitIpg = 0)
        {
            this.plugin = controller.plugin;
            this.controller = controller;
            this.cacheManager = controller.cacheManager;
            this.jobStats = controller.jobStats;
            this.gameCache = controller.gameCache;
            this.PauseInstallCommand = new RelayCommand(() => controller.RequestPauseInstall());
            this.CancelInstallCommand = new RelayCommand(() => controller.RequestCancellInstall());

            this.formatStringCopyingFile = (plugin.GetResourceString("LOCNowPlayingTermsCopying") ?? "Copying") + " '{0}'...";
            this.formatStringXofY = plugin.GetResourceFormatString("LOCNowPlayingProgressXofYFmt2", 2) ?? "{0} of {1}";
            this.formatStringFilesAndBytes = plugin.GetResourceFormatString("LOCNowPlayingProgressFilesAndBytesFmt2", 2) ?? "{0} files,  {1} copied";
            this.formatStringSpeedDurationEta = (plugin.GetResourceString("LOCNowPlayingTermsSpeed") ?? "Speed") + ": {0},   ";
            this.formatStringSpeedDurationEta += (plugin.GetResourceString("LOCNowPlayingTermsDuration") ?? "Duration") + ": {1},   ";
            this.formatStringSpeedDurationEta += (plugin.GetResourceString("LOCNowPlayingTermsEta") ?? "ETA") + ": {2}";
            
            PrepareToInstall(speedLimitIpg);
        }

        public void PrepareToInstall(int speedLimitIpg = 0)
        {
            // . Start in "Preparing to install..." state; until job is underway & statistics are updated 
            this.PreparingToInstall = true;
            this.speedLimitIpg = speedLimitIpg;

            cacheManager.gameCacheManager.eJobStatsUpdated += OnJobStatsUpdated;
            cacheManager.gameCacheManager.eJobCancelled += OnJobDone;
            cacheManager.gameCacheManager.eJobDone += OnJobDone;

            // . initialize any rolling average stats
            this.rollAvgDepth = 50;
            this.rollAvgAvgBps = new RollingAvgLong(rollAvgDepth, cacheManager.GetInstallAverageBps(gameCache.InstallDir, speedLimitIpg));
            this.rollAvgRefreshCnt = 0;
            this.rollAvgRefreshRate = 50;

            this.filesCopied = 0;
            this.bytesCopied = "-";
        }

        /// <summary>
        /// The Model's OnJobStatsUpdated event will notify us whenever stats 
        /// have been updated.
        /// </summary>
        private void OnJobStatsUpdated(object sender, string cacheId)
        {
            if (cacheId == gameCache.Id)
            {
                if (preparingToInstall)
                {
                    PreparingToInstall = false;

                    // . First update only: get auto scale for and bake "OfBytes" to copy string.
                    bytesScale = SmartUnits.GetBytesAutoScale(jobStats.BytesToCopy);
                    bytesToCopy = SmartUnits.Bytes(jobStats.BytesToCopy, userScale: bytesScale);

                    // . Initialize copied files of files and bytes of bytes progress. 
                    filesCopied = jobStats.FilesCopied;
                    bytesCopied = SmartUnits.Bytes(jobStats.GetTotalBytesCopied(), userScale: bytesScale, showUnits: false);
                    copiedFilesOfFiles = string.Format(formatStringXofY, jobStats.FilesCopied, jobStats.FilesToCopy);
                    copiedBytesOfBytes = string.Format(formatStringXofY, bytesCopied, bytesToCopy);

                    OnPropertyChanged(nameof(CopiedFilesAndBytesProgress));
                    OnPropertyChanged(nameof(ProgressPanelTitle));
                    OnPropertyChanged(nameof(ProgressTitleBrush));
                    OnPropertyChanged(nameof(ProgressBarBrush));
                }

                // . update any real-time properties that have changed
                double dval = jobStats.UpdatePercentDone();
                if (percentDone != dval)
                {
                    percentDone = dval;
                    OnPropertyChanged(nameof(PercentDone));
                }

                string sval = SmartUnits.Bytes(jobStats.GetTotalBytesCopied(), userScale: bytesScale, showUnits: false);
                if (filesCopied != jobStats.FilesCopied || bytesCopied != sval)
                {
                    if (filesCopied != jobStats.FilesCopied)
                    {
                        filesCopied = jobStats.FilesCopied;
                        copiedFilesOfFiles = string.Format(formatStringXofY, jobStats.FilesCopied, jobStats.FilesToCopy);
                    }
                    if (bytesCopied != sval)
                    {
                        bytesCopied = sval;
                        copiedBytesOfBytes = string.Format(formatStringXofY, bytesCopied, bytesToCopy);
                    }
                    OnPropertyChanged(nameof(CopiedFilesAndBytesProgress));
                }

                sval = jobStats.CurrFileName;
                if (currentFile != sval)
                {
                    currentFile = sval;
                    OnPropertyChanged(nameof(CurrentFile));
                }

                sval = SmartUnits.Duration(jobStats.GetDuration());
                bool durationUpdated = duration != sval;
                if (durationUpdated)
                {
                    duration = sval;
                    OnPropertyChanged(nameof(SpeedDurationEta));
                }

                // . update rolling averaged stats and display at lower refresh rate... 
                if (rollAvgRefreshCnt++ == 0 || durationUpdated)
                {
                    rollAvgAvgBps.Push(jobStats.GetAvgBytesPerSecond());

                    var timeSpanRemaining = jobStats.GetTimeRemaining(rollAvgAvgBps.GetAverage());
                    sval = SmartUnits.Duration(timeSpanRemaining);

                    if (timeRemaining != sval)
                    {
                        timeRemaining = sval;
                        OnPropertyChanged(nameof(SpeedDurationEta));
                        gameCache.UpdateInstallEta(timeSpanRemaining);
                    }

                    sval = SmartUnits.Bytes(rollAvgAvgBps.GetAverage(), decimals: 1) + "/s";
                    if (averageSpeed != sval)
                    {
                        averageSpeed = sval;
                        OnPropertyChanged(nameof(SpeedDurationEta));
                    }
                }
                if (rollAvgRefreshCnt >= rollAvgRefreshRate)
                {
                    rollAvgRefreshCnt = 0;
                }

                gameCache.UpdateCacheSize();
            }
        }

        private void OnJobDone(object sender, GameCacheJob job)
        {
            if (job.entry.Id == gameCache.Id)
            {
                gameCache.UpdateCacheSize();
                gameCache.UpdateNowInstalling(false);
                if (gameCache.State == GameCacheState.Populated || gameCache.State == GameCacheState.Played)
                {
                    gameCache.UpdateInstallEta(TimeSpan.Zero);
                }
                else
                {
                    gameCache.UpdateInstallEta();
                }

                // . all properties updated  
                OnPropertyChanged(null);

                cacheManager.gameCacheManager.eJobStatsUpdated -= OnJobStatsUpdated;
                cacheManager.gameCacheManager.eJobCancelled -= OnJobDone;
                cacheManager.gameCacheManager.eJobDone -= OnJobDone;
            }
        }

    }
}

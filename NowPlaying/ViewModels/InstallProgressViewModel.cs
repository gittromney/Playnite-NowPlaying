using NowPlaying.Models;
using NowPlaying.Utils;
using Playnite.SDK;
using System;
using System.Security.Permissions;
using System.Threading;
using static NowPlaying.ViewModels.TopPanelViewModel;

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
                    OnPropertyChanged(nameof(CopiedFilesAndBytesProgress));
                    OnPropertyChanged(nameof(CurrentFile));
                    OnPropertyChanged(nameof(SpeedDurationEta));
                }
            }
        }

        private bool isSpeedLimited;
        public bool IsSpeedLimited
        {
            get => isSpeedLimited;
            set
            {
                if (isSpeedLimited != value)
                {
                    isSpeedLimited = value;
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
        private double percentDone;
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

        public string ProgressPanelTitle => (IsSpeedLimited ? 
                                             $"Installing Game Cache w/Speed Limit for '{GameTitle}'..." : 
                                             $"Installing Game Cache for '{GameTitle}'...");
        public string ProgressTitleBrush => IsSpeedLimited ? "SlowInstallBrush" : "GlyphBrush";

        public double PercentDone => percentDone;
        public string ProgressBarBrush => IsSpeedLimited ? "TopPanelSlowInstallFgBrush" : "TopPanelInstallFgBrush";

        public string CopiedFilesOfFiles => copiedFilesOfFiles;
        public string CopiedBytesOfBytes => copiedBytesOfBytes;
        public string CopiedFilesAndBytesProgress => PreparingToInstall ? "Preparing to install..." : $"{copiedFilesOfFiles} files,  {copiedBytesOfBytes} copied";
        public string CurrentFile => PreparingToInstall ? "" : $"Copying '{currentFile}'...";
        public string SpeedDurationEta => PreparingToInstall ? "" : $"Speed: {averageSpeed},   Time: {duration},   ETA: {timeRemaining}";

        public InstallProgressViewModel(NowPlayingInstallController controller, bool isSpeedLimited = false)
        {
            this.plugin = controller.plugin;
            this.controller = controller;
            this.cacheManager = controller.cacheManager;
            this.jobStats = controller.jobStats;
            this.gameCache = controller.gameCache;
            this.PauseInstallCommand = new RelayCommand(() => controller.RequestPauseInstall());
            this.CancelInstallCommand = new RelayCommand(() => controller.RequestCancellInstall());
            PrepareToInstall(isSpeedLimited);
        }

        public void PrepareToInstall(bool isSpeedLimited = false)
        {
            // . Start in "Preparing to install..." state; until job is underway & statistics are updated 
            this.PreparingToInstall = true;
            this.IsSpeedLimited = isSpeedLimited;

            cacheManager.gameCacheManager.eJobStatsUpdated += OnJobStatsUpdated;
            cacheManager.gameCacheManager.eJobCancelled += OnJobDone;
            cacheManager.gameCacheManager.eJobDone += OnJobDone;

            // . initialize any rolling average stats
            this.rollAvgDepth = 50;
            this.rollAvgAvgBps = new RollingAvgLong(rollAvgDepth, cacheManager.GetInstallAverageBps(gameCache.InstallDir, isSpeedLimited));
            this.rollAvgRefreshCnt = 0;
            this.rollAvgRefreshRate = 50;
        }

        /// <summary>
        /// The Model's OnJobStatsUpdated event will notify us whenever stats 
        /// have been updated.
        /// </summary>
        private void OnJobStatsUpdated(object sender, string cacheId)
        {
            if (cacheId == gameCache.Id)
            {
                PreparingToInstall = false;

                // . update any real-time properties that have changed
                double dval = jobStats.UpdatePercentDone();
                if (percentDone != dval)
                {
                    percentDone = dval;
                    OnPropertyChanged(nameof(PercentDone));
                }

                string sval = string.Format("{0} of {1}", jobStats.FilesCopied, jobStats.FilesToCopy);
                if (copiedFilesOfFiles != sval)
                {
                    copiedFilesOfFiles = sval;
                    OnPropertyChanged(nameof(CopiedFilesOfFiles));
                    OnPropertyChanged(nameof(CopiedFilesAndBytesProgress));
                }

                sval = SmartUnits.BytesOfBytes(jobStats.GetTotalBytesCopied(), jobStats.BytesToCopy);
                if (copiedBytesOfBytes != sval)
                {
                    copiedBytesOfBytes = sval;
                    OnPropertyChanged(nameof(CopiedBytesOfBytes));
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

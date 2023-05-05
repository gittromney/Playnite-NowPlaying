using NowPlaying.Models;
using NowPlaying.Core;
using Playnite.SDK;
using System.Xml;
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

        public bool PreparingToInstall;

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
  
        public string ProgressPanelTitle => $"Installing {GameTitle}'s Game Cache";
        public double PercentDone => percentDone;
        public string CopiedFilesOfFiles => copiedFilesOfFiles;
        public string CopiedBytesOfBytes => copiedBytesOfBytes;
        public string CopiedFilesAndBytesProgress => PreparingToInstall ? "Preparing to install..." : $"{copiedFilesOfFiles} files,  {copiedBytesOfBytes} copied";
        public string CurrentFile => PreparingToInstall ? "" : $"Copying '{currentFile}'...";
        public string SpeedDurationEta => PreparingToInstall ? "" : $"Speed: {averageSpeed},   Time: {duration},   ETA: {timeRemaining}";

        //public string Duration => PreparingToInstall ? "" : $"Duration: {duration}";
        //public string TimeRemaining => PreparingToInstall ? "" : $"Estimated time remaining: {timeRemaining}";
        //public string AverageSpeed => PreparingToInstall ? "" : $"Speed: {averageSpeed}";

        public InstallProgressViewModel(NowPlayingInstallController controller)
        {
            this.plugin = controller.plugin;
            this.controller = controller;
            this.cacheManager = controller.cacheManager;
            this.jobStats = controller.jobStats;
            this.gameCache = controller.gameCache;

            // . Start in "Preparing to install..." state; until job is underway & statistics are updated 
            this.PreparingToInstall = true;

            cacheManager.gameCacheManager.eJobStatsUpdated += OnJobStatsUpdated;
            cacheManager.gameCacheManager.eJobCancelled += OnJobDone;
            cacheManager.gameCacheManager.eJobDone += OnJobDone;

            // . initialize any rolling average stats
            this.rollAvgDepth = 100;
            this.rollAvgAvgBps = new RollingAverageLong(rollAvgDepth, cacheManager.GetInstallAverageBps(gameCache.InstallDir));
            this.rollAvgRefreshCnt = 0;
            this.rollAvgRefreshRate = 100;

            PauseInstallCommand = new RelayCommand(controller.RequestPauseInstall);
            CancelInstallCommand = new RelayCommand(controller.RequestCancellInstall);
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
                if (duration != sval)
                {
                    duration = sval;
                    //OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(SpeedDurationEta));
                }

                // . update rolling averaged stats + display at lower refresh rate... 
                rollAvgAvgBps.Push(jobStats.GetAvgBytesPerSecond());
                if (rollAvgRefreshCnt++ == 0)
                {
                    var timeSpanRemaining = jobStats.GetTimeRemaining(rollAvgAvgBps.GetAverage());
                    sval = SmartUnits.Duration(timeSpanRemaining);
                    if (timeRemaining != sval)
                    {
                        timeRemaining = sval;
                        //OnPropertyChanged(nameof(TimeRemaining));
                        OnPropertyChanged(nameof(SpeedDurationEta));

                        // . also update game cache view model, if applicable
                        gameCache.UpdateInstallEta(timeSpanRemaining);
                    }

                    sval = SmartUnits.Bytes(rollAvgAvgBps.GetAverage(), decimals: 1) + "/s";
                    if (averageSpeed != sval)
                    {
                        averageSpeed = sval;
                        //OnPropertyChanged(nameof(AverageSpeed));
                        OnPropertyChanged(nameof(SpeedDurationEta));
                    }
                }
                else if (rollAvgRefreshCnt == rollAvgRefreshRate)
                {
                    rollAvgRefreshCnt = 0;
                }

                ///
                // . updated affected game cache view model properties
                //
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

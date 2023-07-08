using NowPlaying.Models;
using NowPlaying.Utils;
using Playnite.SDK;
using System;
using System.Timers;
using System.Windows.Controls;

namespace NowPlaying.ViewModels
{
    public class InstallProgressViewModel : ViewModelBase
    {
        private readonly NowPlaying plugin;
        private readonly NowPlayingInstallController controller;
        private readonly GameCacheManagerViewModel cacheManager;
        private readonly GameCacheViewModel gameCache;
        private readonly RoboStats jobStats;
        private readonly Timer speedEtaRefreshTimer;
        private readonly long speedEtaInterval = 500;  // calc avg speed, Eta every 1/2 second
        private long totalBytesCopied;
        private long prevTotalBytesCopied;

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

                    OnPropertyChanged(nameof(ProgressBgBrush));
                    OnPropertyChanged(nameof(ProgressValue));
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
        private string formatStringCopyingFilePfr;
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
        private long   currentFileSize;
        private bool   partialFileResume;
        private string duration;
        private string timeRemaining;
        private string currentSpeed;
        private string averageSpeed;

        // . Transfer speed rolling averages
        public RollingAvgLong currSpeedRollAvgBps;
        public RollingAvgLong averageSpeedRollAvgBps;
        private readonly int currSpeedRollAvgDepth = 16;      // current speed → 8 second rolling average
        private readonly int averageSpeedRollAvgDepth = 256;  // average speed → approx 4 minute rolling average

        public string ProgressPanelTitle =>
        (
            speedLimitIpg > 0 ? plugin.FormatResourceString("LOCNowPlayingProgressSpeedLimitTitleFmt2", speedLimitIpg, GameTitle) :
            plugin.FormatResourceString("LOCNowPlayingProgressTitleFmt", GameTitle)
        );
        
        public string ProgressTitleBrush => speedLimitIpg > 0 ? "SlowInstallBrush" : "GlyphBrush";

        public string ProgressValue => PreparingToInstall ? "" : $"{percentDone:n1}%";
        public double PercentDone => percentDone;
        public string ProgressBarBrush => speedLimitIpg > 0 ? "TopPanelSlowInstallFgBrush" : "TopPanelInstallFgBrush";
        public string ProgressBgBrush => PreparingToInstall ? "TopPanelProcessingBgBrush" : "TransparentBgBrush";

        public string CopiedFilesAndBytesProgress =>
        (
            PreparingToInstall
            ? plugin.GetResourceString("LOCNowPlayingPreparingToInstall")
            : string.Format(formatStringFilesAndBytes, copiedFilesOfFiles, copiedBytesOfBytes)
        );

        public string CurrentFile =>
        (
            PreparingToInstall ? "" :
            partialFileResume ? string.Format(formatStringCopyingFilePfr, currentFile, SmartUnits.Bytes(currentFileSize)) :
            string.Format(formatStringCopyingFile, currentFile, SmartUnits.Bytes(currentFileSize))
        );

        public string SpeedDurationEta =>
        (
            PreparingToInstall ? "" : string.Format(formatStringSpeedDurationEta, currentSpeed, averageSpeed, duration, timeRemaining)
        );

        public InstallProgressViewModel(NowPlayingInstallController controller, int speedLimitIpg=0, bool partialFileResume=false)
        {
            this.plugin = controller.plugin;
            this.controller = controller;
            this.cacheManager = controller.cacheManager;
            this.jobStats = controller.jobStats;
            this.gameCache = controller.gameCache;
            this.PauseInstallCommand = new RelayCommand(() => controller.RequestPauseInstall());
            this.CancelInstallCommand = new RelayCommand(() => controller.RequestCancellInstall());
            this.speedEtaRefreshTimer = new Timer() { Interval = speedEtaInterval };

            this.formatStringCopyingFile = (plugin.GetResourceString("LOCNowPlayingTermsCopying") ?? "Copying") + " '{0}' ({1})...";
            this.formatStringCopyingFilePfr = (plugin.GetResourceString("LOCNowPlayingTermsCopying") ?? "Copying") + " '{0}' ({1}) ";
            this.formatStringCopyingFilePfr += (plugin.GetResourceString("LOCNowPlayingWithPartialFileResume") ?? "w/partial file resume") + "...";
            this.formatStringXofY = plugin.GetResourceFormatString("LOCNowPlayingProgressXofYFmt2", 2) ?? "{0} of {1}";
            this.formatStringFilesAndBytes = plugin.GetResourceFormatString("LOCNowPlayingProgressFilesAndBytesFmt2", 2) ?? "{0} files,  {1} copied";
            this.formatStringSpeedDurationEta = (plugin.GetResourceString("LOCNowPlayingTermsSpeed") ?? "Speed") + ": {0},   ";
            this.formatStringSpeedDurationEta += (plugin.GetResourceString("LOCNowPlayingTermsAvgSpeed") ?? "Average speed") + ": {1},   ";
            this.formatStringSpeedDurationEta += (plugin.GetResourceString("LOCNowPlayingTermsDuration") ?? "Duration") + ": {2},   ";
            this.formatStringSpeedDurationEta += (plugin.GetResourceString("LOCNowPlayingTermsEta") ?? "ETA") + ": {3}";
            
            PrepareToInstall(speedLimitIpg, partialFileResume);
        }

        public void PrepareToInstall(int speedLimitIpg=0, bool partialFileResume=false)
        {
            // . Start in "Preparing to install..." state; until job is underway & statistics are updated 
            this.PreparingToInstall = true;
            this.SpeedLimitIpg = speedLimitIpg;
            this.partialFileResume = partialFileResume;

            cacheManager.gameCacheManager.eJobStatsUpdated += OnJobStatsUpdated;
            cacheManager.gameCacheManager.eJobCancelled += OnJobDone;
            cacheManager.gameCacheManager.eJobDone += OnJobDone;
            speedEtaRefreshTimer.Elapsed += OnSpeedEtaRefreshTimerElapsed;
            this.currentSpeed = "-";

            // . initialize any rolling average stats
            var avgBytesPerFile = gameCache.InstallSize / gameCache.InstallFiles;
            var avgBps = cacheManager.GetInstallAverageBps(gameCache.InstallDir, avgBytesPerFile, speedLimitIpg);
            this.currSpeedRollAvgBps = new RollingAvgLong(currSpeedRollAvgDepth, avgBps);
            this.averageSpeedRollAvgBps = new RollingAvgLong(averageSpeedRollAvgDepth, avgBps);

            this.filesCopied = 0;
            this.bytesCopied = "-";
        }

        private void OnSpeedEtaRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            string sval = SmartUnits.Duration(jobStats.GetDuration());
            bool durationUpdated = duration != sval;
            if (durationUpdated)
            {
                duration = sval;
                OnPropertyChanged(nameof(SpeedDurationEta));
            }

            // . current speed
            long intervalBytesCopied = totalBytesCopied - prevTotalBytesCopied;
            long currentBps = (long)((1000.0 * intervalBytesCopied) / speedEtaInterval);
            currSpeedRollAvgBps.Push(currentBps);
            prevTotalBytesCopied = totalBytesCopied;

            sval = SmartUnits.Bytes(currSpeedRollAvgBps.GetAverage(), decimals: 1) + "/s";
            if (currentSpeed != sval)
            {
                currentSpeed = sval;
                OnPropertyChanged(nameof(SpeedDurationEta));
            }

            // . long term average speed, ETA
            var currentAvgBps = jobStats.GetAvgBytesPerSecond();
            averageSpeedRollAvgBps.Push(currentAvgBps);

            var averageAvgBps = averageSpeedRollAvgBps.GetAverage();
            var timeSpanRemaining = jobStats.GetTimeRemaining(averageAvgBps);
            sval = SmartUnits.Duration(timeSpanRemaining);

            if (timeRemaining != sval)
            {
                timeRemaining = sval;
                OnPropertyChanged(nameof(SpeedDurationEta));
                gameCache.UpdateInstallEta(timeSpanRemaining);
            }

            sval = SmartUnits.Bytes(averageAvgBps, decimals: 1) + "/s";
            if (averageSpeed != sval)
            {
                averageSpeed = sval;
                OnPropertyChanged(nameof(SpeedDurationEta));
            }
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

                    OnSpeedEtaRefreshTimerElapsed(null, null); // initialize SpeedDurationEta
                    speedEtaRefreshTimer.Start();              // -> update every 1/2 second thereafter

                    // . First update only: get auto scale for and bake "OfBytes" to copy string.
                    bytesScale = SmartUnits.GetBytesAutoScale(jobStats.BytesToCopy);
                    bytesToCopy = SmartUnits.Bytes(jobStats.BytesToCopy, userScale: bytesScale);

                    // . Initiallize 'current speed' copied bytes trackers
                    totalBytesCopied = jobStats.GetTotalBytesCopied();
                    prevTotalBytesCopied = totalBytesCopied;

                    // . Initialize copied files of files and bytes of bytes progress.
                    filesCopied = jobStats.FilesCopied;
                    bytesCopied = SmartUnits.Bytes(totalBytesCopied, userScale: bytesScale, showUnits: false);
                    copiedFilesOfFiles = string.Format(formatStringXofY, jobStats.FilesCopied, jobStats.FilesToCopy);
                    copiedBytesOfBytes = string.Format(formatStringXofY, bytesCopied, bytesToCopy);

                    OnPropertyChanged(nameof(CopiedFilesAndBytesProgress));
                    OnPropertyChanged(nameof(ProgressPanelTitle));
                    OnPropertyChanged(nameof(ProgressTitleBrush));
                    OnPropertyChanged(nameof(ProgressBarBrush));
                    OnPropertyChanged(nameof(ProgressBgBrush));
                    OnPropertyChanged(nameof(ProgressValue));
                }

                // . update any real-time properties that have changed
                double dval = jobStats.UpdatePercentDone();
                if (percentDone != dval)
                {
                    percentDone = dval;
                    OnPropertyChanged(nameof(PercentDone));
                    OnPropertyChanged(nameof(ProgressValue));
                }

                totalBytesCopied = jobStats.GetTotalBytesCopied();

                string sval = SmartUnits.Bytes(totalBytesCopied, userScale: bytesScale, showUnits: false);
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
                if (currentFile != sval || partialFileResume != jobStats.PartialFileResume)
                {
                    currentFile = jobStats.CurrFileName;
                    currentFileSize = jobStats.CurrFileSize;
                    partialFileResume = jobStats.PartialFileResume;
                    OnPropertyChanged(nameof(CurrentFile));
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

                speedEtaRefreshTimer.Stop();
                speedEtaRefreshTimer.Elapsed -= OnSpeedEtaRefreshTimerElapsed;
            }
        }

    }
}

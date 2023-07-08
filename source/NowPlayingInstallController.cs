using NowPlaying.Utils;
using NowPlaying.Models;
using NowPlaying.ViewModels;
using NowPlaying.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace NowPlaying
{
    public class NowPlayingInstallController : InstallController
    {
        private readonly ILogger logger = NowPlaying.logger;
        private readonly NowPlayingSettings settings;
        private readonly IPlayniteAPI PlayniteApi;
        private readonly Game nowPlayingGame;

        public readonly NowPlaying plugin;
        public readonly RoboStats jobStats;
        public readonly GameCacheViewModel gameCache;
        public readonly GameCacheManagerViewModel cacheManager;
        public readonly InstallProgressViewModel progressViewModel;
        public readonly InstallProgressView progressView;
        private Action speedLimitChangeOnPaused;
        public int speedLimitIpg;

        private bool deleteCacheOnJobCancelled { get; set; } = false;

        public NowPlayingInstallController(NowPlaying plugin, Game nowPlayingGame, GameCacheViewModel gameCache, int speedLimitIpg = 0) 
            : base(nowPlayingGame)
        {
            this.plugin = plugin;
            this.settings = plugin.Settings;
            this.PlayniteApi = plugin.PlayniteApi;
            this.cacheManager = plugin.cacheManager;
            this.nowPlayingGame = nowPlayingGame;
            this.gameCache = gameCache;
            bool partialFileResume = plugin.Settings.PartialFileResume == EnDisThresh.Enabled;
            this.jobStats = new RoboStats(partialFileResume: partialFileResume);
            this.progressViewModel = new InstallProgressViewModel(this, speedLimitIpg, partialFileResume);
            this.progressView = new InstallProgressView(progressViewModel);
            this.speedLimitChangeOnPaused = null;
            this.speedLimitIpg = speedLimitIpg;
        }

        public override void Install(InstallActionArgs args)
        {
            if (plugin.cacheInstallQueuePaused) return;

            // . Workaround: prevent (accidental) install while install|uninstall in progress
            //   -> Note, better solution requires a Playnite fix => Play CanExecute=false while IsInstalling=true||IsUninstalling=true
            //   -> Also, while we're at it: Playnite's Install/Uninstall CanExecute=false while IsInstalling=true||IsUninstalling=true
            //
            if (!plugin.CacheHasUninstallerQueued(gameCache.Id))
            {
                if (plugin.EnqueueCacheInstallerIfUnique(this))
                {
                    // . Proceed only if installer is first -- in the "active install" spot...
                    // . Otherwise, when the active install controller finishes (UninstallDone | InstallPausedCancelled),
                    //   it will automatically invoke NowPlayingInstall on the next controller in the queue.
                    //   
                    if (plugin.cacheInstallQueue.First() == this)
                    {
                        Task.Run(() => NowPlayingInstallAsync());
                    }
                    else
                    {
                        plugin.UpdateInstallQueueStatuses();
                        logger.Info($"NowPlaying installation of '{gameCache.Title}' game cache queued ({gameCache.InstallQueueStatus}).");
                    }
                }
            }
        }

        public async Task NowPlayingInstallAsync()
        {
            bool isAccessible = await plugin.CheckIfGameInstallDirIsAccessibleAsync(gameCache.Title, gameCache.InstallDir);
            
            plugin.topPanelViewModel.NowInstalling(gameCache, isSpeedLimited: speedLimitIpg > 0);

            gameCache.UpdateCacheSpaceWillFit();
            
            if (isAccessible && gameCache.CacheWillFit)
            {
                deleteCacheOnJobCancelled = false;
 
                plugin.UpdateInstallQueueStatuses();
                if (speedLimitIpg > 0)
                {
                    logger.Info($"NowPlaying speed limited installation of '{gameCache.Title}' game cache started (IPG={speedLimitIpg}).");
                }
                else
                {
                    logger.Info($"NowPlaying installation of '{gameCache.Title}' game cache started.");
                }

                nowPlayingGame.IsInstalling = true;
                gameCache.UpdateNowInstalling(true);

                // update speed and display the progress panel
                progressViewModel.SpeedLimitIpg = speedLimitIpg;
                plugin.panelViewModel.InstallProgressView = progressView;

                // . Partial file resume option (over network only)
                //
                PartialFileResumeOpts pfrOpts = null;

                if (DirectoryUtils.IsOnNetworkDrive(gameCache.InstallDir))
                {
                    // . partial file resume settings
                    pfrOpts = new PartialFileResumeOpts
                    {
                        Mode = plugin.Settings.PartialFileResume,
                        FileSizeThreshold = (long)(plugin.Settings.PfrThresholdGigaBytes * 1073741824),
                        OnModeChange = OnPartialFileResumeModeChange
                    };

                    // . initial partial file resume enable (at start of job)
                    gameCache.PartialFileResume = pfrOpts.Mode == EnDisThresh.Enabled || pfrOpts.Mode == EnDisThresh.Threshold && pfrOpts.FileSizeThreshold <= 0;
                    jobStats.PartialFileResume = gameCache.PartialFileResume;
                }

                cacheManager.InstallGameCache(gameCache, jobStats, InstallDone, InstallPausedCancelled, speedLimitIpg, pfrOpts);
            }
            else
            {
                if (!gameCache.CacheWillFit)
                {
                    plugin.NotifyError(plugin.FormatResourceString("LOCNowPlayingInstallSkippedFmt", gameCache.Title));
                }
                nowPlayingGame.IsInstalling = false;
                PlayniteApi.Database.Games.Update(nowPlayingGame);
                plugin.DequeueInstallerAndInvokeNextAsync(gameCache.Id);
            }
        }

        private void OnPartialFileResumeModeChange(bool newPfrValue, bool saveAvgBps)
        {
            // . update averageBps for this install device and save to JSON file
            var avgBytesPerFile = gameCache.InstallSize / gameCache.InstallFiles;
            if (saveAvgBps)
            {
                var avgBps = jobStats.GetAvgBytesPerSecond();
                if (avgBps > 0)
                {
                    cacheManager.UpdateInstallAverageBps(gameCache.InstallDir, avgBytesPerFile, avgBps, speedLimitIpg);
                }
            }
            gameCache.PartialFileResume = newPfrValue;

            // . initiallize rolling average for PFR mode change
            var initAvgBps = cacheManager.GetInstallAverageBps(gameCache.InstallDir, avgBytesPerFile, speedLimitIpg);
            progressViewModel.averageSpeedRollAvgBps.Init(initAvgBps);
        }

        private void InstallDone(GameCacheJob job)
        {
            // . collapse the progress panel
            plugin.panelViewModel.InstallProgressView = null;

            // . exit installing state
            nowPlayingGame.IsInstalled = true;
            nowPlayingGame.IsInstalling = false; // needed if invoked from Panel View
            PlayniteApi.Database.Games.Update(nowPlayingGame);
            gameCache.UpdateNowInstalling(false);

            // . update state to JSON file 
            cacheManager.SaveGameCacheEntriesToJson();

            // . update averageBps for this install device and save to JSON file
            var avgBytesPerFile = gameCache.InstallSize / gameCache.InstallFiles;
            bool partialFileResume = gameCache.PartialFileResume;
            cacheManager.UpdateInstallAverageBps(job.entry.InstallDir, avgBytesPerFile, jobStats.GetAvgBytesPerSecond(), speedLimitIpg);
            
            InvokeOnInstalled(new GameInstalledEventArgs());

            plugin.NotifyInfo(plugin.FormatResourceString("LOCNowPlayingInstallDoneFmt", gameCache.Title));

            plugin.DequeueInstallerAndInvokeNextAsync(gameCache.Id);
        }

        public void RequestPauseInstall(Action speedLimitChangeOnPaused = null)
        {
            this.speedLimitChangeOnPaused = speedLimitChangeOnPaused; 
            deleteCacheOnJobCancelled = false;
            cacheManager.CancelInstall(gameCache.Id);
        }

        public void RequestCancellInstall()
        {
            deleteCacheOnJobCancelled = true;
            cacheManager.CancelInstall(gameCache.Id);
        }

        public void InstallPausedCancelled(GameCacheJob job)
        {
            // . collapse the progress windows
            plugin.panelViewModel.InstallProgressView = null;

            // . exit Installing state
            InvokeOnInstalled(new GameInstalledEventArgs());
            nowPlayingGame.IsInstalling = false; // needed if invoked from Panel View
            PlayniteApi.Database.Games.Update(nowPlayingGame);
            gameCache.UpdateNowInstalling(false);

            if (deleteCacheOnJobCancelled)
            {
                logger.Info(plugin.FormatResourceString("LOCNowPlayingInstallCancelledFmt", gameCache.Title));

                // . enter uninstalling state
                nowPlayingGame.IsUninstalling = true;
                PlayniteApi.Database.Games.Update(nowPlayingGame);
                gameCache.UpdateNowUninstalling(true);

                // . delete the cache
                //   -> allow for retries as robocopy.exe/OS releases file locks
                //
                Task.Run(() =>
                {
                    if (DirectoryUtils.DeleteDirectory(gameCache.CacheDir, maxRetries: 50))
                    {
                        gameCache.entry.State = GameCacheState.Empty;
                        gameCache.entry.CacheSize = 0;
                        gameCache.entry.CacheSizeOnDisk = 0;
                    }
                    else
                    {
                        plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingDeleteCacheFailedFmt", gameCache.CacheDir));
                    }

                    // exit uninstalling state
                    nowPlayingGame.IsUninstalling = false;
                    PlayniteApi.Database.Games.Update(nowPlayingGame);
                    gameCache.UpdateNowUninstalling(false);
                    gameCache.UpdateCacheSize();
                    gameCache.UpdateStatus();
                });
            }
            else
            {
                gameCache.UpdateCacheSize();
                gameCache.UpdateCacheSpaceWillFit();
                gameCache.UpdateStatus();

                // . if install was paused before completing, revert Game state to uninstalled
                if (gameCache.entry.State != GameCacheState.Populated)
                {
                    nowPlayingGame.IsInstalled = false;
                    PlayniteApi.Database.Games.Update(nowPlayingGame);
                }

                if (job.cancelledOnMaxFill)
                {
                    plugin.NotifyError(plugin.FormatResourceString("LOCNowPlayingPausedMaxFillFmt", gameCache.Title));
                }
                else if (job.cancelledOnDiskFull)
                {
                    plugin.NotifyError(plugin.FormatResourceString("LOCNowPlayingPausedDiskFullFmt", gameCache.Title));
                }
                else if (job.cancelledOnError)
                {
                    string seeLogFile = plugin.SaveJobErrorLogAndGetMessage(job, ".install.txt");
                    plugin.PopupError(plugin.FormatResourceString("LOCNowPlayingInstallTerminatedFmt", gameCache.Title) + seeLogFile);
                }
                else if (plugin.cacheInstallQueuePaused && speedLimitChangeOnPaused == null)
                {
                    if (settings.NotifyOnInstallWhilePlayingActivity)
                    {
                        plugin.NotifyInfo(plugin.FormatResourceString("LOCNowPlayingPausedGameStartedFmt", gameCache.Title));
                    }
                    else
                    {
                        logger.Info(plugin.FormatResourceString("LOCNowPlayingPausedGameStartedFmt", gameCache.Title));
                    }
                }
                else
                {
                    logger.Info($"NowPlaying installation paused for '{gameCache.Title}'.");
                }
            }

            // . update state in JSON file 
            cacheManager.SaveGameCacheEntriesToJson();

            // . update averageBps for this install device and save to JSON file
            if (jobStats.BytesCopied > 0 && gameCache.CacheSize > jobStats.ResumeBytes)
            {
                var avgBytesPerFile = gameCache.InstallSize / gameCache.InstallFiles;
                cacheManager.UpdateInstallAverageBps(job.entry.InstallDir, avgBytesPerFile, jobStats.GetAvgBytesPerSecond(), speedLimitIpg);
            }
            if (!plugin.cacheInstallQueuePaused) 
            { 
                plugin.DequeueInstallerAndInvokeNextAsync(gameCache.Id);
            }
            else
            {
                // . update install queue status of queued installers & top panel status
                plugin.UpdateInstallQueueStatuses();
                plugin.topPanelViewModel.InstallQueuePaused();
                speedLimitChangeOnPaused?.Invoke();
            }
        }

    }
}

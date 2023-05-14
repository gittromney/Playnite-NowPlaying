using NowPlaying.Models;
using NowPlaying.ViewModels;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static NowPlaying.Models.GameCacheManager;
using static NowPlaying.NowPlayingSettings;

namespace NowPlaying
{
    public class NowPlayingUninstallController : UninstallController
    {
        private readonly NowPlaying plugin;
        private readonly NowPlayingSettings settings;
        private readonly IPlayniteAPI PlayniteApi;
        private readonly GameCacheManagerViewModel cacheManager;
        private readonly Game nowPlayingGame;
        private readonly string cacheDirectory;
        private readonly string installDirectory;

        public readonly GameCacheViewModel gameCache;

        public NowPlayingUninstallController(NowPlaying plugin, Game nowPlayingGame, GameCacheViewModel gameCache) 
            : base(nowPlayingGame)
        {
            this.plugin = plugin;
            this.settings = plugin.Settings;
            this.PlayniteApi = plugin.PlayniteApi;
            this.cacheManager = plugin.cacheManager;
            this.nowPlayingGame = nowPlayingGame;
            this.gameCache = gameCache;
            this.cacheDirectory = gameCache.CacheDir;
            this.installDirectory = gameCache.InstallDir;
        }

        public override void Uninstall(UninstallActionArgs args)
        {
            if (!plugin.CheckIfGameInstallDirIsAccessible(gameCache.Title, gameCache.InstallDir)) return;

            // . enqueue our controller (but don't add more than once)
            if (plugin.EnqueueCacheUninstallerIfUnique(this))
            {
                // . Proceed only if uninstaller is first -- in the "active install" spot...
                // . Otherwise, when the active install controller finishes it will
                //   automatically invoke NowPlayingUninstall on the next controller in the queue.
                //   
                if (plugin.cacheUninstallQueue.First() == this)
                {
                    Task.Run(() => NowPlayingUninstall());
                }
                else
                {
                    plugin.UpdateUninstallQueueStatuses();
                    NowPlaying.logger.Info($"NowPlaying uninstall of '{gameCache.Title}' game cache queued ({gameCache.UninstallQueueStatus}).");
                }
            }
        }

        public void NowPlayingUninstall()
        {
            bool cacheWriteBackOption = settings.SyncDirtyCache_DoWhen == DoWhen.Always;
            bool cancelUninstall = false;
            string gameTitle = nowPlayingGame.Name;

            gameCache.UpdateNowUninstalling(true);

            if (settings.ConfirmUninstall)
            {
                string message = $"Are you sure you want to uninstall the NowPlaying Game Cache for '{gameTitle}'?";
                string caption = "Confirm NowPlaying Uninstall";
                MessageBoxResult userChoice = PlayniteApi.Dialogs.ShowMessage(message, caption, MessageBoxButton.YesNo);
                cancelUninstall = userChoice == MessageBoxResult.No;
            }

            if (!cancelUninstall && settings.SyncDirtyCache_DoWhen == DoWhen.Ask)
            {
                DirtyCheckResult result = cacheManager.CheckCacheDirty(gameCache.Id);
                if (result.isDirty)
                {
                    string caption = "Confirm NowPlaying Cache Write-Back";
                    string nl = System.Environment.NewLine;
                    string message = string.Empty;
                    message += $"{gameTitle}'s Game Cache differs from its installation directory." + nl + nl;
                    message += result.summary;
                    message += "Do you want to copy new/modified files from the Game Cache" + nl;
                    message += $"back to the install directory ({installDirectory})" + nl;
                    message += "before deleting it?";
                    MessageBoxResult userChoice = PlayniteApi.Dialogs.ShowMessage(message, caption, MessageBoxButton.YesNoCancel);
                    cacheWriteBackOption = userChoice == MessageBoxResult.Yes;
                    cancelUninstall = userChoice == MessageBoxResult.Cancel;
                }
            }

            if (!cancelUninstall)
            {
                // . Workaround: prevent (accidental) play while uninstall in progress
                //   -> Note, real solution requires a Playnite fix => Play CanExecute=false while IsUnistalling=true
                //   -> Also, while we're at it: Playnite's Install CanExecute=false while IsInstalling=true
                //
                nowPlayingGame.IsInstalled = false;
                PlayniteApi.Database.Games.Update(nowPlayingGame);

                cacheManager.UninstallGameCache(gameCache, cacheWriteBackOption, OnUninstallDone, OnUninstallCancelled);
            }

            // . Uninstall cancelled during confirmation (above)...
            else
            {
                // . exit uninstalling state
                InvokeOnUninstalled(new GameUninstalledEventArgs());

                // Restore some items that Playnite's uninstall flow may have changed automatically.
                // . NowPlaying Game's InstallDirectory
                //
                nowPlayingGame.InstallDirectory = cacheDirectory;
                nowPlayingGame.IsUninstalling = false; // needed if invoked from Panel View
                nowPlayingGame.IsInstalled = true;
                PlayniteApi.Database.Games.Update(nowPlayingGame);

                gameCache.UpdateNowUninstalling(false);

                plugin.DequeueUninstallerAndInvokeNext(gameCache.Id);
            }
        }

        private void OnUninstallDone(GameCacheJob job)
        {
            plugin.NotifyInfo($"Uninstalled game cache for '{gameCache.Title}'.");

            // . exit uninstalling state
            InvokeOnUninstalled(new GameUninstalledEventArgs());
 
            // Restore some items that Playnite's uninstall flow may have changed automatically.
            // . NowPlaying Game's InstallDirectory
            //
            nowPlayingGame.InstallDirectory = cacheDirectory;
            nowPlayingGame.IsUninstalling = false; // needed if invoked from Panel View
            nowPlayingGame.IsInstalled = false;
            PlayniteApi.Database.Games.Update(nowPlayingGame);

            gameCache.UpdateCacheSize();
            gameCache.UpdateNowUninstalling(false);
            gameCache.UpdateInstallEta();
            gameCache.cacheRoot.UpdateGameCaches();

            // . update state to JSON file 
            cacheManager.SaveGameCacheEntriesToJson();

            plugin.DequeueUninstallerAndInvokeNext(gameCache.Id);
        }

        private void OnUninstallCancelled(GameCacheJob job)
        {
            // . exit uninstalling state
            InvokeOnUninstalled(new GameUninstalledEventArgs());

            nowPlayingGame.IsUninstalling = false; // needed if invoked from Panel View
            PlayniteApi.Database.Games.Update(nowPlayingGame);

            gameCache.UpdateNowUninstalling(false);

            string seeLogFile = plugin.SaveJobErrorLogAndGetMessage(job, ".uninstall.txt");

            if (job.cancelledOnError)
            {
                plugin.PopupError($"Uninstall of '{gameCache.Title}' game cache terminated because of an error.", seeLogFile);
            }

            // . update state in JSON file 
            cacheManager.SaveGameCacheEntriesToJson();

            plugin.DequeueUninstallerAndInvokeNext(gameCache.Id);
        }

    }
}

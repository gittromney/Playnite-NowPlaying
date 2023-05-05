using NowPlaying.ViewModels;
using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NowPlaying
{
    public class NowPlayingGameEnabler
    {
        private readonly NowPlaying plugin;
        private readonly IPlayniteAPI PlayniteApi;
        private readonly GameCacheManagerViewModel cacheManager;
        private readonly Game game;
        private readonly string cacheRootDir;

        public string Id => game.Id.ToString();

        public NowPlayingGameEnabler(NowPlaying plugin, Game game, string cacheRootDir)
        {
            this.plugin = plugin;
            this.PlayniteApi = plugin.PlayniteApi;
            this.cacheManager = plugin.cacheManager;
            this.game = game;
            this.cacheRootDir = cacheRootDir;
        }

        public void Activate()
        {
            if (plugin.EnqueueGameEnablerIfUnique(this))
            {
                // . Proceed only if enabler is first -- in the "active enabler" spot...
                // . Otherwise, it will automatically be invoked by the previous enabler in the queue.
                //   
                if (plugin.gameEnablerQueue.First() == this)
                {
                    // . modify a game to play from the game cache dir, or preview from original install dir
                    Task.Run(() => EnableGameForNowPlaying());
                }
                else
                {
                    var queueStatus = string.Format("{0} of {1}", plugin.gameEnablerQueue.ToList().IndexOf(this), plugin.gameEnablerQueue.Count());
                    NowPlaying.logger.Info($"Enabling of '{game.Name}' for NowPlaying game caching queued ({queueStatus}).");
                }
            }
        }

        public void EnableGameForNowPlaying()
        {
            string cacheId = Id;
            string title = game.Name;
            string installDir = game.InstallDirectory;
            var sourcePlayAction = plugin.GetSourcePlayAction(game);
            string exePath = plugin.GetIncrementalExePath(sourcePlayAction, game);
            string xtraArgs = PlayniteApi.ExpandGameVariables(game, sourcePlayAction.AdditionalArguments);

            // . create game cache and its view model
            string cacheDir = cacheManager.AddGameCache(cacheId, title, installDir, exePath, xtraArgs, cacheRootDir);

            // . subsume game into the NowPlaying Game Cache library, install directory => game cache directory
            game.InstallDirectory = cacheDir;
            game.IsInstalled = cacheManager.IsGameCacheInstalled(cacheId);
            game.PluginId = plugin.Id;
            game.SourceId = plugin.Id;
            game.GameActions = new ObservableCollection<GameAction>
            {
                // Play from Game Cache (default play action)
                new GameAction()
                {
                    Name = NowPlaying.nowPlayingActionName,
                    Path = Path.Combine(cacheDir, exePath),
                    WorkingDir = cacheDir,
                    AdditionalArguments = xtraArgs?.Replace(installDir, cacheDir),
                    IsPlayAction = true
                },

                // Preview - play game from source install directory
                // -> disabled as nowPlayingGame sourcePlayAction (but playable via right mouse menu)
                //
                new GameAction()
                {
                    Name = NowPlaying.previewPlayActionName,
                    Path = Path.Combine(installDir, exePath),
                    WorkingDir = installDir,
                    AdditionalArguments = xtraArgs,
                    IsPlayAction = false
                }
            };

            // . Re-add the game (rather than just update) in order for library change to register
            PlayniteApi.Database.Games.Remove(game);
            PlayniteApi.Database.Games.Add(game);

            plugin.NotifyInfo($"Enabled '{title}' for game caching.");

            // this causes deadlock
            //plugin.panelViewModel.RefreshGameCaches();

            plugin.DequeueEnablerAndInvokeNext(Id);
        }
    }

}

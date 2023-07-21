using NowPlaying.ViewModels;
using Playnite.SDK.Models;
using Playnite.SDK;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using NowPlaying.Models;
using System;

namespace NowPlaying
{
    public class NowPlayingGameEnabler
    {
        private readonly ILogger logger = NowPlaying.logger;
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
                    Task.Run(() => EnableGameForNowPlayingAsync());
                }
                else
                {
                    var queueStatus = string.Format("{0} of {1}", plugin.gameEnablerQueue.ToList().IndexOf(this), plugin.gameEnablerQueue.Count());
                    logger.Info($"Enabling of '{game.Name}' for NowPlaying game caching queued ({queueStatus}).");
                }
            }
        }

        public async Task EnableGameForNowPlayingAsync()
        {
            logger.Info($"EnableGameForNowPlayingAsync called for {game.Name}");
            
            // . make sure game isn't already enabled
            if (!plugin.IsGameNowPlayingEnabled(game))
            {
                string cacheId = Id;
                string title = game.Name;
                string installDir = game.InstallDirectory;
                string exePath = null;
                string xtraArgs = null;

                var sourcePlayAction = NowPlaying.GetSourcePlayAction(game);
                var platform = NowPlaying.GetGameCachePlatform(game);

                switch (platform)
                {
                    case GameCachePlatform.WinPC:
                        exePath = plugin.GetIncrementalExePath(sourcePlayAction, game);
                        xtraArgs = PlayniteApi.ExpandGameVariables(game, sourcePlayAction.Arguments);
                        break;

                    case GameCachePlatform.PS2:
                    case GameCachePlatform.PS3:
                    case GameCachePlatform.Xbox:
                    case GameCachePlatform.X360:
                    case GameCachePlatform.GameCube:
                    case GameCachePlatform.Wii:
                    case GameCachePlatform.Switch:
                        exePath = plugin.GetIncrementalRomPath(game.Roms?.First().Path, installDir, game);
                        xtraArgs = PlayniteApi.ExpandGameVariables(game, sourcePlayAction.AdditionalArguments);
                        break;

                    default: 
                        break;
                }

                if (exePath != null && await plugin.CheckIfGameInstallDirIsAccessibleAsync(title, installDir))
                {
                    // . create game cache and its view model
                    string cacheDir = cacheManager.AddGameCache(cacheId, title, installDir, exePath, xtraArgs, cacheRootDir, platform: platform);

                    // . subsume game into the NowPlaying Game Cache library, install directory => game cache directory
                    game.InstallDirectory = cacheDir;
                    game.IsInstalled = cacheManager.IsGameCacheInstalled(cacheId);
                    game.PluginId = plugin.Id;

                    // replace source Play action w/ NowPlaying Play and Preview play actions:
                    // -> Play from Game Cache (default play action)
                    // -> Preview - play game from source install directory (playable via right mouse menu)
                    //
                    game.GameActions = new ObservableCollection<GameAction>(game.GameActions.Where(a => !a.IsPlayAction));
                    
                    switch (platform)
                    {
                        case GameCachePlatform.WinPC:
                            game.GameActions.Add
                            (
                                new GameAction()
                                {
                                    Name = NowPlaying.nowPlayingActionName,
                                    Path = Path.Combine(cacheDir, exePath),
                                    WorkingDir = cacheDir,
                                    Arguments = xtraArgs?.Replace(installDir, cacheDir),
                                    IsPlayAction = true
                                }
                            );
                            game.GameActions.Add
                            (
                                new GameAction()
                                {
                                    Name = NowPlaying.previewPlayActionName,
                                    Path = Path.Combine(installDir, exePath),
                                    WorkingDir = installDir,
                                    Arguments = xtraArgs,
                                    IsPlayAction = false
                                }
                            );
                            break;

                        case GameCachePlatform.PS2:
                        case GameCachePlatform.PS3:
                        case GameCachePlatform.Xbox:
                        case GameCachePlatform.X360:
                        case GameCachePlatform.GameCube:
                        case GameCachePlatform.Wii:
                        case GameCachePlatform.Switch:
                            game.GameActions.Add
                            (
                                new GameAction()
                                {
                                    Name = NowPlaying.nowPlayingActionName,
                                    Type = GameActionType.Emulator,
                                    EmulatorId = sourcePlayAction.EmulatorId,
                                    EmulatorProfileId = sourcePlayAction.EmulatorProfileId,
                                    AdditionalArguments = xtraArgs?.Replace(installDir, cacheDir),
                                    IsPlayAction = true
                                }
                            );
                            game.GameActions.Add
                            (
                                new GameAction()
                                {
                                    Name = NowPlaying.previewPlayActionName,
                                    Type = GameActionType.Emulator,
                                    EmulatorId = sourcePlayAction.EmulatorId,
                                    EmulatorProfileId = sourcePlayAction.EmulatorProfileId,
                                    OverrideDefaultArgs = true,
                                    Arguments = "\"" + Path.Combine(installDir, exePath) + "\"" +
                                        (!string.IsNullOrEmpty(xtraArgs) ? " " + xtraArgs : ""),
                                    IsPlayAction = false
                                }
                            );
                            break;

                        default: 
                            break;
                    }

                    PlayniteApi.Database.Games.Update(game);
                    plugin.NotifyInfo($"Enabled '{title}' for game caching.");
                }
            }

            plugin.DequeueEnablerAndInvokeNextAsync(Id);
        }
    }

}

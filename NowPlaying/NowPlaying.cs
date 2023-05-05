using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using Path = System.IO.Path;
using NowPlaying.Views;
using System.Collections.ObjectModel;
using System.Windows;
using NowPlaying.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
using NowPlaying.Core;
using NowPlaying.Models;
using System.IO;
using System.Diagnostics;

namespace NowPlaying
{
    public class NowPlaying : LibraryPlugin
    {
        public override Guid Id { get; } = Guid.Parse("0dbead64-b7ed-47e5-904c-0ccdb5d5ff59");
        public override string Name => "NowPlaying Game Cacher";
        public static readonly ILogger logger = LogManager.GetLogger();

        public const string previewPlayActionName = "Preview (play from install directory)";
        public const string nowPlayingActionName = "Play from game cache";

        public NowPlayingSettings Settings { get; private set; }
        public readonly NowPlayingSettingsViewModel settingsViewModel;
        public readonly NowPlayingSettingsView settingsView;

        public readonly CacheRootsViewModel cacheRootsViewModel;
        public readonly CacheRootsView cacheRootsView;

        public readonly TopPanelViewModel topPanelViewModel;
        public readonly TopPanelView topPanelView;
        public readonly TopPanelItem topPanelItem;

        public readonly SidebarItem sidebarItem;

        public readonly NowPlayingPanelViewModel panelViewModel;
        public readonly NowPlayingPanelView panelView;

        public readonly GameCacheManagerViewModel cacheManager;

        public Queue<NowPlayingGameEnabler> gameEnablerQueue;
        public Queue<NowPlayingInstallController> cacheInstallQueue;
        public Queue<NowPlayingUninstallController> cacheUninstallQueue;

        public NowPlaying(IPlayniteAPI api) : base(api)
        {
            Properties = new LibraryPluginProperties
            {
                HasCustomizedGameImport = true,
                CanShutdownClient = true,
                HasSettings = true
            };

            cacheManager = new GameCacheManagerViewModel(this);

            gameEnablerQueue = new Queue<NowPlayingGameEnabler>();
            cacheInstallQueue = new Queue<NowPlayingInstallController>();
            cacheUninstallQueue = new Queue<NowPlayingUninstallController>();

            Settings = LoadPluginSettings<NowPlayingSettings>() ?? new NowPlayingSettings();
            settingsViewModel = new NowPlayingSettingsViewModel(this);
            settingsView = new NowPlayingSettingsView(settingsViewModel);

            cacheRootsViewModel = new CacheRootsViewModel(this);
            cacheRootsView = new CacheRootsView(cacheRootsViewModel);

            panelViewModel = new NowPlayingPanelViewModel(this);
            panelView = new NowPlayingPanelView(panelViewModel);
            panelViewModel.ResetShowState();

            topPanelViewModel = new TopPanelViewModel(this);
            topPanelView = new TopPanelView(topPanelViewModel);
            topPanelItem = new TopPanelItem()
            {
                Title = "NowPlaying game cache activity",
                Icon = new TopPanelView(topPanelViewModel),
                Visible = false
            };

            this.sidebarItem = new SidebarItem()
            {
                Type = SiderbarItemType.View,
                Title = this.Name,
                Visible = true,
                ProgressValue = 0,
                Icon = new TextBlock
                {
                    TextAlignment = TextAlignment.Center,
                    Text = "NP"
                },
                Opened = () => panelView
            };
        }

        public void UpdateSettings(NowPlayingSettings settings)
        {
            Settings = settings;
            settingsViewModel.Settings = settings;
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when sourceGame is started running.
            if (args.Game.PluginId != Id) return;

            Game nowPlayingGame = args.Game;
            string cacheId = nowPlayingGame.SourceId.ToString();
            if (cacheManager.GameCacheExists(cacheId))
            {
                cacheManager.SetGameCacheAndDirStateAsPlayed(cacheId);
            }
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            cacheManager.LoadCacheRootsFromJson();
            cacheRootsViewModel.RefreshRootsList();
            cacheManager.LoadInstallAverageBpsFromJson();

            try
            {
                cacheManager.LoadGameCacheEntriesFromJson();
            }
            catch
            {
                // . attempt to reconstruct Game Cache entries from the Playnite database
                var nowPlayingGames = PlayniteApi.Database.Games.Where(a => a.PluginId == this.Id);
                foreach (var nowPlayingGame in nowPlayingGames)
                {
                    string cacheId = nowPlayingGame.Id.ToString();
                    RecreateMissingGameCacheIfPossible(cacheId, nowPlayingGame);
                }
                cacheManager.SaveGameCacheEntriesToJson();
            }

            CheckForOrphanedGameCaches();
            CheckForBrokenNowPlayingGames();
            cacheRootsViewModel.RefreshRootsList();
        }

        public void CheckForOrphanedGameCaches()
        {
            foreach (var root in cacheManager.CacheRoots)
            {
                foreach (var subDir in DirectoryUtils.GetSubDirectories(root.Directory))
                {
                    string possibleCacheDir = Path.Combine(root.Directory, subDir);
                    if (!cacheManager.IsGameCacheDirectory(possibleCacheDir))
                    {
                        NotifyWarning($"Unknown/orphaned directory '{subDir}' found under NowPlaying cache cacheRoot '{root}'", () =>
                        {
                            string caption = "NowPlaying: unknown or orphaned cache directory";
                            string message = $"Do you want to delete '{possibleCacheDir}'?";
                            if (PlayniteApi.Dialogs.ShowMessage(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                DirectoryUtils.DeleteDirectory(possibleCacheDir);
                            }
                        });
                    }
                }
            }
        }

        public void CheckForBrokenNowPlayingGames()
        {
            bool foundBroken = false;
            foreach (var game in PlayniteApi.Database.Games.Where(g => g.PluginId == this.Id))
            {
                if (!cacheManager.GameCacheExists(game.Id.ToString())) 
                {
                    // . NowPlaying game is missing the supporting game cache... attempt to recreate it
                    if (!RecreateMissingGameCacheIfPossible(game.Id.ToString(), game))
                    {
                        NotifyWarning($"NowPlaying game cache information missing; disabling game caching for '{game.Name}'.");
                        DisableNowPlayingGameCaching(game);
                        foundBroken = true;
                    }
                }
            }
            if (foundBroken)
            {
                cacheManager.SaveGameCacheEntriesToJson();
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
            cacheManager.Shutdown();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return new NowPlayingSettingsViewModel(this);
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new NowPlayingSettingsView(null);
        }

        private class DummyInstaller : InstallController
        {
            public DummyInstaller(Game game) : base(game) { }
            public override void Install(InstallActionArgs args) { }
        }

        public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
        {
            if (args.Game.PluginId != Id) return null;
            Game nowPlayingGame = args.Game;
            string cacheId = nowPlayingGame.Id.ToString();

            if (!cacheManager.GameCacheExists(cacheId))
            {
                RecreateMissingGameCacheIfPossible(cacheId, nowPlayingGame);
            }

            if (cacheManager.GameCacheExists(cacheId))
            {
                if (!CacheHasInstallerQueued(cacheId))
                {
                    return new List<InstallController> { new NowPlayingInstallController(this, nowPlayingGame, cacheManager.FindGameCache(cacheId)) };
                }
                else
                {
                    return new List<InstallController> { new DummyInstaller(nowPlayingGame) };
                }
            }
            else
            {
                PopupError($"Unable to find (or recreate) NowPlaying Game Cache entry for '{args.Game.Name}'");
                return null;
            }
        }

        private class DummyUninstaller : UninstallController 
        {
            public DummyUninstaller(Game game) : base(game) { }
            public override void Uninstall(UninstallActionArgs args) { } 
        }

        public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id) return null;

            Game nowPlayingGame = args.Game;
            string cacheId = nowPlayingGame.Id.ToString();

            if (!cacheManager.GameCacheExists(cacheId))
            {
                RecreateMissingGameCacheIfPossible(cacheId, nowPlayingGame);
            }

            if (cacheManager.GameCacheExists(cacheId))
            {
                if (!CacheHasUninstallerQueued(cacheId)) 
                { 
                    return new List<UninstallController> { new NowPlayingUninstallController(this, nowPlayingGame, cacheManager.FindGameCache(cacheId)) };
                }
                else
                {
                    return new List<UninstallController> { new DummyUninstaller(nowPlayingGame) };
                }
            }
            else
            {
                PopupError($"Unable to find (or recreate) NowPlaying Game Cache entry for '{args.Game.Name}'");
                return null;
            }
        }

        public bool RecreateMissingGameCacheIfPossible(string cacheId, Game nowPlayingGame)
        {
            string title = nowPlayingGame.Name;
            string installDir = GetPreviewPlayAction(nowPlayingGame)?.WorkingDir;
            string exePath = GetIncrementalExePath(GetNowPlayingAction(nowPlayingGame)) ?? GetIncrementalExePath(GetPreviewPlayAction(nowPlayingGame));
            string xtraArgs = GetNowPlayingAction(nowPlayingGame)?.AdditionalArguments ?? GetPreviewPlayAction(nowPlayingGame)?.AdditionalArguments;

            // . Separate cacheDir into its cacheRootDir and cacheSubDir components, assuming nowPlayingGame matching Cache RootDir exists.
            (string cacheRootDir, string cacheSubDir) = cacheManager.FindCacheRootAndSubDir(nowPlayingGame.InstallDirectory);

            if (title != null && installDir != null && exePath != null && cacheRootDir != null && cacheSubDir != null)
            {
                cacheManager.AddGameCache(cacheId, title, installDir, exePath, xtraArgs, cacheRootDir, cacheSubDir);

                NotifyWarning($"Recreated NowPlaying game cache entry for '{title}'");
                return true;
            }
            return false;
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var gameMenuItems = new List<GameMenuItem>();
            int gameCount = args.Games.Count;
            string plural = gameCount > 1 ? "s" : "";

            bool allNowPlaying = args.Games.Where(a => a.PluginId == this.Id).Count() == gameCount;

            if (allNowPlaying)
            {
                bool allNowPlayingEmpty = args.Games.Where(a => IsEmptyNowPlayingGame(a)).Count() == gameCount;

                //if (allNowPlayingEmpty)
                //{
                //    gameMenuItems.Add(new GameMenuItem
                //    {
                //        Description = $"Remove selected NowPlaying game{plural}", 
                //        Action = (nowPlayingGame) => RemoveEmptyNowPlayingGames(args.Games)
                //    });
                //    if (GameCacheRoots.Count > 1) 
                //    {
                //        foreach (var cacheRoot in GameCacheRoots)
                //        {
                //            gameMenuItems.Add(new GameMenuItem
                //            {
                //                MenuSection = $"Change NowPlaying installDevice for selected game{plural}",
                //                Description = $"To {cacheRoot}",
                //                Action = (nowPlayingGame) => EnableNowPlayingWithRoot(args.Games, cacheRoot)
                //            });
                //        }
                //    }

                //}
                //else
                //{
                //    gameMenuItems.Add(new GameMenuItem
                //    {
                //        Description = $"Uninstall and remove selected NowPlaying game{plural}",
                //        Action = (nowPlayingGame) =>
                //        {
                //            UninstallNowPlayingGames(args.Games);
                //            RemoveEmptyNowPlayingGames(args.Games);
                //        }
                //    });
                //}
            }

            // There are multiple Game Cache RootDir directory choices...
            else if (cacheManager.CacheRoots.Count > 1)
            {
                // . If one or more of the selected games is NowPlaying enabled and Installed/Installing...
                //   -> Don't allow these selected games to be enabled/or have their Game Cache RootDir changed.
                int enabledNonemptyCount = args.Games.Where(a => IsGameEnabledAndNonempty(a)).Count();

                if (enabledNonemptyCount > 0)
                {
                    string descr;
                    if (enabledNonemptyCount == gameCount)
                    {
                        descr = $"(Selected game{plural} are NowPlaying enabled and installed)";
                    }
                    else
                    {
                        descr = $"(*Some* of the selected games are NowPlaying enabled and installed)";
                    }
                    gameMenuItems.Add(new GameMenuItem
                    {
                        Description = descr
                    });
                }

                // . If all of the selected games are eligible to have NowPlaying enabled, which includes
                //   games that are currently NowPlaying enabled but have an empty Game Cache...
                //   -> Allow selected games to be NowPlaying enabled
                //   -> Allow selected NowPlaying enabled games to have their Cache RootDir cacheRoot changed.
                //
                else if (AreAllGamesNowPlayingEnabledOrEligible(args.Games))
                {
                    foreach (var cacheRoot in cacheManager.CacheRoots)
                    {
                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = $"Activate NowPlaying caching for selected game{plural}",
                            Description = $"To {cacheRoot.Directory}",
                            Action = (a) => { foreach (var game in args.Games) { EnableNowPlayingWithRoot(game, cacheRoot); } }
                        });
                    }
                }
            }

            // Single Game Cache RootDir directory...
            else if (cacheManager.CacheRoots.Count == 1)
            {
                if (AreAllGamesNowPlayingEnabled(args.Games))
                {
                    gameMenuItems.Add(new GameMenuItem
                    {
                        Description = string.Format("(Selected game{0} NowPlaying enabled)", gameCount > 1 ? "s are" : " is")
                    });
                }
                else if (AreAllGamesNowPlayingEnabledOrEligible(args.Games))
                {
                    var cacheRoot = cacheManager.CacheRoots.First();
                    gameMenuItems.Add(new GameMenuItem
                    {
                        Description = $"Activate NowPlaying caching for selected game{plural}",
                        Action = (a) => { foreach (var game in args.Games) { EnableNowPlayingWithRoot(game, cacheRoot); } }
                    });
                }
            }

            return gameMenuItems;
        }

        private void UninstallNowPlayingGames(List<Game> games)
        {
            foreach (var game in games)
            {
                string cacheId = game.Id.ToString();
                if (cacheManager.GameCacheExists(cacheId))
                {
                    var controller = new NowPlayingUninstallController(this, game, cacheManager.FindGameCache(cacheId));
                    controller.Uninstall(new UninstallActionArgs());
                }
            }
        }

        private bool IsEmptyNowPlayingGame(Game nowPlayingGame)
        {
            string cacheId = nowPlayingGame.SourceId.ToString();
            return nowPlayingGame.PluginId == this.Id && cacheManager.GameCacheIsUninstalled(cacheId);
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            return new List<SidebarItem> { sidebarItem };
        }

        /// <summary>
        /// Gets top panel items provided by this plugin.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            return new List<TopPanelItem> { topPanelItem };
        }

        public bool IsGameNowPlayingEligible(Game game)
        {
            bool eligible = (game != null
                && game.PluginId == Guid.Empty
                && game.SourceId == Guid.Empty
                && game.IsInstalled
                && game.IsCustomGame
                && game.Platforms?.Count == 1 && game.Platforms?.First().SpecificationId == "pc_windows"
                && game.Roms == null || game.Roms?.Count == 0
                && game.GameActions?.Where(a => a.IsPlayAction).Count() == 1
                && GetIncrementalExePath(game.GameActions?[0], game) != null
            );
            return eligible;
        }

        private bool IsGameNowPlayingEnabled(Game game)
        {
            return game.PluginId == this.Id && cacheManager.GameCacheExists(game.Id.ToString());
        }

        private bool AreAllGamesNowPlayingEnabled(List<Game> games)
        {
            return games.Where(a => IsGameNowPlayingEnabled(a)).Count() == games.Count();
        }

        private bool IsGameEnabledAndNonempty(Game game)
        {
            return IsGameNowPlayingEnabled(game) && cacheManager.FindGameCache(game.Id.ToString()).CacheSize > 0;
        }

        private bool AreAllGamesNowPlayingEnabledOrEligible(List<Game> games)
        {
            return games.Where(a => IsGameNowPlayingEnabled(a) || IsGameNowPlayingEligible(a)).Count() == games.Count();
        }

        public void EnableNowPlayingWithRoot(Game game, CacheRootViewModel cacheRoot)
        {
            string cacheId = game.Id.ToString();

            // . Already a NowPlaying enabled game
            //   -> change cache cacheRoot, if applicable
            //
            if (cacheManager.GameCacheExists(cacheId))
            {
                var gameCache = cacheManager.FindGameCache(cacheId);
                if (gameCache.cacheRoot != cacheRoot)
                {
                    if (gameCache.IsUninstalled())
                    {
                        // . change cache cacheRoot, get updated cache directory (located under new cacheRoot)
                        string cacheDir = cacheManager.ChangeGameCacheRoot(gameCache, cacheRoot);

                        // . game install directory is now the NowPlaying cache directory
                        game.InstallDirectory = cacheDir;

                        GameAction playAction = GetNowPlayingAction(game);
                        if (playAction != null)
                        {
                            // . Update play action Path/Work to point to new cache cacheRoot
                            playAction.Path = Path.Combine(cacheDir, gameCache.ExePath);
                            playAction.WorkingDir = cacheDir;

                            // . Update whether game cache is currently installed or not
                            game.IsInstalled = cacheManager.IsGameCacheInstalled(cacheId);
                            PlayniteApi.Database.Games.Update(game);
                        }
                        else
                        {
                            PopupError($"Couldn't find NowPlaying play action for game: {game.Name}");
                        }
                    }
                    else
                    {
                        PopupError($"Attempted to change cache cacheRoot of non-empty game cache: {game.Name}");
                    }
                }
            }

            // . Source game (NowPlaying eligible)
            //   -> Activate game for NowPlaying game caching
            //
            else
            {
                (new NowPlayingGameEnabler(this, game, cacheRoot.Directory)).Activate();
            }
        }

        public bool EnqueueGameEnablerIfUnique(NowPlayingGameEnabler enabler)
        {
            // . enqueue our NowPlaying enabler (but don't add more than once)
            if (!GameHasEnablerQueued(enabler.Id))
            {
                gameEnablerQueue.Enqueue(enabler);
                topPanelViewModel.QueuedEnabler();
                return true;
            }
            return false;
        }

        public bool GameHasEnablerQueued(string id)
        {
            return gameEnablerQueue.Where(e => e.Id == id).Count() > 0;
        }

        public void DequeueEnablerAndInvokeNext(string id)
        {
            // Dequeue the enabler (and sanity check it was ours)
            var activeId = gameEnablerQueue.Dequeue().Id;
            Debug.Assert(activeId == id, $"Unexpected game enabler Id at head of the Queue ({activeId})");

            // . update status of queued enablers
            topPanelViewModel.EnablerDoneOrCancelled();

            // Invoke next in queue's enabler, if applicable.
            if (gameEnablerQueue.Count > 0)
            {
                gameEnablerQueue.First().EnableGameForNowPlaying();
            }

            panelViewModel.RefreshGameCaches();
        }

        public bool DisableNowPlayingGameCaching(Game game, string installDir = null, string exePath = null, string xtraArgs = null)
        {
            // . attempt to extract original install and play action parameters, if not provided by caller
            if (installDir == null || exePath == null)
            {
                var previewPlayAction = GetPreviewPlayAction(game);
                if (previewPlayAction != null)
                {
                    exePath = GetIncrementalExePath(previewPlayAction);
                    installDir = previewPlayAction.WorkingDir;
                    if (xtraArgs == null)
                    {
                        xtraArgs = previewPlayAction.AdditionalArguments;
                    }
                }
            }

            // . restore game with original (or functionally equivalent) play action
            if (installDir != null && exePath != null)
            {
                game.InstallDirectory = installDir;
                game.IsInstalled = true;
                game.PluginId = Guid.Empty;
                game.SourceId = Guid.Empty;
                game.GameActions = new ObservableCollection<GameAction>
                {
                    new GameAction()
                    {
                        Name = game.Name,
                        Path = Path.Combine("{InstallDir}", exePath),
                        WorkingDir = "{InstallDir}",
                        AdditionalArguments = xtraArgs?.Replace(installDir, "{InstallDir}"),
                        IsPlayAction = true
                    }
                };

                // . Re-add the game (rather than just update) in order for library change to register
                PlayniteApi.Database.Games.Remove(game);
                PlayniteApi.Database.Games.Add(game);
                return true;
            }
            else
            {
                return false;
            }
        }

        public GameAction GetSourcePlayAction(Game game)
        {
            var actions = game.GameActions.Where(a => a.IsPlayAction);
            return actions.Count() == 1 ? actions.First() : null;
        }

        private GameAction GetNowPlayingAction(Game game)
        {
            var actions = game.GameActions.Where(a => a.IsPlayAction && a.Name == nowPlayingActionName);
            return actions.Count() == 1 ? actions.First() : null;
        }

        private GameAction GetPreviewPlayAction(Game game)
        {
            var actions = game.GameActions.Where(a => a.Name == previewPlayActionName);
            return actions.Count() == 1 ? actions.First() : null;
        }

        public Game FindNowPlayingGame(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var games = PlayniteApi.Database.Games.Where(a => a.PluginId == this.Id && a.Id.ToString() == id);

                if (games.Count() > 0)
                {
                    return games.First();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public string GetIncrementalExePath(GameAction action, Game variableReferenceGame = null)
        {
            if (action != null)
            {
                string work, path;
                if (variableReferenceGame != null)
                {
                    work = PlayniteApi.ExpandGameVariables(variableReferenceGame, action.WorkingDir);
                    path = PlayniteApi.ExpandGameVariables(variableReferenceGame, action.Path);
                }
                else
                {
                    work = action.WorkingDir;
                    path = action.Path;
                }
                if (work != null && path != null && work == path.Substring(0, work.Length))
                {
                    return path.Substring(work.Length + 1);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }


        public string GetInstallQueueStatus(NowPlayingInstallController controller)
        {
            // . Return queue index, excluding slot 0, which is the active installation controller
            int index = cacheInstallQueue.ToList().IndexOf(controller);
            int size = cacheInstallQueue.Count - 1;
            if (index > 0)
            {
                return $"{index} of {size}";
            }
            else
            {
                return null;
            }
        }

        public string GetUninstallQueueStatus(NowPlayingUninstallController controller)
        {
            // . Return queue index, excluding slot 0, which is the active installation controller
            int index = cacheUninstallQueue.ToList().IndexOf(controller);
            int size = cacheUninstallQueue.Count - 1;
            if (index > 0)
            {
                return $"{index} of {size}";
            }
            else
            {
                return null;
            }
        }

        public void UpdateInstallQueueStatuses()
        {
            foreach (var controller in cacheInstallQueue.ToList())
            {
                controller.gameCache.UpdateInstallQueueStatus(GetInstallQueueStatus(controller));
            }
        }

        public void UpdateUninstallQueueStatuses()
        {
            foreach (var controller in cacheUninstallQueue.ToList())
            {
                controller.gameCache.UpdateUninstallQueueStatus(GetUninstallQueueStatus(controller));
            }
        }

        public bool CacheHasInstallerQueued(string cacheId)
        {
            return cacheInstallQueue.Where(c => c.gameCache.Id == cacheId).Count() > 0;
        }

        public bool CacheHasUninstallerQueued(string cacheId)
        {
            return cacheUninstallQueue.Where(c => c.gameCache.Id == cacheId).Count() > 0;
        }

        public bool EnqueueCacheInstallerIfUnique(NowPlayingInstallController controller)
        {
            // . enqueue our controller (but don't add more than once)
            if (!CacheHasInstallerQueued(controller.gameCache.Id))
            {
                cacheInstallQueue.Enqueue(controller);
                topPanelViewModel.QueuedInstall(controller.gameCache.InstallSize, controller.gameCache.InstallEtaTimeSpan);
                return true;
            }
            return false;
        }

        public bool EnqueueCacheUninstallerIfUnique(NowPlayingUninstallController controller)
        {
            // . enqueue our controller (but don't add more than once)
            if (!CacheHasUninstallerQueued(controller.gameCache.Id))
            {
                cacheUninstallQueue.Enqueue(controller);
                topPanelViewModel.QueuedUninstall();
                return true;
            }
            return false;
        }

        public void DequeueInstallerAndInvokeNext(string cacheId)
        {
            // Dequeue the controller (and sanity check it was ours)
            var activeId = cacheInstallQueue.Dequeue().gameCache.Id;
            Debug.Assert(activeId == cacheId, $"Unexpected install controller cacheId at head of the Queue ({activeId})");

            // . update install queue status of queued installers & top panel status
            UpdateInstallQueueStatuses();
            topPanelViewModel.InstallDoneOrCancelled();

            // Invoke next in queue's controller, if applicable.
            if (cacheInstallQueue.Count > 0)
            {
                cacheInstallQueue.First().NowPlayingInstall();
            }
            else
            {
                topPanelViewModel.TopPanelVisible = false;
            }
        }

        public void DequeueUninstallerAndInvokeNext(string cacheId)
        {
            // Dequeue the installer (and sanity check it was ours)
            var activeId = cacheUninstallQueue.Dequeue().gameCache.Id;
            Debug.Assert(activeId == cacheId, $"Unexpected uninstall controller cacheId at head of the Queue ({activeId})");

            // . update status of queued uninstallers
            UpdateUninstallQueueStatuses();
            topPanelViewModel.UninstallDoneOrCancelled();

            // Invoke next in queue's installer, if applicable.
            if (cacheUninstallQueue.Count > 0)
            {
                cacheUninstallQueue.First().NowPlayingUninstall();
            }
        }


        public void NotifyInfo(string message)
        {
            NowPlaying.logger.Info(message);
            PlayniteApi.Notifications.Add(Guid.NewGuid().ToString(), message, NotificationType.Info);
        }

        public void NotifyWarning(string message, Action action = null)
        {
            NowPlaying.logger.Warn(message);
            var notification = new NotificationMessage(Guid.NewGuid().ToString(), message, NotificationType.Info, action);
            PlayniteApi.Notifications.Add(notification);
        }

        public void NotifyError(string message, Action action = null)
        {
            NowPlaying.logger.Error(message);
            var notification = new NotificationMessage(Guid.NewGuid().ToString(), message, NotificationType.Error, action);
            PlayniteApi.Notifications.Add(notification);
        }

        public void PopupError(string message, string selectableMessage = null)
        {
            if (!string.IsNullOrEmpty(selectableMessage))
            {
                NowPlaying.logger.Error(message + selectableMessage);
                PlayniteApi.Dialogs.ShowSelectableString(message, "NowPlaying Error:", selectableMessage);
            }
            else
            {
                NowPlaying.logger.Error(message);
                PlayniteApi.Dialogs.ShowMessage(message, "NowPlaying Error:");
            }
        }

        public string SaveJobErrorLogAndGetMessage(GameCacheJob job, string logFileSuffix)
        {
            string seeLogFile = "";
            if (job.errorLog != null)
            {
                // save error log
                string errorLogs = Path.Combine(GetPluginUserDataPath(), "errorLogs");
                string errorLog = Path.Combine(errorLogs, job.entry.Id + " " + DirectoryUtils.ToSafeFileName(job.entry.Title) + logFileSuffix);
                if (DirectoryUtils.MakeDir(errorLogs))
                {
                    try
                    {
                        const int showMaxErrorLineCount = 25;
                        File.Create(errorLog)?.Dispose();
                        File.WriteAllLines(errorLog, job.errorLog);
                        seeLogFile = $"(See {errorLog}):";

                        // . show at most the last 'showMaxErrorLineCount' lines of the error log in the popup
                        seeLogFile += errorLogs.Skip(Math.Max(0, errorLog.Count() - showMaxErrorLineCount));
                    }
                    catch { }
                }
            }
            return seeLogFile;
        }

    }
}
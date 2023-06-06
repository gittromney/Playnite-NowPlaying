using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using Path = System.IO.Path;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using NowPlaying.Utils;
using NowPlaying.Models;
using NowPlaying.Properties;
using NowPlaying.Views;
using NowPlaying.ViewModels;
using System.Reflection;

namespace NowPlaying
{
    public class NowPlaying : LibraryPlugin
    {
        public override Guid Id { get; } = Guid.Parse("0dbead64-b7ed-47e5-904c-0ccdb5d5ff59");
        public override string LibraryIcon { get; }

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

        public readonly Rectangle sidebarIcon;
        public readonly SidebarItem sidebarItem;

        public readonly NowPlayingPanelViewModel panelViewModel;
        public readonly NowPlayingPanelView panelView;

        public readonly GameCacheManagerViewModel cacheManager;

        public bool IsGamePlaying = false;
        public WhilePlaying WhilePlayingMode { get; private set; }
        public int SpeedLimitIPG { get; private set; } = 0;

        private string formatStringXofY;
        public Queue<NowPlayingGameEnabler> gameEnablerQueue;
        public Queue<NowPlayingInstallController> cacheInstallQueue;
        public Queue<NowPlayingUninstallController> cacheUninstallQueue;
        public bool cacheInstallQueuePaused;


        public NowPlaying(IPlayniteAPI api) : base(api)
        {
            Properties = new LibraryPluginProperties
            {
                HasCustomizedGameImport = true,
                CanShutdownClient = true,
                HasSettings = true
            };

            LibraryIcon = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"icon.png");

            cacheManager = new GameCacheManagerViewModel(this, logger);

            formatStringXofY = GetResourceFormatString("LOCNowPlayingProgressXofYFmt2", 2) ?? "{0} of {1}";
            gameEnablerQueue = new Queue<NowPlayingGameEnabler>();
            cacheInstallQueue = new Queue<NowPlayingInstallController>();
            cacheUninstallQueue = new Queue<NowPlayingUninstallController>();
            cacheInstallQueuePaused = false;

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
                Title = GetResourceString("LOCNowPlayingTopPanelToolTip"),
                Icon = new TopPanelView(topPanelViewModel),
                Visible = false
            };

            this.sidebarIcon = new System.Windows.Shapes.Rectangle()
            {
                Fill = (Brush)PlayniteApi.Resources.GetResource("TextBrush"),
                Width = 256,
                Height = 256,
                OpacityMask = new ImageBrush()
                {
                    ImageSource = ImageUtils.BitmapToBitmapImage(Resources.now_playing_icon)
                }
            };

            this.sidebarItem = new SidebarItem()
            {
                Type = SiderbarItemType.View,
                Title = GetResourceString("LOCNowPlayingSideBarToolTip"),
                Visible = true,
                ProgressValue = 0,
                Icon = sidebarIcon,
                Opened = () =>
                {
                    sidebarIcon.Fill = (Brush)PlayniteApi.Resources.GetResource("GlyphBrush");
                    return panelView;
                },
                Closed = () => sidebarIcon.Fill = (Brush)PlayniteApi.Resources.GetResource("TextBrush")
            };
        }

        public string GetResourceString(string key)
        {
            return key != null ? PlayniteApi.Resources.GetString(key) : null;
        }

        public string GetResourceFormatString(string key, int formatItemCount)
        {
            if (key != null)
            {
                string formatString = PlayniteApi.Resources.GetString(key);
                bool validFormat = !string.IsNullOrEmpty(formatString);
                for (int fi = 0; validFormat && fi < formatItemCount; fi++) {
                    validFormat &= formatString.Contains("{" + fi + "}");
                }    
                if (validFormat) {
                    return formatString;
                }
                else if (formatItemCount > 1)
                {
                    PopupError($"Bad resource: key='{key}', value='{formatString}'; must contain '{{0}} - '{{{formatItemCount-1}}}' patterns");
                    return null;
                }
                else
                {
                    PopupError($"Bad resource: key='{key}', value='{formatString}'; must contain '{{0}}' pattern");
                    return null;
                }
            }
            return null;
        }

        public string FormatResourceString(string key, params object[] formatItems)
        {
            string formatString = GetResourceFormatString(key, formatItems.Count());
            return formatString != null ? string.Format(formatString, formatItems) : null;
        }


        public void UpdateSettings(NowPlayingSettings settings)
        {
            Settings = settings;
            settingsViewModel.Settings = settings;
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when sourceGame is started running.
            if (args.Game.PluginId == Id)
            {
                Game nowPlayingGame = args.Game;
                string cacheId = nowPlayingGame.SourceId.ToString();
                if (cacheManager.GameCacheExists(cacheId))
                {
                    cacheManager.SetGameCacheAndDirStateAsPlayed(cacheId);
                }
            }

            IsGamePlaying = true;

            // Save relevant settings just in case the are changed while a game is playing
            WhilePlayingMode = Settings.WhilePlayingMode;
            SpeedLimitIPG = WhilePlayingMode == WhilePlaying.SpeedLimit ? Settings.SpeedLimitIPG : 0;

            if (WhilePlayingMode == WhilePlaying.Pause)
            {
                PauseCacheInstallQueue();
            }
            else if (SpeedLimitIPG > 0)
            {
                SpeedLimitCacheInstalls(SpeedLimitIPG);
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            base.OnGameStopped(args);

            IsGamePlaying = false;

            if (WhilePlayingMode == WhilePlaying.Pause)
            {
                ResumeCacheInstallQueue();
            }
            else if (SpeedLimitIPG > 0)
            {
                SpeedLimitIPG = 0;
                ResumeFullSpeedCacheInstalls();
            }
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            cacheManager.LoadCacheRootsFromJson();
            cacheRootsViewModel.RefreshCacheRoots();
            cacheManager.LoadInstallAverageBpsFromJson();

            try
            {
                cacheManager.LoadGameCacheEntriesFromJson();
            }
            catch (Exception ex)
            {
                logger.Error($"Exception thrown in LoadGameCacheEntriesFromJson: '{ex.Message}'");

                // . attempt to reconstruct Game Cache entries from the Playnite database
                var nowPlayingGames = PlayniteApi.Database.Games.Where(a => a.PluginId == this.Id);
                foreach (var nowPlayingGame in nowPlayingGames)
                {
                    string cacheId = nowPlayingGame.Id.ToString();
                    if (!cacheManager.GameCacheExists(cacheId))
                    {
                        TryRestoreMissingGameCache(cacheId, nowPlayingGame);
                    }
                }
                cacheManager.SaveGameCacheEntriesToJson();
            }

            CheckForOrphanedCacheDirectories();
            CheckForBrokenNowPlayingGames();
            cacheRootsViewModel.RefreshCacheRoots();

            PlayniteApi.Database.Games.ItemCollectionChanged += CheckForRemovedNowPlayingGames;
        }

        public void CheckForRemovedNowPlayingGames(object o, ItemCollectionChangedEventArgs<Game> args)
        {
            if (args.RemovedItems.Count > 0)
            {
                foreach (var game in args.RemovedItems)
                {
                    if (game != null && cacheManager.GameCacheExists(game.Id.ToString()))
                    {
                        var gameCache = cacheManager.FindGameCache(game.Id.ToString());
                        if (gameCache.CacheSize > 0)
                        {
                            NotifyWarning(FormatResourceString("LOCNowPlayingRemovedEnabledGameNonEmptyCacheFmt", game.Name));
                        }
                        else
                        {
                            NotifyWarning(FormatResourceString("LOCNowPlayingRemovedEnabledGameFmt", game.Name));
                        }
                        DirectoryUtils.DeleteDirectory(gameCache.CacheDir);
                        cacheManager.RemoveGameCache(gameCache.Id);
                    }
                }
            }
        }

        public void CheckForOrphanedCacheDirectories()
        {
            foreach (var root in cacheManager.CacheRoots)
            {
                foreach (var subDir in DirectoryUtils.GetSubDirectories(root.Directory))
                {
                    string possibleCacheDir = Path.Combine(root.Directory, subDir);
                    if (!cacheManager.IsGameCacheDirectory(possibleCacheDir))
                    {
                        NotifyWarning(FormatResourceString("LOCNowPlayingUnknownOrphanedDirectoryFoundFmt2", subDir, root.Directory), () =>
                        {
                            string caption = GetResourceString("LOCNowPlayingUnknownOrphanedDirectoryCaption");
                            string message = FormatResourceString("LOCNowPlayingUnknownOrphanedDirectoryMsgFmt", possibleCacheDir);
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
                    if (!TryRestoreMissingGameCache(game.Id.ToString(), game))
                    {
                        NotifyWarning(FormatResourceString("LOCNowPlayingDisableGameCacheMissingInfoFmt", game.Name));
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
                TryRestoreMissingGameCache(cacheId, nowPlayingGame);
            }

            if (cacheManager.GameCacheExists(cacheId))
            {
                var gameCache = cacheManager.FindGameCache(cacheId);
                if (gameCache.CacheWillFit && !CacheHasInstallerQueued(cacheId))
                {
                    var controller = new NowPlayingInstallController(this, nowPlayingGame, cacheManager.FindGameCache(cacheId), SpeedLimitIPG);
                    return new List<InstallController> { controller };
                }
                else
                {
                    if (!gameCache.CacheWillFit)
                    {
                        string message = FormatResourceString("LOCNowPlayingCacheWillNotFitFmt2", gameCache.Title, gameCache.CacheDir);
                        message += Environment.NewLine + Environment.NewLine + GetResourceString("LOCNowPlayingCacheWillNotFitMsg");
                        PopupError(message);
                    }
                    return new List<InstallController> { new DummyInstaller(nowPlayingGame) };
                }
            }
            else
            {
                PopupError(FormatResourceString("LOCNowPlayingUnabletoRestoreCacheInfoFmt", args.Game.Name));
                return new List<InstallController> { new DummyInstaller(nowPlayingGame) };
            }
        }

        private class DummyUninstaller : UninstallController 
        {
            public DummyUninstaller(Game game) : base(game) { }
            public override void Uninstall(UninstallActionArgs args) { } 
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            base.OnLibraryUpdated(args);
        }

        public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id) return null;

            Game nowPlayingGame = args.Game;
            string cacheId = nowPlayingGame.Id.ToString();

            if (!cacheManager.GameCacheExists(cacheId))
            {
                TryRestoreMissingGameCache(cacheId, nowPlayingGame);
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
                PopupError(FormatResourceString("LOCNowPlayingUnabletoRestoreCacheInfoFmt", args.Game.Name));
                return null;
            }
        }

        public bool TryRestoreMissingGameCache(string cacheId, Game nowPlayingGame)
        {
            string title = nowPlayingGame.Name;
            string installDir = GetPreviewPlayAction(nowPlayingGame)?.WorkingDir;
            string exePath = GetIncrementalExePath(GetNowPlayingAction(nowPlayingGame)) ?? GetIncrementalExePath(GetPreviewPlayAction(nowPlayingGame));
            string xtraArgs = GetNowPlayingAction(nowPlayingGame)?.AdditionalArguments ?? GetPreviewPlayAction(nowPlayingGame)?.AdditionalArguments;

            if (!CheckIfGameInstallDirIsAccessible(title, installDir)) return false;

            // . Separate cacheDir into its cacheRootDir and cacheSubDir components, assuming nowPlayingGame matching Cache RootDir exists.
            (string cacheRootDir, string cacheSubDir) = cacheManager.FindCacheRootAndSubDir(nowPlayingGame.InstallDirectory);

            if (title != null && installDir != null && exePath != null && cacheRootDir != null && cacheSubDir != null)
            {
                cacheManager.AddGameCache(cacheId, title, installDir, exePath, xtraArgs, cacheRootDir, cacheSubDir);
                NotifyWarning(FormatResourceString("LOCNowPlayingRestoredCacheInfoFmt", title));
                return true;
            }
            return false;
        }

        public class SelectedGamesContext
        {
            private readonly NowPlaying plugin;
            public bool allEligible;
            public bool allEnabled;
            public bool allEnabledAndEmpty;
            public int count;

            public SelectedGamesContext(NowPlaying plugin, List<Game> games)
            {
                this.plugin = plugin;
                UpdateContext(games);
            }

            private void ResetContext()
            {
                allEligible = true;
                allEnabled = true;
                allEnabledAndEmpty = true;
                count = 0;
            }

            public void UpdateContext(List<Game> games)
            {
                ResetContext();
                foreach (var game in games)
                {
                    bool isEnabled = plugin.IsGameNowPlayingEnabled(game);
                    bool isEligible = !isEnabled && plugin.IsGameNowPlayingEligible(game);
                    bool isEnabledAndEmpty = isEnabled && plugin.cacheManager.FindGameCache(game.Id.ToString())?.CacheSize == 0;
                    allEligible &= isEligible;
                    allEnabled &= isEnabled;
                    allEnabledAndEmpty &= isEnabledAndEmpty;
                    count++;
                }
            }
        }
    
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var gameMenuItems = new List<GameMenuItem>();

            // . get selected games context
            var context = new SelectedGamesContext(this, args.Games);

            // . Enable game caching menu
            if (context.allEligible)
            {
                string description = "NowPlaying: ";
                if (context.count > 1)
                {
                    description += FormatResourceString("LOCNowPlayingEnableGameCachesFmt", context.count);
                }
                else
                {
                    description += GetResourceString("LOCNowPlayingEnableGameCache");
                }
                if (cacheManager.CacheRoots.Count > 1)
                {
                    foreach (var cacheRoot in cacheManager.CacheRoots)
                    {
                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = description,
                            Description = GetResourceString("LOCNowPlayingTermsTo") + " " + cacheRoot.Directory,
                            Action = (a) => { foreach (var game in args.Games) { EnableNowPlayingWithRoot(game, cacheRoot); } }
                        });
                    }
                }
                else
                {
                    var cacheRoot = cacheManager.CacheRoots.First();
                    gameMenuItems.Add(new GameMenuItem
                    {
                        Description = description,
                        Action = (a) => { foreach (var game in args.Games) { EnableNowPlayingWithRoot(game, cacheRoot); } }
                    });
                }
            }

            // . Disable game caching menu
            else if (context.allEnabledAndEmpty)
            {
                string description = "NowPlaying: ";
                if (context.count > 1)
                {
                    description += FormatResourceString("LOCNowPlayingDisableGameCachesFmt", context.count);
                }
                else
                {
                    description += GetResourceString("LOCNowPlayingDisableGameCache");
                }
                gameMenuItems.Add(new GameMenuItem
                {
                    Description = description,
                    Action = (a) => 
                    { 
                        foreach (var game in args.Games) 
                        { 
                            DisableNowPlayingGameCaching(game);
                            cacheManager.RemoveGameCache(game.Id.ToString());
                            NotifyInfo(FormatResourceString("LOCNowPlayingMsgGameCachingDisabledFmt", game.Name));
                        } 
                    }
                });
            }

            return gameMenuItems;
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
                && game.IsInstalled
                && game.IsCustomGame
                && game.Platforms?.Count == 1 && game.Platforms?.First().SpecificationId == "pc_windows"
                && game.Roms == null || game.Roms?.Count == 0
                && game.GameActions?.Where(a => a.IsPlayAction).Count() == 1
                && GetIncrementalExePath(game.GameActions?[0], game) != null
            );
            return eligible;
        }

        public bool IsGameNowPlayingEnabled(Game game)
        {
            return game.PluginId == this.Id && cacheManager.GameCacheExists(game.Id.ToString());
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
                    bool noCacheDirOrEmpty = !Directory.Exists(gameCache.CacheDir) || DirectoryUtils.IsEmptyDirectory(gameCache.CacheDir);

                    if (gameCache.IsUninstalled() && noCacheDirOrEmpty)
                    {
                        // . delete old, empty cache dir, if necessary 
                        if (Directory.Exists(gameCache.CacheDir))
                        {
                            Directory.Delete(gameCache.CacheDir);
                        }

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
                            PopupError(FormatResourceString("LOCNowPlayingPlayActionNotFoundFmt", game.Name));
                        }
                    }
                    else
                    {
                        PopupError(FormatResourceString("LOCNowPlayingNonEmptyRerootAttemptedFmt", game.Name));
                    }
                }
            }

            else if (CheckIfGameInstallDirIsAccessible(game.Name, game.InstallDirectory))
            {
                if (CheckAndConfirmEnableIfInstallDirIsProblematic(game.Name, game.InstallDirectory)) 
                {
                    // . Enable source game for NowPlaying game caching
                    (new NowPlayingGameEnabler(this, game, cacheRoot.Directory)).Activate();
                }
            }
        }

        // . Applies a heuristic test to screen for whether a game's installation directory might be set too deeply.
        // . Screening is done by checking for 'problematic' subdirectories (e.g. "bin", "x64", etc.) in the
        //   install dir path.
        //   
        public bool CheckAndConfirmEnableIfInstallDirIsProblematic(string title, string installDirectory)
        {
            bool continueWithEnable = true;
            string[] problematicKeywords = { "bin", "binaries", "x64", "x86", "win64", "win32", "sources", "nodvd", "retail" };
            List<string> matchedSubdirs = new List<string>();
            
            foreach (var subDir in installDirectory.Split(Path.DirectorySeparatorChar))
            {
                foreach (var keyword in problematicKeywords)
                {
                    if (String.Equals(subDir, keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedSubdirs.Add(subDir);
                    }
                }
            }
            if (matchedSubdirs.Count > 0)
            {
                // . See if user wants to continue enabling game, anyway
                string nl = System.Environment.NewLine;
                string problematicSubdirs = string.Join("', '", matchedSubdirs);
                string message = FormatResourceString("LOCNowPlayingProblematicInstallDirFmt3", title, installDirectory, problematicSubdirs);
                message += nl + nl + GetResourceString("LOCNowPlayingProblematicInstallDirConfirm");
                string caption = GetResourceString("LOCNowPlayingConfirmationCaption");
                continueWithEnable = PlayniteApi.Dialogs.ShowMessage(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            }
            return continueWithEnable;
        }

        // . Sanity check: make sure game's InstallDir is accessable (e.g. disk mounted, decrypted, etc?)
        public bool CheckIfGameInstallDirIsAccessible(string title, string installDir, bool silentMode = false)
        {
            string installDevice = DirectoryUtils.TryGetRootDevice(installDir);
            if (DriveInfo.GetDrives().Any(d => d.Name == installDevice) && Directory.Exists(installDir))
            {
                return true;
            }
            else
            {
                if (!silentMode)
                {
                    PopupError(FormatResourceString("LOCNowPlayingGameInstallDirNotFoundFmt2", title, installDir));
                }
                return false;
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
            var previewPlayAction = GetPreviewPlayAction(game);

            // . attempt to extract original install and play action parameters, if not provided by caller
            if (installDir == null || exePath == null)
            {
                if (previewPlayAction != null)
                {
                    exePath = GetIncrementalExePath(previewPlayAction);
                    installDir = previewPlayAction.WorkingDir;
                    if (xtraArgs == null)
                    {
                        xtraArgs = previewPlayAction.Arguments;
                    }
                }
            }

            // . restore the game to Playnite library
            if (installDir != null && exePath != null)
            {
                game.InstallDirectory = installDir;
                game.IsInstalled = true;
                game.PluginId = Guid.Empty;

                // restore original Play action (or functionally equivalent):
                game.GameActions = new ObservableCollection<GameAction>(game.GameActions.Where(a => !a.IsPlayAction && a != previewPlayAction));
                game.GameActions.Add
                (
                     new GameAction()
                     {
                         Name = game.Name,
                         Path = Path.Combine("{InstallDir}", exePath),
                         WorkingDir = "{InstallDir}",
                         Arguments = xtraArgs?.Replace(installDir, "{InstallDir}"),
                         IsPlayAction = true
                     }
                );

                PlayniteApi.Database.Games.Update(game);
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
                return string.Format(formatStringXofY, index, size);
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
                return string.Format(formatStringXofY, index, size);
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

        public void CancelQueuedInstaller(string cacheId)
        {
            if (CacheHasInstallerQueued(cacheId))
            {
                // . remove entry from installer queue
                var controller = cacheInstallQueue.Where(c => c.gameCache.Id == cacheId).First();
                cacheInstallQueue = new Queue<NowPlayingInstallController>(cacheInstallQueue.Where(c => c != controller));

                // . update game cache state
                var gameCache = cacheManager.FindGameCache(cacheId);
                gameCache.UpdateInstallQueueStatus(null);
                UpdateInstallQueueStatuses();
                topPanelViewModel.CancelledFromInstallQueue(gameCache.InstallSize, gameCache.InstallEtaTimeSpan);
                panelViewModel.RefreshGameCaches();
            }
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
            if (cacheInstallQueue.Count > 0 && !cacheInstallQueuePaused)
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
            // Dequeue the controller (and sanity check it was ours)
            var activeId = cacheUninstallQueue.Dequeue().gameCache.Id;
            Debug.Assert(activeId == cacheId, $"Unexpected uninstall controller cacheId at head of the Queue ({activeId})");

            // . update status of queued uninstallers
            UpdateUninstallQueueStatuses();
            topPanelViewModel.UninstallDoneOrCancelled();

            // Invoke next in queue's controller, if applicable.
            if (cacheUninstallQueue.Count > 0)
            {
                cacheUninstallQueue.First().NowPlayingUninstall();
            }
        }

        private void PauseCacheInstallQueue(Action speedLimitChangeOnPaused = null)
        {
            if (cacheInstallQueue.Count > 0)
            {
                cacheInstallQueuePaused = true;
                cacheInstallQueue.First().RequestPauseInstall(speedLimitChangeOnPaused);
            }
        }

        private void ResumeCacheInstallQueue(int speedLimitIPG = 0, bool resumeFromSpeedLimitedMode = false)
        {
            cacheInstallQueuePaused = false;
            if (cacheInstallQueue.Count > 0)
            {
                foreach (var controller in cacheInstallQueue)
                {
                    // . restore top panel queue state
                    topPanelViewModel.QueuedInstall(controller.gameCache.InstallSize, controller.gameCache.InstallEtaTimeSpan);

                    // . update transfer speed 
                    controller.speedLimitIPG = speedLimitIPG;
                }

                string title = cacheInstallQueue.First().gameCache.Title;
                if (speedLimitIPG > 0)
                {
                    if (Settings.NotifyOnInstallWhilePlayingActivity)
                    {
                        NotifyInfo(FormatResourceString("LOCNowPlayingContinueWithSpeedLimitFmt2", title, speedLimitIPG));
                    }
                    else
                    {
                        logger.Info($"Game started: NowPlaying installation for '{title}' continuing at limited speed (IPG={speedLimitIPG}).");
                    }
                }
                else if (resumeFromSpeedLimitedMode)
                {
                    if (Settings.NotifyOnInstallWhilePlayingActivity)
                    {
                        NotifyInfo(FormatResourceString("LOCNowPlayingContinueAtFullSpeedFmt", title));
                    }
                    else
                    {
                        logger.Info($"Game stopped; NowPlaying installation for '{title}' continuing at full speed.");
                    }
                }
                else
                {
                    if (Settings.NotifyOnInstallWhilePlayingActivity)
                    {
                        NotifyInfo(FormatResourceString("LOCNowPlayingResumeGameStoppedFmt", title));
                    }
                    else
                    {
                        logger.Info($"Game stopped; NowPlaying installation resumed for '{title}'.");
                    }
                }

                cacheInstallQueue.First().progressViewModel.PrepareToInstall(speedLimitIPG);
                Task.Run(() => cacheInstallQueue.First().NowPlayingInstall());
            }
        }

        private void SpeedLimitCacheInstalls(int speedLimitIPG)
        {
            PauseCacheInstallQueue(() => ResumeCacheInstallQueue(speedLimitIPG: speedLimitIPG));
        }

        private void ResumeFullSpeedCacheInstalls()
        {
            PauseCacheInstallQueue(() => ResumeCacheInstallQueue(resumeFromSpeedLimitedMode: true));
        }

        public void NotifyInfo(string message)
        {
            logger.Info(message);
            PlayniteApi.Notifications.Add(Guid.NewGuid().ToString(), message, NotificationType.Info);
        }

        public void NotifyWarning(string message, Action action = null)
        {
            logger.Warn(message);
            var notification = new NotificationMessage(Guid.NewGuid().ToString(), message, NotificationType.Info, action);
            PlayniteApi.Notifications.Add(notification);
        }

        public void NotifyError(string message, Action action = null)
        {
            logger.Error(message);
            var notification = new NotificationMessage(Guid.NewGuid().ToString(), message, NotificationType.Error, action);
            PlayniteApi.Notifications.Add(notification);
        }

        public void PopupError(string message)
        {
            logger.Error(message);
            PlayniteApi.Dialogs.ShowMessage(message, "NowPlaying Error:");
        }

        public string SaveJobErrorLogAndGetMessage(GameCacheJob job, string logFileSuffix)
        {
            string seeLogFile = "";
            if (job.errorLog != null)
            {
                // save error log
                string errorLogsDir = Path.Combine(GetPluginUserDataPath(), "errorLogs");
                string errorLogFile = Path.Combine(errorLogsDir, job.entry.Id + " " + DirectoryUtils.ToSafeFileName(job.entry.Title) + logFileSuffix);
                if (DirectoryUtils.MakeDir(errorLogsDir))
                {
                    try
                    {
                        const int showMaxErrorLineCount = 10;
                        File.Create(errorLogFile)?.Dispose();
                        File.WriteAllLines(errorLogFile, job.errorLog);
                        string nl = System.Environment.NewLine;
                        seeLogFile = nl + $"(See {errorLogFile})";

                        seeLogFile += nl + nl;
                        seeLogFile += $"Last {Math.Min(showMaxErrorLineCount, job.errorLog.Count())} lines:" + nl; 

                        // . show at most the last 'showMaxErrorLineCount' lines of the error log in the popup
                        foreach (var err in job.errorLog.Skip(Math.Max(0, job.errorLog.Count() - showMaxErrorLineCount)))
                        {
                            seeLogFile += err + nl;
                        }
                    }
                    catch { }
                }
            }
            return seeLogFile;
        }

    }
}
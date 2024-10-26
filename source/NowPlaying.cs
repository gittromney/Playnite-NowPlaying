using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Threading.Tasks;
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
using Playnite.SDK.Data;
using Color = System.Windows.Media.Color;
using System.Drawing.Imaging;
using UserControl = System.Windows.Controls.UserControl;
using NowPlaying.Controls;
using System.Windows.Controls;
using TopPanelItem = Playnite.SDK.Plugins.TopPanelItem;
using SidebarItem = Playnite.SDK.Plugins.SidebarItem;
using System.ComponentModel;

namespace NowPlaying
{
    public partial class NowPlaying : LibraryPlugin
    {
        public override Guid Id { get; } = Guid.Parse("0dbead64-b7ed-47e5-904c-0ccdb5d5ff59");
        public override string LibraryIcon { get; }
        public string StatusIconPath { get; }
        public bool statusIconBrushDarker;

        public override string Name => "NowPlaying Game Cacher";
        public static readonly ILogger logger = LogManager.GetLogger();

        public const string previewPlayActionName = "Preview (play from install directory)";
        public const string nowPlayingActionName = "Play from game cache";

        public NowPlayingSettings Settings { get; private set; }
        public readonly NowPlayingSettingsViewModel settingsViewModel;
        public readonly NowPlayingSettingsView settingsView;
        public readonly ThemeResources themeResources;
        public event EventHandler SettingsUpdated;

        public readonly CacheRootsViewModel cacheRootsViewModel;
        public readonly CacheRootsView cacheRootsView;

        public readonly TopPanelViewModel topPanelViewModel;
        public readonly TopPanelView topPanelView;
        public readonly TopPanelItem topPanelItem;

        public readonly SidebarIcon sidebarIcon;
        public readonly SidebarItem sidebarItem;

        public readonly NowPlayingPanelViewModel panelViewModel;
        public readonly NowPlayingPanelView panelView;

        public readonly GameCacheManagerViewModel cacheManager;

        public bool IsShutdownInProgress = false;
        public bool IsGamePlaying = false;
        public WhilePlaying WhilePlayingMode { get; private set; }
        public int SpeedLimitIpg { get; private set; } = 0;

        private string formatStringXofY;
        public Queue<NowPlayingGameEnabler> gameEnablerQueue;
        public Queue<NowPlayingInstallController> cacheInstallQueue;
        public Queue<NowPlayingUninstallController> cacheUninstallQueue;
        public string cacheInstallQueueStateJsonPath;
        public bool cacheInstallQueuePaused;

        public Action OnInstallProgressViewLoaded = null;

        public NowPlaying(IPlayniteAPI api) : base(api)
        {
            Properties = new LibraryPluginProperties
            {
                HasCustomizedGameImport = true,
                CanShutdownClient = true,
                HasSettings = true
            };

            LibraryIcon = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"icon.png");
            StatusIconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"status-icon.png");

            cacheManager = new GameCacheManagerViewModel(this, logger);

            formatStringXofY = GetResourceFormatString("LOCNowPlayingProgressXofYFmt2", 2) ?? "{0} of {1}";
            gameEnablerQueue = new Queue<NowPlayingGameEnabler>();
            cacheInstallQueue = new Queue<NowPlayingInstallController>();
            cacheUninstallQueue = new Queue<NowPlayingUninstallController>();
            cacheInstallQueueStateJsonPath = Path.Combine(GetPluginUserDataPath(), "cacheInstallQueueState.json");
            cacheInstallQueuePaused = false;

            Settings = LoadPluginSettings<NowPlayingSettings>() ?? new NowPlayingSettings();
            settingsViewModel = new NowPlayingSettingsViewModel(this);
            settingsView = new NowPlayingSettingsView(settingsViewModel);
            settingsView.Loaded += TweakSettingsViewThemeStyling;

            cacheRootsViewModel = new CacheRootsViewModel(this);
            cacheRootsView = new CacheRootsView(cacheRootsViewModel);

            this.themeResources = new ThemeResources(this, settingsView);

            panelViewModel = new NowPlayingPanelViewModel(this, themeResources);
            panelView = new NowPlayingPanelView(panelViewModel);
            panelViewModel.ResetShowState();

            topPanelViewModel = new TopPanelViewModel(this, themeResources);
            topPanelView = new TopPanelView(topPanelViewModel);
            topPanelItem = new TopPanelItem()
            {
                Title = GetResourceString("LOCNowPlayingTopPanelToolTip"),
                Icon = new TopPanelView(topPanelViewModel),
                Visible = false
            };

            this.sidebarIcon = new SidebarIcon(panelViewModel);
            this.sidebarItem = new SidebarItem()
            {
                Type = SiderbarItemType.View,
                Title = GetResourceString("LOCNowPlayingSideBarToolTip"),
                Visible = true,
                ProgressValue = 0,
                Icon = sidebarIcon,
                Opened = () =>
                {
                    return panelView;
                },
            };

            if (ColorizeStatusIcon())
            {
                statusIconBrushDarker = Settings.StatusIconBrushDarker;
                SettingsUpdated += (s, e) => MonitorIconBrushChanged();
            }
            else
            {
                NotifyWarning("Failed to color status info/menu tip icon (status-icon.png); using standard icon instead.");
                StatusIconPath = LibraryIcon;
                statusIconBrushDarker = false;
            }

            // . initialize search text from saved state (see Settings) and status column's MinWidth
            //    note: must be set after panelView is loaded; otherwise, it's overwritten w/ ""
            if (panelView.IsLoaded)
            {
                panelViewModel.SearchText = Settings.SearchText;
                panelViewModel.InitStatusColumnMinWidth();
            }
            else
            {
                panelView.Loaded += (s, e) =>
                {
                    panelViewModel.SearchText = Settings.SearchText;
                    panelViewModel.InitStatusColumnMinWidth();
                };
            }

            // . initialize column sorting state (see Settings)
            InitListViewSortingState(cacheRootsView.CacheRoots, Settings.CacheRootsSortedColumn, Settings.CacheRootsSortDirection);
            InitListViewSortingState(panelView.GameCaches, Settings.GameCachesSortedColumn, Settings.GameCachesSortDirection);
        }

        private void InitListViewSortingState(ListView listView, string sortedColumn, string sortDirection)
        {
            if (!string.IsNullOrEmpty(sortedColumn) && !string.IsNullOrEmpty(sortDirection))
            {
                if (listView.IsLoaded == true)
                {
                    var sortedColumnHeader = GridViewUtils.GetColumnHeaderByName(listView, sortedColumn);
                    if (sortedColumnHeader != null)
                    {
                        var direction = sortDirection == "Ascending" ? ListSortDirection.Ascending : ListSortDirection.Descending;
                        GridViewUtils.SetupAndApplyColumnSort(listView, sortedColumnHeader, sortedColumn, direction);
                    }
                    listView.Loaded -= (s, e) => InitListViewSortingState(listView, sortedColumn, sortDirection);
                }
                else
                {
                    listView.Loaded += (s, e) => InitListViewSortingState(listView, sortedColumn, sortDirection);
                }
            }
        }

        private void MonitorIconBrushChanged()
        {
            if (Settings.StatusIconBrushDarker != statusIconBrushDarker)
            {
                statusIconBrushDarker = Settings.StatusIconBrushDarker;
                ColorizeStatusIcon();
            }
        }

        private void TweakSettingsViewThemeStyling(object sender, RoutedEventArgs e)
        {
            // clear TabControl background color created by some themes (e.g. Daze)
            var settingsView = sender as NowPlayingSettingsView;
            if (settingsView != null)
            {
                var headerBorder = WpfUtils.GetChildByName(settingsView, "HeaderBorder") as Border;
                if (headerBorder != null)
                {
                    headerBorder.Background = themeResources.TransparentBrush;
                }
                var contentPanel = WpfUtils.GetChildByName(settingsView, "ContentPanel") as Border;
                if (contentPanel != null)
                {
                    contentPanel.Background = themeResources.TransparentBrush;
                }
                // apply only once per view instance
                settingsView.Loaded -= TweakSettingsViewThemeStyling;
            }
        }

        // . create "status-icon.png" for "NowPlaying status" menu items, color matched to theme's TextColor/TextColorDarker 
        public bool ColorizeStatusIcon()
        {
            try
            {
                var bitmap = Resources.now_playing_icon;
                var statusColor = (Color)PlayniteApi.Resources.GetResource(Settings.StatusIconBrushDarker ? "TextColorDarker" : "TextColor");
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        var pixelColor = bitmap.GetPixel(x, y);
                        if (pixelColor.A > 0)
                        {
                            var newColor = System.Drawing.Color.FromArgb(pixelColor.A, statusColor.R, statusColor.G, statusColor.B);
                            bitmap.SetPixel(x, y, newColor);
                        }
                    }
                }
                bitmap.Save(StatusIconPath, ImageFormat.Png);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void UpdateSettings(NowPlayingSettings settings)
        {
            Settings = settings;
            settingsViewModel.Settings = settings;
            SettingsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return new NowPlayingSettingsViewModel(this);
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            var settingsViewForPlaynite = new NowPlayingSettingsView(null);
            settingsViewForPlaynite.Loaded += TweakSettingsViewThemeStyling;
            return settingsViewForPlaynite;
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            themeResources.ApplyThemeStylingTweaks();

            cacheManager.LoadCacheRootsFromJson();
            cacheRootsViewModel.RefreshCacheRoots();
            cacheManager.LoadInstallAverageBpsFromJson();
            cacheManager.LoadGameCacheEntriesFromJson();
            cacheRootsViewModel.RefreshCacheRoots();

            PlayniteApi.Database.Games.ItemCollectionChanged += CheckForRemovedNowPlayingGames;

            Task.Run(() => CheckForFixableGameCacheIssuesAsync());

            // . if applicable, restore cache install queue state
            TryRestoreCacheInstallQueueState();
        }

        public class InstallerInfo
        {
            public string title;
            public string cacheId;
            public int speedLimitIpg;
            public InstallerInfo()
            {
                this.title = string.Empty;
                this.cacheId = string.Empty;
                this.speedLimitIpg = 0;
            }
        }

        public void SaveCacheInstallQueueToJson()
        {
            Queue<InstallerInfo> installerRestoreQueue = new Queue<InstallerInfo>();
            foreach (var ci in cacheInstallQueue)
            {
                var ii = new InstallerInfo()
                {
                    title = ci.gameCache.Title,
                    cacheId = ci.gameCache.Id, 
                    speedLimitIpg = ci.speedLimitIpg 
                };
                installerRestoreQueue.Enqueue(ii);
            }
            try
            {
                File.WriteAllText(cacheInstallQueueStateJsonPath, Serialization.ToJson(installerRestoreQueue));
            }
            catch (Exception ex)
            {
                logger.Error($"SaveInstallerQueueToJson to '{cacheInstallQueueStateJsonPath}' failed: {ex.Message}");
            }
        }

        public void TryRestoreCacheInstallQueueState()
        {
            if (File.Exists(cacheInstallQueueStateJsonPath))
            {
                logger.Info("Attempting to restore cache installation queue state...");

                var installersInfoList = new List<InstallerInfo>();
                try
                {
                    installersInfoList = Serialization.FromJsonFile<List<InstallerInfo>>(cacheInstallQueueStateJsonPath);
                }
                catch (Exception ex)
                {
                    logger.Error($"TryRestoreCacheInstallQueueState from '{cacheInstallQueueStateJsonPath}' failed: {ex.Message}");
                }

                foreach (var ii in installersInfoList)
                {
                    if (!CacheHasInstallerQueued(ii.cacheId))
                    {
                        if (cacheManager.GameCacheExists(ii.cacheId))
                        {
                            var nowPlayingGame = FindNowPlayingGame(ii.cacheId);
                            var gameCache = cacheManager.FindGameCache(ii.cacheId);
                            if (nowPlayingGame != null && gameCache != null)
                            {
                                NotifyInfo($"Restored cache install/queue state of '{gameCache.Title}'");
                                var controller = new NowPlayingInstallController(this, nowPlayingGame, gameCache, SpeedLimitIpg);
                                controller.Install(new InstallActionArgs());
                            }
                        }
                    }
                }
                File.Delete(cacheInstallQueueStateJsonPath);
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            IsShutdownInProgress = true;

            // Save Settings (including current SearchText and ListView sorting state)
            Settings.SearchText = panelViewModel.SearchText;
            if (panelView != null)
            {
                // . CacheRoots sorting state
                (Settings.CacheRootsSortedColumn, Settings.CacheRootsSortDirection) = GetListViewSortState(cacheRootsView.CacheRoots);

                // . GameCaches sorting state
                (Settings.GameCachesSortedColumn, Settings.GameCachesSortDirection) = GetListViewSortState(panelView.GameCaches);
            }
            SavePluginSettings(Settings);

            // Add code to be executed when Playnite is shutting down.
            if (cacheInstallQueue.Count > 0)
            {
                // . create install queue details file, so it can be restored on Playnite restart. 
                SaveCacheInstallQueueToJson();

                var title = cacheInstallQueue.First().gameCache.Title;
                logger.Warn($"Playnite exit detected: pausing game cache installation of '{title}'...");
                cacheInstallQueuePaused = true;
                cacheInstallQueue.First().PauseInstallOnPlayniteExit();
            }
            cacheManager.Shutdown();
        }

        private Tuple<string, string> GetListViewSortState(ListView listView)
        {
            var sortedColumn = GridViewUtils.GetSortedColumnName(listView);
            if (!string.IsNullOrEmpty(sortedColumn))
            {
                var sortedHeader = GridViewUtils.GetSortedColumnHeader(listView);
                var sortedByDefault = GridViewUtils.GetSortedByDefault(sortedHeader.Column);
                var sortAscending = GridViewUtils.GetSortDirection(listView) == ListSortDirection.Ascending;
                bool nonDefaultSorting = sortAscending ? sortedByDefault != "Ascending" : sortedByDefault != "Descending";
                if (nonDefaultSorting)
                {
                    return new Tuple<string, string>(sortedColumn, sortAscending ? "Ascending" : "Descending");
                }
            }
            return new Tuple<string, string>(string.Empty, string.Empty);
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when sourceGame is started running.
            if (args.Game.PluginId == Id)
            {
                Game nowPlayingGame = args.Game;
                string cacheId = nowPlayingGame.Id.ToString();
                cacheManager.SetGameCacheAndDirStateAsPlayed(cacheId);
            }

            IsGamePlaying = true;

            // Save relevant settings just in case the are changed while a game is playing
            WhilePlayingMode = Settings.WhilePlayingMode;
            SpeedLimitIpg = WhilePlayingMode == WhilePlaying.SpeedLimit ? Settings.SpeedLimitIpg : 0;

            if (WhilePlayingMode == WhilePlaying.Pause)
            {
                PauseCacheInstallQueue();
            }
            else if (SpeedLimitIpg > 0)
            {
                SpeedLimitCacheInstalls(SpeedLimitIpg);
            }
            panelViewModel.RefreshGameCaches();
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            base.OnGameStopped(args);

            if (IsGamePlaying)
            {
                IsGamePlaying = false;

                if (WhilePlayingMode == WhilePlaying.Pause)
                {
                    ResumeCacheInstallQueue();
                }
                else if (SpeedLimitIpg > 0)
                {
                    SpeedLimitIpg = 0;
                    ResumeFullSpeedCacheInstalls();
                }
                panelViewModel.RefreshGameCaches();
            }
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

        public async void CheckForFixableGameCacheIssuesAsync()
        {
            await CheckForBrokenNowPlayingGamesAsync();
            CheckForOrphanedCacheDirectories();
        }

        public async Task CheckForBrokenNowPlayingGamesAsync()
        {
            bool foundBroken = false;
            foreach (var game in PlayniteApi.Database.Games.Where(g => g.PluginId == this.Id))
            {
                string cacheId = game.Id.ToString();
                if (cacheManager.GameCacheExists(cacheId))
                {
                    // . check game platform and correct if necessary
                    var platform = GetGameCachePlatform(game);
                    var gameCache = cacheManager.FindGameCache(cacheId);

                    if (gameCache.entry.Platform != platform)
                    {
                        logger.Warn($"Corrected {game.Name}'s game cache platform encoding: {gameCache.Platform} → {platform}");
                        gameCache.entry.Platform = platform;
                        foundBroken = true;
                    }
                }
                else
                {
                    topPanelViewModel.NowProcessing(true, GetResourceString("LOCNowPlayingCheckBrokenGamesProgress"));

                    // . NowPlaying game is missing the supporting game cache... attempt to recreate it
                    if (await TryRecoverMissingGameCacheAsync(cacheId, game))
                    {
                        NotifyWarning(FormatResourceString("LOCNowPlayingRestoredCacheInfoFmt", game.Name));
                        foundBroken = true;
                    }
                    else 
                    { 
                        NotifyWarning(FormatResourceString("LOCNowPlayingUnabletoRestoreCacheInfoFmt", game.Name), () =>
                        {
                            string nl = Environment.NewLine;
                            string caption = GetResourceString("LOCNowPlayingConfirmationCaption");
                            string message = FormatResourceString("LOCNowPlayingUnabletoRestoreCacheInfoFmt", game.Name) + "." + nl + nl;
                            message += GetResourceString("LOCNowPlayingDisableBrokenGameCachingPrompt");
                            if (PlayniteApi.Dialogs.ShowMessage(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                DisableNowPlayingGameCaching(game);
                            }
                        });
                    }
                }
            }
            if (foundBroken)
            {
                cacheManager.SaveGameCacheEntriesToJson();
            }

            topPanelViewModel.NowProcessing(false, GetResourceString("LOCNowPlayingCheckBrokenGamesProgress"));
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

            if (!CacheHasInstallerQueued(cacheId))
            {
                if (cacheManager.GameCacheExists(cacheId))
                {
                    var gameCache = cacheManager.FindGameCache(cacheId);
                    if (gameCache.CacheWillFit)
                    {
                        var controller = new NowPlayingInstallController(this, nowPlayingGame, gameCache, SpeedLimitIpg);
                        return new List<InstallController> { controller };
                    }
                    else
                    {
                        string nl = Environment.NewLine;
                        string message = FormatResourceString("LOCNowPlayingCacheWillNotFitFmt2", gameCache.Title, gameCache.CacheDir);
                        message += nl + nl + GetResourceString("LOCNowPlayingCacheWillNotFitMsg");
                        PopupError(message);
                        return new List<InstallController> { new DummyInstaller(nowPlayingGame) };
                    }
                }
                else
                {
                    logger.Error($"Game cache information missing for '{nowPlayingGame.Name}'; skipping installation.");
                    return new List<InstallController> { new DummyInstaller(nowPlayingGame) };
                }
            }
            else
            {
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

            if (!CacheHasUninstallerQueued(cacheId))
            {
                if (cacheManager.GameCacheExists(cacheId))
                { 
                    return new List<UninstallController> { new NowPlayingUninstallController(this, nowPlayingGame, cacheManager.FindGameCache(cacheId)) };
                }
                else
                {
                    logger.Error($"Game cache information missing for '{nowPlayingGame.Name}'; skipping uninstall.");
                    return new List<UninstallController> { new DummyUninstaller(nowPlayingGame) };
                }
            }
            else
            {
                return new List<UninstallController> { new DummyUninstaller(nowPlayingGame) };
            }
        }

        public async Task<bool> TryRecoverMissingGameCacheAsync(string cacheId, Game nowPlayingGame)
        {
            string title = nowPlayingGame.Name;
            string installDir = null;
            string exePath = null;
            string xtraArgs = null;

            var previewPlayAction = GetPreviewPlayAction(nowPlayingGame);
            var nowPlayingAction = GetNowPlayingAction(nowPlayingGame);
            var platform = GetGameCachePlatform(nowPlayingGame);

            switch (platform)
            {
                case GameCachePlatform.WinPC:
                    installDir = previewPlayAction?.WorkingDir;
                    exePath = GetIncrementalExePath(nowPlayingAction, nowPlayingGame) ?? GetIncrementalExePath(previewPlayAction, nowPlayingGame);
                    xtraArgs = nowPlayingAction?.Arguments ?? previewPlayAction?.Arguments;
                    break;

                case GameCachePlatform.PS2:
                case GameCachePlatform.PS3:
                case GameCachePlatform.Xbox:
                case GameCachePlatform.X360:
                case GameCachePlatform.GameCube:
                case GameCachePlatform.Wii:
                case GameCachePlatform.Switch:
                    exePath = GetIncrementalRomPath(nowPlayingGame.Roms?.First()?.Path, nowPlayingGame.InstallDirectory, nowPlayingGame);
                    if (exePath != null)
                    {
                        // attempt to determine original installation directory (i.e. ROM path) from the emulator arguments
                        var emuArgList = StringUtils.QuotedSplit(previewPlayAction.Arguments);
                        foreach (var arg in StringUtils.Unquote(emuArgList))
                        {
                            var exePathIndex = arg.IndexOf(exePath);
                            if (exePathIndex > 1)
                            {
                                installDir = DirectoryUtils.TrimEndingSlash(arg.Substring(0, exePathIndex));
                                break;
                            }
                        }
                        // grab additional emulator arguments (if there are any) from the NowPlayingAction
                        xtraArgs = nowPlayingAction?.AdditionalArguments;
                    }
                    break;

                default: 
                    break;
            }

            if (installDir != null && exePath != null && await CheckIfGameInstallDirIsAccessibleAsync(title, installDir))
            {
                // . Separate cacheDir into its cacheRootDir and cacheSubDir components, assuming nowPlayingGame matching Cache RootDir exists.
                (string cacheRootDir, string cacheSubDir) = cacheManager.FindCacheRootAndSubDir(nowPlayingGame.InstallDirectory);

                if (cacheRootDir != null && cacheSubDir != null)
                {
                    cacheManager.AddGameCache(cacheId, title, installDir, exePath, xtraArgs, cacheRootDir, cacheSubDir, platform: platform);

                    // . the best we can do is assume the current size on disk are all installed bytes (completed files)
                    var gameCache = cacheManager.FindGameCache(cacheId);
                    gameCache.entry.UpdateCacheDirStats();
                    gameCache.entry.CacheSize = gameCache.entry.CacheSizeOnDisk;
                    gameCache.UpdateCacheSize();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public class SelectedGamesContext
        {
            private readonly NowPlaying plugin;
            public bool allEligible;
            public bool allEnabled;
            public bool allEnabledAndEmpty;
            public bool allEnabledAndNotEmpty;
            public bool allEnabledAndInstalled;
            public bool allEnabledAndNotInstalled;
            public int count;
            public int eligibleCount;
            public int enabledCount;
            public int installedCount;

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
                allEnabledAndNotEmpty = true;
                allEnabledAndInstalled = true;
                allEnabledAndNotInstalled = true;
                count = 0;
                eligibleCount = 0;
                enabledCount = 0;
                installedCount = 0;
            }

            public void UpdateContext(List<Game> games)
            {
                ResetContext();
                foreach (var game in games)
                {
                    var gameCache = plugin.cacheManager.FindGameCache(game.Id.ToString());
                    bool isEnabled = plugin.IsGameNowPlayingEnabled(game);
                    bool isEligible = !isEnabled && plugin.IsGameNowPlayingEligible(game) != GameCachePlatform.InEligible;
                    bool isEnabledAndEmpty = isEnabled && gameCache?.CacheSize == 0;
                    bool isEnabledAndNotEmpty = isEnabled && gameCache?.CacheSize > 0;
                    bool isEnabledAndInstalled = isEnabled && game.IsInstalled == true;
                    bool isEnabledAndNotInstalled = isEnabled && game.IsInstalled == false;
                    allEligible &= isEligible;
                    allEnabled &= isEnabled;
                    allEnabledAndEmpty &= isEnabledAndEmpty;
                    allEnabledAndNotEmpty &= isEnabledAndNotEmpty;
                    allEnabledAndInstalled &= isEnabledAndInstalled;
                    allEnabledAndNotInstalled &= isEnabledAndNotInstalled;
                    count++;
                    if (isEligible) { eligibleCount++; }
                    if (isEnabled) { enabledCount++; }
                    if (isEnabledAndInstalled) { installedCount++; }
                }
            }
        }
    
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var gameMenuItems = new List<GameMenuItem>();

            // . get selected games context
            var context = new SelectedGamesContext(this, args.Games);

            // . NowPlaying status + single enabled game menu tips
            //   -> skip "all eligible" case since information provided here would be redundant
            //      (see 'Enable game caching menu' code, below)
            //
            if (Settings.ShowStatusAndMenuTips == true && !context.allEligible)
            {
                string description = "";
                if (context.allEnabledAndInstalled)
                {
                    if (context.count > 1)
                    {
                        description = string.Format("NowPlaying cache status: >> Installed ({0} games) <<", context.count);
                    }
                    else
                    {
                        description = "NowPlaying cache status: >> Installed <<";
                    }
                }
                else if (context.allEnabledAndNotInstalled)
                {
                    if (context.count > 1)
                    {
                        description = string.Format("NowPlaying cache status: >> Uninstalled ({0} games) <<", context.count);
                    }
                    else
                    {
                        description = "NowPlaying cache status: >> Uninstalled <<";
                    }
                }

                // . Mixed status
                else if (context.eligibleCount > 0 || context.enabledCount > 0)
                {
                    description = "NowPlaying status:";
             
                    int ineligibleCount = context.count - context.enabledCount - context.eligibleCount;
                    int uninstalledCount = context.enabledCount - context.installedCount;
                    if (ineligibleCount > 0) 
                    {
                        if (ineligibleCount > 1)
                        {
                            description += Environment.NewLine + string.Format("{0} games are not eligible for caching", ineligibleCount);
                        }
                        else
                        {
                            description += Environment.NewLine + "1 game is not eligible for caching";
                        }
                    }
                    if (context.eligibleCount > 0) 
                    {
                        if (context.eligibleCount > 1)
                        {
                            description += Environment.NewLine + string.Format("{0} games are eligible for caching but not enabled", context.eligibleCount);
                        }
                        else
                        {
                            description += Environment.NewLine + "1 game is eligible for caching but not enabled";
                        }
                    }
                    if (context.installedCount > 0) 
                    {
                        if (context.installedCount > 1)
                        {
                            description += Environment.NewLine + string.Format("{0} games have caches installed", context.installedCount);
                        }
                        else
                        {
                            description += Environment.NewLine + "1 game has its cache installed";
                        }
                    } 
                    if (uninstalledCount > 0) 
                    {
                        if (uninstalledCount > 1)
                        {
                            description += Environment.NewLine + string.Format("{0} games have caches uninstalled", uninstalledCount);
                        }
                        else
                        {
                            description += Environment.NewLine + "1 game has its cache uninstalled";
                        }
                    }
                }

                else
                {
                    if (context.count > 1)
                    {
                        description = string.Format("NowPlaying: games are not eligible for caching ({0} games)", context.count);
                    }
                    else
                    {
                        description = "NowPlaying: game is not eligible for caching";
                    }
                }

                // . Playnite Menu Tips (single game only)
                if (context.allEnabled && context.count == 1)
                {
                    description += Environment.NewLine + "Playnite menu tips:";
                    var leadin = Environment.NewLine + "      ";
                    if (context.allEnabledAndNotInstalled) { description += leadin + "• Install  \t-    create game cache"; }
                    if (context.allEnabledAndNotInstalled) { description += leadin + "• Preview  \t-    play from slow device w/o installing cache"; }
                    if (context.allEnabledAndInstalled) { description += leadin + "• Play     \t-    play from game cache"; }
                    if (context.allEnabledAndInstalled) { description += leadin + "• Uninstall\t-    delete game cache"; }
                }
                gameMenuItems.Add(new GameMenuItem
                {
                    Icon = StatusIconPath,
                    Description = description,
                    Action = null
                });
            }

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
                            Icon = LibraryIcon,
                            MenuSection = description,
                            Description = GetResourceString("LOCNowPlayingTermsTo") + " " + cacheRoot.Directory,
                            Action = async (a) => { foreach (var game in args.Games) { await EnableNowPlayingWithRootAsync(game, cacheRoot); } }
                        });
                    }
                }
                else
                {
                    var cacheRoot = cacheManager.CacheRoots.First();
                    gameMenuItems.Add(new GameMenuItem
                    {
                        Icon = LibraryIcon,
                        Description = description,
                        Action = async (a) => { foreach (var game in args.Games) { await EnableNowPlayingWithRootAsync(game, cacheRoot); } }
                    });
                }
            }

            // . Install game cache(s) menu
            if (context.allEnabledAndNotInstalled && context.count > 1)
            {
                string description = string.Format("NowPlaying: Install game caches ({0} games)", context.count);

                gameMenuItems.Add(new GameMenuItem
                {
                    Icon = LibraryIcon,
                    Description = description,
                    Action = (a) =>
                    {
                        var gameCaches = new List<GameCacheViewModel>();
                        foreach (var game in args.Games)
                        {
                            var gameCache = cacheManager.FindGameCache(game.Id.ToString());
                            if (gameCache != null) {
                                gameCaches.Add(gameCache);
                            }
                        }
                        this.panelViewModel.InstallSelectedCaches(gameCaches);
                    }
                });
            }

            // . Uninstall game cache(s) menu
            if (context.allEnabledAndNotEmpty && context.count > 1)
            {
                string description = string.Format("NowPlaying: Uninstall game caches ({0} games)", context.count);

                gameMenuItems.Add(new GameMenuItem
                {
                    Icon = LibraryIcon,
                    Description = description,
                    Action = (a) =>
                    {
                        foreach (var game in args.Games)
                        {
                            var gameCache = cacheManager.FindGameCache(game.Id.ToString());
                            if (gameCache != null)
                            {
                                this.panelViewModel.UninstallGameCache(gameCache);
                            }
                        }
                    }
                });
            }

            // . Uninstall + Disable caching menu item
            if (context.allEnabledAndNotEmpty && Settings.ShowUninstallAndDisableMenu == true)
            {
                string description = "NowPlaying: ";
                if (context.count > 1)
                {
                    description += string.Format("Uninstall & Disable - delete caches and disable caching ({0} games)", context.count);
                }
                else
                {
                    description += "Uninstall & Disable - delete game cache and disable caching";
                }

                gameMenuItems.Add(new GameMenuItem
                {
                    Icon = LibraryIcon,
                    Description = description,
                    Action = (a) =>
                    {
                        foreach (var game in args.Games)
                        {
                            var gameCache = cacheManager.FindGameCache(game.Id.ToString());
                            if (gameCache != null)
                            {
                                this.panelViewModel.UninstallGameCache(gameCache, andThenDisableCaching: true);                                
                            }
                        }
                    }
                });
            }

            // . Disable game caching menu
            if (context.allEnabledAndEmpty)
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
                    Icon = LibraryIcon,
                    Description = description,
                    Action = (a) => 
                    { 
                        foreach (var game in args.Games) 
                        {
                            var cacheId = game.Id.ToString();
                            var gameCache = cacheManager.FindGameCache(cacheId);
                            var installDir = gameCache?.InstallDir;
                            var exePath = gameCache?.ExePath;
                            var xtraArgs = gameCache?.XtraArgs;
                            DisableNowPlayingGameCaching(game, installDir, exePath, xtraArgs);
                            cacheManager.RemoveGameCache(cacheId);
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

        public GameCachePlatform IsGameNowPlayingEligible(Game game)
        {
            bool preReq = (
                game != null
                && game.PluginId == Guid.Empty
                && game.IsInstalled
                && game.IsCustomGame
                && game.GameActions?.Where(a => a.IsPlayAction).Count() == 1
            );
            if (preReq)
            {
                var platform = GetGameCachePlatform(game);
                if (platform == GameCachePlatform.WinPC)
                {
                    if ((game.Roms == null || game.Roms?.Count == 0)
                        && GetIncrementalExePath(game.GameActions?[0], game) != null)
                    {
                        return GameCachePlatform.WinPC;
                    }
                    else
                    {
                        return GameCachePlatform.InEligible;
                    }
                }
                else if (platform != GameCachePlatform.InEligible)  // Not PC but eligible == emulated platform
                {
                    if (game.Roms?.Count == 1
                        && game.GameActions?[0].Type == GameActionType.Emulator
                        && game.GameActions?[0].EmulatorId != Guid.Empty
                        && game.GameActions?[0].OverrideDefaultArgs == false
                        && GetIncrementalRomPath(game.Roms?[0].Path, game.InstallDirectory, game) != null)
                    {
                        return platform;
                    }
                    else
                    {
                        return GameCachePlatform.InEligible;
                    }
                }
                else
                {
                    return GameCachePlatform.InEligible;
                }
            }
            else
            {
                return GameCachePlatform.InEligible;
            }
        }

        static public GameCachePlatform GetGameCachePlatform(Game game)
        {
            var gcPlatform = GameCachePlatform.InEligible;
            var hitCount = 0;

            // Allow for a list of Playnite "platforms", which may potentially include
            // user-created dummy platforms used for grouping games by type or similar, but the
            // list must include EXACTLY ONE eligible platform; otherwise the game is ineligible
            // for game caching.
            //
            foreach (var platform in game?.Platforms)
            {
                var specId = platform?.SpecificationId;
                if (specId == "pc_windows")
                {
                    gcPlatform = GameCachePlatform.WinPC;
                    hitCount++;
                }
                else if (specId == "sony_playstation2")
                {
                    gcPlatform = GameCachePlatform.PS2;
                    hitCount++;
                }
                else if (specId == "sony_playstation3")
                {
                    gcPlatform = GameCachePlatform.PS3;
                    hitCount++;
                }
                else if (specId == "xbox")
                {
                    gcPlatform = GameCachePlatform.Xbox;
                    hitCount++;
                }
                else if (specId == "xbox360")
                {
                    gcPlatform = GameCachePlatform.X360;
                    hitCount++;
                }
                else if (specId == "nintendo_gamecube")
                {
                    gcPlatform = GameCachePlatform.GameCube;
                    hitCount++;
                }
                else if (specId == "nintendo_wii")
                {
                    gcPlatform = GameCachePlatform.Wii;
                    hitCount++;
                }
                else if (specId == "nintendo_switch")
                {
                    gcPlatform = GameCachePlatform.Switch;
                    hitCount++;
                }
            }
            return hitCount==1 ? gcPlatform : GameCachePlatform.InEligible;
        }

        public bool IsGameNowPlayingEnabled(Game game)
        {
            return game.PluginId == this.Id && cacheManager.GameCacheExists(game.Id.ToString());
        }

        public async Task EnableNowPlayingWithRootAsync(Game game, CacheRootViewModel cacheRoot)
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

            else if (await CheckIfGameInstallDirIsAccessibleAsync(game.Name, game.InstallDirectory))
            {
                if (CheckAndConfirmOrAdjustInstallDirDepth(game))
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
        public bool CheckAndConfirmOrAdjustInstallDirDepth(Game game)
        {
            string title = game.Name;
            string installDirectory = DirectoryUtils.CollapseMultipleSlashes(game.InstallDirectory);
            var platform = GetGameCachePlatform(game);

            var problematicKeywords = platform == GameCachePlatform.PS3 ? Settings.ProblematicPS3InstDirKeywords : Settings.ProblematicInstallDirKeywords;

            List<string> matchedSubdirs = new List<string>();
            string recommendedInstallDir = string.Empty;

            foreach (var subDir in installDirectory.Split(Path.DirectorySeparatorChar))
            {
                foreach (var keyword in problematicKeywords)
                {
                    if (String.Equals(subDir, keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedSubdirs.Add(subDir);
                    }
                }
                if (matchedSubdirs.Count == 0)
                {
                    if (string.IsNullOrEmpty(recommendedInstallDir))
                    {
                        recommendedInstallDir = subDir;
                    }
                    else
                    {
                        recommendedInstallDir += Path.DirectorySeparatorChar + subDir;
                    }
                }
            }

            bool continueWithEnable = true;
            if (matchedSubdirs.Count > 0)
            {
                string nl = System.Environment.NewLine;
                string problematicSubdirs = string.Join("', '", matchedSubdirs);

                // . See if user wants to adopt the recommended, shallower Install Directory
                string message = FormatResourceString("LOCNowPlayingProblematicInstallDirFmt3", title, installDirectory, problematicSubdirs);
                message += nl + nl + FormatResourceString("LOCNowPlayingChangeInstallDirFmt1", recommendedInstallDir);
                string caption = GetResourceString("LOCNowPlayingConfirmationCaption");

                bool changeInstallDir = 
                (
                    Settings.ChangeProblematicInstallDir_DoWhen == DoWhen.Always ||
                    Settings.ChangeProblematicInstallDir_DoWhen == DoWhen.Ask 
                    && (PlayniteApi.Dialogs.ShowMessage(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                );

                if (changeInstallDir)
                {
                    string exePath;
                    string missingExePath;
                    bool success = false;
 
                    switch (platform)
                    {
                        case GameCachePlatform.WinPC:
                            var sourcePlayAction = GetSourcePlayAction(game);
                            exePath = GetIncrementalExePath(sourcePlayAction);
                            if (sourcePlayAction != null && exePath != null)
                            {
                                missingExePath = installDirectory.Substring(recommendedInstallDir.Length + 1);
                                game.InstallDirectory = recommendedInstallDir;
                                sourcePlayAction.Path = Path.Combine(sourcePlayAction.WorkingDir, missingExePath, exePath);
                                PlayniteApi.Database.Games.Update(game);
                                success = true;
                            }
                            break;

                        case GameCachePlatform.PS2:
                        case GameCachePlatform.PS3:
                        case GameCachePlatform.Xbox:
                        case GameCachePlatform.X360:
                        case GameCachePlatform.GameCube:
                        case GameCachePlatform.Wii:
                        case GameCachePlatform.Switch:
                            var rom = game.Roms?.First();
                            if (rom != null && (exePath = GetIncrementalRomPath(rom.Path, installDirectory, game)) != null)
                            {
                                missingExePath = installDirectory.Substring(recommendedInstallDir.Length + 1);
                                game.InstallDirectory = recommendedInstallDir;
                                var exePathIndex = rom.Path.IndexOf(exePath);
                                if (exePathIndex > 0)
                                {
                                    rom.Path = Path.Combine(rom.Path.Substring(0, exePathIndex), missingExePath, exePath);
                                    PlayniteApi.Database.Games.Update(game);
                                    success = true;
                                }
                            }
                            break;

                        default:
                            break;
                    }

                    if (success)
                    {
                        NotifyInfo(FormatResourceString("LOCNowPlayingChangedInstallDir", title, recommendedInstallDir));
                        continueWithEnable = true;
                    }
                    else
                    {
                        PopupError(FormatResourceString("LOCNowPlayingErrorChangingInstallDir", title, recommendedInstallDir));
                        continueWithEnable = false;
                    }
                }
                else
                {
                    // . See if user wants to continue enabling game, anyway
                    message = FormatResourceString("LOCNowPlayingProblematicInstallDirFmt3", title, installDirectory, problematicSubdirs);
                    message += nl + nl + GetResourceString("LOCNowPlayingConfirmOrEditInstallDir");
                    message += nl + nl + GetResourceString("LOCNowPlayingProblematicInstallDirConfirm");
                    continueWithEnable = PlayniteApi.Dialogs.ShowMessage(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
                }
            }
            return continueWithEnable;
        }

        // . Sanity check: make sure game's InstallDir is accessable (e.g. disk mounted, decrypted, etc?)
        public async Task<bool> CheckIfGameInstallDirIsAccessibleAsync(string title, string installDir, bool silentMode = false)
        {
            if (installDir != null)
            {
                // . This can take awhile (e.g. if the install directory is on a network drive and is having connectivity issues)
                //   -> display topPanel status, unless caller is already doing so.
                //
                bool showInTopPanel = !topPanelViewModel.IsProcessing;
                if (showInTopPanel)
                {
                    topPanelViewModel.NowProcessing(true, GetResourceString("LOCNowPlayingCheckInstallDirAccessibleProgress"));
                }

                var installRoot = await DirectoryUtils.TryGetRootDeviceAsync(installDir);
                var dirExists = await Task.Run(() => Directory.Exists(installDir));

                if (showInTopPanel)
                {
                    topPanelViewModel.NowProcessing(false, GetResourceString("LOCNowPlayingCheckInstallDirAccessibleProgress"));
                }

                if (installRoot != null && dirExists)
                {
                    return true;
                }
                else
                {
                    if (!silentMode)
                    {
                        PopupError(FormatResourceString("LOCNowPlayingGameInstallDirNotFoundFmt2", title, installDir));
                    }
                    else
                    {
                        logger.Error(FormatResourceString("LOCNowPlayingGameInstallDirNotFoundFmt2", title, installDir));
                    }
                    return false;
                }
            }
            else
            {
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

        public async void DequeueEnablerAndInvokeNextAsync(string id)
        {
            // Dequeue the enabler (and sanity check it was ours)
            var activeId = gameEnablerQueue.Dequeue().Id;
            Debug.Assert(activeId == id, $"Unexpected game enabler Id at head of the Queue ({activeId})");

            // . update status of queued enablers
            topPanelViewModel.EnablerDoneOrCancelled();

            // Invoke next in queue's enabler, if applicable.
            if (gameEnablerQueue.Count > 0)
            {
                await gameEnablerQueue.First().EnableGameForNowPlayingAsync();
            }

            panelViewModel.RefreshGameCaches();
        }

        public bool DisableNowPlayingGameCaching(Game game, string installDir = null, string exePath = null, string xtraArgs = null)
        {
            var previewPlayAction = GetPreviewPlayAction(game);
            var platform = GetGameCachePlatform(game);

            // . attempt to extract original install and play action parameters, if not provided by caller
            if (installDir == null || exePath == null)
            {
                if (previewPlayAction != null)
                {
                    switch (platform)
                    {
                        case GameCachePlatform.WinPC:
                            exePath = GetIncrementalExePath(previewPlayAction);
                            installDir = previewPlayAction.WorkingDir;

                            // if not already provided, grab additional game arguments (if there are any) from the preview play action
                            if (xtraArgs == null)
                            {
                                xtraArgs = previewPlayAction.Arguments;
                            }
                            break;

                        case GameCachePlatform.PS2:
                        case GameCachePlatform.PS3:
                        case GameCachePlatform.Xbox:
                        case GameCachePlatform.X360:
                        case GameCachePlatform.GameCube:
                        case GameCachePlatform.Wii:
                        case GameCachePlatform.Switch:
                            exePath = GetIncrementalRomPath(game.Roms?.First()?.Path, game.InstallDirectory, game);
                            if (exePath != null)
                            {
                                // attempt to determine original installation directory (i.e. ROM path) from the emulator arguments
                                var emuArgList = StringUtils.QuotedSplit(previewPlayAction.Arguments);
                                foreach (var arg in StringUtils.Unquote(emuArgList))
                                {
                                    var exePathIndex = arg.IndexOf(exePath);
                                    if (exePathIndex > 1)
                                    {
                                        installDir = DirectoryUtils.TrimEndingSlash(arg.Substring(0, exePathIndex));
                                        break;
                                    }
                                }

                                // if not already provided, grab additional emulator arguments (if there are any) from the NowPlayingAction
                                if (xtraArgs == null)
                                {
                                    var nowPlayingAction = GetNowPlayingAction(game);
                                    xtraArgs = nowPlayingAction?.AdditionalArguments;
                                }
                            }
                            break;

                        default:
                            break;
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
                switch (platform)
                {
                    case GameCachePlatform.WinPC:
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
                        break;

                    case GameCachePlatform.PS2:
                    case GameCachePlatform.PS3:
                    case GameCachePlatform.Xbox:
                    case GameCachePlatform.X360:
                    case GameCachePlatform.GameCube:
                    case GameCachePlatform.Wii:
                    case GameCachePlatform.Switch:
                        var imagePath = Path.Combine(installDir, exePath);
                        if (xtraArgs != null)
                        {
                            xtraArgs = xtraArgs 
                                .Replace(imagePath, "{ImagePath}")
                                .Replace(exePath, "{ImageName}")
                                .Replace(installDir, "{InstallDir}");
                        }
                        game.GameActions.Add
                        (
                             new GameAction()
                             {
                                 Name = game.Name,
                                 Type = GameActionType.Emulator,
                                 EmulatorId = previewPlayAction.EmulatorId,
                                 EmulatorProfileId = previewPlayAction.EmulatorProfileId,
                                 AdditionalArguments = xtraArgs,
                                 IsPlayAction = true
                             }
                        );
                        break;

                    default:
                        break;
                }

                PlayniteApi.Database.Games.Update(game);
                return true;
            }
            else
            {
                return false;
            }
        }

        static public GameAction GetSourcePlayAction(Game game)
        {
            var actions = game.GameActions.Where(a => a.IsPlayAction);
            return actions.Count() == 1 ? actions.First() : null;
        }

        static public GameAction GetNowPlayingAction(Game game)
        {
            var actions = game.GameActions.Where(a => a.IsPlayAction && a.Name == nowPlayingActionName);
            return actions.Count() == 1 ? actions.First() : null;
        }

        static public GameAction GetPreviewPlayAction(Game game)
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

                work = DirectoryUtils.CollapseMultipleSlashes(work);
                path = DirectoryUtils.CollapseMultipleSlashes(path);

                if (work != null && path != null && work == path.Substring(0, work.Length))
                {
                    return path.Substring(DirectoryUtils.TrimEndingSlash(work).Length + 1);
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

        public string GetIncrementalRomPath(string romPath, string installDir, Game variableReferenceGame = null)
        {
            if (romPath != null && installDir != null)
            {
                string work, path;
                if (variableReferenceGame != null)
                {
                    work = PlayniteApi.ExpandGameVariables(variableReferenceGame, installDir);
                    path = PlayniteApi.ExpandGameVariables(variableReferenceGame, romPath);
                }
                else
                {
                    work = installDir;
                    path = romPath;
                }

                work = DirectoryUtils.CollapseMultipleSlashes(work);
                path = DirectoryUtils.CollapseMultipleSlashes(path);

                if (work != null && path != null && work.Length <= path.Length && work == path.Substring(0, work.Length))
                {
                    return path.Substring(DirectoryUtils.TrimEndingSlash(work).Length + 1);
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
                panelViewModel.OnInstallUninstallQueuesUpdated();
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
                panelViewModel.OnInstallUninstallQueuesUpdated();
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
                
                // . exit installing state
                controller.Game.IsInstalling = false;
                PlayniteApi.Database.Games.Update(controller.Game);

                // . update game cache state
                var gameCache = cacheManager.FindGameCache(cacheId);
                gameCache.UpdateInstallQueueStatus(null);
                UpdateInstallQueueStatuses();
                topPanelViewModel.CancelledFromInstallQueue(gameCache.InstallSize, gameCache.InstallEtaTimeSpan);
                panelViewModel.OnInstallUninstallQueuesUpdated();
                panelViewModel.RefreshGameCaches();
            }
        }

        public async void DequeueInstallerAndInvokeNextAsync(string cacheId)
        {
            // Dequeue the controller (and sanity check it was ours)
            var activeId = cacheInstallQueue.Dequeue().gameCache.Id;
            Debug.Assert(activeId == cacheId, $"Unexpected install controller cacheId at head of the Queue ({activeId})");

            // . update install queue status of queued installers & top panel status
            UpdateInstallQueueStatuses();
            topPanelViewModel.InstallDoneOrCancelled();
            panelViewModel.OnInstallUninstallQueuesUpdated();

            // Invoke next in queue's controller, if applicable.
            if (cacheInstallQueue.Count > 0 && !cacheInstallQueuePaused)
            {
                await cacheInstallQueue.First().NowPlayingInstallAsync();
            }
            else
            {
                topPanelViewModel.TopPanelVisible = false;
            }
        }

        public async void DequeueUninstallerAndInvokeNextAsync(string cacheId)
        {
            // Dequeue the controller (and sanity check it was ours)
            var controller = cacheUninstallQueue.Dequeue();
            var activeId = controller.gameCache.Id;
            Debug.Assert(activeId == cacheId, $"Unexpected uninstall controller cacheId at head of the Queue ({activeId})");

            // . update status of queued uninstallers
            UpdateUninstallQueueStatuses();
            topPanelViewModel.UninstallDoneOrCancelled();
            panelViewModel.OnInstallUninstallQueuesUpdated();

            // Invoke next in queue's controller, if applicable.
            if (cacheUninstallQueue.Count > 0)
            {
                await cacheUninstallQueue.First().NowPlayingUninstallAsync();
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

        private void ResumeCacheInstallQueue(int speedLimitIpg = 0, bool resumeFromSpeedLimitedMode = false)
        {
            cacheInstallQueuePaused = false;
            if (cacheInstallQueue.Count > 0)
            {
                foreach (var controller in cacheInstallQueue)
                {
                    // . restore top panel queue state
                    topPanelViewModel.QueuedInstall(controller.gameCache.InstallSize, controller.gameCache.InstallEtaTimeSpan);

                    // . update transfer speed 
                    controller.speedLimitIpg = speedLimitIpg;
                }

                // . possibly adjust Status column width
                panelViewModel.OnInstallUninstallQueuesUpdated();

                string title = cacheInstallQueue.First().gameCache.Title;
                if (speedLimitIpg > 0)
                {
                    if (Settings.NotifyOnInstallWhilePlayingActivity)
                    {
                        NotifyInfo(FormatResourceString("LOCNowPlayingContinueWithSpeedLimitFmt2", title, speedLimitIpg));
                    }
                    else
                    {
                        logger.Info($"Game started: NowPlaying installation for '{title}' continuing at limited speed (IPG={speedLimitIpg}).");
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

                cacheInstallQueue.First().progressViewModel.PrepareToInstall(speedLimitIpg);
                Task.Run(() => cacheInstallQueue.First().NowPlayingInstallAsync());
            }
        }

        private void SpeedLimitCacheInstalls(int speedLimitIpg)
        {
            PauseCacheInstallQueue(() => ResumeCacheInstallQueue(speedLimitIpg: speedLimitIpg));
        }

        private void ResumeFullSpeedCacheInstalls()
        {
            PauseCacheInstallQueue(() => ResumeCacheInstallQueue(resumeFromSpeedLimitedMode: true));
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
                for (int fi = 0; validFormat && fi < formatItemCount; fi++)
                {
                    validFormat &= formatString.Contains("{" + fi + "}");
                }
                if (validFormat)
                {
                    return formatString;
                }
                else if (formatItemCount > 1)
                {
                    PopupError($"Bad resource: key='{key}', value='{formatString}'; must contain '{{0}} - '{{{formatItemCount - 1}}}' patterns");
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
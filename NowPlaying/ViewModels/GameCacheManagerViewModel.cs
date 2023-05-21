using NowPlaying.Utils;
using NowPlaying.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace NowPlaying.ViewModels
{
    public class GameCacheManagerViewModel : ViewModelBase
    {
        private readonly NowPlaying plugin;
        private readonly string pluginUserDataPath;
        private readonly string cacheRootsJsonPath;
        private readonly string gameCacheEntriesJsonPath;
        private readonly string installAverageBpsJsonPath;

        public readonly GameCacheManager gameCacheManager;

        public ObservableCollection<CacheRootViewModel> CacheRoots { get; private set; }
        public ObservableCollection<GameCacheViewModel> GameCaches { get; private set; }

        public Dictionary<string,long> InstallAverageBps { get; private set; }

        public GameCacheManagerViewModel(NowPlaying plugin)
        {
            this.plugin = plugin;
            this.pluginUserDataPath = plugin.GetPluginUserDataPath();

            cacheRootsJsonPath = Path.Combine(pluginUserDataPath, "CacheRoots.json");
            gameCacheEntriesJsonPath = Path.Combine(pluginUserDataPath, "gameCacheEntries.json");
            installAverageBpsJsonPath = Path.Combine(pluginUserDataPath, "InstallAverageBps.json");

            gameCacheManager = new GameCacheManager();
            CacheRoots = new ObservableCollection<CacheRootViewModel>();
            GameCaches = new ObservableCollection<GameCacheViewModel>();
            InstallAverageBps = new Dictionary<string,long>();
        }

        public void UpdateGameCaches()
        {
            foreach (var gameCache in GameCaches)
            {
                gameCache.UpdateCacheRoot();
            }
            OnPropertyChanged(nameof(GameCaches));
        }

        public void AddCacheRoot(string rootDirectory, double maximumFillLevel)
        {
            if (!CacheRootExists(rootDirectory))
            {
                // . add cache root
                var root = new CacheRoot(rootDirectory, maximumFillLevel);
                gameCacheManager.AddCacheRoot(root);

                // . add cache root view model
                CacheRoots.Add(new CacheRootViewModel(this, root));

                SaveCacheRootsToJson();
                NowPlaying.logger.Info($"Added cache root '{rootDirectory}' with {maximumFillLevel}% max fill.");
            }
        }

        public void RemoveCacheRoot(string rootDirectory)
        {
            if (FindCacheRoot(rootDirectory)?.GameCaches.Count() == 0)
            {
                // . remove cache root
                gameCacheManager.RemoveCacheRoot(rootDirectory);

                // . remove cache root view model
                CacheRoots.Remove(FindCacheRoot(rootDirectory));

                SaveCacheRootsToJson();
                NowPlaying.logger.Info($"Removed cache root '{rootDirectory}'.");
            }
        }

        public bool CacheRootExists(string rootDirectory)
        {
            return CacheRoots.Where(r => r.Directory == rootDirectory).Count() > 0;
        }

        public CacheRootViewModel FindCacheRoot(string rootDirectory)
        {
            var roots = CacheRoots.Where(r => r.Directory == rootDirectory);
            return rootDirectory != null && roots.Count() == 1 ? roots.First() : null;
        }

        public string AddGameCache
            (
                string cacheId,
                string title,
                string installDir,
                string exePath,
                string xtraArgs,
                string cacheRootDir,
                string cacheSubDir = null
            )
        {
            if (!GameCacheExists(cacheId) && CacheRootExists(cacheRootDir) && plugin.CheckIfGameInstallDirIsAccessible(title, installDir))
            {
                // . re-encode cacheSubDir as 'null' if it represents the file-safe game title.
                if (cacheSubDir?.Equals(DirectoryUtils.ToSafeFileName(title)) == true)
                {
                    cacheSubDir = null;
                }

                // . create new game cache entry
                gameCacheManager.AddGameCacheEntry(cacheId, title, installDir, exePath, xtraArgs, cacheRootDir, cacheSubDir);

                // . update install/cache dir stats/sizes
                var entry = gameCacheManager.GetGameCacheEntry(cacheId);
                entry.UpdateInstallDirStats();
                entry.UpdateCacheDirStats();

                // . add new game cache view model
                var cacheRoot = FindCacheRoot(cacheRootDir);
                var gameCache = new GameCacheViewModel(this, entry, cacheRoot);

                // . use UI dispatcher if necessary (i.e. if this is called from a Game Enabler / background task)
                if (plugin.panelView.Dispatcher.CheckAccess())
                {
                    GameCaches.Add(gameCache);
                }
                else
                {
                    plugin.panelView.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(() => GameCaches.Add(gameCache)));
                }

                // . update respective cache root view model of added game cache
                cacheRoot.UpdateGameCaches();

                SaveGameCacheEntriesToJson();
                NowPlaying.logger.Info($"Added game cache: '{entry}'");

                // . return the games cache directory (or null)
                return entry.CacheDir;
            }
            else
            {
                // . couldn't create, null cache directory
                return null;
            }
        }

        public void RemoveGameCache(string cacheId)
        {
            var gameCache = FindGameCache(cacheId);
            
            if (gameCache != null)
            {
                // . remove game cache entry
                gameCacheManager.RemoveGameCacheEntry(cacheId);

                // . remove game cache view model
                GameCaches.Remove(gameCache);

                // . notify cache root view model of the change
                gameCache.cacheRoot.UpdateGameCaches();

                SaveGameCacheEntriesToJson();
                NowPlaying.logger.Info($"Removed game cache: '{gameCache.entry}'");
            }
        }

        public bool GameCacheExists(string cacheId)
        {
            return GameCaches.Where(gc => gc.Id == cacheId).Count() > 0;
        }

        public GameCacheViewModel FindGameCache(string cacheId)
        {
            var caches = GameCaches.Where(gc => gc.Id == cacheId);
            return caches.Count() == 1 ? caches.First() : null;
        }


        public bool IsPopulateInProgess(string cacheId)
        {
            return gameCacheManager.IsPopulateInProgess(cacheId);
        }

        public void SetGameCacheAndDirStateAsPlayed(string cacheId)
        {
            if (GameCacheExists(cacheId))
            {
                gameCacheManager.SetGameCacheAndDirStateAsPlayed(cacheId);
            }
        }

        public long GetInstallAverageBps(string installDir, bool isSpeedLimited = false)
        {
            string installDevice = Directory.GetDirectoryRoot(installDir);
            if (isSpeedLimited)
            {
                installDevice += "(speed limited)";
            }

            if (InstallAverageBps.ContainsKey(installDevice))
            {
                return InstallAverageBps[installDevice];
            }
            else
            {
                return isSpeedLimited ? 5242880 : 52428800; // initial default: 50 MB/s (or speed limited => 5 MB/s) 
            }
        }

        public void UpdateInstallAverageBps(string installDir, long averageBps, bool isSpeedLimited = false)
        {
            string installDevice = Directory.GetDirectoryRoot(installDir);
            if (isSpeedLimited)
            {
                installDevice += "(speed limited)";
            }

            if (InstallAverageBps.ContainsKey(installDevice))
            {
                // take 50/50 average of saved Bps and current value
                InstallAverageBps[installDevice] = (InstallAverageBps[installDevice] + averageBps) / 2;
            }
            else
            {
                InstallAverageBps.Add(installDevice, averageBps);
            }
            SaveInstallAverageBpsToJson();
        }

        public void SaveInstallAverageBpsToJson()
        {
            try
            {
                File.WriteAllText(installAverageBpsJsonPath, Serialization.ToJson(InstallAverageBps));
            }
            catch
            {
                plugin.PopupError("SaveInstallAverageBpsToJson failed");
            }
        }

        public void LoadInstallAverageBpsFromJson()
        {
            if (File.Exists(installAverageBpsJsonPath))
            {
                try
                {
                    InstallAverageBps = Serialization.FromJsonFile<Dictionary<string, long>>(installAverageBpsJsonPath);
                }
                catch
                {
                    plugin.PopupError("LoadInstallAverageBpsFromJson failed");
                }
            }
        }

        public void SaveCacheRootsToJson()
        {
            try
            {
                File.WriteAllText(cacheRootsJsonPath, Serialization.ToJson(gameCacheManager.GetCacheRoots()));
            }
            catch
            {
                plugin.PopupError("SaveCacheRootsToJson failed");
            }
        }

        public void LoadCacheRootsFromJson()
        {
            if (File.Exists(cacheRootsJsonPath))
            {
                var roots = new List<CacheRoot>();
                try
                {
                    roots = Serialization.FromJsonFile<List<CacheRoot>>(cacheRootsJsonPath);
                }
                catch
                {
                    plugin.PopupError("LoadCacheRootsFromJson failed");
                }
                
                foreach (var root in roots)
                {
                    if (DirectoryUtils.ExistsAndIsWritable(root.Directory) || DirectoryUtils.MakeDir(root.Directory))
                    {
                        gameCacheManager.AddCacheRoot(root);
                        CacheRoots.Add(new CacheRootViewModel(this, root));
                    }
                    else
                    {
                        plugin.NotifyError($"Removing '{root.Directory}' as a NowPlaying cache root; Path not found or not writable.");
                    }
                }
            }
            SaveCacheRootsToJson();
        }

        public void SaveGameCacheEntriesToJson()
        {
            try
            {
                File.WriteAllText(gameCacheEntriesJsonPath, Serialization.ToJson(gameCacheManager.GetGameCacheEntries()));
            }
            catch
            {
                plugin.PopupError("SaveGameCacheEntriesToJson failed");
            }
        }

        public void LoadGameCacheEntriesFromJson()
        {
            if (File.Exists(gameCacheEntriesJsonPath))
            {
                var entries = new List<GameCacheEntry>();
                try
                {
                    entries = Serialization.FromJsonFile<List<GameCacheEntry>>(gameCacheEntriesJsonPath);
                }
                catch
                {
                    plugin.PopupError("LoadGameCacheEntriesFromJson failed");
                }

                foreach (var entry in entries)
                {
                    if (plugin.FindNowPlayingGame(entry.Id) != null)
                    {
                        var cacheRoot = FindCacheRoot(entry.CacheRoot);
                        if (cacheRoot != null)
                        {
                            gameCacheManager.AddGameCacheEntry(entry);
                            GameCaches.Add(new GameCacheViewModel(this, entry, cacheRoot));
                        }
                        else
                        {
                            plugin.NotifyWarning($"Cache root '{entry.CacheRoot}' not found; disabling game caching for '{entry.Title}'.");
                            plugin.DisableNowPlayingGameCaching(plugin.FindNowPlayingGame(entry.Id), entry.InstallDir, entry.ExePath, entry.XtraArgs);
                        }
                    }
                    else
                    {
                        plugin.NotifyWarning($"NowPlaying enabled game not found; disabling game caching for '{entry.Title}'.");
                    }
                }
                plugin.panelViewModel.RefreshGameCaches();
            }
            SaveGameCacheEntriesToJson();

            // . notify cache roots of updates to their resective game caches
            foreach (var cacheRoot in CacheRoots)
            {
                cacheRoot.UpdateGameCaches();
            }
        }

        public (string cacheRootDir, string cacheSubDir) FindCacheRootAndSubDir(string installDirectory)
        {
            return gameCacheManager.FindCacheRootAndSubDir(installDirectory);
        }

        public bool GameCacheIsUninstalled(string cacheId)
        {
            return gameCacheManager.GameCacheExistsAndEmpty(cacheId);
        }

        public string ChangeGameCacheRoot(GameCacheViewModel gameCache, CacheRootViewModel newCacheRoot)
        {
            if (gameCache.IsUninstalled() && gameCache.cacheRoot != newCacheRoot)
            {
                var oldCacheRoot = gameCache.cacheRoot;
                gameCacheManager.ChangeGameCacheRoot(gameCache.Id, newCacheRoot.Directory);
                gameCache.cacheRoot = newCacheRoot;
                gameCache.UpdateCacheRoot();

                // . reflect removal of game from previous cache root
                oldCacheRoot.UpdateGameCaches();
            }
            return gameCache.CacheDir;
        }

        public bool IsGameCacheDirectory(string possibleCacheDir)
        {
            return gameCacheManager.IsGameCacheDirectory(possibleCacheDir);
        }

        public void Shutdown()
        {
            gameCacheManager.Shutdown();
        }

        public bool IsGameCacheInstalled(string cacheId)
        {
            return gameCacheManager.GameCacheExistsAndPopulated(cacheId);
        }

        private class InstallCallbacks
        {
            private readonly GameCacheManager manager;
            private readonly GameCacheViewModel gameCache;
            private readonly Action<GameCacheJob> InstallDone;
            private readonly Action<GameCacheJob> InstallCancelled;
            private bool cancelOnMaxFill;
            public InstallCallbacks
                (
                    GameCacheManager manager, 
                    GameCacheViewModel gameCache,
                    Action<GameCacheJob> installDone, 
                    Action<GameCacheJob> installCancelled
                )
            {
                this.manager = manager;
                this.gameCache = gameCache;
                this.InstallDone = installDone;
                this.InstallCancelled = installCancelled;
                this.cancelOnMaxFill = false;
            }

            public void Done(object sender, GameCacheJob job)
            {
                if (job.entry.Id == gameCache.Id)
                {
                    manager.eJobDone -= this.Done;
                    manager.eJobCancelled -= this.Cancelled;
                    manager.eJobStatsUpdated -= this.MaxFillLevelCanceller;

                    InstallDone(job);
                }
            }
            public void Cancelled(object sender, GameCacheJob job)
            {
                if (job.entry.Id == gameCache.Id)
                {
                    manager.eJobDone -= this.Done;
                    manager.eJobCancelled -= this.Cancelled;
                    manager.eJobStatsUpdated -= this.MaxFillLevelCanceller;

                    if (cancelOnMaxFill) 
                    { 
                        job.cancelledOnMaxFill = true; 
                    }
                    InstallCancelled(job);
                }
            }
            public void MaxFillLevelCanceller(object sender, string cacheId)
            {
                if (cacheId == gameCache.Id)
                {
                    // . automatically pause cache installation if max fill level exceeded
                    if (gameCache.cacheRoot.BytesAvailableForCaches <= 0)
                    {
                        cancelOnMaxFill = true;
                        manager.CancelPopulateOrResume(cacheId);
                    }
                }
            }
        }

        public void InstallGameCache
            (
                GameCacheViewModel gameCache, 
                RoboStats jobStats, 
                Action<GameCacheJob> installDone, 
                Action<GameCacheJob> installCancelled, 
                int interPacketGap = 0
            )
        {
            var callbacks = new InstallCallbacks(gameCacheManager, gameCache, installDone, installCancelled);
            gameCacheManager.eJobDone += callbacks.Done;
            gameCacheManager.eJobCancelled += callbacks.Cancelled;
            gameCacheManager.eJobStatsUpdated += callbacks.MaxFillLevelCanceller;

            gameCacheManager.StartPopulateGameCacheJob(gameCache.Id, jobStats, interPacketGap);
        }

        public void CancelInstall(string cacheId)
        {
            gameCacheManager.CancelPopulateOrResume(cacheId);
        }

        private class UninstallCallbacks
        {
            private readonly GameCacheManager manager;
            private readonly GameCacheViewModel gameCache;
            private readonly Action<GameCacheJob> UninstallDone;
            private readonly Action<GameCacheJob> UninstallCancelled;

            public UninstallCallbacks
                (
                    GameCacheManager manager, 
                    GameCacheViewModel gameCache, 
                    Action<GameCacheJob> uninstallDone, 
                    Action<GameCacheJob> uninstallCancelled
                )
            {
                this.manager = manager;
                this.gameCache = gameCache;
                this.UninstallDone = uninstallDone;
                this.UninstallCancelled = uninstallCancelled;
            }

            public void Done(object sender, GameCacheJob job)
            {
                if (job.entry.Id == gameCache.Id)
                {
                    manager.eJobDone -= this.Done;
                    manager.eJobCancelled -= this.Cancelled;
                    
                    UninstallDone(job);
                }
            }
            public void Cancelled(object sender, GameCacheJob job)
            {
                if (job.entry.Id == gameCache.Id)
                {
                    manager.eJobDone -= this.Done;
                    manager.eJobCancelled -= this.Cancelled;

                    UninstallCancelled(job);
                }
            }
        }

        public void UninstallGameCache
            (
                GameCacheViewModel gameCache, 
                bool cacheWriteBackOption, 
                Action<GameCacheJob> uninstallDone, 
                Action<GameCacheJob> uninstallCancelled
            )
        {
            var callbacks = new UninstallCallbacks(gameCacheManager, gameCache, uninstallDone, uninstallCancelled);
            gameCacheManager.eJobDone += callbacks.Done;
            gameCacheManager.eJobCancelled += callbacks.Cancelled;

            gameCacheManager.StartEvictGameCacheJob(gameCache.Id, cacheWriteBackOption);
        }

        public GameCacheManager.DirtyCheckResult CheckCacheDirty(string cacheId)
        {
            return gameCacheManager.CheckCacheDirty(cacheId);
        }

    }
}

using NowPlaying.Core;
using NowPlaying.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NowPlaying.ViewModels
{
    public class CacheRootViewModel : ObservableObject
    {
        public readonly GameCacheManagerViewModel manager;
        public CacheRoot root;
        public string Directory => root.Directory;
        public string Device => System.IO.Directory.GetDirectoryRoot(Directory);
        public double MaxFillLevel => root.MaxFillLevel;
        public string MaxFillReserved => $"{MaxFillLevel}%" + ReservedSpaceOnDevice;

        public ObservableCollection<GameCacheViewModel> GameCaches { get; private set; }
        public long GamesEnabled { get; private set; }
        public string CachesInstalled { get; private set; }
        public long BytesAvailableForCaches { get; private set; }
        public string SpaceAvailableForCaches { get; private set; }
        public string ReservedSpaceOnDevice { get; private set; }

        public CacheRootViewModel(GameCacheManagerViewModel manager, CacheRoot root)
        {
            this.manager = manager;
            this.root = root;

            UpdateGameCaches();
        }

        public void UpdateGameCaches()
        {
            GameCaches = new ObservableCollection<GameCacheViewModel>(manager.GameCaches.Where(gc => gc.Root == Directory));
            GamesEnabled = GameCaches.Count();

            OnPropertyChanged(nameof(GameCaches));
            OnPropertyChanged(nameof(GamesEnabled));

            UpdateCachesInstalled();
            UpdateSpaceAvailableForCaches();
        }

        public void UpdateCachesInstalled()
        {
            CachesInstalled = string.Format("{0}{1}",
                GameCaches.Where(gc => IsCacheInstalled(gc)).Count(),
                GetAggregateCacheSize(GameCaches.Where(gc => IsCacheNonEmpty(gc)).ToList())
            );
            OnPropertyChanged(nameof(CachesInstalled));
        }

        public static long GetReservedSpaceOnDevice(string rootDir, double maxFillLevel)
        {
            return (long)(DirectoryUtils.GetRootDeviceCapacity(rootDir) * (1.0 - maxFillLevel / 100.0));
        }

        public static long GetAvailableSpaceForCaches(string rootDir, double maxFillLevel)
        {
            return DirectoryUtils.GetAvailableFreeSpace(rootDir) - GetReservedSpaceOnDevice(rootDir, maxFillLevel);
        }

        public void UpdateSpaceAvailableForCaches()
        {
            BytesAvailableForCaches = GetAvailableSpaceForCaches(Directory, MaxFillLevel);
            OnPropertyChanged(nameof(BytesAvailableForCaches));

            foreach (var gc in GameCaches)
            {
                gc.UpdateCacheSpaceWillFit();
            }

            if (MaxFillLevel < 100)
            {
                ReservedSpaceOnDevice = "  (leave "+ SmartUnits.Bytes(GetReservedSpaceOnDevice(Directory, MaxFillLevel)) +" unused)"; 
            }
            else
            {
                ReservedSpaceOnDevice = "";
            }
            OnPropertyChanged(nameof(ReservedSpaceOnDevice));
            OnPropertyChanged(nameof(MaxFillReserved));

            SpaceAvailableForCaches = SmartUnits.Bytes(BytesAvailableForCaches);
            OnPropertyChanged(nameof(SpaceAvailableForCaches));
        }

        private string GetAggregateCacheSize(List<GameCacheViewModel> gameCaches)
        {
            var aggregateSize = gameCaches.Select(gc => gc.CacheSize).Sum(x => x);
            return aggregateSize > 0 ? "  (" + SmartUnits.Bytes(aggregateSize) + ")" : "";
        }

        private bool IsCacheInstalled(GameCacheViewModel gameCache)
        {
            return gameCache.State == GameCacheState.Populated || gameCache.State == GameCacheState.Played;
        }

        private bool IsCacheNonEmpty(GameCacheViewModel gameCache)
        {
            GameCacheState state = gameCache.State;
            return state == GameCacheState.InProgress || state == GameCacheState.Populated || state == GameCacheState.Played;
        }

        public void SetMaxFillLevel(double maximumFillLevel)
        {
            root.MaxFillLevel = maximumFillLevel;
            OnPropertyChanged(nameof(MaxFillLevel));
            UpdateSpaceAvailableForCaches();
        }
    }
}

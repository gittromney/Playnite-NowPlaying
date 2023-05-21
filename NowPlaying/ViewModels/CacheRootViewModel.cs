using NowPlaying.Utils;
using NowPlaying.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<GameCacheViewModel> GameCaches { get; private set; }
        public long GamesEnabled { get; private set; }
        public int CachesInstalled { get; private set; }
        public string CachesInstalledSize { get; private set; }
        public long BytesAvailableForCaches { get; private set; }
        public string SpaceAvailableForCaches { get; private set; }
        public string SpaceAvailableForCachesColor => BytesAvailableForCaches > 0 ? "TextBrush" : "WarningBrush";

        private long bytesReservedOnDevice;
        public string ReservedSpaceOnDevice { get; private set; }

        public CacheRootViewModel(GameCacheManagerViewModel manager, CacheRoot root)
        {
            this.manager = manager;
            this.root = root;

            SetMaxFillLevel(root.MaxFillLevel);
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
            int ival = GameCaches.Where(gc => IsCacheInstalled(gc)).Count();
            if (CachesInstalled != ival)
            {
                CachesInstalled = ival;
                OnPropertyChanged(nameof(CachesInstalled));
            }

            string sval = GetAggregateCacheSize(GameCaches.Where(gc => IsCacheNonEmpty(gc)).ToList());
            if (CachesInstalledSize != sval)
            {
                CachesInstalledSize = sval;
                OnPropertyChanged(nameof(CachesInstalledSize));
            }
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
            BytesAvailableForCaches = DirectoryUtils.GetAvailableFreeSpace(Directory) - bytesReservedOnDevice;
            OnPropertyChanged(nameof(BytesAvailableForCaches));
            if (GameCaches != null)
            {
                foreach (var gc in GameCaches)
                {
                    gc.UpdateCacheSpaceWillFit();
                }
            }
            SpaceAvailableForCaches = SmartUnits.Bytes(BytesAvailableForCaches);
            OnPropertyChanged(nameof(SpaceAvailableForCaches));
            OnPropertyChanged(nameof(SpaceAvailableForCachesColor));
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

            bytesReservedOnDevice = GetReservedSpaceOnDevice(Directory, MaxFillLevel);

            if (MaxFillLevel < 100)
            {
                ReservedSpaceOnDevice = "  (leave " + SmartUnits.Bytes(bytesReservedOnDevice) + " unused)";
            }
            else
            {
                ReservedSpaceOnDevice = "";
            }
            OnPropertyChanged(nameof(ReservedSpaceOnDevice));

            UpdateSpaceAvailableForCaches();
        }
    }
}

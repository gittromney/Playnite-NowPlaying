﻿using NowPlaying.Utils;
using NowPlaying.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using NowPlaying.Extensions;

namespace NowPlaying.ViewModels
{
    public class CacheRootViewModel : ColumnSortableObject
    {
        public readonly GameCacheManagerViewModel manager;
        public readonly NowPlaying plugin;
        public readonly ThemeResources theme;
        public CacheRoot root;
        public ThemeResources Theme => theme;
        
        public string Directory => root.Directory;
        public string Device => System.IO.Directory.GetDirectoryRoot(Directory);

        public ObservableCollection<GameCacheViewModel> GameCaches { get; private set; }
        public long GamesEnabled { get; private set; }
        public int CachesInstalled { get; private set; }

        public long cachesAggregateSizeOnDisk { get; private set; }
        public string CachesInstalledSize => cachesAggregateSizeOnDisk > 0 ? "(" + SmartUnits.Bytes(cachesAggregateSizeOnDisk) + ")" : "";
        public long BytesAvailableForCaches { get; private set; }
        public string SpaceAvailableForCaches { get; private set; }
        public Brush SpaceAvailableForCachesBrush => BytesAvailableForCaches > 0 ? Theme.TextBrush : Theme.WarningBrush;

        public double MaxFillLevel => root.MaxFillLevel;
        public string MaxFillLevelPercent => string.Format($"{MaxFillLevel:#,0.#}%");

        public long bytesReservedOnDevice;
        public string ReservedSpaceOnDevice { get; private set; }

        public CacheRootViewModel(GameCacheManagerViewModel manager, CacheRoot root)
        {
            this.manager = manager;
            this.plugin = manager.plugin;
            this.theme = plugin.themeResources;
            this.root = root;
            this.cachesAggregateSizeOnDisk = 0;

            SetMaxFillLevel(root.MaxFillLevel);
            UpdateGameCaches();
        }

        public void UpdateGameCaches()
        {
            GameCaches = new ObservableCollection<GameCacheViewModel>(manager.GameCaches.Where(gc => gc.Root == Directory));
            GamesEnabled = GameCaches.Count();

            OnPropertyChanged(nameof(GameCaches));
            OnPropertyChanged(nameof(GamesEnabled));
            OnSortableColumnChanged(Directory, "GamesEnabled");
            UpdateCachesInstalled();
            UpdateSpaceAvailableForCaches();
        }

        public void UpdateCachesInstalled()
        {
            bool sortableColumnChanged = false;
            int ival = GameCaches.Where(gc => IsCacheInstalled(gc)).Count();
            if (CachesInstalled != ival)
            {
                CachesInstalled = ival;
                OnPropertyChanged(nameof(CachesInstalled));
                sortableColumnChanged = true;
            }

            long lval = GetAggregateCacheSizeOnDisk(GameCaches.Where(gc => IsCacheNonEmpty(gc)).ToList());
            if (cachesAggregateSizeOnDisk != lval)
            {
                cachesAggregateSizeOnDisk = lval;
                OnPropertyChanged(nameof(cachesAggregateSizeOnDisk));
                OnPropertyChanged(nameof(CachesInstalledSize));
                sortableColumnChanged = true;
            }
            if (sortableColumnChanged)
            {
                OnSortableColumnChanged(Directory, "CachesInstalledAndSize");
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
            OnPropertyChanged(nameof(SpaceAvailableForCachesBrush));
            OnSortableColumnChanged(Directory, "SpaceAvailableForCaches");
        }

        private long GetAggregateCacheSizeOnDisk(List<GameCacheViewModel> gameCaches)
        {
            return gameCaches.Select(gc => gc.CacheSizeOnDisk).Sum(x => x);
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
            OnPropertyChanged(nameof(MaxFillLevelPercent));

            bytesReservedOnDevice = GetReservedSpaceOnDevice(Directory, MaxFillLevel);

            if (MaxFillLevel < 100)
            {
                ReservedSpaceOnDevice = plugin.FormatResourceString("LOCNowPlayingCacheRootReservedSpaceFmt", SmartUnits.Bytes(bytesReservedOnDevice));
            }
            else
            {
                ReservedSpaceOnDevice = "";
            }
            OnPropertyChanged(nameof(ReservedSpaceOnDevice));
            OnSortableColumnChanged(Directory, "MaxFillAndReserved");
            UpdateSpaceAvailableForCaches();
        }
    }
}

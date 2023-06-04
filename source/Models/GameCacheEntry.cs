using NowPlaying.Utils;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace NowPlaying.Models
{
    public enum GameCacheState { Empty, InProgress, Populated, Played, Unknown, Invalid };

    /// <summary>
    /// Game Cache specification entry class
    /// </summary>
    [Serializable]
    public class GameCacheEntry : ISerializable
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string InstallDir { get; private set; }
        public string ExePath { get; private set; }
        public string XtraArgs { get; private set; }

        private string cacheRoot;
        private string cacheSubDir;
        private string cacheDir;

        public string CacheRoot
        {
            get => cacheRoot; 
            set
            {
                cacheRoot = value;
                if (cacheRoot != null)
                {
                    // . When cacheSubDir==null, use file-safe game title as the sub dir name 
                    cacheDir = Path.Combine(cacheRoot, CacheSubDir ?? DirectoryUtils.ToSafeFileName(Title));
                }
                else
                {
                    cacheDir = null;
                }
            }
        }
        public string CacheSubDir 
        {
            get => cacheSubDir; 
            set
            {
                cacheSubDir = value;
                if (cacheRoot != null)
                {
                    // . When cacheSubDir==null, use file-safe game title as the sub dir name 
                    cacheDir = Path.Combine(cacheRoot, cacheSubDir ?? DirectoryUtils.ToSafeFileName(Title));
                }
                else
                {
                    cacheDir = null;
                }
            }
        }
        public string CacheDir => cacheDir;

        public GameCacheState State { get; set; }

        private long installFiles;
        private long installSize;
        private long cacheSizeOnDisk;
        
        public long InstallFiles => installFiles;
        public long InstallSize => installSize;
        public long CacheSize { get; set; }
        public long CacheSizeOnDisk
        {
            get => cacheSizeOnDisk;
            set { cacheSizeOnDisk = value; }
        }

        /// <summary>
        /// Constructs a new Game Cache specification entry and reads the installation folder to
        /// get cacheInstalledSize statistics. System.IO will throw exceptions for cases such as 
        /// folder doesn't exist, bitlocker drive is locked, etc.
        /// </summary>
        /// <param name="id">Distinct game Id</param>
        /// <param name="title">Game title or name</param>
        /// <param name="installDir">Directory where source game is installed</param>
        /// <param name="cacheRoot">RootDir directory where Game Cache is to be created</param>
        /// <param name="cacheSubDir">(Optional) Unique sub directory under root where Game Cache is to be created</param>
        /// <param name="installFiles">(Optional) Source game installation file count</param>
        /// <param name="installSize">(Optional) Source game installation cacheInstalledSize</param>
        /// <param name="cacheSize">(Optional) Current game cache cacheInstalledSize</param>
        /// <param name="state">(Optional) Game cache entry state.</param>
        public GameCacheEntry(
            string id,
            string title, 
            string installDir,
            string exePath,
            string xtraArgs,
            string cacheRoot,
            string cacheSubDir = null,
            long installFiles = 0,
            long installSize = 0,
            long cacheSize = 0,
            GameCacheState state = GameCacheState.Unknown)
        {
            this.Id = id;
            this.Title = title;
            this.InstallDir = installDir;
            this.ExePath = exePath;
            this.XtraArgs = xtraArgs;
            this.CacheRoot = cacheRoot;
            this.CacheSubDir = cacheSubDir;
            this.installFiles = installFiles;
            this.installSize = installSize;
            this.CacheSize = cacheSize;
            this.State = state;
            if (CacheDir != null) GetQuickCacheDirState();
        }

        // Serialization constructor
        public GameCacheEntry(SerializationInfo info, StreamingContext context)
        {
            this.Id = (string)info.GetValue(nameof(Id), typeof(string));
            this.Title = (string)info.GetValue(nameof(Title), typeof(string));
            this.InstallDir = (string)info.GetValue(nameof(InstallDir), typeof(string));
            this.ExePath = (string)info.GetValue(nameof(ExePath), typeof(string));
            this.XtraArgs = (string)info.GetValue(nameof(XtraArgs), typeof(string));
            this.CacheRoot = (string)info.GetValue(nameof(CacheRoot), typeof(string));
            this.CacheSubDir = (string)info.GetValue(nameof(CacheSubDir), typeof(string));
            this.installFiles = (long)info.GetValue(nameof(InstallFiles), typeof(long));
            this.installSize = (long)info.GetValue(nameof(InstallSize), typeof(long));
            this.CacheSize = (long)info.GetValue(nameof(CacheSize), typeof(long));
            this.State = (GameCacheState)info.GetValue(nameof(State), typeof(GameCacheState));
            if (CacheDir != null) GetQuickCacheDirState();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Title), Title);
            info.AddValue(nameof(InstallDir), InstallDir);
            info.AddValue(nameof(ExePath), ExePath);
            info.AddValue(nameof(XtraArgs), XtraArgs);
            info.AddValue(nameof(CacheRoot), CacheRoot);
            info.AddValue(nameof(CacheSubDir), CacheSubDir);
            info.AddValue(nameof(InstallFiles), InstallFiles);
            info.AddValue(nameof(InstallSize), InstallSize);
            info.AddValue(nameof(CacheSize), CacheSize);
            info.AddValue(nameof(State), State);
        }

        public void UpdateInstallDirStats()
        {
            long installDirs = 0;
            installFiles = 0;
            installSize = 0;

            // . get install folder stats
            try
            {
                DirectoryUtils.GetDirectoryStats(InstallDir, 
                    ref installDirs, 
                    ref installFiles, 
                    ref installSize
                ); 
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public void UpdateCacheDirStats()
        {
            long cacheDirs = 0;
            long cacheFiles = 0;
            cacheSizeOnDisk = 0;

            if (Directory.Exists(CacheDir))
            {
                // . get cache folder stats..
                try
                {
                    DirectoryUtils.GetDirectoryStats(CacheDir,
                        ref cacheDirs,
                        ref cacheFiles,
                        ref cacheSizeOnDisk
                    );
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(ex.Message);
                }
            }
        }

        public void GetQuickCacheDirState()
        {
            // . Check validity of chosen cache folder. Valid cases:
            //   -> empty folder
            //   -> non existant folder (make sure that it is also "createable")
            //   -> non empty folder containing a ".NowPlaying.<folder state>" marker file
            //
            if (!Directory.Exists(CacheDir))
            {
                try
                {
                    // . make sure folder is createable...
                    Directory.CreateDirectory(CacheDir);
                    Directory.Delete(CacheDir);
                    State = GameCacheState.Empty;
                    CacheSize = 0;
                    CacheSizeOnDisk = 0;
                }
                catch
                {
                    State = GameCacheState.Invalid;
                    CacheSize = 0;
                    CacheSizeOnDisk = 0;
                    throw new InvalidOperationException($"Cannot create cache folder '{CacheDir}'");
                }
            }
            else if (DirectoryUtils.IsEmptyDirectory(CacheDir))
            {
                State = GameCacheState.Empty;
                CacheSize = 0;
                CacheSizeOnDisk = 0;
            }
            else
            {
                // . get cache folder state
                if (File.Exists(CacheDir + $@"\.NowPlaying.Played"))
                {
                    State = GameCacheState.Played;
                }
                else if (File.Exists(CacheDir + $@"\.NowPlaying.Populated"))
                {
                    State = GameCacheState.Populated;
                }
                else if (File.Exists(CacheDir + $@"\.NowPlaying.InProgress"))
                {
                    State = GameCacheState.InProgress;
                    UpdateCacheDirStats();
                }
                else
                {
                    // wait to have RoboCacher analyze the cache folder contents 
                    State = GameCacheState.Unknown;
                }
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Id:{0} Title:{1} Install:{2} Exe:{3} Xtra:{4} Cache:{5} IFiles:{6} ISize:{7} CSize:{8} State:{9}",
                Id, Title, InstallDir, ExePath, XtraArgs, CacheDir, InstallFiles, 
                SmartUnits.Bytes(InstallSize, decimals:1), 
                SmartUnits.Bytes(CacheSize, decimals:1), 
                State
            );
        }

    }
}

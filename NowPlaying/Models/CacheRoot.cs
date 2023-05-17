using NowPlaying.Utils;

namespace NowPlaying.Models
{
    public class CacheRoot
    {
        public string Directory { get; private set; }
        public double MaxFillLevel { get; set; }

        public CacheRoot(string directory, double maxFillLevel)
        {
            this.Directory = DirectoryUtils.TrimEndingSlash(directory);
            this.MaxFillLevel = maxFillLevel;
        }
    }
}

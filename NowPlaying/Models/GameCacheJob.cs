using System.Collections.Generic;
using System.Threading;
using System.Windows.Documents;

namespace NowPlaying.Models
{
    public class GameCacheJob
    {
        public readonly GameCacheEntry entry;
        public readonly RoboStats stats;
        public readonly CancellationTokenSource tokenSource;
        public readonly CancellationToken token;
        public bool cancelledOnDiskFull;
        public bool cancelledOnMaxFill;
        public bool cancelledOnError;
        public List<string> errorLog;

        public GameCacheJob(GameCacheEntry entry, RoboStats stats = null)
        {
            this.entry = entry;
            this.tokenSource = new CancellationTokenSource();
            this.token = tokenSource.Token;
            this.stats = stats;
            this.cancelledOnDiskFull = false;
            this.cancelledOnMaxFill = false;
            this.cancelledOnError = false;
            this.errorLog = null;
            stats?.Reset(entry.InstallFiles, entry.InstallSize);
        }

        public override string ToString()
        {
            return $"{entry} {stats} OnDiskFull:{cancelledOnDiskFull} OnError:{cancelledOnError} ErrorLog:{errorLog}";
        }
    }
}

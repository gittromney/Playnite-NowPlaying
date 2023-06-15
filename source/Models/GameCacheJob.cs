using NowPlaying.Utils;
using System.Collections.Generic;
using System.Threading;

namespace NowPlaying.Models
{
    public class GameCacheJob
    {
        public readonly GameCacheEntry entry;
        public readonly RoboStats stats;
        public readonly CancellationTokenSource tokenSource;
        public readonly CancellationToken token;
        public PartialFileResumeOpts pfrOpts;
        public long? partialFileResumeThresh;
        public int interPacketGap;
        public bool cancelledOnDiskFull;
        public bool cancelledOnMaxFill;
        public bool cancelledOnError;
        public List<string> errorLog;

        public GameCacheJob(GameCacheEntry entry, RoboStats stats=null, int interPacketGap = 0, PartialFileResumeOpts pfrOpts = null)
        {
            this.entry = entry;
            this.tokenSource = new CancellationTokenSource();
            this.token = tokenSource.Token;
            this.stats = stats;
            this.pfrOpts = pfrOpts;
            this.interPacketGap = interPacketGap;
            this.cancelledOnDiskFull = false;
            this.cancelledOnMaxFill = false;
            this.cancelledOnError = false;
            this.errorLog = null;

            bool partialFileResume = pfrOpts?.Mode == EnDisThresh.Enabled || pfrOpts?.Mode == EnDisThresh.Threshold && pfrOpts?.FileSizeThreshold <= 0;
            stats?.Reset(entry.InstallFiles, entry.InstallSize, partialFileResume);
        }

        public override string ToString()
        {
            return $"{entry} {stats} Ipg:{interPacketGap} PfrOpts:[{pfrOpts}] OnDiskFull:{cancelledOnDiskFull} OnError:{cancelledOnError} ErrorLog:{errorLog}";
        }
    }
}

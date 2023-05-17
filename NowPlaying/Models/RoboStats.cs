using NowPlaying.Utils;
using System;

namespace NowPlaying.Models
{
    public class RoboStats
    {
        // Overall Job stats
        public DateTime StartTime { get; private set; }
        public long FilesToCopy { get; private set;  }
        public long BytesToCopy { get; private set; }
        public long FilesCopied { get; set; }
        public long BytesCopied { get; set; }
        public long ResumeBytes { get; set; }
        public double PercentDone { get; set; }

        // Current File stats
        public string CurrFileName { get; set; }
        public long CurrFileSize { get; set; }
        public double CurrFilePct { get; set; }

        public void Reset(long filesToCopy, long bytesToCopy)
        {
            this.StartTime = DateTime.Now;
            this.FilesToCopy = filesToCopy;
            this.BytesToCopy = bytesToCopy;
            this.FilesCopied = 0;
            this.BytesCopied = 0;
            this.ResumeBytes = 0;
            this.PercentDone = 0.0;
            this.CurrFileName = string.Empty;
            this.CurrFileSize = 0;
            this.CurrFilePct = 0.0;
        }

        public RoboStats(long filesToCopy=0, long bytesToCopy=0)
        {
            this.Reset(filesToCopy, bytesToCopy);
        }

        public long GetTotalBytesCopied()
        {
            return BytesCopied + (long)(CurrFileSize * (CurrFilePct / 100.0));
        }

        public double UpdatePercentDone()
        {
            if (BytesToCopy > 0)
            {
                PercentDone = (100.0 * GetTotalBytesCopied()) / BytesToCopy;
            }
            else
            {
                PercentDone = 0.0;
            }
            return PercentDone;
        }

        public TimeSpan GetDuration()
        {
            return DateTime.Now - StartTime;
        }

        public long GetAvgBytesPerSecond()
        {
            double duration = GetDuration().TotalMilliseconds;
            if (duration > 0)
            {
                return (long) ((1000.0 * (GetTotalBytesCopied() - ResumeBytes)) / duration);
            }
            else
            {
                return 0;
            }
        }

        public TimeSpan GetTimeRemaining(double avgBPS)
        {
            long bytesRemaining = BytesToCopy - GetTotalBytesCopied();
            if (avgBPS > 0)
            {
                return TimeSpan.FromSeconds((1.0 * bytesRemaining) / avgBPS);
            }
            else
            {
                return TimeSpan.FromMilliseconds(-1); // overload: -1ms => infinite duration
            }
        }

        public override string ToString()
        {
            // . use smart units, in the format "<bytes copied> of <bytes to copy> <units>" 
            int btcScale = SmartUnits.GetBytesAutoScale(BytesToCopy);
            string btc = SmartUnits.Bytes(BytesToCopy, userScale:btcScale);
            string bcp = SmartUnits.Bytes(BytesCopied, userScale:btcScale, showUnits:false);
            
            return string.Format(
                "{0} of {1} files, {2} of {3}, {4}% done", 
                FilesCopied, FilesToCopy, bcp, btc, PercentDone
            );
        }
    }
}

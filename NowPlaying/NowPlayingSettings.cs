using System.Collections.Generic;

namespace NowPlaying
{
    public enum DoWhen { Always, Ask, Never };
    public enum WhilePlaying { Pause, SpeedLimit, Normal };

    public class NowPlayingSettings : ObservableObject
    {
        private DoWhen syncDirtyCache_DoWhen;
        public DoWhen SyncDirtyCache_DoWhen 
        {
            get => syncDirtyCache_DoWhen;
            set
            {
                if (syncDirtyCache_DoWhen != value)
                {
                    syncDirtyCache_DoWhen = value;
                    OnPropertyChanged();
                }
            }
        }

        private WhilePlaying whilePlayingMode;
        public WhilePlaying WhilePlayingMode
        {
            get => whilePlayingMode;
            set
            {
                if (whilePlayingMode != value)
                {
                    whilePlayingMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private int speedLimitIPG;
        public int SpeedLimitIPG 
        {
            get => speedLimitIPG;
            set
            {
                if (value < 1) value = 1;
                if (value > 250) value = 250;
                if (speedLimitIPG != value)
                {
                    speedLimitIPG = value;
                    OnPropertyChanged();
                }
            } 
        }

        public bool ConfirmUninstall { get; set; }

        public NowPlayingSettings()
        {
            this.ConfirmUninstall = false;
            this.SyncDirtyCache_DoWhen = DoWhen.Always;
            this.WhilePlayingMode = WhilePlaying.SpeedLimit;
            this.SpeedLimitIPG = 75;
        }
    }
}
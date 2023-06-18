﻿using System.Collections.Generic;

namespace NowPlaying
{
    public enum DoWhen { Always, Ask, Never };
    public enum WhilePlaying { Pause, SpeedLimit, Normal };
    public enum EnDisThresh { Enabled, Disabled, Threshold };

    public class NowPlayingSettings : ObservableObject
    {
        public bool ConfirmUninstall { get; set; }
        public bool NotifyOnInstallWhilePlayingActivity { get; set; }

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

        private int speedLimitIpg;
        public int SpeedLimitIpg 
        {
            get => speedLimitIpg;
            set
            {
                if (value < 1) value = 1;
                if (value > 250) value = 250;
                if (speedLimitIpg != value)
                {
                    speedLimitIpg = value;
                    OnPropertyChanged();
                }
            } 
        }

        private EnDisThresh partialFileResume;
        public EnDisThresh PartialFileResume
        {
            get => partialFileResume;
            set
            {
                if (partialFileResume != value)
                {
                    partialFileResume = value;
                    OnPropertyChanged();
                }
            }
        }

        private double pfrThresholdGigaBytes;
        public double PfrThresholdGigaBytes
        {
            get => pfrThresholdGigaBytes;
            set
            {
                if (value < 0.0) value = 0.0;
                if (pfrThresholdGigaBytes != value)
                {
                    pfrThresholdGigaBytes = value;
                    OnPropertyChanged();
                }
            }
        }

        private double defaultAvgMegaBpsNormal;
        public double DefaultAvgMegaBpsNormal
        {
            get => defaultAvgMegaBpsNormal;
            set
            {
                if (value < 0.1) value = 0.1;
                if (value > 1024) value = 1024;
                if (defaultAvgMegaBpsNormal != value)
                {
                    defaultAvgMegaBpsNormal = value;
                    OnPropertyChanged();
                }
            }
        }
        private double defaultAvgMegaBpsSpeedLimited;
        public double DefaultAvgMegaBpsSpeedLimited 
        { 
            get => defaultAvgMegaBpsSpeedLimited;
            set
            {
                if (value < 0.1) value = 0.1;
                if (value > 1024) value = 1024;
                if (defaultAvgMegaBpsSpeedLimited != value)
                {
                    defaultAvgMegaBpsSpeedLimited = value;
                    OnPropertyChanged();
                }
            }
        }

        public NowPlayingSettings()
        {
            this.ConfirmUninstall = true;
            this.NotifyOnInstallWhilePlayingActivity = false;

            this.SyncDirtyCache_DoWhen = DoWhen.Always;
            this.WhilePlayingMode = WhilePlaying.SpeedLimit;
            this.SpeedLimitIpg = 75;
            this.PartialFileResume = EnDisThresh.Threshold;
            this.PfrThresholdGigaBytes = 0.1;

            this.DefaultAvgMegaBpsNormal = 50.0;
            this.DefaultAvgMegaBpsSpeedLimited = 5.0;
        }
    }
}
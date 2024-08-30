using System.Collections.Generic;

namespace NowPlaying
{
    public enum DoWhen { Always, Ask, Never };
    public enum WhilePlaying { Pause, SpeedLimit, Normal };
    public enum EnDisThresh { Enabled, Disabled, Threshold };

    public class NowPlayingSettings : ObservableObject
    {
        public bool ConfirmUninstall { get; set; }
        public bool NotifyOnInstallWhilePlayingActivity { get; set; }

        public bool ShowRootDevice { get; set; }
        public bool ShowRootDirectory { get; set; }
        public bool ShowRootSpaceAvail { get; set; }
        public bool ShowRootGames { get; set; }
        public bool ShowRootInstalled { get; set; }
        public bool ShowRootMaxFill { get; set; }

        public bool ShowGameCacheTitle { get; set; }
        public bool ShowGameCachePlatform { get; set; }
        public bool ShowGameCacheSourceDir { get; set; }
        public bool ShowGameCacheStatus { get; set; }
        public bool ShowGameCacheCanInstall { get; set; }
        public bool ShowGameCacheInstallEta { get; set; }
        public bool ShowGameCacheSize { get; set; }
        public bool ShowGameCacheRoot { get; set; }
        public bool ShowGameCacheSpaceAvail { get; set; }

        public bool ShowUninstallAndDisableMenu { get; set; }
        public bool ShowStatusAndMenuTips { get; set; }


        private bool statusIconBrushDarker;
        public bool StatusIconBrushDarker 
        { 
            get => statusIconBrushDarker;
            set
            {
                if (statusIconBrushDarker != value)
                {
                    statusIconBrushDarker = value;
                    OnPropertyChanged();
                }
            }
        }


        private bool sidebarIconBrushDarker;
        public bool SidebarIconBrushDarker
        {
            get => sidebarIconBrushDarker;
            set
            {
                if (sidebarIconBrushDarker != value)
                {
                    sidebarIconBrushDarker = value;
                    OnPropertyChanged();
                }
            }
        }


        private DoWhen changeProblematicInstallDir_DoWhen;
        public DoWhen ChangeProblematicInstallDir_DoWhen
        {
            get => changeProblematicInstallDir_DoWhen;
            set
            {
                if (changeProblematicInstallDir_DoWhen != value)
                {
                    changeProblematicInstallDir_DoWhen = value;
                    OnPropertyChanged();
                }
            }
        }

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


        public string[] ProblematicInstallDirKeywords = { "bin", "binaries", "bin32", "bin64", "x64", "x86", "win64", "win32", "sources", "nodvd", "retail" };
        public string[] ProblematicPS3InstDirKeywords = { "ps3_game", "usrdir" };

        public NowPlayingSettings()
        {
            this.ConfirmUninstall = true;
            this.NotifyOnInstallWhilePlayingActivity = false;

            this.ShowRootDevice = true;
            this.ShowRootDirectory = true;
            this.ShowRootSpaceAvail = true;
            this.ShowRootGames = true;
            this.ShowRootInstalled = true;
            this.ShowRootMaxFill = true;

            this.ShowGameCacheTitle = true;
            this.ShowGameCachePlatform = true;
            this.ShowGameCacheSourceDir = true;
            this.ShowGameCacheStatus = true;
            this.ShowGameCacheCanInstall = true;
            this.ShowGameCacheInstallEta = true;
            this.ShowGameCacheSize = true;
            this.ShowGameCacheRoot = true;
            this.ShowGameCacheSpaceAvail = true;

            this.ChangeProblematicInstallDir_DoWhen = DoWhen.Ask;
            
            this.SyncDirtyCache_DoWhen = DoWhen.Always;
            
            this.WhilePlayingMode = WhilePlaying.SpeedLimit;
            this.SpeedLimitIpg = 75;
            
            this.PartialFileResume = EnDisThresh.Threshold;
            this.PfrThresholdGigaBytes = 0.1;

            this.DefaultAvgMegaBpsNormal = 50.0;
            this.DefaultAvgMegaBpsSpeedLimited = 5.0;

            this.ShowUninstallAndDisableMenu = false;
            this.ShowStatusAndMenuTips = false;
            this.StatusIconBrushDarker = false;
            this.SidebarIconBrushDarker = false;
        }
    }
}
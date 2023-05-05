using System.Collections.Generic;

namespace NowPlaying
{
    public class NowPlayingSettings : ObservableObject
    {
        public enum HideOption { Enabled, Installed, Never };
        public HideOption HideSourceGameOption { get; set; }
        public string NowPlayingTitleTag { get; set; }
        public bool ConfirmUninstall { get; set; }
        public bool AskDirtyWriteBack { get; set; }

        public bool HideSource_Enabled
        {
            get => HideSourceGameOption == HideOption.Enabled;
            set 
            {
                if (value)
                {
                    HideSourceGameOption = HideOption.Enabled;
                }
            }
        }
        public bool HideSource_Installed
        {
            get => HideSourceGameOption == HideOption.Installed;
            set 
            {
                if (value)
                {
                    HideSourceGameOption = HideOption.Installed;
                }
            }
        }
        public bool HideSource_Never
        {
            get => HideSourceGameOption == HideOption.Never;
            set 
            {
                if (value)
                {
                    HideSourceGameOption = HideOption.Never;
                }
            }
        }

        public NowPlayingSettings()
        {
            this.HideSourceGameOption = HideOption.Enabled;
            this.NowPlayingTitleTag = " (NowPlaying)";
            this.ConfirmUninstall = false;
            this.AskDirtyWriteBack = true;
        }
    }
}
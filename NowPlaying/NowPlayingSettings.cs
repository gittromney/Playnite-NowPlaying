using System.Collections.Generic;

namespace NowPlaying
{
    public class NowPlayingSettings : ObservableObject
    {
        public enum DoWhen { Always, Ask, Never };
        public DoWhen SyncDirtyCache_DoWhen { get; set; }
        public DoWhen WhilePlayingInstall_DoWhen { get; set; }
        public bool ConfirmUninstall { get; set; }

        public NowPlayingSettings()
        {
            this.ConfirmUninstall = false;
            this.SyncDirtyCache_DoWhen = DoWhen.Always;
            this.WhilePlayingInstall_DoWhen = DoWhen.Always;
        }
    }
}
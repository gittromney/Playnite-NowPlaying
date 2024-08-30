using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace NowPlaying.ViewModels
{
    public class NowPlayingSettingsViewModel : ObservableObject, ISettings
    {
        private readonly NowPlaying plugin;

        // . editable settings values.
        private NowPlayingSettings settings;
        public NowPlayingSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged(null);
            }
        }

        public NowPlayingSettingsViewModel(NowPlaying plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;
            this.Settings = Serialization.GetClone(plugin.Settings);
        }

        public void BeginEdit()
        {
            // Code executed when settingsViewModel view is opened and user starts editing values.
            Settings = Serialization.GetClone(plugin.Settings);
            OnPropertyChanged(nameof(Settings));
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            Settings = Serialization.GetClone(plugin.Settings);
            OnPropertyChanged(nameof(Settings));
            plugin.settingsViewModel.OnPropertyChanged(nameof(Settings));
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            plugin.SavePluginSettings(Settings);
            OnPropertyChanged(nameof(Settings));
            plugin.UpdateSettings(Serialization.GetClone(Settings));
            plugin.settingsViewModel.OnPropertyChanged(nameof(Settings));
            plugin.ColorizeStatusIcon();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }

    }
}

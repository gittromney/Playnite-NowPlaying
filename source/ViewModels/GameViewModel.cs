using NowPlaying.Models;
using NowPlaying.Utils;
using Playnite.SDK.Models;
using System.Linq;

namespace NowPlaying.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        public readonly Game game;

        public string Title => game.Name;
        public string InstallDir => game.InstallDirectory;
        public string InstallSize => SmartUnits.Bytes((long)(game.InstallSize ?? 0));
        public long InstallSizeBytes => (long)(game.InstallSize ?? 0);
        public string Genres => game.Genres != null ? string.Join(", ", game.Genres.Select(x => x.Name)) : "";
        public GameCachePlatform Platform { get; private set; }

        public GameViewModel(Game game)
        {
            this.game = game;
            this.Platform = NowPlaying.GetGameCachePlatform(game);
        }
    }
}

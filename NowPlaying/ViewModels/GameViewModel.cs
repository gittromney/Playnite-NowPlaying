using NowPlaying.Utils;
using Playnite.SDK.Models;
using System.Linq;

namespace NowPlaying.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        public readonly Game game;

        public string Title => game.Name;
        public string InstallSize => SmartUnits.Bytes((long)(game.InstallSize ?? 0));
        public long InstallSizeBytes => (long)(game.InstallSize ?? 0);
        public string Genres => game.Genres != null ? string.Join(", ", game.Genres.Select(x => x.Name)) : "";
        public GameViewModel(Game game)
        {
            this.game = game;
        }
    }
}

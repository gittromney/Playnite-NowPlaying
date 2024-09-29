using System;
using System.Threading;
using System.Windows.Threading;

namespace NowPlaying.Utils
{
    public static class DispatcherUtils
    {
        public static void Invoke(Dispatcher dispatcher, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (dispatcher?.CheckAccess() == true)
            {
                action.Invoke();
            }
            else
            {
                dispatcher?.Invoke(priority, new ThreadStart(() => action.Invoke()));
            }
        }
    }
}

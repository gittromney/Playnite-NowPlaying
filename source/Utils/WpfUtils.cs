using System.Windows;
using System.Windows.Media;

namespace NowPlaying.Utils
{
    public static class WpfUtils
    {
        /// <summary>
        /// Gets the window left.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        public static double GetWindowLeft(this Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                var leftField = typeof(Window).GetField("_actualLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (double)leftField.GetValue(window);
            }
            else
                return window.Left;
        }

        /// <summary>
        /// Gets the window top.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        public static double GetWindowTop(this Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                var topField = typeof(Window).GetField("_actualTop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (double)topField.GetValue(window);
            }
            else
                return window.Top;
        }

        /// <summary>
        /// Gets the window width.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        public static double GetWindowWidth(this Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                return window.ActualWidth;
            }
            else
                return window.Width;
        }

        /// <summary>
        /// Gets the window width.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        public static double GetWindowHeight(this Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                return window.ActualHeight;
            }
            else
                return window.Height;
        }

        public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public static DependencyObject GetChildByName(this DependencyObject depObj, string targetName, DependencyObject skipThisObj = null)
        {
            if (depObj == null || targetName == null || depObj == skipThisObj) return null;

            var childrenCount = VisualTreeHelper.GetChildrenCount(depObj);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child != skipThisObj)
                {
                    var name = (child is FrameworkElement) ? ((FrameworkElement)child).Name : null;
                    if (name != targetName)
                    {
                        child = GetChildByName(child, targetName);
                    }
                    if (child != null) return child;
                }
            }
            return null;
        }

        public static T GetAncestor<T>(DependencyObject reference) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(reference);
            while (!(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent != null)
                return (T)parent;
            else
                return null;
        }

        public static DependencyObject GetRootAncestor(DependencyObject reference)
        { 
            var root = reference;
            var parent = VisualTreeHelper.GetParent(reference);
            while (parent != null)
            {
                root = parent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return root;
        }
    }
}

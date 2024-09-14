using System;
using System.Windows;
using System.Windows.Controls;
using Playnite.DesktopApp.Controls;

namespace NowPlaying.Controls
{
    // version of Theme-styled TopPanelItem control for general plugin use
    public class PluginTopPanelItem : TopPanelItem
    {
        static PluginTopPanelItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PluginTopPanelItem), new FrameworkPropertyMetadata(typeof(PluginTopPanelItem)));
        }

        public override void OnApplyTemplate()
        {
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            // HACK: skip over TopPanelItem's OnApplyTemplate(), which creates bindings to TopPanelWrapperItem 
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            var ptr = typeof(Button).GetMethod("OnApplyTemplate").MethodHandle.GetFunctionPointer();
            var ButtonOnApplyTemplate = (Action)Activator.CreateInstance(typeof(Action), this, ptr);
            ButtonOnApplyTemplate();
        }
    }
}

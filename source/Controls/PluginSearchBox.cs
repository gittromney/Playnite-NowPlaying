using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Playnite.DesktopApp.Controls;

namespace NowPlaying.Controls
{
    /// <summary>
    /// version of Playnite's SearchBox control for general plugin use.
    /// </summary>
    [ContentProperty(name: "Text")]
    public class PluginSearchBox : SearchBox
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var TextInputText = Template.FindName("PART_TextInpuText", this) as TextBox;
            if (TextInputText != null)
            {
                // . replace SearchBox's binding of Text property (which is broken/has wrong OnWay direction)
                BindingOperations.ClearBinding(TextInputText, TextBox.TextProperty);
                BindingOperations.SetBinding
                (
                    TextInputText,
                    TextBox.TextProperty,
                    new Binding()
                    {
                        Source = this,
                        Path = new PropertyPath(nameof(Text)),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        NotifyOnTargetUpdated = true,
                        NotifyOnSourceUpdated = true
                    }
                );
            }
        }
    }

}
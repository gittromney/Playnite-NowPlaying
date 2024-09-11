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
    [ContentProperty(name: "SearchText")]
    public class PluginSearchBox : SearchBox
    {
        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SearchText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(PluginSearchBox), new PropertyMetadata(string.Empty));

        static PluginSearchBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PluginSearchBox), new FrameworkPropertyMetadata(typeof(PluginSearchBox)));
        }

        public PluginSearchBox() {}

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var TextInputText = Template.FindName("PART_TextInpuText", this) as TextBox;
            if (TextInputText != null)
            {
                BindingOperations.SetBinding
                (
                    TextInputText,
                    TextBox.TextProperty,
                    new Binding()
                    {
                        Source = this,
                        Path = new PropertyPath(nameof(SearchText)),
                        Mode = BindingMode.OneWayToSource,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        NotifyOnTargetUpdated = true
                    }
                );
            }
        }
    }

}
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Media.Effects;
using Brush = System.Windows.Media.Brush;
using System.Windows.Media;

namespace NowPlaying.ViewModels
{
    public class ThemeResources : ViewModelBase
    {
        private readonly UserControl resourceView;

        #region brush resources


        private Brush textBrush;
        public Brush TextBrush
        {
            get => textBrush;
            set
            {
                if (textBrush != value)
                {
                    textBrush = value;
                    OnPropertyChanged(nameof(TextBrush));
                }
            }
        }

        private Brush textBrushDarker;
        public Brush TextBrushDarker
        {
            get => textBrushDarker;
            set
            {
                if (textBrushDarker != value)
                {
                    textBrushDarker = value;
                    OnPropertyChanged(nameof(TextBrushDarker));
                }
            }
        }

        private Brush glyphBrush;
        public Brush GlyphBrush
        {
            get => glyphBrush;
            set
            {
                if (glyphBrush != value)
                {
                    glyphBrush = value;
                    OnPropertyChanged(nameof(GlyphBrush));
                }
            }
        }

        private Brush warningBrush;
        public Brush WarningBrush
        {
            get => warningBrush;
            set
            {
                if (warningBrush != value)
                {
                    warningBrush = value;
                    OnPropertyChanged(nameof(WarningBrush));
                }
            }
        }

        private Brush installBrush;
        public Brush InstallBrush
        {
            get => installBrush;
            set
            {
                if (installBrush != value)
                {
                    installBrush = value;
                    OnPropertyChanged(nameof(InstallBrush));
                }
            }
        }

        private Brush slowInstallBrush;
        public Brush SlowInstallBrush
        {
            get => slowInstallBrush;
            set
            {
                if (slowInstallBrush != value)
                {
                    slowInstallBrush = value;
                    OnPropertyChanged(nameof(SlowInstallBrush));
                }
            }
        }

        #endregion

        private Brush selectedListViewItemBrush;
        public Brush SelectedListViewItemBrush
        {
            get => selectedListViewItemBrush;
            set
            {
                if (selectedListViewItemBrush != value)
                {
                    selectedListViewItemBrush = value;
                    OnPropertyChanged(nameof(SelectedListViewItemBrush));
                }
            }
        }

        private Brush mainPanelBackgroundBrush;
        public Brush MainPanelBackgroundBrush
        {
            get => mainPanelBackgroundBrush;
            set
            {
                if (mainPanelBackgroundBrush != value)
                {
                    mainPanelBackgroundBrush = value;
                    OnPropertyChanged(nameof(MainPanelBackgroundBrush));
                }
            }
        }

        private Brush mainPanelDarkeningBrush;
        public Brush MainPanelDarkeningBrush
        {
            get => mainPanelDarkeningBrush;
            set
            {
                if (mainPanelDarkeningBrush != value)
                {
                    mainPanelDarkeningBrush = value;
                    OnPropertyChanged(nameof(MainPanelDarkeningBrush));
                }
            }
        }

        private Brush mainPanelSeparatorBrush;
        public Brush MainPanelSeparatorBrush
        {
            get => mainPanelSeparatorBrush;
            set
            {
                if (mainPanelSeparatorBrush != value)
                {
                    mainPanelSeparatorBrush = value;
                    OnPropertyChanged(nameof(MainPanelSeparatorBrush));
                }
            }
        }

        private Brush mainPanelBorderBrush;
        public Brush MainPanelBorderBrush
        {
            get => mainPanelBorderBrush;
            set
            {
                if (mainPanelBorderBrush != value)
                {
                    mainPanelBorderBrush = value;
                    OnPropertyChanged(nameof(MainPanelBorderBrush));
                }
            }
        }

        private HorizontalAlignment topPanelHorizontalAlignment;
        public HorizontalAlignment TopPanelHorizontalAlignment
        {
            get => topPanelHorizontalAlignment;
            set
            {
                if (topPanelHorizontalAlignment != value)
                {
                    topPanelHorizontalAlignment = value;
                    OnPropertyChanged(nameof(TopPanelHorizontalAlignment));
                }
            }
        }

        private double topPanelMinCenterGap;
        public double TopPanelMinCenterGap
        {
            get => topPanelMinCenterGap;
            set
            {
                if (topPanelMinCenterGap != value)
                {
                    topPanelMinCenterGap = value;
                    OnPropertyChanged(nameof(TopPanelMinCenterGap));
                }
            }
        }

        private double topPanelHeight;
        public double TopPanelHeight
        {
            get => topPanelHeight;
            set
            {
                if (topPanelHeight != value)
                {
                    topPanelHeight = value;
                    OnPropertyChanged(nameof(TopPanelHeight));
                }
            }
        }

        private Thickness topPanelMargin;
        public Thickness TopPanelMargin
        {
            get => topPanelMargin;
            set
            {
                if (topPanelMargin != value)
                {
                    topPanelMargin = value;
                    OnPropertyChanged(nameof(TopPanelMargin));
                }
            }
        }

        private double topPanelSearchGap;
        public double TopPanelSearchGap
        {
            get => topPanelSearchGap;
            set
            {
                if (topPanelSearchGap != value)
                {
                    topPanelSearchGap = value;
                    OnPropertyChanged(nameof(TopPanelSearchGap));
                    OnPropertyChanged(nameof(TopPanelSearchGapEnable));
                }
            }
        }
        public bool TopPanelSearchGapEnable => TopPanelSearchGap > 0;

        private double topPanelSearchBoxHeight;
        public double TopPanelSearchBoxHeight
        {
            get => topPanelSearchBoxHeight;
            set
            {
                if (topPanelSearchBoxHeight != value)
                {
                    topPanelSearchBoxHeight = value;
                    OnPropertyChanged(nameof(TopPanelSearchBoxHeight));
                }
            }
        }

        private double topPanelSearchBoxWidth;
        public double TopPanelSearchBoxWidth
        {
            get => topPanelSearchBoxWidth;
            set
            {
                if (topPanelSearchBoxWidth != value)
                {
                    topPanelSearchBoxWidth = value;
                    OnPropertyChanged(nameof(TopPanelSearchBoxWidth));
                }
            }
        }

        private Brush topPanelBorderBrush;
        public Brush TopPanelBorderBrush
        {
            get => topPanelBorderBrush;
            set
            {
                if (topPanelBorderBrush != value)
                {
                    topPanelBorderBrush = value;
                    OnPropertyChanged(nameof(TopPanelBorderBrush));
                }
            }
        }

        private Thickness topPanelBorderThickness;
        public Thickness TopPanelBorderThickness
        {
            get => topPanelBorderThickness;
            set
            {
                if (topPanelBorderThickness != value)
                {
                    topPanelBorderThickness = value;
                    OnPropertyChanged(nameof(TopPanelBorderThickness));
                }
            }
        }

        private Brush topPanelSeparatorBrush;
        public Brush TopPanelSeparatorBrush
        {
            get => topPanelSeparatorBrush;
            set
            {
                if (topPanelSeparatorBrush != value)
                {
                    topPanelSeparatorBrush = value;
                    OnPropertyChanged(nameof(TopPanelSeparatorBrush));
                }
            }
        }

        private Brush topPanelBackgroundBrush;
        public Brush TopPanelBackgroundBrush
        {
            get => topPanelBackgroundBrush;
            set
            {
                if (topPanelBackgroundBrush != value)
                {
                    topPanelBackgroundBrush = value;
                    OnPropertyChanged(nameof(TopPanelBackgroundBrush));
                }
            }
        }


        private Effect topPanelDropShadow;
        public Effect TopPanelDropShadow
        {
            get => topPanelDropShadow;
            set
            {
                if (topPanelDropShadow != value)
                {
                    topPanelDropShadow = value;
                    OnPropertyChanged(nameof(TopPanelDropShadow));
                }
            }
        }

        private Style topPanelSearchBoxStyle;
        public Style TopPanelSearchBoxStyle
        {
            get => topPanelSearchBoxStyle;
            set
            {
                if (topPanelSearchBoxStyle != value)
                {
                    topPanelSearchBoxStyle = value;
                    OnPropertyChanged(nameof(TopPanelSearchBoxStyle));
                }
            }
        }

        private Style topPanelItemStyle;
        public Style TopPanelItemStyle
        {
            get => topPanelItemStyle;
            set
            {
                if (topPanelItemStyle != value)
                {
                    topPanelItemStyle = value;
                    OnPropertyChanged(nameof(TopPanelItemStyle));
                }
            }
        }

        private double topPanelProgressBarHeight;
        public double TopPanelProgressBarHeight
        {
            get => topPanelProgressBarHeight;
            set
            {
                if (topPanelProgressBarHeight != value)
                {
                    topPanelProgressBarHeight = value;
                    OnPropertyChanged(nameof(TopPanelProgressBarHeight));
                }
            }
        }

        private Thickness topPanelProgressBarMargin;
        public Thickness TopPanelProgressBarMargin
        {
            get => topPanelProgressBarMargin;
            set
            {
                if (topPanelProgressBarMargin != value)
                {
                    topPanelProgressBarMargin = value;
                    OnPropertyChanged(nameof(TopPanelProgressBarMargin));
                }
            }
        }


        private Brush lowerPanelBorderBrush;
        public Brush LowerPanelBorderBrush
        {
            get => lowerPanelBorderBrush;
            set
            {
                if (lowerPanelBorderBrush != value)
                {
                    lowerPanelBorderBrush = value;
                    OnPropertyChanged(nameof(LowerPanelBorderBrush));
                }
            }
        }

        private Thickness lowerPanelBorderThickness;
        public Thickness LowerPanelBorderThickness
        {
            get => lowerPanelBorderThickness;
            set
            {
                if (lowerPanelBorderThickness != value)
                {
                    lowerPanelBorderThickness = value;
                    OnPropertyChanged(nameof(LowerPanelBorderThickness));
                }
            }
        }

        private Style progressBarStyle;
        public Style ProgressBarStyle
        {
            get => progressBarStyle;
            set
            {
                if (progressBarStyle != value)
                {
                    progressBarStyle = value;
                    OnPropertyChanged(nameof(ProgressBarStyle));
                }
            }
        }

        private string progressInstallFgBrush;
        public string ProgressInstallFgBrush
        {
            get => progressInstallFgBrush;
            set
            {
                if (progressInstallFgBrush != value)
                {
                    progressInstallFgBrush = value;
                    OnPropertyChanged(nameof(ProgressInstallFgBrush));
                }
            }
        }


        private string progressInstallBgBrush;
        public string ProgressInstallBgBrush
        {
            get => progressInstallBgBrush;
            set
            {
                if (progressInstallBgBrush != value)
                {
                    progressInstallBgBrush = value;
                    OnPropertyChanged(nameof(ProgressInstallBgBrush));
                }
            }
        }
        public ThemeResources(UserControl resourceView)
        {
            this.resourceView = resourceView;
        }

        public object GetResource(string resourceKey)
        {
            return resourceView.TryFindResource(resourceKey);
        }

        public void ResetToDefaults()
        {
            TextBrush = GetResource("TextBrush") as Brush;
            TextBrushDarker = GetResource("TextBrushDarker") as Brush;
            GlyphBrush = GetResource("GlyphBrush") as Brush;
            WarningBrush = GetResource("WarningBrush") as Brush;
            InstallBrush = GetResource("InstallBrush") as Brush;
            SlowInstallBrush = GetResource("SlowInstallBrush") as Brush;

            SelectedListViewItemBrush = new BrushConverter().ConvertFrom("Black") as Brush;

            MainPanelBackgroundBrush = GetResource("TransparentBrush") as Brush;
            MainPanelDarkeningBrush = GetResource("TransparentBrush") as Brush;
            MainPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
            MainPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;

            TopPanelHorizontalAlignment = HorizontalAlignment.Stretch; // Stretch|Center
            TopPanelMinCenterGap = 8;
            TopPanelHeight = 50;
            TopPanelMargin = new Thickness(5, 0, 125, 10);
            TopPanelSearchGap = 0;
            TopPanelSearchBoxHeight = 31;
            TopPanelSearchBoxWidth = 300;
            TopPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
            TopPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
            TopPanelBorderThickness = new Thickness(0, 0, 0, 1);
            TopPanelBackgroundBrush = GetResource("TransparentBrush") as Brush;
            TopPanelDropShadow = null as Effect;
            TopPanelSearchBoxStyle = (GetResource("TopPanelSearchBox") ?? GetResource("TopPanelPluginSearchBox")) as Style;
            TopPanelItemStyle = GetResource("ThemeTopPanelItem") as Style;
            TopPanelProgressBarHeight = 31;
            TopPanelProgressBarMargin = new Thickness(0, 0, 0, 0);

            LowerPanelBorderBrush = GetResource("TransparentBrush") as Brush;
            LowerPanelBorderThickness = new Thickness(1, 1, 0, 0);
            
            ProgressBarStyle = GetResource("ThemeProgressBarStyle") as Style;
            ProgressInstallFgBrush = "ProgressInstallFgBrush";
            ProgressInstallBgBrush = "ProgressInstallBgBrush";
        }
    }

}
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Media.Effects;
using Brush = System.Windows.Media.Brush;
using System.Windows.Media;
using NowPlaying.Controls;
using Playnite.DesktopApp.Controls;
using System;
using NowPlaying.Utils;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;

namespace NowPlaying.ViewModels
{
    public class ThemeResources : ViewModelBase
    {
        private readonly NowPlaying plugin;
        private readonly UserControl resourceView;

        public ThemeResources(NowPlaying plugin, UserControl resourceView)
        {
            this.plugin = plugin;
            this.resourceView = resourceView;
        }

        #region common brush resources

        private Brush transparentBrush;
        public Brush TransparentBrush
        {
            get => transparentBrush;
            set
            {
                if (transparentBrush != value)
                {
                    transparentBrush = value;
                    OnPropertyChanged(nameof(TransparentBrush));
                }
            }
        }

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

        private Brush installFgBrush;
        public Brush InstallFgBrush
        {
            get => installFgBrush;
            set
            {
                if (installFgBrush != value)
                {
                    installFgBrush = value;
                    OnPropertyChanged(nameof(InstallFgBrush));
                }
            }
        }

        private Brush installBgBrush;
        public Brush InstallBgBrush
        {
            get => installBgBrush;
            set
            {
                if (installBgBrush != value)
                {
                    installBgBrush = value;
                    OnPropertyChanged(nameof(InstallBgBrush));
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

        private Brush slowInstallFgBrush;
        public Brush SlowInstallFgBrush
        {
            get => slowInstallFgBrush;
            set
            {
                if (slowInstallFgBrush != value)
                {
                    slowInstallFgBrush = value;
                    OnPropertyChanged(nameof(SlowInstallFgBrush));
                }
            }
        }

        private Brush slowInstallBgBrush;
        public Brush SlowInstallBgBrush
        {
            get => slowInstallBgBrush;
            set
            {
                if (slowInstallBgBrush != value)
                {
                    slowInstallBgBrush = value;
                    OnPropertyChanged(nameof(SlowInstallBgBrush));
                }
            }
        }

        private Brush processingFgBrush;
        public Brush ProcessingFgBrush
        {
            get => processingFgBrush;
            set
            {
                if (processingFgBrush != value)
                {
                    processingFgBrush = value;
                    OnPropertyChanged(nameof(ProcessingFgBrush));
                }
            }
        }

        private Brush processingBgBrush;
        public Brush ProcessingBgBrush
        {
            get => processingBgBrush;
            set
            {
                if (processingBgBrush != value)
                {
                    processingBgBrush = value;
                    OnPropertyChanged(nameof(ProcessingBgBrush));
                }
            }
        }

        private Brush enableFgBrush;
        public Brush EnableFgBrush
        {
            get => enableFgBrush;
            set
            {
                if (enableFgBrush != value)
                {
                    enableFgBrush = value;
                    OnPropertyChanged(nameof(EnableFgBrush));
                }
            }
        }

        private Brush enableBgBrush;
        public Brush EnableBgBrush
        {
            get => enableBgBrush;
            set
            {
                if (enableBgBrush != value)
                {
                    enableBgBrush = value;
                    OnPropertyChanged(nameof(EnableBgBrush));
                }
            }
        }

        private Brush uninstallFgBrush;
        public Brush UninstallFgBrush
        {
            get => uninstallFgBrush;
            set
            {
                if (uninstallFgBrush != value)
                {
                    uninstallFgBrush = value;
                    OnPropertyChanged(nameof(UninstallFgBrush));
                }
            }
        }

        private Brush uninstallBgBrush;
        public Brush UninstallBgBrush
        {
            get => uninstallBgBrush;
            set
            {
                if (uninstallBgBrush != value)
                {
                    uninstallBgBrush = value;
                    OnPropertyChanged(nameof(UninstallBgBrush));
                }
            }
        }

        #endregion

        #region theme tweakable resources

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

        private double topPanelItemHeight;
        public double TopPanelItemHeight
        {
            get => topPanelItemHeight;
            set
            {
                if (topPanelItemHeight != value)
                {
                    topPanelItemHeight = value;
                    OnPropertyChanged(nameof(TopPanelItemHeight));
                }
            }
        }

        private Brush progressBarTextBrush;
        public Brush ProgressBarTextBrush
        {
            get => progressBarTextBrush;
            set
            {
                if (progressBarTextBrush != value)
                {
                    progressBarTextBrush = value;
                    OnPropertyChanged(nameof(ProgressBarTextBrush));
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

        private double topPanelProgressBarWidth;
        public double TopPanelProgressBarWidth
        {
            get => topPanelProgressBarWidth;
            set
            {
                if (topPanelProgressBarWidth != value)
                {
                    topPanelProgressBarWidth = value;
                    OnPropertyChanged(nameof(TopPanelProgressBarWidth));
                }
            }
        }

        private double topPanelProgressBarRadius;
        public double TopPanelProgressBarRadius
        {
            get => topPanelProgressBarRadius;
            set
            {
                if (topPanelProgressBarRadius != value)
                {
                    topPanelProgressBarRadius = value;
                    OnPropertyChanged(nameof(TopPanelProgressBarRadius));
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

        private Style topPanelProgressBarStyle;
        public Style TopPanelProgressBarStyle
        {
            get => topPanelProgressBarStyle;
            set
            {
                if (topPanelProgressBarStyle != value)
                {
                    topPanelProgressBarStyle = value;
                    OnPropertyChanged(nameof(TopPanelProgressBarStyle));
                }
            }
        }

        private double installProgressBarHeight;
        public double InstallProgressBarHeight
        {
            get => installProgressBarHeight;
            set
            {
                if (installProgressBarHeight != value)
                {
                    installProgressBarHeight = value;
                    OnPropertyChanged(nameof(InstallProgressBarHeight));
                }
            }
        }

        private double installProgressBarRadius;
        public double InstallProgressBarRadius
        {
            get => installProgressBarRadius;
            set
            {
                if (installProgressBarRadius != value)
                {
                    installProgressBarRadius = value;
                    OnPropertyChanged(nameof(InstallProgressBarRadius));
                }
            }
        }

        private Style installProgressBarStyle;
        public Style InstallProgressBarStyle
        {
            get => installProgressBarStyle;
            set
            {
                if (installProgressBarStyle != value)
                {
                    installProgressBarStyle = value;
                    OnPropertyChanged(nameof(InstallProgressBarStyle));
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

        #endregion

        public object GetResource(string resourceKey)
        {
            return resourceView.TryFindResource(resourceKey);
        }

        public void ResetToDefaults()
        {
            TransparentBrush = new BrushConverter().ConvertFrom("Transparent") as Brush;
            TextBrush = GetResource("TextBrush") as Brush;
            TextBrushDarker = GetResource("TextBrushDarker") as Brush;
            GlyphBrush = GetResource("GlyphBrush") as Brush;
            WarningBrush = GetResource("WarningBrush") as Brush;
            InstallBrush = GetResource("InstallBrush") as Brush;
            InstallFgBrush = GetResource("InstallFgBrush") as Brush;
            InstallBgBrush = GetResource("InstallBgBrush") as Brush;
            SlowInstallBrush = GetResource("SlowInstallBrush") as Brush;
            SlowInstallFgBrush = GetResource("SlowInstallFgBrush") as Brush;
            SlowInstallBgBrush = GetResource("SlowInstallBgBrush") as Brush;
            ProcessingFgBrush = GetResource("ProcessingFgBrush") as Brush;
            ProcessingBgBrush = GetResource("ProcessingBgBrush") as Brush;
            EnableFgBrush = GetResource("EnableFgBrush") as Brush;
            EnableBgBrush = GetResource("EnableBgBrush") as Brush;
            UninstallFgBrush = GetResource("UninstallFgBrush") as Brush;
            UninstallBgBrush = GetResource("UninstallBgBrush") as Brush;

            MainPanelBackgroundBrush = TransparentBrush;
            MainPanelDarkeningBrush = TransparentBrush;
            MainPanelSeparatorBrush = TransparentBrush;
            MainPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
            TopPanelBackgroundBrush = TransparentBrush;
            TopPanelSeparatorBrush = TransparentBrush;
            TopPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
            TopPanelBorderThickness = new Thickness(0, 0, 0, 1);
            TopPanelDropShadow = null as Effect;
            LowerPanelBorderBrush = TransparentBrush;
            LowerPanelBorderThickness = new Thickness(1, 1, 0, 0);

            TopPanelHorizontalAlignment = HorizontalAlignment.Stretch; // Stretch|Center|Left|Right
            TopPanelMinCenterGap = 8;
            TopPanelHeight = 50;
            TopPanelMargin = new Thickness(5, 0, 125, 10);
            TopPanelItemStyle = GetResource("ThemeTopPanelItem") as Style;
            TopPanelItemHeight = 31;

            TopPanelSearchBoxStyle = (GetResource("TopPanelSearchBox") ?? GetResource("TopPanelPluginSearchBox")) as Style;
            TopPanelSearchBoxWidth = 300;
            TopPanelSearchGap = 0;

            TopPanelProgressBarHeight = 22;
            TopPanelProgressBarWidth = 202;
            TopPanelProgressBarMargin = new Thickness(5, 0, 5, 0);
            TopPanelProgressBarStyle = GetResource("ProgressBarStyle") as Style;
            TopPanelProgressBarRadius = 1.5;
            InstallProgressBarHeight = 22;
            InstallProgressBarStyle = GetResource("ProgressBarStyle") as Style;
            InstallProgressBarRadius = 2.5;
            ProgressBarTextBrush = TextBrush;

            SelectedListViewItemBrush = new BrushConverter().ConvertFrom("Black") as Brush;
        }

        public void ApplyThemeStylingTweaks()
        {
            ResetToDefaults();

            // apply theme-specific overrides
            var currentTheme = plugin.PlayniteApi.ApplicationSettings.DesktopTheme;

            // Classic
            if (currentTheme == "Playnite_builtin_ClassicDesktop") { }
            // Classic Blue
            else if (currentTheme == "Playnite_builtin_ClassicDesktopBlue") { }
            // Classic Green
            else if (currentTheme == "Playnite_builtin_ClassicDesktopGreen") { }
            // Classic Plain
            else if (currentTheme == "Playnite_builtin_ClassicDesktopPlain") { }
            // Daze
            else if (currentTheme == "Daze_27790ca9-d3a4-480f-bffe-914ec6768363")
            {
                TopPanelHeight = 60;
                var margin = TopPanelMargin; margin.Left = 25; margin.Right = 135;
                TopPanelMargin = margin;
                TopPanelItemHeight = 31;
                TopPanelSearchGap = 15;
                var brush = new BrushConverter().ConvertFromString("#FFF2F2F2") as Brush; brush.Opacity = 0.2;
                TopPanelBorderBrush = brush;

                // attempt to override theme's fixed progress bar colors, but otherwise keep styling intact
                TopPanelProgressBarStyle = GetResource("ThemeProgressBarStyle") as Style;
                TopPanelProgressBarMargin = new Thickness(0, 0, 0, 0);
                TweakDazeThemeProgressBar(plugin.topPanelItem.Icon as UserControl, "TopPanelProgressBar");
                TweakDazeThemeProgressBar(plugin.topPanelView as UserControl, "TopPanelProgressBar");
                InstallProgressBarStyle = GetResource("ThemeProgressBarStyle") as Style;
                plugin.OnInstallProgressViewLoaded = new Action(() =>
                {
                    TweakDazeThemeProgressBar(plugin.panelViewModel.InstallProgressView as UserControl, "InstallProgressBar");
                });
            }
            // Default
            else if (currentTheme == "Playnite_builtin_DefaultDesktop") { }
            // Default Red
            else if (currentTheme == "Playnite_builtin_DefaultDesktopRed") { }
            // DefaultExtend
            else if (currentTheme == "playnite-defaultextend-theme") { }
            // DH_Dawn
            else if (currentTheme == "felixkmh_DesktopTheme_DH_Dawn") 
            {
                SelectedListViewItemBrush = TextBrush;
            }
            // DH_Night
            else if (currentTheme == "felixkmh_DuplicateHider_Night_Theme")
            {
                TopPanelHeight = 48.5;
                var margin = TopPanelMargin; margin.Bottom = 8.5;
                TopPanelMargin = margin;
                var brush = GetResource("BackgroundImage") as Brush;
                MainPanelSeparatorBrush = brush;
                MainPanelBorderBrush = brush;
                MainPanelBackgroundBrush = brush;
                MainPanelDarkeningBrush = new BrushConverter().ConvertFromString("#B2202020") as Brush; // trial-and-error value
                TopPanelBorderThickness = new Thickness(0);
                LowerPanelBorderBrush = brush;
                LowerPanelBorderThickness = new Thickness(0, 1, 0, 0);
                SelectedListViewItemBrush = TextBrush;
            }
            // eMixedNite
            else if (currentTheme == "eMixedNite_d3544fdb-be37-4677-b317-7d747adc6b8e") 
            {
                SelectedListViewItemBrush = new BrushConverter().ConvertFromString("#FFC0C0C0") as Brush;
            }
            // eMixedNiteMC
            else if (currentTheme == "eMixedNiteMC_4333b3b2-0374-43a1-a9eb-d27f3ea89ef8")
            {
                TopPanelHeight = 44;
                var margin = TopPanelMargin; margin.Bottom = 6;
                TopPanelMargin = margin;
                TopPanelItemHeight = 32;
                //TopPanelProgressBarHeight = 32;
                SelectedListViewItemBrush = TextBrushDarker;
                InstallFgBrush = TextBrushDarker;
                WarningBrush = new BrushConverter().ConvertFromString("#FFFF4500") as Brush;
            }
            // Enhanced Grid View Extend
            else if (currentTheme == "Enhaced_Grid_View_Extend_1") { }
            // GridViewCards
            else if (currentTheme == "GridViewCards_9af15fb8-f51c-45df-93fc-235c50bfcd39")
            {
                var margin = TopPanelMargin; margin.Left = 30; margin.Bottom = 10.5; margin.Right = 120;
                TopPanelMargin = margin;
                TopPanelSearchBoxWidth = 350;
                TopPanelItemHeight = 30;
                TopPanelSearchGap = 10;
                TopPanelBackgroundBrush = new BrushConverter().ConvertFromString("#FF2C2C2D") as Brush;
                MainPanelBackgroundBrush = new BrushConverter().ConvertFromString("#FF191919") as Brush;
                TopPanelProgressBarRadius = 4;
                InstallProgressBarRadius = 4;
                InstallFgBrush = new BrushConverter().ConvertFromString("#FF959595") as Brush;
                WarningBrush = new BrushConverter().ConvertFromString("#FFFF4500") as Brush;
            }
            // Harmony
            else if (currentTheme == "Harmony_d49ef7bc-49de-4fd0-9a67-bd1f26b56047")
            {
                TopPanelHeight = 52;
                var margin = TopPanelMargin; margin.Left = 20; margin.Bottom = 9; margin.Right = 137;
                TopPanelMargin = margin;
                TopPanelSearchBoxWidth = 250;
                TopPanelItemHeight = 34;
                TopPanelSearchGap = 30;
                TopPanelProgressBarHeight = 24;
                TopPanelProgressBarRadius = 4;
            }
            // Helium
            else if (currentTheme == "8b15c46a-90c2-4fe5-9ebb-1ab25ba7fcb1")
            {
                var margin = TopPanelMargin; margin.Left = 0; margin.Bottom = 9;
                TopPanelMargin = margin;
                TopPanelSearchBoxWidth = 350;
                TopPanelItemHeight = 32;
                TopPanelProgressBarRadius = 2.5;
                TopPanelBackgroundBrush = GetResource("TopPanelBackgroundBrush") as Brush;
                TopPanelBorderBrush = GetResource("TransparentBrush") as Brush;
                TopPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
                LowerPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
                SelectedListViewItemBrush = null; // keep Foreground color as is when selected
            }
            // KNARZnite
            else if (currentTheme == "KNARZnite_68cee656-e677-42ab-a33e-9d9e6dfbefb9")
            {
                TopPanelBackgroundBrush = GetResource("WindowBackgourndBrush") as Brush;
                TopPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
                TopPanelDropShadow = GetResource("DefaultDropShadow") as Effect;
                TopPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
            }
            // Minimal
            else if (currentTheme == "Minimal_01b9013c-0777-46ba-a09e-035bd66a79e2") { }
            // Mythic
            else if (currentTheme == "Mythic_e231056c-4fa7-49d8-ad2b-0a6f1c589eb8")
            {
                TopPanelHeight = 100;
                var margin = TopPanelMargin; margin.Left = 12; margin.Bottom = 31; margin.Right = 190;
                TopPanelMargin = margin;
                TopPanelSearchBoxWidth = 240;
                TopPanelItemHeight = 40;
                TopPanelSearchGap = 0;
                TopPanelProgressBarHeight = 28;
                TopPanelProgressBarRadius = 10;
                SelectedListViewItemBrush = TextBrush;

                // add background setter to theme's TopPanelSearchBox style
                var style = new Style(typeof(SearchBox), basedOn: GetResource("TopPanelSearchBox") as Style);
                var brush = GetResource("TopPanelSearchBoxBackgroundBrush") as Brush;
                style.Setters.Add(new Setter(PluginSearchBox.BackgroundProperty, brush));
                TopPanelSearchBoxStyle = style;
            }
            // Neon
            else if (currentTheme == "8b15c46a-90c2-4fe5-9ebb-1ab25ba7fcb2")
            {
                var margin = TopPanelMargin; margin.Bottom = 10;
                TopPanelMargin = margin;
                TopPanelItemHeight = 31;
                TopPanelSearchGap = 0;
                TopPanelBackgroundBrush = GetResource("TopPanelBackgroundBrush") as Brush;
                SelectedListViewItemBrush = null;
            }
            // Nova X
            else if (currentTheme == "Nova_X_0a95b7a3-00e4-412d-b301-f2fa3f98dfad")
            {
                TopPanelHorizontalAlignment = HorizontalAlignment.Center;
                TopPanelMinCenterGap = 22;
                TopPanelHeight = 84;
                var margin = TopPanelMargin; margin.Left = 5; margin.Bottom = 32; margin.Right = 5;
                TopPanelMargin = margin;
                TopPanelSearchBoxWidth = 120;
                TopPanelSearchGap = 0;
                TopPanelBorderBrush = GetResource("TransparentBrush") as Brush;
                TopPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
                SelectedListViewItemBrush = TextBrush;

                // add background + fontweight setters to theme's TopPanelSearchBox style
                var style = new Style(typeof(SearchBox), basedOn: GetResource("SearchBoxTopPanel") as Style);
                var brush = GetResource("TopPanelSearchBoxBackgroundBrush") as Brush;
                style.Setters.Add(new Setter(PluginSearchBox.BackgroundProperty, brush));
                style.Setters.Add(new Setter(PluginSearchBox.FontWeightProperty, FontWeights.SemiBold));
                TopPanelSearchBoxStyle = style;
            }
            // Rwl
            else if (currentTheme == "Raohmaru_Rwl_Desktop_e3065120-6742-4ebd-95de-b9ce142737ac")
            {
                var margin = TopPanelMargin; margin.Bottom = 10;
                TopPanelMargin = margin;
                TopPanelItemHeight = 31;
                TopPanelSearchGap = 0;
                TopPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
                MainPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
                TopPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
                MainPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
            }
            // Seaside
            else if (currentTheme == "Seaside_df4e11f8-2347-4a2d-b835-757aec63e15c")
            {
                TopPanelHorizontalAlignment = HorizontalAlignment.Center;
                TopPanelMinCenterGap = 8;
                TopPanelHeight = 70;
                var margin = TopPanelMargin; margin.Left = 5; margin.Bottom = 10; margin.Right = 5;
                TopPanelMargin = margin;
                TopPanelSearchBoxWidth = 200;
                TopPanelItemHeight = 45;
                TopPanelSearchGap = 2;
                TopPanelProgressBarHeight = 30;
                TopPanelProgressBarRadius = 3;
                TopPanelBorderBrush = GetResource("TransparentBrush") as Brush;
                TopPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
                TopPanelBackgroundBrush = GetResource("TopPanelFadeBackgroundBrush") as Brush;
                MainPanelDarkeningBrush = new BrushConverter().ConvertFromString("#701f1e20") as Brush; // trial-and-error value
                SelectedListViewItemBrush = TextBrush;

                // add background + fontweight setters to theme's SearchBox style
                var style = new Style(typeof(PluginSearchBox), basedOn: GetResource("SearchBoxTopPanel") as Style);
                var brush = GetResource("TopPanelSearchBoxBackgroundBrush") as Brush;
                style.Setters.Add(new Setter(PluginSearchBox.BackgroundProperty, brush));
                style.Setters.Add(new Setter(PluginSearchBox.FontWeightProperty, FontWeights.SemiBold));
                TopPanelSearchBoxStyle = style;
            }
            // Standard X
            else if (currentTheme == "StandardX") 
            {
                InstallFgBrush = TextBrushDarker;
            }
            // Stardust
            else if (currentTheme == "Stardust 2.0_1fb333b2-255b-43dd-aec1-8e2f2d5ea002")
            {
                TopPanelHorizontalAlignment = HorizontalAlignment.Center;
                TopPanelMinCenterGap = 8;
                var margin = TopPanelMargin; margin.Left = 5; margin.Bottom = 0; margin.Right = 5;
                TopPanelMargin = margin;
                TopPanelSearchBoxWidth = 200;
                TopPanelItemHeight = 35;
                TopPanelSearchGap = 0;
                TopPanelProgressBarHeight = 25;
                TopPanelProgressBarRadius = 3;
                TopPanelProgressBarMargin = new Thickness(3, 0, 3, 0);
                TopPanelBorderBrush = GetResource("TransparentBrush") as Brush;
                TopPanelSeparatorBrush = GetResource("TransparentBrush") as Brush;
                SelectedListViewItemBrush = TextBrush;

                // add background + fontweight + height setters to theme's SearchBox style
                var style = new Style(typeof(PluginSearchBox), basedOn: GetResource("SearchBoxTopPanel") as Style);
                var brush = GetResource("TopPanelSearchBoxBackgroundBrush") as Brush;
                style.Setters.Add(new Setter(PluginSearchBox.BackgroundProperty, brush));
                style.Setters.Add(new Setter(PluginSearchBox.FontWeightProperty, FontWeights.SemiBold));
                TopPanelSearchBoxStyle = style;
            }
        }

        private static void TweakDazeThemeProgressBar(UserControl view, string targetProgressBarName)
        {
            if (view != null && view.IsLoaded)
            {
                try
                {
                    var progressBar = view.FindName(targetProgressBarName) as ProgressBar;
                    progressBar.ApplyTemplate();

                    // bind progress bar's Foreground to indicator rectangle's Fill
                    var indicator = progressBar.Template.FindName("PART_Indicator", progressBar) as Decorator;
                    var rectangle = WpfUtils.GetChildOfType<Rectangle>(indicator) as Rectangle;
                    BindingOperations.SetBinding
                    (
                        rectangle,
                        Rectangle.FillProperty,
                        new Binding()
                        {
                            Source = progressBar,
                            Path = new PropertyPath("Foreground"),
                            Mode = BindingMode.OneWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                            NotifyOnTargetUpdated = true
                        }
                    );

                    // bind progress bar's Background to animation rectangle's Fill
                    var animation = progressBar.Template.FindName("Animation", progressBar) as Grid;
                    rectangle = WpfUtils.GetChildOfType<Rectangle>(animation) as Rectangle;
                    BindingOperations.SetBinding
                    (
                        rectangle,
                        Rectangle.FillProperty,
                        new Binding()
                        {
                            Source = progressBar,
                            Path = new PropertyPath("Background"),
                            Mode = BindingMode.OneWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                            NotifyOnTargetUpdated = true
                        }
                    );
                }
                catch { }
                view.Loaded -= (s, e) => TweakDazeThemeProgressBar(view, targetProgressBarName);
            }
            else if (view != null)
            {
                view.Loaded += (s, e) => TweakDazeThemeProgressBar(view, targetProgressBarName);
            }
        }

    }

}
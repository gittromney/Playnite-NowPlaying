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
using System.Collections.Generic;

namespace NowPlaying.ViewModels
{
    public class ThemeResources : ViewModelBase
    {
        public enum TopPanelColumn { FarLeft=0, LGap=1, Left=2, Middle=3, Right=4, RGap=5, FarRight=6 };

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

        private double headingTextFontSize = 18;
        public double HeadingTextFontSize
        {
            get => headingTextFontSize;
            set
            {
                if (headingTextFontSize != value)
                {
                    headingTextFontSize = value;
                    OnPropertyChanged(nameof(HeadingTextFontSize));
                }
            }
        }

        private Brush popupBackgroundBrush;
        public Brush PopupBackgroundBrush
        {
            get => popupBackgroundBrush;
            set
            {
                if (popupBackgroundBrush != value)
                {
                    popupBackgroundBrush = value;
                    OnPropertyChanged(nameof(PopupBackgroundBrush));
                }
            }
        }

        private Brush popupDarkeningBrush;
        public Brush PopupDarkeningBrush
        {
            get => popupDarkeningBrush;
            set
            {
                if (popupDarkeningBrush != value)
                {
                    popupDarkeningBrush = value;
                    OnPropertyChanged(nameof(PopupDarkeningBrush));
                }
            }
        }

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

        private int topPanelMenuItemsColumnSpan = 1;
        public int TopPanelMenuItemsColumnSpan
        {
            get => topPanelMenuItemsColumnSpan;
            set
            {
                if (topPanelMenuItemsColumnSpan != value)
                {
                    topPanelMenuItemsColumnSpan = value;
                    OnPropertyChanged(nameof(TopPanelMenuItemsColumnSpan));
                }
            }
        }

        private int topPanelSearchBoxColumnSpan = 1;
        public int TopPanelSearchBoxColumnSpan
        {
            get => topPanelSearchBoxColumnSpan;
            set
            {
                if (topPanelSearchBoxColumnSpan != value)
                {
                    topPanelSearchBoxColumnSpan = value;
                    OnPropertyChanged(nameof(TopPanelSearchBoxColumnSpan));
                }
            }
        }

        private int topPanelSearchBoxColumn;
        public int TopPanelSearchBoxColumn
        {
            get => topPanelSearchBoxColumn;
            set
            {
                if (topPanelSearchBoxColumn != value)
                {
                    topPanelSearchBoxColumn = value;
                    OnPropertyChanged(nameof(TopPanelSearchBoxColumn));
                }
            }
        }

        private int topPanelSearchGapColumn;
        public int TopPanelSearchGapColumn
        {
            get => topPanelSearchGapColumn;
            set
            {
                if (topPanelSearchGapColumn != value)
                {
                    topPanelSearchGapColumn = value;
                    OnPropertyChanged(nameof(TopPanelSearchGapColumn));
                }
            }
        }

        private int topPanelMenuItemsColumn;
        public int TopPanelMenuItemsColumn
        {
            get => topPanelMenuItemsColumn;
            set
            {
                if (topPanelMenuItemsColumn != value)
                {
                    topPanelMenuItemsColumn = value;
                    OnPropertyChanged(nameof(TopPanelMenuItemsColumn));
                }
            }
        }

        private int topPanelProgressBarColumn;
        public int TopPanelProgressBarColumn
        {
            get => topPanelProgressBarColumn;
            set
            {
                if (topPanelProgressBarColumn != value)
                {
                    topPanelProgressBarColumn = value;
                    OnPropertyChanged(nameof(TopPanelProgressBarColumn));
                }
            }
        }

        private Thickness topPanelMenuItemsMargin;
        public Thickness TopPanelMenuItemsMargin
        {
            get => topPanelMenuItemsMargin;
            set
            {
                if (topPanelMenuItemsMargin != value)
                {
                    topPanelMenuItemsMargin = value;
                    OnPropertyChanged(nameof(TopPanelMenuItemsMargin));
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

        private Thickness topPanelSearchBoxMargin;
        public Thickness TopPanelSearchBoxMargin
        {
            get => topPanelSearchBoxMargin;
            set
            {
                if (topPanelSearchBoxMargin != value)
                {
                    topPanelSearchBoxMargin = value;
                    OnPropertyChanged(nameof(TopPanelSearchBoxMargin));
                }
            }
        }

        private Brush topPanelSearchBoxBgBrush;
        public Brush TopPanelSearchBoxBgBrush
        {
            get => topPanelSearchBoxBgBrush;
            set
            {
                if (topPanelSearchBoxBgBrush != value)
                {
                    topPanelSearchBoxBgBrush = value;
                    OnPropertyChanged(nameof(TopPanelSearchBoxBgBrush));
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

        private double topPanelProgressBarFontSize = 13;
        public double TopPanelProgressBarFontSize
        {
            get => topPanelProgressBarFontSize;
            set
            {
                if (topPanelProgressBarFontSize != value)
                {
                    topPanelProgressBarFontSize = value;
                    OnPropertyChanged(nameof(TopPanelProgressBarFontSize));
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

        private Thickness topPanelDockMargin;
        public Thickness TopPanelDockMargin
        {
            get => topPanelDockMargin;
            set
            {
                if (topPanelDockMargin != value)
                {
                    topPanelDockMargin = value;
                    OnPropertyChanged(nameof(TopPanelDockMargin));
                }
            }
        }

        private Thickness lowerPanelDockMargin;
        public Thickness LowerPanelDockMargin
        {
            get => lowerPanelDockMargin;
            set
            {
                if (lowerPanelDockMargin != value)
                {
                    lowerPanelDockMargin = value;
                    OnPropertyChanged(nameof(LowerPanelDockMargin));
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

        private double installProgressBarFontSize = 16;
        public double InstallProgressBarFontSize
        {
            get => installProgressBarFontSize;
            set
            {
                if (installProgressBarFontSize != value)
                {
                    installProgressBarFontSize = value;
                    OnPropertyChanged(nameof(InstallProgressBarFontSize));
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

        private Style listViewItemStyle;
        public Style ListViewItemStyle
        {
            get => listViewItemStyle;
            set
            {
                if (listViewItemStyle != value)
                {
                    listViewItemStyle = value;
                    OnPropertyChanged(nameof(ListViewItemStyle));
                }
            }
        }


        private Style listViewStyle;
        public Style ListViewStyle
        {
            get => listViewStyle;
            set
            {
                if (listViewStyle != value)
                {
                    listViewStyle = value;
                    OnPropertyChanged(nameof(ListViewStyle));
                }
            }
        }

        private Thickness listViewPadding;
        public Thickness ListViewPadding
        {
            get => listViewPadding;
            set
            {
                if (listViewPadding != value)
                {
                    listViewPadding = value;
                    OnPropertyChanged(nameof(ListViewPadding));
                }
            }
        }

        private Thickness listViewMargin;
        public Thickness ListViewMargin
        {
            get => listViewMargin;
            set
            {
                if (listViewMargin != value)
                {
                    listViewMargin = value;
                    OnPropertyChanged(nameof(ListViewMargin));
                }
            }
        }

        private Brush listViewBorderBrush;
        public Brush ListViewBorderBrush
        {
            get => listViewBorderBrush;
            set
            {
                if (listViewBorderBrush != value)
                {
                    listViewBorderBrush = value;
                    OnPropertyChanged(nameof(ListViewBorderBrush));
                }
            }
        }

        private Thickness listViewBorderThickness;
        public Thickness ListViewBorderThickness
        {
            get => listViewBorderThickness;
            set
            {
                if (listViewBorderThickness != value)
                {
                    listViewBorderThickness = value;
                    OnPropertyChanged(nameof(ListViewBorderThickness));
                }
            }
        }

        private Thickness listViewControlsMargin;
        public Thickness ListViewControlsMargin
        {
            get => listViewControlsMargin;
            set
            {
                if (listViewControlsMargin != value)
                {
                    listViewControlsMargin = value;
                    OnPropertyChanged(nameof(ListViewControlsMargin));
                }
            }
        }

        private double rootsIconRectSize;
        public double RootsIconRectSize
        {
            get => rootsIconRectSize;
            set
            {
                if (rootsIconRectSize != value)
                {
                    rootsIconRectSize = value;
                    OnPropertyChanged(nameof(RootsIconRectSize));
                }
            }
        }

        private double settingsIconFontSize = 17;
        public double SettingsIconFontSize
        {
            get => settingsIconFontSize;
            set
            {
                if (settingsIconFontSize != value)
                {
                    settingsIconFontSize = value;
                    OnPropertyChanged(nameof(SettingsIconFontSize));
                }
            }
        }

        private Style bottomButtonStyle;
        public Style BottomButtonStyle
        {
            get => bottomButtonStyle;
            set
            {
                if (bottomButtonStyle != value)
                {
                    bottomButtonStyle = value;
                    OnPropertyChanged(nameof(BottomButtonStyle));
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
            
            PopupBackgroundBrush = TransparentBrush;
            PopupDarkeningBrush = TransparentBrush;
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

            // default panel order: search box, (gap), menu items, (min center gap), progress bar 
            TopPanelSearchBoxColumn = (int)TopPanelColumn.FarLeft;
            TopPanelSearchGapColumn = (int)TopPanelColumn.LGap;
            TopPanelMenuItemsColumn = (int)TopPanelColumn.Left;
            TopPanelProgressBarColumn = (int)TopPanelColumn.FarRight;
            TopPanelMenuItemsMargin = new Thickness(0);
            TopPanelMenuItemsColumnSpan = 1;
            TopPanelSearchBoxColumnSpan = 1;
            TopPanelSearchGap = 0;

            TopPanelSearchBoxStyle = (GetResource("TopPanelSearchBox") ?? GetResource("TopPanelPluginSearchBox")) as Style;
            TopPanelSearchBoxWidth = 300;
            TopPanelSearchBoxHeight = double.NaN; // NaN - use TopPanelItemHeight
            TopPanelSearchBoxMargin = new Thickness(0);
            TopPanelSearchBoxBgBrush = GetResource("TopPanelSearchBoxBackgroundBrush") as Brush ?? TransparentBrush;

            TopPanelProgressBarHeight = 22;
            TopPanelProgressBarWidth = 202;
            TopPanelProgressBarMargin = new Thickness(5, 0, 5, 0);
            TopPanelProgressBarStyle = GetResource("ProgressBarStyle") as Style;
            TopPanelProgressBarRadius = 1.5;
            TopPanelProgressBarFontSize = (double)GetResource("FontSize") * (13.0 / 14.0);

            TopPanelDockMargin = new Thickness(0);
            LowerPanelDockMargin = new Thickness(5, 0, 0, 0);

            InstallProgressBarHeight = 22;
            InstallProgressBarStyle = GetResource("ProgressBarStyle") as Style;
            InstallProgressBarRadius = 2.5;
            InstallProgressBarFontSize = (double)GetResource("FontSize") * (16.0 / 14.0);
            ProgressBarTextBrush = TextBrush;

            HeadingTextFontSize = (double)GetResource("FontSizeLarge") * 1.2;
            RootsIconRectSize = 22;
            SettingsIconFontSize = 17;

            ListViewItemStyle = GetResource("ThemeListViewItem") as Style;
            ListViewStyle = GetResource("ThemeListView") as Style;
            ListViewPadding = new Thickness(2, 0.5, 5, 0.5);
            ListViewMargin = new Thickness(0);
            ListViewBorderBrush = TransparentBrush;
            ListViewBorderThickness = new Thickness(0);
            ListViewControlsMargin = new Thickness(1, 5, 0, 10);
            SelectedListViewItemBrush = new BrushConverter().ConvertFrom("Black") as Brush;

            var basedOnStyle = GetResource("BottomButton") as Style;
            var style = new Style(typeof(Button), basedOn: basedOnStyle);
            style.Setters.Add(new Setter(Button.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            BottomButtonStyle = style;
        }

        private bool currentThemeIsFusionX = false;

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
                PopupDarkeningBrush = BrushFromString("#FFF2F2F2", Opacity: 0.07);
                TopPanelHeight = 60;
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 25, Right: 135);
                TopPanelItemHeight = 31;
                TopPanelSearchGap = 15;
                TopPanelBorderBrush = BrushFromString("#FFF2F2F2", Opacity: 0.2);
                LowerPanelDockMargin = new Thickness(30, 60, 30, 8);
                ListViewPadding = AdjustMargin(ListViewPadding, Left: 5);

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
                PopupDarkeningBrush = BrushCloneFromResource("PopupBackgroundBrush", Opacity: 0.5);
                SelectedListViewItemBrush = TextBrush;
            }
            // DH_Night
            else if (currentTheme == "felixkmh_DuplicateHider_Night_Theme")
            {
                PopupDarkeningBrush = BrushCloneFromResource("PopupBackgroundBrush", Opacity: 0.8);
                TopPanelHeight = 48.5;
                TopPanelMargin = AdjustMargin(TopPanelMargin, Bottom: 8.5);
                var brush = GetResource("BackgroundImage") as Brush;
                PopupBackgroundBrush = brush;
                MainPanelSeparatorBrush = brush;
                MainPanelBorderBrush = brush;
                MainPanelBackgroundBrush = brush;
                MainPanelDarkeningBrush = BrushFromString("#B2202020"); // trial-and-error value
                TopPanelBorderThickness = new Thickness(0);
                LowerPanelBorderBrush = brush;
                LowerPanelBorderThickness = new Thickness(0, 1, 0, 0);
                SelectedListViewItemBrush = TextBrush;
            }
            // eMixedNite
            else if (currentTheme == "eMixedNite_d3544fdb-be37-4677-b317-7d747adc6b8e") 
            {
                MainPanelDarkeningBrush = BrushFromString("#70202020"); // trial-and-error value
                PopupDarkeningBrush = BrushFromString("#B2202020"); // trial-and-error value
                TopPanelHeight = 50;
                TopPanelMargin = AdjustMargin(TopPanelMargin, Bottom: 10, Right: 188);
                TopPanelSearchBoxColumn = (int)TopPanelColumn.FarLeft;
                TopPanelMenuItemsColumn = (int)TopPanelColumn.Right;
                TopPanelSearchGapColumn = (int)TopPanelColumn.RGap;
                TopPanelProgressBarColumn = (int)TopPanelColumn.FarRight;
                TopPanelSearchGap = 15;
                SelectedListViewItemBrush = BrushFromString("#FFC8C8C8"); // more readable than theme's choice
            }
            // eMixedNiteMC
            else if (currentTheme == "eMixedNiteMC_4333b3b2-0374-43a1-a9eb-d27f3ea89ef8")
            {
                TopPanelHeight = 45;
                TopPanelMargin = AdjustMargin(TopPanelMargin, Bottom: 7, Right: 168);
                TopPanelItemHeight = 32;
                TopPanelSearchBoxColumn = (int)TopPanelColumn.FarLeft;
                TopPanelSearchGapColumn = (int)TopPanelColumn.LGap;
                TopPanelProgressBarColumn = (int)TopPanelColumn.Left;
                TopPanelMenuItemsColumn = (int)TopPanelColumn.FarRight;
                TopPanelSearchGap = 15;
                InstallFgBrush = TextBrushDarker;
                WarningBrush = BrushFromString("#FFFF4500");
                SelectedListViewItemBrush = TextBrushDarker;
            }
            // Enhanced Grid View Extend
            else if (currentTheme == "Enhaced_Grid_View_Extend_1") { }
            // FusionX
            else if (currentTheme == "FusionX_54244ec8-29ec-418e-bce7-415250c8d67b")
            {
                PopupBackgroundBrush = GetResource("MainBackgroundBrush") as Brush;
                PopupDarkeningBrush = BrushCloneFromResource("PopupBackgroundBrush", Opacity: 0.7);
                ListViewItemStyle = GetResource("ListViewItem_FusionX") as Style;
                ListViewStyle = GetResource("ListView_FusionX") as Style;
                ListViewMargin = new Thickness(-18, -18, 0, 0);
                SelectedListViewItemBrush = TextBrush;

                currentThemeIsFusionX = true;
                TweakFusionXThemeListView(plugin.panelView.GameCaches);
                TweakFusionXThemeListView(plugin.cacheRootsView.CacheRoots);

                TopPanelHeight = 60;
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 12, Bottom: 14.5, Right: 128);
                TopPanelItemHeight = 32;
                TopPanelProgressBarMargin = new Thickness(-3, 0, -3, 0);
                TopPanelProgressBarHeight = 26;
                TopPanelProgressBarRadius = 10;
                InstallProgressBarRadius = 4;

                // centered search bar (unaffected by the progress bar)
                TopPanelMenuItemsColumn = (int)TopPanelColumn.Left;
                TopPanelSearchBoxColumn = (int)TopPanelColumn.Middle;
                TopPanelProgressBarColumn = (int)TopPanelColumn.Right;
                TopPanelSearchBoxColumnSpan = 2; // span Middle/Right

                TopPanelSearchBoxWidth = 200;
                TopPanelSearchBoxHeight = 36;
                TopPanelSearchBoxMargin = new Thickness(42, 0, 0, -2);
                LowerPanelDockMargin = new Thickness(18, 18, 0, 0);
                RootsIconRectSize = 18;
                SettingsIconFontSize = 14.5;
            }
            // GridViewCards
            else if (currentTheme == "GridViewCards_9af15fb8-f51c-45df-93fc-235c50bfcd39")
            {
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 30, Bottom: 10.5, Right: 120);
                TopPanelSearchBoxWidth = 350;
                TopPanelItemHeight = 30;

                // centered menu items (unaffected by search box/progress bar)
                TopPanelSearchBoxColumn = (int)TopPanelColumn.Left;
                TopPanelMenuItemsColumn = (int)TopPanelColumn.Left;
                TopPanelProgressBarColumn = (int)TopPanelColumn.Right;
                TopPanelMenuItemsColumnSpan = 3; // span Left/Middle/Right
                TopPanelMenuItemsMargin = new Thickness(40, 0, 0, 0);

                TopPanelBackgroundBrush = BrushFromString("#FF2C2C2D");
                MainPanelBackgroundBrush = BrushFromString("#FF191919");
                TopPanelProgressBarHeight = 24;
                TopPanelProgressBarRadius = 6;
                InstallProgressBarRadius = 4;
                InstallFgBrush = BrushFromString("#FF959595");
                WarningBrush = BrushFromString("#FFFF4500");
            }
            // Harmony
            else if (currentTheme == "Harmony_d49ef7bc-49de-4fd0-9a67-bd1f26b56047")
            {
                TopPanelHeight = 52;
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 20, Bottom: 9, Right: 137);
                TopPanelSearchBoxWidth = 250;
                TopPanelItemHeight = 34;
                TopPanelSearchGap = 30;
                TopPanelProgressBarHeight = 24;
                TopPanelProgressBarRadius = 4;
                LowerPanelDockMargin = new Thickness(20, 20, 0, 0);
            }
            // Helium
            else if (currentTheme == "8b15c46a-90c2-4fe5-9ebb-1ab25ba7fcb1")
            {
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 0, Bottom: 9);
                TopPanelSearchBoxWidth = 350;
                TopPanelItemHeight = 32;
                TopPanelProgressBarRadius = 2.5;
                TopPanelBackgroundBrush = GetResource("TopPanelBackgroundBrush") as Brush;
                TopPanelBorderBrush = TransparentBrush;
                TopPanelSeparatorBrush = TransparentBrush;
                LowerPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
                SelectedListViewItemBrush = null; // keep Foreground color as is when selected
            }
            // KNARZnite
            else if (currentTheme == "KNARZnite_68cee656-e677-42ab-a33e-9d9e6dfbefb9")
            {
                TopPanelBackgroundBrush = GetResource("WindowBackgourndBrush") as Brush;
                TopPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
                TopPanelDropShadow = GetResource("DefaultDropShadow") as Effect;
                TopPanelSeparatorBrush = TransparentBrush;
                SelectedListViewItemBrush = GetResource("TextBrushDark") as Brush;
            }
            // Minimal
            else if (currentTheme == "Minimal_01b9013c-0777-46ba-a09e-035bd66a79e2") { }
            // Mythic
            else if (currentTheme == "Mythic_e231056c-4fa7-49d8-ad2b-0a6f1c589eb8")
            {
                TopPanelHeight = 98;
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 12, Bottom: 29, Right: 190);
                TopPanelItemHeight = 40;
                TopPanelSearchBoxWidth = 240;
                TopPanelProgressBarHeight = 28;
                TopPanelProgressBarRadius = 10;
                TopPanelBorderBrush = TransparentBrush;
                TopPanelSeparatorBrush = TransparentBrush;
                SelectedListViewItemBrush = TextBrush;
                LowerPanelDockMargin = new Thickness(10, 0, 0, 0);
                InstallBrush = TextBrush;

                // add background setter to theme's TopPanelSearchBox style
                var style = new Style(typeof(SearchBox), basedOn: GetResource("TopPanelSearchBox") as Style);
                var brush = GetResource("TopPanelSearchBoxBackgroundBrush") as Brush;
                style.Setters.Add(new Setter(PluginSearchBox.BackgroundProperty, brush));
                TopPanelSearchBoxStyle = style;
            }
            // Neon
            else if (currentTheme == "8b15c46a-90c2-4fe5-9ebb-1ab25ba7fcb2")
            {
                TopPanelMargin = AdjustMargin(TopPanelMargin, Bottom: 10);
                TopPanelItemHeight = 31;
                TopPanelBackgroundBrush = GetResource("TopPanelBackgroundBrush") as Brush;
                TopPanelSearchBoxBgBrush = BrushFromString("#99212124");
                SelectedListViewItemBrush = null;
            }
            // Nova X
            else if (currentTheme == "Nova_X_0a95b7a3-00e4-412d-b301-f2fa3f98dfad")
            {
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 5, Bottom: 32, Right: 5);
                TopPanelHeight = 84;
                TopPanelSearchBoxWidth = 120;

                // use center grouping w/ order: menu items, search, progress
                TopPanelHorizontalAlignment = HorizontalAlignment.Center;
                TopPanelMenuItemsColumn = (int)TopPanelColumn.FarLeft;
                TopPanelSearchGapColumn = (int)TopPanelColumn.LGap;
                TopPanelSearchBoxColumn = (int)TopPanelColumn.Left;
                TopPanelProgressBarColumn = (int)TopPanelColumn.Right;
                TopPanelMinCenterGap = 15;
                TopPanelSearchGap = 5;

                TopPanelBorderBrush = TransparentBrush;
                TopPanelSeparatorBrush = TransparentBrush;
                SelectedListViewItemBrush = TextBrush;
                LowerPanelDockMargin = new Thickness(6, 6, 0, 0);
                ListViewControlsMargin = AdjustMargin(ListViewControlsMargin, Left: 2);
                InstallBrush = TextBrush;

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
                TopPanelMargin = AdjustMargin(TopPanelMargin, Bottom: 10);
                TopPanelItemHeight = 31;
                TopPanelSeparatorBrush = TransparentBrush;
                MainPanelSeparatorBrush = TransparentBrush;
                TopPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
                MainPanelBorderBrush = GetResource("PanelSeparatorBrush") as Brush;
            }
            // Seaside
            else if (currentTheme == "Seaside_df4e11f8-2347-4a2d-b835-757aec63e15c")
            {
                PopupBackgroundBrush = GetResource("BackgroundGradientBrush") as Brush;
                PopupDarkeningBrush = BrushFromString("#a02f2e30"); // trial-and-error value
                TopPanelHeight = 80;
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 5, Bottom: 20, Right: 5);
                TopPanelItemHeight = 45;
                TopPanelSearchBoxWidth = 200;
                TopPanelProgressBarHeight = 30;
                TopPanelProgressBarRadius = 3;

                // use center grouping with default order: search, menu, progress
                TopPanelHorizontalAlignment = HorizontalAlignment.Center;
                TopPanelMinCenterGap = 15;
                TopPanelSearchGap = 2;

                TopPanelBorderBrush = TransparentBrush;
                TopPanelSeparatorBrush = TransparentBrush;
                TopPanelBackgroundBrush = GetResource("TopPanelFadeBackgroundBrush") as Brush;
                MainPanelDarkeningBrush = BrushFromString("#701f1e20"); // trial-and-error value
                ListViewMargin = AdjustMargin(ListViewMargin, Left: -0.5);
                ListViewControlsMargin = AdjustMargin(ListViewControlsMargin, Left: 2);
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
                InstallBrush = TextBrush;
            }
            // Stardust
            else if 
            (
                currentTheme == "Stardust 2.0_1fb333b2-255b-43dd-aec1-8e2f2d5ea002" ||
                currentTheme == "Stardust_LegacyLayout"
            )
            {
                PopupBackgroundBrush = GetResource("BackgroundGradientBrush") as Brush;
                PopupDarkeningBrush = BrushCloneFromResource("PopupBackgroundBrush", Opacity: 0.4);
                TopPanelMargin = AdjustMargin(TopPanelMargin, Left: 5, Bottom: 15, Right: 5);
                TopPanelHeight = 65;
                TopPanelItemHeight = 35;
                TopPanelSearchBoxWidth = 120;
                TopPanelProgressBarHeight = 25;
                TopPanelProgressBarRadius = 3;
                TopPanelProgressBarMargin = new Thickness(3, 0, 3, 0);

                // use center grouping w/order: menu items, search, progress
                TopPanelHorizontalAlignment = HorizontalAlignment.Center;
                TopPanelMenuItemsColumn = (int)TopPanelColumn.FarLeft;
                TopPanelSearchGapColumn = (int)TopPanelColumn.LGap;
                TopPanelSearchBoxColumn = (int)TopPanelColumn.Left;
                TopPanelProgressBarColumn = (int)TopPanelColumn.Right;
                TopPanelMinCenterGap = 15;
                TopPanelSearchGap = 5;
                
                TopPanelBorderBrush = TransparentBrush;
                TopPanelSeparatorBrush = TransparentBrush;
                SelectedListViewItemBrush = TextBrush;
                ListViewControlsMargin = AdjustMargin(ListViewControlsMargin, Left: 2);

                // add background + fontweight + height setters to theme's SearchBox style
                var style = new Style(typeof(PluginSearchBox), basedOn: GetResource("SearchBoxTopPanel") as Style);
                var brush = GetResource("TopPanelSearchBoxBackgroundBrush") as Brush;
                style.Setters.Add(new Setter(PluginSearchBox.BackgroundProperty, brush));
                style.Setters.Add(new Setter(PluginSearchBox.FontWeightProperty, FontWeights.SemiBold));
                TopPanelSearchBoxStyle = style;
            }

            // . resolve optional search box height
            if (double.IsNaN(TopPanelSearchBoxHeight))
            {
                TopPanelSearchBoxHeight = TopPanelItemHeight;
            }
        }

        private Brush BrushCloneFromResource(string resourceName, double Opacity = 1)
        {
            var refBrush = GetResource(resourceName) as Brush;
            var brush = refBrush?.Clone();
            brush.Opacity = Opacity;
            if (refBrush.IsFrozen)
            {
                brush.Freeze();
            }
            return brush;
        }

        private Brush BrushFromString(string colorString, double Opacity = 1)
        {
            var brush = new BrushConverter().ConvertFromString(colorString) as Brush; 
            brush.Opacity = Opacity;
            return brush;
        }

        private Thickness AdjustMargin 
        (
            Thickness reference, 
            double? Left = null, double? Top = null, double? Right = null, double? Bottom = null
        )
        {
            var margin = reference;
            margin.Left = Left ?? margin.Left;
            margin.Top = Top ?? margin.Top;
            margin.Right = Right ?? margin.Right;
            margin.Bottom = Bottom ?? margin.Bottom;
            return margin;
        }

        private Dictionary<ListView,RoutedEventHandler> tweakFusionXThemeListViewHandler = new Dictionary<ListView, RoutedEventHandler>();

        public void TweakFusionXThemeListView(ListView listView)
        {
            if (currentThemeIsFusionX && listView != null && listView.IsLoaded)
            {
                try
                {
                    // hack removal of header margin.Bottom=8 (gap between list view header and list view items)
                    var presenter = WpfUtils.GetChildByName(listView, "PART_ScrollContentPresenter") as ScrollContentPresenter;
                    var margin = presenter.Margin; margin.Bottom = 0;
                    presenter.Margin = margin;
                }
                finally
                {
                    if (tweakFusionXThemeListViewHandler.ContainsKey(listView))
                    {
                        listView.Loaded -= tweakFusionXThemeListViewHandler[listView];
                        tweakFusionXThemeListViewHandler[listView] = null;
                    }
                }
            }
            else if (currentThemeIsFusionX && listView != null)
            {
                tweakFusionXThemeListViewHandler.Add(listView, (s, e) => TweakFusionXThemeListView(listView));
                listView.Loaded += tweakFusionXThemeListViewHandler[listView];
            }
        }

        private Dictionary<string, RoutedEventHandler> tweakDazeThemeProgressBarHandler = new Dictionary<string, RoutedEventHandler>();
        
        private void TweakDazeThemeProgressBar(UserControl view, string targetProgressBarName)
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
                finally
                {
                    if (tweakDazeThemeProgressBarHandler.ContainsKey(targetProgressBarName))
                    {
                        view.Loaded -= tweakDazeThemeProgressBarHandler[targetProgressBarName];
                        tweakDazeThemeProgressBarHandler[targetProgressBarName] = null;
                    }
                }
            }
            else if (view != null)
            {
                tweakDazeThemeProgressBarHandler.Add
                (
                    targetProgressBarName, (s, e) => TweakDazeThemeProgressBar(view, targetProgressBarName)
                );
                view.Loaded += tweakDazeThemeProgressBarHandler[targetProgressBarName];
            }
        }

    }

}
﻿using NowPlaying.ViewModels;
using System.Windows.Controls;

namespace NowPlaying.Views
{
    /// <summary>
    /// Interaction logic for EditMaxFillViewModel.xaml
    /// </summary>
    public partial class EditMaxFillView : UserControl
    {
        public EditMaxFillView(EditMaxFillViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

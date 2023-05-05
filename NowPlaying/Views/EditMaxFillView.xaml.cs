using NowPlaying.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NowPlaying.Views
{
    /// <summary>
    /// Interaction logic for AddCacheRootView.xaml
    /// </summary>
    public partial class AddCacheRootView : UserControl
    {
        public AddCacheRootView(AddCacheRootViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

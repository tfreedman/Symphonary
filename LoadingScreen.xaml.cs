using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfAnimatedControl;

namespace Symphonary
{
    /// <summary>
    /// Interaction logic for LoadingScreen.xaml
    /// </summary>
    public partial class LoadingScreen : Window
    {
        public LoadingScreen()
        {
            InitializeComponent();
        }

        public void setProgress(int i_ProgressPercentage)
        {
            boat.SetValue(Canvas.LeftProperty, 40 + (400 * ((double)i_ProgressPercentage / 100)));
            percentageText.Text = i_ProgressPercentage + "% Complete";
        }
    }
}

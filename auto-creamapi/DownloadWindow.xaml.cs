using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using auto_creamapi.Utils;

namespace auto_creamapi
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow
    {
        public DownloadWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        private void ProgressBar_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //MyLogger.Log.Information(ProgressBar.Value.ToString("N"));
        }
    }
}

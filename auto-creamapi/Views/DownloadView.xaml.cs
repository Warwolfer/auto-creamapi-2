using System.Windows;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;

namespace auto_creamapi.Views
{
    /// <summary>
    ///     Interaction logic for DownloadWindow.xaml
    /// </summary>
    [MvxWindowPresentation(Identifier = nameof(DownloadView), Modal = true)]
    public partial class DownloadView
    {
        public DownloadView()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        /*private void ProgressBar_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //MyLogger.Log.Information(ProgressBar.Value.ToString("N"));
        }*/
    }
}
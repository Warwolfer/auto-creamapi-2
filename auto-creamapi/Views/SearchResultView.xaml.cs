using MvvmCross.Platforms.Wpf.Presenters.Attributes;

namespace auto_creamapi.Views
{
    /// <summary>
    ///     Interaction logic for SearchResultWindow.xaml
    /// </summary>
    [MvxWindowPresentation(Identifier = nameof(SearchResultView), Modal = false)]
    public partial class SearchResultView
    {
        public SearchResultView()
        {
            InitializeComponent();
            //DgApps.ItemsSource = list;
        }

        /*private void OK_OnClick(object sender, RoutedEventArgs e)
        {
            GetSelectedApp();
        }

        private void DgApps_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GetSelectedApp();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GetSelectedApp()
        {
            if (Application.Current.MainWindow is MainWindow currentMainWindow)
            {
                var app = (SteamApp) DgApps.SelectedItem;
                if (app != null)
                {
                    MyLogger.Log.Information($"Successfully got app {app}");
                    //currentMainWindow.Game.Text = app.Name;
                    //currentMainWindow.AppId.Text = app.AppId.ToString();
                }
            }

            Close();
        }*/
    }
}
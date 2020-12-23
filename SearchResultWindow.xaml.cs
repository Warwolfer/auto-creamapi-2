using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using auto_creamapi.POCOs;
using auto_creamapi.Utils;
using NinjaNye.SearchExtensions;
using NinjaNye.SearchExtensions.Models;

namespace auto_creamapi
{
    /// <summary>
    /// Interaction logic for SearchResultWindow.xaml
    /// </summary>
    public partial class SearchResultWindow
    {
        public SearchResultWindow(IEnumerable<SteamApp> list)
        {
            InitializeComponent();
            DgApps.ItemsSource = list;
        }

        private void OK_OnClick(object sender, RoutedEventArgs e)
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
                    currentMainWindow.Game.Text = app.Name;
                    currentMainWindow.AppId.Text = app.AppId.ToString();
                }
            }

            Close();
        }
    }
}

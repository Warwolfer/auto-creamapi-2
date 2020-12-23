using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using auto_creamapi.Model;
using auto_creamapi.POCOs;
using auto_creamapi.Utils;
using Microsoft.Win32;

namespace auto_creamapi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string DefaultLangSelection = "english";
        private static CacheModel _cacheModel;
        private static CreamConfigModel _configModel;
        private static CreamDllModel _dllModel;

        public MainWindow()
        {
            PreInit();
            InitializeComponent();
            new Action(PostInit).Invoke();
        }

        private static void PreInit()
        {
            _cacheModel = CacheModel.Instance;
            _configModel = CreamConfigModel.Instance;
            _dllModel = CreamDllModel.Instance;
        }

        private async void PostInit()
        {
            MainWindowGrid.IsEnabled = false;
            var task = _dllModel.Initialize();
            await task;
            _cacheModel.Languages.ForEach(x => Lang.Items.Add(x));
            Lang.SelectedItem = DefaultLangSelection;
            SteamDb.IsChecked = true;
            task.Wait();
            MainWindowGrid.IsEnabled = true;
            Status.Text = "Ready.";
        }

        /// <summary>
        /// Looks up the AppID of the specified game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            var app = _cacheModel.GetAppByName(Game.Text);
            if (app != null)
            {
                Game.Text = app.Name;
                AppId.Text = app.AppId.ToString();
            }
            else
            {
                var listOfAppsByName = _cacheModel.GetListOfAppsByName(Game.Text);
                var searchWindow = new SearchResultWindow(listOfAppsByName);
                searchWindow.Show();
            }
        }

        private void DllPath_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MyOpenFile();
        }

        /// <summary>
        /// Opens a file chooser to select the path to steam_api(64).dll.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            MyOpenFile();
        }

        private void MyOpenFile()
        {
            Status.Text = "Waiting for file...";
            var dialog = new OpenFileDialog
            {
                Filter = "SteamAPI DLL|steam_api.dll;steam_api64.dll|" +
                         "All files (*.*)|*.*",
                Multiselect = false,
                Title = "Select SteamAPI DLL..."
            };
            if (dialog.ShowDialog() == true)
            {
                //Console.WriteLine(dialog.FileName);
                var filePath = dialog.FileName;
                DllPath.Text = filePath;
                var dirPath = Path.GetDirectoryName(filePath);
                if (dirPath != null)
                {
                    _configModel.ReadFile(Path.Combine(dirPath, "cream_api.ini"));
                    ResetFormData();
                    _dllModel.TargetPath = dirPath;
                    _dllModel.CheckExistence();
                    CheckExistance();
                    Status.Text = "Ready.";
                }
            }
        }

        /// <summary>
        /// Gets a list of DLCs for the specified AppID.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void GetListOfDlc_Click(object sender, RoutedEventArgs e)
        {
            Status.Text = "Trying to get DLC...";
            if (int.TryParse(AppId.Text, out var appId))
            {
                if (appId > 0)
                {
                    var app = new SteamApp() {AppId = appId, Name = Game.Text};
                    var steamDbIsChecked = SteamDb.IsChecked != null && (bool) SteamDb.IsChecked;
                    var task = _cacheModel.GetListOfDlc(app, steamDbIsChecked);
                    MainWindowGrid.IsEnabled = false;
                    var listOfDlc = await task;
                    if (task.IsCompletedSuccessfully)
                    {
                        var result = "";
                        listOfDlc.Sort((app1, app2) => app1.AppId.CompareTo(app2.AppId));
                        listOfDlc.ForEach(x => result += $"{x.AppId}={x.Name}\n");
                        ListOfDlcs.Text = result;
                        Status.Text = $"Got DLC for AppID {appId}";
                    }
                    else
                    {
                        Status.Text = $"Could not get DLC for AppID {appId}";
                    }
                    MainWindowGrid.IsEnabled = true;
                }
                else
                {
                    Status.Text = $"Could not get DLC for AppID {appId}";
                    MyLogger.Log.Error($"GetListOfDlc: Invalid AppID {appId}");
                }
            }
            else
            {
                Status.Text = $"Could not get DLC: Invalid AppID {appId}";
                MyLogger.Log.Error($"GetListOfDlc: Invalid AppID {appId}");
            }
        }

        /// <summary>
        /// Saves form data to cream_api.ini.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Status.Text = "Saving...";
            _configModel.SetConfigData(
                    Convert.ToInt32(AppId.Text),
                    Lang.SelectedItem.ToString(),
                    UnlockAll.IsChecked != null && (bool) UnlockAll.IsChecked,
                    ExtraProtection.IsChecked != null && (bool) ExtraProtection.IsChecked,
                    ForceOffline.IsChecked != null && (bool) ForceOffline.IsChecked,
                    ListOfDlcs.Text
                );
            _configModel.SaveFile();
            _dllModel.Save();
            CheckExistance();
            Status.Text = "Saving successful.";
        }

        /// <summary>
        /// Resets form data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Status.Text = "Resetting...";
            ResetFormData();
            CheckExistance();
            Status.Text = "Resetting successful.";
        }

        /// <summary>
        /// Gets app name on id change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyEventArgs"></param>
        private void AppId_OnTextChanged(object sender, KeyEventArgs keyEventArgs)
        {
            SetNameById();
        }

        private void SetNameById()
        {
            if (int.TryParse(AppId.Text, out var appId))
            {
                if (appId > 0)
                {
                    var app = _cacheModel.GetAppById(appId);
                    if (app != null)
                    {
                        Game.Text = app.Name;
                    }
                    else
                    {
                        /*Game.Text = "";
                        AppId.Text = "";*/
                        MyLogger.Log.Error($"No app found for ID {appId}");
                    }
                }
                else
                {
                    /*Game.Text = "";
                    AppId.Text = "";*/
                    MyLogger.Log.Error($"SetNameById: Invalid AppID {appId}");
                }
            }
        }

        private void ResetFormData()
        {
            var configAppId = _configModel.Config.AppId;
            AppId.Text = configAppId.ToString();
            Game.Text = configAppId > 0 ? _cacheModel.GetAppById(configAppId).Name : "";
            Lang.SelectedItem = _configModel.Config.Language;
            UnlockAll.IsChecked = _configModel.Config.UnlockAll; // public bool UnlockAll;
            ExtraProtection.IsChecked = _configModel.Config.ExtraProtection; // public bool ExtraProtection;
            ForceOffline.IsChecked = _configModel.Config.ForceOffline; // public bool ForceOffline;
            // public Dictionary<int, string> DlcList;
            var dlcListString = "";
            if (_configModel.Config.DlcList.Count > 0)
            {
                foreach (var (id, name) in _configModel.Config.DlcList)
                {
                    dlcListString += $"{id}={name}\n";
                }
            }

            ListOfDlcs.Text = dlcListString;
        }

        private void CheckExistance()
        {
            CreamApiApplied.IsChecked = _dllModel.CreamApiApplied();
            ConfigExists.IsChecked = _configModel.ConfigExists();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            // UseShellExecute = true;
            Status.Text = "Opening URL...";
            if (int.TryParse(AppId.Text, out var appId))
            {
                if (appId > 0)
                {
                    var searchTerm = appId;//$"{Game.Text.Replace(" ", "+")}+{appId}";
                    var destinationUrl = 
                        "https://cs.rin.ru/forum/search.php?keywords=" +
                        searchTerm +
                        "&terms=any&fid[]=10&sf=firstpost&sr=topics&submit=Search";
                    var uri = new Uri(destinationUrl);
                    var process = new ProcessStartInfo(uri.AbsoluteUri)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(process);
                }
                else
                {
                    MyLogger.Log.Error($"OpenURL: Invalid AppID {appId}");
                    Status.Text = $"Could not open URL: Invalid AppID {appId}";
                }
            }
            else
            {
                MyLogger.Log.Error($"OpenURL: Invalid AppID {appId}");
                Status.Text = $"Could not open URL: Invalid AppID {appId}";
            }
        }
    }
}

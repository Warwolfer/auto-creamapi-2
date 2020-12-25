using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using auto_creamapi.Models;
using auto_creamapi.Services;
using auto_creamapi.Utils;
using auto_creamapi.Views;
using Microsoft.Win32;
using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace auto_creamapi.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        private readonly ICacheService _cache;
        private readonly ICreamConfigService _config;
        private readonly ICreamDllService _dll;
        private readonly IDownloadCreamApiService _download;
        private bool _mainWindowEnabled;
        private string _dllPath;
        private string _gameName;
        private int _appId;
        private string _lang;
        private bool _offline;
        private bool _extraprotection;
        private bool _unlockall;
        private bool _useSteamDb;
        private ObservableCollection<SteamApp> _dlcs;
        private bool _dllApplied;
        private bool _configExists;
        private string _status;
        private ObservableCollection<string> _languages;
        private const string DefaultLanguageSelection = "english";
        private const string DlcRegexPattern = @"(?<id>.*) *= *(?<name>.*)";

        public MainViewModel(ICacheService cache, ICreamConfigService config, ICreamDllService dll,
            IDownloadCreamApiService download)
        {
            _cache = cache;
            _config = config;
            _dll = dll;
            _download = download;
        }

        public override async Task Initialize()
        {
            await base.Initialize();
            Languages = new ObservableCollection<string>(_cache.Languages);
            ResetForm();
            _download.Initialize();
            _dll.Initialize();
            Lang = DefaultLanguageSelection;
            UseSteamDb = true;
            MainWindowEnabled = true;
            Status = "Ready.";
        }

        // // COMMANDS // //

        public IMvxCommand OpenFileCommand => new MvxCommand(OpenFile);

        private void OpenFile()
        {
            Status = "Waiting for file...";
            var dialog = new OpenFileDialog
            {
                Filter = "SteamAPI DLL|steam_api.dll;steam_api64.dll|" +
                         "All files (*.*)|*.*",
                Multiselect = false,
                Title = "Select SteamAPI DLL..."
            };
            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                DllPath = filePath;
                var dirPath = Path.GetDirectoryName(filePath);
                if (dirPath != null)
                {
                    _config.ReadFile(Path.Combine(dirPath, "cream_api.ini"));
                    ResetForm();
                    _dll.TargetPath = dirPath;
                    _dll.CheckIfDllExistsAtTarget();
                    CheckExistence();
                    Status = "Ready.";
                }
            }
        }

        public IMvxCommand SearchCommand => new MvxCommand(Search);

        private void Search()
        {
            var app = _cache.GetAppByName(GameName);
            if (app != null)
            {
                GameName = app.Name;
                AppId = app.AppId;
            }

            /*else
            {
                var listOfAppsByName = _cache.GetListOfAppsByName(GameName);
                var searchWindow = new SearchResultWindow(listOfAppsByName);
                searchWindow.Show();
            }*/
        }

        public IMvxCommand GetListOfDlcCommand => new MvxAsyncCommand(GetListOfDlc);

        private async Task GetListOfDlc()
        {
            Status = "Trying to get DLC...";
            if (AppId > 0)
            {
                var app = new SteamApp() {AppId = AppId, Name = GameName};
                var task = _cache.GetListOfDlc(app, UseSteamDb);
                MainWindowEnabled = false;
                var listOfDlc = await task;
                var result = "";
                if (task.IsCompletedSuccessfully)
                {
                    listOfDlc.Sort((app1, app2) => app1.AppId.CompareTo(app2.AppId));
                    listOfDlc.ForEach(x => result += $"{x.AppId}={x.Name}\n");
                    Dlcs = result;
                    Status = $"Got DLC for AppID {AppId}";
                }
                else
                {
                    Status = $"Could not get DLC for AppID {AppId}";
                }

                MainWindowEnabled = true;
            }
            else
            {
                Status = $"Could not get DLC for AppID {AppId}";
                MyLogger.Log.Error($"GetListOfDlc: Invalid AppID {AppId}");
            }
        }

        public IMvxCommand SaveCommand => new MvxCommand(Save);

        private void Save()
        {
            Status = "Saving...";
            _config.SetConfigData(
                AppId,
                Lang,
                Unlockall,
                Extraprotection,
                Offline,
                Dlcs
            );
            _config.SaveFile();
            _dll.Save();
            CheckExistence();
            Status = "Saving successful.";
        }

        public IMvxCommand ResetFormCommand => new MvxCommand(ResetForm);

        private void ResetForm()
        {
            AppId = _config.Config.AppId;
            Lang = _config.Config.Language;
            Unlockall = _config.Config.UnlockAll;
            Extraprotection = _config.Config.ExtraProtection;
            Offline = _config.Config.ForceOffline;
            var configDlcList = _config.Config.DlcList;
            var result = "";
            foreach (var x in configDlcList)
            {
                result += $"{x.AppId}={x.Name}\n";
            }

            Dlcs = result;
        }

        public IMvxCommand GoToForumThreadCommand => new MvxCommand(GoToForumThread);

        private void GoToForumThread()
        {
            Status = "Opening URL...";
            if (AppId > 0)
            {
                var searchTerm = AppId; //$"{GameName.Replace(" ", "+")}+{appId}";
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
                MyLogger.Log.Error($"OpenURL: Invalid AppID {AppId}");
                Status = $"Could not open URL: Invalid AppID {AppId}";
            }
        }

        private void CheckExistence()
        {
            DllApplied = _dll.CreamApiApplied();
            ConfigExists = _config.ConfigExists();
        }

        // // ATTRIBUTES // //

        public bool MainWindowEnabled
        {
            get => _mainWindowEnabled;
            set
            {
                _mainWindowEnabled = value;
                RaisePropertyChanged(() => MainWindowEnabled);
            }
        }

        public string DllPath
        {
            get => _dllPath;
            set
            {
                _dllPath = value;
                RaisePropertyChanged(() => DllPath);
            }
        }

        public string GameName
        {
            get => _gameName;
            set
            {
                _gameName = value;
                RaisePropertyChanged(() => GameName);
                //MyLogger.Log.Debug($"GameName: {value}");
            }
        }

        public int AppId
        {
            get => _appId;
            set
            {
                _appId = value;
                RaisePropertyChanged(() => AppId);
                if (value > 0) SetNameById();
            }
        }

        private void SetNameById()
        {
            var appById = _cache.GetAppById(_appId);
            GameName = appById != null ? appById.Name : "";
        }

        public string Lang
        {
            get => _lang;
            set
            {
                _lang = value;
                RaisePropertyChanged(() => Lang);
                //MyLogger.Log.Debug($"Lang: {value}");
            }
        }

        public bool Offline
        {
            get => _offline;
            set
            {
                _offline = value;
                RaisePropertyChanged(() => Offline);
            }
        }

        public bool Extraprotection
        {
            get => _extraprotection;
            set
            {
                _extraprotection = value;
                RaisePropertyChanged(() => Extraprotection);
            }
        }

        public bool Unlockall
        {
            get => _unlockall;
            set
            {
                _unlockall = value;
                RaisePropertyChanged(() => Unlockall);
            }
        }

        public bool UseSteamDb
        {
            get => _useSteamDb;
            set
            {
                _useSteamDb = value;
                RaisePropertyChanged(() => UseSteamDb);
            }
        }

        /*public List<SteamApp> Dlcs
        {
            get => _dlcs;
            set
            {
                _dlcs = value;
                RaisePropertyChanged(Dlcs);
                MyLogger.Log.Debug($"Dlcs: {value}");
            }
        }*/

        public string Dlcs
        {
            get => DlcListToString(_dlcs);
            set
            {
                _dlcs = StringToDlcList(value);
                RaisePropertyChanged();
                //MyLogger.Log.Debug($"Dlcs: {DlcListToString(_dlcs)}");
            }
        }

        private static ObservableCollection<SteamApp> StringToDlcList(string value)
        {
            var result = new List<SteamApp>();
            var expression = new Regex(@"(?<id>.*) *= *(?<name>.*)");
            using var reader = new StringReader(value);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = expression.Match(line);
                if (match.Success)
                {
                    result.Add(new SteamApp
                    {
                        AppId = int.Parse(match.Groups["id"].Value),
                        Name = match.Groups["name"].Value
                    });
                }
            }

            return new ObservableCollection<SteamApp>(result);
        }

        private static string DlcListToString(IEnumerable<SteamApp> value)
        {
            return value.Aggregate("", (current, steamApp) => current + $"{steamApp}\n");
        }

        public bool DllApplied
        {
            get => _dllApplied;
            set
            {
                _dllApplied = value;
                RaisePropertyChanged(() => DllApplied);
            }
        }

        public bool ConfigExists
        {
            get => _configExists;
            set
            {
                _configExists = value;
                RaisePropertyChanged(() => ConfigExists);
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                RaisePropertyChanged(() => Status);
            }
        }

        public ObservableCollection<string> Languages
        {
            get => _languages;
            set
            {
                _languages = value;
                RaisePropertyChanged(() => Languages);
            }
        }
    }
}
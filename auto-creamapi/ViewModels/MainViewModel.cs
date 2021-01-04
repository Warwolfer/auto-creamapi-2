using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using auto_creamapi.Models;
using auto_creamapi.Services;
using auto_creamapi.Utils;
using Microsoft.Win32;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace auto_creamapi.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        private readonly ICacheService _cache;
        private readonly ICreamConfigService _config;

        private readonly ICreamDllService _dll;
        private readonly IMvxNavigationService _navigationService;
        private int _appId;
        private bool _configExists;
        private ObservableCollection<SteamApp> _dlcs;
        private bool _dllApplied;
        private string _dllPath;
        private bool _extraProtection;
        private string _gameName;
        private string _lang;
        private ObservableCollection<string> _languages;

        //private readonly IDownloadCreamApiService _download;
        private bool _mainWindowEnabled;
        private bool _offline;
        private string _status;
        private bool _unlockAll;

        private bool _useSteamDb;

        private bool _ignoreUnknown;
        //private const string DlcRegexPattern = @"(?<id>.*) *= *(?<name>.*)";

        public MainViewModel(ICacheService cache, ICreamConfigService config, ICreamDllService dll,
            IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;
            _cache = cache;
            _config = config;
            _dll = dll;
            //_download = download;
        }

        public override async Task Initialize()
        {
            _config.Initialize();
            var tasks = new List<Task> {base.Initialize(), _cache.Initialize()};
            if (!File.Exists("steam_api.dll") | !File.Exists("steam_api64.dll"))
                tasks.Add(_navigationService.Navigate<DownloadViewModel>());
            tasks.Add(_dll.Initialize());
            await Task.WhenAll(tasks);
            Languages = new ObservableCollection<string>(Misc.DefaultLanguages);
            ResetForm();
            UseSteamDb = true;
            MainWindowEnabled = true;
            Status = "Ready.";
        }

        // // COMMANDS // //

        public IMvxCommand OpenFileCommand => new MvxAsyncCommand(OpenFile);

        public IMvxCommand SearchCommand => new MvxAsyncCommand(async () => await Search()); //Command(Search);

        public IMvxCommand GetListOfDlcCommand => new MvxAsyncCommand(GetListOfDlc);

        public IMvxCommand SaveCommand => new MvxCommand(Save);

        public IMvxCommand ResetFormCommand => new MvxCommand(ResetForm);

        public IMvxCommand GoToForumThreadCommand => new MvxCommand(GoToForumThread);

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
                SetNameById();
            }
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

        public bool ExtraProtection
        {
            get => _extraProtection;
            set
            {
                _extraProtection = value;
                RaisePropertyChanged(() => ExtraProtection);
            }
        }

        public bool UnlockAll
        {
            get => _unlockAll;
            set
            {
                _unlockAll = value;
                RaisePropertyChanged(() => UnlockAll);
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

        public ObservableCollection<SteamApp> Dlcs
        {
            get => _dlcs;
            set
            {
                _dlcs = value;
                RaisePropertyChanged(() => Dlcs);
            }
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

        public bool IgnoreUnknown
        {
            get => _ignoreUnknown;
            set
            {
                _ignoreUnknown = value;
                RaisePropertyChanged(() => IgnoreUnknown);
            }
        }

        private async Task OpenFile()
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
                    CheckSetupStatus();
                    if (!ConfigExists)
                    {
                        var separator = Path.DirectorySeparatorChar;
                        var strings = new List<string>(dirPath.Split(separator));
                        var index = strings.Contains("common") ? strings.FindIndex(x => x.Equals("common")) + 1 : -1;
                        if (index == -1)
                            index = strings.Contains("steamapps")
                                ? strings.FindIndex(x => x.Equals("steamapps")) + 2
                                : -1;
                        var s = index > -1 ? strings[index] : null;
                        if (s != null) GameName = s;
                        await Search();
                        await GetListOfDlc();
                    }

                    Status = "Ready.";
                }
            }
            else
            {
                Status = "File selection canceled.";
            }
        }

        private async Task Search()
        {
            if (!string.IsNullOrEmpty(GameName))
            {
                var app = _cache.GetAppByName(GameName);
                if (app != null)
                {
                    GameName = app.Name;
                    AppId = app.AppId;
                }
                else
                {
                    MainWindowEnabled = false;
                    var navigate = _navigationService.Navigate<SearchResultViewModel, IEnumerable<SteamApp>, SteamApp>(
                        _cache.GetListOfAppsByName(GameName));
                    await navigate;
                    var navigateResult = navigate.Result;
                    if (navigateResult != null)
                    {
                        GameName = navigateResult.Name;
                        AppId = navigateResult.AppId;
                    }
                }

                await GetListOfDlc();
            }
            else
            {
                MyLogger.Log.Warning("Empty game name, cannot initiate search!");
            }

            MainWindowEnabled = true;
        }

        private async Task GetListOfDlc()
        {
            Status = "Trying to get DLC...";
            if (AppId > 0)
            {
                var app = new SteamApp {AppId = AppId, Name = GameName};
                var task = _cache.GetListOfDlc(app, UseSteamDb, IgnoreUnknown);
                MainWindowEnabled = false;
                var listOfDlc = await task;
                if (task.IsCompletedSuccessfully)
                {
                    listOfDlc.Sort((app1, app2) => app1.AppId.CompareTo(app2.AppId));
                    Dlcs = new ObservableCollection<SteamApp>(listOfDlc);
                    Status = $"Got DLC for AppID {AppId} (Count: {Dlcs.Count})";
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

        private void Save()
        {
            Status = "Saving...";
            _config.SetConfigData(
                AppId,
                Lang,
                UnlockAll,
                ExtraProtection,
                Offline,
                Dlcs
            );
            _config.SaveFile();
            _dll.Save();
            CheckSetupStatus();
            Status = "Saving successful.";
        }

        private void ResetForm()
        {
            AppId = _config.Config.AppId;
            Lang = _config.Config.Language;
            UnlockAll = _config.Config.UnlockAll;
            ExtraProtection = _config.Config.ExtraProtection;
            Offline = _config.Config.ForceOffline;
            Dlcs = new ObservableCollection<SteamApp>(_config.Config.DlcList);
            Status = "Changes have been reset.";
        }

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

        private void CheckSetupStatus()
        {
            DllApplied = _dll.CreamApiApplied();
            ConfigExists = _config.ConfigExists();
        }

        private void SetNameById()
        {
            if (_appId > 0)
            {
                var appById = _cache.GetAppById(_appId);
                GameName = appById != null ? appById.Name : "";
            }
            else GameName = "";
        }
    }
}
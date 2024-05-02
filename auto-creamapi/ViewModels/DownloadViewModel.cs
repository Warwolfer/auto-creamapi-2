using System;
using System.Threading.Tasks;
using System.Windows;
using auto_creamapi.Messenger;
using auto_creamapi.Services;
using auto_creamapi.Utils;
using Microsoft.Extensions.Logging;
using MvvmCross.Navigation;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace auto_creamapi.ViewModels
{
    public class DownloadViewModel : MvxNavigationViewModel
    {
        private readonly IDownloadCreamApiService _download;
        private readonly IMvxNavigationService _navigationService;
        private readonly MvxSubscriptionToken _token;
        private readonly ILogger<DownloadViewModel> _logger;
        private string _filename;

        private string _info;
        private double _progress;

        public DownloadViewModel(ILoggerFactory loggerFactory, IMvxNavigationService navigationService,
            IDownloadCreamApiService download, IMvxMessenger messenger) : base(loggerFactory, navigationService)
        {
            _navigationService = navigationService;
            _logger = loggerFactory.CreateLogger<DownloadViewModel>();
            _download = download;
            _token = messenger.Subscribe<ProgressMessage>(OnProgressMessage);
            _logger.LogDebug("{Count}", messenger.CountSubscriptionsFor<ProgressMessage>());
        }

        public string InfoLabel
        {
            get => _info;
            set
            {
                _info = value;
                RaisePropertyChanged(() => InfoLabel);
            }
        }

        public string FilenameLabel
        {
            get => _filename;
            set
            {
                _filename = value;
                RaisePropertyChanged(() => FilenameLabel);
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                RaisePropertyChanged(() => Progress);
                RaisePropertyChanged(() => ProgressPercent);
            }
        }

        public string ProgressPercent => _progress.ToString("P2");

        public override void Prepare()
        {
            InfoLabel = "Please wait...";
            FilenameLabel = "";
            Progress = 0.0;
        }

        public override async Task Initialize()
        {
            MessageBox.Show("Could not download CreamAPI!\nPlease add CreamAPI DLLs manually!\nShutting down...",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _token.Dispose();
            await _navigationService.Close(this).ConfigureAwait(false);
            Environment.Exit(0);
        }

        private void OnProgressMessage(ProgressMessage obj)
        {
            InfoLabel = obj.Info;
            FilenameLabel = obj.Filename;
            Progress = obj.PercentComplete;
        }
    }
}
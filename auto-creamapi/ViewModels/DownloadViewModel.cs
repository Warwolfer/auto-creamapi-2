using System.Threading.Tasks;
using auto_creamapi.Messenger;
using auto_creamapi.Services;
using auto_creamapi.Utils;
using MvvmCross.Logging;
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
        private string _filename;

        private string _info;
        private double _progress;

        public DownloadViewModel(IMvxLogProvider logProvider, IMvxNavigationService navigationService,
            IDownloadCreamApiService download, IMvxMessenger messenger) : base(logProvider, navigationService)
        {
            _navigationService = navigationService;
            _download = download;
            _token = messenger.Subscribe<ProgressMessage>(OnProgressMessage);
            MyLogger.Log.Debug(messenger.CountSubscriptionsFor<ProgressMessage>().ToString());
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

        public override async Task Initialize()
        {
            await base.Initialize().ConfigureAwait(false);
            InfoLabel = "Please wait...";
            FilenameLabel = "";
            Progress = 0.0;
            var download = _download.Download(Secrets.ForumUsername, Secrets.ForumPassword);
            var filename = await download.ConfigureAwait(false);
            /*var extract = _download.Extract(filename);
            await extract;*/
            var extract = _download.Extract(filename);
            await extract.ConfigureAwait(false);
            _token.Dispose();
            await _navigationService.Close(this).ConfigureAwait(false);
        }

        private void OnProgressMessage(ProgressMessage obj)
        {
            //MyLogger.Log.Debug($"{obj.Filename}: {obj.BytesTransferred}");
            InfoLabel = obj.Info;
            FilenameLabel = obj.Filename;
            Progress = obj.PercentComplete;
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using auto_creamapi.Models;
using auto_creamapi.Utils;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace auto_creamapi.ViewModels
{
    public class SearchResultViewModel : MvxNavigationViewModel<IEnumerable<SteamApp>>,
        IMvxViewModel<IEnumerable<SteamApp>, SteamApp>
    {
        private readonly IMvxNavigationService _navigationService;
        private readonly ILogger<SearchResultViewModel> _logger;
        private IEnumerable<SteamApp> _steamApps;

        /*public override async Task Initialize()
        {
            await base.Initialize();
        }*/
        public SearchResultViewModel(ILoggerFactory loggerFactory, IMvxNavigationService navigationService) : base(
            loggerFactory, navigationService)
        {
            _navigationService = navigationService;
            _logger = loggerFactory.CreateLogger<SearchResultViewModel>();
        }

        public IEnumerable<SteamApp> Apps
        {
            get => _steamApps;
            set
            {
                _steamApps = value;
                RaisePropertyChanged(() => Apps);
            }
        }

        public SteamApp Selected
        {
            get;
            set;
            //RaisePropertyChanged(Selected);
        }

        public IMvxCommand SaveCommand => new MvxAsyncCommand(Save);

        public IMvxCommand CloseCommand => new MvxCommand(Close);

        public override void Prepare(IEnumerable<SteamApp> parameter)
        {
            Apps = parameter;
        }

        public TaskCompletionSource<object> CloseCompletionSource { get; set; }

        public override void ViewDestroy(bool viewFinishing = true)
        {
            if (viewFinishing && CloseCompletionSource?.Task.IsCompleted == false &&
                !CloseCompletionSource.Task.IsFaulted)
            {
                CloseCompletionSource?.TrySetCanceled();
            }

            base.ViewDestroy(viewFinishing);
        }

        private async Task Save()
        {
            if (Selected != null)
            {
                _logger.LogInformation("Successfully got app {Selected}", Selected);
                await _navigationService.Close(this, Selected).ConfigureAwait(false);
            }
        }

        private void Close()
        {
            //throw new System.NotImplementedException();
            _navigationService.Close(this);
        }
    }
}
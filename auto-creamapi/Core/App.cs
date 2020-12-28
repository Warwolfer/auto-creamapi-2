using auto_creamapi.ViewModels;
using MvvmCross.IoC;
using MvvmCross.ViewModels;

namespace auto_creamapi.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            //Mvx.IoCProvider.RegisterType<ICacheService, CacheService>();
            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();

            RegisterAppStart<MainViewModel>();
        }
    }
}
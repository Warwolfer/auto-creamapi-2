using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Core;

namespace auto_creamapi
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<MvxWpfSetup<Core.App>>();
        }
    }
}
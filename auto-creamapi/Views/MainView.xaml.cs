using MvvmCross.Platforms.Wpf.Presenters.Attributes;

namespace auto_creamapi.Views
{
    [MvxContentPresentation(WindowIdentifier = nameof(MainView), StackNavigation = false)]
    // ReSharper disable once UnusedType.Global
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
        }
    }
}
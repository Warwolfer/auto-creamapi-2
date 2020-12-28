using MvvmCross.Platforms.Wpf.Presenters.Attributes;

namespace auto_creamapi
{
    [MvxWindowPresentation(Identifier = nameof(MainWindow), Modal = false)]
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
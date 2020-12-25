using System.Windows;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;

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
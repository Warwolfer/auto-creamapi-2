using MvvmCross.Platforms.Wpf.Presenters.Attributes;

namespace auto_creamapi.Views
{
    /// <summary>
    ///     Interaction logic for SearchResultWindow.xaml
    /// </summary>
    [MvxWindowPresentation(Identifier = nameof(SearchResultView), Modal = false)]
    public partial class SearchResultView
    {
        public SearchResultView()
        {
            InitializeComponent();
        }
    }
}
using FileMatcherApp.Filters;

namespace FileMatcherApp.Views
{
    /// <summary>
    /// Interaction logic for ViewFilter.xaml
    /// </summary>
    public partial class ViewFilterWindow
    {
        #region Constructors

        public ViewFilterWindow(DuplicatesFilter filterViewModel)
        {
            InitializeComponent();
            DataContext = filterViewModel;
        }

        #endregion
    }
}

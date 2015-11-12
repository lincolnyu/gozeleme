namespace FileMatcherApp
{
    /// <summary>
    /// Interaction logic for ViewFilter.xaml
    /// </summary>
    public partial class ViewFilterWindow
    {
        #region Constructors

        public ViewFilterWindow(DuplicatesSummary.ViewFilter filterViewModel)
        {
            InitializeComponent();
            DataContext = filterViewModel;
        }

        #endregion
    }
}

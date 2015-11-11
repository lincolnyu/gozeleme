namespace FileMatcher
{
    /// <summary>
    /// Interaction logic for ViewFilter.xaml
    /// </summary>
    public partial class ViewFilterWindow
    {
        #region Constructors

        public ViewFilterWindow(DuplicatesSummary.ViewFilter fileterViewModel)
        {
            InitializeComponent();
            DataContext = fileterViewModel;
        }

        #endregion
    }
}

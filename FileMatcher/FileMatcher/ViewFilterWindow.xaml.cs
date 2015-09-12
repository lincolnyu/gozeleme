namespace FileMatcher
{
    /// <summary>
    /// Interaction logic for ViewFilter.xaml
    /// </summary>
    public partial class ViewFilterWindow
    {
        #region Constructors

        public ViewFilterWindow(RedunduncySummary.ViewFilter fileterViewModel)
        {
            InitializeComponent();
            DataContext = fileterViewModel;
        }

        #endregion
    }
}

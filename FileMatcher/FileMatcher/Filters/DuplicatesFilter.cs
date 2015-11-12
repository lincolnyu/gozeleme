namespace FileMatcherApp.Filters
{
    public class DuplicatesFilter
    {
        private bool _isEnabled;

        private double _totalDuplicateMbs;

        public delegate void FilterChangedEventHandler();

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (_isEnabled !=value)
                {
                    _isEnabled = value;
                    RaiseFilterChangedEvent();
                }
            }
        }

        public double TotalDuplicateMbs
        {
            get
            {
                return _totalDuplicateMbs;
            }
            set
            {
                if (_totalDuplicateMbs != value)
                {
                    _totalDuplicateMbs = value;
                    RaiseFilterChangedEvent();
                }
            }
        }

        public event FilterChangedEventHandler FilterChanged;

        public bool Predicate(object o)
        {
            if (!IsEnabled)
            {
                return true;
            }
            var fileInfo = (FileInfoEx)o;
            var size = fileInfo.Length;
            var totalDuplicateBytes = TotalDuplicateMbs * 1024 * 1024;
            return size > totalDuplicateBytes;
        }

        private void RaiseFilterChangedEvent()
        {
            if (FilterChanged !=null)
            {
                FilterChanged();
            }
        }
    }
}

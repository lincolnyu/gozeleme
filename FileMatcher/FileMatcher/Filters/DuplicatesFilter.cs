using System;
using FileMatcherApp.Models;

namespace FileMatcherApp.Filters
{
    public class DuplicatesFilter
    {
        public delegate void FilterChangedEventHandler();

        private bool _isEnabled;

        private double _totalDuplicateMbs;

        private bool _raisingEvent;

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
                if (Math.Abs(_totalDuplicateMbs - value) > double.Epsilon)
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

            if (fileInfo.Duplicates >= 2)
            {
                var size = fileInfo.Length * (fileInfo.Duplicates - 1);
                var totalDuplicateBytes = TotalDuplicateMbs * 1024 * 1024;
                var selected = size > totalDuplicateBytes;
                return selected;
            }
            return false;
        }

        public bool Emerged(FileInfoEx fi)
        {
            var dup = fi.Duplicates;
            var newSize = dup * fi.Length;
            var oldSize = (dup - 1) * fi.Length;
            var totalDuplicateBytes = TotalDuplicateMbs * 1024 * 1024;
            return newSize > totalDuplicateBytes && 
                (oldSize < totalDuplicateBytes || dup<3);
        }

        private void RaiseFilterChangedEvent()
        {
            if (_raisingEvent)
            {
                return;
            }
            _raisingEvent = true;
            if (FilterChanged !=null)
            {
                FilterChanged();
            }
            _raisingEvent = false;
        }
    }
}

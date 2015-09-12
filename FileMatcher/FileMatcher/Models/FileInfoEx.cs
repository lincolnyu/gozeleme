using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace FileMatcher
{
    public class FileInfoEx : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        public string Name
        {
            get { return _fileInfo.Name; }
        }

        public string DirectoryName
        {
            get { return _fileInfo.DirectoryName; }
        }

        public long Length
        {
            get { return _fileInfo.Length; }
        }

        public string DisplayFileSize
        {
            get
            {
#if false
                var nfo = new NumberFormatInfo
                    {
                        CurrencyGroupSeparator = ",",
                        CurrencyGroupSizes = new[] {3},
                        CurrencySymbol = ""
                    };
                return Length.ToString("N0", nfo);
#else
                return Length.ToString("###,###,###,##0");
#endif
            }
        }

        public string FullName
        {
            get { return _fileInfo.FullName; }
        }

        public bool IsSelectedToDelete
        {
            get { return _isSelectedToDelete; }
            set 
            {
                if (_isSelectedToDelete == value)
                {
                    return;
                }
                _isSelectedToDelete = value;
                if (PropertyChanged == null) return;
                PropertyChanged(this, new PropertyChangedEventArgs("IsSelectedToDelete"));
                PropertyChanged(this, new PropertyChangedEventArgs("State"));
            }
        }

        public string ShortcutTooltip
        {
            get
            {
                if (Shortcut == null)
                {
                    return State;
                }
                var tooltip = string.Format("soft-linked to '{0}' as '{1}'", Shortcut.FullName, ShortcutName);
                return tooltip;
            }
        }

        public FileInfoEx Shortcut
        {
            get { return _shortcut; }
            set
            {
                if (_shortcut == value)
                {
                    return;
                }
                _shortcut = value;
                // NOTE IsSelectedToDelete is set in a preceding operation appropriately
                // so no need to set it again here
                PropertyChanged(this, new PropertyChangedEventArgs("State"));
            }
        }

        public string ShortcutName { get; set; }

        public int GroupId
        {
            get { return _groupId; }
            set 
            { 
                if (_groupId == value)
                {
                    return;
                }
                _groupId = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("GropuId"));
                }
            }
        }

        public string State
        {
            get
            {
                if (Shortcut != null)
                {
                    return "Softlinked";
                }
                return IsSelectedToDelete ? "To Delete" : "To Keep";
            }
        }

        public FileInfo InternalFileInfo
        {
            get { return _fileInfo; }
        }

        #endregion

        #region Constructors

        public FileInfoEx(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }
        
        #endregion

        #region Fields

        private readonly FileInfo _fileInfo;
        private bool _isSelectedToDelete;
        private int _groupId;
        private FileInfoEx _shortcut;

        #endregion
    }
}

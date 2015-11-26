using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace FileMatcher
{
    /// <summary>
    ///  enumerates all files in the specified starting directory and its subdirectories
    /// </summary>
    public class FileScanner : IEnumerable<FileInfo>, INotifyPropertyChanged
    {
        #region Delegates

        public delegate void UpdatedEventHandler();

        #endregion

        #region Fields

        /// <summary>
        ///  starting dirctory of the scanning
        /// </summary>
        private readonly DirectoryInfo _startingDir;

        private readonly ISet<string> _excludedDirs;

        private DirectoryInfo _currentDirectory;

        #endregion

        #region Constructors

        /// <summary>
        ///  Instantiates a scanner with the specified path and status updator
        /// </summary>
        /// <param name="startingPath">The path to the starting directory</param>
        /// <param name="updateStatus">Delegate that updates the status to the progress displaying UI</param>
        public FileScanner(string startingPath)
            : this(new DirectoryInfo(startingPath))
        {
        }

        /// <summary>
        ///  Instantiates a scanner with the specified directoryand status updator
        /// </summary>
        /// <param name="startingDir">The starting directory</param>
        /// <param name="excludedDirs">The directories to be excluded from searching</param>
        /// <param name="updateStatus">Delegate that updates the status to the progress displaying UI</param>
        public FileScanner(DirectoryInfo startingDir, ISet<DirectoryInfo> excludedDirs = null)
        {
            _startingDir = startingDir;
            if (excludedDirs != null)
            {
                _excludedDirs = new HashSet<string>();
                foreach (var ed in excludedDirs)
                {
                    _excludedDirs.Add(ed.FullName.ToLower());
                }
            }
            else
            {
                _excludedDirs = null;
            }
        }

        #endregion

        #region Properties

        #region INotifyPropertyChanged members

        /// <summary>
        ///  event that updates status to the UI
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public DirectoryInfo CurrentDirectory
        {
            get
            {
                return _currentDirectory;
            }
            private set
            {
                if (_currentDirectory != value)
                {
                    _currentDirectory = value;
                    RaisePropertyChanged("CurrentDirectory");
                }
            }
        }

        #endregion

        #region Methods

        #region IEnumerable<FileInfo> members

        /// <summary>
        ///  returns an enumerator of FileInfo objects
        /// </summary>
        /// <returns>The enumerator of FileInfo objects</returns>
        public IEnumerator<FileInfo> GetEnumerator()
        {
            var qd = new Queue<DirectoryInfo>();
            qd.Enqueue(_startingDir);
            while (qd.Count > 0)
            {
                var d = qd.Dequeue();
                var subds = TryGetDirectories(d);
                foreach (var subd in subds)
                {
                    if (_excludedDirs != null && _excludedDirs.Contains(subd.FullName.ToLower()))
                    {
                        continue;
                    }
                    qd.Enqueue(subd);
                }
                var fs = TryGetFiles(d);
                foreach (var f in fs)
                {
                    yield return f;
                }
            }
        }

        /// <summary>
        ///  returns an enumerator of FileInfo objects
        /// </summary>
        /// <returns>The enumerator of FileInfo objects</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///  tries to get sub-directories of the specified directory
        /// </summary>
        /// <param name="d">The directory to get sub-directories of</param>
        /// <returns>All the subdirectory if successful or an empty list</returns>
        IEnumerable<DirectoryInfo> TryGetDirectories(DirectoryInfo d)
        {
            try
            {
                return d.EnumerateDirectories();
            }
            catch (Exception)
            {
                return new DirectoryInfo[0];
            }
        }

        /// <summary>
        ///  tries to get files in the specified directory
        /// </summary>
        /// <param name="d">The directory to get files from</param>
        /// <returns>All the files if successful or an empty list</returns>
        private IEnumerable<FileInfo> TryGetFiles(DirectoryInfo d)
        {
            try
            {
                CurrentDirectory = d;
                return d.EnumerateFiles();
            }
            catch (Exception)
            {
                return new FileInfo[0];
            }
        }

        private void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion
    }
}

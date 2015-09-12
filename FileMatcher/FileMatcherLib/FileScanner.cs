using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace FileMatcherLib
{
    /// <summary>
    ///  enumerates all files in the specified starting directory and its subdirectories
    /// </summary>
    public class FileScanner : IEnumerable<FileInfo>
    {
        #region Fields

        /// <summary>
        ///  starting dirctory of the scanning
        /// </summary>
        private readonly DirectoryInfo _startingDir;

        #endregion

        #region Constructors

        /// <summary>
        ///  Instantiates a scanner with the specified path and status updator
        /// </summary>
        /// <param name="startingPath">The path to the starting directory</param>
        /// <param name="updateStatus">Delegate that updates the status to the progress displaying UI</param>
        public FileScanner(string startingPath, UpdateStatusDelegate updateStatus)
            : this(new DirectoryInfo(startingPath), updateStatus)
        {
        }

        /// <summary>
        ///  Instantiates a scanner with the specified directoryand status updator
        /// </summary>
        /// <param name="startingDir">The starting directory</param>
        /// <param name="updateStatus">Delegate that updates the status to the progress displaying UI</param>
        public FileScanner(DirectoryInfo startingDir, UpdateStatusDelegate updateStatus)
        {
            _startingDir = startingDir;
            UpdateStatus = updateStatus;
        }

        #endregion

        #region Properties

        /// <summary>
        ///  Delegate that update status to the UI
        /// </summary>
        public UpdateStatusDelegate UpdateStatus { get; private set; }

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
                if (UpdateStatus != null)
                {
                    var msg = string.Format(Strings.ScanningDirectory+"\n", d.FullName);
                    UpdateStatus(msg);
                }
                return d.EnumerateFiles();
            }
            catch (Exception)
            {
                return new FileInfo[0];
            }
        }

        #endregion
    }
}

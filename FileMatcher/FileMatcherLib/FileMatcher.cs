using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FileMatcherLib
{
    /// <summary>
    ///  The class that invokes file scanner for enumeration of files and uses FileDictionary to 
    ///  find out the duplicates
    /// </summary>
    public class FileMatcher
    {
        #region Nested classes

        public class CancellerInfo
        {
            public FileMatchingCanceller Canceller;
            public AutoResetEvent SyncEvent;
        }

        #endregion

        #region Fields

        private bool _finished;
        private readonly Queue<FileInfo> _queuedFiles = new Queue<FileInfo>();
        private readonly AutoResetEvent _fileMatchingEvent = new AutoResetEvent(false);

        private string _scannerMessage;
        private int _numFilesFound;
        private int _numFilesAdded;

        private int _numDuplicates;
        private int _numDuplicateGroups;
        private long _totalDuplicateBytes;

        private DateTime _lastUpdate;
        private const int MinUpdateInterval = 200;

        #endregion

        #region Constructors

        /// <summary>
        ///  instantiates a FileMatcher with the specified path strings in variable argument list
        /// </summary>
        /// <param name="startingPaths">The path strings to the starting directories</param>
        public FileMatcher(params string[] startingPaths)
            : this((IEnumerable<string>)startingPaths)
        {
        }

        /// <summary>
        ///  instantiates a FileMatcher with the specified starting directories in variable argument list
        /// </summary>
        /// <param name="startingDirs">The starting directories</param>
        public FileMatcher(params DirectoryInfo[] startingDirs)
            : this((IEnumerable<DirectoryInfo>)startingDirs)
        {
        }

        /// <summary>
        ///  instantiates a FileMatcher with the specified path strings to the starting directories
        /// </summary>
        /// <param name="startingPaths">The path strings to the starting directories</param>
        public FileMatcher(IEnumerable<string> startingPaths)
        {
            StartingDirectories = StartDirectoryValidator.Validate(startingPaths);
            _lastUpdate = DateTime.Now;
        }

        /// <summary>
        ///  instantiates a FileMatcher with the specified starting directories
        /// </summary>
        /// <param name="startingDirs">The starting directories</param>
        public FileMatcher(IEnumerable<DirectoryInfo> startingDirs)
        {
            StartingDirectories = StartDirectoryValidator.Validate(startingDirs);
            _lastUpdate = DateTime.Now;
        }

        #endregion

        #region Properties
        
        /// <summary>
        ///  The starting directories to work from (redundancy already removed)
        /// </summary>
        public List<DirectoryInfo> StartingDirectories { get; private set; }

        /// <summary>
        ///  The status updator
        /// </summary>
        public UpdateStatusDelegate UpdateStatus { get; set; }

        /// <summary>
        ///  The progress updator
        /// </summary>
        public UpdateProgressDelegate UpdateProgress { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///  returns a list of identical file groups
        /// </summary>
        /// <param name="fileHash">The file hash function to use</param>
        /// <param name="canceller">The canceller that can cancel the process as per user's request</param>
        /// <returns>The list of groups each containg more than one identical file found</returns>
        public List<IdenticalFiles> GetIdenticalFiles(IFileHash fileHash, FileMatchingCanceller canceller)
        {
            var fd = new FileDictionary(fileHash);

            _numFilesFound = 0;
            var thread = new Thread(ScanFiles);

            var cancellerInfo = new CancellerInfo
                {
                    Canceller = canceller,
                    SyncEvent = new AutoResetEvent(false)
                };
            canceller.CanceledEvent += () => _fileMatchingEvent.Set();
            canceller.PauseStateChangeEvent += () =>
                {
                    _fileMatchingEvent.Set();
                    cancellerInfo.SyncEvent.Set();
                };

            thread.Start(cancellerInfo);

            _finished = false;
            _numFilesAdded = 0;
            while (true)
            {
                int count;
                lock (_queuedFiles)
                {
                    count = _queuedFiles.Count;
                    if (count == 0 && _finished || canceller.Canceled)
                    {
                        break;
                    }
                }
                if (count == 0)
                {
                    _fileMatchingEvent.WaitOne();
                }

                while (!canceller.Canceled)
                {
                    while (canceller.Paused)
                    {
                        // TODO: Notify the UI that it's completely paused now
                        _fileMatchingEvent.WaitOne();
                    }

                    FileInfo file;
                    lock (_queuedFiles)
                    {
                        count = _queuedFiles.Count;
                        if (count == 0)
                        {
                            break;
                        }
                        file = _queuedFiles.Dequeue();
                    }
                    fd.AddFile(file);
                    _numFilesAdded++;
                    _numDuplicateGroups = fd.DuplicateGroups;
                    _numDuplicates = fd.DuplicateFiles;
                    _totalDuplicateBytes = fd.DuplicateBytes;
                    UpdateMessage();
                }
            }

            thread.Join();

            if (UpdateStatus != null)
            {
                UpdateStatus(Strings.PostProcessingRemovingSingles);
            }

            fd.RemoveSingleFiles();

            if (UpdateStatus != null)
            {
                UpdateStatus(Strings.PostProcessingGeneratingList);
            }

            var list = fd.GenerateIdenticalList();

            if (UpdateStatus != null)
            {
                UpdateStatus(Strings.PostProcessingDone);
            }
            if (UpdateProgress != null)
            {
                UpdateProgress(1);
            }

            return list;
        }


        /// <summary>
        ///  returns a list of identical file groups
        /// </summary>
        /// <param name="canceller">The canceller that can cancel the process as per user's request</param>
        /// <returns>The list of groups each containg more than one identical file found</returns>
        public List<IdenticalFiles> GetIdenticalFiles(FileMatchingCanceller canceller)
        {
            return GetIdenticalFiles(FileHash.Instance, canceller);
        }

        private void ScanFiles(object o)
        {
            var cancellerInfo = (CancellerInfo) o;

            var canceller = cancellerInfo.Canceller;
            var syncEvent = cancellerInfo.SyncEvent;
            
            UpdateStatusDelegate innerUpdate = s =>
            {
                _scannerMessage = s;
                UpdateMessage();
            };

            foreach (var sd in StartingDirectories)
            {
                if (canceller.Canceled)
                {
                    break;
                }

                var fs = new FileScanner(sd, innerUpdate);
                foreach (var f in fs)
                {
                    while (canceller.Paused)
                    {
                        syncEvent.WaitOne();
                    }
                    if (canceller.Canceled)
                    {
                        break;
                    }

                    lock (_queuedFiles)
                    {
                        _queuedFiles.Enqueue(f);
                    }

                    _numFilesFound++;
                    _fileMatchingEvent.Set();
                }
            }

            _finished = true;
            _scannerMessage = "";
            UpdateMessage();
        }

        private void UpdateMessage()
        {
            // not to update the message so frequently
            var now = DateTime.Now;
            if ((now - _lastUpdate).Milliseconds < MinUpdateInterval)
            {
                return;
            }
            _lastUpdate = now;
            lock (this)
            {
                if (UpdateStatus != null)
                {
                    var sb = new StringBuilder();
                    sb.Append(_scannerMessage);
                    sb.AppendFormat(Strings.FilesProcessedSoFar + "\n", _numFilesAdded);
                    sb.AppendFormat(Strings.DuplicationUpdate+"\n",
                        _numDuplicateGroups, _numDuplicates, _totalDuplicateBytes.ToString("###,###,###,###,##0"));
                    if (!_finished)
                    {
                        sb.AppendFormat(Strings.FilesFoundSoFar + "\n", _numFilesFound);
                    }
                    else
                    {
                        sb.AppendFormat(Strings.FilesFoundTotal + "\n", _numFilesFound);
                    }
                    UpdateStatus(sb.ToString());
                }
                if (_finished && UpdateProgress != null)
                {
                    var completeRate = (float) _numFilesAdded/_numFilesFound;
                    UpdateProgress(completeRate);
                }
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;

namespace FileMatcher
{
    /// <summary>
    ///  The class that invokes file scanner for enumeration of files and uses FileDictionary to 
    ///  find out the duplicates
    /// </summary>
    public class FileMatcher
    {
        #region Enumerations

        public enum Statuses
        {
            Scanning,
            CleaningUp, // clean up: eliminating single item groups
            Done,
        }

        #endregion

        #region Delegates

        public delegate void UpdatedEventHandler(object sender);

        #endregion

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

        private DateTime _lastUpdate;
        private const int MinUpdateInterval = 200;

        #endregion

        #region Constructors

        /// <summary>
        ///  instantiates a FileMatcher with the specified path strings in variable argument list
        /// </summary>
        /// <param name="fileHash">The file hash function to use</param>
        /// <param name="startingPaths">The path strings to the starting directories</param>
        public FileMatcher(IFileHash fileHash, params string[] startingPaths)
            : this(fileHash, (IEnumerable<string>)startingPaths)
        {
        }

        public FileMatcher(params string[] startingPaths)
            : this((IEnumerable<string>)startingPaths)
        {
        }

        /// <summary>
        ///  instantiates a FileMatcher with the specified starting directories in variable argument list
        /// </summary>
        /// <param name="fileHash">The file hash function to use</param>
        /// <param name="startingDirs">The starting directories</param>
        public FileMatcher(IFileHash fileHash, params DirectoryInfo[] startingDirs)
            : this(fileHash, (IEnumerable<DirectoryInfo>)startingDirs)
        {
        }

        public FileMatcher(params DirectoryInfo[] startingDirs)
        : this((IEnumerable<DirectoryInfo>)startingDirs)
        {
        }


        /// <summary>
        ///  instantiates a FileMatcher with the specified path strings to the starting directories
        /// </summary>
        /// <param name="fileHash">The file hash function to use</param>
        /// <param name="startingPaths">The path strings to the starting directories</param>
        public FileMatcher(IFileHash fileHash, IEnumerable<string> startingPaths)
        {
            StartingDirectories = StartDirectoryValidator.Validate(startingPaths);
            _lastUpdate = DateTime.Now;

            FileDictionary = new FileDictionary(fileHash);
            Adaptor = new DynamicFileGroupAdaptor(FileDictionary);
        }

        public FileMatcher(IEnumerable<string> startingPaths)
        {
            StartingDirectories = StartDirectoryValidator.Validate(startingPaths);
            _lastUpdate = DateTime.Now;
            
            FileDictionary = new FileDictionary(FileHash.Instance);
            Adaptor = new DynamicFileGroupAdaptor(FileDictionary);
        }

        /// <summary>
        ///  instantiates a FileMatcher with the specified starting directories
        /// </summary>
        /// <param name="fileHash">The file hash function to use</param>
        /// <param name="startingDirs">The starting directories</param>
        public FileMatcher(IFileHash fileHash, IEnumerable<DirectoryInfo> startingDirs)
        {
            StartingDirectories = StartDirectoryValidator.Validate(startingDirs);
            _lastUpdate = DateTime.Now;

            FileDictionary = new FileDictionary(fileHash);
            Adaptor = new DynamicFileGroupAdaptor(FileDictionary);
        }

        public FileMatcher(IEnumerable<DirectoryInfo> startingDirs)
        {
            StartingDirectories = StartDirectoryValidator.Validate(startingDirs);
            _lastUpdate = DateTime.Now;

            FileDictionary = new FileDictionary(FileHash.Instance);
            Adaptor = new DynamicFileGroupAdaptor(FileDictionary);
        }

        #endregion

        #region Properties

        /// <summary>
        ///  The starting directories to work from (redundancy already removed)
        /// </summary>
        public List<DirectoryInfo> StartingDirectories { get; private set; }

        public ObservableCollection<IdenticalFiles> IdenticalFilesList
        {
            get
            {
                return Adaptor.IdenticalFileGroups;
            }
        }

        /// <summary>
        ///  The status updator
        /// </summary>
        public event UpdatedEventHandler StatusUpdated;

        public event UpdatedEventHandler ProgressUpdated;

        public FileDictionary FileDictionary { get; private set; }

        public DynamicFileGroupAdaptor Adaptor { get; private set; }

        public int NumFilesFound { get; private set; }

        public int NumFilesAdded { get; private set; }

        public int NumDuplicates { get; set; }

        public int NumDuplicateGroups { get; set; }

        public long TotalDuplicateBytes { get; private set; }

        public DirectoryInfo CurrentDirectory { get; private set; }

        public double Progress { get { return (double)NumFilesAdded / NumFilesFound; } }

        public Statuses Status { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        ///  Work to update the list of identical file groups
        /// </summary>
        /// <param name="canceller">The canceller that can cancel the process as per user's request</param>
        /// <returns>The list of groups each containg more than one identical file found</returns>
        public void GetIdenticalFiles(FileMatchingCanceller canceller)
        {
            CurrentDirectory = null;
            NumFilesFound = 0;
            NumDuplicateGroups = 0;
            NumDuplicates = 0;
            NumFilesAdded = 0;
            TotalDuplicateBytes = 0;
            Status = Statuses.Scanning;
            UpdateProgress();
            UpdateStatus();

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
            NumFilesAdded = 0;
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
                    FileDictionary.AddFile(file);
                    NumFilesAdded++;
                    NumDuplicateGroups = FileDictionary.DuplicateGroups;
                    NumDuplicates = FileDictionary.DuplicateFiles;
                    TotalDuplicateBytes = FileDictionary.DuplicateBytes;
                    UpdateProgress();
                }
            }

            thread.Join();

            Status = Statuses.CleaningUp;
            UpdateStatus();

            FileDictionary.RemoveSingleFiles();

            Status = Statuses.Done;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (StatusUpdated != null)
            {
                StatusUpdated(this);
            }
        }

        private void UpdateProgress()
        {
            if (ProgressUpdated != null)
            {
                ProgressUpdated(this);
            }
        }

        private void ScanFiles(object o)
        {
            var cancellerInfo = (CancellerInfo) o;

            var canceller = cancellerInfo.Canceller;
            var syncEvent = cancellerInfo.SyncEvent;
            
            foreach (var sd in StartingDirectories)
            {
                if (canceller.Canceled)
                {
                    break;
                }

                var fs = new FileScanner(sd);
                fs.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == "CurrentDirectory")
                    {
                        CurrentDirectory = fs.CurrentDirectory;
                        UpdateStatus();
                    };
                };
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

                    NumFilesFound++;
                    _fileMatchingEvent.Set();
                }
            }

            _finished = true;
            CurrentDirectory = null;
            UpdateStatus();
        }
        
        #endregion
    }
}

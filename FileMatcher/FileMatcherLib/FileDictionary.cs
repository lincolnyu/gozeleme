using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileMatcher
{
    /// <summary>
    ///  A dictionary that maps a key hashed from FileInfo to a list of identical file groups that share this key
    /// </summary>
    public class FileDictionary
    {
        #region Delegates

        public delegate void DictionaryAddedEventHandler(object sender, DictionaryAddedEventArgs args);

        #endregion

        #region Nested types

        public class DictionaryAddedEventArgs : EventArgs
        {
            public DictionaryAddedEventArgs(int hash, int index, FileInfo file)
            {
                Hash = hash;
                Index = index;
                File = file;
            }

            public int Hash { get; private set; }

            /// <summary>
            ///  File added
            /// </summary>
            public FileInfo File { get; private set; }

            /// <summary>
            ///  Index of the identical file group
            /// </summary>
            public int Index { get; private set; }
        }

        #endregion

        #region Fields

        /// <summary>
        ///  inner dictionary that stores the identical file groups according to their hash keys
        /// </summary>
        private readonly Dictionary<int, List<IdenticalFiles>> _innerDictionary =
            new Dictionary<int, List<IdenticalFiles>>();

        private readonly IFileHash _hash = FileHash.Instance;

        #endregion

        #region Constructors

        /// <summary>
        ///  Instantiates a file dictionary with the specified hash function
        /// </summary>
        /// <param name="hash">The function to map FileInfo to integer</param>
        public FileDictionary(IFileHash hash) : this()
        {
            _hash = hash;
        }

        /// <summary>
        ///  Instantiates a file dictionary without specifying a hash function (using the default)
        /// </summary>
        public FileDictionary()
        {
            DuplicateFiles = 0;
            DuplicateBytes = 0;
            DuplicateGroups = 0;
        }

        #endregion

        #region Properties

        public List<IdenticalFiles> this[int key]
        {
            get { return _innerDictionary[key]; }
        }

        /// <summary>
        ///  Number of duplicate files not including the single copy for each
        /// </summary>
        public int DuplicateFiles { get; private set; }

        /// <summary>
        ///  Total number of bytes in the above files
        /// </summary>
        public long DuplicateBytes { get; private set; }

        /// <summary>
        ///  Number of group of identical files that contain more than on file
        /// </summary>
        public int DuplicateGroups { get; private set; }

        #endregion

        #region Event handlers

        public event DictionaryAddedEventHandler DictionaryAdded;

        #endregion

        #region Methods

        private void RaiseDictionaryAddedEvent(int hash, int index, FileInfo file)
        {
            if (DictionaryAdded != null)
            {
                var args = new DictionaryAddedEventArgs(hash, index, file);
                DictionaryAdded(this, args);
            }
        }

        /// <summary>
        ///  Adds a file to the dictionary
        /// </summary>
        /// <param name="fileInfo">The file to add</param>
        public void AddFile(FileInfo fileInfo)
        {
            int key;
            try
            {
                key = _hash.Hash(fileInfo);
            }
            catch (Exception)
            {
                return; // error accessing the file, cancel the adding
            }

            if (!_innerDictionary.ContainsKey(key))
            {
                _innerDictionary[key] = new List<IdenticalFiles>();
            }

            var fgsWithSameKey = _innerDictionary[key];

            var temp = new IdenticalFiles();
            temp.AddFirst(fileInfo);

            int index;
            try
            {
                index = fgsWithSameKey.BinarySearch(temp, FileComparer.Instance);
            }
            catch (Exception)
            {
                return; // error accessing the file, cancel the adding
            }
            if (index < 0)
            {
                index = -index - 1;
                fgsWithSameKey.Insert(index, temp);
            }
            else
            {
                var match = fgsWithSameKey[index];
                match.AddLast(fileInfo);
                if (match.Count >= 2)
                {
                    DuplicateFiles++;
                    DuplicateBytes += fileInfo.Length;
                    if (match.Count == 2)
                    {
                        DuplicateGroups++;
                    }
                }
            }
            RaiseDictionaryAddedEvent(key, index, fileInfo);
        }

        /// <summary>
        ///  Removes all identical groups with only one file and the buckets that only contain groups like that
        /// </summary>
        public void RemoveSingleFiles()
        {
            var keysToRemove = new List<int>();
            foreach (var pair in _innerDictionary)
            {
                var fgsWithSameKey = pair.Value;
                for (var i = fgsWithSameKey.Count-1; i>=0; i--)
                {
                    var fg = fgsWithSameKey[i];
                    if (fg.Count == 1)
                    {
                        fgsWithSameKey.RemoveAt(i);
                    }
                }
                if (fgsWithSameKey.Count == 0)
                {
                    keysToRemove.Add(pair.Key);
                }
            }
            foreach (var keyToRemove in keysToRemove)
            {
                _innerDictionary.Remove(keyToRemove);
            }
        }

        /// <summary>
        ///  returns the list of duplicate file groups sorted by order determined by FileComparer
        /// </summary>
        /// <returns>The list of duplicate file groups</returns>
        public List<IdenticalFiles> GenerateIdenticalList()
        {
            var result =
                (from fgsWithSameKey in _innerDictionary.Values
                 from ifg in fgsWithSameKey
                 where ifg.Count != 1
                 select ifg).ToList();
            // sort result
            result.Sort(FileComparer.Instance);
            return result;
        }

        #endregion
    }
}

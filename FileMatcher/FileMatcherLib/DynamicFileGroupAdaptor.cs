using System.Collections.ObjectModel;
using FileMatcher.Collections;
using System;

namespace FileMatcher
{
    /// <summary>
    ///  Adapts IdenticalFileGroups (linear) to FileDictionary
    /// </summary>
    public class DynamicFileGroupAdaptor
    {
        private class Comparer : IComparable<IdenticalFiles>
        {
            public Comparer(IdenticalFiles target)
            {
                Target = target;
            }

            public IdenticalFiles Target { get; private set; }

            public int CompareTo(IdenticalFiles other)
            {
                return FileComparer.Instance.Compare(other, Target);
            }
        }

        public DynamicFileGroupAdaptor(FileDictionary fd)
        {
            FileDictionary = fd;
            fd.DictionaryAdded += FileDictionaryOnAdded;
        }

        public FileDictionary FileDictionary { get; private set; }

        public ObservableCollection<IdenticalFiles> IdenticalFileGroups { get; private set; } = new ObservableCollection<IdenticalFiles>();

        private void FileDictionaryOnAdded(object sender, FileDictionary.DictionaryAddedEventArgs args)
        {
            var key = args.Hash;
            var skg = FileDictionary[key];
            var index = args.Index;
            var ig = skg[index];
            if (ig.Count > 1)
            {
                int outlistIndex;
                var comparer = new Comparer(ig);
                var found = BinarySearch.Search(IdenticalFileGroups, 0, IdenticalFileGroups.Count,
                   comparer, out outlistIndex);
                if (!found)
                {
                    // NOTE this gets the output list to use the same file group object as the dictionary
                    IdenticalFileGroups.Insert(outlistIndex, ig);
                }
            }
        }
    }
}

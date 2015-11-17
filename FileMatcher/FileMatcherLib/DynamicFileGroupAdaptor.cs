using System.Collections.ObjectModel;
using FileMatcher.Collections;
using System;
using System.Globalization;
using System.IO;

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

        public int FindGroup(FileInfo fi)
        {
            var ig = new IdenticalFiles();
            ig.AddFirst(fi);
            return FindGroup(ig);
        }

        public int FindGroup(IdenticalFiles ig)
        {
            int outlistIndex;
            var comparer = new Comparer(ig);
            var found = IdenticalFileGroups.Search(0, IdenticalFileGroups.Count,
                   comparer, out outlistIndex);
            return found ? outlistIndex : -outlistIndex - 1;
        }

        public bool RemoveItemFromGroup(FileInfo f)
        {
            var groupIndex = FindGroup(f);
            if (groupIndex < 0)
            {
                return false;
            }
            var ig = IdenticalFileGroups[groupIndex];
            ig.Remove(f);
            if (ig.Count <= 1)
            {
                IdenticalFileGroups.RemoveAt(groupIndex);
            }
            return true;
        }

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
                var found = IdenticalFileGroups.Search(0, IdenticalFileGroups.Count,
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

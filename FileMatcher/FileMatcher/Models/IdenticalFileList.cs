using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FileMatcher
{
    /// <summary>
    ///  A class that keeps all files that have duplicates
    ///  in the following order (governed by FileComparer)
    ///  1. file size, descending
    ///  2. file name, ascending dictionary order
    ///  3. full directory, ascending dictionary order
    /// </summary>
    public class IdenticalFileList : List<FileInfoEx>, INotifyCollectionChanged
    {
        public class FileComparer : IComparer<FileInfoEx>
        {
            public int Compare(FileInfoEx x, FileInfoEx y)
            {
                var c = x.GroupId.CompareTo(y.GroupId);
                if (c != 0)
                {
                    return c;
                }
                c = String.Compare(x.Name, y.Name, StringComparison.Ordinal);
                return c != 0 ? c : String.Compare(x.FullName, y.FullName, 
                    StringComparison.Ordinal);
            }

            public static FileComparer Instance = new FileComparer();
        }

        public new void Add(FileInfoEx fileInfo)
        {
            var index = Find(fileInfo);
            if (index >= 0)
            {
                return; // already exists
            }
            index = -index - 1;
            Insert(index, fileInfo);
        }

        public new void Insert(int index, FileInfoEx fileInfo)
        {
            base.Insert(index, fileInfo);
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, fileInfo));
            }
        }

        public new void RemoveAt(int index)
        {
            var obj = this[index];
            base.RemoveAt(index);
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, obj, index));
            }
        }

        public int Find(FileInfoEx fileInfo)
        {
            return BinarySearch(fileInfo, FileComparer.Instance);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}

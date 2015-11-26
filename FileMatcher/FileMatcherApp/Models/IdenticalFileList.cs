using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace FileMatcherApp.Models
{
    /// <summary>
    ///  A class that keeps all files that have duplicates
    ///  in the following order (governed by FileComparer)
    ///  1. length, descending
    ///  2. group id, ascedning
    ///  3. file name, ascending dictionary order
    ///  4. full directory, ascending dictionary order
    /// </summary>
    public class IdenticalFileList : List<FileInfoEx>, INotifyCollectionChanged
    {
        public class FileComparer : IComparer<FileInfoEx>
        {
            public int Compare(FileInfoEx x, FileInfoEx y)
            {
                var c = y.Length.CompareTo(x.Length);
                if (c != 0)
                {
                    return c;
                }
                c = x.GroupId.CompareTo(y.GroupId);
                if (c != 0)
                {
                    return c;
                }
                c = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
                return c != 0 ? c : string.Compare(x.FullName, y.FullName, 
                    StringComparison.Ordinal);
            }

            public static FileComparer Instance = new FileComparer();
        }

        /// <summary>
        ///  This is to notify the backend through the updator
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionRemoved;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

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
            UpdateDuplicateCountForAdding(index);
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, fileInfo, index));
            }
        }

        public new void RemoveAt(int index)
        {
            var obj = this[index];
            UpdateDuplicateCountPreRemoving(index);
            base.RemoveAt(index);

            if (CollectionChanged != null)
            {
                CollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    obj, index));
            }

            if (CollectionRemoved != null)
            {
                CollectionRemoved(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    obj, index));
            }
        }

        private void UpdateDuplicateCountForAdding(int index)
        {
            var added = this[index];
            added.Duplicates = 1;
            for (var i = index - 1; i >= 0; i--)
            {
                var fi = this[i];
                if (fi.GroupId != added.GroupId)
                {
                    break;
                }
                added.Duplicates = ++fi.Duplicates;
            }
            for (var i = index + 1; i < Count; i++)
            {
                var fi = this[i];
                if (fi.GroupId != added.GroupId)
                {
                    break;
                }
                fi.Duplicates++;
                added.Duplicates = ++fi.Duplicates;
            }
        }

        private void UpdateDuplicateCountPreRemoving(int index)
        {
            var added = this[index];
            for (var i = index - 1; i >= 0; i--)
            {
                var fi = this[i];
                if (fi.GroupId != added.GroupId)
                {
                    break;
                }
                --fi.Duplicates;
            }
            for (var i = index + 1; i < Count; i++)
            {
                var fi = this[i];
                if (fi.GroupId != added.GroupId)
                {
                    break;
                }
                --fi.Duplicates;
            }
        }

        public int Find(FileInfoEx fileInfo)
        {
            return BinarySearch(fileInfo, FileComparer.Instance);
        }

        public int Find(FileInfo fileInfo)
        {
            var target = new FileInfoEx(fileInfo);
            var i = Find(target);
            // NOTE groupId is not available, so the search returns the
            //      first item with the same length
            if (i < 0)
            {
                i = -i - 1;
            }
            for (; i < Count; i++)
            {
                var item = this[i];
                if (item.InternalFileInfo == fileInfo)
                {
                    return i;
                }
            }
            return -1;// not found
        }

        public int FindGroupStart(FileInfoEx fileInfo)
        {
            var i = Find(fileInfo);
            var groupId = fileInfo.GroupId;
            for (;i>=0 && this[i].GroupId == groupId; i--)
            {
            }
            return i + 1;
        }
        public int FindGroupStart(FileInfo fileInfo)
        {
            var target = new FileInfoEx(fileInfo);
            return FindGroupStart(target);
        }
    }
}

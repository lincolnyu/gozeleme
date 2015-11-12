using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace FileMatcher
{
    /// <summary>
    ///  contains all identical files
    /// </summary>
    public class IdenticalFiles : LinkedList<FileInfo>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public new void AddFirst(FileInfo f)
        {
            base.AddFirst(f);
            RaiseItemAddedEvent(0, f);
        }

        public new void AddLast(FileInfo f)
        {
            base.AddLast(f);
            RaiseItemAddedEvent(Count - 1, f);
        }

        private void RaiseItemAddedEvent(int index, FileInfo f)
        {
            if (CollectionChanged != null)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    f, index);
                CollectionChanged(this, args);
            }
        }
    }
}

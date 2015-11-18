using FileMatcher;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Windows.Threading;
using FileMatcherApp.Models;

namespace FileMatcherApp.FileGrouping
{
    public class IdenticalFileListUpdater
    {
        // TODO thread synchronisation...
        // TODO removal of event handlers

        private int _lastGroupId;
        private readonly Dictionary<IdenticalFiles, int> _groupIdMap = new Dictionary<IdenticalFiles, int>();

        public IdenticalFileListUpdater(DynamicFileGroupAdaptor adaptor, Dispatcher dispatcher)
        {
            Adaptor = adaptor;

            Dispatcher = dispatcher;

            BindSource();
        }

        public Dispatcher Dispatcher { get; private set; }

        public DynamicFileGroupAdaptor Adaptor { get; private set; }

        public IdenticalFileList IdenticalFileList { get; private set; } = new IdenticalFileList();

        private void BindSource()
        {
            Adaptor.IdenticalFileGroups.CollectionChanged += FileGroupsOnCollectionChanged;
            IdenticalFileList.CollectionRemoved += IdenticalFileListOnCollectionRemoved;
        }

        private void IdenticalFileListOnCollectionRemoved(object sender, NotifyCollectionChangedEventArgs args)
        {
            // TODO if this is not sufficient, use sync lock
            // only deals with removal initiated from the UI
            if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var oldItem in args.OldItems.Cast<FileInfoEx>())
                {
                    Adaptor.RemoveItemFromGroup(oldItem.InternalFileInfo);
                }
            }
        }

        /// <summary>
        ///  responds to file group change at the back end
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void FileGroupsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            // TODO if this is not sufficient, use sync lock
            if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
            {
                foreach (var newItem in args.NewItems.Cast<IdenticalFiles>())
                {
                    LoadFiles(newItem);
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove && args.OldItems != null)
            {
                foreach (var oldItem in args.OldItems.Cast<IdenticalFiles>())
                {
                    UnloadFiles(oldItem);
                }
            }
        }

        private void LoadFiles(IdenticalFiles files)
        {
            files.CollectionChanged += IdenticalFilesCollectionChanged;
            foreach (var f in files)
            {
                var fex = new FileInfoEx(f) { GroupId = GetOrCreateGroupId(files) };
                Dispatcher.Invoke(() =>
                {
                    IdenticalFileList.Add(fex);
                });
            }
        }

        private void UnloadFiles(IdenticalFiles files)
        {
            foreach (var f in files)
            {
                var index = IdenticalFileList.Find(f);
                Dispatcher.Invoke(() =>
                {
                    IdenticalFileList.RemoveAt(index);
                });
            }
            files.CollectionChanged -= IdenticalFilesCollectionChanged;
        }

        private int GetOrCreateGroupId(IdenticalFiles files)
        {
            int groupId;
            if (!_groupIdMap.TryGetValue(files, out groupId))
            {
                groupId = ++_lastGroupId;
                _groupIdMap[files] = groupId;
            }
            return groupId;
        }

        private void IdenticalFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            var ifs = (IdenticalFiles)sender;
            if (args.OldItems != null)
            {
                foreach (var f in args.OldItems.Cast<FileInfo>())
                {
                    var index = IdenticalFileList.Find(f);
                    Dispatcher.Invoke(() =>
                    {
                        IdenticalFileList.RemoveAt(index);
                    });
                }
            }
            if (args.NewItems != null)
            {
                foreach(var f in args.NewItems.Cast<FileInfo>())
                {
                    var fex = new FileInfoEx(f) { GroupId = GetOrCreateGroupId(ifs) };
                    Dispatcher.Invoke(() =>
                    {
                        IdenticalFileList.Add(fex);
                    });
                }
            }
        }
    }
}

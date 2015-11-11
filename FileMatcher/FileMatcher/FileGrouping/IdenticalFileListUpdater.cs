using FileMatcherLib;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Windows.Threading;

namespace FileMatcher.FileGrouping
{
    public class IdenticalFileListUpdater
    {
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
        }

        private void FileGroupsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            // NOTE we don't deal with OldItems as the back end only adds files and 
            //      the deletions/changes initiated from the front end don't affect the 
            if (args.NewItems != null)
            {
                foreach (var newItem in args.NewItems.Cast<IdenticalFiles>())
                {
                    LoadFiles(newItem);
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

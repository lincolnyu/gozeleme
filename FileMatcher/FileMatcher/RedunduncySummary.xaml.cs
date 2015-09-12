using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using FileMatcher.Controllers;
using FileMatcher.Extensions;
using FileMatcherLib;

namespace FileMatcher
{
    /// <summary>
    /// Interaction logic for RedunduncySummary.xaml
    /// </summary>
    public partial class RedunduncySummary : INotifyPropertyChanged
    {
        #region Nested types

        public class ViewFilter : INotifyPropertyChanged
        {
            #region Fields

            private double _percentSize;

            #endregion

            #region Constructors

            public ViewFilter(IdenticalFileList files, ListView listView)
            {
                Files = files;
                FileSizes = new long[99];
                ListView = listView;
                UpdateFilter();
            }

            #endregion

            #region Properties

            public double PercentSize
            {
                get { return _percentSize; }
                set
                {
                    var iToSet = (int) Math.Round(value);
                    var iCurr = (int) Math.Round(_percentSize);
                    if (iToSet == iCurr) return;
                    _percentSize = iToSet;
                    OnPropertyChanged("PercentSize");
                    UpdateFilter();
                }
            }

            public IdenticalFileList Files { get; private set; }

            public long[] FileSizes { get; private set; }

            public ListView ListView { get; private set; }

            #endregion

            #region Events

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Methods

            public bool Filter(object o)
            {
                var fileInfo = (FileInfoEx) o;
                var size = fileInfo.Length;

                var percent = (int) Math.Round(PercentSize);
                return (size >= FileSizes[percent]);
            }

            public void Update()
            {
                if (Files.Count > 0)
                {
                    var j = 0;
                    for (var i = 1; i < 100; i++)
                    {
                        while (j*99 < i*Files.Count)
                        {
                            j++;
                        }
                        if (j >= Files.Count) j = Files.Count - 1; // this shouldn't happen; just for safety
                        FileSizes[99 - i] = Files[j].Length; // reverse order
                    }
                }
                else
                {
                    for (var i = 0; i < 99; i++)
                    {
                        FileSizes[i] = -1; // no files exist
                    }
                }
            }

            protected virtual void OnPropertyChanged(string propertyName = null)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            private void UpdateFilter()
            {
                ListView.Items.Filter = Filter;
            }

            #endregion
        }

        #endregion

        #region Fields

        private readonly Dictionary<int, int> _firstIndexOfGroup = new Dictionary<int, int>();
        private int _currOp;
        private readonly List<String> _lastColumnsClicked = new List<string>();
        private readonly List<bool> _ascendingOrdescending = new List<bool>();
        private readonly List<FileInfoEx> _filesToShortcut = new List<FileInfoEx>();

        #endregion

        #region Constructors

        public RedunduncySummary()
        {
            IdenticalFiles = new IdenticalFileList();
            UserCommands = new List<UserCommand>();
            _currOp = 0;
            InitializeComponent();
            Filter = new ViewFilter(IdenticalFiles, LvRedundant);
            LvRedundant.ItemsSource = IdenticalFiles;
            DataContext = this;
            DeleteToRecycleBin = true;
        }

        #endregion

        #region Events

        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion

        #region Properties

        public IdenticalFileList IdenticalFiles { get; private set; }

        private List<UserCommand> UserCommands { get; set; }

        public bool Undoable
        {
            get { return _currOp > 0; }
        }

        public bool Redoable
        {
            get { return _currOp < UserCommands.Count; }
        }

        public ViewFilter Filter { get; private set; }

        public bool DeleteToRecycleBin { get; private set; }

        #endregion

        #region Methods

        public void InitializeDialog(List<IdenticalFiles> igs)
        {
            var groupId = 1;

            // reverse iteration produces groups with size in descending order
            for (var i = igs.Count - 1; i >= 0; i--)
            {
                var ig = igs[i];
                if (ig.Count < 1) continue;
                _firstIndexOfGroup[groupId] = IdenticalFiles.Count;
                foreach (var f in ig)
                {
                    var fex = new FileInfoEx(f) {GroupId = groupId};
                    IdenticalFiles.Add(fex);
                }
                groupId++;
            }
            Filter.Update();

            // populate the initial sorting precedence list
            _lastColumnsClicked.Add("State");
            _lastColumnsClicked.Add("DirectoryName");
            _lastColumnsClicked.Add("Name");
            _ascendingOrdescending.Add(true);
            _ascendingOrdescending.Add(true);
            _ascendingOrdescending.Add(true);

            UpdateSorting();
        }

        private void LvRedundant_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AddGrouping();
        }

        private void AddGrouping()
        {
            if (LvRedundant.ItemsSource == null)
            {
                return;
            }

            var myView = (CollectionView) CollectionViewSource.GetDefaultView(LvRedundant.ItemsSource);
            var groupDesc = new PropertyGroupDescription("GroupId");
            if (myView.GroupDescriptions != null)
            {
                myView.GroupDescriptions.Add(groupDesc);
            }
        }

        /// <summary>
        ///  check if the deletions resulting from the command cause any groups to be complete wiped out
        /// </summary>
        /// <param name="command">The command to check</param>
        /// <returns>true if giving the deletion a go or false</returns>
        private bool CheckGroup(UserCommand command)
        {
            var allDeletedGroups = new List<int>();
            var processedGroups = new HashSet<int>();

            var filesToDelete = (from delop in command.Operations.OfType<DeleteOrUndeleteOperation>()
                where delop.ActionType == DeleteOrUndeleteOperation.ActionTypes.Delete
                select delop.File).ToList();

            foreach (var groupId in filesToDelete.Select(fileToDelete => fileToDelete.GroupId)
                .Where(groupId => !processedGroups.Contains(groupId)))
            {
                processedGroups.Add(groupId);
                var start = _firstIndexOfGroup[groupId];
                var allDelete = true;
                for (var i = start; i < IdenticalFiles.Count && IdenticalFiles[i].GroupId == groupId; i++)
                {
                    var isToDelete = IdenticalFiles[i].IsSelectedToDelete
                                     || filesToDelete.Contains(IdenticalFiles[i]);
                    if (isToDelete) continue;
                    allDelete = false;
                    break;
                }
                if (!allDelete) continue;
                var index = allDeletedGroups.BinarySearch(groupId);
                index = -index - 1;
                allDeletedGroups.Insert(index, groupId);
            }

            if (allDeletedGroups.Count == 0)
            {
                return true; // no group has been totally cleared
            }
            var msgBuilder = new StringBuilder();
            msgBuilder.Append(Strings.AllFilesInGroups);
            foreach (var i in allDeletedGroups)
            {
                msgBuilder.Append(string.Format("{0} ", i));
            }
            msgBuilder.Append(Strings.ProceedWithDeletion);
            return (MessageBox.Show(msgBuilder.ToString(), Strings.AppName,
                MessageBoxButton.YesNo) == MessageBoxResult.Yes);
        }

        private void AddSelectAndActionOperation(DeleteOrUndeleteOperation.ActionTypes actionType)
        {
            var command = new UserCommand();

            foreach (var item in LvRedundant.SelectedItems)
            {
                var file = (FileInfoEx)item;

                if (actionType == DeleteOrUndeleteOperation.ActionTypes.Undelete)
                {
                    // this is to clear the shortcut for file to undelete
                    if (file.Shortcut != null)
                    {
                        var unshortcut = new ShortcutOperation(file);
                        command.Operations.Add(unshortcut);
                    }
                }

                var delop = new DeleteOrUndeleteOperation(actionType, file);
                command.Operations.Add(delop);
            }

            if (command.Operations.Count == 0)
            {
                return;
            }

            if (actionType == DeleteOrUndeleteOperation.ActionTypes.Delete)
            {
                if (!CheckGroup(command))
                {
                    return;
                }
            }

            DoNewCommandIfNonEmpty(command);
        }

        private void UpdateUndoRedoable()
        {
            if (PropertyChanged == null)
            {
                return;
            }

            PropertyChanged(this, new PropertyChangedEventArgs("Undoable"));
            PropertyChanged(this, new PropertyChangedEventArgs("Redoable"));
        }

        private void MiUndo_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Undoable) return;
            _currOp--;
            var delop = UserCommands[_currOp];
            delop.Undo();

            UpdateUndoRedoable();
        }

        private void MiRedo_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Redoable) return;
            var delop = UserCommands[_currOp];
            delop.Redo();
            _currOp++;

            UpdateUndoRedoable();
        }

        private void MiApply_OnClick(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(DeleteToRecycleBin
                ? Strings.ProceedToApplyRecycling
                : Strings.ProceedToApplyDeletion,
                Strings.AppName, MessageBoxButton.YesNo);
            if (res != MessageBoxResult.Yes) return;
            var indicesToRemove = new List<int>();
            var totalSizeRemoved = 0L;
            var shortcutCount = 0;
            for (var i = 0; i < IdenticalFiles.Count; i++)
            {
                var f = IdenticalFiles[i];
                if (f.Shortcut != null)
                {
                    var shellLink = (IShellLinkW) new CShellLink();
                    shellLink.SetDescription(f.ShortcutName);
                    shellLink.SetPath(f.Shortcut.FullName);
                    var linkFile = (IPersistFile) shellLink;
                    var name = Path.Combine(f.DirectoryName, f.ShortcutName) + ".lnk";
                    linkFile.Save(name, true);
                    shortcutCount++;
                }
                if (!f.IsSelectedToDelete) continue;
                totalSizeRemoved += f.Length;
                if (DeleteToRecycleBin)
                {
                    RecycleBin.SendSilent(f.FullName);
                }
                else
                {
                    File.Delete(f.FullName);
                }

                indicesToRemove.Add(i);
            }

            for (var i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                var index = indicesToRemove[i];
                IdenticalFiles.RemoveAt(index);
            }

            // updates _firstIndexOfGroup
            _firstIndexOfGroup.Clear();
            var groupId = -1; // a group that doesn't exist
            for (var index = 0; index < IdenticalFiles.Count; index++)
            {
                var f = IdenticalFiles[index];
                if (f.GroupId == groupId) continue;
                _firstIndexOfGroup[f.GroupId] = index;
                groupId = f.GroupId;
            }

            UserCommands.Clear();
            _currOp = 0;
            UpdateUndoRedoable();

            if (shortcutCount > 0 || indicesToRemove.Count > 0)
            {
                var msg = string.Format(Strings.ApplicationSummary, indicesToRemove.Count,
                    shortcutCount, totalSizeRemoved.ToString("###,###,###,##0"));
                MessageBox.Show(msg, Strings.AppName, MessageBoxButton.OK);
            }
        }

        private void MiFilter_OnClick(object sender, RoutedEventArgs e)
        {
            var vfw = new ViewFilterWindow(Filter)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            vfw.Show();
        }

        private void OnCanExecuteOpenFolder(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = LvRedundant.SelectedItems != null && LvRedundant.SelectedItems.Count == 1;
        }

        private void OnExecuteOpenFolder(object sender, ExecutedRoutedEventArgs e)
        {
            var sel = LvRedundant.SelectedItem;
            if (sel == null)
            {
                return;
            }
            var fex = (FileInfoEx) sel;
            System.Diagnostics.Process.Start("explorer.exe", "/select,  \"" + fex.FullName + "\"");
        }

        private void OnCanExecuteDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            // just check if anything has been selected
            if (LvRedundant == null || LvRedundant.SelectedItems == null || LvRedundant.SelectedItems.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            var canDelete = false;
// ReSharper disable LoopCanBeConvertedToQuery
            foreach (var sel in LvRedundant.SelectedItems)
// ReSharper restore LoopCanBeConvertedToQuery
            {
                var fex = (FileInfoEx) sel;
                if (fex.IsSelectedToDelete) continue;
                canDelete = true;
                break;
            }
            e.CanExecute = canDelete;
        }

        private void OnExecuteDelete(object sender, ExecutedRoutedEventArgs e)
        {
            AddSelectAndActionOperation(DeleteOrUndeleteOperation.ActionTypes.Delete);
        }

        private void OnCanExecuteUndelete(object sender, CanExecuteRoutedEventArgs e)
        {
            // just check if anything has been selected
            if (LvRedundant == null || LvRedundant.SelectedItems == null || LvRedundant.SelectedItems.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            var canUndelete = false;
// ReSharper disable LoopCanBeConvertedToQuery
            foreach (var sel in LvRedundant.SelectedItems)
// ReSharper restore LoopCanBeConvertedToQuery
            {
                var fex = (FileInfoEx) sel;
                if (!fex.IsSelectedToDelete) continue;
                canUndelete = true;
                break;
            }
            e.CanExecute = canUndelete;
        }

        private void OnExecuteUndelete(object sender, ExecutedRoutedEventArgs e)
        {
            AddSelectAndActionOperation(DeleteOrUndeleteOperation.ActionTypes.Undelete);
        }

        private void OnCanExecuteShortcut(object sender, CanExecuteRoutedEventArgs e)
        {
            // just check if anything has been selected
            if (LvRedundant == null || LvRedundant.SelectedItems == null || LvRedundant.SelectedItems.Count == 0)
            {
                e.CanExecute = false;
                return;
            }

            // check if all files are from the same group
            var groupId = -1;
            foreach (var sel in LvRedundant.SelectedItems)
            {
                var fex = (FileInfoEx) sel;
                if (groupId == -1)
                {
                    groupId = fex.GroupId;
                }
                else if (groupId != fex.GroupId)
                {
                    e.CanExecute = false;
                    return;
                }
            }

            // then check if there are remaining items in the group other than the 
            // selected files and those that have been chosen to delete
            var start = _firstIndexOfGroup[groupId];
            var deleteAll = true;
            for (var i = start; i < IdenticalFiles.Count && IdenticalFiles[i].GroupId == groupId; i++)
            {
                var file = IdenticalFiles[i];
                if (!file.IsSelectedToDelete && !LvRedundant.SelectedItems.Contains(file))
                {
                    deleteAll = false;
                }
            }
            if (deleteAll)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        private void OnExecuteShortcut(object sender, ExecutedRoutedEventArgs e)
        {
            _filesToShortcut.Clear();
            foreach (var sel in LvRedundant.SelectedItems)
            {
                var fex = (FileInfoEx) sel;
                _filesToShortcut.Add(fex);
            }
        }

        private void MiPickRepresentative_OnClick(object sender, RoutedEventArgs e)
        {
            MiPickSurvivor.IsChecked = false;
            LvRedundant.SelectionMode = (MiPickRepresentative.IsChecked) ? SelectionMode.Single : SelectionMode.Extended;
        }

        private void MiPickSurvivor_OnClick(object sender, RoutedEventArgs e)
        {
            MiPickRepresentative.IsChecked = false;
            LvRedundant.SelectionMode = (MiPickSurvivor.IsChecked) ? SelectionMode.Single : SelectionMode.Extended;
        }

        private void MiClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MiOnlineHelp_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Strings.OnlineHelpUrl);
        }

        private void LvRedundantGridViewColumnHeaderOnClick(object sender, RoutedEventArgs e)
        {
            var header = e.OriginalSource as GridViewColumnHeader;
            if (header == null) return;

            var headerTitle = header.Column.Header as string;
            if (headerTitle != "Name" && headerTitle != "DirectoryName" && headerTitle != "State")
            {
                return;
            }

            var isAscending = false;
            for (var i = 0; i < _lastColumnsClicked.Count; i++)
            {
                if (_lastColumnsClicked[i] != headerTitle) continue;
                _lastColumnsClicked.RemoveAt(i);
                isAscending = _ascendingOrdescending[i];
                _ascendingOrdescending.RemoveAt(i);
                break;
            }
            _lastColumnsClicked.Add(headerTitle);
            _ascendingOrdescending.Add(!isAscending);

            UpdateSorting();
        }

        private void SelectionChangedCreatingShortcuts(object sender, SelectionChangedEventArgs e)
        {
            if (_filesToShortcut.Count == 0)
            {
                return;
            }

            if (e.AddedItems.Count == 0)
            {
                _filesToShortcut.Clear();   // cancel the shorcut creation process
                return;
            }

            var target = e.AddedItems[0] as FileInfoEx;
            if (target == null || target.GroupId != _filesToShortcut[0].GroupId)
            {
                _filesToShortcut.Clear();   // cancel the shortcut creation process
                return;
            }

            var command = new UserCommand();
            foreach (var file in _filesToShortcut)
            {
                // should delete the file first
                // NOTE the deletion operation needs to be performed anyway so the previous deletion won't be unintentionally undone 
                var delop = new DeleteOrUndeleteOperation(DeleteOrUndeleteOperation.ActionTypes.Delete, file);
                command.Operations.Add(delop);

                // NOTE for the same reason the shortcut is created regardless if it's already been set up so
                var linkop = new ShortcutOperation(file, target, file.Name);
                command.Operations.Add(linkop);
            }

            DoNewCommandIfNonEmpty(command);

            _filesToShortcut.Clear();
        }

        private void SelectionChangedPickingRepresentative(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            var target = e.AddedItems[0] as FileInfoEx; // the file to keep
            if (target == null)
            {
                return;
            }

            if (target.IsSelectedToDelete)
            {
                // nothing is made to happen for the time being
                return;
            }

            var targetIndex = IdenticalFiles.Find(target);
            var groupId = target.GroupId;
            var command = new UserCommand();

            for (var i = targetIndex - 1; i >= 0 && IdenticalFiles[i].GroupId == groupId; i--)
            {
                var file = IdenticalFiles[i];
                var delop = new DeleteOrUndeleteOperation(DeleteOrUndeleteOperation.ActionTypes.Delete, file);
                command.Operations.Add(delop);
                var linkop = new ShortcutOperation(file, target, file.Name);
                command.Operations.Add(linkop);
            }
            for (var i = targetIndex + 1; i < IdenticalFiles.Count && IdenticalFiles[i].GroupId == groupId; i++)
            {
                var file = IdenticalFiles[i];
                var delop = new DeleteOrUndeleteOperation(DeleteOrUndeleteOperation.ActionTypes.Delete, file);
                command.Operations.Add(delop);
                var linkop = new ShortcutOperation(file, target, file.Name);
                command.Operations.Add(linkop);
            }

            DoNewCommandIfNonEmpty(command);
        }

        private void SelectionChangedPickingSurvivor(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            var target = e.AddedItems[0] as FileInfoEx; // the file to keep
            if (target == null)
            {
                return;
            }

            if (target.IsSelectedToDelete)
            {
                // nothing is made to happen for the time being
                return;
            }

            var targetIndex = IdenticalFiles.Find(target);
            var groupId = target.GroupId;
            var command = new UserCommand();

            for (var i = targetIndex - 1; i >= 0 && IdenticalFiles[i].GroupId == groupId; i--)
            {
                var file = IdenticalFiles[i];
                var delop = new DeleteOrUndeleteOperation(DeleteOrUndeleteOperation.ActionTypes.Delete, file);
                command.Operations.Add(delop);
            }
            for (var i = targetIndex + 1; i < IdenticalFiles.Count && IdenticalFiles[i].GroupId == groupId; i++)
            {
                var file = IdenticalFiles[i];
                var delop = new DeleteOrUndeleteOperation(DeleteOrUndeleteOperation.ActionTypes.Delete, file);
                command.Operations.Add(delop);
            }

            DoNewCommandIfNonEmpty(command);
        }

        private void LvRedundant_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MiPickRepresentative.IsChecked)
            {
                SelectionChangedPickingRepresentative(sender, e);
            }
            else if (MiPickSurvivor.IsChecked)
            {
                SelectionChangedPickingSurvivor(sender, e);
            }
            else
            {
                SelectionChangedCreatingShortcuts(sender, e);
            }
        }

        private void LvRedundant_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _filesToShortcut.Clear();
        }

        private void UpdateSorting()
        {
            var sortDesc = LvRedundant.Items.SortDescriptions;

            sortDesc.Clear();

            // The first sorting criterion has to be GroupId so the groups will display in order;
            // this guarantees sorting by size in descending order
            sortDesc.Add(new SortDescription("GroupId", ListSortDirection.Ascending));

            for (var i = _lastColumnsClicked.Count - 1; i >= 0; i--)
            {
                sortDesc.Add(new SortDescription(_lastColumnsClicked[i], 
                    _ascendingOrdescending[i]? ListSortDirection.Ascending : ListSortDirection.Descending));
            }
        }

        /// <summary>
        ///  Performs the redo of a command if the command is not empty
        /// </summary>
        /// <param name="command">The command to execute</param>
        private void DoNewCommandIfNonEmpty(UserCommand command)
        {
            if (command.Operations.Count == 0)
            {
                return; // only do commands that have non-empty operations
            }

            command.Redo();

            // remove all commands after the current point as they are not redoable any more
            if (_currOp < UserCommands.Count)
            {
                UserCommands.RemoveRange(_currOp, UserCommands.Count - _currOp);
            }
            UserCommands.Add(command);
            _currOp++;

            UpdateUndoRedoable();
        }

        #endregion

    }
}

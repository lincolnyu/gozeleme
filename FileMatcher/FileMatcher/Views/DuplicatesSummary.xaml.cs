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
using FileMatcherApp.Controllers;
using FileMatcherApp.Extensions;
using FileMatcherApp.Models;
using FileMatcherApp.FileGrouping;
using FileMatcherApp.Filters;
using System.Collections.Specialized;
using System.Threading;

namespace FileMatcherApp.Views
{
    /// <summary>
    /// Interaction logic for DuplicatesSummary.xaml
    /// </summary>
    public partial class DuplicatesSummary : INotifyPropertyChanged
    {
        #region Fields

        private int _currOp;
        private readonly List<string> _lastColumnsClicked = new List<string>();
        private readonly List<bool> _ascendingOrdescending = new List<bool>();
        private readonly List<FileInfoEx> _filesToShortcut = new List<FileInfoEx>();
        private double _progressPercentage;
        private string _status;

        private bool _isSearching;
        private string _pauseButtonTitle;

        private readonly DuplicatesFilter _filter;

        private Timer _filterUpdateTimer;
        private bool _filterUpdateRequested;

        #endregion

        #region Constructors

        public DuplicatesSummary(FileMatcherWorkingObject fmwo)
        {
            UserCommands = new List<UserCommand>();
            _currOp = 0;
            InitializeComponent();

            ProgressPercentage = 0;
            FileMatcherWorkingObject = fmwo;
            fmwo.FileMatcher.ProgressUpdated += FileMatcherOnUpdateProgress;
            fmwo.FileMatcher.StatusUpdated += FileMatcherOnUpdateStatus;
            var adaptor = fmwo.FileMatcher.Adaptor;
            Updater = new IdenticalFileListUpdater(adaptor, Dispatcher);
            IdenticalFiles = Updater.IdenticalFileList;
            IdenticalFiles.CollectionChanged += IdenticalFilesOnCollectionChanged;
            LvRedundant.ItemsSource = IdenticalFiles;
            _filter = new DuplicatesFilter();
            _filter.FilterChanged += FilterOnChanged;

            DataContext = this;
            DeleteToRecycleBin = true;

            IsSearching = true;
            PauseButtonTitle = Strings.PauseSearch;

            _filterUpdateTimer = new Timer(FilterUpdateTimerCallback, null, 5000, 5000);
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

        public bool IsSearching
        {
            get { return _isSearching; }
            set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;
                    RaisePropertyChangedEvent("IsSearching");
                }
            }
        }

        public string PauseButtonTitle
        {
            get { return _pauseButtonTitle; }
            set
            {
                if (_pauseButtonTitle != value)
                {
                    _pauseButtonTitle = value;
                    RaisePropertyChangedEvent("PauseButtonTitle");
                }
            }
        }

        public FileMatcherWorkingObject FileMatcherWorkingObject { get; private set; }

        public bool DeleteToRecycleBin { get; private set; }

        public IdenticalFileListUpdater Updater { get; private set; }

        public double ProgressPercentage
        {
            get
            {
                return _progressPercentage;
            }
            set
            {
                if (Math.Abs(_progressPercentage - value) > double.Epsilon)
                {
                    _progressPercentage = value;
                    RaisePropertyChangedEvent("ProgressPercentage");
                }
            }
        }

        /// <summary>
        ///  One line status suitable to display on the status bar
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    RaisePropertyChangedEvent("Status");
                }
            }
        }

        #endregion

        #region Methods

        public void InitializeDialog()
        {
            // populate the initial sorting precedence list with paths (properties of FileInfoEx)
            _lastColumnsClicked.Add("State");
            _lastColumnsClicked.Add("DirectoryName");
            _lastColumnsClicked.Add("Name");
            _ascendingOrdescending.Add(true);
            _ascendingOrdescending.Add(true);
            _ascendingOrdescending.Add(true);

            UpdateSorting();
        }

        private void IdenticalFilesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                var item = (FileInfoEx)args.NewItems[0];
                if (_filter.Emerged(item))
                {
                    FilterOnChanged();
                }
            }
        }

        private void LvRedundantOnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AddGrouping();
        }

        private void AddGrouping()
        {
            if (LvRedundant.ItemsSource == null)
            {
                return;
            }

            var myView = (CollectionView)CollectionViewSource.GetDefaultView(LvRedundant.ItemsSource);
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

            foreach (var fileToDelete in filesToDelete.Where(f => !processedGroups.Contains(f.GroupId)))
            {
                var groupId = fileToDelete.GroupId;
                processedGroups.Add(groupId);
                var start = IdenticalFiles.FindGroupStart(fileToDelete);
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

        private void MiApplyOnClick(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(DeleteToRecycleBin
                ? Strings.ProceedToApplyRecycling
                : Strings.ProceedToApplyDeletion,
                Strings.AppName, MessageBoxButton.YesNo);
            if (res != MessageBoxResult.Yes) return;
            var indicesToRemove = new List<int>();
            var totalSizeRemoved = 0L;
            var shortcutCount = 0;
            var filesToErase = new LinkedList<string>();
            for (var i = 0; i < IdenticalFiles.Count; i++)
            {
                var f = IdenticalFiles[i];
                if (f.Shortcut != null)
                {
                    var shellLink = (IShellLinkW)new CShellLink();
                    shellLink.SetDescription(f.ShortcutName);
                    shellLink.SetPath(f.Shortcut.FullName);
                    var linkFile = (IPersistFile)shellLink;
                    var name = Path.Combine(f.DirectoryName, f.ShortcutName) + ".lnk";
                    linkFile.Save(name, true);
                    shortcutCount++;
                }
                if (!f.IsSelectedToDelete)
                {
                    continue;
                }
                totalSizeRemoved += f.Length;

                filesToErase.AddLast(f.FullName);

                indicesToRemove.Add(i);
            }

            for (var i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                var index = indicesToRemove[i];
                IdenticalFiles.RemoveAt(index);
            }

            foreach (var f in filesToErase)
            {
                if (DeleteToRecycleBin)
                {
                    RecycleBin.SendSilent(f);
                }
                else
                {
                    File.Delete(f);
                }
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
            var vfw = new ViewFilterWindow(_filter)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            vfw.Show();
        }

        private void FilterOnChanged()
        {
            _filterUpdateRequested = true;
        }

        private void FilterUpdateTimerCallback(object state)
        {
            if (_filterUpdateRequested)
            {
                Dispatcher.Invoke(() =>
                {
                    LvRedundant.Items.Filter = _filter.Predicate;
                    _filterUpdateRequested = true;
                });
            }
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
            var fex = (FileInfoEx)sel;
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
                var fex = (FileInfoEx)sel;
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
                var fex = (FileInfoEx)sel;
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
            FileInfoEx fileWithGroupId = null;
            foreach (var sel in LvRedundant.SelectedItems)
            {
                var fex = (FileInfoEx)sel;
                if (groupId == -1)
                {
                    groupId = fex.GroupId;
                    fileWithGroupId = fex;
                }
                else if (groupId != fex.GroupId)
                {
                    e.CanExecute = false;
                    return;
                }
            }

            // then check if there are remaining items in the group other than the 
            // selected files and those that have been chosen to delete

            var start = IdenticalFiles.FindGroupStart(fileWithGroupId);
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
                var fex = (FileInfoEx)sel;
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

            var binding = header.Column.DisplayMemberBinding as Binding;
            if (binding == null) return;
            var bindingPath = binding.Path.Path;
            if (bindingPath != "Name" && bindingPath != "DirectoryName" && bindingPath != "State")
            {
                return;
            }

            var isAscending = false;
            for (var i = 0; i < _lastColumnsClicked.Count; i++)
            {
                if (_lastColumnsClicked[i] != bindingPath) continue;
                _lastColumnsClicked.RemoveAt(i);
                isAscending = _ascendingOrdescending[i];
                _ascendingOrdescending.RemoveAt(i);
                break;
            }
            _lastColumnsClicked.Add(bindingPath);
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

            sortDesc.Add(new SortDescription("Length", ListSortDirection.Descending));
            sortDesc.Add(new SortDescription("GroupId", ListSortDirection.Ascending));

            for (var i = _lastColumnsClicked.Count - 1; i >= 0; i--)
            {
                sortDesc.Add(new SortDescription(_lastColumnsClicked[i],
                    _ascendingOrdescending[i] ? ListSortDirection.Ascending : ListSortDirection.Descending));
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
        private void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void FileMatcherOnUpdateProgress(object sender)
        {
            var fm = FileMatcherWorkingObject.FileMatcher;
            Dispatcher.Invoke(() =>
            {
                ProgressPercentage = fm.Progress * 100;
                UpdateScanningStatus();
            });
        }

        private void FileMatcherOnUpdateStatus(object sender)
        {
            var fm = FileMatcherWorkingObject.FileMatcher;
            Dispatcher.Invoke(() =>
            {
                if (FileMatcherWorkingObject.Canceller.Canceled)
                {
                    Status = Strings.StatusCanceled;
                    ProgressPercentage = 100;
                    IsSearching = false;
                }
                else
                {
                    switch (fm.Status)
                    {
                        case FileMatcher.FileMatcher.Statuses.Done:
                            Status = Strings.StatusDone;
                            ProgressPercentage = 100;
                            IsSearching = false;
                            break;
                        case FileMatcher.FileMatcher.Statuses.Scanning:
                            UpdateScanningStatus();
                            break;
                        case FileMatcher.FileMatcher.Statuses.CleaningUp:
                            Status = Strings.StatusCleanup;
                            break;
                    }
                }
            });
        }

        private void UpdateScanningStatus()
        {
            var fm = FileMatcherWorkingObject.FileMatcher;
            var progressStr = string.Format(Strings.SearchProgressUpdateFormat, fm.NumDuplicateGroups,
                                fm.NumDuplicates, fm.TotalDuplicateBytes, fm.NumFilesAdded, fm.NumFilesFound);
            Status = string.Format("{0} {1}", FileMatcherWorkingObject.Canceller.Paused ?
                Strings.StatusPaused : Strings.StatusSearching, progressStr);
        }

        private void MiPauseOnClick(object sender, RoutedEventArgs e)
        {
            var paused = !FileMatcherWorkingObject.Canceller.Paused;
            FileMatcherWorkingObject.Canceller.Paused = paused;
            PauseButtonTitle = paused ? "Resume Search" : "Pause Search";
            FileMatcherOnUpdateStatus(this);
        }

        private void MiStopOnClick(object sender, RoutedEventArgs e)
        {
            var choice = MessageBox.Show(Strings.FileMatchingToCancel, Strings.Warning, MessageBoxButton.YesNo);
            if (choice == MessageBoxResult.Yes)
            {
                FileMatcherWorkingObject.Canceller.Canceled = true;
                FileMatcherOnUpdateStatus(this);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (FileMatcherWorkingObject.FileMatcher.Status != FileMatcher.FileMatcher.Statuses.Done)
            {
                FileMatcherWorkingObject.Canceller.Canceled = true;
            }

            _filterUpdateTimer?.Dispose();
        }

        private bool IsCompleted()
        {
            return FileMatcherWorkingObject.Canceller.Canceled 
                || FileMatcherWorkingObject.FileMatcher.Status == FileMatcher.FileMatcher.Statuses.Done;
        }

        #endregion
    }
}

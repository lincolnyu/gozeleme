using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using FileMatcher.Extensions;
using FileMatcher.Models;
using FileMatcher.Properties;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace FileMatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
            Title = Strings.AppName + Strings.VersionOpeningBracket
                    + GetRunningVersion() + " Beta" + Strings.VersionClosingBracket;
        }

        #endregion

        #region Methods

        #region Event handler

        private void BtnRemoveFolderClick(object sender, RoutedEventArgs e)
        {
            if (LstFolders.SelectedIndex < 0)
            {
                return;
            }
            LstFolders.Items.RemoveAt(LstFolders.SelectedIndex);
            UpdateBtnRedudantEnabled();
        }

        private void BtnAddFolderClick(object sender, RoutedEventArgs e)
        {
            var folder = TxtFileToAdd.Text;
            if (!Directory.Exists(folder))
            {
                MessageBox.Show(Strings.RequestValidFolder, Strings.AppName);
                return;
            }
            AddFolder(folder);
        }

        private void BtnRedundantClick(object sender, RoutedEventArgs e)
        {
            var folders = (from ListBoxItem item in LstFolders.Items select (string) item.Content).ToList();

            var fm =  new FileMatcherLib.FileMatcher(folders);

            if (fm.StartingDirectories.Count < folders.Count)
            {
                MessageBox.Show(Strings.RedudantSearchFolders, Strings.AppName);
            }

            var rs = new RedunduncySummary
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var fmwo = new FileMatcherWorkingObject
            {
                FileMatcher = fm
            };

            var pd = new ProgressDialog(fmwo) { Owner = this };

            ThreadPool.QueueUserWorkItem(FileMatcherWorkingMethod, fmwo);

            pd.ShowDialog();

            if (!fmwo.Finished)
            {
                fmwo.Finish.WaitOne();
            }

            if (fmwo.Canceled)
            {
                MessageBox.Show(Strings.FileMatchingCanceledByUser, Strings.Alert);
            }

            // redundancy summary
            var igs = fmwo.IdenticalGroups;
            rs.InitializeDialog(igs);

            rs.ShowDialog();
        }

        private void BtnAddLocation(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog {SelectedPath = TxtFileToAdd.Text, ShowNewFolderButton = false};
            if (dlg.ShowDialog(this.GetIWin32Window()) != System.Windows.Forms.DialogResult.OK) return;
            var selectedPath = dlg.SelectedPath;
            TxtFileToAdd.Text = selectedPath;
        }

        private void LstFolders_OnPreviewDrop(object sender, DragEventArgs e)
        {
            var added = false;
            foreach (var ss in e.Data.GetFormats().Select(f => e.Data.GetData(f)).OfType<string[]>())
            {
                foreach (var s in ss.Where(Directory.Exists))
                {
                    AddFolder(s);
                    added = true;
                }
                if (added)
                {
                    break;
                }
            }

            e.Handled = true;
        }

        private void TxtFileToAdd_OnPreviewDrop(object sender, DragEventArgs e)
        {
        }

        private void TxtFileToAdd_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            BtnAddFolder.IsEnabled = TxtFileToAdd.Text.Trim() != "";
        }

        private void LstFolders_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnRemoveFolder.IsEnabled = LstFolders.SelectedItems.Count > 0;
        }

        private void MainWindow_OnPreviewDrop(object sender, DragEventArgs e)
        {
        }

        private void BtnHelpClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Strings.OnlineHelpUrl);
        }

        #endregion

        private void AddFolder(string folder)
        {
            if (LstFolders.Items.Contains(folder)) return;
            var newItem = new ListBoxItem {Content = folder, ToolTip = folder};
            LstFolders.Items.Add(newItem);
            UpdateBtnRedudantEnabled();
        }

        private void UpdateBtnRedudantEnabled()
        {
            BtnRedundant.IsEnabled = LstFolders.Items.Count > 0;
        }

        private static void FileMatcherWorkingMethod(object a)
        {
            var fmwo = (FileMatcherWorkingObject)a;
            var pd = fmwo.ProgressDialog;

            fmwo.FileMatcher.UpdateProgress = pd.UpdateProgress;
            fmwo.FileMatcher.UpdateStatus = pd.UpdateStatus;

            var igs = fmwo.FileMatcher.GetIdenticalFiles(fmwo.Canceller);
            fmwo.Canceled = fmwo.Canceller.Canceled;    // the canceled signal before dialog close (which itself sends the signal) determines if it's been canceled by the user
            pd.Finish();
            fmwo.IdenticalGroups = igs;
            fmwo.Finished = true;
            fmwo.Finish.Set();
        }

        private string GetRunningVersion()
        {
            try
            {
                return System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            catch
            {
                try
                {
                    return Settings.Default.PublishVersion;
                }
                catch (Exception)
                {
                    return Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
            }
        }


        #endregion
    }
}

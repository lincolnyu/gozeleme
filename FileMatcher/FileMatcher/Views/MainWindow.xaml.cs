using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using FileMatcherApp.Extensions;
using FileMatcherApp.Models;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace FileMatcherApp.Views
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
            SetTitle();
        }

        #endregion

        #region Methods

        #region Event handler

        private void BtnRemoveFolderClick(object sender, RoutedEventArgs e)
        {
            var selectedIncluded = new object[LstFolders.SelectedItems.Count];
            LstFolders.SelectedItems.CopyTo(selectedIncluded, 0);
            foreach (var si in selectedIncluded)
            {
                LstFolders.Items.Remove(si);
            }

            var selectedExcluded = new object[LstExclFolders.SelectedItems.Count];
            LstExclFolders.SelectedItems.CopyTo(selectedExcluded, 0);
            foreach (var se in selectedExcluded)
            {
                LstExclFolders.Items.Remove(se);
            }

            UpdateBtnRedudantEnabled();
        }

        private void BtnAddIncludedFolderOnClick(object sender, RoutedEventArgs e)
        {
            var folder = TxtFileToAdd.Text;
            if (!Directory.Exists(folder))
            {
                MessageBox.Show(Strings.RequestValidFolder, Strings.AppName);
                return;
            }
            AddFolder(folder);
        }

        private void BtnAddExcludedFolderOnClick(object sender, RoutedEventArgs e)
        {
            var folder = TxtFileToAdd.Text;
            if (!Directory.Exists(folder))
            {
                MessageBox.Show(Strings.RequestValidFolder, Strings.AppName);
                return;
            }
            AddExclFolder(folder);
        }
        
        private void BtnStartSearchingClick(object sender, RoutedEventArgs e)
        {
            var folders = (from ListBoxItem item in LstFolders.Items select (string) item.Content).ToList();
            var excludedFolders = (from ListBoxItem item in LstExclFolders.Items select (string)item.Content).ToList();

            var fm =  new FileMatcher.FileMatcher(folders, excludedFolders);

            if (fm.StartingDirectories.Count < folders.Count)
            {
                MessageBox.Show(Strings.RedudantSearchFolders, Strings.AppName);
            }

            var fmwo = new FileMatcherWorkingObject
            {
                FileMatcher = fm
            };

            var rs = new DuplicatesSummary(fmwo)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            ThreadPool.QueueUserWorkItem(FileMatcherWorkingMethod, fmwo);

            // redundancy summary
            rs.InitializeDialog();

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


        private void LstExclFoldersOnPreviewDrop(object sender, DragEventArgs args)
        {
            var added = false;
            foreach (var ss in args.Data.GetFormats().Select(f => args.Data.GetData(f)).OfType<string[]>())
            {
                foreach (var s in ss.Where(Directory.Exists))
                {
                    AddExclFolder(s);
                    added = true;
                }
                if (added)
                {
                    break;
                }
            }
            args.Handled = true;
        }

        private void LstExclFoldersOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBtnRemoveFolderEnabledState();
        }

        private void UpdateBtnRemoveFolderEnabledState()
        {
            BtnRemoveFolder.IsEnabled
                = LstFolders.SelectedItems.Count > 0 ||
                LstExclFolders.SelectedItems.Count > 0;
        }

        private void TxtFileToAdd_OnPreviewDrop(object sender, DragEventArgs e)
        {
        }

        private void TxtFileToAdd_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            BtnAddIncludedFolder.IsEnabled = TxtFileToAdd.Text.Trim() != "";
            BtnAddExcludedFolder.IsEnabled = TxtFileToAdd.Text.Trim() != "";
        }

        private void LstFolders_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBtnRemoveFolderEnabledState();
        }

        private void MainWindowOnPreviewDrop(object sender, DragEventArgs e)
        {
        }

        private void BtnHelpClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Strings.OnlineHelpUrl);
        }

        #endregion

        private void AddFolder(string folder)
        {
            if (LstFolders.Items.Cast<ListBoxItem>().Select(x=>x.Content)
                .Contains(folder))
            {
                return;
            }
            var newItem = new ListBoxItem {Content = folder, ToolTip = folder};
            LstFolders.Items.Add(newItem);
            UpdateBtnRedudantEnabled();
        }

        private void AddExclFolder(string folder)
        {
            if (LstExclFolders.Items.Cast<ListBoxItem>().Select(x=>x.Content).
                Contains(folder))
            {
                return;
            }
            var newItem = new ListBoxItem { Content = folder, ToolTip = folder };
            LstExclFolders.Items.Add(newItem);
        }

        private void UpdateBtnRedudantEnabled()
        {
            BtnRedundant.IsEnabled = LstFolders.Items.Count > 0;
        }

        private static void FileMatcherWorkingMethod(object a)
        {
            var fmwo = (FileMatcherWorkingObject)a;

            fmwo.FileMatcher.GetIdenticalFiles(fmwo.Canceller);

            fmwo.Finished = true;
            fmwo.Finish.Set();
        }

        /// <summary>
        ///  Sets app title as per app name and version
        /// </summary>
        /// <remarks>
        ///  References:
        ///  1. http://stackoverflow.com/questions/22527830/how-to-get-the-publish-version-of-a-wpf-application
        /// </remarks>
        private void SetTitle()
        {
            try
            {
                var ver = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.
                    CurrentVersion;
                Title = string.Format("{0} (Ver {1}.{2} Beta)", Strings.AppName, ver.Major, ver.Minor);
            }
            catch (System.Deployment.Application.InvalidDeploymentException)
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                Title = string.Format("{0} (Asm Ver {1}.{2} Beta)", Strings.AppName, ver.Major, ver.Minor);
            }
        }

        #endregion
    }
}

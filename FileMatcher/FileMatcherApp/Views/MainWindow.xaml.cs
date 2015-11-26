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
            var selectedIncluded = new object[LstIncludedFolders.SelectedItems.Count];
            LstIncludedFolders.SelectedItems.CopyTo(selectedIncluded, 0);
            foreach (var si in selectedIncluded)
            {
                LstIncludedFolders.Items.Remove(si);
            }

            var selectedExcluded = new object[LstExcludedFolders.SelectedItems.Count];
            LstExcludedFolders.SelectedItems.CopyTo(selectedExcluded, 0);
            foreach (var se in selectedExcluded)
            {
                LstExcludedFolders.Items.Remove(se);
            }

            UpdateBtnRedudantEnabled();
        }

        private void BtnAddIncludedFolderOnClick(object sender, RoutedEventArgs e)
        {
            var folder = TxtFolderToAdd.Text;
            if (!Directory.Exists(folder))
            {
                MessageBox.Show(Strings.RequestValidFolder, Strings.AppName);
                return;
            }
            AddIncludedFolder(folder);
        }

        private void BtnAddExcludedFolderOnClick(object sender, RoutedEventArgs e)
        {
            var folder = TxtFolderToAdd.Text;
            if (!Directory.Exists(folder))
            {
                MessageBox.Show(Strings.RequestValidFolder, Strings.AppName);
                return;
            }
            AddExcludedFolder(folder);
        }
        
        private void BtnStartSearchingClick(object sender, RoutedEventArgs e)
        {
            var folders = (from ListBoxItem item in LstIncludedFolders.Items select (string) item.Content).ToList();
            var excludedFolders = (from ListBoxItem item in LstExcludedFolders.Items select (string)item.Content).ToList();

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
            var dlg = new FolderBrowserDialog {SelectedPath = TxtFolderToAdd.Text, ShowNewFolderButton = false};
            if (dlg.ShowDialog(this.GetIWin32Window()) != System.Windows.Forms.DialogResult.OK) return;
            var selectedPath = dlg.SelectedPath;
            TxtFolderToAdd.Text = selectedPath;
        }

        private void LstIncludedFoldersOnPreviewDrop(object sender, DragEventArgs e)
        {
            var added = false;
            foreach (var ss in e.Data.GetFormats().Select(f => e.Data.GetData(f)).OfType<string[]>())
            {
                foreach (var s in ss.Where(Directory.Exists))
                {
                    AddIncludedFolder(s);
                    added = true;
                }
                if (added)
                {
                    break;
                }
            }

            e.Handled = true;
        }


        private void LstExcludedFoldersOnPreviewDrop(object sender, DragEventArgs args)
        {
            var added = false;
            foreach (var ss in args.Data.GetFormats().Select(f => args.Data.GetData(f)).OfType<string[]>())
            {
                foreach (var s in ss.Where(Directory.Exists))
                {
                    AddExcludedFolder(s);
                    added = true;
                }
                if (added)
                {
                    break;
                }
            }
            args.Handled = true;
        }

        private void LstExcludedFoldersOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBtnRemoveFolderEnabledState();
        }

        private void UpdateBtnRemoveFolderEnabledState()
        {
            BtnRemoveFolder.IsEnabled
                = LstIncludedFolders.SelectedItems.Count > 0 ||
                LstExcludedFolders.SelectedItems.Count > 0;
        }

        private void WindowOnPreviewDrop(object sender, DragEventArgs args)
        {
            var ss = args.Data.GetFormats().Select(f => args.Data.GetData(f)).OfType<string[]>()
                .FirstOrDefault(x => x.Any(Directory.Exists));
            if (ss != null)
            {
                TxtFolderToAdd.Text = ss.First();
            }
            else
            {
                TxtFolderToAdd.Text = "";
            }
        }

        private void TxtFileToAddOnTextChanged(object sender, TextChangedEventArgs e)
        {
            BtnAddIncludedFolder.IsEnabled = TxtFolderToAdd.Text.Trim() != "";
            BtnAddExcludedFolder.IsEnabled = TxtFolderToAdd.Text.Trim() != "";
        }

        private void LstIncludedFoldersOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBtnRemoveFolderEnabledState();
        }
        
        private void BtnHelpClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Strings.OnlineHelpUrl);
        }

        #endregion

        private void AddIncludedFolder(string folder)
        {
            if (LstIncludedFolders.Items.Cast<ListBoxItem>().Select(x=>x.Content)
                .Contains(folder))
            {
                return;
            }
            var newItem = new ListBoxItem {Content = folder, ToolTip = folder};
            LstIncludedFolders.Items.Add(newItem);
            UpdateBtnRedudantEnabled();
        }

        private void AddExcludedFolder(string folder)
        {
            if (LstExcludedFolders.Items.Cast<ListBoxItem>().Select(x=>x.Content).
                Contains(folder))
            {
                return;
            }
            var newItem = new ListBoxItem { Content = folder, ToolTip = folder };
            LstExcludedFolders.Items.Add(newItem);
        }

        private void UpdateBtnRedudantEnabled()
        {
            BtnRedundant.IsEnabled = LstIncludedFolders.Items.Count > 0;
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

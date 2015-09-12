using System;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using FileMatcher.Models;

namespace FileMatcher
{
    /// <summary>
    ///  Interaction logic for ProgressDialog.xaml the dialog that displays the progress of a process 
    /// </summary>
    /// <remarks>
    ///  http://blog.nostatic.org/2007/12/wpf-progress-bars.html
    /// </remarks>
    public partial class ProgressDialog
    {
        #region Fields

        /// <summary>
        ///  The minimum interval between update calls in milliseconds
        /// </summary>
        private const int UpdateInterval = 200;

        /// <summary>
        ///  last time the UpdateStatus methods was called
        /// </summary>
        private DateTime _lastCallToUpdateStatus;

        /// <summary>
        ///  last time the UpdateProgress methods was called
        /// </summary>
        private DateTime _lastCallToUpdateProgress;

        #endregion

        #region Constructors

        /// <summary>
        ///  Instantiates a progress dialog
        /// </summary>
        /// <param name="fmwo">The working object associated with the dialog</param>
        public ProgressDialog(FileMatcherWorkingObject fmwo)
        {
            InitializeComponent();
            CancelButton.Click += (sender, args) => Cancel(); // note we chose not to call Close() here
            PauseButton.Click += (sender, args) => Pause();
            Closing += (sender, args) => Cancel();
            System.Diagnostics.Trace.Assert(fmwo != null);
            fmwo.ProgressDialog = this;
            FileMatcherWorkingObj = fmwo;
            _lastCallToUpdateStatus = DateTime.Now;
            _lastCallToUpdateProgress = DateTime.Now;
        }

        #endregion

        #region Properties

        public FileMatcherWorkingObject FileMatcherWorkingObj { get; private set; }

        #endregion

        #region Methods

        #region Event handlers

        /// <summary>
        ///  Cancels the process
        /// </summary>
        private void Cancel()
        {
            CancelButton.IsEnabled = false;
            CancelButton.Content = Strings.Cancelling;
            FileMatcherWorkingObj.Canceller.Canceled = true; // a cancelling signal sent to the working thread
        }

        private void Pause()
        {
            // TODO: localise the strings
            if ((string) PauseButton.Content == "Pause")
            {
                PauseButton.Content = "Resume";
                FileMatcherWorkingObj.Canceller.Paused = true;
            }
            else if ((string) PauseButton.Content == "Resume")
            {
                PauseButton.Content = "Pause";
                FileMatcherWorkingObj.Canceller.Paused = false;
            }
        }

        #endregion

        /// <summary>
        ///  Updates the progress to the UI
        /// </summary>
        /// <param name="progress"></param>
        public void UpdateProgress(double progress)
        {
            var now = DateTime.Now;
            if ((now - _lastCallToUpdateProgress).Milliseconds < UpdateInterval)
            {
                return;
            }
            _lastCallToUpdateProgress = now;
            if (FileMatcherWorkingObj.Canceller.Canceled)
            {
                // NOTE the dialog can be closed before the last invocation of this method happens, that's why we need this check
                return;
            }
            Dispatcher.Invoke(DispatcherPriority.Background,
                              (SendOrPostCallback)
                              delegate { Progress.SetValue(RangeBase.ValueProperty, progress); }, null);
        }

        /// <summary>
        ///  Updates the status to the UI
        /// </summary>
        /// <param name="status">The status message</param>
        public void UpdateStatus(string status)
        {
            var now = DateTime.Now;
            if ((now - _lastCallToUpdateStatus).Milliseconds < UpdateInterval)
            {
                return;
            }
            _lastCallToUpdateStatus = now;
            if (FileMatcherWorkingObj.Canceller.Canceled)
            {
                // NOTE the dialog can be closed before the last invocation of this method happens, that's why we need this check
                return;
            }
            Dispatcher.Invoke(DispatcherPriority.Input,
                              (SendOrPostCallback)
                              delegate { StatusText.SetValue(TextBox.TextProperty, status); }, null);
        }

        /// <summary>
        ///  Indicates the end of the process and closes the dialog
        /// </summary>
        public void Finish()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Send,
                              (SendOrPostCallback) delegate
                                  {
                                      Close();
                                  }, null);
        }

        #endregion
    }
}

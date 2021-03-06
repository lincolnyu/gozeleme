﻿using System.Threading;
using FileMatcher;

namespace FileMatcherApp.Models
{
    public class FileMatcherWorkingObject
    {
        #region Constructors

        public FileMatcherWorkingObject()
        {
            Canceller = new FileMatchingCanceller();
            Finished = false;
            Finish = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        #endregion

        #region Properties

        public FileMatcher.FileMatcher FileMatcher { get; set; }

        //public ProgressDialog ProgressDialog { get; set; }

        public EventWaitHandle Finish { get; private set; }

        public bool Finished { get; set; }

        public FileMatchingCanceller Canceller { get; private set; }

        #endregion
    }
}

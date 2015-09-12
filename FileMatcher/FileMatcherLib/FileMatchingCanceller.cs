namespace FileMatcherLib
{
    /// <summary>
    ///  an object that indicates if the file matching process is to be cancelled by the user
    /// </summary>
    public class FileMatchingCanceller
    {
        #region Delegates

        public delegate void ParameterlessEventDelegate();


        #endregion

        #region Fields

        private volatile bool _canceled;

        private volatile bool _paused;

        #endregion

        #region Constructors

        /// <summary>
        ///  Instantiates a canceller and initialises the Canceled state to false
        /// </summary>
        public FileMatchingCanceller()
        {
            Canceled = false;
            Paused = false;
        }

        #endregion

        #region Events

        public event ParameterlessEventDelegate CanceledEvent;

        public event ParameterlessEventDelegate PauseStateChangeEvent;


        #endregion

        #region Properties

        /// <summary>
        ///  The flag that indicates if it's to be cancelled
        /// </summary>
        public bool Canceled
        {
            get { return _canceled; }
            set
            {
                lock(this)
                {
                    _canceled = value;
                    if (!_canceled) return;
                    Paused = false;
                    if (CanceledEvent != null)
                    {
                        CanceledEvent();
                    }
                }
            }
        }

        public bool Paused
        {
            get { return _paused && !Canceled; }
            set
            {
                lock (this)
                {
                    _paused = value;
                    if (PauseStateChangeEvent != null)
                    {
                        PauseStateChangeEvent();
                    }
                }
            }
        }

        #endregion
    }
}

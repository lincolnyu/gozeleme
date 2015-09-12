using System.Windows.Input;

namespace FileMatcher
{
    public static class CommandLibrary
    {
        #region Fields
        // ReSharper disable InconsistentNaming

        private static readonly RoutedUICommand _openFolder 
            = new RoutedUICommand("Open folder", "OpenFolder", typeof(CommandLibrary));

        private static readonly RoutedUICommand _delete
            = new RoutedUICommand("Delete", "Delete", typeof(CommandLibrary));

        private static readonly RoutedUICommand _undelete
            = new RoutedUICommand("Undelete", "Undelete", typeof(CommandLibrary));

        private static readonly RoutedUICommand _shortcut
            = new RoutedUICommand("Replace with Shortcut", "Shortcut", typeof(CommandLibrary));

        // ReSharper restore InconsistentNaming
        #endregion

        #region Properties

        public static RoutedUICommand OpenFolder
        {
            get { return _openFolder; }
        }

        public static RoutedUICommand Delete
        {
            get { return _delete; }
        }

        public static RoutedUICommand Undelete
        {
            get { return _undelete; }
        }

        public static RoutedUICommand Shortcut
        {
            get { return _shortcut; }
        }

        #endregion
    }
}

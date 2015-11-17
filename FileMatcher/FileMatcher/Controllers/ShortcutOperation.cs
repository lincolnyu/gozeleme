using System.IO;
using System.Windows.Forms;
using FileMatcherApp.Models;

namespace FileMatcherApp.Controllers
{
    class ShortcutOperation : IOperation
    {
        #region Fields

        public readonly FileInfoEx Replaced;

        public readonly FileInfoEx Target;
        public readonly string ShortcutFileName;

        private FileInfoEx _originalTarget;
        private string _originalShortcutFileName;

        #endregion

        #region Constructors

        public ShortcutOperation(FileInfoEx replaced, FileInfoEx target, string shortcutFileName)
        {
            Replaced = replaced;
            Target = target;
            ShortcutFileName = shortcutFileName;
        }

        /// <summary>
        ///  This is to create a remove-shortcut operation
        /// </summary>
        /// <param name="replaced">The file to remove shortcut</param>
        public ShortcutOperation(FileInfoEx replaced)
        {
            Replaced = replaced;
            Target = null;
            ShortcutFileName = null;
        }

        #endregion

        #region Methods
        #region IOperation members

        public void Redo()
        {
            _originalShortcutFileName = Replaced.ShortcutName;
            _originalTarget = Replaced.Shortcut;

            Replaced.ShortcutName = ShortcutFileName;
            Replaced.Shortcut = Target;
        }

        public void Undo()
        {
            Replaced.ShortcutName = _originalShortcutFileName;
            Replaced.Shortcut = _originalTarget;
        }

        #endregion
        #endregion
    }
}

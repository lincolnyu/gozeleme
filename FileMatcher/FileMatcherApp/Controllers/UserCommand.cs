using System.Collections.Generic;

namespace FileMatcherApp.Controllers
{
    public class UserCommand
    {
        #region Fields

        private readonly List<IOperation> _operations = new List<IOperation>();

        #endregion

        #region Properties

        public List<IOperation> Operations { get { return _operations; } }

        #endregion

        #region Methods

        public void Redo()
        {
            foreach (var operation in _operations)
            {
                operation.Redo();
            }
        }

        public void Undo()
        {
            foreach (var operation in _operations)
            {
                operation.Undo();
            }
        }

        #endregion
    }
}

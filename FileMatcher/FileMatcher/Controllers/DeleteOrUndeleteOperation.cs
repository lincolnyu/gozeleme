namespace FileMatcherApp.Controllers
{
    public class DeleteOrUndeleteOperation : IOperation
    {
        #region Enumerations

        public enum ActionTypes
        {
            Delete,
            Undelete
        }

        #endregion

        #region Fields

        public readonly FileInfoEx File;

        public readonly ActionTypes ActionType;

        private bool _originalState;

        #endregion

        #region Constructors

        public DeleteOrUndeleteOperation(ActionTypes actionType, FileInfoEx file)
        {
            ActionType = actionType;
            File = file;
        }

        #endregion

        #region Methods

        #region IOperation members

        public void Redo()
        {
            _originalState = File.IsSelectedToDelete;
            File.IsSelectedToDelete = ActionType == ActionTypes.Delete;
        }

        public void Undo()
        {
            File.IsSelectedToDelete = _originalState;
        }

        #endregion

        #endregion
    }
}

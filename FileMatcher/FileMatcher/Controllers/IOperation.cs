﻿namespace FileMatcher.Controllers
{
    public interface IOperation
    {
        #region Methods
        
        void Redo();
        void Undo();

        #endregion
    }
}

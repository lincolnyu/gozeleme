using System;

namespace FileMatcherLib
{
    /// <summary>
    ///  A delegate that updates the status of an internal process to the UI
    /// </summary>
    /// <param name="status">The string that represents the status</param>
    public delegate void UpdateStatusDelegate(String status);
}

namespace FileMatcherLib
{
    /// <summary>
    ///  A delegate that updates the progress of an internal process to the UI
    /// </summary>
    /// <param name="progress">The progress (between 0 and 1)</param>
    public delegate void UpdateProgressDelegate(double progress);
}

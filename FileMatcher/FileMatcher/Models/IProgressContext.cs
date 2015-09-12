namespace FileMatcher
{
    public interface IProgressContext
    {
        void UpdateProgress(double progress);
        void UpdateStatus(string status);
        void Finish();
        bool Canceled { get; }
    }
}

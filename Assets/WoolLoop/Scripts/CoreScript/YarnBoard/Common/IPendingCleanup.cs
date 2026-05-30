namespace Common
{
    public interface IPendingCleanup
    {
        bool IsPendingCleanup { get; }
        void CleanupForLevelUnload();
    }
}

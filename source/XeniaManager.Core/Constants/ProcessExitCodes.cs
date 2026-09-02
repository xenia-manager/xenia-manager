namespace XeniaManager.Core.Constants;

/// <summary>
/// Cross-app process exit codes used by the desktop app to interpret how a
/// BigScreen session ended.
/// </summary>
public static class ProcessExitCodes
{
    /// <summary>
    /// BigScreen closed with "Return to Xenia Manager" off - the desktop app
    /// should shut down too instead of restoring its window.
    /// </summary>
    public const int CloseEverything = 1;
}
namespace XeniaManager.Core;

public static class Constants
{
    // Directories
    public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string DownloadDir = Path.Combine(Constants.BaseDirectory, "Downloads");
}
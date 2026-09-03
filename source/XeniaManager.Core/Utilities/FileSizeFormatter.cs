namespace XeniaManager.Core.Utilities;

/// <summary>
/// Formats byte counts into human-readable strings.
/// </summary>
public static class FileSizeFormatter
{
    /// <summary>
    /// Formats a byte count (e.g. 12.4 MB, 512 B).
    /// </summary>
    /// <param name="bytes">The byte count.</param>
    /// <returns>Human-readable size string.</returns>
    public static string FormatBytes(long bytes)
    {
        const long kb = 1024;
        const long mb = 1024 * 1024;
        const long gb = 1024L * 1024 * 1024;
        return bytes switch
        {
            >= gb => $"{bytes / (double)gb:F1} GB",
            >= mb => $"{bytes / (double)mb:F1} MB",
            >= kb => $"{bytes / (double)kb:F1} KB",
            _ => $"{bytes} B"
        };
    }
}
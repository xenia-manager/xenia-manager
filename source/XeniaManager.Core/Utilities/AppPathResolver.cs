namespace XeniaManager.Core.Utilities.Paths;

/// <summary>
/// Provides helper methods for converting relative paths to absolute paths
/// based on the application's runtime base directory.
/// </summary>
public static class AppPathResolver
{
    private static readonly string _baseDirectory = ResolveBaseDirectory();

    /// <summary>
    /// Resolves the most appropriate base directory for the application at runtime.
    /// </summary>
    /// <returns>
    /// An absolute path representing the application's base directory.
    /// </returns>
    private static string ResolveBaseDirectory()
    {
        // Most accurate method of returning the directory where the executable is
        string? exePath = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrEmpty(exePath))
        {
            return exePath;
        }

        // NOTE: Can return the temp folder if used as a single file
        string appDomainDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!string.IsNullOrEmpty(appDomainDir) && !IsTempDirectory(appDomainDir))
        {
            return appDomainDir;
        }

        // NOTE: Can return the temp folder if used as a single file
        string appContextDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(appContextDir) && !IsTempDirectory(appContextDir))
        {
            return appContextDir;
        }

        // Return the current working directory as a last resort
        return Directory.GetCurrentDirectory();

        // Determines whether the specified path is located within the system temp directory
        static bool IsTempDirectory(string path)
        {
            string tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
            string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            return normalizedPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Base directory for the app
    /// </summary>
    /// <returns>Absolute path to the directory where the application is located</returns>
    public static string BaseDirectory() => _baseDirectory;

    /// <summary>
    /// Converts a relative path into an absolute path using the application's base directory.
    /// </summary>
    /// <param name="relativePath">A path relative to the application's base directory.</param>
    /// <returns>The absolute path.</returns>
    public static string GetFullPath(string relativePath) => Path.Combine(_baseDirectory, relativePath);

    /// <summary>
    /// Combines multiple relative path segments into a single absolute path
    /// using the application's base directory.
    /// </summary>
    /// <param name="relativePaths">An ordered set of relative path segments.</param>
    /// <returns>The resulting absolute path.</returns>
    public static string GetFullPath(params string[] relativePaths) => Path.Combine(new[] { _baseDirectory }.Concat(relativePaths).ToArray());
}
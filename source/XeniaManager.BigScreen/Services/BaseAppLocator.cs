using System;
using System.IO;
using XeniaManager.BigScreen.Constants;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Locates the base Xenia Manager application folder so BigScreen can share its
/// game library, games, artwork and profile data.
/// </summary>
public static class BaseAppLocator
{
    /// <summary>
    /// Resolves the base app directory, or null when it can't be determined
    /// (the app then falls back to its own folder).
    /// </summary>
    public static string? Resolve(string[] args)
    {
        // 1. Explicit command-line override: --base-dir <path>
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "--base-dir" or "-d" && !string.IsNullOrWhiteSpace(args[i + 1]))
            {
                return Path.GetFullPath(args[i + 1]);
            }
        }

        string? exeDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrEmpty(exeDirectory))
        {
            return null;
        }

        // 2. Side-by-side deployment: XeniaManager.exe next to BigScreen.exe
        if (File.Exists(Path.Combine(exeDirectory, AppConstants.BaseAppExecutable)))
        {
            return exeDirectory;
        }

        // 3. Repo/dev layout: sibling project folder with the same bin configuration
        //    ...\XeniaManager.BigScreen\bin\{Debug|Release}\net10.0\
        //    -> ...\XeniaManager\bin\{Debug|Release}\net10.0\
        DirectoryInfo exeDirInfo = new DirectoryInfo(exeDirectory);
        string tfm = exeDirInfo.Name;
        string? config = exeDirInfo.Parent?.Name;

        DirectoryInfo? current = exeDirInfo;
        for (int i = 0; i < 6 && current != null; i++, current = current.Parent)
        {
            string sibling = Path.Combine(current.FullName, "XeniaManager", "bin", config ?? string.Empty, tfm);
            if (File.Exists(Path.Combine(sibling, AppConstants.BaseAppExecutable)))
            {
                return sibling;
            }
        }

        return null;
    }
}

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
    /// How many parent directories are walked when hunting the sibling project.
    /// </summary>
    private const int MaxParentWalkDepth = 6;

    /// <summary>
    /// Resolves the base directory from an explicit command-line override
    /// (<c>--base-dir &lt;path&gt;</c> or <c>-d &lt;path&gt;</c>).
    /// </summary>
    private static string? ResolveFromCommandLine(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "--base-dir" or "-d")
            {
                if (!string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    return Path.GetFullPath(args[i + 1]);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the base directory from a side-by-side deployment
    /// (XeniaManager.exe next to BigScreen.exe).
    /// </summary>
    private static string? ResolveSideBySide(string exeDirectory)
    {
        if (File.Exists(Path.Combine(exeDirectory, AppConstants.BaseAppExecutable)))
        {
            return exeDirectory;
        }

        return null;
    }

    /// <summary>
    /// Resolves the base directory from a repo/dev layout: a sibling project
    /// folder with the same bin configuration
    /// (<c>...\XeniaManager.BigScreen\bin\{Debug|Release}\net10.0\</c> →
    /// <c>...\XeniaManager\bin\{Debug|Release}\net10.0\</c>).
    /// </summary>
    private static string? ResolveDevLayout(string exeDirectory)
    {
        DirectoryInfo exeDirInfo = new DirectoryInfo(exeDirectory);
        string tfm = exeDirInfo.Name;
        string? config = exeDirInfo.Parent?.Name;

        DirectoryInfo? current = exeDirInfo;
        for (int i = 0; i < MaxParentWalkDepth && current != null; i++, current = current.Parent)
        {
            string sibling = Path.Combine(current.FullName, "XeniaManager", "bin", config ?? string.Empty, tfm);
            if (File.Exists(Path.Combine(sibling, AppConstants.BaseAppExecutable)))
            {
                return sibling;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the base app directory, or null when it can't be determined
    /// (the app then falls back to its own folder).
    /// </summary>
    public static string? Resolve(string[] args)
    {
        string? fromArgs = ResolveFromCommandLine(args);
        if (fromArgs != null)
        {
            return fromArgs;
        }

        string? exeDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrEmpty(exeDirectory))
        {
            return null;
        }

        return ResolveSideBySide(exeDirectory) ?? ResolveDevLayout(exeDirectory);
    }
}
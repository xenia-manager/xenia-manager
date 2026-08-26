namespace XeniaManager.Core.Models;

/// <summary>
/// Defines how content packages get installed into Xenia's content directory.
/// </summary>
public enum ContentInstallationMethod
{
    /// <summary>
    /// Extracts the package contents into Xenia's directory structure (compatible with every Xenia version)
    /// </summary>
    ExtractedFolder,

    /// <summary>
    /// Copies the package file into Xenia's content directory as-is (requires a Xenia Canary build with XContent package support)
    /// </summary>
    PackageFile
}
namespace XeniaManager.Core.Models;

/// <summary>
/// Represents cached build information for Xenia emulator releases
/// </summary>
/// <param name="TagName">The version tag name of the release</param>
/// <param name="Date">The release date of the build</param>
/// <param name="Url">The download URL for the build</param>
public record CachedBuild(string TagName, DateTime Date, string Url);

/// <summary>
/// Represents cached build information for Xenia Manager releases
/// </summary>
/// <param name="Version">The version string of the manager</param>
public record ManagerBuild(string Version, string Url);

/// <summary>
/// Enum representing different types of releases that can be cached
/// </summary>
public enum ReleaseType
{
    /// <summary>Xenia Canary release</summary>
    XeniaCanary,

    /// <summary>Xenia Netplay Stable release</summary>
    NetplayStable,

    /// <summary>Xenia Netplay Nightly release</summary>
    NetplayNightly,

    /// <summary>Xenia Mousehook Standard release</summary>
    MousehookStandard,

    /// <summary>Xenia Mousehook Netplay release</summary>
    MousehookNetplay,

    /// <summary>Xenia Manager Stable release</summary>
    XeniaManagerStable,

    /// <summary>Xenia Manager Experimental release</summary>
    XeniaManagerExperimental
}

/// <summary>
/// Cache class that holds information about various Xenia emulator and Xenia Manager releases
/// </summary>
public class ReleaseCache
{
    /// <summary>Cached information for Xenia Canary release</summary>
    public CachedBuild? XeniaCanary { get; set; }

    /// <summary>Cached information for Xenia Netplay Stable release</summary>
    public CachedBuild? NetplayStable { get; set; }

    /// <summary>Cached information for Xenia Netplay Nightly release</summary>
    public CachedBuild? NetplayNightly { get; set; }

    /// <summary>Cached information for Xenia Mousehook Standard release</summary>
    public CachedBuild? MousehookStandard { get; set; }

    /// <summary>Cached information for Xenia Mousehook Netplay release</summary>
    public CachedBuild? MousehookNetplay { get; set; }

    /// <summary>Cached information for Xenia Manager Stable release</summary>
    public ManagerBuild? XeniaManagerStable { get; set; }

    /// <summary>Cached information for Xenia Manager Experimental release</summary>
    public ManagerBuild? XeniaManagerExperimental { get; set; }

    /// <summary>
    /// Converts the manifest cache properties to a dictionary representation
    /// </summary>
    /// <returns>A dictionary mapping property names to their values</returns>
    public Dictionary<string, object?> AsDictionary() => new Dictionary<string, object?>
    {
        ["XeniaCanary"] = XeniaCanary,
        ["NetplayStable"] = NetplayStable,
        ["NetplayNightly"] = NetplayNightly,
        ["MousehookStandard"] = MousehookStandard,
        ["MousehookNetplay"] = MousehookNetplay,
        ["XeniaManagerStable"] = XeniaManagerStable,
        ["XeniaManagerExperimental"] = XeniaManagerExperimental
    };
}
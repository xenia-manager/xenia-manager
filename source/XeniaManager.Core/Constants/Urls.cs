namespace XeniaManager.Core.Constants;

/// <summary>
/// Provides URLs for accessing Xbox database services, game compatibility data,
/// and related resources.
/// </summary>
public static class Urls
{
    /// <summary> Base URL for Xenia Manager.</summary>
    public static readonly string XeniaManagerBase = "https://xenia-manager.github.io/";
    /// <summary> Base URL for Xenia Manager database.</summary>
    public static readonly string XeniaManagerDatabaseBase = $"{XeniaManagerBase}database/data/";

    // Xbox Database URLs
    /// <summary>Base URL for Xbox 360 database.</summary>
    public static readonly string XboxDatabaseBase = $"{XeniaManagerBase}x360db/";
    /// <summary>URL for the Xbox Marketplace games database in JSON format.</summary>
    public static readonly string XboxDatabase = $"{XboxDatabaseBase}games.json";
    /// <summary>URL for detailed Xbox game information.</summary>
    public static readonly string XboxDatabaseGameInfo = XboxDatabaseBase + "titles/{0}/info.json";
    /// <summary>Base URL for Xbox database artwork assets.</summary>
    public static readonly string XboxDatabaseArtworkBase = XboxDatabaseBase + "titles/{0}/artwork/{1}";

    // Game Compatibility
    /// <summary>URL to the game compatibility JSON database.</summary>
    public static readonly string GameCompatibility = $"{XeniaManagerDatabaseBase}game-compatibility/canary.json";

    // Xenia Manager
    /// <summary>URL to the .JSON file containing information about latest Xenia Manager Versions</summary>
    public static readonly string LatestXeniaManagerVersions = $"{XeniaManagerDatabaseBase}version.json";

    // Optimized Settings
    public static readonly string OptimizedSettings = XeniaManagerBase + "optimized-settings/settings/{0}.json";

    // Xenia Builds & Extensions
    /// <summary>URL to the nightly build of Xenia Netplay for Windows.</summary>
    public static readonly string NetplayNightlyBuild = "https://nightly.link/AdrianCassar/xenia-canary/workflows/Windows_build/netplay_canary_experimental/xenia_canary_netplay_windows.zip";
    /// <summary>URL to mousehook bindings configuration.</summary>
    public static readonly string MousehookBindings = "https://raw.githubusercontent.com/marinesciencedude/xenia-canary-mousehook/refs/heads/mousehook/bindings.ini";
}
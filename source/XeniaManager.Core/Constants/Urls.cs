namespace XeniaManager.Core.Constants;
/// <summary>
/// Provides URLs for accessing Xbox database services, game compatibility data,
/// and related resources.
/// </summary>
public static class Urls
{
    /// <summary>URL for the Xbox Marketplace games database in JSON format.</summary>
    public static readonly string XboxDatabase = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/xbox_marketplace_games.json";

    /// <summary>URL for detailed Xbox game information.</summary>
    public static readonly string XboxDatabaseGameInfo = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/Xbox%20Marketplace";

    /// <summary>Base URL for Xbox database artwork assets.</summary>
    public static readonly string XboxDatabaseArtworkBase = "https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork";

    /// <summary>URL to the game compatibility JSON database.</summary>
    public static readonly string GameCompatibility = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/game_compatibility.json";

    /// <summary>URL to the nigthly build of Xenia Netplay for Windows.</summary>
    public static readonly string NetplayNightlyBuild = "https://nightly.link/AdrianCassar/xenia-canary/workflows/Windows_build/netplay_canary_experimental/xenia_canary_netplay_windows.zip";
}
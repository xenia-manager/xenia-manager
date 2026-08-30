namespace XeniaManager.Database.Constants;

/// <summary>
/// URLs used by the database layer. Extracted from XeniaManager.Core.Constants.Urls
/// to make XeniaManager.Database leaf-independent from Core.
/// </summary>
public static class DatabaseUrls
{
    private const string GITHUB_PAGES = "https://xenia-manager.github.io";
    private const string GITHUB_RAW = "https://raw.githubusercontent.com";

    /// <summary>
    /// Array of URLs to fetch the Xbox Marketplace games database.
    /// </summary>
    public static readonly string[] XboxMarketplaceDatabase =
    [
        $"{GITHUB_PAGES}/x360db/games.json",
        $"{GITHUB_RAW}/xenia-manager/x360db/main/games.json"
    ];

    public static readonly string[] XboxMarketplaceDatabaseGameInfo =
    [
        GITHUB_PAGES + "/x360db/titles/{0}/info.json",
        GITHUB_RAW + "/xenia-manager/x360db/main/titles/{0}/info.json"
    ];

    public static readonly string[] XboxMarketplaceDatabaseArtwork =
    [
        GITHUB_PAGES + "/x360db/titles/{0}/artwork/{1}",
        GITHUB_RAW + "/xenia-manager/x360db/main/titles/{0}/artwork/{1}"
    ];

    public static readonly string[] GameCompatibilityDatabase =
    [
        GITHUB_PAGES + "/database/data/game-compatibility/canary.json",
        GITHUB_RAW + "/xenia-manager/database/main/data/game-compatibility/canary.json"
    ];

    public static readonly string[] MousehookCompatibilityDatabase =
    [
        GITHUB_PAGES + "/database/data/game-compatibility/mousehook.json",
        GITHUB_RAW + "/xenia-manager/database/main/data/game-compatibility/mousehook.json"
    ];

    public static readonly string[] NetplayCompatibilityDatabase =
    [
        GITHUB_PAGES + "/database/data/game-compatibility/netplay.json",
        GITHUB_RAW + "/xenia-manager/database/main/data/game-compatibility/netplay.json"
    ];

    public static readonly string[] OptimizedSettingsDatabase =
    [
        $"{GITHUB_PAGES}/optimized-settings/data/settings.json",
        $"{GITHUB_RAW}/xenia-manager/optimized-settings/main/data/settings.json"
    ];

    public static readonly string[] BaseOptimizedSettingsUrl =
    [
        $"{GITHUB_PAGES}/optimized-settings/settings/",
        $"{GITHUB_RAW}/xenia-manager/optimized-settings/main/settings/"
    ];

    public static class PatchesDatabase
    {
        public static readonly string[] CanaryPatches =
        [
            GITHUB_PAGES + "/database/data/patches/canary.json",
            GITHUB_RAW + "/xenia-manager/database/main/data/patches/canary.json"
        ];

        public static readonly string[] NetplayPatches =
        [
            GITHUB_PAGES + "/database/data/patches/netplay.json",
            GITHUB_RAW + "/xenia-manager/database/main/data/patches/netplay.json"
        ];
    }
}
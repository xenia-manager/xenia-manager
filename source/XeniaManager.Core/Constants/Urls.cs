namespace XeniaManager.Core.Constants;

/// <summary>
/// Contains all URLs used throughout the application
/// Organized by category for easy maintenance and updates
/// </summary>
public class Urls
{
    /// <summary>
    /// Contains base URLs used throughout the application
    /// </summary>
    public static class Base
    {
        /// <summary>
        /// Base URL for the Xenia Manager GitHub Pages site
        /// Used as the primary source for various resources
        /// </summary>
        public const string GITHUB_PAGES = "https://xenia-manager.github.io";

        /// <summary>
        /// Base URL for raw GitHub content
        /// Used as an alternative source for resources hosted on GitHub
        /// </summary>
        public const string GITHUB_RAW = "https://raw.githubusercontent.com";
    }

    /// <summary>
    /// Array of URLs to fetch the "version.json" file containing information about latest releases of Xenia & Xenia Manager
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/database/data/version.json)
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/xenia-manager/database/main/data/version.json)
    /// </summary>
    public static readonly string[] Manifest =
    [
        $"{Base.GITHUB_PAGES}/database/data/version.json",
        $"{Base.GITHUB_RAW}/xenia-manager/database/main/data/version.json"
    ];

    /// <summary>
    /// Array of URLs to fetch the "gamecontrollerdb.txt" file containing game controller mappings for Xenia SDL
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source
    /// 2. Raw GitHub - Fallback source
    /// </summary>
    public static readonly string[] GameControllerDatabase =
    [
        $"{Base.GITHUB_PAGES}/database/data/gamecontrollerdb.txt",
        $"{Base.GITHUB_RAW}/xenia-manager/database/main/data/gamecontrollerdb.txt"
    ];

    /// <summary>
    /// URL to fetch the Xenia Mousehook bindings file (bindings.ini).
    /// This file contains button mapping configurations for Xenia Mousehook
    /// and is used by the application to provide game-specific control bindings.
    /// Source:
    /// - Raw GitHub - Official Xenia Mousehook repository
    /// </summary>
    public static readonly string XeniaMousehookBindingsFile = @"https://raw.githubusercontent.com/marinesciencedude/xenia-canary-mousehook/refs/heads/mousehook/bindings.ini";

    /// <summary>
    /// Array of URLs to fetch the Xbox Marketplace games database.
    /// This database contains information about Xbox 360 games and is used by the application
    /// to retrieve game details and metadata. Multiple URLs are provided to ensure availability,
    /// with fallback options in case the primary source is not reachable.
    /// Sources include:
    /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/x360db/games.json)
    /// 2. Raw GitHub - Secondary source (https://raw.githubusercontent.com/xenia-manager/x360db/main/games.json)
    /// </summary>
    public static readonly string[] XboxMarketplaceDatabase =
    [
        $"{Base.GITHUB_PAGES}/x360db/games.json",
        $"{Base.GITHUB_RAW}/xenia-manager/x360db/main/games.json"
    ];

    /// <summary>
    /// Array of URLs to fetch detailed game information from the Xbox marketplace database
    /// These are format strings with {0} as a placeholder for the title ID
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/x360db/titles/{0}/info.json)
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/xenia-manager/x360db/main/titles/{0}/info.json)
    /// </summary>
    public static readonly string[] XboxMarketplaceDatabaseGameInfo =
    [
        Base.GITHUB_PAGES + "/x360db/titles/{0}/info.json",
        Base.GITHUB_RAW + "/xenia-manager/x360db/main/titles/{0}/info.json"
    ];

    /// <summary>
    /// Array of URLs to fetch artwork files from the Xbox marketplace database
    /// These are format strings with {0} as a placeholder for the title ID and {1} as a placeholder for the artwork filename
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/x360db/titles/{0}/artwork/{1})
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/xenia-manager/x360db/main/titles/{0}/artwork/{1})
    /// </summary>
    public static readonly string[] XboxMarketplaceDatabaseArtwork =
    [
        Base.GITHUB_PAGES + "/x360db/titles/{0}/artwork/{1}",
        Base.GITHUB_RAW + "/xenia-manager/x360db/main/titles/{0}/artwork/{1}"
    ];

    /// <summary>
    /// Array of URLs to fetch the Game Compatibility database.
    /// This database contains information about game compatibility ratings with the emulator
    /// and is used by the application to retrieve compatibility status for games.
    /// Multiple URLs are provided to ensure availability, with fallback options in case
    /// the primary source is not reachable.
    /// Sources include:
    /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/database/game-compatibility/canary.json)
    /// 2. Raw GitHub - Secondary source (https://raw.githubusercontent.com/xenia-manager/database/main/data/game-compatibility/canary.json)
    /// 3. Cloudflare Pages - Backup source (https://xeniamanagerdb.pages.dev/data/game-compatibility/canary.json)
    /// </summary>
    public static readonly string[] GameCompatibilityDatabase =
    [
        Base.GITHUB_PAGES + "/database/data/game-compatibility/canary.json",
        Base.GITHUB_RAW + "/xenia-manager/database/main/data/game-compatibility/canary.json"
    ];

    /// <summary>
    /// Array of URLs to fetch list of optimized settings
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/optimized-settings/data/settings.json)
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/xenia-manager/optimized-settings/main/data/settings.json)
    /// </summary>
    public static readonly string[] OptimizedSettingsDatabase =
    [
        $"{Base.GITHUB_PAGES}/optimized-settings/data/settings.json",
        $"{Base.GITHUB_RAW}/xenia-manager/optimized-settings/main/data/settings.json"
    ];

    /// <summary>
    /// Array of base URLs to fetch optimized settings for specific games
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/xenia-manager/optimized-settings/main/settings/)
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/xenia-manager/optimized-settings/main/settings/)
    /// </summary>
    public static readonly string[] BaseOptimizedSettingsUrl =
    [
        $"{Base.GITHUB_PAGES}/optimized-settings/settings/",
        $"{Base.GITHUB_RAW}/xenia-manager/optimized-settings/main/settings/"
    ];

    /// <summary>
    /// Contains URLs to fetch the Patches database.
    /// This database contains patch files for Xenia emulator games and is used by the application
    /// to retrieve and apply game patches. Multiple URLs are provided to ensure availability,
    /// with fallback options in case the primary source is not reachable.
    /// Separate arrays are provided for Canary and Netplay patch versions.
    /// Sources include:
    /// 1. GitHub Pages - Primary source
    /// 2. Raw GitHub - Secondary source
    /// </summary>
    public static class PatchesDatabase
    {
        /// <summary>
        /// Array of URLs to fetch the Canary patches database.
        /// This database contains patch files for the Canary version of the emulator.
        /// Multiple URLs are provided to ensure availability, with fallback options in case
        /// the primary source is not reachable.
        /// Sources include:
        /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/database/data/patches/canary.json)
        /// 2. Raw GitHub - Secondary source (https://raw.githubusercontent.com/xenia-manager/database/main/data/patches/canary.json)
        /// </summary>
        public static readonly string[] CanaryPatches =
        [
            Base.GITHUB_PAGES + "/database/data/patches/canary.json",
            Base.GITHUB_RAW + "/xenia-manager/database/main/data/patches/canary.json"
        ];

        /// <summary>
        /// Array of URLs to fetch the Netplay patches database.
        /// This database contains patch files for the Netplay version of the emulator.
        /// Multiple URLs are provided to ensure availability, with fallback options in case
        /// the primary source is not reachable.
        /// Sources include:
        /// 1. GitHub Pages - Primary source (https://xenia-manager.github.io/database/data/patches/netplay.json)
        /// 2. Raw GitHub - Secondary source (https://raw.githubusercontent.com/xenia-manager/database/main/data/patches/netplay.json)
        /// </summary>
        public static readonly string[] NetplayPatches =
        [
            Base.GITHUB_PAGES + "/database/data/patches/netplay.json",
            Base.GITHUB_RAW + "/xenia-manager/database/main/data/patches/netplay.json"
        ];
    }
}
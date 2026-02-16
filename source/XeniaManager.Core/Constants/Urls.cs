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

        /// <summary>
        /// Base URL for Cloudflare Pages deployment
        /// Used as a backup/alternative source for resources for regions that GitHub blocked
        /// </summary>
        public const string CLOUDFLARE = "https://xeniamanagerdb.pages.dev";
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
    /// 3. Cloudflare Pages - Backup source (https://xeniamanagerdb.pages.dev/data/version.json)
    /// </summary>
    public static readonly string[] Manifest =
    [
        $"{Base.GITHUB_PAGES}/database/data/version.json",
        $"{Base.GITHUB_RAW}/xenia-manager/database/main/data/version.json",
        $"{Base.CLOUDFLARE}/data/version.json"
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
    /// 3. Cloudflare Pages - Backup source
    /// </summary>
    public static readonly string[] GameControllerDatabase =
    [
        $"{Base.GITHUB_PAGES}/database/data/gamecontrollerdb.txt",
        $"{Base.GITHUB_RAW}/xenia-manager/database/main/data/gamecontrollerdb.txt",
        $"{Base.CLOUDFLARE}/data/gamecontrollerdb.txt"
    ];

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
}
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
}
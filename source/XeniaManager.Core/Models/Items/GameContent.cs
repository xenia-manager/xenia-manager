using System.Globalization;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Stfs;

namespace XeniaManager.Core.Models.Items;

/// <summary>
/// Represents game-specific content with a universal XUID (0000000000000000).
/// Inherits from <see cref="AccountContent"/> and handles installer headers and other
/// game-related content not tied to a specific user account.
/// </summary>
public class GameContent : AccountContent
{
    /// <summary>
    /// Gets the list of installer header files associated with the game.
    /// These files contain metadata for installer packages.
    /// <para>
    /// Path: XeniaContentFolder/0000000000000000/TitleId/Headers/ContentType.Installer.ToHexString()/name.header
    /// </para>
    /// </summary>
    public List<HeaderFile> InstallerHeaderFiles { get; internal set; } = [];

    /// <summary>
    /// Gets the list of marketplace content header files associated with the game.
    /// These files contain metadata for marketplace downloads (DLC, themes, etc.).
    /// <para>
    /// Path: XeniaContentFolder/0000000000000000/TitleId/Headers/ContentType.MarketplaceContent.ToHexString()/name.header
    /// </para>
    /// </summary>
    public List<HeaderFile> MarketplaceContentHeaderFiles { get; internal set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="GameContent"/> class.
    /// </summary>
    /// <param name="xeniaVersion">Xenia Version of the content.</param>
    /// <param name="titleId">Game TitleId</param>
    public GameContent(XeniaVersion xeniaVersion, string titleId)
        : base(CreateTemporaryAccountInfo(), xeniaVersion, titleId)
    {
        LoadInstallerHeader(titleId);
        LoadMarketplaceContentHeader(titleId);
    }

    /// <summary>
    /// Creates a temporary AccountInfo with XUID 0 and Gamertag "GameContent" for universal content.
    /// </summary>
    /// <returns>A temporary AccountInfo instance.</returns>
    private static AccountInfo CreateTemporaryAccountInfo()
    {
        return new AccountInfo
        {
            Xuid = new AccountXuid(0),
            PathXuid = new AccountXuid(0),
            Gamertag = "GameContent"
        };
    }

    /// <summary>
    /// Loads the installer header files for the specified game title.
    /// In case that the header file cannot be loaded, a temporary HeaderFile is created.
    /// </summary>
    /// <param name="titleId">The title ID of the game for which to load the installer headers.</param>
    private void LoadInstallerHeader(string titleId)
    {
        Logger.Debug<GameContent>($"Loading installer headers for Title ID {titleId}");

        string installerFolder = Path.Combine(XeniaContentFolder, XuidHex,
            titleId.ToUpperInvariant(),
            ContentType.Installer.ToHexString());
        string installerHeaderFolder = Path.Combine(XeniaContentFolder, XuidHex,
            titleId.ToUpperInvariant(), "Headers",
            ContentType.Installer.ToHexString());

        Logger.Debug<GameContent>($"Installer folder: {installerFolder}");
        Logger.Debug<GameContent>($"Installer header folder: {installerHeaderFolder}");

        if (!Directory.Exists(installerFolder))
        {
            Logger.Warning<GameContent>($"Installer folder does not exist: {installerFolder}");
            return;
        }

        InstallerHeaderFiles = [];
        List<string> installerFolderContent =
        [
            ..Directory.GetDirectories(installerFolder),
            ..Directory.GetFiles(installerFolder)
        ];

        Logger.Debug<GameContent>($"Found {installerFolderContent.Count} installer items to process");

        foreach (string content in installerFolderContent)
        {
            string contentName = Path.GetFileName(content);
            string contentHeaderFile = Path.Combine(installerHeaderFolder, $"{contentName}.header");
            try
            {
                // Load the HeaderFile and add it to the InstallerHeaderFiles list
                Logger.Debug<GameContent>($"Loading header file for {contentName}");
                HeaderFile header = HeaderFile.Load(contentHeaderFile);
                InstallerHeaderFiles.Add(header);
                Logger.Debug<GameContent>($"Successfully loaded header for {contentName}");
            }
            catch (Exception ex)
            {
                // In case this fails, create a temporary HeaderFile
                Logger.Warning<GameContent>($"Failed to load header file for {contentName}");
                Logger.LogExceptionDetails<GameContent>(ex);
                Logger.Info<GameContent>($"Creating temporary header file for {contentName}");
                HeaderFile tempHeader = new HeaderFile
                {
                    FileName = contentName,
                    DisplayName = contentName,
                    ContentType = ContentType.Installer,
                    TitleId = uint.Parse(titleId, NumberStyles.HexNumber),
                    AccountXuid = new AccountXuid(0), // Universal XUID for installed content
                    HeaderSize = HeaderFile.FullHeaderSize
                };
                InstallerHeaderFiles.Add(tempHeader);
            }
        }

        Logger.Info<GameContent>($"Loaded {InstallerHeaderFiles.Count} installer headers for Title ID {titleId}");
    }

    /// <summary>
    /// Loads the marketplace content header files for the specified game title.
    /// In case that the header file cannot be loaded, a temporary HeaderFile is created.
    /// </summary>
    /// <param name="titleId">The title ID of the game for which to load the marketplace content headers.</param>
    private void LoadMarketplaceContentHeader(string titleId)
    {
        Logger.Debug<GameContent>($"Loading marketplace content headers for Title ID {titleId}");

        string marketplaceContentFolder = Path.Combine(XeniaContentFolder, XuidHex,
            titleId.ToUpperInvariant(),
            ContentType.MarketplaceContent.ToHexString());
        string marketplaceContentHeaderFolder = Path.Combine(XeniaContentFolder, XuidHex,
            titleId.ToUpperInvariant(), "Headers",
            ContentType.MarketplaceContent.ToHexString());

        Logger.Debug<GameContent>($"Marketplace content folder: {marketplaceContentFolder}");
        Logger.Debug<GameContent>($"Marketplace content header folder: {marketplaceContentHeaderFolder}");

        if (!Directory.Exists(marketplaceContentFolder))
        {
            Logger.Warning<GameContent>($"Marketplace content folder does not exist: {marketplaceContentFolder}");
            return;
        }

        MarketplaceContentHeaderFiles = [];
        List<string> marketplaceContentFolderContent =
        [
            ..Directory.GetDirectories(marketplaceContentFolder),
            ..Directory.GetFiles(marketplaceContentFolder)
        ];

        Logger.Debug<GameContent>($"Found {marketplaceContentFolderContent.Count} marketplace content items to process");

        foreach (string content in marketplaceContentFolderContent)
        {
            string contentName = Path.GetFileName(content);
            string contentHeaderFile = Path.Combine(marketplaceContentHeaderFolder, $"{contentName}.header");
            try
            {
                // Load the HeaderFile and add it to the MarketplaceContentHeaderFiles list
                Logger.Debug<GameContent>($"Loading header file for {contentName}");
                HeaderFile header = HeaderFile.Load(contentHeaderFile);
                MarketplaceContentHeaderFiles.Add(header);
                Logger.Debug<GameContent>($"Successfully loaded header for {contentName}");
            }
            catch (Exception ex)
            {
                // In case this fails, create a temporary HeaderFile
                Logger.Warning<GameContent>($"Failed to load header file for {contentName}");
                Logger.LogExceptionDetails<GameContent>(ex);
                Logger.Info<GameContent>($"Creating temporary header file for {contentName}");
                HeaderFile tempHeader = new HeaderFile
                {
                    FileName = contentName,
                    DisplayName = contentName,
                    ContentType = ContentType.MarketplaceContent,
                    TitleId = uint.Parse(titleId, NumberStyles.HexNumber),
                    AccountXuid = new AccountXuid(0), // Universal XUID for marketplace content
                    HeaderSize = HeaderFile.FullHeaderSize
                };
                MarketplaceContentHeaderFiles.Add(tempHeader);
            }
        }

        Logger.Info<GameContent>($"Loaded {MarketplaceContentHeaderFiles.Count} marketplace content headers for Title ID {titleId}");
    }
}
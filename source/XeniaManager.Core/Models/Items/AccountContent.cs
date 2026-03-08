using System.Globalization;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Models.Items;

/// <summary>
/// Represents all content associated with a specific Xbox 360 account profile.
/// Contains account information, saved games, and achievement data.
/// </summary>
public class AccountContent
{
    /// <summary>
    /// Gets the Xenia version associated with this profile. (If `UnifiedContentFolder` is selected, this doesn't matter)
    /// </summary>
    public XeniaVersion XeniaVersion { get; internal set; }

    /// <summary>
    /// Gets the path to the Xenia content folder.
    /// </summary>
    public string XeniaContentFolder => AppPathResolver.GetFullPath(XeniaVersionInfo.GetXeniaVersionInfo(XeniaVersion).ContentFolderLocation);

    /// <summary>
    /// Gets the account information for this profile.
    /// </summary>
    public AccountInfo AccountInfo { get; internal set; }

    /// <summary>
    /// Gets the Xbox 360 User ID (XUID) for this profile.
    /// </summary>
    public ulong Xuid => AccountInfo.PathXuid?.Value ?? AccountInfo.Xuid.Value;

    /// <summary>
    /// Gets the Xbox 360 User ID (XUID) as a hexadecimal string.
    /// </summary>
    public string XuidHex => Xuid.ToString("X16");

    /// <summary>
    /// Gets the Title ID of the game associated with this profile.
    /// </summary>
    public string TitleId { get; internal set; }

    /// <summary>
    /// Gets the path to the account file.
    /// <para>
    /// Path: XeniaContentFolder/XUID/FFFE07D1/ContentType.Profile.ToHexString()/XUID/Account
    /// </para>
    /// </summary>
    public string AccountFilePath { get; internal set; }

    /// <summary>
    /// Gets the profile GPD file containing account-wide settings and images.
    /// </summary>
    public GpdFile? ProfileGpd { get; internal set; }

    /// <summary>
    /// Gets the path to the profile GPD file.
    /// <para>
    /// Path: XeniaContentFolder/XUID/FFFE07D1/ContentType.Profile.ToHexString()/XUID/FFFE07D1.gpd
    /// </para>
    /// </summary>
    public string ProfileGpdPath { get; internal set; }

    /// <summary>
    /// Gets the GPD file that contains achievement data for a specific game linked to the Xbox 360 account profile.
    /// Used for managing and retrieving game-specific achievement information.
    /// </summary>
    public GpdFile? GameAchievementGpdFile { get; internal set; }

    /// <summary>
    /// Gets the file path for the game achievement GPD (Game Progress Data) file associated with this account.
    /// <para>
    /// Path: XeniaContentFolder/XUID/FFFE07D1/ContentType.Profile.ToHexString()/XUID/titleid.gpd
    /// </para>
    /// </summary>
    public string GameAchievementGpdPath { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the list of saved game header files associated with the Xbox 360 account profile.
    /// These files contain metadata for saved games, such as titles, file paths, and related account information.
    /// <para>
    /// Path: XeniaContentFolder/XUID/TitleId/Headers/ContentType.SavedGame.ToHexString()/name.header
    /// </para>
    /// </summary>
    public List<HeaderFile> SavedGameHeaderFiles { get; internal set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountContent"/> class.
    /// </summary>
    /// <param name="accountInfo">The account information.</param>
    /// <param name="xeniaVersion">Xenia Version of the account.</param>
    /// <param name="titleId">Game TitleId</param>
    public AccountContent(AccountInfo accountInfo, XeniaVersion xeniaVersion, string titleId)
    {
        AccountInfo = accountInfo;
        XeniaVersion = xeniaVersion;
        AccountFilePath = Path.Combine(XeniaContentFolder,
            XuidHex,
            "FFFE07D1",
            ContentType.Profile.ToHexString(),
            XuidHex,
            "Account");
        ProfileGpdPath = Path.Combine(XeniaContentFolder,
            XuidHex,
            "FFFE07D1",
            ContentType.Profile.ToHexString(),
            XuidHex,
            "FFFE07D1.gpd");
        TitleId = titleId;

        LoadProfileGpd();
        LoadGameAchievementGpd(TitleId);
        LoadSavedGamesHeader(TitleId);
    }

    /// <summary>
    /// Loads the profile GPD file if it exists.
    /// </summary>
    private void LoadProfileGpd()
    {
        try
        {
            if (File.Exists(ProfileGpdPath))
            {
                ProfileGpd = GpdFile.Load(ProfileGpdPath);
                Logger.Debug<AccountContent>($"Loaded profile GPD for XUID {XuidHex} from {ProfileGpdPath}");
            }
            else
            {
                Logger.Debug<AccountContent>($"Profile GPD not found at {ProfileGpdPath} for XUID {XuidHex}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<AccountContent>($"Failed to load profile GPD from {ProfileGpdPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a game-specific achievement GPD file if it exists.
    /// </summary>
    /// <param name="titleId">The Title ID of the game.</param>
    private void LoadGameAchievementGpd(string titleId)
    {
        try
        {
            string gpdPath = Path.Combine(XeniaContentFolder,
                XuidHex,
                "FFFE07D1",
                ContentType.Profile.ToHexString(),
                XuidHex,
                $"{titleId.ToUpperInvariant()}.gpd");

            if (File.Exists(gpdPath))
            {
                GameAchievementGpdFile = GpdFile.Load(gpdPath);
                GameAchievementGpdPath = gpdPath;
                Logger.Debug<AccountContent>($"Loaded game achievement GPD for Title ID {titleId} from {gpdPath}");
            }
            else
            {
                Logger.Debug<AccountContent>($"Game achievement GPD not found at {gpdPath} for Title ID {titleId}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<AccountContent>($"Failed to load game achievement GPD for Title ID {titleId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the saved games header files for the specified game title.
    /// In case that the header file cannot be loaded, a temporary HeaderFile is created.
    /// </summary>
    /// <param name="titleId">The title ID of the game for which to load the saved games headers.</param>
    private void LoadSavedGamesHeader(string titleId)
    {
        Logger.Debug<AccountContent>($"Loading saved games headers for Title ID {titleId}, XUID {XuidHex}");

        string savedGamesFolder = Path.Combine(XeniaContentFolder, XuidHex,
            titleId.ToUpperInvariant(),
            ContentType.SavedGame.ToHexString());
        string savedGamesHeaderFolder = Path.Combine(XeniaContentFolder, XuidHex,
            titleId.ToUpperInvariant(), "Headers",
            ContentType.SavedGame.ToHexString());

        Logger.Debug<AccountContent>($"Saved games folder: {savedGamesFolder}");
        Logger.Debug<AccountContent>($"Saved games header folder: {savedGamesHeaderFolder}");

        if (!Directory.Exists(savedGamesFolder))
        {
            Logger.Warning<AccountContent>($"Saved games folder does not exist: {savedGamesFolder}");
            return;
        }

        SavedGameHeaderFiles = [];
        List<string> savedGamesFolderContent =
        [
            ..Directory.GetDirectories(savedGamesFolder),
            ..Directory.GetFiles(savedGamesFolder)
        ];

        Logger.Debug<AccountContent>($"Found {savedGamesFolderContent.Count} saved game items to process");

        foreach (string content in savedGamesFolderContent)
        {
            string contentName = Path.GetFileName(content);
            string contentHeaderFile = Path.Combine(savedGamesHeaderFolder, $"{contentName}.header");
            try
            {
                // Load the HeaderFile and add it to the SavedGameHeaderFiles list
                Logger.Debug<AccountContent>($"Loading header file for {contentName}");
                HeaderFile header = HeaderFile.Load(contentHeaderFile);
                SavedGameHeaderFiles.Add(header);
                Logger.Debug<AccountContent>($"Successfully loaded header for {contentName}");
            }
            catch (Exception ex)
            {
                // In case this fails, create a temporary HeaderFile
                Logger.Warning<AccountContent>($"Failed to load header file for {contentName}");
                Logger.LogExceptionDetails<AccountContent>(ex);
                Logger.Info<AccountContent>($"Creating temporary header file for {contentName}");
                HeaderFile tempHeader = new HeaderFile
                {
                    FileName = contentName,
                    DisplayName = contentName,
                    ContentType = ContentType.SavedGame,
                    TitleId = uint.Parse(titleId, NumberStyles.HexNumber),
                    AccountXuid = AccountInfo.PathXuid ?? AccountInfo.Xuid,
                    HeaderSize = HeaderFile.FullHeaderSize
                };
                SavedGameHeaderFiles.Add(tempHeader);
            }
        }

        Logger.Info<AccountContent>($"Loaded {SavedGameHeaderFiles.Count} saved game headers for Title ID {titleId}");
    }
}
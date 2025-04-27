using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

// Imported
using ImageMagick;
using XeniaManager.Core.Database;
using XeniaManager.Core.Downloader;
using XeniaManager.Core.VirtualFileSystem;

namespace XeniaManager.Core.Game;

/// <summary>
/// Current compatibility of the emulator with the game
/// </summary>
public class Compatibility
{
    /// <summary>
    /// URL to the compatibility page
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Compatibility rating
    /// </summary>
    [JsonPropertyName("rating")]
    public CompatibilityRating Rating { get; set; }
}

/// <summary>
/// All the game artwork used by Xenia Manager
/// </summary>
public class GameArtwork
{
    /// <summary>
    /// Path to the game's background
    /// </summary>
    [JsonPropertyName("background")]
    public string Background { get; set; }

    /// <summary>
    /// Path to the game's boxart
    /// </summary>
    [JsonPropertyName("boxart")]
    public string Boxart { get; set; }

    /// <summary>
    /// Path to the game's shortcut icon
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; }
}

/// <summary>
/// Grouping of all file locations related to the game (ISO, patch, configuration, and emulator)
/// </summary>
public class GameFiles
{
    /// <summary>
    /// Path to the game's ISO file
    /// </summary>
    [JsonPropertyName("game")]
    public string Game { get; set; }

    /// <summary>
    /// Path to the game's patch file
    /// </summary>
    [JsonPropertyName("patch")]
    public string? Patch { get; set; }

    /// <summary>
    /// Path to the game's configuration file (null if it doesn't exist)
    /// </summary>
    [JsonPropertyName("config")]
    public string Config { get; set; }

    /// <summary>
    /// The location of the custom Xenia executable (null if not applicable)
    /// </summary>
    [JsonPropertyName("custom_emulator_executable")]
    public string? CustomEmulatorExecutable { get; set; }
}

/// <summary>
/// Game class containing all the information about the game
/// </summary>
public class Game
{
    /// <summary>
    /// The unique identifier for the game
    /// </summary>
    [JsonPropertyName("game_id")]
    public string? GameId { get; set; }

    /// <summary>
    /// Alternative game_id's that the game can use (Useful for searching for game compatibility)
    /// </summary>
    [JsonPropertyName("alternative_id")]
    public List<string> AlternativeIDs { get; set; } = new List<string>();

    /// <summary>
    /// The unique identifier for the game
    /// </summary>
    [JsonPropertyName("media_id")]
    public string? MediaId { get; set; }

    /// <summary>
    /// Game name
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// Which Xenia version (Custom/Canary/Mousehook/Netplay) the game uses
    /// </summary>
    [JsonPropertyName("xenia_version")]
    public XeniaVersion XeniaVersion { get; set; }

    /// <summary>
    /// Current compatibility of the emulator with the game
    /// </summary>
    [JsonPropertyName("compatibility")]
    public Compatibility Compatibility { get; set; }

    /// <summary>
    /// All paths towards different artworks for the game
    /// </summary>
    [JsonPropertyName("artwork")]
    public GameArtwork Artwork { get; set; } = new GameArtwork();

    /// <summary>
    /// All paths related to the game (Game, Config, Patch...)
    /// </summary>
    [JsonPropertyName("file_locations")]
    public GameFiles FileLocations { get; set; } = new GameFiles();
}

/// <summary>
/// Manages loading, saving, grabbing details about games, adding, removing games
/// </summary>
public static class GameManager
{
    // Variables
    /// <summary>
    /// All the currently installed games
    /// </summary>
    public static List<Game> Games { get; set; } = new List<Game>();

    // Functions
    /// <summary>
    /// Initializes new game library
    /// </summary>
    private static void InitializeGameLibrary()
    {
        Logger.Info("Creating new game library");
        Games = new List<Game>();
    }

    /// <summary>
    /// Loads all the games from a .JSON file
    /// </summary>
    public static void LoadLibrary()
    {
        if (!File.Exists(Constants.FilePaths.GameLibrary))
        {
            Logger.Warning("Couldn't find file that stores all of the installed games");
            InitializeGameLibrary();
            SaveLibrary();
            return;
        }

        Logger.Info("Loading game library");
        Games = JsonSerializer.Deserialize<List<Game>>(File.ReadAllText(Constants.FilePaths.GameLibrary));
    }

    /// <summary>
    /// Saves all the games into a .JSON file
    /// </summary>
    public static void SaveLibrary()
    {
        try
        {
            string gameLibrarySerialized = JsonSerializer.Serialize(Games, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Constants.FilePaths.GameLibrary, gameLibrarySerialized);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, ex.Message);
        }
    }

    /// <summary>
    /// Retrieves game details such as title, game ID, and media ID for a game located
    /// at the specified path without using Xenia emulator.
    /// </summary>
    /// <param name="gamePath">The file path to the game from which details are to be retrieved.</param>
    /// <returns>
    /// A tuple containing the game title, game ID, and media ID. If details cannot be retrieved,
    /// default values of "Not found" or an empty string are provided.
    /// </returns>
    public static (string, string, string) GetGameDetailsWithoutXenia(string gamePath)
    {
        string gameTitle = "Not found";
        string gameId = "Not found";
        string mediaId = "";
        string header = Helpers.GetHeader(gamePath);
        Logger.Debug($"Header: {header}");
        if (header is "CON" or "PIRS" or "LIVE")
        {
            // STFS
            Logger.Info("Game is in STFS format");
            Stfs game = new Stfs(gamePath);
            gameTitle = game.GetTitle();
            if (gameTitle == "Not found")
            {
                gameTitle = game.GetDisplayName();
            }
            gameId = game.GetTitleId();
            mediaId = game.GetMediaId();
        }
        else if (header is "XEX2")
        {
            // XEX
            Logger.Info("File is in .XEX format");
            if (XexUtility.ExtractData(File.ReadAllBytes(gamePath), out string parsedTitleId, out string parsedMediaId))
            {
                gameId = parsedTitleId;
                mediaId = parsedMediaId;
            }
        }
        else
        {
            // Unpack .xex file (.iso??)
            throw new NotImplementedException("Currently not supported.");
        }

        return (gameTitle, gameId, mediaId);
    }

    /// <summary>
    /// Launches a specified version of Xenia emulator to retrieve game details, including the title, title ID, and media ID.
    /// </summary>
    /// <param name="gamePath">The file path to the game to be analyzed.</param>
    /// <param name="version">The version of the Xenia emulator to use.</param>
    /// <returns>A tuple containing the game title, title ID, and media ID.</returns>
    /// <exception cref="NotImplementedException">Thrown if the specified Xenia version (e.g., Mousehook/Netplay) is not supported.</exception>
    /// <exception cref="InvalidEnumArgumentException">Thrown if an invalid Xenia version is provided.</exception>
    public static async Task<(string, string, string)> GetGameDetailsWithXenia(string gamePath, XeniaVersion version)
    {
        Logger.Info($"Launching the game with Xenia {version} to find game title, game_id and media_id");
        Process xenia = new Process();
        switch (version)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir);
                break;
            // TODO: Mousehook/Netplay grabbing details with Xenia (Executable/Emulator location needed)
            case XeniaVersion.Mousehook:
                throw new NotImplementedException();
            case XeniaVersion.Netplay:
                throw new NotImplementedException();
            default:
                throw new InvalidEnumArgumentException("Invalid Xenia version.");
        }

        xenia.StartInfo.Arguments = $@"""{gamePath}"""; // Add game path to the arguments so it's launched through Xenia
        // Start Xenia and wait for input
        xenia.Start();
        xenia.WaitForInputIdle();

        // Things we're looking for
        string gameTitle = "Not found";
        string titleId = "Not found";
        string mediaId = string.Empty;

        Process process = Process.GetProcessById(xenia.Id);
        Logger.Info("Trying to find the game title from Xenia Window Title");
        int NumberOfTries = 0;

        // Method 1 - Using Xenia Window Title
        // Repeats for certain time before moving on
        while (gameTitle == "Not found")
        {
            // Regex used to scrape Title and TitleID from Xenia Window Title
            Regex titleRegex = new Regex(@"\]\s+([^<]+)\s+<");
            Regex idRegex = new Regex(@"\[(\w{8}) v[\d\.]+\]");

            // Grabbing Title from Xenia Window Title
            Match gameNameMatch = titleRegex.Match(process.MainWindowTitle);
            gameTitle = gameNameMatch.Success ? gameNameMatch.Groups[1].Value : "Not found";

            // Grabbing TitleID from Xenia Window Title
            Match versionMatch = idRegex.Match(process.MainWindowTitle);
            titleId = versionMatch.Success ? versionMatch.Groups[1].Value : "Not found";

            process = Process.GetProcessById(xenia.Id);

            NumberOfTries++;

            // Check if this reached the maximum number of attempts
            // If it did, break the while loop
            if (NumberOfTries > 1000)
            {
                gameTitle = "Not found";
                titleId = "Not found";
                break;
            }

            await Task.Delay(100); // Delay between repeating to ensure everything loads
        }

        xenia.Kill(); // Force close Xenia

        // Method 2 - Using Xenia.log (In case method 1 fails)
        // Checks if xenia.log exists and if it does, goes through it, trying to find Title, TitleID and MediaID
        if (File.Exists(Path.Combine(xenia.StartInfo.WorkingDirectory, "xenia.log")))
        {
            using (FileStream fs = new FileStream(Path.Combine(xenia.StartInfo.WorkingDirectory, "xenia.log"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs))
            {
                // Goes through every line in xenia.log, trying to find lines that contain Title, TitleID and MediaID
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    switch (true)
                    {
                        case var _ when line.ToLower().Contains("title name"):
                        {
                            string[] split = line.Split(':');
                            if (gameTitle == "Not found")
                            {
                                gameTitle = split[1].TrimStart();
                            }

                            break;
                        }
                        case var _ when line.ToLower().Contains("title id"):
                        {
                            string[] split = line.Split(':');
                            titleId = split[1].TrimStart();
                            if (titleId == "Not found")
                            {
                                titleId = split[1].TrimStart();
                            }

                            break;
                        }
                        case var _ when line.ToLower().Contains("media id"):
                        {
                            string[] split = line.Split(':');
                            mediaId = split[1].TrimStart();
                            break;
                        }
                    }
                }
            }
        }

        // Return what has been found
        return (gameTitle, titleId, mediaId);
    }

    /// <summary>
    /// Adds the "unknown" game by assigning it default artwork
    /// </summary>
    /// <param name="gameTitle">Game Title</param>
    /// <param name="titleId">Game's Title ID</param>
    /// <param name="mediaId">Game's Media ID</param>
    /// <param name="gamePath">Path to the game</param>
    /// <param name="version">Game's selected Xenia Version</param>
    /// <exception cref="NotImplementedException">Missing Mousehook/Netplay support</exception>
    public static async Task AddUnknownGame(string gameTitle, string titleId, string? mediaId, string gamePath, XeniaVersion version)
    {
        Logger.Info($"Selected game: {gameTitle} ({titleId})");
        Game newGame = new Game();
        newGame.Title = gameTitle.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
        newGame.GameId = titleId;
        newGame.MediaId = mediaId;
        newGame.FileLocations.Game = gamePath;

        // Compatibility rating with titleid
        await CompatibilityManager.GetCompatibility(newGame, titleId);

        // Checking for duplicates
        if (Games.Any(game => game.Title == newGame.Title))
        {
            Logger.Info("This game title already exists");
            Logger.Info($"Adding this game as a duplicate");
            int counter = 1;
            string OriginalGameTitle = newGame.Title;
            while (Games.Any(game => game.Title == newGame.Title))
            {
                newGame.Title = $"{OriginalGameTitle} ({counter})";
                counter++;
            }
        }

        Logger.Info($"Creating a new configuration file for {newGame.Title}");
        switch (version)
        {
            case XeniaVersion.Canary:
                File.Copy(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation),
                    Path.Combine(Constants.DirectoryPaths.Base, Path.GetDirectoryName(Constants.Xenia.Canary.ConfigLocation), $"{newGame.Title}.config.toml"), true);
                newGame.FileLocations.Config = Path.Combine(Path.GetDirectoryName(Constants.Xenia.Canary.ConfigLocation), $"{newGame.Title}.config.toml");
                break;
            case XeniaVersion.Mousehook:
                throw new NotImplementedException();
                break;
            case XeniaVersion.Netplay:
                throw new NotImplementedException();
                break;
        }

        newGame.XeniaVersion = version;

        // Download Artwork
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork"));

        // Default Boxart
        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg", Path.Combine(Constants.DirectoryPaths.Base, "GameData", newGame.Title, "Artwork", "Boxart.png"), MagickFormat.Png);
        newGame.Artwork.Boxart = Path.Combine("GameData", newGame.Title, "Artwork", "Boxart.png");

        // Default Icon
        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Icon.png", Path.Combine(Constants.DirectoryPaths.Base, "GameData", newGame.Title, "Artwork", "Icon.ico"), MagickFormat.Ico, 64, 64);
        newGame.Artwork.Icon = Path.Combine("GameData", newGame.Title, "Artwork", "Icon.ico");

        // Default Background
        // TODO: Implement Background downloading when adding possible loading screen
        //ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg", Path.Combine(Constants.BaseDir, "GameData", newGame.Title, "Artwork", "Background.jpg"), MagickFormat.Jpeg, 1280, 720);
        //newGame.Artwork.Background = Path.Combine("GameData", newGame.Title, "Artwork", "Background.jpg");

        Logger.Info("Adding the game to game library");
        Games.Add(newGame);
        SaveLibrary();
        Logger.Info("Finished adding the game");
    }

    /// <summary>
    /// Adds the game by downloading the information from the database
    /// </summary>
    /// <param name="selectedGame">Selected game from the UI</param>
    /// <param name="titleId">Title ID of the game being added</param>
    /// <param name="mediaId">Media ID of the game being added</param>
    /// <param name="gamePath">Path to the game</param>
    /// <param name="xeniaVersion">Xenia version the game will use</param>
    /// <exception cref="Exception"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static async Task AddGame(GameInfo selectedGame, string titleId, string? mediaId, string gamePath, XeniaVersion xeniaVersion)
    {
        // Grab full game information
        XboxDatabaseGameInfo fullGameInfo = await XboxDatabase.GetFullGameInfo(titleId);
        if (fullGameInfo == null)
        {
            Logger.Error("Couldn't fetch game information");
            throw new Exception("Couldn't fetch game information");
        }

        // Add new Game entry
        Logger.Info($"Selected game: {fullGameInfo.Title.Full} ({titleId})");
        Game newGame = new Game
        {
            Title = fullGameInfo.Title.Full.Replace(":", " -").Replace('\\', ' ').Replace('/', ' '),
            GameId = titleId,
            AlternativeIDs = selectedGame.AlternativeId,
            MediaId = mediaId,
            XeniaVersion = xeniaVersion
        };
        newGame.FileLocations.Game = gamePath;

        // Compatibility rating with titleid
        await CompatibilityManager.GetCompatibility(newGame, titleId);

        // Compatibility rating with alternative titleid's
        if (newGame.Compatibility == null)
        {
            foreach (string alternativeid in newGame.AlternativeIDs)
            {
                await CompatibilityManager.GetCompatibility(newGame, alternativeid);
                if (newGame.Compatibility != null)
                {
                    break;
                }
            }
        }

        // Checking for duplicates
        if (Games.Any(game => game.Title == newGame.Title))
        {
            Logger.Info("This game title already exists");
            Logger.Info($"Adding this game as a duplicate");
            int counter = 1;
            string OriginalGameTitle = newGame.Title;
            while (Games.Any(game => game.Title == newGame.Title))
            {
                newGame.Title = $"{OriginalGameTitle} ({counter})";
                counter++;
            }
        }

        // Create new configuration file for the game
        Logger.Info($"Creating a new configuration file for {newGame.Title}");
        switch (xeniaVersion)
        {
            case XeniaVersion.Canary:
                File.Copy(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation),
                    Path.Combine(Constants.DirectoryPaths.Base, Path.GetDirectoryName(Constants.Xenia.Canary.ConfigLocation), $"{newGame.Title}.config.toml"), true);
                newGame.FileLocations.Config = Path.Combine(Path.GetDirectoryName(Constants.Xenia.Canary.ConfigLocation), $"{newGame.Title}.config.toml");
                break;
            case XeniaVersion.Mousehook:
                throw new NotImplementedException();
                break;
            case XeniaVersion.Netplay:
                throw new NotImplementedException();
                break;
        }

        // Create Artwork directory
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork"));
        DownloadManager downloadManager = new DownloadManager();

        // Download Boxart
        if (fullGameInfo.Artwork.Boxart == null)
        {
            // Use default boxart since the game doesn't have any boxart
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg", Path.Combine(Constants.DirectoryPaths.Base, "GameData", newGame.Title, "Artwork", "boxart.png"), MagickFormat.Png);
        }
        else
        {
            // Check if the Xbox Marketplace url works before downloading it
            if (await downloadManager.CheckIfUrlWorksAsync(fullGameInfo.Artwork.Boxart, "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} boxart from Xbox Marketplace.");
                await downloadManager.DownloadArtwork(fullGameInfo.Artwork.Boxart, Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "boxart.png"), MagickFormat.Png);
            }
            // Check if the GitHub repo url works before downloading it
            else if (await downloadManager.CheckIfUrlWorksAsync($"{Constants.Urls.XboxDatabaseArtworkBase}/{titleId}/boxart.png", "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} boxart from GitHub repository.");
                await downloadManager.DownloadArtwork($"{Constants.Urls.XboxDatabaseArtworkBase}/{titleId}/boxart.png", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "boxart.png"), MagickFormat.Png);
            }
            else
            {
                // Use default boxart since Xenia Manager can't fetch the boxart from the internet
                Logger.Info($"Using default artwork since we can't fetch the boxart from the internet");
                ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg", Path.Combine(Constants.DirectoryPaths.Base, "GameData", newGame.Title, "Artwork", "boxart.png"), MagickFormat.Png);
            }
        }
        newGame.Artwork.Boxart = Path.Combine("GameData", newGame.Title, "Artwork", "boxart.png");

        // Download Icon
        if (fullGameInfo.Artwork.Icon == null)
        {
            // Use default icon since the game doesn't have any icon
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Icon.png", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "icon.ico"), MagickFormat.Ico, 64, 64);
        }
        else
        {
            // Check if the Xbox Marketplace url works before downloading it
            if (await downloadManager.CheckIfUrlWorksAsync(fullGameInfo.Artwork.Icon, "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} icon from Xbox Marketplace.");
                await downloadManager.DownloadArtwork(fullGameInfo.Artwork.Icon, Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "icon.ico"), MagickFormat.Ico, 64, 64);
            }
            // Check if the GitHub repo url works before downloading it
            else if (await downloadManager.CheckIfUrlWorksAsync($"{Constants.Urls.XboxDatabaseArtworkBase}/{titleId}/icon.png", "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} icon from GitHub repository.");
                await downloadManager.DownloadArtwork($"{Constants.Urls.XboxDatabaseArtworkBase}/{titleId}/icon.png", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "icon.ico"), MagickFormat.Ico, 64, 64);
            }
            else
            {
                // Use default icon since Xenia Manager can't fetch the icon from the internet
                Logger.Info($"Using default artwork since we can't fetch the icon from the internet");
                ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Icon.png", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "icon.ico"), MagickFormat.Ico, 64, 64);
            }
        }
        newGame.Artwork.Icon = Path.Combine("GameData", newGame.Title, "Artwork", "icon.ico");

        // Download Background
        // TODO: Implement Background downloading when adding possible loading screen
        //ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg", Path.Combine(Constants.GamedataDir, newGame.Title, "Artwork", "Background.jpg"), MagickFormat.Jpeg, 1280, 720);
        //newGame.Artwork.Background = Path.Combine("GameData", newGame.Title, "Artwork", "Background.jpg");

        Logger.Info("Adding the game to game library");
        Games.Add(newGame);
        SaveLibrary();
        Logger.Info("Finished adding the game");
    }

    /// <summary>
    /// Removes the specified game from Xenia Manager.
    /// </summary>
    /// <param name="game">The game to be removed.</param>
    /// <param name="deleteGameContent">Indicates whether the game's content should also be removed.</param>
    /// <exception cref="NotImplementedException">Thrown for unimplemented methods on other versions of Xenia.</exception>
    public static void RemoveGame(Game game, bool deleteGameContent = false)
    {
        // Remove game patch
        if (game.FileLocations.Patch != null && File.Exists(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Patch)))
        {
            Logger.Debug($"Deleting patch file: {Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Patch)}");
            File.Delete(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Patch));
        }

        // Remove game configuration file
        if (game.FileLocations.Config != null && File.Exists(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config)))
        {
            Logger.Debug($"Deleting configuration file: {Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config)}");
            File.Delete(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config));
        }

        // Remove GameData
        if (game.Artwork != null && Directory.Exists(Path.Combine(Constants.DirectoryPaths.Base, "GameData", game.Title)))
        {
            Logger.Debug($"Deleting configuration file: {Path.Combine(Constants.DirectoryPaths.Base, "GameData", game.Title)}");
            Directory.Delete(Path.Combine(Constants.DirectoryPaths.Base, "GameData", game.Title), true);
        }

        // Removing game content
        if (deleteGameContent)
        {
            string gameContentFolder = game.XeniaVersion switch
            {
                XeniaVersion.Canary => Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir, "content", game.GameId),
                _ => throw new NotImplementedException($"Xenia {game.XeniaVersion} is not implemented")
            };

            if (Directory.Exists(gameContentFolder))
            {
                Logger.Info($"Deleting content folder of {game.Title}");
                Directory.Delete(gameContentFolder, true);
            }
        }

        // Removing the game
        Games.Remove(game);
        SaveLibrary(); // Saving changes
    }

    /// <summary>
    /// Cleans up the given game title by removing invalid file name characters, reducing multiple spaces to a single space, and trimming excess whitespace.
    /// </summary>
    /// <param name="gameTitle">The original game title to clean up.</param>
    /// <returns>A cleaned version of the game title, suitable for file naming and other purposes.</returns>
    public static string TitleCleanup(string gameTitle)
    {
        return Regex.Replace(string.Concat(gameTitle.Where(c => !Path.GetInvalidFileNameChars().Contains(c))), @"\s+", " ").Trim();
    }

    /// <summary>
    /// Checks if a game title is a duplicate within the game library.
    /// </summary>
    /// <param name="gameTitle">The title of the game to check for duplicates.</param>
    /// <returns>
    /// True if the game title already exists in the library; otherwise, false.
    /// </returns>
    public static bool CheckForDuplicateTitle(string gameTitle)
    {
        foreach (Game game in Games)
        {
            if (game.Title == gameTitle)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Adjusts the game information, including renaming associated configuration files,
    /// relocating game data folders, and updating artwork paths to reflect the new title.
    /// </summary>
    /// <param name="game">The game object to be updated.</param>
    /// <param name="newGameTitle">The new title for the game.</param>
    public static void AdjustGameInfo(Game game, string newGameTitle)
    {
        if (game.FileLocations.Config.Contains(game.Title))
        {
            Logger.Info("Renaming the configuration file to fit the new title");
            File.Move(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config),
                Path.Combine(Constants.DirectoryPaths.Base, Path.GetDirectoryName(game.FileLocations.Config), $"{newGameTitle}.config.toml"), true);
            game.FileLocations.Config = Path.Combine(Path.GetDirectoryName(game.FileLocations.Config), $"{newGameTitle}.config.toml");
        }

        Logger.Info("Moving the game related data to a new folder");
        if (Path.Combine(Constants.DirectoryPaths.GameData, game.Title) != Path.Combine(Constants.DirectoryPaths.GameData, newGameTitle))
        {
            Directory.Move(Path.Combine(Constants.DirectoryPaths.GameData, game.Title), Path.Combine(Constants.DirectoryPaths.GameData, newGameTitle));
        }

        Logger.Info("Update game info in the library");
        game.Title = newGameTitle;
        game.Artwork.Boxart = Path.Combine("GameData", game.Title, "Artwork", "boxart.png");
        game.Artwork.Icon = Path.Combine("GameData", game.Title, "Artwork", "icon.ico");
    }
}
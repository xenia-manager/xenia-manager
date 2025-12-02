using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

// Imported Libraries
using ImageMagick;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Database;
using XeniaManager.Core.Downloader;
using XeniaManager.Core.Enum;
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
    /// Holds how much time the user spent on playing this game
    /// </summary>
    [JsonPropertyName("playtime")]
    public double? Playtime { get; set; } = 0;

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

    [JsonIgnore]
    public BitmapImage IconImage
    {
        get
        {
            try
            {
                return ArtworkManager.CacheLoadArtwork(Path.Combine(Constants.DirectoryPaths.Base, Artwork.Icon));
            }
            catch
            {
                // Return a fallback image if needed
                return new BitmapImage(); // or a default resource URI
            }
        }
    }
}

public enum GameSortField
{
    Title,
    Playtime,
    Compatibility,
    GameId,
    MediaId,
    XeniaVersion
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
    /// Attempts to recover the game library from a backup file
    /// </summary>
    private static void AttemptBackupRecovery(string backupPath)
    {
        if (!File.Exists(backupPath))
        {
            Logger.Info("No backup found. Initializing new game library.");
            InitializeGameLibrary();
            SaveLibrary();
            return;
        }

        try
        {
            Logger.Warning($"Attempting to recover from backup: {backupPath}");
            string backupContent = File.ReadAllText(backupPath);

            if (string.IsNullOrWhiteSpace(backupContent))
            {
                throw new JsonException("Backup file is empty");
            }

            List<Game> recoveredGames = JsonSerializer.Deserialize<List<Game>>(backupContent);

            if (recoveredGames == null)
            {
                throw new JsonException("Backup deserialization resulted in null");
            }

            Games = recoveredGames;
            Logger.Warning($"Successfully recovered {Games.Count} games from backup");

            // Immediately re-save to clean state
            SaveLibrary();
        }
        catch (Exception backupEx)
        {
            Logger.Error($"Backup recovery failed: {backupEx.Message}");
            Logger.Info("Initializing new game library");
            InitializeGameLibrary();
            SaveLibrary();
        }
    }

    /// <summary>
    /// Loads all the games from a .JSON file with robust error handling and recovery
    /// </summary>
    public static void LoadLibrary()
    {
        string path = FilePaths.GameLibrary;
        string backupPath = path + ".backup";

        try
        {
            if (!File.Exists(path))
            {
                Logger.Warning("Couldn't find file that stores all of the installed games");
                InitializeGameLibrary();
                SaveLibrary();
                return;
            }

            Logger.Info("Loading game library");
            string content = File.ReadAllText(path);

            // Validate content is not empty
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new JsonException("Game library file is empty");
            }

            // Attempt deserialization
            List<Game> deserializedGames = JsonSerializer.Deserialize<List<Game>>(content);

            // Validate deserialized data
            if (deserializedGames == null)
            {
                throw new JsonException("Deserialization resulted in null");
            }

            // Validate all games have required fields
            foreach (Game game in deserializedGames)
            {
                if (string.IsNullOrWhiteSpace(game.Title) || string.IsNullOrWhiteSpace(game.GameId))
                {
                    Logger.Warning($"Game entry with missing required fields detected. Skipping malformed entry.");
                    continue;
                }
            }

            Games = deserializedGames;
            Logger.Info($"Successfully loaded {Games.Count} games from the library");
        }
        catch (JsonException jsonEx)
        {
            Logger.Error($"Failed to parse game library JSON: {jsonEx.Message}");
            AttemptBackupRecovery(backupPath);
        }
        catch (IOException ioEx)
        {
            Logger.Error($"File I/O error while loading game library: {ioEx.Message}");
            AttemptBackupRecovery(backupPath);
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error while loading game library: {ex.Message}\nFull Error:\n{ex}");
            AttemptBackupRecovery(backupPath);
        }
    }

    /// <summary>
    /// Sorts the library based on a field
    /// </summary>
    /// <param name="sortField"></param>
    /// <param name="descending"></param>
    public static void SortLibrary(GameSortField sortField, bool descending = false)
    {
        Comparison<Game> comparison = sortField switch
        {
            GameSortField.Title => (x, y) => string.Compare(x.Title, y.Title, StringComparison.OrdinalIgnoreCase),
            GameSortField.Playtime => (x, y) => Nullable.Compare(x.Playtime, y.Playtime),
            GameSortField.Compatibility => (x, y) => x.Compatibility.Rating.CompareTo(y.Compatibility.Rating),
            GameSortField.GameId => (x, y) => string.Compare(x.GameId, y.GameId, StringComparison.OrdinalIgnoreCase),
            GameSortField.MediaId => (x, y) => string.Compare(x.MediaId, y.MediaId, StringComparison.OrdinalIgnoreCase),
            GameSortField.XeniaVersion => (x, y) => x.XeniaVersion.CompareTo(y.XeniaVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(sortField), $"Unsupported sort field: {sortField}")
        };

        if (descending)
        {
            Games.Sort((x, y) => comparison(y, x));
        }
        else
        {
            Games.Sort(comparison);
        }
    }

    /// <summary>
    /// Cleans up temporary file if it still exists
    /// </summary>
    private static void CleanupTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
                Logger.Debug("Temporary file cleaned up");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to clean up temporary file: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves all the games into a .JSON file with atomic writes and backup
    /// </summary>
    public static void SaveLibrary()
    {
        string path = FilePaths.GameLibrary;
        string tempPath = path + ".tmp";
        string backupPath = path + ".backup";

        try
        {
            // Sort before serialization to avoid side effects during save
            SortLibrary(GameSortField.Title);

            // Serialize to string first to catch errors before writing
            string json = JsonSerializer.Serialize(Games, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("Serialization produced empty or null JSON");
            }

            // Write to temporary file (atomic operation)
            File.WriteAllText(tempPath, json);

            // Verify temp file was written and is readable
            if (!File.Exists(tempPath))
            {
                throw new IOException("Temporary file was not created");
            }

            string tempContent = File.ReadAllText(tempPath);
            if (string.IsNullOrWhiteSpace(tempContent))
            {
                throw new IOException("Temporary file is empty after write");
            }

            // Create backup from current file before replacing it
            if (File.Exists(path))
            {
                File.Copy(path, backupPath, overwrite: true);
                Logger.Debug("Backup created successfully");
            }

            // Atomically replace the main file
            File.Move(tempPath, path, overwrite: true);
            Logger.Info("Game library saved successfully");
        }
        catch (JsonException jsonEx)
        {
            Logger.Error($"JSON serialization failed during save: {jsonEx.Message}");
            CleanupTempFile(tempPath);
        }
        catch (IOException ioEx)
        {
            Logger.Error($"File I/O error during save: {ioEx.Message}");
            CleanupTempFile(tempPath);
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error while saving game library: {ex.Message}\nFull Error:\n{ex}");
            CleanupTempFile(tempPath);
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
            Logger.Info("File is in .XEX format.");
            if (XexUtility.ExtractData(File.ReadAllBytes(gamePath), out string parsedTitleId, out string parsedMediaId))
            {
                gameId = parsedTitleId;
                mediaId = parsedMediaId;
            }
        }
        else
        {
            // Unpack .xex file (.iso??)
            using XisoContainerReader xisoContainerReader = new XisoContainerReader(gamePath);
            if (xisoContainerReader.TryMount() && xisoContainerReader.TryGetDefault(out byte[] data))
            {
                Logger.Info("File is in .ISO format.");
                if (XexUtility.ExtractData(data, out string parsedTitleId, out string parsedMediaId))
                {
                    gameId = parsedTitleId;
                    mediaId = parsedMediaId;
                }
            }
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
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaCanary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir);
                break;
            case XeniaVersion.Mousehook:
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaMousehook.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir);
                break;
            case XeniaVersion.Netplay:
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaNetplay.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir);
                break;
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

    public static bool CheckForDuplicateGame(string gamePath)
    {
        foreach (Game game in Games)
        {
            if (gamePath == game.FileLocations.Game)
            {
                return true;
            }
        }
        return false;
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
        try
        {
            await CompatibilityManager.GetCompatibility(newGame, titleId);
        }
        catch (HttpRequestException httpReqEx)
        {
            Logger.Error($"{httpReqEx.Message}\nFull Error:\n{httpReqEx}");
        }
        catch (TaskCanceledException taskEx)
        {
            // This exception may indicate a timeout.
            Logger.Error($"{taskEx.Message}\nFull Error:\n{taskEx}");
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

        Logger.Info($"Creating a new configuration file for {newGame.Title}");
        switch (version)
        {
            case XeniaVersion.Canary:
                File.Copy(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation),
                    Path.Combine(DirectoryPaths.Base, Path.GetDirectoryName(XeniaCanary.ConfigLocation), $"{newGame.Title}.config.toml"), true);
                newGame.FileLocations.Config = Path.Combine(Path.GetDirectoryName(XeniaCanary.ConfigLocation), $"{newGame.Title}.config.toml");
                break;
            case XeniaVersion.Mousehook:
                File.Copy(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation),
                    Path.Combine(DirectoryPaths.Base, Path.GetDirectoryName(XeniaMousehook.ConfigLocation), $"{newGame.Title}.config.toml"), true);
                newGame.FileLocations.Config = Path.Combine(Path.GetDirectoryName(XeniaMousehook.ConfigLocation), $"{newGame.Title}.config.toml");
                break;
            case XeniaVersion.Netplay:
                File.Copy(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation),
                    Path.Combine(DirectoryPaths.Base, Path.GetDirectoryName(XeniaNetplay.ConfigLocation), $"{newGame.Title}.config.toml"), true);
                newGame.FileLocations.Config = Path.Combine(Path.GetDirectoryName(XeniaNetplay.ConfigLocation), $"{newGame.Title}.config.toml");
                break;
        }

        newGame.XeniaVersion = version;

        // Download Artwork
        Directory.CreateDirectory(Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork"));

        // Default Boxart
        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Boxart.png"), MagickFormat.Png);
        newGame.Artwork.Boxart = Path.Combine("GameData", newGame.Title, "Artwork", "Boxart.png");

        // Default Icon
        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Icon.png", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Icon.ico"), MagickFormat.Ico);
        newGame.Artwork.Icon = Path.Combine("GameData", newGame.Title, "Artwork", "Icon.ico");

        // Default Background
        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Background.jpg"), MagickFormat.Jpeg);
        newGame.Artwork.Background = Path.Combine("GameData", newGame.Title, "Artwork", "Background.jpg");

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
                File.Copy(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation),
                    Path.Combine(DirectoryPaths.Base, Path.GetDirectoryName(XeniaCanary.ConfigLocation), $"{newGame.Title}.config.toml"), true);
                newGame.FileLocations.Config = Path.Combine(Path.GetDirectoryName(XeniaCanary.ConfigLocation), $"{newGame.Title}.config.toml");
                break;
            case XeniaVersion.Mousehook:
                File.Copy(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation),
                    Path.Combine(DirectoryPaths.Base, Path.GetDirectoryName(XeniaMousehook.ConfigLocation), $"{newGame.Title}.config.toml"), true);
                newGame.FileLocations.Config = Path.Combine(Path.GetDirectoryName(XeniaMousehook.ConfigLocation), $"{newGame.Title}.config.toml");
                break;
            case XeniaVersion.Netplay:
                File.Copy(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation),
                    Path.Combine(DirectoryPaths.Base, Path.GetDirectoryName(XeniaNetplay.ConfigLocation), $"{newGame.Title}.config.toml"), true);
                newGame.FileLocations.Config = Path.Combine(Path.GetDirectoryName(XeniaNetplay.ConfigLocation), $"{newGame.Title}.config.toml");
                break;
        }

        // Create an Artwork directory
        Directory.CreateDirectory(Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork"));
        DownloadManager downloadManager = new DownloadManager();

        // Download Boxart
        if (fullGameInfo.Artwork.Boxart == null)
        {
            // Use default boxart since the game doesn't have any boxart
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Boxart.png"), MagickFormat.Png);
        }
        else
        {
            // Check if the Xbox Marketplace url works before downloading it
            if (await downloadManager.CheckIfUrlWorksAsync(fullGameInfo.Artwork.Boxart, "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} boxart from Xbox Marketplace.");
                await downloadManager.DownloadArtwork(fullGameInfo.Artwork.Boxart, Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Boxart.png"), MagickFormat.Png);
            }
            // Check if the GitHub repo url works before downloading it
            else if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxDatabaseArtworkBase, titleId, "boxart.jpg"), "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} boxart from GitHub repository.");
                await downloadManager.DownloadArtwork(string.Format(Urls.XboxDatabaseArtworkBase, titleId, "boxart.jpg"), Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Boxart.png"), MagickFormat.Png);
            }
            else
            {
                // Use default boxart since Xenia Manager can't fetch the boxart from the internet
                Logger.Info($"Using default artwork since we can't fetch the boxart from the internet");
                ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Boxart.png"), MagickFormat.Png);
            }
        }
        newGame.Artwork.Boxart = Path.Combine("GameData", newGame.Title, "Artwork", "Boxart.png");

        // Download Icon
        if (fullGameInfo.Artwork.Icon == null)
        {
            // Use the default icon since the game doesn't have any icon
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Icon.png", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Icon.ico"), MagickFormat.Ico);
        }
        else
        {
            // Check if the Xbox Marketplace url works before downloading it
            if (await downloadManager.CheckIfUrlWorksAsync(fullGameInfo.Artwork.Icon, "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} icon from Xbox Marketplace.");
                await downloadManager.DownloadArtwork(fullGameInfo.Artwork.Icon, Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Icon.ico"), MagickFormat.Ico, 64, 64);
            }
            // Check if the GitHub repo url works before downloading it
            else if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxDatabaseArtworkBase, titleId, "icon.png"), "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} icon from GitHub repository.");
                await downloadManager.DownloadArtwork(string.Format(Urls.XboxDatabaseArtworkBase, titleId, "icon.png"), Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Icon.ico"), MagickFormat.Ico, 64, 64);
            }
            else
            {
                // Use the default icon since Xenia Manager can't fetch the icon from the internet
                Logger.Info($"Using default artwork since we can't fetch the icon from the internet");
                ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Icon.png", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Icon.ico"), MagickFormat.Ico, 64, 64);
            }
        }
        newGame.Artwork.Icon = Path.Combine("GameData", newGame.Title, "Artwork", "Icon.ico");

        // Download Background
        if (fullGameInfo.Artwork.Background == null)
        {
            // Use the default background since the game doesn't have any icon
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Background.jpg"), MagickFormat.Jpeg);
        }
        else
        {
            // Check if the Xbox Marketplace url works before downloading it
            if (await downloadManager.CheckIfUrlWorksAsync(fullGameInfo.Artwork.Background, "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} background from Xbox Marketplace.");
                await downloadManager.DownloadArtwork(fullGameInfo.Artwork.Background, Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Background.jpg"), MagickFormat.Jpeg);
            }
            // Check if the GitHub repo url works before downloading it
            else if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxDatabaseArtworkBase, titleId, "background.jpg"), "image/"))
            {
                Logger.Info($"Downloading {newGame.Title} background from GitHub repository.");
                await downloadManager.DownloadArtwork(string.Format(Urls.XboxDatabaseArtworkBase, titleId, "background.jpg"), Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Background.jpg"), MagickFormat.Jpeg);
            }
            else
            {
                // Use the default background since Xenia Manager can't fetch the icon from the internet
                Logger.Info($"Using default artwork since we can't fetch the icon from the internet");
                ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg", Path.Combine(Constants.DirectoryPaths.GameData, newGame.Title, "Artwork", "Background.jpg"), MagickFormat.Jpeg);
            }
        }
        newGame.Artwork.Background = Path.Combine("GameData", newGame.Title, "Artwork", "Background.jpg");

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
                XeniaVersion.Canary => Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir, "content", game.GameId),
                XeniaVersion.Mousehook => Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir, "content", game.GameId),
                XeniaVersion.Netplay => Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir, "content", game.GameId),
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

    private static string UpdateGamePath(string originalPath, string newTitle)
    {
        string[] parts = originalPath.Split(Path.DirectorySeparatorChar);
        parts[1] = newTitle; // Replace the title part
        return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
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
        game.Artwork.Boxart = UpdateGamePath(game.Artwork.Boxart, newGameTitle);
        game.Artwork.Background = UpdateGamePath(game.Artwork.Background, newGameTitle);
        game.Artwork.Icon = UpdateGamePath(game.Artwork.Icon, newGameTitle);
    }
}
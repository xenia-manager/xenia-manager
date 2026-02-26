using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Database;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Database;
using XeniaManager.Core.Models.Database.Xbox;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages the game library by loading from and saving to a local file
/// </summary>
public class GameManager
{
    /// <summary>
    /// Gets or sets the list of games in the library
    /// </summary>
    public static List<Game> Games { get; set; } = [];

    /// <summary>
    /// Loads the game library from the local file
    /// If the file doesn't exist, creates a new empty library
    /// If the file is corrupted, attempts to recover from backup
    /// </summary>
    public static void LoadLibrary()
    {
        string path = AppPaths.GameLibraryPath;
        string backupPath = path + ".backup";

        try
        {
            if (!File.Exists(path))
            {
                // Can't find the local library file so create a new one
                Logger.Info<GameManager>($"Game library file not found at {path}, creating a new empty library");
                Games = [];
                SaveLibrary();
                Logger.Info<GameManager>("New empty game library created and saved successfully");
                return;
            }

            Logger.Info<GameManager>($"Loading game library from {path}");
            string content = File.ReadAllText(path);

            // Validate content is not empty
            if (string.IsNullOrWhiteSpace(content))
            {
                Logger.Error<GameManager>("Game library file is empty, throwing JsonException");
                throw new JsonException("Game library file is empty");
            }

            Logger.Debug<GameManager>("Attempting to deserialize game library content");
            // Attempt deserialization
            List<Game>? deserializedGames = JsonSerializer.Deserialize<List<Game>>(content);

            if (deserializedGames == null)
            {
                Logger.Error<GameManager>("Deserialization resulted in null, throwing JsonException");
                throw new JsonException("Deserialization resulted in null");
            }

            Games = [];

            Logger.Debug<GameManager>("Validating game entries for required fields");
            // Validate all games have required fields
            foreach (Game game in deserializedGames)
            {
                if (string.IsNullOrWhiteSpace(game.Title) || string.IsNullOrWhiteSpace(game.GameId))
                {
                    // Game entry with missing required fields detected, log it and skip it
                    Logger.Warning<GameManager>($"Game entry with missing required fields detected - Title: '{game.Title}', GameId: '{game.GameId}'. Skipping this entry.");
                    continue;
                }

                Logger.Trace<GameManager>($"Adding game entry to library: {game.Title} ({game.GameId})");
                Games.Add(game);
            }

            Logger.Info<GameManager>($"Successfully loaded {Games.Count} games from the library");
        }
        catch (JsonException jsonEx)
        {
            Logger.Error<GameManager>($"JSON parsing error while loading game library: {jsonEx.Message}");
            Logger.LogExceptionDetails<GameManager>(jsonEx);
            AttemptBackupRecovery(backupPath);
        }
        catch (IOException ioEx)
        {
            Logger.Error<GameManager>($"IO error while loading game library: {ioEx.Message}");
            Logger.LogExceptionDetails<GameManager>(ioEx);
            AttemptBackupRecovery(backupPath);
        }
        catch (Exception ex)
        {
            Logger.Error<GameManager>($"Unexpected error while loading game library: {ex.Message}");
            Logger.LogExceptionDetails<GameManager>(ex);
            AttemptBackupRecovery(backupPath);
        }
    }

    /// <summary>
    /// Saves the current game library to the local file
    /// Uses atomic file operations to prevent corruption during write
    /// Creates a backup of the previous version before saving
    /// </summary>
    public static void SaveLibrary(GameSortOption sortOption = GameSortOption.Title)
    {
        string path = AppPaths.GameLibraryPath;
        string tempPath = path + ".tmp";
        string backupPath = path + ".backup";

        try
        {
            SortLibrary(sortOption);
            Logger.Info<GameManager>($"Serializing {Games.Count} games to JSON for saving to {path}");

            string json = JsonSerializer.Serialize(Games, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (string.IsNullOrWhiteSpace(json))
            {
                Logger.Error<GameManager>("Serialization produced empty or null JSON, throwing InvalidOperationException");
                throw new InvalidOperationException("Serialization produced empty or null JSON");
            }

            Logger.Debug<GameManager>($"Writing serialized data to temporary file: {tempPath}");
            // Write to the temporary file (atomic operation)
            File.WriteAllText(tempPath, json);

            // Verify the temp file was written and is readable
            Logger.Debug<GameManager>($"Verifying temporary file was created at: {tempPath}");
            if (!File.Exists(tempPath))
            {
                Logger.Error<GameManager>($"Temporary file was not created at: {tempPath}, throwing IOException");
                throw new IOException("Temporary file was not created");
            }

            string tempContent = File.ReadAllText(tempPath);
            if (string.IsNullOrWhiteSpace(tempContent))
            {
                Logger.Error<GameManager>($"Temporary file is empty after write at: {tempPath}, throwing IOException");
                throw new IOException("Temporary file is empty after write");
            }

            Logger.Debug<GameManager>($"Creating backup of current library file from {path} to {backupPath}");
            // Create a backup from the current file before replacing it
            if (File.Exists(path))
            {
                File.Copy(path, backupPath, overwrite: true);
                Logger.Info<GameManager>($"Backup created successfully at: {backupPath}");
            }

            Logger.Info<GameManager>($"Atomically replacing main file {path} with temporary file {tempPath}");
            // Atomically replace the main file
            File.Move(tempPath, path, overwrite: true);

            Logger.Info<GameManager>($"Game library successfully saved to {path}");
        }
        catch (JsonException jsonEx)
        {
            Logger.Error<GameManager>($"JSON serialization error while saving game library: {jsonEx.Message}");
            Logger.LogExceptionDetails<GameManager>(jsonEx);
            CleanupTempFile(tempPath);
        }
        catch (IOException ioEx)
        {
            Logger.Error<GameManager>($"IO error while saving game library: {ioEx.Message}");
            Logger.LogExceptionDetails<GameManager>(ioEx);
            CleanupTempFile(tempPath);
        }
        catch (Exception ex)
        {
            Logger.Error<GameManager>($"Unexpected error while saving game library: {ex.Message}");
            Logger.LogExceptionDetails<GameManager>(ex);
            CleanupTempFile(tempPath);
        }
    }

    /// <summary>
    /// Sorts the game library based on the specified sort option.
    /// </summary>
    /// <param name="sortOption">The field to sort by (title, playtime, compatibility, etc.).</param>
    /// <param name="descending">If true, sorts in descending order; otherwise, ascending order.</param>
    public static void SortLibrary(GameSortOption sortOption = GameSortOption.Title, bool descending = false)
    {
        Logger.Debug<GameManager>($"Starting to sort game library by {sortOption}, descending: {descending}");
        Logger.Debug<GameManager>($"Current library contains {Games.Count} games before sorting");

        Comparison<Game> comparison = sortOption switch
        {
            GameSortOption.Title => (x, y) => string.Compare(x.Title, y.Title, StringComparison.OrdinalIgnoreCase),
            GameSortOption.Playtime => (x, y) => Nullable.Compare<double>(x.Playtime, y.Playtime),
            GameSortOption.Compatibility => (x, y) => x.Compatibility.Rating.CompareTo(y.Compatibility.Rating),
            GameSortOption.GameId => (x, y) => string.Compare(x.GameId, y.GameId, StringComparison.OrdinalIgnoreCase),
            GameSortOption.MediaId => (x, y) => string.Compare(x.MediaId, y.MediaId, StringComparison.OrdinalIgnoreCase),
            GameSortOption.XeniaVersion => (x, y) => x.XeniaVersion.CompareTo(y.XeniaVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(sortOption), $"Unsupported sort field: {sortOption}")
        };

        Logger.Trace<GameManager>($"Applying sort comparison for {sortOption}");
        Games.Sort((x, y) => descending ? comparison(y, x) : comparison(x, y));
        Logger.Info<GameManager>($"Game library successfully sorted by {sortOption} (descending: {descending})");
    }

    /// <summary>
    /// Retrieves game details by launching Xenia emulator with the specified game and parsing the window title and log file
    /// Uses two methods to extract game information: window title parsing and log file scanning
    /// </summary>
    /// <param name="gamePath">The path to the game file to analyze</param>
    /// <param name="version">The Xenia version to use for launching the game</param>
    /// <returns>A tuple containing (gameTitle, titleId, mediaId) extracted from Xenia</returns>
    public static async Task<(string, string, string)> GetGameDetailsWithXenia(string gamePath, XeniaVersion version)
    {
        Logger.Info<GameManager>($"Starting to retrieve game details with Xenia for game: {gamePath}, version: {version}");

        // Grab info about the selected Xenia version
        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
        Logger.Debug<GameManager>($"Retrieved Xenia version info - Executable: {versionInfo.ExecutableLocation}, Emulator Dir: {versionInfo.EmulatorDir}");

        // Launch the game with Xenia to fetch details
        Process xenia = new Process();
        xenia.StartInfo.FileName = AppPathResolver.GetFullPath(versionInfo.ExecutableLocation);
        xenia.StartInfo.WorkingDirectory = AppPathResolver.GetFullPath(versionInfo.EmulatorDir);
        xenia.StartInfo.Arguments = $@"""{gamePath}""";
        xenia.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

        Logger.Trace<GameManager>($"Setting up process - Executable: {xenia.StartInfo.FileName}, Working Directory: {xenia.StartInfo.WorkingDirectory}, Arguments: {xenia.StartInfo.Arguments}");

        xenia.Start();
        Logger.Info<GameManager>($"Started Xenia process for game: {gamePath} with PID: {xenia.Id}");

        xenia.WaitForInputIdle();

        // Information we're looking for
        string gameTitle = "Not found";
        string titleId = "Not found";
        string mediaId = string.Empty;

        Process process = Process.GetProcessById(xenia.Id);
        Logger.Info<GameManager>("Trying to find the game title from Xenia Window Title");
        int NumberOfTries = 0;

        // Method 1 - Using Xenia Window Title
        // Repeats for a certain time before moving on
        Logger.Debug<GameManager>("Starting to extract game details from Xenia window title");
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

            Logger.Trace<GameManager>($"Attempt {NumberOfTries}: Window title: '{process.MainWindowTitle}', Extracted Title: '{gameTitle}', ID: '{titleId}'");

            process = Process.GetProcessById(xenia.Id);

            NumberOfTries++;

            // Check if this reached the maximum number of attempts
            // If it did, break the while loop
            if (NumberOfTries > 1000)
            {
                Logger.Warning<GameManager>($"Maximum attempts reached ({NumberOfTries}), could not extract game details from window title. Game title: '{gameTitle}', ID: '{titleId}'");
                gameTitle = "Not found";
                titleId = "Not found";
                break;
            }

            await Task.Delay(100); // Delay between repeating to ensure everything loads
        }
        Logger.Debug<GameManager>($"Completed window title extraction. Found Title: '{gameTitle}', ID: '{titleId}', Attempts: {NumberOfTries}");

        Logger.Info<GameManager>($"Killing Xenia process with PID: {xenia.Id}");
        xenia.Kill(); // Force to close Xenia

        // Method 2 - Using Xenia.log (In case method 1 fails)
        // Checks if xenia.log exists and if it does, goes through it, trying to find Title, TitleID and MediaID
        string logFilePath = Path.Combine(xenia.StartInfo.WorkingDirectory, "xenia.log");
        if (File.Exists(logFilePath))
        {
            Logger.Debug<GameManager>($"Found xenia.log file at: {logFilePath}, starting to parse for game details");

            await using FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader sr = new StreamReader(fs);

            int linesProcessed = 0;
            // Goes through every line in xenia.log, trying to find lines that contain Title, TitleID and MediaID
            while (await sr.ReadLineAsync() is { } line)
            {
                linesProcessed++;

                switch (true)
                {
                    case var _ when line.ToLower().Contains("title name"):
                    {
                        string[] split = line.Split(':');
                        if (gameTitle == "Not found")
                        {
                            gameTitle = split[1].TrimStart();
                            Logger.Debug<GameManager>($"Extracted title name from log: '{gameTitle}' at line {linesProcessed}");
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
                            Logger.Debug<GameManager>($"Extracted title ID from log: '{titleId}' at line {linesProcessed}");
                        }

                        break;
                    }
                    case var _ when line.ToLower().Contains("media id"):
                    {
                        string[] split = line.Split(':');
                        mediaId = split[1].TrimStart();
                        Logger.Debug<GameManager>($"Extracted media ID from log: '{mediaId}' at line {linesProcessed}");
                        break;
                    }
                }
            }
            Logger.Debug<GameManager>($"Completed parsing xenia.log, processed {linesProcessed} lines");
        }
        else
        {
            Logger.Warning<GameManager>($"xenia.log file not found at: {logFilePath}, skipping log parsing method");
        }

        // If game details were not found, use "foldername\filename" as fallback
        if (gameTitle == "Not found")
        {
            Logger.Warning<GameManager>($"Could not extract game title from Xenia, using fallback method. Game path: {gamePath}");
            string? directoryName = Path.GetFileName(Path.GetDirectoryName(gamePath));
            string fileName = Path.GetFileNameWithoutExtension(gamePath);
            gameTitle = $"{directoryName}\\{fileName}";
            Logger.Debug<GameManager>($"Applied fallback game title: '{gameTitle}'");
        }

        Logger.Info<GameManager>($"Successfully retrieved game details - Title: '{gameTitle}', ID: '{titleId}', Media ID: '{mediaId}'");

        // Return what has been found
        return (gameTitle, titleId, mediaId);
    }

    /// <summary>
    /// Adds a new game to the library by fetching detailed information from the Xbox database,
    /// downloading artwork, and creating necessary configuration files.
    /// </summary>
    /// <param name="xeniaVersion">The Xenia version to associate with this game.</param>
    /// <param name="selectedGame">The game information containing alternative IDs.</param>
    /// <param name="gamePath">The file path to the game.</param>
    /// <param name="titleId">The title ID of the game.</param>
    /// <param name="mediaId">The media ID of the game.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when game information cannot be fetched from the database.</exception>
    public static async Task AddGame(XeniaVersion xeniaVersion, GameInfo selectedGame, string gamePath, string gameTitle, string titleId, string mediaId)
    {
        Logger.Trace<GameManager>($"Starting AddGame operation - TitleId: {titleId}, MediaId: {mediaId}, XeniaVersion: {xeniaVersion}, GamePath: {gamePath}");

        // Grab full game information
        Logger.Info<GameManager>($"Fetching detailed game information from Xbox database for TitleId: {titleId}");
        GameDetailedInfo? detailedGameInfo = await XboxDatabase.GetFullGameInfo(titleId);
        if (detailedGameInfo == null)
        {
            Logger.Error<GameManager>($"Failed to fetch game information for TitleId: {titleId} - database returned null");
            // TODO: Throw an exception couldn't fetch game information
            throw new Exception("Couldn't fetch game information");
        }

        Logger.Info<GameManager>($"Successfully retrieved game information - Title: '{detailedGameInfo.Title?.Full}'");
        Logger.Debug<GameManager>($"Processing game title - Original: '{detailedGameInfo.Title?.Full}', Sanitized: '{detailedGameInfo.Title?.Full?.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ')}'");

        // Create a new game entry
        Game newGame = new Game
        {
            Title = detailedGameInfo.Title?.Full?.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ') ?? gameTitle.Replace(":", " -").Replace('\\', ' ').Replace('/', ' '),
            GameId = titleId,
            AlternativeIDs = selectedGame.AlternativeId!,
            MediaId = mediaId,
            XeniaVersion = xeniaVersion,
            FileLocations =
            {
                Game = gamePath
            }
        };

        Logger.Debug<GameManager>($"Created new game entry - Title: '{newGame.Title}', GameId: {newGame.GameId}, MediaId: {newGame.MediaId}");

        // Fetch Compatibility Rating
        await GameCompatibilityDatabase.SetCompatibilityRating(newGame);

        // Check for duplicates
        Logger.Debug<GameManager>($"Checking for duplicate games with title: '{newGame.Title}'");
        if (Games.Any(game => game.Title == newGame.Title))
        {
            Logger.Warning<GameManager>($"Duplicate game title detected: '{newGame.Title}'. Generating unique title with counter suffix.");
            int counter = 1;
            string? OriginalGameTitle = newGame.Title;
            while (Games.Any(game => game.Title == newGame.Title))
            {
                newGame.Title = $"{OriginalGameTitle} ({counter})";
                Logger.Trace<GameManager>($"Trying unique title: '{newGame.Title}' (counter: {counter})");
                counter++;
            }
            Logger.Info<GameManager>($"Generated unique title: '{newGame.Title}' to avoid duplicate");
        }
        else
        {
            Logger.Debug<GameManager>($"No duplicate found for title: '{newGame.Title}'");
        }

        // Create a new configuration file for the game
        Logger.Info<GameManager>($"Creating configuration file for game: '{newGame.Title}'");
        newGame.FileLocations.Config = Path.Combine(XeniaPaths.Canary.ConfigFolderLocation, $"{newGame.Title}.config.toml");
        Logger.Debug<GameManager>($"Configuration file path: {newGame.FileLocations.Config}");
        ConfigManager.CreateConfigurationFile(AppPathResolver.GetFullPath(newGame.FileLocations.Config), xeniaVersion);
        Logger.Info<GameManager>($"Configuration file created successfully at: {newGame.FileLocations.Config}");

        // Create Artwork Directory
        string artworkDirectory = Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork");
        Logger.Debug<GameManager>($"Creating artwork directory: {artworkDirectory}");
        Directory.CreateDirectory(artworkDirectory);
        Logger.Info<GameManager>($"Artwork directory created successfully: {artworkDirectory}");

        DownloadManager downloadManager = new DownloadManager();
        Logger.Trace<GameManager>($"DownloadManager initialized for artwork download operations");

        // Download Artwork
        // Boxart
        Logger.Info<GameManager>($"Starting boxart download process for game: '{newGame.Title}'");
        if (detailedGameInfo.Artwork?.Boxart != null)
        {
            Logger.Debug<GameManager>($"Boxart URL available from database: {detailedGameInfo.Artwork.Boxart}");

            // Check if the Xbox Marketplace url works before downloading it
            Logger.Trace<GameManager>($"Attempting to download boxart from Xbox Marketplace URL");
            if (await downloadManager.CheckIfUrlWorksAsync(detailedGameInfo.Artwork.Boxart, "image/"))
            {
                Logger.Info<GameManager>($"Xbox Marketplace URL is valid, downloading boxart");
                await downloadManager.DownloadArtwork(detailedGameInfo.Artwork.Boxart, Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Boxart.png"), SKEncodedImageFormat.Png);
                Logger.Info<GameManager>($"Boxart downloaded successfully from Xbox Marketplace");
            }
            // Check if the GitHub Pages url works before downloading it
            else
            {
                Logger.Warning<GameManager>($"Xbox Marketplace URL failed, trying GitHub Pages URL");
                Logger.Trace<GameManager>($"Attempting to download boxart from GitHub Pages URL: {string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "boxart.jpg")}");
                if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "boxart.jpg"), "image/"))
                {
                    Logger.Info<GameManager>($"GitHub Pages URL is valid, downloading boxart");
                    await downloadManager.DownloadArtwork(string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "boxart.jpg"),
                        Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Boxart.png"), SKEncodedImageFormat.Png);
                    Logger.Info<GameManager>($"Boxart downloaded successfully from GitHub Pages");
                }
                // Check if the Raw Github url works before downloading it
                else
                {
                    Logger.Warning<GameManager>($"GitHub Pages URL failed, trying Raw GitHub URL");
                    Logger.Trace<GameManager>($"Attempting to download boxart from Raw GitHub URL: {string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "boxart.jpg")}");
                    if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "boxart.jpg"), "image/"))
                    {
                        Logger.Info<GameManager>($"Raw GitHub URL is valid, downloading boxart");
                        await downloadManager.DownloadArtwork(string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "boxart.jpg"),
                            Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Boxart.png"), SKEncodedImageFormat.Png);
                        Logger.Info<GameManager>($"Boxart downloaded successfully from Raw GitHub");
                    }
                    else
                    {
                        Logger.Warning<GameManager>($"All remote boxart URLs failed, using local default artwork");
                        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg",
                            Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Boxart.png"), SKEncodedImageFormat.Png);
                        Logger.Info<GameManager>($"Default local boxart applied successfully");
                    }
                }
            }
        }
        else
        {
            Logger.Warning<GameManager>($"No boxart URL available in database, using local default artwork");
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg",
                Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Boxart.png"), SKEncodedImageFormat.Png);
            Logger.Info<GameManager>($"Default local boxart applied successfully");
        }

        newGame.Artwork.Boxart = Path.Combine("GameData", newGame.Title!, "Artwork", "Boxart.png");
        Logger.Debug<GameManager>($"Boxart artwork path set: {newGame.Artwork.Boxart}");

        // Icon
        Logger.Info<GameManager>($"Starting icon download process for game: '{newGame.Title}'");
        if (detailedGameInfo.Artwork?.Icon != null)
        {
            Logger.Debug<GameManager>($"Icon URL available from database: {detailedGameInfo.Artwork.Icon}");

            // Check if the Xbox Marketplace url works before downloading it
            Logger.Trace<GameManager>($"Attempting to download icon from Xbox Marketplace URL");
            if (await downloadManager.CheckIfUrlWorksAsync(detailedGameInfo.Artwork.Icon, "image/"))
            {
                Logger.Info<GameManager>($"Xbox Marketplace URL is valid, downloading icon");
                await downloadManager.DownloadArtwork(detailedGameInfo.Artwork.Icon, Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Icon.ico"));
                Logger.Info<GameManager>($"Icon downloaded successfully from Xbox Marketplace");
            }
            // Check if the GitHub Pages url works before downloading it
            else
            {
                Logger.Warning<GameManager>($"Xbox Marketplace URL failed, trying GitHub Pages URL");
                Logger.Trace<GameManager>($"Attempting to download icon from GitHub Pages URL: {string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "icon.png")}");
                if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "icon.png"), "image/"))
                {
                    Logger.Info<GameManager>($"GitHub Pages URL is valid, downloading icon");
                    await downloadManager.DownloadArtwork(string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "icon.png"),
                        Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Icon.ico"));
                    Logger.Info<GameManager>($"Icon downloaded successfully from GitHub Pages");
                }
                // Check if the Raw Github url works before downloading it
                else
                {
                    Logger.Warning<GameManager>($"GitHub Pages URL failed, trying Raw GitHub URL");
                    Logger.Trace<GameManager>($"Attempting to download icon from Raw GitHub URL: {string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "icon.png")}");
                    if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "icon.png"), "image/"))
                    {
                        Logger.Info<GameManager>($"Raw GitHub URL is valid, downloading icon");
                        await downloadManager.DownloadArtwork(string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "icon.png"),
                            Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Icon.ico"));
                        Logger.Info<GameManager>($"Icon downloaded successfully from Raw GitHub");
                    }
                    else
                    {
                        Logger.Warning<GameManager>($"All remote icon URLs failed, using local default icon");
                        ArtworkManager.LocalArtworkAsIcon("XeniaManager.Core.Assets.Artwork.Icon.png",
                            Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Icon.ico"));
                        Logger.Info<GameManager>($"Default local icon applied successfully");
                    }
                }
            }
        }
        else
        {
            Logger.Warning<GameManager>($"No icon URL available in database, using local default icon");
            ArtworkManager.LocalArtworkAsIcon("XeniaManager.Core.Assets.Artwork.Icon.png",
                Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Icon.ico"));
            Logger.Info<GameManager>($"Default local icon applied successfully");
        }

        newGame.Artwork.Icon = Path.Combine("GameData", newGame.Title!, "Artwork", "Icon.ico");
        Logger.Debug<GameManager>($"Icon artwork path set: {newGame.Artwork.Icon}");

        // Background
        Logger.Info<GameManager>($"Starting background download process for game: '{newGame.Title}'");
        if (detailedGameInfo.Artwork?.Background != null)
        {
            Logger.Debug<GameManager>($"Background URL available from database: {detailedGameInfo.Artwork.Background}");

            // Check if the Xbox Marketplace url works before downloading it
            Logger.Trace<GameManager>($"Attempting to download background from Xbox Marketplace URL");
            if (await downloadManager.CheckIfUrlWorksAsync(detailedGameInfo.Artwork.Background, "image/"))
            {
                Logger.Info<GameManager>($"Xbox Marketplace URL is valid, downloading background");
                await downloadManager.DownloadArtwork(detailedGameInfo.Artwork.Background,
                    Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Background.jpg"), SKEncodedImageFormat.Jpeg);
                Logger.Info<GameManager>($"Background downloaded successfully from Xbox Marketplace");
            }
            // Check if the GitHub Pages url works before downloading it
            else
            {
                Logger.Warning<GameManager>($"Xbox Marketplace URL failed, trying GitHub Pages URL");
                Logger.Trace<GameManager>($"Attempting to download background from GitHub Pages URL: {string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "background.jpg")}");
                if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "background.jpg"), "image/"))
                {
                    Logger.Info<GameManager>($"GitHub Pages URL is valid, downloading background");
                    await downloadManager.DownloadArtwork(string.Format(Urls.XboxMarketplaceDatabaseArtwork[0], titleId, "background.jpg"),
                        Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Background.jpg"), SKEncodedImageFormat.Jpeg);
                    Logger.Info<GameManager>($"Background downloaded successfully from GitHub Pages");
                }
                // Check if the Raw Github url works before downloading it
                else
                {
                    Logger.Warning<GameManager>($"GitHub Pages URL failed, trying Raw GitHub URL");
                    Logger.Trace<GameManager>($"Attempting to download background from Raw GitHub URL: {string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "background.jpg")}");
                    if (await downloadManager.CheckIfUrlWorksAsync(string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "background.jpg"), "image/"))
                    {
                        Logger.Info<GameManager>($"Raw GitHub URL is valid, downloading background");
                        await downloadManager.DownloadArtwork(string.Format(Urls.XboxMarketplaceDatabaseArtwork[1], titleId, "background.jpg"),
                            Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Background.jpg"), SKEncodedImageFormat.Jpeg);
                        Logger.Info<GameManager>($"Background downloaded successfully from Raw GitHub");
                    }
                    else
                    {
                        Logger.Warning<GameManager>($"All remote background URLs failed, using local default background");
                        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg",
                            Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Background.jpg"), SKEncodedImageFormat.Jpeg);
                        Logger.Info<GameManager>($"Default local background applied successfully");
                    }
                }
            }
        }
        else
        {
            Logger.Warning<GameManager>($"No background URL available in database, using local default background");
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg",
                Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Background.jpg"), SKEncodedImageFormat.Jpeg);
            Logger.Info<GameManager>($"Default local background applied successfully");
        }

        newGame.Artwork.Background = Path.Combine("GameData", newGame.Title!, "Artwork", "Background.jpg");
        Logger.Debug<GameManager>($"Background artwork path set: {newGame.Artwork.Background}");

        // Add the new game to the library
        Logger.Info<GameManager>($"Adding game '{newGame.Title}' ({newGame.GameId}) to the game library");
        Games.Add(newGame);
        Logger.Debug<GameManager>($"Game added to in-memory library. Total games in library: {Games.Count}");

        // Save the library to persist changes
        Logger.Info<GameManager>($"Saving game library to persist changes");
        SaveLibrary();
        Logger.Info<GameManager>($"Game library saved successfully");

        Logger.Info<GameManager>($"AddGame operation completed successfully - Title: '{newGame.Title}', GameId: {newGame.GameId}");
        Logger.Trace<GameManager>("AddGame operation finished");
    }

    /// <summary>
    /// Adds a new game to the library without fetching detailed information from the Xbox database.
    /// Uses default artwork and creates necessary configuration files.
    /// This method is typically used for games not found in the Xbox database or for custom game entries.
    /// </summary>
    /// <param name="xeniaVersion">The Xenia version to associate with this game.</param>
    /// <param name="gameTitle">The title of the game to add.</param>
    /// <param name="gamePath">The file path to the game.</param>
    /// <param name="titleId">The title ID of the game.</param>
    /// <param name="mediaId">The media ID of the game.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task AddUnknownGame(XeniaVersion xeniaVersion, string gameTitle, string gamePath, string titleId, string mediaId)
    {
        Logger.Trace<GameManager>($"Starting AddUnknownGame operation - Title: '{gameTitle}', TitleId: {titleId}, MediaId: {mediaId}, XeniaVersion: {xeniaVersion}, GamePath: {gamePath}");

        Logger.Info<GameManager>($"Creating new game entry for unknown game: '{gameTitle}' (TitleId: {titleId})");
        Logger.Debug<GameManager>($"Processing game title - Original: '{gameTitle}', Sanitized: '{gameTitle.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ')}'");

        // Create a new game entry
        Game newGame = new Game
        {
            Title = gameTitle.Replace(":", " -").Replace('\\', ' ').Replace('/', ' '),
            GameId = titleId,
            MediaId = mediaId,
            XeniaVersion = xeniaVersion,
            FileLocations =
            {
                Game = gamePath
            }
        };

        Logger.Debug<GameManager>($"Created new game entry - Title: '{newGame.Title}', " +
                                  $"GameId: {newGame.GameId}, MediaId: {newGame.MediaId}");

        // Fetch Compatibility Rating
        await GameCompatibilityDatabase.SetCompatibilityRating(newGame);

        // Check for duplicates
        Logger.Debug<GameManager>($"Checking for duplicate games with title: '{newGame.Title}'");
        if (Games.Any(game => game.Title == newGame.Title))
        {
            Logger.Warning<GameManager>($"Duplicate game title detected: '{newGame.Title}'. Generating unique title with counter suffix.");
            int counter = 1;
            string? OriginalGameTitle = newGame.Title;
            while (Games.Any(game => game.Title == newGame.Title))
            {
                newGame.Title = $"{OriginalGameTitle} ({counter})";
                Logger.Trace<GameManager>($"Trying unique title: '{newGame.Title}' (counter: {counter})");
                counter++;
            }
            Logger.Info<GameManager>($"Generated unique title: '{newGame.Title}' to avoid duplicate");
        }
        else
        {
            Logger.Debug<GameManager>($"No duplicate found for title: '{newGame.Title}'");
        }

        // Create a new configuration file for the game
        Logger.Info<GameManager>($"Creating configuration file for game: '{newGame.Title}'");
        newGame.FileLocations.Config = Path.Combine(XeniaPaths.Canary.ConfigFolderLocation, $"{newGame.Title}.config.toml");
        Logger.Debug<GameManager>($"Configuration file path: {newGame.FileLocations.Config}");
        ConfigManager.CreateConfigurationFile(AppPathResolver.GetFullPath(newGame.FileLocations.Config), xeniaVersion);
        Logger.Info<GameManager>($"Configuration file created successfully at: {newGame.FileLocations.Config}");

        // Create Artwork Directory
        string artworkDirectory = Path.Combine(AppPaths.GameDataDirectory, newGame.Title, "Artwork");
        Logger.Debug<GameManager>($"Creating artwork directory: {artworkDirectory}");
        Directory.CreateDirectory(artworkDirectory);
        Logger.Info<GameManager>($"Artwork directory created successfully: {artworkDirectory}");

        // Default Boxart
        Logger.Info<GameManager>($"Applying default boxart artwork for game: '{newGame.Title}'");
        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg",
            Path.Combine(AppPaths.GameDataDirectory, newGame.Title, "Artwork", "Boxart.png"), SKEncodedImageFormat.Png);
        Logger.Info<GameManager>($"Default boxart applied successfully");
        newGame.Artwork.Boxart = Path.Combine("GameData", newGame.Title, "Artwork", "Boxart.png");
        Logger.Debug<GameManager>($"Boxart artwork path set: {newGame.Artwork.Boxart}");

        // Default Icon
        Logger.Info<GameManager>($"Applying default icon artwork for game: '{newGame.Title}'");
        ArtworkManager.LocalArtworkAsIcon("XeniaManager.Core.Assets.Artwork.Icon.png",
            Path.Combine(AppPaths.GameDataDirectory, newGame.Title, "Artwork", "Icon.ico"));
        Logger.Info<GameManager>($"Default icon applied successfully");
        newGame.Artwork.Icon = Path.Combine("GameData", newGame.Title, "Artwork", "Icon.ico");
        Logger.Debug<GameManager>($"Icon artwork path set: {newGame.Artwork.Icon}");

        // Default Background
        Logger.Info<GameManager>($"Applying default background artwork for game: '{newGame.Title}'");
        ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg",
            Path.Combine(AppPaths.GameDataDirectory, newGame.Title, "Artwork", "Background.jpg"), SKEncodedImageFormat.Jpeg);
        Logger.Info<GameManager>($"Default background applied successfully");
        newGame.Artwork.Background = Path.Combine("GameData", newGame.Title, "Artwork", "Background.jpg");
        Logger.Debug<GameManager>($"Background artwork path set: {newGame.Artwork.Background}");

        // Add the new game to the library
        Logger.Info<GameManager>($"Adding unknown game '{newGame.Title}' ({newGame.GameId}) to the game library");
        Games.Add(newGame);
        Logger.Debug<GameManager>($"Game added to in-memory library. Total games in library: {Games.Count}");

        // Save the library to persist changes
        Logger.Info<GameManager>($"Saving game library to persist changes");
        SaveLibrary();
        Logger.Info<GameManager>($"Game library saved successfully");

        Logger.Info<GameManager>($"AddUnknownGame operation completed successfully - Title: '{newGame.Title}', GameId: {newGame.GameId}");
        Logger.Trace<GameManager>("AddUnknownGame operation finished");
    }

    /// <summary>
    /// Removes a game from the library and optionally deletes associated files.
    /// Cleans up patch files, configuration files, artwork, and optionally the game content itself.
    /// </summary>
    /// <param name="game">The game to remove from the library.</param>
    /// <param name="deleteGameContent">
    /// If true, also deletes the game content folder from the Xenia content directory.
    /// If false, only removes metadata, artwork, and configuration files (default).
    /// </param>
    public static void RemoveGame(Game game, bool deleteGameContent = false)
    {
        Logger.Trace<GameManager>($"Starting RemoveGame operation - Title: '{game.Title}', GameId: {game.GameId}, DeleteGameContent: {deleteGameContent}");
        Logger.Info<GameManager>($"Initiating removal of game: '{game.Title}' ({game.GameId})");

        // Remove game patch
        Logger.Debug<GameManager>($"Checking for patch file at: {game.FileLocations.Patch}");
        if (!string.IsNullOrEmpty(game.FileLocations.Patch)
            && File.Exists(AppPathResolver.GetFullPath(game.FileLocations.Patch)))
        {
            Logger.Info<GameManager>($"Deleting patch file: {game.FileLocations.Patch}");
            File.Delete(AppPathResolver.GetFullPath(game.FileLocations.Patch));
            Logger.Debug<GameManager>($"Patch file deleted successfully: {game.FileLocations.Patch}");
        }
        else
        {
            Logger.Debug<GameManager>($"No patch file found or patch path is null, skipping patch deletion");
        }

        // Remove the game configuration file
        Logger.Debug<GameManager>($"Checking for configuration file at: {game.FileLocations.Config}");
        if (!string.IsNullOrEmpty(game.FileLocations.Config) 
            && File.Exists(AppPathResolver.GetFullPath(game.FileLocations.Config)))
        {
            Logger.Info<GameManager>($"Deleting configuration file: {game.FileLocations.Config}");
            File.Delete(AppPathResolver.GetFullPath(game.FileLocations.Config));
            Logger.Debug<GameManager>($"Configuration file deleted successfully: {game.FileLocations.Config}");
        }
        else
        {
            Logger.Debug<GameManager>($"No configuration file found or config path is null, skipping config deletion");
        }

        // Remove GameData (Artwork directory)
        Logger.Debug<GameManager>($"Checking for artwork directory. Game Title: '{game.Title}'");
        if (Directory.Exists(Path.Combine(AppPaths.GameDataDirectory, game.Title)))
        {
            string artworkDirectory = Path.Combine(AppPaths.GameDataDirectory, game.Title);
            Logger.Info<GameManager>($"Deleting artwork directory: {artworkDirectory}");
            Directory.Delete(Path.Combine(AppPaths.GameDataDirectory, game.Title), true);
            Logger.Debug<GameManager>($"Artwork directory deleted successfully: {artworkDirectory}");
        }
        else
        {
            Logger.Debug<GameManager>($"No artwork directory found or game title/artwork is null, skipping artwork deletion");
        }

        // Remove Game Content (Optional)
        if (deleteGameContent)
        {
            Logger.Info<GameManager>($"DeleteGameContent flag is true, proceeding with game content deletion");
            string emulatorContentFolder = AppPathResolver.GetFullPath(XeniaVersionInfo.GetXeniaVersionInfo(game.XeniaVersion).ContentFolderLocation);

            if (Directory.Exists(emulatorContentFolder))
            {
                foreach (string profileContentDirectory in Directory.EnumerateDirectories(emulatorContentFolder))
                {
                    string gameContentDirectory = Path.Combine(profileContentDirectory, game.GameId);
                    Logger.Debug<GameManager>($"Checking for profile content directory: {gameContentDirectory}");
                    if (!Directory.Exists(gameContentDirectory))
                    {
                        Logger.Debug<GameManager>($"Game content directory not found, skipping: {gameContentDirectory}");
                        continue;
                    }
                    Directory.Delete(gameContentDirectory, true);
                    Logger.Info<GameManager>($"Deleting profile content directory: {gameContentDirectory}");
                }
            }
            else
            {
                Logger.Warning<GameManager>($"Xenia content directory not found at: {emulatorContentFolder}");
            }
        }
        else
        {
            Logger.Debug<GameManager>("DeleteGameContent flag is false, skipping game content deletion");
        }

        // Remove game from the library
        Logger.Info<GameManager>($"Removing game '{game.Title}' ({game.GameId}) from in-memory library");
        bool removed = Games.Remove(game);
        if (removed)
        {
            Logger.Debug<GameManager>($"Game removed successfully from library. Remaining games: {Games.Count}");
        }
        else
        {
            Logger.Warning<GameManager>($"Game '{game.Title}' was not found in the library, nothing to remove");
        }

        // Save the library to persist changes
        Logger.Info<GameManager>($"Saving game library to persist removal changes");
        SaveLibrary();
        Logger.Info<GameManager>($"Game library saved successfully after removal");

        Logger.Info<GameManager>($"RemoveGame operation completed successfully - Title: '{game.Title}', GameId: {game.GameId}");
        Logger.Trace<GameManager>("RemoveGame operation finished");
    }

    /// <summary>
    /// Attempts to recover the game library from a backup file
    /// If recovery is successful, immediately re-saves to update the main file
    /// If recovery fails, creates a new empty library
    /// </summary>
    /// <param name="backupPath">The path to the backup file to recover from</param>
    private static void AttemptBackupRecovery(string backupPath)
    {
        Logger.Warning<GameManager>($"Attempting to recover game library from backup at: {backupPath}");

        if (!File.Exists(backupPath))
        {
            // No backup file found, clear the library and return
            Logger.Warning<GameManager>($"No backup file found at: {backupPath}. Creating a new empty library.");
            Games = [];
            SaveLibrary();
            Logger.Info<GameManager>("New empty game library created after failed recovery attempt.");
            return;
        }

        try
        {
            Logger.Info<GameManager>($"Reading backup file content from: {backupPath}");
            string backupContent = File.ReadAllText(backupPath);

            if (string.IsNullOrWhiteSpace(backupContent))
            {
                Logger.Error<GameManager>($"Backup file is empty at: {backupPath}, throwing JsonException");
                throw new JsonException("Backup file is empty");
            }

            Logger.Debug<GameManager>("Attempting to deserialize backup file content");
            List<Game>? recoveredGames = JsonSerializer.Deserialize<List<Game>>(backupContent);

            Games = recoveredGames ?? throw new JsonException("Backup deserialization resulted in null");

            Logger.Info<GameManager>($"Successfully recovered {Games.Count} games from backup. Re-saving library to clean state.");
            // Immediately re-save to clean state
            SaveLibrary();
            Logger.Info<GameManager>("Game library successfully re-saved after backup recovery.");
        }
        catch (Exception backupEx)
        {
            Logger.Error<GameManager>($"Failed to recover game library from backup: {backupEx.Message}");
            Logger.LogExceptionDetails<GameManager>(backupEx);
            Games = [];
            SaveLibrary();
            Logger.Warning<GameManager>("Created new empty game library after failed backup recovery.");
        }
    }

    /// <summary>
    /// Cleans up the temporary file created during the save operation
    /// Ensures that temporary files don't remain on the system after operations
    /// </summary>
    /// <param name="tempPath">The path to the temporary file to clean up</param>
    private static void CleanupTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                Logger.Debug<GameManager>($"Cleaning up temporary file at: {tempPath}");
                File.Delete(tempPath);
                Logger.Debug<GameManager>($"Temporary file successfully deleted: {tempPath}");
            }
            else
            {
                Logger.Debug<GameManager>($"Temporary file does not exist at: {tempPath}, no cleanup needed");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<GameManager>($"Failed to clean up temporary file at {tempPath}: {ex.Message}");
            Logger.LogExceptionDetails<GameManager>(ex);
        }
    }
}
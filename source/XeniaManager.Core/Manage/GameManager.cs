using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Database;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Database.Xbox;
using XeniaManager.Core.Models.Files;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;

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
    /// Retrieves game details by parsing the game file directly without launching Xenia.
    /// Supports STFS packages (CON, LIVE, PIRS), XEX executables (.xex), and ISO disc images (.iso, .xiso).
    /// </summary>
    /// <param name="gamePath">The path to the game file to analyze</param>
    /// <returns>Parsed game details. Returns default values if the file type is not recognized.</returns>
    public static ParsedGameDetails GetGameDetails(string gamePath)
    {
        Logger.Info<GameManager>($"Starting to retrieve game details for: {gamePath}");

        if (!File.Exists(gamePath))
        {
            Logger.Error<GameManager>($"Game file does not exist: {gamePath}");
            return new ParsedGameDetails();
        }

        try
        {
            FileSignature fileSignature = FileIdentifier.IdentifyFileType(gamePath);
            Logger.Debug<GameManager>($"Detected file type: {fileSignature}");

            switch (fileSignature)
            {
                // STFS packages (CON, LIVE, PIRS)
                case FileSignature.CON:
                case FileSignature.LIVE:
                case FileSignature.PIRS:
                {
                    Logger.Info<GameManager>($"Detected STFS package ({fileSignature}), parsing: {gamePath}");
                    StfsFile stfs = StfsFile.Load(gamePath);
                    string title = string.IsNullOrWhiteSpace(stfs.Metadata.TitleName) ? stfs.Metadata.DisplayName : stfs.Metadata.TitleName;
                    string titleId = stfs.Metadata.TitleIdHex;
                    string mediaId = stfs.Metadata.MediaIdHex;
                    Logger.Info<GameManager>($"STFS parsed - Title: '{title}', TitleID: {titleId}, MediaID: {mediaId}");
                    return new ParsedGameDetails
                    {
                        Title = title,
                        TitleId = titleId,
                        MediaId = mediaId
                    };
                }

                // XEX executables (XEX1, XEX2)
                case FileSignature.XEX1:
                case FileSignature.XEX2:
                {
                    Logger.Info<GameManager>($"Detected XEX file ({fileSignature}), parsing: {gamePath}");
                    XexFile xex = XexFile.Load(gamePath);
                    if (!xex.IsValid)
                    {
                        Logger.Warning<GameManager>($"XEX file is invalid or could not be parsed: {xex.ValidationError}");
                        return new ParsedGameDetails();
                    }

                    string title = "Not found";
                    string titleId = xex.TitleId;
                    string mediaId = xex.MediaId;
                    Logger.Info<GameManager>($"XEX parsed - Title: '{title}', TitleID: {titleId}, MediaID: {mediaId}");
                    return new ParsedGameDetails
                    {
                        Title = title,
                        TitleId = titleId,
                        MediaId = mediaId
                    };
                }

                // Disc images (ISO, XISO)
                case FileSignature.ISO:
                case FileSignature.XISO:
                {
                    Logger.Info<GameManager>($"Detected disc image ({fileSignature}), parsing: {gamePath}");
                    IsoFile iso = IsoFile.Load(gamePath);
                    if (!iso.IsValid || iso.XexFile == null)
                    {
                        Logger.Warning<GameManager>($"Disc image is invalid or default.xex could not be parsed: {iso.ValidationError}");
                        iso.Dispose();
                        return new ParsedGameDetails();
                    }

                    string title = Path.GetFileNameWithoutExtension(gamePath);
                    string titleId = iso.XexFile.TitleId;
                    string mediaId = iso.XexFile.MediaId;
                    Logger.Info<GameManager>($"Disc image parsed - Title: '{title}', TitleID: {titleId}, MediaID: {mediaId}");
                    iso.Dispose();
                    return new ParsedGameDetails
                    {
                        Title = title,
                        TitleId = titleId,
                        MediaId = mediaId
                    };
                }

                // Unsupported file types
                case FileSignature.ZAR:
                {
                    Logger.Warning<GameManager>($"ZAR archives are not supported: {gamePath}");
                    return new ParsedGameDetails();
                }

                default:
                {
                    Logger.Warning<GameManager>($"Unrecognized file type: {fileSignature}");
                    return new ParsedGameDetails();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<GameManager>($"Error parsing game file: {ex.Message}");
            Logger.LogExceptionDetails<GameManager>(ex);
            return new ParsedGameDetails();
        }
    }

    /// <summary>
    /// Retrieves game details by launching Xenia emulator with the specified game and parsing the window title and log file
    /// Uses XeniaOutputHandler to extract game details from process output
    /// Falls back to log file parsing if XeniaOutputHandler fails to find details
    /// </summary>
    /// <param name="gamePath">The path to the game file to analyze</param>
    /// <param name="version">The Xenia version to use for launching the game</param>
    /// <returns>Parsed game details extracted from Xenia</returns>
    public static async Task<ParsedGameDetails> GetGameDetailsWithXenia(string gamePath, XeniaVersion version)
    {
        Logger.Info<GameManager>($"Starting to retrieve game details with Xenia for game: {gamePath}, version: {version}");

        // Grab info about the selected Xenia version
        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
        Logger.Debug<GameManager>($"Retrieved Xenia version info - Executable: {versionInfo.ExecutableLocation}, Emulator Dir: {versionInfo.EmulatorDir}");

        // Launch the game with Xenia to fetch details using XeniaOutputHandler
        Process xenia = new Process();
        xenia.StartInfo.FileName = AppPathResolver.GetFullPath(versionInfo.ExecutableLocation);
        xenia.StartInfo.WorkingDirectory = AppPathResolver.GetFullPath(versionInfo.EmulatorDir);
        xenia.StartInfo.Arguments = $@"""{gamePath}""";
        xenia.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        Logger.Trace<GameManager>($"Setting up process - Executable: {xenia.StartInfo.FileName}, Working Directory: {xenia.StartInfo.WorkingDirectory}, Arguments: {xenia.StartInfo.Arguments}");

        // Initialize XeniaOutputHandler for reading game details
        XeniaOutputHandler outputHandler = new XeniaOutputHandler(null, true);
        outputHandler.ConfigureProcess(xenia);

        xenia.Start();
        Logger.Info<GameManager>($"Started Xenia process for game: {gamePath} with PID: {xenia.Id}");

        xenia.WaitForInputIdle();

        // Start capturing output
        outputHandler.StartCapture(xenia);

        // Wait for the output handler to extract all game details (title, title ID, and media ID)
        int numberOfTries = 0;
        const int maxTries = 150; // 15 seconds (150 * 100ms)

        Logger.Debug<GameManager>("Starting to extract game details from Xenia output");
        while (numberOfTries < maxTries)
        {
            Logger.Trace<GameManager>($"Attempt {numberOfTries}: Extracted Title: '{outputHandler.GameDetails.Title}', ID: '{outputHandler.GameDetails.TitleId}', Media ID: '{outputHandler.GameDetails.MediaId}'");

            // Check if all details have been extracted
            if (outputHandler.GameDetails.Title != "Not found" &&
                outputHandler.GameDetails.TitleId != "00000000" &&
                outputHandler.GameDetails.MediaId != "00000000")
            {
                Logger.Debug<GameManager>($"All game details extracted successfully");
                break;
            }

            numberOfTries++;
            await Task.Delay(100); // Delay between repeats to ensure everything loads
        }

        // Stop capturing and get the results
        outputHandler.StopCapture(xenia);

        // Extract the results from XeniaOutputHandler
        ParsedGameDetails details = outputHandler.GameDetails;

        Logger.Debug<GameManager>($"Completed output extraction. Found Title: '{details.Title}', ID: '{details.TitleId}', Media ID: '{details.MediaId}', Attempts: {numberOfTries}");

        Logger.Info<GameManager>($"Killing Xenia process with PID: {xenia.Id}");
        xenia.Kill(); // Force to close Xenia

        // Fallback - Using Xenia.log to fill in any missing details
        if (details.Title == "Not found" || details.TitleId == "00000000" || details.MediaId == "00000000")
        {
            Logger.Warning<GameManager>($"XeniaOutputHandler missed some details (Title: {details.Title != "Not found"}, TitleId: {details.TitleId != "00000000"}, MediaId: {details.MediaId != "00000000"}), falling back to log file parsing");
            string logFilePath = Path.Combine(xenia.StartInfo.WorkingDirectory, "xenia.log");
            if (File.Exists(logFilePath))
            {
                Logger.Debug<GameManager>($"Found xenia.log file at: {logFilePath}, starting to parse for missing game details");

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
                            if (details.Title == "Not found" && split.Length > 1 && !string.IsNullOrWhiteSpace(split[1]))
                            {
                                details.Title = split[1].TrimStart();
                                Logger.Debug<GameManager>($"Extracted title name from log: '{details.Title}' at line {linesProcessed}");
                            }

                            break;
                        }
                        case var _ when line.ToLower().Contains("title id"):
                        {
                            string[] split = line.Split(':');
                            if (details.TitleId == "00000000" && split.Length > 1 && !string.IsNullOrWhiteSpace(split[1]))
                            {
                                string titleId = split[1].TrimStart();
                                // Validate that Title ID is a valid hex value
                                if (ulong.TryParse(titleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                                {
                                    details.TitleId = titleId;
                                    Logger.Debug<GameManager>($"Extracted valid title ID from log: '{details.TitleId}' at line {linesProcessed}");
                                }
                                else
                                {
                                    Logger.Warning<GameManager>($"Invalid title ID format in log: '{titleId}' at line {linesProcessed}");
                                }
                            }

                            break;
                        }
                        case var _ when line.ToLower().Contains("media id"):
                        {
                            string[] split = line.Split(':');
                            if (details.MediaId == "00000000" && split.Length > 1 && !string.IsNullOrWhiteSpace(split[1]))
                            {
                                string mediaId = split[1].TrimStart();
                                // Validate that Media ID is a valid hex value
                                if (ulong.TryParse(mediaId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                                {
                                    details.MediaId = mediaId;
                                    Logger.Debug<GameManager>($"Extracted valid media ID from log: '{details.MediaId}' at line {linesProcessed}");
                                }
                                else
                                {
                                    Logger.Warning<GameManager>($"Invalid media ID format in log: '{mediaId}' at line {linesProcessed}");
                                }
                            }
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
        }

        // If game details were not found, use "foldername\filename" as fallback
        if (details.Title == "Not found")
        {
            Logger.Warning<GameManager>($"Could not extract game title from Xenia, using fallback method. Game path: {gamePath}");
            string? directoryName = Path.GetFileName(Path.GetDirectoryName(gamePath));
            string fileName = Path.GetFileNameWithoutExtension(gamePath);
            details.Title = $"{directoryName}\\{fileName}";
            Logger.Debug<GameManager>($"Applied fallback game title: '{details.Title}'");
        }

        Logger.Info<GameManager>($"Successfully retrieved game details - Title: '{details.Title}', ID: '{details.TitleId}', Media ID: '{details.MediaId}'");

        // Return what has been found
        return details;
    }

    /// <summary>
    /// Adds a new game to the library by fetching detailed information from the Xbox database,
    /// downloading artwork, and creating necessary configuration files.
    /// </summary>
    /// <param name="xeniaVersion">The Xenia version to associate with this game.</param>
    /// <param name="selectedGame">The game information containing alternative IDs.</param>
    /// <param name="gamePath">The file path to the game.</param>
    /// <param name="details">The parsed game details (title, title ID, media ID).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when game information cannot be fetched from the database.</exception>
    public static async Task AddGame(XeniaVersion xeniaVersion, GameInfo selectedGame, string gamePath, ParsedGameDetails details)
    {
        // Use GameInfo's Id if titleId is "00000000"
        string actualTitleId = details.TitleId == "00000000" ? selectedGame.Id ?? details.TitleId : details.TitleId;

        Logger.Trace<GameManager>($"Starting AddGame operation - TitleId: {actualTitleId}, MediaId: {details.MediaId}, XeniaVersion: {xeniaVersion}, GamePath: {gamePath}");

        // Grab full game information
        Logger.Info<GameManager>($"Fetching detailed game information from Xbox database for TitleId: {actualTitleId}");
        GameDetailedInfo? detailedGameInfo = await XboxDatabase.GetFullGameInfo(actualTitleId);
        if (detailedGameInfo == null)
        {
            Logger.Error<GameManager>($"Failed to fetch game information for TitleId: {actualTitleId} - database returned null");
            // TODO: Throw an exception couldn't fetch game information
            throw new Exception("Couldn't fetch game information");
        }

        Logger.Info<GameManager>($"Successfully retrieved game information - Title: '{detailedGameInfo.Title?.Full}'");
        Logger.Debug<GameManager>($"Processing game title - Original: '{detailedGameInfo.Title?.Full}', Sanitized: '{detailedGameInfo.Title?.Full?.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ')}'");

        // Create a new game entry
        Game newGame = new Game
        {
            Title = detailedGameInfo.Title?.Full?.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ') ?? details.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' '),
            GameId = actualTitleId,
            AlternativeIDs = selectedGame.AlternativeId!,
            MediaId = details.MediaId,
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
        newGame.FileLocations.Config = Path.Combine(XeniaVersionInfo.GetXeniaVersionInfo(xeniaVersion).ConfigFolderLocation, $"{newGame.Title}.config.toml");
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
        string boxartSavePath = Path.Combine(AppPaths.GameDataDirectory, newGame.Title, "Artwork", "Boxart.png");
        await XboxDatabase.DownloadArtworkAsync(downloadManager, detailedGameInfo.Artwork?.Boxart, actualTitleId, "boxart.jpg", savePath: boxartSavePath);

        // Check if remote download succeeded, if not, use local default
        if (!File.Exists(boxartSavePath))
        {
            Logger.Warning<GameManager>($"Remote boxart download failed, using local default artwork");
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Boxart.jpg", boxartSavePath, SKEncodedImageFormat.Png);
            Logger.Info<GameManager>($"Default local boxart applied successfully");
        }

        newGame.Artwork.Boxart = Path.Combine("GameData", newGame.Title, "Artwork", "Boxart.png");
        Logger.Debug<GameManager>($"Boxart artwork path set: {newGame.Artwork.Boxart}");

        // Icon
        Logger.Info<GameManager>($"Starting icon download process for game: '{newGame.Title}'");
        string iconSavePath = Path.Combine(AppPaths.GameDataDirectory, newGame.Title, "Artwork", "Icon.ico");
        await XboxDatabase.DownloadArtworkAsync(downloadManager, detailedGameInfo.Artwork?.Icon, actualTitleId, "icon.png", savePath: iconSavePath);

        // Check if remote download succeeded, if not, use local default
        if (!File.Exists(iconSavePath))
        {
            Logger.Warning<GameManager>($"Remote icon download failed, using local default icon");
            ArtworkManager.LocalArtworkAsIcon("XeniaManager.Core.Assets.Artwork.Icon.png", iconSavePath);
            Logger.Info<GameManager>($"Default local icon applied successfully");
        }

        newGame.Artwork.Icon = Path.Combine("GameData", newGame.Title, "Artwork", "Icon.ico");
        Logger.Debug<GameManager>($"Icon artwork path set: {newGame.Artwork.Icon}");

        // Background
        Logger.Info<GameManager>($"Starting background download process for game: '{newGame.Title}'");
        string backgroundSavePath = Path.Combine(AppPaths.GameDataDirectory, newGame.Title!, "Artwork", "Background.jpg");
        await XboxDatabase.DownloadArtworkAsync(downloadManager, detailedGameInfo.Artwork?.Background, actualTitleId, "background.jpg", savePath: backgroundSavePath);

        // Check if remote download succeeded, if not, use local default
        if (!File.Exists(backgroundSavePath))
        {
            Logger.Warning<GameManager>($"Remote background download failed, using local default background");
            ArtworkManager.LocalArtwork("XeniaManager.Core.Assets.Artwork.Background.jpg", backgroundSavePath, SKEncodedImageFormat.Jpeg);
            Logger.Info<GameManager>($"Default local background applied successfully");
        }

        newGame.Artwork.Background = Path.Combine("GameData", newGame.Title, "Artwork", "Background.jpg");
        Logger.Debug<GameManager>($"Background artwork path set: {newGame.Artwork.Background}");

        // Add the new game to the library
        Logger.Info<GameManager>($"Adding game '{newGame.Title}' ({newGame.GameId}) to the game library");
        Games.Add(newGame);
        Logger.Debug<GameManager>($"Game added to in-memory library. Total games in library: {Games.Count}");

        // Save the library to persist changes
        Logger.Info<GameManager>($"Saving game library to persist changes");
        SaveLibrary();
        Logger.Info<GameManager>($"Game library saved successfully");

        // Notify listeners that the game library has changed
        Logger.Debug<GameManager>($"Notifying listeners of game library change");
        EventManager.Instance.OnGameLibraryChanged();

        Logger.Info<GameManager>($"AddGame operation completed successfully - Title: '{newGame.Title}', GameId: {newGame.GameId}");
        Logger.Trace<GameManager>("AddGame operation finished");
    }

    /// <summary>
    /// Adds a new game to the library without fetching detailed information from the Xbox database.
    /// Uses default artwork and creates necessary configuration files.
    /// This method is typically used for games not found in the Xbox database or for custom game entries.
    /// </summary>
    /// <param name="xeniaVersion">The Xenia version to associate with this game.</param>
    /// <param name="details">The parsed game details (title, title ID, media ID).</param>
    /// <param name="gamePath">The file path to the game.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task AddUnknownGame(XeniaVersion xeniaVersion, ParsedGameDetails details, string gamePath)
    {
        Logger.Trace<GameManager>($"Starting AddUnknownGame operation - Title: '{details.Title}', TitleId: {details.TitleId}, MediaId: {details.MediaId}, XeniaVersion: {xeniaVersion}, GamePath: {gamePath}");

        Logger.Info<GameManager>($"Creating new game entry for unknown game: '{details.Title}' (TitleId: {details.TitleId})");
        string sanitizedTitle = details.Title != "Not found"
            ? details.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ')
            : Path.GetFileNameWithoutExtension(gamePath).Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
        Logger.Debug<GameManager>($"Processing game title - Original: '{details.Title}', Sanitized: '{sanitizedTitle}'");

        // Create a new game entry

        Game newGame = new Game
        {
            Title = sanitizedTitle,
            GameId = details.TitleId,
            MediaId = details.MediaId,
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
        newGame.FileLocations.Config = Path.Combine(XeniaVersionInfo.GetXeniaVersionInfo(xeniaVersion).ConfigFolderLocation, $"{newGame.Title}.config.toml");
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

        // Notify listeners that the game library has changed
        Logger.Debug<GameManager>($"Notifying listeners of game library change");
        EventManager.Instance.OnGameLibraryChanged();

        Logger.Info<GameManager>($"AddUnknownGame operation completed successfully - Title: '{newGame.Title}', GameId: {newGame.GameId}");
        Logger.Trace<GameManager>("AddUnknownGame operation finished");
    }

    /// <summary>
    /// Checks if a game with the specified file path already exists in the library.
    /// </summary>
    /// <param name="gamePath">The file path to check for duplicates.</param>
    /// <returns>True if a game with the same file path exists, false otherwise.</returns>
    public static bool IsDuplicateGame(string gamePath)
    {
        bool isDuplicate = Games.Any(game => game.FileLocations.Game == gamePath);
        if (isDuplicate)
        {
            Logger.Debug<GameManager>($"Duplicate game detected for path: {gamePath}");
        }
        return isDuplicate;
    }

    /// <summary>
    /// Scans a directory and all subdirectories to discover compatible game files.
    /// Scans the entire directory tree for all game files (.iso, .xiso, .zar, .xex, STFS).
    /// - Discovers all compatible game files in each directory
    /// - Skips STFS files that are Installer or MarketplaceContent types
    /// - For XEX files: finds all XEX files in a directory, then stops subdirectory scanning (unless there is a "content" folder, that folder will be scanned as well)
    /// - For other types (ISO, ZAR, STFS): continues scanning subdirectories
    /// Supported file types:
    /// 1. ISO files (.iso, .xiso)
    /// 2. ZAR archives (.zar) - only if scanZarFiles is true
    /// 3. XEX files (.xex) - Finding any XEX stops subdirectory scanning (unless there is a "content" folder, that folder will be scanned as well)
    /// 4. STFS files (CON, LIVE, PIRS - detected by header)
    /// </summary>
    /// <param name="directoryPath">The root directory to scan for games.</param>
    /// <param name="scanZarFiles">Whether to scan for .zar files. Defaults to true.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the scan operation.</param>
    /// <param name="progressReporter">
    /// Optional progress reporter callback: (statusMessage, currentDirectory, directoriesScanned, gameFilesFound, progressPercentage)
    /// </param>
    /// <returns>A list of all game file paths found in the directory tree.</returns>
    public static List<string> DiscoverGameFiles(string directoryPath, bool scanZarFiles = true,
        CancellationToken cancellationToken = default, Action<string, string, int, int, int>? progressReporter = null)
    {
        List<string> gameFiles = [];

        // Use a queue for breadth-first traversal
        Queue<string> directoriesToScan = new Queue<string>();
        directoriesToScan.Enqueue(directoryPath);

        int directoriesScanned = 0;
        int estimatedTotalDirectories = 1; // Start with the root directory estimate

        Logger.Debug<GameManager>($"Starting directory traversal from: {directoryPath}");
        progressReporter?.Invoke(
            LocalizationHelper.GetText("FolderScanProgressDialog.Status.Starting"),
            directoryPath,
            0,
            0,
            0);

        while (directoriesToScan.Count > 0)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            string currentDirectory = directoriesToScan.Dequeue();
            directoriesScanned++;

            Logger.Trace<GameManager>($"Scanning directory: {currentDirectory}");

            // Report progress
            progressReporter?.Invoke(
                string.Format(LocalizationHelper.GetText("FolderScanProgressDialog.Status.Scanning"), Path.GetFileName(currentDirectory)),
                currentDirectory,
                directoriesScanned,
                gameFiles.Count,
                Math.Min(100, (directoriesScanned * 100) / Math.Max(1, estimatedTotalDirectories)));

            bool xexFound = false;

            // Find all ISO files
            foreach (string extension in new[] { ".iso", ".xiso" })
            {
                string[] isoFiles = Directory.GetFiles(currentDirectory, $"*{extension}", SearchOption.TopDirectoryOnly);
                foreach (string isoFile in isoFiles)
                {
                    Logger.Trace<GameManager>($"Found ISO file: {isoFile}");
                    gameFiles.Add(isoFile);
                }
            }

            // Find all ZAR files (only if scanZarFiles is enabled)
            if (scanZarFiles)
            {
                string[] zarFiles = Directory.GetFiles(currentDirectory, "*.zar", SearchOption.TopDirectoryOnly);
                foreach (string zarFile in zarFiles)
                {
                    Logger.Trace<GameManager>($"Found ZAR file: {zarFile}");
                    gameFiles.Add(zarFile);
                }
            }

            // Find all XEX files
            // First check for default.xex (case-insensitive), then fall back to any .xex files
            string? defaultXex = Directory.GetFiles(currentDirectory, "default.xex", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => f.Equals(Path.Combine(currentDirectory, "default.xex"), StringComparison.OrdinalIgnoreCase));

            string[] xexFiles;
            if (!string.IsNullOrEmpty(defaultXex))
            {
                // Prefer default.xex if it exists
                xexFiles = [defaultXex];
                Logger.Trace<GameManager>($"Found default.xex file: {defaultXex}");
            }
            else
            {
                // Fall back to finding all .xex files
                xexFiles = Directory.GetFiles(currentDirectory, "*.xex", SearchOption.TopDirectoryOnly);
            }

            if (xexFiles.Length > 0)
            {
                foreach (string xexFile in xexFiles)
                {
                    Logger.Trace<GameManager>($"Found XEX file: {xexFile}");
                    gameFiles.Add(xexFile);
                }
                xexFound = true;
            }

            // Find all STFS files (CON, LIVE, PIRS)
            foreach (string file in Directory.GetFiles(currentDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                // Skip if already identified as ISO, ZAR, or XEX
                string extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension is ".iso" or ".xiso" or ".zar" or ".xex")
                {
                    continue;
                }

                try
                {
                    FileSignature signature = FileIdentifier.IdentifyFileType(file);
                    if (signature is FileSignature.CON or FileSignature.LIVE or FileSignature.PIRS)
                    {
                        // Check if the STFS file is a valid game type (not Installer or MarketplaceContent)
                        if (IsValidStfsGameFile(file))
                        {
                            Logger.Trace<GameManager>($"Found STFS file ({signature}): {file}");
                            gameFiles.Add(file);
                        }
                        else
                        {
                            Logger.Trace<GameManager>($"Skipping STFS file (Installer/MarketplaceContent): {file}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Trace<GameManager>($"Failed to identify file {file}: {ex.Message}");
                }
            }

            // Log how many game files were found in this directory
            if (gameFiles.Any(f => Path.GetDirectoryName(f) == currentDirectory))
            {
                Logger.Debug<GameManager>($"Added {gameFiles.Count(f => Path.GetDirectoryName(f) == currentDirectory)} game file(s) from {currentDirectory}");
            }
            else
            {
                Logger.Trace<GameManager>($"No supported game file found in directory: {currentDirectory}");
            }

            // Add subdirectories to the queue for scanning
            // If XEX files found, only scan "content" subdirectory (for DLC/additional content)
            if (xexFound)
            {
                try
                {
                    string? contentDirectory = Directory.GetDirectories(currentDirectory, "content", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(d => d.Equals(Path.Combine(currentDirectory, "content"), StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(contentDirectory))
                    {
                        directoriesToScan.Enqueue(contentDirectory);
                        estimatedTotalDirectories++; // Update estimate
                        Logger.Trace<GameManager>($"XEX file(s) found in {currentDirectory}, scanning 'content' subdirectory for DLC");
                    }
                    else
                    {
                        Logger.Trace<GameManager>($"XEX file(s) found in {currentDirectory}, no 'content' subdirectory found");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Logger.Warning<GameManager>($"Access denied to 'content' subdirectory of {currentDirectory}");
                    Logger.LogExceptionDetails<GameManager>(ex);
                }
                catch (Exception ex)
                {
                    Logger.Warning<GameManager>($"Failed to enumerate 'content' subdirectory of {currentDirectory}");
                    Logger.LogExceptionDetails<GameManager>(ex);
                }
            }
            else
            {
                // No XEX found, scan all subdirectories
                try
                {
                    string[] subDirectories = Directory.GetDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly);
                    foreach (string subDirectory in subDirectories)
                    {
                        directoriesToScan.Enqueue(subDirectory);
                    }
                    estimatedTotalDirectories += subDirectories.Length; // Update estimate
                    Logger.Trace<GameManager>($"Queued {subDirectories.Length} subdirectories for scanning");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Logger.Warning<GameManager>($"Access denied to subdirectories of {currentDirectory}");
                    Logger.LogExceptionDetails<GameManager>(ex);
                }
                catch (Exception ex)
                {
                    Logger.Warning<GameManager>($"Failed to enumerate subdirectories of {currentDirectory}");
                    Logger.LogExceptionDetails<GameManager>(ex);
                }
            }
        }

        // Report completion
        progressReporter?.Invoke(string.Format(LocalizationHelper.GetText("FolderScanProgressDialog.Status.Complete"), gameFiles.Count),
            directoryPath, directoriesScanned, gameFiles.Count, 100);

        Logger.Info<GameManager>($"Directory traversal complete. Found {gameFiles.Count} game files.");
        return gameFiles;
    }

    /// <summary>
    /// Determines if an STFS file is a valid game file by checking its content type.
    /// Filters out Installer and MarketplaceContent packages which are not standalone games.
    /// </summary>
    /// <param name="filePath">The path to the STFS file to check.</param>
    /// <returns>True if the file is a valid game type, false otherwise.</returns>
    private static bool IsValidStfsGameFile(string filePath)
    {
        try
        {
            using StfsFile stfs = StfsFile.Load(filePath);
            ContentType contentType = stfs.Metadata.ContentType;

            // Only accept Xbox360Title/Arcade Title/Demo/GOD
            if (contentType is ContentType.Xbox360Title or ContentType.ArcadeTitle or ContentType.GameDemo or ContentType.GameOnDemand)
            {
                Logger.Trace<GameManager>($"STFS file has valid content type: {contentType}");
                return true;
            }

            // Others are considered either DLC, Title Update or not launchable
            Logger.Trace<GameManager>($"STFS file has excluded content type: {contentType}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Trace<GameManager>($"Failed to check STFS content type for {filePath}: {ex.Message}");
            // If we can't determine the content type, assume it's not a valid game file
            return false;
        }
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

        // Notify listeners that the game library has changed
        Logger.Debug<GameManager>($"Notifying listeners of game library change");
        EventManager.Instance.OnGameLibraryChanged();

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
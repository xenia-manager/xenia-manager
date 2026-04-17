using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Handles capturing and processing output from the Xenia emulator process by reading its log file.
/// Detects and tracks loaded gamer profiles and game loading state.
/// </summary>
public partial class XeniaOutputHandler
{
    private readonly Game? _game;
    private readonly XeniaVersion? _xeniaVersion;
    private readonly List<AccountInfo> _loadedProfiles;
    private readonly Dictionary<int, AccountInfo> _slotToProfile;
    private readonly Dictionary<ulong, AccountInfo> _xuidToProfile;
    private readonly Lock _lock = new Lock();
    private bool _isGameLoading;
    private Stopwatch? _gameLoadStopwatch;
    private readonly bool _readingGameDetails;
    private readonly TimeSpan _gameLoadTimeout;

    // Game details extracted from output
    private readonly ParsedGameDetails _gameDetails;

    // Log file reading state
    private CancellationTokenSource? _logReaderCts;
    private Task? _logReaderTask;

    /// <summary>
    /// Default timeout for game loading detection (30 seconds)
    /// </summary>
    private static readonly TimeSpan DefaultGameLoadTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Regex pattern to detect loaded gamer profiles
    /// Matches: Loaded Gamertag (GUID: XUID) to slot 0-4
    /// Case-insensitive to handle both uppercase and lowercase hex
    /// </summary>
    [GeneratedRegex(@"\bLoaded\s(?<Gamertag>\w+)\s\(GUID:\s(?<GUID>[A-F0-9]+)\)\sto\sslot\s(?<Slot>[0-4])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GamerProfilesRegex();

    /// <summary>
    /// Regex pattern to detect profile XUID from [Profiles] section
    /// Matches: logged_profile_slot_0-4_xuid = "XUID"
    /// Case-insensitive to handle both uppercase and lowercase hex
    /// </summary>
    [GeneratedRegex(@"logged_profile_slot_([0-4])_xuid\s*=\s*""(?<XUID>[A-F0-9]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProfileSlotRegex();

    /// <summary>
    /// Regex pattern to detect game title from Xenia output
    /// Matches: Title name: GameTitle
    /// </summary>
    [GeneratedRegex(@"Title name:\s*(?<Title>.+)", RegexOptions.CultureInvariant)]
    private static partial Regex TitleNameRegex();

    /// <summary>
    /// Event raised when the game starts loading
    /// </summary>
    public event EventHandler? GameLoadingStarted;

    /// <summary>
    /// Gets the list of loaded gamer profiles
    /// </summary>
    public IReadOnlyList<AccountInfo> LoadedProfiles
    {
        get
        {
            lock (_lock)
            {
                return _loadedProfiles.AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the XeniaOutputHandler class
    /// </summary>
    /// <param name="game">The game being launched (null when reading game details)</param>
    /// <param name="readingGameDetails">True when only reading game details, not tracking profiles</param>
    public XeniaOutputHandler(Game? game, bool readingGameDetails = false)
    {
        _game = game;
        _xeniaVersion = game?.XeniaVersion;
        _loadedProfiles = [];
        _slotToProfile = new Dictionary<int, AccountInfo>();
        _xuidToProfile = new Dictionary<ulong, AccountInfo>();
        _readingGameDetails = readingGameDetails;
        _gameDetails = new ParsedGameDetails();
        _gameLoadTimeout = DefaultGameLoadTimeout;
    }

    /// <summary>
    /// Gets the extracted game details
    /// </summary>
    public ParsedGameDetails GameDetails => _gameDetails;

    /// <summary>
    /// Configures a process for Xenia launch.
    /// Output is captured from Xenia's log file rather than redirected stdout/stderr.
    /// <para>
    /// This is to fix an issue where if Console is enabled, it'll redirect everything and nothing will be parsed
    /// </para>
    /// </summary>
    /// <param name="process">The Xenia process to configure</param>
    public void ConfigureProcess(Process process)
    {
        // No stdout/stderr redirection - we read from the log file instead
        process.EnableRaisingEvents = true;
        process.Exited += OnProcessExited;
    }

    /// <summary>
    /// Starts capturing output from Xenia's log file
    /// </summary>
    /// <param name="process">The Xenia process</param>
    public void StartCapture(Process process)
    {
        lock (_lock)
        {
            _isGameLoading = false;
            _gameLoadStopwatch = Stopwatch.StartNew();
            _loadedProfiles.Clear();
            _slotToProfile.Clear();
            _xuidToProfile.Clear();
        }

        Logger.Info<XeniaOutputHandler>($"Starting log file capture for {_game?.Title}");

        // Start reading the log file asynchronously
        _logReaderCts = new CancellationTokenSource();
        _logReaderTask = StartLogFileReaderAsync(process, _logReaderCts.Token);
    }

    /// <summary>
    /// Stops capturing output from Xenia's log file
    /// </summary>
    /// <param name="process">The Xenia process</param>
    public void StopCapture(Process process)
    {
        _logReaderCts?.Cancel();
        try
        {
            _logReaderTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Ignore cancellation exceptions
        }
        _logReaderCts?.Dispose();
        _logReaderCts = null;
        _logReaderTask = null;

        process.Exited -= OnProcessExited;

        lock (_lock)
        {
            Logger.Info<XeniaOutputHandler>(_readingGameDetails
                ? $"Stopped game details extraction. Title: '{_gameDetails.Title}', Title ID: '{_gameDetails.TitleId}', Media ID: '{_gameDetails.MediaId}'"
                : $"Stopped output capture for {_game?.Title}. Profiles loaded: {_loadedProfiles.Count}");
        }
    }

    /// <summary>
    /// Reads the Xenia log file continuously as it grows, processing new lines
    /// Uses exponential backoff polling to minimize CPU usage
    /// </summary>
    private async Task StartLogFileReaderAsync(Process process, CancellationToken cancellationToken)
    {
        string logFilePath = GetLogFilePath(process);
        if (string.IsNullOrEmpty(logFilePath))
        {
            Logger.Warning<XeniaOutputHandler>("Could not determine log file path, output capture disabled");
            return;
        }

        Logger.Debug<XeniaOutputHandler>($"Watching log file: {logFilePath}");

        // Wait for log file to be created (with exponential backoff)
        int waitAttempts = 0;
        int waitMs = 100;
        while (!File.Exists(logFilePath) && waitAttempts < 50 && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(waitMs, cancellationToken);
            waitAttempts++;
            waitMs = Math.Min(waitMs * 2, 1000);
        }

        if (!File.Exists(logFilePath))
        {
            Logger.Warning<XeniaOutputHandler>($"Log file not found at: {logFilePath}");
            return;
        }

        // Read the log file as it grows
        try
        {
            await using FileStream fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 8192, useAsync: true);
            using StreamReader reader = new StreamReader(fileStream);

            // Adaptive polling: start fast, slow down when idle
            const int fastPollMs = 20;
            const int slowPollMs = 200;
            int pollDelay = fastPollMs;

            while (!cancellationToken.IsCancellationRequested && !process.HasExited)
            {
                string? line;
                bool sawNewData = false;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    ProcessLine(line);
                    sawNewData = true;
                }

                // Check game load timeout on every poll tick, not just when lines arrive
                CheckGameLoadTimeout();

                if (sawNewData)
                {
                    pollDelay = fastPollMs;
                }
                else
                {
                    // No new data - increase polling interval (exponential backoff)
                    pollDelay = Math.Min(pollDelay * 2, slowPollMs);
                }

                await Task.Delay(pollDelay, cancellationToken);
            }

            // Read any remaining data after process exits
            while (await reader.ReadLineAsync(cancellationToken) is { } remainingLine)
            {
                ProcessLine(remainingLine);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaOutputHandler>($"Error reading log file: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the log file path from the Xenia version info or process working directory
    /// </summary>
    private string GetLogFilePath(Process process)
    {
        if (_xeniaVersion.HasValue && _xeniaVersion.Value != XeniaVersion.Custom)
        {
            try
            {
                XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(_xeniaVersion.Value);
                string fullPath = AppPathResolver.GetFullPath(versionInfo.LogLocation);
                return fullPath;
            }
            catch (Exception ex)
            {
                Logger.Warning<XeniaOutputHandler>($"Failed to get log location from XeniaVersionInfo");
                Logger.LogExceptionDetails<XeniaOutputHandler>(ex);
            }
        }

        // Fallback to working directory
        if (!string.IsNullOrEmpty(process.StartInfo.WorkingDirectory))
        {
            return Path.Combine(process.StartInfo.WorkingDirectory, "xenia.log");
        }

        return string.Empty;
    }

    /// <summary>
    /// Processes a line of output from Xenia
    /// </summary>
    /// <param name="line">The output line to process</param>
    private void ProcessLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        // Trace-log every line (disabled by default to avoid log spam during gameplay)
        // Uncomment the line below to restore full trace logging of all Xenia output
        // Logger.Trace<XeniaOutputHandler>($"[Xenia] {line}");

        // Extract game details if we're reading them
        if (_readingGameDetails)
        {
            ExtractGameDetails(line);
        }

        // Check for game loading events (only if the game hasn't started loading yet)
        if (!_isGameLoading)
        {
            CheckForGameLoad(line);
        }

        // Check for profile load events
        CheckForProfileLoad(line);
    }

    /// <summary>
    /// Extracts game title, title ID, and media ID from Xenia output
    /// </summary>
    /// <param name="line">The line to check</param>
    private void ExtractGameDetails(string line)
    {
        ReadOnlySpan<char> lineSpan = line.AsSpan();

        // Fast pre-filter
        if (lineSpan.IndexOf("Title", StringComparison.Ordinal) == -1 &&
            lineSpan.IndexOf("Media", StringComparison.Ordinal) == -1)
        {
            return;
        }

        // Extract title name
        if (lineSpan.IndexOf("Title name", StringComparison.Ordinal) >= 0)
        {
            Match match = TitleNameRegex().Match(line);
            if (match.Success)
            {
                _gameDetails.Title = match.Groups["Title"].Value.Trim();
                Logger.Debug<XeniaOutputHandler>($"Extracted title name: '{_gameDetails.Title}'");
            }
        }

        // Extract title ID
        if (lineSpan.IndexOf("Title ID", StringComparison.Ordinal) is int titleIdIdx and >= 0)
        {
            int colonIdx = lineSpan[titleIdIdx..].IndexOf(':');
            if (colonIdx != -1)
            {
                ReadOnlySpan<char> titleId = lineSpan[(titleIdIdx + colonIdx + 1)..].Trim();
                // Validate that Title ID is a valid hex value
                if (ulong.TryParse(titleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                {
                    _gameDetails.TitleId = titleId.ToString();
                    Logger.Debug<XeniaOutputHandler>($"Extracted valid title ID: '{_gameDetails.TitleId}'");
                }
                else
                {
                    Logger.Warning<XeniaOutputHandler>($"Invalid title ID format in output: '{titleId.ToString()}'");
                }
            }
        }

        // Extract media ID
        if (lineSpan.IndexOf("Media ID", StringComparison.Ordinal) is int mediaIdIdx and >= 0)
        {
            int colonIdx = lineSpan[mediaIdIdx..].IndexOf(':');
            if (colonIdx != -1)
            {
                ReadOnlySpan<char> mediaId = lineSpan[(mediaIdIdx + colonIdx + 1)..].Trim();
                // Validate that Media ID is a valid hex value
                if (ulong.TryParse(mediaId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                {
                    _gameDetails.MediaId = mediaId.ToString();
                    Logger.Debug<XeniaOutputHandler>($"Extracted valid media ID: '{_gameDetails.MediaId}'");
                }
                else
                {
                    Logger.Warning<XeniaOutputHandler>($"Invalid media ID format in output: '{mediaId.ToString()}'");
                }
            }
        }
    }

    /// <summary>
    /// Checks if the game load timeout has elapsed and fires the event if so.
    /// Called on every poll tick to ensure the timeout fires promptly regardless
    /// of whether new log output has arrived.
    /// </summary>
    private void CheckGameLoadTimeout()
    {
        if (_isGameLoading)
        {
            return;
        }

        Stopwatch? stopwatch;
        lock (_lock)
        {
            stopwatch = _gameLoadStopwatch;
        }

        if (stopwatch == null || stopwatch.Elapsed < _gameLoadTimeout)
        {
            return;
        }

        lock (_lock)
        {
            if (!_isGameLoading)
            {
                _isGameLoading = true;
                Logger.Info<XeniaOutputHandler>($"Game marked as loaded after {_gameLoadTimeout.TotalSeconds} seconds");
                GameLoadingStarted?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Checks if a line indicates the game is loading and extracts the game title.
    /// For timeout-based detection, see <see cref="CheckGameLoadTimeout"/>.
    /// </summary>
    /// <param name="line">The line to check</param>
    private void CheckForGameLoad(string line)
    {
        // Fast pre-filter: skip lines that don't contain "Title name"
        if (!line.Contains("Title name", StringComparison.Ordinal))
        {
            return;
        }

        Match match = TitleNameRegex().Match(line);
        if (!match.Success)
        {
            return;
        }

        string gameTitle = match.Groups["Title"].Value.Trim();

        lock (_lock)
        {
            if (!_isGameLoading)
            {
                _isGameLoading = true;
                Logger.Info<XeniaOutputHandler>($"Game loading detected: {gameTitle}");
                GameLoadingStarted?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Checks if a line contains a profile load event and extracts profile information
    /// Handles both the [Profiles] section (XUID only) and "Loaded" messages (full info)
    /// </summary>
    /// <param name="line">The line to check</param>
    private void CheckForProfileLoad(string line)
    {
        // Fast pre-filter: skip lines that don't contain relevant keywords
        if (!line.Contains("Loaded", StringComparison.OrdinalIgnoreCase) &&
            !line.Contains("logged_profile_slot", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // First, try to match the [Profiles] section format: logged_profile_slot_X_xuid = "XUID"
        Match slotMatch = ProfileSlotRegex().Match(line);
        if (slotMatch.Success)
        {
            string xuidString = slotMatch.Groups["XUID"].Value;
            int profileSlot = int.Parse(slotMatch.Groups[1].Value, CultureInfo.InvariantCulture);

            // Skip if XUID is null/empty or zero
            if (!string.IsNullOrEmpty(xuidString) &&
                ulong.TryParse(xuidString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong slotXuidValue) &&
                slotXuidValue != 0)
            {
                AddOrUpdateProfile(slotXuidValue, profileSlot, gamertag: null);
            }
            return;
        }

        // Second, try to match the full profile load format: "Loaded Gamertag (GUID: XUID) to slot X"
        Match match = GamerProfilesRegex().Match(line);
        if (!match.Success)
        {
            return;
        }

        string gamertag = match.Groups["Gamertag"].Value;
        string guid = match.Groups["GUID"].Value;
        int slot = int.Parse(match.Groups["Slot"].Value, CultureInfo.InvariantCulture);

        // Parse XUID from hex string
        if (!ulong.TryParse(guid, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong xuidValue))
        {
            Logger.Warning<XeniaOutputHandler>($"Failed to parse XUID '{guid}' for profile {gamertag}");
            return;
        }

        AddOrUpdateProfile(xuidValue, slot, gamertag);
    }

    /// <summary>
    /// Adds a new profile or updates an existing one with gamertag information.
    /// If a different profile was previously in the same slot, it will be removed.
    /// </summary>
    /// <param name="xuidValue">The XUID value</param>
    /// <param name="slot">The profile slot</param>
    /// <param name="gamertag">The gamertag (null if only XUID is known)</param>
    private void AddOrUpdateProfile(ulong xuidValue, int slot, string? gamertag)
    {
        lock (_lock)
        {
            // Check if there's already a profile in this slot - if so, remove it (profile switch detected)
            if (_slotToProfile.TryGetValue(slot, out AccountInfo? existingProfileInSlot))
            {
                // Only remove if it's a different XUID
                if (existingProfileInSlot.Xuid.Value != xuidValue)
                {
                    _loadedProfiles.Remove(existingProfileInSlot);
                    _slotToProfile.Remove(slot);
                    _xuidToProfile.Remove(existingProfileInSlot.Xuid.Value);
                    Logger.Info<XeniaOutputHandler>($"Removed profile from slot {slot}: {existingProfileInSlot.Gamertag} (XUID: {existingProfileInSlot.Xuid}) - profile switch detected");
                }
            }

            // Check if a profile with this XUID already exists (O(1) lookup)
            if (_xuidToProfile.TryGetValue(xuidValue, out AccountInfo? existingProfile))
            {
                // Update gamertag if it was missing before
                if (!string.IsNullOrEmpty(gamertag) && string.IsNullOrEmpty(existingProfile.Gamertag))
                {
                    existingProfile.Gamertag = gamertag;
                    Logger.Info<XeniaOutputHandler>($"Updated profile: {gamertag} (XUID: {xuidValue:X16}, Slot: {slot})");
                }
                else
                {
                    Logger.Debug<XeniaOutputHandler>($"Profile already tracked: {gamertag ?? existingProfile.Gamertag} (XUID: {xuidValue:X16})");
                }

                // Update slot mapping
                _slotToProfile[slot] = existingProfile;
            }
            else
            {
                // Create a new profile
                AccountInfo profile = new AccountInfo
                {
                    Gamertag = gamertag ?? string.Empty,
                    Xuid = new AccountXuid(xuidValue)
                };
                _loadedProfiles.Add(profile);
                _slotToProfile[slot] = profile;
                _xuidToProfile[xuidValue] = profile;
                Logger.Info<XeniaOutputHandler>($"Profile {(gamertag != null ? "loaded" : "detected")}: {(gamertag ?? "Unknown")} (XUID: {xuidValue:X16}, Slot: {slot})");
            }
        }
    }

    // Event handlers for process exit
    private void OnProcessExited(object? sender, EventArgs e)
    {
        Logger.Info<XeniaOutputHandler>($"Xenia process exited for {_game?.Title}");
        lock (_lock)
        {
            if (_loadedProfiles.Count > 0)
            {
                Logger.Info<XeniaOutputHandler>($"Total profiles loaded during session: {_loadedProfiles.Count}");
                foreach (AccountInfo profile in _loadedProfiles)
                {
                    Logger.Debug<XeniaOutputHandler>($"  - {profile.Gamertag} (XUID: {profile.Xuid})");
                }
            }
        }
    }
}
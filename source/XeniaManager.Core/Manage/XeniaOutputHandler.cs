using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Handles capturing and processing output from the Xenia emulator process
/// Detects and tracks loaded gamer profiles and game loading state
/// </summary>
public class XeniaOutputHandler
{
    private readonly Game _game;
    private readonly StringBuilder _outputBuffer;
    private readonly List<AccountInfo> _loadedProfiles;
    private readonly Lock _lock = new Lock();
    private bool _isGameLoading;
    private int _gameLoadCheckAttempts;

    /// <summary>
    /// Maximum number of attempts to check for the game to start loading
    /// <remarks>
    /// This is just a magic number and should probably be replaced with a stopwatch
    /// </remarks>
    /// </summary>
    private const int MaxGameLoadCheckAttempts = 3000;

    /// <summary>
    /// Regex pattern to detect loaded gamer profiles
    /// Matches: Loaded Gamertag (GUID: XUID) to slot 0-4
    /// Case-insensitive to handle both uppercase and lowercase hex
    /// </summary>
    private static readonly Regex _gamerProfilesRegex = new Regex(
        @"\bLoaded\s(?<Gamertag>\w+)\s\(GUID:\s(?<GUID>[A-F0-9]+)\)\sto\sslot\s(?<Slot>[0-4])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regex pattern to detect profile XUID from [Profiles] section
    /// Matches: logged_profile_slot_0-4_xuid = "XUID"
    /// Case-insensitive to handle both uppercase and lowercase hex
    /// </summary>
    private static readonly Regex _profileSlotRegex = new Regex(
        @"logged_profile_slot_([0-4])_xuid\s*=\s*""(?<XUID>[A-F0-9]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regex pattern to detect game title from Xenia output
    /// Matches: Title name: GameTitle
    /// </summary>
    private static readonly Regex _titleNameRegex = new Regex(
        @"Title name:\s*(?<Title>.+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

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

    public XeniaOutputHandler(Game game)
    {
        _game = game;
        _outputBuffer = new StringBuilder();
        _loadedProfiles = [];
    }

    /// <summary>
    /// Configures a process to redirect output for handling
    /// </summary>
    /// <param name="process">The Xenia process to configure</param>
    public void ConfigureProcess(Process process)
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = false;
        process.EnableRaisingEvents = true;

        process.OutputDataReceived += OnOutputDataReceived;
        process.Exited += OnProcessExited;
    }

    /// <summary>
    /// Starts capturing output from the process
    /// </summary>
    /// <param name="process">The Xenia process</param>
    public void StartCapture(Process process)
    {
        _outputBuffer.Clear();
        _isGameLoading = false;
        _gameLoadCheckAttempts = 0;

        lock (_lock)
        {
            _loadedProfiles.Clear();
        }

        Logger.Info<XeniaOutputHandler>($"Starting output capture for {_game.Title}");

        // Begin asynchronous reading of output streams
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    /// <summary>
    /// Stops capturing output from the process
    /// </summary>
    /// <param name="process">The Xenia process</param>
    public void StopCapture(Process process)
    {
        process.OutputDataReceived -= OnOutputDataReceived;
        process.Exited -= OnProcessExited;

        lock (_lock)
        {
            Logger.Info<XeniaOutputHandler>($"Stopped output capture for {_game.Title}. Profiles loaded: {_loadedProfiles.Count}");
        }
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

        _outputBuffer.AppendLine(line);

        // Log the output
        Logger.Trace<XeniaOutputHandler>($"[Xenia] {line}");

        // Check for game loading events (only if the game hasn't started loading yet)
        if (!_isGameLoading)
        {
            CheckForGameLoad(line);
        }

        // Check for profile load events
        CheckForProfileLoad(line);
    }

    /// <summary>
    /// Checks if a line indicates the game is loading and extracts the game title
    /// Also tracks attempts and marks the game as loaded after max attempts
    /// </summary>
    /// <param name="line">The line to check</param>
    private void CheckForGameLoad(string line)
    {
        _gameLoadCheckAttempts++;

        // Check if we've exceeded max attempts and mark the game as loaded
        if (_gameLoadCheckAttempts >= MaxGameLoadCheckAttempts)
        {
            _isGameLoading = true;
            Logger.Info<XeniaOutputHandler>($"Game marked as loaded after {MaxGameLoadCheckAttempts} check attempts");
            GameLoadingStarted?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Fast pre-filter: skip lines that don't contain "Title name"
        if (!line.Contains("Title name", StringComparison.InvariantCulture))
        {
            return;
        }

        Match match = _titleNameRegex.Match(line);
        if (!match.Success)
        {
            return;
        }

        string gameTitle = match.Groups["Title"].Value.Trim();

        if (!_isGameLoading)
        {
            _isGameLoading = true;
            Logger.Info<XeniaOutputHandler>($"Game loading detected: {gameTitle}");
            GameLoadingStarted?.Invoke(this, EventArgs.Empty);
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
        if (!line.Contains("Loaded", StringComparison.InvariantCultureIgnoreCase) &&
            !line.Contains("logged_profile_slot", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        // First, try to match the [Profiles] section format: logged_profile_slot_X_xuid = "XUID"
        Match slotMatch = _profileSlotRegex.Match(line);
        if (slotMatch.Success)
        {
            string xuidString = slotMatch.Groups["XUID"].Value.ToUpperInvariant();
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
        Match match = _gamerProfilesRegex.Match(line);
        if (!match.Success)
        {
            return;
        }

        string gamertag = match.Groups["Gamertag"].Value;
        string guid = match.Groups["GUID"].Value.ToUpperInvariant();
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
    /// Adds a new profile or updates an existing one with gamertag information
    /// </summary>
    /// <param name="xuidValue">The XUID value</param>
    /// <param name="slot">The profile slot</param>
    /// <param name="gamertag">The gamertag (null if only XUID is known)</param>
    private void AddOrUpdateProfile(ulong xuidValue, int slot, string? gamertag)
    {
        lock (_lock)
        {
            // Check if a profile with this XUID already exists
            AccountInfo? existingProfile = _loadedProfiles.FirstOrDefault(p => p.Xuid.Value == xuidValue);

            if (existingProfile != null)
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
                Logger.Info<XeniaOutputHandler>($"Profile {(gamertag != null ? "loaded" : "detected")}: {(gamertag ?? "Unknown")} (XUID: {xuidValue:X16}, Slot: {slot})");
            }
        }
    }

    // Event handlers for process output
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        ProcessLine(e.Data ?? string.Empty);
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        Logger.Info<XeniaOutputHandler>($"Xenia process exited for {_game.Title}");
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
using System.Text.Json.Serialization;
using XeniaManager.Core.Constants;

namespace XeniaManager.Core.Models.Game;

/// <summary>
/// Grouping of all file locations related to the game (ISO, patch, configuration, and emulator)
/// </summary>
public class GameFiles
{
    /// <summary>
    /// Path to the game's ISO file (Disc 1 when the game has multiple discs)
    /// </summary>
    [JsonPropertyName("game")]
    public string Game { get; set; } = string.Empty;

    /// <summary>
    /// Paths to additional discs for multi-disc games (Disc 2, Disc 3, ...).
    /// Empty for single-disc games.
    /// </summary>
    [JsonPropertyName("additional_discs")]
    public List<GameDisc> AdditionalDiscs { get; set; } = [];

    /// <summary>
    /// Whether this game has more than one disc associated with it
    /// </summary>
    [JsonIgnore]
    public bool IsMultiDisc => AdditionalDiscs.Count > 0;

    /// <summary>
    /// Total number of discs associated with this game (including Disc 1)
    /// </summary>
    [JsonIgnore]
    public int DiscCount => 1 + AdditionalDiscs.Count;

    /// <summary>
    /// Path to the game's patch file
    /// </summary>
    [JsonPropertyName("patch")]
    public string? Patch { get; set; }

    /// <summary>
    /// Path to the game's configuration file
    /// </summary>
    [JsonPropertyName("config")]
    public string Config { get; set; } = string.Empty;

    /// <summary>
    /// The location of the custom Xenia executable (null if not applicable)
    /// </summary>
    [JsonPropertyName("custom_emulator_executable")]
    public string? CustomEmulatorExecutable { get; set; }

    /// <summary>
    /// Gets the resolved game file path, converting relative paths to absolute using the Games directory.
    /// </summary>
    [JsonIgnore]
    public string ResolvedGamePath => Path.IsPathRooted(Game)
        ? Game
        : Path.Combine(AppPaths.GamesDirectory, Game);

    /// <summary>
    /// Whether the game's path is valid
    /// </summary>
    [JsonIgnore]
    public bool IsGamePathValid => !string.IsNullOrEmpty(Game)
                                   && File.Exists(ResolvedGamePath);

    /// <summary>
    /// Returns the file path for the given disc number (1-based).
    /// Disc 1 returns <see cref="Game"/>; Disc 2+ returns the matching entry in <see cref="AdditionalDiscs"/>.
    /// Returns null if the disc number doesn't exist.
    /// </summary>
    public string? GetDiscPath(int discNumber)
    {
        if (discNumber <= 1)
        {
            return Game;
        }

        int index = discNumber - 2;
        if (index < 0 || index >= AdditionalDiscs.Count)
        {
            return null;
        }

        return AdditionalDiscs[index].Path;
    }
}

/// <summary>
/// Represents a single additional disc belonging to a multi-disc game
/// </summary>
public class GameDisc
{
    /// <summary>
    /// Disc number as shown to the user (2, 3, 4, ...)
    /// </summary>
    [JsonPropertyName("disc_number")]
    public int DiscNumber { get; set; }

    /// <summary>
    /// Path to this disc's game file
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom label for the disc (e.g. "Disco 2 - Città di Nod").
    /// Falls back to "Disc {DiscNumber}" when empty.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// Whether this disc's path currently points to an existing file
    /// </summary>
    [JsonIgnore]
    public bool IsPathValid => !string.IsNullOrEmpty(Path) && File.Exists(Path);
}
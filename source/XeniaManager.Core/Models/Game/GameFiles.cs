using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Game;

/// <summary>
/// Grouping of all file locations related to the game (ISO, patch, configuration, and emulator)
/// </summary>
public class GameFiles
{
    /// <summary>
    /// Path to the game's ISO file
    /// </summary>
    [JsonPropertyName("game")]
    public string Game { get; set; } = string.Empty;

    /// <summary>
    /// Path to the game's patch file
    /// </summary>
    [JsonPropertyName("patch")]
    public string Patch { get; set; } = string.Empty;

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
}
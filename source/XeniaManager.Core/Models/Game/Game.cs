using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Game;

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
    public string? Title { get; set; }

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
    public Compatibility? Compatibility { get; set; }

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
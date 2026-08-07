using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Game;

/// <summary>
/// User-defined collection of games for filtering the library.
/// </summary>
public class GameGroup
{
    /// <summary>
    /// Unique identifier for the group.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name of the group.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Keys of games that belong to this group.
    /// Keys are produced by <see cref="Manage.GroupManager.GetGameKey"/>.
    /// </summary>
    [JsonPropertyName("game_keys")]
    public List<string> GameKeys { get; set; } = [];
}

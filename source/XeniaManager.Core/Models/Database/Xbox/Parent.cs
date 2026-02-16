using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// Represents parent information for a game, including ID and title
/// </summary>
public class Parent
{
    /// <summary>
    /// Gets or sets the unique identifier for the parent
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the parent
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
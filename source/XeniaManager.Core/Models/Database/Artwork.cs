using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database;

/// <summary>
/// This is used to parse the "artwork" section of .JSON file
/// </summary>
public class Artwork
{
    // Universal
    [JsonPropertyName("boxart")]
    public string Boxart { get; set; }

    // Launchbox DB specific
    [JsonPropertyName("disc")]
    public string? Disc { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    // Xbox Marketplace specific
    [JsonPropertyName("background")]
    public string? Background { get; set; }

    [JsonPropertyName("banner")]
    public string? Banner { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("gallery")]
    public List<string>? Gallery { get; set; }
}